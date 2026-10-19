"""Run with python3 -m backend.server. Loopback and bundled-only by default."""

import argparse
from concurrent.futures import Future
import hashlib
import hmac
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
import json
import os
import select
import socket
import threading
import time

from .content import coverage
from .contracts import ProposalError, RequestError, StoryRequest, bundled_proposal, compose_story, strict_json, validate_story
from .providers import Cancellation, Cancelled, DEFAULT_MODEL, GeminiProvider, GoogleNarrationProvider, ProviderError

MAX_REQUEST_BYTES = 16 * 1024
MAX_NARRATION_REQUEST_BYTES = 32 * 1024


class PersonalStoryService:
    def __init__(self, provider=None, narration_provider=None):
        self.provider, self.narration_provider = provider, narration_provider
        self._signing_key = os.urandom(32)

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

    def signature(self, story, expires=None):
        expires = int(time.time()) + 600 if expires is None else expires
        body = str(expires).encode("ascii") + b"." + json.dumps(story, sort_keys=True, separators=(",", ":"), ensure_ascii=False).encode("utf-8")
        return str(expires) + "." + hmac.new(self._signing_key, body, hashlib.sha256).hexdigest()

    def narrate(self, data, cancellation):
        if not self.narration_provider:
            raise RequestError("narration_unavailable", "Optional generated narration is not configured. Read captions or use the bundled experience.", 503)
        if not isinstance(data, dict) or set(data) != {"story", "chapter_id", "token"} or not isinstance(data["story"], dict) or not isinstance(data["chapter_id"], str) or not isinstance(data["token"], str):
            raise RequestError("invalid_request", "Supply a service-issued story, chapter_id and narration token.")
        try:
            expiry, _ = data["token"].split(".", 1)
            expiry = int(expiry)
            if not int(time.time()) <= expiry <= int(time.time()) + 600 or not hmac.compare_digest(data["token"], self.signature(data["story"], expiry)):
                raise ValueError()
            validate_story(data["story"])
            chapter = next(c for c in data["story"]["chapters"] if c["id"] == data["chapter_id"])
        except (ValueError, KeyError, TypeError, StopIteration, ProposalError):
            raise RequestError("invalid_narration_token", "Narration requires an unchanged, recently issued story from this service.", 422) from None
        cancellation.remaining()
        return self.narration_provider.synthesize(chapter["narration"], cancellation)


class PersonalStoryHTTPServer(ThreadingHTTPServer):
    daemon_threads = True
    block_on_close = False

    def __init__(self, address, service, request_timeout=25.0, service_token=None):
        self.service, self.request_timeout, self.service_token = service, request_timeout, service_token
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
        if self.path == "/health":
            self._json(200, {"status": "ok", "generation": "configured" if self.server.service.provider else "bundled-only", "narration": bool(self.server.service.narration_provider)})
        elif self.path == "/coverage":
            result = coverage()
            result["capabilities"]["generated_text"] = bool(self.server.service.provider)
            result["capabilities"]["generated_audio"] = bool(self.server.service.narration_provider)
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
        limit = MAX_NARRATION_REQUEST_BYTES if self.path == "/narration" else MAX_REQUEST_BYTES
        if length < 1 or length > limit:
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
        if self.path not in {"/create-story", "/narration"}:
            self._json(404, {"error": {"code": "not_found", "message": "Unknown endpoint."}})
            return
        cancellation = Cancellation(self.server.request_timeout)
        acquired = False
        worker_started = False
        try:
            data = self._read_json()
            if self.path == "/create-story":
                StoryRequest.parse(data)  # Reject unsupported input before any provider call.
            acquired = self.server.generation_slots.acquire(blocking=False)
            if not acquired:
                raise RequestError("busy", "The service is busy. Try the bundled story or retry later.", 429)
            future = Future()
            def generate():
                try:
                    value = self.server.service.create(data, cancellation) if self.path == "/create-story" else self.server.service.narrate(data, cancellation)
                    future.set_result(value)
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
                        story = self.server.service.fallback(data)
                        self._respond_story(story, "provider_timeout")
                        return
                    raise RequestError("narration_timeout", "Narration took too long. Captions remain available.", 504)
                cancellation.event.wait(0.03)
            value = future.result()
            if self.path == "/create-story":
                self._respond_story(*value)
            else:
                self._bytes(200, value, "audio/mpeg")
        except RequestError as error:
            self._json(error.status, {"error": {"code": error.code, "message": error.message}})
        except ProviderError:
            self._json(503, {"error": {"code": "narration_unavailable", "message": "Generated narration is unavailable. Captions remain available."}})
        except Cancelled:
            pass
        except Exception:
            self._json(500, {"error": {"code": "internal_error", "message": "The service could not complete the request."}})
        finally:
            cancellation.cancel()
            if acquired and not worker_started:
                self.server.generation_slots.release()

    def _respond_story(self, story, reason):
        headers = {"X-Sankofa-Mode": story["mode"]}
        if reason:
            headers["X-Sankofa-Fallback-Reason"] = reason
        if self.server.service.narration_provider:
            headers["X-Sankofa-Narration-Token"] = self.server.service.signature(story)
        self._json(200, story, headers)


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
        with PersonalStoryHTTPServer((args.host, args.port), service, service_token=token) as server:
            server.serve_forever(poll_interval=0.2)
    except KeyboardInterrupt:
        pass
    except (OSError, ValueError):
        parser.exit(1, "The service could not start. Check local address and explicit configuration.\n")


if __name__ == "__main__":
    main()
