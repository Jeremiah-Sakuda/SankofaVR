# Optional personal-story service

This development service generates two imagined passages around an explicit visitor request. It returns the shared `sankofa.personal-story.v1` contract with approved local art IDs. Historical narration and answers come verbatim from the same C1/C6/C7 inventory as the app's `PersonalStoryCatalog`; a model cannot add historical claims by attaching citations. The supported context is Enkare Nairobi in the 1850s, in English. Family, community, pronunciation and generated-prose review remain open.

This implements the provider-adapter part of PRD SK-03/04/05/10/14/16: bounded personalisation, context disclosure, application validation, transparent fallback and server-side credentials. Different freeform requests can change the imagined tension and resolution. It does not generate new images, geometry, music or live conversation. Optional narration reads the accepted chapter text. The Unity client still owns consent, cancellation, accepted state and explicit local save/replay.

## Run locally

From the repository root, with Python 3.10 or newer and no additional packages:

```sh
python3 -m backend.server
```

The default address is `127.0.0.1:8787`. Without explicit credentials it returns complete authored tellings labelled `mode: bundled`. It never treats those as new personal generations. Configuration is read from the process environment; `.env` files are not automatically loaded. [`.env.example`](.env.example) contains names only.

For the Gemini Developer API, configure `GEMINI_API_KEY` (or `GOOGLE_API_KEY`) server-side. The documented default model is `gemini-3.8-flash`; override with `SANKOFA_GEMINI_MODEL` if the account supports a different structured-output model. No key belongs in Unity.

Alternatively, explicitly configure `SANKOFA_GOOGLE_PROJECT` for captured `gcloud auth print-access-token` OAuth. `SANKOFA_GEMINI_BACKEND=vertex` selects the fixed Google Cloud endpoint with the explicit project; `developer` selects the Gemini Developer endpoint. The live synthetic test in this workspace succeeded with Vertex and `SANKOFA_GEMINI_MODEL=gemini-2.5-flash`. Developer API OAuth with the default model returned HTTP 403; the service changed no cloud configuration.

`SANKOFA_ENABLE_NARRATION=1` plus the explicit Google project enables transient MP3 narration through Cloud Text-to-Speech. The default `SANKOFA_TTS_MODEL` is `gemini-3.1-flash-tts-preview`, with the fixed Kore voice. Narration is optional and captions remain usable when it fails. No playback occurs in the service or tests.

`--host` and `--port` override the bind address. Non-loopback binding requires `SANKOFA_SERVICE_TOKEN`; clients then send `Authorization: Bearer <service token>` on every endpoint. This is a development HTTP server; internet hosting needs separate TLS, authentication and operational controls. No hosting or deployment is provided here.

## HTTP contract

- `GET /health`: local process health and configured capabilities. “Configured” does not establish provider account access.
- `GET /coverage`: explicit place, period, interests, language, draft-review status and actual configured text/audio capability. No browser origins are enabled.
- `POST /create-story`: uncompressed JSON, at most 16 KiB. Fields are `region`, `period`, `interest`, optional `fragment` (1000 UTF-16 units) and optional `question` (500). Plain text only. Supported interest IDs are `journeys`, `trade-household`, `place-water`. Region aliases are `Nairobi`, `Enkare Nairobi`, `Nairobi, Kenya`; period alias is `before the railway`. Unsupported context returns HTTP 422 before any provider call.

Example public synthetic request:

```json
{"region":"enkare-nairobi","period":"1850s","interest":"journeys","fragment":"A fictional traveller wonders whether to wait for a neighbour.","question":"What does the name of Nairobi refer to?"}
```

A successful response is the story JSON itself, with `X-Sankofa-Mode: generated|bundled`. Bundled responses also have a bounded `X-Sankofa-Fallback-Reason`. The service inserts exact historical passages, accepted reconstructed passages, known sources and approved art references. A freeform question shapes the imagined telling and can surface exact approved context about the name, trade or gourd; it does not enable live factual conversation. Imaginative questions receive a fiction explanation rather than a false failure notice; unsupported ancestry questions retain the evidence limit. Both performed scenes stay at the illustrated outdoor bank; known interior-location mismatches are rejected, without claiming complete semantic verification. The supplied fragment/question are not automatically echoed into the story or saved by this service.

When narration is configured, creation also returns `X-Sankofa-Narration-Token`. `POST /narration` accepts up to 32 KiB of JSON:

```json
{"story":{"schema":"sankofa.personal-story.v1"},"chapter_id":"imagined-1","token":"the response header value"}
```

