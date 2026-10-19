#!/usr/bin/env python3
"""Offline unit and real loopback HTTP integration checks. No provider calls/audio."""

from copy import deepcopy
import contextlib
import io
import json
from pathlib import Path
import socket
import subprocess
import sys
import threading
import time
import unittest
from unittest.mock import patch
import urllib.error
import urllib.request

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from backend.content import HISTORICAL, SOURCES
from backend.contracts import ProposalError, RequestError, StoryRequest, bundled_proposal, compose_story, strict_json, validate_proposal, validate_story
from backend.providers import Cancellation, Cancelled, GeminiProvider, GoogleAuth, GoogleNarrationProvider, ProviderError, StubProvider
from backend.server import PersonalStoryHTTPServer, PersonalStoryService

PUBLIC_REQUEST = {"region": "Nairobi", "period": "1850s", "interest": "journeys", "fragment": "A fictional traveller wonders what to carry home.", "question": "What is known about the water?"}


class ContractTests(unittest.TestCase):
    def test_aliases_are_explicit_and_coverage_is_not_inferred(self):
        request = StoryRequest.parse(PUBLIC_REQUEST)
        self.assertEqual((request.region, request.period), ("enkare-nairobi", "1850s"))
        for changes, expected in [({"region": "Kenya"}, "unsupported_region"), ({"region": "Accra"}, "unsupported_region"), ({"period": "today"}, "unsupported_period"), ({"interest": "ancestry"}, "unsupported_interest")]:
            with self.subTest(changes=changes), self.assertRaises(RequestError) as raised:
                StoryRequest.parse({**PUBLIC_REQUEST, **changes})
            self.assertEqual(raised.exception.code, expected)

    def test_plain_text_and_input_bounds(self):
        for changes in [{"fragment": "x" * 1001}, {"question": "x" * 501}, {"fragment": "🎒" * 501}, {"region": ["Nairobi"]}, {"fragment": "<color=red>test</color>"}, {"fragment": "bad\u0000text"}, {"fragment": "[link]"}, {"fragment": "wrong\u202edirection"}, {"fragment": "https://example.org"}, {"fragment": "\ud800"}, {"extra": "not accepted"}]:
            with self.subTest(changes=changes), self.assertRaises(RequestError):
                StoryRequest.parse({**PUBLIC_REQUEST, **changes})

    def test_duplicate_keys_and_nonfinite_json_rejected(self):
        for raw in ['{"region":"Nairobi","region":"Accra"}', '{"value":NaN}', "[" * 10000 + "]" * 10000]:
            with self.assertRaises(ValueError):
                strict_json(raw)

    def test_generated_contract_keeps_facts_exact_and_fragment_ephemeral(self):
        story, reason = PersonalStoryService(StubProvider()).create(PUBLIC_REQUEST, Cancellation())
        self.assertIsNone(reason)
        self.assertEqual(story["mode"], "generated")
        self.assertNotIn(PUBLIC_REQUEST["fragment"], json.dumps(story))
        self.assertNotIn(PUBLIC_REQUEST["question"], json.dumps(story))
        validate_story(story)
        self.assertEqual(story["chapters"][0]["narration"], HISTORICAL["C1"])
        for chapter in story["chapters"][2:]:
            self.assertEqual(chapter["truth"], "reconstructed")
            self.assertTrue(chapter["narration"].startswith("Imagined scene: "))

    def test_interests_change_story_action_not_just_metadata(self):
        service = PersonalStoryService(StubProvider())
        stories = [service.create({**PUBLIC_REQUEST, "interest": interest}, Cancellation())[0] for interest in ("journeys", "trade-household", "place-water")]
        self.assertEqual(len({s["chapters"][2]["narration"] for s in stories}), 3)
        self.assertEqual(len({s["chapters"][3]["narration"] for s in stories}), 3)

    def test_question_context_uses_only_exact_inventory_or_declares_limit(self):
        service = PersonalStoryService(StubProvider())
        for question, claim in [("What does Nairobi's name mean?", "C1"), ("What is known about trade?", "C6"), ("What is documented about a gourd?", "C7"), ("Who was my ancestor?", None)]:
            story, _ = service.create({**PUBLIC_REQUEST, "question": question}, Cancellation())
            answer = story["chapters"][-1]["questions"][-1]
            if claim:
                self.assertEqual((answer["truth"], answer["answer"], answer["sources"]), ("historical", HISTORICAL[claim], [claim]))
            else:
                self.assertEqual((answer["truth"], answer["sources"]), ("reconstructed", []))
                self.assertIn("cannot establish", answer["answer"])

    def test_bundled_fallback_is_honest_and_does_not_echo_input(self):
        story, reason = PersonalStoryService().create(PUBLIC_REQUEST, Cancellation())
        self.assertEqual((story["mode"], reason), ("bundled", "provider_not_configured"))
        self.assertNotIn(PUBLIC_REQUEST["fragment"], json.dumps(story))

    def test_historical_text_source_urls_and_art_cannot_be_laundered(self):
        story, _ = PersonalStoryService().create(PUBLIC_REQUEST, Cancellation())
        mutations = [lambda s: s["chapters"][0].update(narration="An unsupported claim."), lambda s: s["chapters"][0].update(sources=["C6"]), lambda s: s["sources"][0].update(url="https://example.org/invented"), lambda s: s["chapters"][2].update(illustration="https://example.org/picture.png"), lambda s: s["chapters"][2].update(truth="historical", sources=["C1"])]
        for mutate in mutations:
            changed = deepcopy(story)
            mutate(changed)
            with self.assertRaises(ProposalError):
                validate_story(changed)

    def test_provider_proposal_must_be_narrow_and_disclosed(self):
        mutations = [lambda p: p.update(sources=[SOURCES["C1"]]), lambda p: p.update(title="Your ancestors lived here"), lambda p: p["chapters"][0].update(title="Your grandfather was a trader"), lambda p: p["chapters"][0].update(truth="historical"), lambda p: p["chapters"][0].update(narration="This happened."), lambda p: p["chapters"][0].update(illustration="modern-river"), lambda p: p["chapters"][0].update(narration="Imagined scene: Your ancestors built this place."), lambda p: p["chapters"][0].update(narration="Imagined scene: Look at https://example.com."), lambda p: p["chapters"][0].update(narration="Imagined scene: <b>Read this</b>.")]
        for mutate in mutations:
            proposal = bundled_proposal("journeys")
            mutate(proposal)
            with self.assertRaises(ProposalError):
                validate_proposal(proposal)

    def test_invalid_provider_yields_bundled_not_partially_generated_story(self):
        class Invalid:
            def generate(self, request, cancellation):
                return {"title": "Invalid", "chapters": []}
        story, reason = PersonalStoryService(Invalid()).create(PUBLIC_REQUEST, Cancellation())
        self.assertEqual((story["mode"], reason), ("bundled", "provider_invalid_response"))

    def test_cancellation_never_creates_a_fallback(self):
        token = Cancellation()
        token.cancel()
        with self.assertRaises(Cancelled):
            PersonalStoryService(StubProvider()).create(PUBLIC_REQUEST, token)

    def test_provider_deadline_returns_bundled_without_connection_drop(self):
        class Deadline:
            def generate(self, request, cancellation):
                cancellation.deadline = time.monotonic() - 1
                raise ProviderError("provider_timeout")
        story, reason = PersonalStoryService(Deadline()).create(PUBLIC_REQUEST, Cancellation())
        self.assertEqual((story["mode"], reason), ("bundled", "provider_timeout"))

    def test_unexpected_adapter_exception_does_not_expose_input(self):
        class Broken:
            def generate(self, request, cancellation):
                raise RuntimeError("synthetic input that must not reach client")
        story, reason = PersonalStoryService(Broken()).create(PUBLIC_REQUEST, Cancellation())
        self.assertEqual((story["mode"], reason), ("bundled", "provider_unavailable"))
        self.assertNotIn("synthetic input", json.dumps(story))


