"""Run with python3 -m backend.server. Loopback and bundled-only by default."""

import argparse
from collections import deque
from concurrent.futures import Future
import hashlib
import hmac
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
import json
import os
import re
import select
import socket
import threading
import time

from .ask import MAX_AUDIO_BASE64, Inquiry, accept_answer, ask_fields, bundled_reply, revealed_pages
from .content import ANSWER_SCHEMA, AUTHORED_TELLINGS, coverage
from .contracts import ProposalError, RequestError, StoryRequest, bundled_proposal, compose_story, strict_json, validate_story
from .providers import Cancellation, Cancelled, DEFAULT_MODEL, GeminiProvider, GoogleNarrationProvider, ProviderError

MAX_REQUEST_BYTES = 16 * 1024
MAX_NARRATION_REQUEST_BYTES = 32 * 1024
# A story as /narration takes it, plus one recorded question of at most twelve seconds.
MAX_ASK_REQUEST_BYTES = 544 * 1024
assert MAX_ASK_REQUEST_BYTES >= MAX_NARRATION_REQUEST_BYTES + MAX_AUDIO_BASE64
TOKEN_SECONDS = 600
ASK_TIMEOUT = 12.0


class LimitReached(RequestError):
    def __init__(self, code, message, retry_after):
        super().__init__(code, message, 429)
        self.retry_after = max(1, int(retry_after))