Replace the abbreviated `story` with the complete unchanged creation response. The token authenticates that exact story for ten minutes using an ephemeral server key. Story mutation, missing chapters, expiry and a server restart invalidate it. A successful response is transient `audio/mpeg` bytes. Narration cannot be used as an arbitrary text proxy. The server stores neither the story nor audio. A client that promises replay with recorded narration must explicitly retain the realised MP3 locally when the visitor saves; it must not silently regenerate an expired recording.

All responses use `Cache-Control: no-store`. Client errors are JSON `{ "error": { "code": "...", "message": "..." } }`. Validation failure, provider rejection, malformed output or a 25-second generation deadline selects labelled bundled content; a narration deadline returns HTTP 504. Body reads are bounded to five seconds. Four generation/narration workers are allowed; further requests receive HTTP 429. Disconnects cancel the provider socket; stale results are discarded. There are no automatic provider retries.

## Privacy and evidence boundaries

No request-body/access/error logging, telemetry, database or story storage is enabled. Only the explicit request and the small approved context inventory are sent to Google during live generation. No private development brief, research file, room image, microphone recording or chat history is loaded. Narration sends an integrity-checked chapter to Google. Provider retention and applicable contractual terms require independent review before any privacy promise beyond the service's own behaviour.

Validation checks structure, text bounds, approved art IDs, source identity and exact historical wording. The reconstruction disclosure is mandatory. These checks do not independently verify meaning, cultural quality, narrative quality or all assertions in generated fictional prose. No model output is described as self-verified. Runtime/content review remains necessary.

No code or assets were imported from the separate AGPL web project.

## Verification

```sh
python3 scripts/test_personal_service.py
python3 scripts/test_personal_service.py --emit-fixtures /tmp/sankofa-personal-service-fixtures
```

The offline suite uses synthetic requests and deterministic providers, including real loopback HTTP checks for coverage, unsupported input, size limits, explicit fallback, no logging, timeouts, disconnect cancellation and narration-token tampering. The app's actual .NET reader/validator accepted both emitted modes and their round trips.

Only after authorizing ordinary provider testing against an explicit project:

```sh
python3 scripts/test_personal_service_live.py --project YOUR_EXPLICIT_PROJECT --backend vertex --model gemini-2.5-flash --narration
```

This makes two synthetic public requests with the same interest and different fragments/questions, checks that both imagined chapters differ, optionally synthesizes one chapter, and prints bounded status metadata. It never prints credentials or plays audio. `--fixtures-dir` optionally exports only these synthetic generated story results for contract/content inspection; raw inputs and audio are not exported. Do not add personal data to this script.

Official API/model references checked October 18, 2026: [Gemini structured output](https://ai.google.dev/gemini-api/docs/generate-content/structured-output), [Gemini 3.8 Flash](https://ai.google.dev/gemini-api/docs/models/gemini-3.8-flash), [Gemini OAuth](https://ai.google.dev/gemini-api/docs/oauth), [Vertex AI quickstart](https://docs.cloud.google.com/vertex-ai/generative-ai/docs/start/quickstart), [Vertex JSON output](https://docs.cloud.google.com/vertex-ai/generative-ai/docs/samples/generativeaionvertexai-gemini-controlled-generation-response-schema-2), [Cloud Gemini TTS](https://docs.cloud.google.com/text-to-speech/docs/gemini-tts).


## Unity connection and final smoke

The scene's `PersonalStoryApp` accepts an HTTPS service root and a limited `serviceAccessToken`; both creation and narration use the same Bearer header. For local Editor checks, `SANKOFA_STORY_SERVICE` and `SANKOFA_STORY_ACCESS_TOKEN` supply them without changing a scene. Android requires HTTPS; localhost on a headset is not the developer computer. The checked-in scene contains neither an endpoint nor a live access credential. A client token is extractable from a distributed APK and is not a replacement for server quotas, access expiry/rotation and proper deployment controls. Never use a Google credential as a client token.

The opt-in `Sankofa.Tests.PersonalTableLiveHttpTests` uses two public-synthetic question-only requests with the same interest through the actual Unity transport. It also fetches/decodes one real narration clip into memory with narration disabled. Run it separately with `SANKOFA_TEST_LIVE_SERVICE=1`, the local token environment and Unity `-noaudio`; do not run it implicitly in the regular offline suite. Its ignored response artifacts permit manual comparison of realised actions. The final local token-protected service is stopped after verification and its temporary token deleted. This is not a deployed judge service.