class FakeResponse:
    def __init__(self, data, status=200):
        self.raw = io.BytesIO(data if isinstance(data, bytes) else json.dumps(data).encode())
        self.status = status
    def read1(self, size):
        return self.raw.read(size)


def fake_transport(response):
    class Connection:
        instances = []
        def __init__(self, host, timeout):
            self.host, self.timeout, self.sock = host, timeout, None
            self.closed = False
            self.__class__.instances.append(self)
        def request(self, method, path, body, headers):
            self.method, self.path, self.body, self.headers = method, path, strict_json(body), headers
        def getresponse(self):
            return response
        def close(self):
            self.closed = True
    return Connection


def gemini_response(proposal=None, finish="STOP"):
    return {"candidates": [{"finishReason": finish, "content": {"parts": [{"text": json.dumps(proposal or bundled_proposal("journeys"))}]}}]}


class ProviderTests(unittest.TestCase):
    def test_gemini_request_is_fixed_host_structured_only_supplied_context(self):
        transport = fake_transport(FakeResponse(gemini_response()))
        provider = GeminiProvider("synthetic-test-key", connection_factory=transport)
        provider.generate(StoryRequest.parse(PUBLIC_REQUEST), Cancellation())
        call = transport.instances[-1]
        self.assertEqual(call.host, "generativelanguage.googleapis.com")
        self.assertEqual(call.path, "/v1beta/models/gemini-3.8-flash:generateContent")
        self.assertNotIn("key=", call.path)
        self.assertTrue(call.closed)
        self.assertEqual(call.headers["x-goog-api-key"], "synthetic-test-key")
        self.assertEqual(call.body["generationConfig"]["responseFormat"]["text"]["mimeType"], "application/json")
        self.assertNotIn("tools", call.body)
        sent = strict_json(call.body["contents"][0]["parts"][0]["text"])
        self.assertEqual(sent["approved_context"], HISTORICAL)
        self.assertEqual(set(sent), {"approved_context", "approved_illustrations", "visitor_input"})

    def test_provider_errors_do_not_echo_remote_body_or_secret(self):
        transport = fake_transport(FakeResponse(b"SYNTHETIC PRIVATE ERROR BODY", status=403))
        with self.assertRaises(ProviderError) as raised:
            GeminiProvider("synthetic-secret", connection_factory=transport).generate(StoryRequest.parse(PUBLIC_REQUEST), Cancellation())
        self.assertEqual(str(raised.exception), "provider_rejected")
        self.assertNotIn("synthetic-secret", str(raised.exception))

    def test_vertex_uses_explicit_project_fixed_google_host_and_compatible_schema(self):
        transport = fake_transport(FakeResponse(gemini_response()))
        provider = GeminiProvider(project="synthetic-project", backend="vertex", model="gemini-2.5-flash", connection_factory=transport)
        with patch.object(provider._auth, "headers", return_value={"Authorization": "Bearer synthetic"}):
            provider.generate(StoryRequest.parse(PUBLIC_REQUEST), Cancellation())
        call = transport.instances[-1]
        self.assertEqual(call.host, "aiplatform.googleapis.com")
        self.assertEqual(call.path, "/v1/projects/synthetic-project/locations/global/publishers/google/models/gemini-2.5-flash:generateContent")
        self.assertIn("responseJsonSchema", call.body["generationConfig"])
        self.assertNotIn("responseFormat", call.body["generationConfig"])

    def test_truncated_blocked_invalid_and_oversized_responses_fail_closed(self):
        bodies = [gemini_response(finish="MAX_TOKENS"), {"promptFeedback": {"blockReason": "SAFETY"}}, b"not JSON", b" " * 65537, gemini_response({"title": "wrong", "chapters": []})]
        for body in bodies:
            transport = fake_transport(FakeResponse(body))
            with self.subTest(body_type=type(body).__name__), self.assertRaises(ProviderError):
                GeminiProvider("synthetic", connection_factory=transport).generate(StoryRequest.parse(PUBLIC_REQUEST), Cancellation())

    def test_oauth_requires_explicit_project_and_never_prints_token(self):
        with patch("backend.providers.subprocess.run") as run:
            with self.assertRaises(ProviderError):
                GoogleAuth().headers(Cancellation())
            run.assert_not_called()
            run.return_value = subprocess.CompletedProcess([], 0, "synthetic-oauth-token\n", "")
            output = io.StringIO()
            with contextlib.redirect_stdout(output), contextlib.redirect_stderr(output):
                headers = GoogleAuth(project="synthetic-project").headers(Cancellation())
            self.assertEqual(output.getvalue(), "")
            self.assertEqual(headers["Authorization"], "Bearer synthetic-oauth-token")
            self.assertTrue(run.call_args.kwargs["capture_output"])
            self.assertEqual(run.call_args.args[0], ["gcloud", "auth", "print-access-token"])

    def test_oauth_error_does_not_include_stderr(self):
        with patch("backend.providers.subprocess.run", side_effect=subprocess.CalledProcessError(1, "gcloud", stderr="synthetic-secret")):
            with self.assertRaises(ProviderError) as raised:
                GoogleAuth(project="synthetic-project").headers(Cancellation())
        self.assertEqual(str(raised.exception), "provider_auth_unavailable")

    def test_narration_uses_text_only_and_returns_transient_mp3(self):
        transport = fake_transport(FakeResponse({"audioContent": "SUQzAQID"}))
        narrator = GoogleNarrationProvider("synthetic-project", connection_factory=transport)
        with patch.object(narrator._auth, "headers", return_value={"Authorization": "Bearer synthetic"}):
            self.assertEqual(narrator.synthesize(HISTORICAL["C1"], Cancellation()), b"ID3\x01\x02\x03")
        call = transport.instances[-1]
        self.assertEqual(call.host, "texttospeech.googleapis.com")
        self.assertEqual(call.body["audioConfig"]["audioEncoding"], "MP3")
        self.assertEqual(call.body["input"]["text"], HISTORICAL["C1"])