class Limits:
    """In-memory guards on the requests that can reach a paid provider.

    One sliding minute for each service token and one UTC-day total for the whole process. Only counts are kept,
    never request content. A restart clears both, so a provider-side budget remains the durable limit.
    """
    DEFAULT_PER_MINUTE = 20
    DEFAULT_DAILY_CAP = 500

    def __init__(self, per_minute=DEFAULT_PER_MINUTE, daily_cap=DEFAULT_DAILY_CAP, clock=time.time):
        for value in (per_minute, daily_cap):
            if isinstance(value, bool) or not isinstance(value, int) or not 0 <= value <= 1_000_000:
                raise ValueError("A limit is a whole number from 0 to 1000000")
        self.per_minute, self.daily_cap, self._clock = per_minute, daily_cap, clock
        self._lock = threading.Lock()
        self._recent, self._day, self._used = {}, None, 0

    @classmethod
    def from_environment(cls):
        def read(name, default):
            raw = os.environ.get(name, "")
            if not raw:
                return default
            # A mistyped limit stops the service rather than silently becoming a larger default.
            if not re.fullmatch(r"[0-9]{1,7}", raw):
                raise ValueError("Invalid " + name)
            return int(raw)
        return cls(read("SANKOFA_RATE_LIMIT_PER_MINUTE", cls.DEFAULT_PER_MINUTE), read("SANKOFA_DAILY_CAP", cls.DEFAULT_DAILY_CAP))

    def _today(self, now):
        day = int(now // 86400)
        if day != self._day:
            self._day, self._used = day, 0
        return day

    def admit(self, key):
        """Count one request for this token, or raise LimitReached without counting it."""
        now = self._clock()
        with self._lock:
            day = self._today(now)
            if self._used >= self.daily_cap:
                raise LimitReached("daily_cap_reached", "Today's limit for this service has been reached. The prepared story and questions remain available.", (day + 1) * 86400 - now)
            recent = self._recent.setdefault(key, deque())
            while recent and recent[0] <= now - 60:
                recent.popleft()
            if len(recent) >= self.per_minute:
                raise LimitReached("rate_limited", "Too many requests in the last minute. Wait a moment, or use the prepared story and questions.", recent[0] + 60 - now if recent else 60)
            recent.append(now)
            self._used += 1

    def usage(self):
        with self._lock:
            self._today(self._clock())
            return {"per_minute": self.per_minute, "daily_cap": self.daily_cap, "used_today": self._used}


class PersonalStoryService:
    def __init__(self, provider=None, narration_provider=None):
        self.provider, self.narration_provider = provider, narration_provider
        self._signing_key = os.urandom(32)

    @property
    def answers(self):
        """Whether a configured provider can answer questions; otherwise every reply is the bundled form."""
        return callable(getattr(self.provider, "answer", None))

    @classmethod
    def from_environment(cls):
        key = os.environ.get("GEMINI_API_KEY") or os.environ.get("GOOGLE_API_KEY")
        project = os.environ.get("SANKOFA_GOOGLE_PROJECT")
        provider = GeminiProvider(key, os.environ.get("SANKOFA_GEMINI_MODEL") or DEFAULT_MODEL, project=project, backend=os.environ.get("SANKOFA_GEMINI_BACKEND") or "developer") if key or project else None
        narrator = GoogleNarrationProvider(project, os.environ.get("SANKOFA_TTS_MODEL") or "gemini-3.1-flash-tts-preview") if project and os.environ.get("SANKOFA_ENABLE_NARRATION") == "1" else None
        return cls(provider, narrator)

    def create(self, data, cancellation):
        request = StoryRequest.parse(data)
        cancellation.remaining()
        reason = "provider_not_configured"
        if self.provider:
            try:
                proposal = self.provider.generate(request, cancellation)
                cancellation.remaining()
                return compose_story(request, proposal, "generated"), None
            except ProviderError as error:
                reason = error.code
            except ProposalError:
                reason = "provider_invalid_response"
            except Cancelled:
                raise
            except Exception:
                # Adapter failures must not strand the complete authored path or
                # expose provider exception text that may contain personal input.
                reason = "provider_unavailable"
        if cancellation.event.is_set():
            raise Cancelled()
        return compose_story(request, bundled_proposal(request.interest), "bundled"), reason

    def fallback(self, data):
        request = StoryRequest.parse(data)
        return compose_story(request, bundled_proposal(request.interest), "bundled")

    def _sign(self, purpose, value, expires):
        expires = int(time.time()) + TOKEN_SECONDS if expires is None else expires
        body = str(expires).encode("ascii") + purpose + json.dumps(value, sort_keys=True, separators=(",", ":"), ensure_ascii=False).encode("utf-8")
        return str(expires) + "." + hmac.new(self._signing_key, body, hashlib.sha256).hexdigest()

    def signature(self, story, expires=None):
        return self._sign(b".", story, expires)

    def answer_signature(self, answer, expires=None):
        """Signed like a story, under its own purpose, so neither proof can stand in for the other."""
        return self._sign(b".answer.", answer, expires)

    def _issued(self, token, value, sign):
        """True only for an unexpired proof this process issued for exactly this value."""
        try:
            expiry = int(token.split(".", 1)[0])
            now = int(time.time())
            return now <= expiry <= now + TOKEN_SECONDS and hmac.compare_digest(token, sign(value, expiry))
        except (ValueError, TypeError):
            return False

    def inquiry(self, data):
        """Check a question before any provider call: its form, an unchanged telling, and the page it is asked beside.

        The telling is one this service issued, shown by its token, or one of the app's prepared tellings, which
        needs no token because it is compared with the authored text itself.
        """
        story, token, chapter_id, question, audio = ask_fields(data)
        try:
            if token is None:
                if AUTHORED_TELLINGS.get(story.get("id")) != story:
                    raise ValueError()
            else:
                if not self._issued(token, story, self.signature):
                    raise ValueError()
                validate_story(story)
            pages = revealed_pages(story, chapter_id)
        except (ValueError, KeyError, TypeError, ProposalError):
            raise RequestError("invalid_story_token", "A question needs an unchanged telling: one this service issued recently, or one of the app's prepared tellings.", 422) from None
        if not pages:
            raise RequestError("unknown_chapter", "A question is asked beside a page of its telling.", 422)
        return Inquiry(story, pages[-1], pages, question, audio)

    def ask(self, inquiry, cancellation):
        """One provider call at most. Anything short of an accepted answer is the labelled bundled reply."""
        reason = "provider_not_configured"
        if self.answers:
            try:
                cancellation.remaining()
                proposal = self.provider.answer(inquiry, cancellation)
                cancellation.remaining()
                return accept_answer(proposal, inquiry), None
            except ProviderError as error:
                reason = error.code
            except ProposalError:
                reason = "provider_invalid_response"
            except Cancelled:
                raise
            except Exception:
                # As in create: never strand the authored path or expose exception text that may hold the question.
                reason = "provider_unavailable"
        if cancellation.event.is_set():
            raise Cancelled()
        return bundled_reply(inquiry), reason

    def ask_fallback(self, inquiry):
        return bundled_reply(inquiry)

    def narration_text(self, data):
        """The exact text a valid proof allows to be read aloud: a chapter of an issued story, or an issued answer."""
        if not self.narration_provider:
            raise RequestError("narration_unavailable", "Optional generated narration is not configured. Read captions or use the bundled experience.", 503)
        if isinstance(data, dict) and set(data) == {"answer", "token"} and isinstance(data["answer"], dict) and isinstance(data["token"], str):
            answer = data["answer"]
            if not self._issued(data["token"], answer, self.answer_signature) or answer.get("schema") != ANSWER_SCHEMA or answer.get("kind") != "answer" or not isinstance(answer.get("answer"), str):
                raise RequestError("invalid_narration_token", "Narration requires an unchanged, recently issued answer from this service.", 422)
            return answer["answer"]
        if not isinstance(data, dict) or set(data) != {"story", "chapter_id", "token"} or not isinstance(data["story"], dict) or not isinstance(data["chapter_id"], str) or not isinstance(data["token"], str):
            raise RequestError("invalid_request", "Supply a service-issued story, chapter_id and narration token, or a service-issued answer and its token.")
        try:
            if not self._issued(data["token"], data["story"], self.signature):
                raise ValueError()
            validate_story(data["story"])
            chapter = next(c for c in data["story"]["chapters"] if c["id"] == data["chapter_id"])
        except (ValueError, KeyError, TypeError, StopIteration, ProposalError):
            raise RequestError("invalid_narration_token", "Narration requires an unchanged, recently issued story from this service.", 422) from None
        return chapter["narration"]

    def speak(self, narration, cancellation):
        cancellation.remaining()
        return self.narration_provider.synthesize(narration, cancellation)

    def narrate(self, data, cancellation):
        return self.speak(self.narration_text(data), cancellation)


class PersonalStoryHTTPServer(ThreadingHTTPServer):
    daemon_threads = True
    block_on_close = False

    def __init__(self, address, service, request_timeout=25.0, service_token=None, limits=None, ask_timeout=None):
        self.service, self.request_timeout, self.service_token = service, request_timeout, service_token
        # A spoken answer that takes longer than this is no longer an answer; the prepared questions stand in.
        self.ask_timeout = min(request_timeout, ASK_TIMEOUT) if ask_timeout is None else ask_timeout
        self.limits = limits or Limits()
        self.generation_slots = threading.BoundedSemaphore(4)
        super().__init__(address, PersonalStoryHandler)

    def handle_error(self, request, client_address):
        # Default server tracebacks can expose request/local state. No payload logs.
        pass


class PersonalStoryHandler(BaseHTTPRequestHandler):
    server_version = "SankofaPersonalStory/1"
    sys_version = ""
    timeout = 5

    def log_message(self, format, *args):
        pass

    def send_error(self, code, message=None, explain=None):
        self._json(code, {"error": {"code": "http_error", "message": "Invalid HTTP request."}})

    def _authorized(self):
        if self.headers.get("Origin"):
            self._json(403, {"error": {"code": "browser_origin_denied", "message": "Browser origins are not enabled for this local service."}})
            return False
        token = self.server.service_token
        if token and not hmac.compare_digest(self.headers.get("Authorization", ""), "Bearer " + token):
            self._json(401, {"error": {"code": "unauthorized", "message": "A service access token is required."}})
            return False
        return True

    def _bytes(self, status, body, content_type, headers=None):
        try:
            self.send_response(status)
            self.send_header("Content-Type", content_type)
            self.send_header("Content-Length", str(len(body)))
            self.send_header("Cache-Control", "no-store")
            self.send_header("X-Content-Type-Options", "nosniff")
            self.send_header("Connection", "close")
            for key, value in (headers or {}).items():
                self.send_header(key, value)
            self.end_headers()
            self.wfile.write(body)
        except (BrokenPipeError, ConnectionResetError, OSError):
            pass
        self.close_connection = True

    def _json(self, status, value, headers=None):
        self._bytes(status, json.dumps(value, ensure_ascii=False, separators=(",", ":")).encode("utf-8"), "application/json; charset=utf-8", headers)

    def do_GET(self):
        if not self._authorized():
            return
        service = self.server.service
        if self.path == "/health":
            self._json(200, {"status": "ok", "generation": "configured" if service.provider else "bundled-only", "answers": "configured" if service.answers else "bundled-only", "narration": bool(service.narration_provider), "limits": self.server.limits.usage()})
        elif self.path == "/coverage":
            result = coverage()
            result["capabilities"]["generated_text"] = bool(service.provider)
            result["capabilities"]["generated_audio"] = bool(service.narration_provider)
            result["capabilities"]["generated_answers"] = result["capabilities"]["spoken_questions"] = service.answers
            self._json(200, result)
        else:
            self._json(404, {"error": {"code": "not_found", "message": "Unknown endpoint."}})

    def _read_json(self):
        if self.headers.get_content_type() != "application/json" or self.headers.get("Content-Encoding", "identity") != "identity" or self.headers.get("Transfer-Encoding"):
            raise RequestError("unsupported_content_type", "Send uncompressed application/json with Content-Length.", 415)
        lengths = self.headers.get_all("Content-Length", [])
        try:
            if len(lengths) != 1:
                raise ValueError()
            length = int(lengths[0])
        except (ValueError, TypeError):
            raise RequestError("invalid_content_length", "A single Content-Length is required.", 411) from None
        limit = {"/narration": MAX_NARRATION_REQUEST_BYTES, "/ask": MAX_ASK_REQUEST_BYTES}.get(self.path, MAX_REQUEST_BYTES)
        if length < 1 or length > limit:
            # A client still sending a slightly oversized body would see a dropped connection instead of this status,
            # so up to twice the limit is read and discarded first. Anything larger is refused unread.
            if 0 < length <= limit * 2:
                try:
                    remaining = length
                    while remaining > 0:
                        chunk = self.rfile.read(min(65536, remaining))
                        if not chunk:
                            break
                        remaining -= len(chunk)
                except (TimeoutError, OSError):
                    pass
            raise RequestError("request_too_large", "Request exceeds the endpoint size limit.", 413)
        try:
            raw = self.rfile.read(length)
            if len(raw) != length:
                raise ValueError()
            return strict_json(raw.decode("utf-8"))
        except (UnicodeError, ValueError, RecursionError):
            raise RequestError("invalid_json", "Request must be one valid UTF-8 JSON object.") from None
        except (TimeoutError, OSError):
            raise RequestError("request_timeout", "The request body did not arrive in time.", 408) from None

    def _disconnected(self):
        try:
            ready, _, _ = select.select([self.connection], [], [], 0)
            return bool(ready) and not self.connection.recv(1, socket.MSG_PEEK)
        except OSError:
            return True

    def do_POST(self):
        if not self._authorized():
            return
        if self.path not in {"/create-story", "/narration", "/ask"}:
            self._json(404, {"error": {"code": "not_found", "message": "Unknown endpoint."}})
            return
        service = self.server.service
        cancellation = None
        acquired = False
        worker_started = False
        try:
            data = self._read_json()
            # The deadline covers the work, not the upload. Unusable input is rejected before a worker slot,
            # a counted request or any provider call.
            cancellation = Cancellation(self.server.ask_timeout if self.path == "/ask" else self.server.request_timeout)
            if self.path == "/create-story":
                StoryRequest.parse(data)
                work = lambda: service.create(data, cancellation)
            elif self.path == "/ask":
                inquiry = service.inquiry(data)
                work = lambda: service.ask(inquiry, cancellation)
            else:
                spoken = service.narration_text(data)
                work = lambda: service.speak(spoken, cancellation)
            acquired = self.server.generation_slots.acquire(blocking=False)
            if not acquired:
                raise RequestError("busy", "The service is busy. Try the bundled story or retry later.", 429)
            self.server.limits.admit(hashlib.sha256((self.server.service_token or "").encode("utf-8")).hexdigest())
            future = Future()
            def generate():
                try:
                    future.set_result(work())
                except Exception as error:
                    future.set_exception(error)
                finally:
                    self.server.generation_slots.release()
            threading.Thread(target=generate, daemon=True).start()
            worker_started = True
            while not future.done():
                if self._disconnected():
                    cancellation.cancel()
                    return
                if time.monotonic() >= cancellation.deadline:
                    cancellation.cancel()
                    if self.path == "/create-story":
                        story = service.fallback(data)
                        self._respond_story(story, "provider_timeout")
                        return
                    if self.path == "/ask":
                        self._respond_answer(service.ask_fallback(inquiry), "provider_timeout")
                        return
                    raise RequestError("narration_timeout", "Narration took too long. Captions remain available.", 504)
                cancellation.event.wait(0.03)
            value = future.result()
            if self.path == "/create-story":
                self._respond_story(*value)
            elif self.path == "/ask":
                self._respond_answer(*value)
            else:
                self._bytes(200, value, "audio/mpeg")
        except RequestError as error:
            retry = getattr(error, "retry_after", None)
            self._json(error.status, {"error": {"code": error.code, "message": error.message}}, {"Retry-After": str(retry)} if retry else None)
        except ProviderError:
            self._json(503, {"error": {"code": "narration_unavailable", "message": "Generated narration is unavailable. Captions remain available."}})
        except Cancelled:
            pass
        except Exception:
            self._json(500, {"error": {"code": "internal_error", "message": "The service could not complete the request."}})
        finally:
            if cancellation:
                cancellation.cancel()
            if acquired and not worker_started:
                self.server.generation_slots.release()

    def _respond_story(self, story, reason):
        token = self.server.service.signature(story)
        headers = {"X-Sankofa-Mode": story["mode"], "X-Sankofa-Story-Token": token}
        if reason:
            headers["X-Sankofa-Fallback-Reason"] = reason
        if self.server.service.narration_provider:
            headers["X-Sankofa-Narration-Token"] = token
        self._json(200, story, headers)

    def _respond_answer(self, answer, reason):
        headers = {"X-Sankofa-Mode": answer["mode"]}
        if reason:
            headers["X-Sankofa-Fallback-Reason"] = reason
        if answer["kind"] == "answer" and self.server.service.narration_provider:
            headers["X-Sankofa-Answer-Token"] = self.server.service.answer_signature(answer)
        self._json(200, answer, headers)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=8787)
    args = parser.parse_args()
    token = os.environ.get("SANKOFA_SERVICE_TOKEN")
    if args.host not in {"127.0.0.1", "localhost"} and not token:
        parser.error("Non-loopback access requires an explicit SANKOFA_SERVICE_TOKEN.")
    try:
        service = PersonalStoryService.from_environment()
        with PersonalStoryHTTPServer((args.host, args.port), service, service_token=token, limits=Limits.from_environment()) as server:
            server.serve_forever(poll_interval=0.2)
    except KeyboardInterrupt:
        pass
    except (OSError, ValueError):
        parser.exit(1, "The service could not start. Check local address and explicit configuration.\n")


if __name__ == "__main__":
    main()