class DummyNarrator:
    def synthesize(self, narration, cancellation):
        cancellation.remaining()
        return b"ID3synthetic-not-played"


class HTTPTests(unittest.TestCase):
    def setUp(self):
        self.output = io.StringIO()
        self.stdout = contextlib.redirect_stdout(self.output)
        self.stderr = contextlib.redirect_stderr(self.output)
        self.stdout.__enter__()
        self.stderr.__enter__()
        self.server = PersonalStoryHTTPServer(("127.0.0.1", 0), PersonalStoryService(StubProvider(), DummyNarrator()), request_timeout=0.4)
        self.thread = threading.Thread(target=self.server.serve_forever, kwargs={"poll_interval": 0.01}, daemon=True)
        self.thread.start()
        self.url = "http://127.0.0.1:" + str(self.server.server_port)

    def tearDown(self):
        self.server.shutdown()
        self.server.server_close()
        self.thread.join(2)
        self.stderr.__exit__(None, None, None)
        self.stdout.__exit__(None, None, None)
        self.assertEqual(self.output.getvalue(), "", "The default service must not log request content")

    def request(self, path, data=None, raw=None, headers=None):
        headers = {"Content-Type": "application/json", **(headers or {})}
        payload = json.dumps(data).encode() if data is not None else raw
        req = urllib.request.Request(self.url + path, data=payload, headers=headers)
        try:
            response = urllib.request.urlopen(req, timeout=3)
        except urllib.error.HTTPError as error:
            response = error
        with response:
            body = response.read()
            return response.status, response.headers, strict_json(body) if response.headers.get_content_type() == "application/json" else body

    def test_health_coverage_and_create_story(self):
        self.assertEqual(self.request("/health")[2]["generation"], "configured")
        self.assertEqual(self.request("/coverage")[2]["regions"][0]["id"], "enkare-nairobi")
        status, headers, story = self.request("/create-story", PUBLIC_REQUEST)
        self.assertEqual(status, 200)
        self.assertEqual(headers["Cache-Control"], "no-store")
        self.assertEqual(headers["X-Sankofa-Mode"], "generated")
        self.assertEqual(story["schema"], "sankofa.personal-story.v1")

    def test_http_rejects_invalid_unsupported_oversized_and_unknown(self):
        for path, data, raw, expected in [("/create-story", {**PUBLIC_REQUEST, "region": "Accra"}, None, 422), ("/create-story", None, b"not-json", 400), ("/create-story", None, b" " * 17000, 413), ("/unknown", None, None, 404)]:
            with self.subTest(path=path):
                self.assertEqual(self.request(path, data, raw)[0], expected)

    def test_browser_origin_and_optional_service_token(self):
        self.assertEqual(self.request("/health", headers={"Origin": "https://example.org"})[0], 403)
        self.server.service_token = "synthetic-service-token"
        self.assertEqual(self.request("/health")[0], 401)
        self.assertEqual(self.request("/health", headers={"Authorization": "Bearer synthetic-service-token"})[0], 200)

    def test_narration_requires_unchanged_issued_story(self):
        _, headers, story = self.request("/create-story", PUBLIC_REQUEST)
        request = {"story": story, "chapter_id": "imagined-1", "token": headers["X-Sankofa-Narration-Token"]}
        status, headers, audio = self.request("/narration", request)
        self.assertEqual((status, headers.get_content_type(), audio), (200, "audio/mpeg", b"ID3synthetic-not-played"))
        request["story"]["chapters"][2]["narration"] = "Imagined scene: Arbitrary text."
        self.assertEqual(self.request("/narration", request)[0], 422)

    def test_expired_narration_token_and_missing_chapter(self):
        story, _ = self.server.service.create(PUBLIC_REQUEST, Cancellation())
        request = {"story": story, "chapter_id": "imagined-1", "token": self.server.service.signature(story, int(time.time()) - 1)}
        self.assertEqual(self.request("/narration", request)[0], 422)
        request.update(token=self.server.service.signature(story), chapter_id="missing")
        self.assertEqual(self.request("/narration", request)[0], 422)

    def test_provider_failure_returns_labelled_bundled_story(self):
        class Failure:
            def generate(self, request, cancellation):
                raise ProviderError("provider_unavailable")
        self.server.service.provider = Failure()
        status, headers, story = self.request("/create-story", PUBLIC_REQUEST)
        self.assertEqual((status, story["mode"], headers["X-Sankofa-Fallback-Reason"]), (200, "bundled", "provider_unavailable"))

    def test_busy_capacity_returns_bounded_client_error(self):
        for _ in range(4):
            self.assertTrue(self.server.generation_slots.acquire(blocking=False))
        try:
            status, _, body = self.request("/create-story", PUBLIC_REQUEST)
            self.assertEqual((status, body["error"]["code"]), (429, "busy"))
        finally:
            for _ in range(4):
                self.server.generation_slots.release()

    def test_http_timeout_cancels_provider_and_returns_bundled(self):
        stopped = threading.Event()
        class Slow:
            def generate(self, request, cancellation):
                cancellation.event.wait(2)
                stopped.set()
                raise Cancelled()
        self.server.service.provider = Slow()
        status, headers, story = self.request("/create-story", PUBLIC_REQUEST)
        self.assertEqual((status, story["mode"], headers["X-Sankofa-Fallback-Reason"]), (200, "bundled", "provider_timeout"))
        self.assertTrue(stopped.wait(1))

    def test_client_disconnect_cancels_generation(self):
        started, stopped = threading.Event(), threading.Event()
        class Slow:
            def generate(self, request, cancellation):
                started.set()
                cancellation.event.wait(2)
                stopped.set()
                raise Cancelled()
        self.server.service.provider = Slow()
        body = json.dumps(PUBLIC_REQUEST).encode()
        client = socket.create_connection(self.server.server_address)
        client.sendall(b"POST /create-story HTTP/1.1\r\nHost: localhost\r\nContent-Type: application/json\r\nContent-Length: " + str(len(body)).encode() + b"\r\n\r\n" + body)
        self.assertTrue(started.wait(1))
        client.close()
        self.assertTrue(stopped.wait(1))


if __name__ == "__main__":
    if len(sys.argv) == 3 and sys.argv[1] == "--emit-fixtures":
        target = Path(sys.argv[2])
        target.mkdir(parents=True, exist_ok=True)
        for mode, provider in [("generated", StubProvider()), ("bundled", None)]:
            story, _ = PersonalStoryService(provider).create(PUBLIC_REQUEST, Cancellation())
            (target / (mode + ".json")).write_text(json.dumps(story, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        print("Emitted synthetic public contract fixtures.")
    else:
        unittest.main()
