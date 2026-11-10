# Optional personal-story service

This development service generates two imagined passages around an explicit visitor request. It returns the shared `sankofa.personal-story.v1` contract with approved local art IDs. Historical narration and answers come verbatim from the same C1/C6/C7 inventory as the app's `PersonalStoryCatalog`; a model cannot add historical claims by attaching citations. The supported context is Enkare Nairobi in the 1850s, in English. Family, community, pronunciation and generated-prose review remain open.

This implements the provider-adapter part of PRD SK-03/04/05/10/14/16: bounded personalisation, context disclosure, application validation, transparent fallback and server-side credentials. Different freeform requests can change the imagined tension and resolution. It does not generate new images, geometry, music or live conversation. Optional narration reads the accepted chapter text. The Unity client still owns consent, cancellation, accepted state and explicit local save/replay.

November 10, 2026: `POST /ask` adds one bounded question about the page a visitor is on, typed or recorded, with a labelled answer and a complete offline form ([contract below](#ask-the-fire)). It is a single question and a single reply, not a conversation. The service side is built and software-checked with fake providers; the Unity app does not call it yet, no real provider has answered a question, and nothing is hosted. Whether the shipped build asks for the microphone and whether a service is hosted are the entrant's decisions; see [hosting the voice service](../docs/release/voice-service-hosting.md).

## Run locally

From the repository root, with Python 3.10 or newer and no additional packages:

```sh
python3 -m backend.server
```

The default address is `127.0.0.1:8787`. Without explicit credentials it returns complete authored tellings labelled `mode: bundled`. It never treats those as new personal generations. Configuration is read from the process environment; `.env` files are not automatically loaded. [`.env.example`](.env.example) contains names only.

For the Gemini Developer API, configure `GEMINI_API_KEY` (or `GOOGLE_API_KEY`) server-side. The documented default model is `gemini-3.8-flash`; override with `SANKOFA_GEMINI_MODEL` if the account supports a different structured-output model. No key belongs in Unity.

Alternatively, explicitly configure `SANKOFA_GOOGLE_PROJECT` for captured `gcloud auth print-access-token` OAuth. `SANKOFA_GEMINI_BACKEND=vertex` selects the fixed Google Cloud endpoint with the explicit project; `developer` selects the Gemini Developer endpoint. The live synthetic test in this workspace succeeded with Vertex and `SANKOFA_GEMINI_MODEL=gemini-2.5-flash`. Developer API OAuth with the default model returned HTTP 403; the service changed no cloud configuration.

`SANKOFA_ENABLE_NARRATION=1` plus the explicit Google project enables transient MP3 narration through Cloud Text-to-Speech. The default `SANKOFA_TTS_MODEL` is `gemini-3.1-flash-tts-preview`, with the fixed Kore voice. Narration is optional and captions remain usable when it fails. No playback occurs in the service or tests.

`--host` and `--port` override the bind address. Non-loopback binding requires `SANKOFA_SERVICE_TOKEN`; clients then send `Authorization: Bearer <service token>` on every endpoint. This is a development HTTP server; internet hosting needs separate TLS, authentication and operational controls. No hosting or deployment is provided here; [hosting the voice service](../docs/release/voice-service-hosting.md) describes what it would take.

`SANKOFA_RATE_LIMIT_PER_MINUTE` (default 20) and `SANKOFA_DAILY_CAP` (default 500) bound the requests that can reach a provider; see [limits](#limits). A value that is not a whole number stops the service from starting, so a typing mistake never becomes a larger limit.

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

Creation always returns `X-Sankofa-Story-Token`, the same proof under a name that does not depend on narration, so a question can be asked about a created story when narration is off.

### Ask the fire

`POST /ask` accepts up to 544 KiB of JSON: the unchanged story, the page the visitor is on, and exactly one of a typed question or a recording.

```json
{"story":{"schema":"sankofa.personal-story.v1"},"token":"the X-Sankofa-Story-Token value","chapter_id":"imagined-2","question":"Why did she wait?"}
```

| Field | Rule |
|---|---|
| `story`, `token` | As `/narration` takes them: the complete unchanged creation response and its proof. `token` is left out only for one of the app's three prepared tellings, which the service recognises by comparing it with the authored text in `authored_tellings.json`; any difference is refused |
| `chapter_id` | A page of that story that can be reached by reading it |
| `question` | Plain text, at most 500 UTF-16 units |
| `audio` | Base64 WAV: 16 kHz, mono, 16-bit PCM, at most 12 seconds. A longer or larger recording is HTTP 413 `audio_too_large`; another format is HTTP 400 `invalid_audio` |

Both `question` and `audio`, or neither, is HTTP 400 `invalid_request`. A changed story, a wrong or expired token, or an unrecognised story without a token is HTTP 422 `invalid_story_token`; an unknown page is HTTP 422 `unknown_chapter`. All of these are decided before any provider call.

A successful reply is HTTP 200 with `X-Sankofa-Mode: generated|bundled` and one of three bodies:

```json
{"schema":"sankofa.personal-answer.v1","mode":"generated","kind":"answer","answer":"...","truth":"reconstructed","sources":[]}
{"schema":"sankofa.personal-answer.v1","mode":"bundled","kind":"answer","question_id":"museum-gourd","answer":"...","truth":"historical","sources":["C7"]}
{"schema":"sankofa.personal-answer.v1","mode":"bundled","kind":"questions","questions":[{"id":"museum-gourd","label":"What is documented about the gourd?"}]}
```

**With a provider.** One call receives the pages read so far, the approved passages and sources of the sources that story declares, and the question or recording. Pages after the current one, and the side of a choice that was not followed, are not sent, so an answer cannot give away a turn. The provider must return `answer`, `truth` and `sources`; the service then decides the label itself:

| `truth` | Given when | `sources` |
|---|---|---|
| `historical` | `answer` is exactly one approved passage that this story declares | That passage's one source |
| `reconstructed` | Any other accepted text, whatever label the provider gave it | None |
| `unknown` | The provider marked the question as outside this place and period. Its text is discarded and the fixed reply in `content.UNKNOWN_STORY` is sent | None |

An accepted answer is plain text of at most 60 words and 600 UTF-16 units. New wording that contains digits, a web address, an approved passage of another telling or a statement about the visitor's own family is refused. These checks bound the form and the label. They do not establish that imagined wording is accurate, appropriate or culturally sound, and a determined visitor can still steer what the model says within them. A reconstructed answer carries no spoken disclosure of its own: the client must show `truth` beside it.

**Without one.** With no provider, a provider error, a 12-second deadline or refused output, the reply is `mode: bundled` with `X-Sankofa-Fallback-Reason`. A typed question is compared, word by word, with the current page's authored questions: a word shared with a question counts twice a word shared with its answer, and a single best match returns that authored answer unchanged with its `question_id`. No shared word, a tie, or a recording returns the page's authored questions as `kind: questions`, which is an empty list on a page that has none. A bundled reply is never labelled generated. The app holds the same authored questions, so it can offer them with no service at all.

**Voicing an answer.** When narration is configured, a reply with `kind: answer` also carries `X-Sankofa-Answer-Token`. `POST /narration` then accepts `{"answer": <the reply, unchanged>, "token": "<that header>"}` and returns the same transient `audio/mpeg`. Answers are signed like stories, under a separate purpose, for ten minutes: a changed answer, a story's proof, an expired proof or a list of questions is HTTP 422 `invalid_narration_token`. Narration still cannot read arbitrary text.

### Limits

Every accepted `POST` that can reach a provider (`/create-story`, `/ask`, `/narration`) is counted once, whether or not a provider is configured. Requests refused for their content, for a missing service token or because the four workers are busy are not counted.

| Limit | Environment | Default | When reached |
|---|---|---|---|
| Requests per service token in any 60 seconds | `SANKOFA_RATE_LIMIT_PER_MINUTE` | 20 | HTTP 429 `rate_limited`, with `Retry-After` |
| Requests per UTC day, whole process | `SANKOFA_DAILY_CAP` | 500 | HTTP 429 `daily_cap_reached`, with `Retry-After` to the next UTC midnight |

`0` closes the endpoints. The service accepts one service token, so today the per-token limit is in effect one for the whole service. Counts live in memory and hold no request content; a restart clears them, so a budget set with the provider remains the durable limit. `GET /health` reports `limits.used_today`.

### Shared behaviour

All responses use `Cache-Control: no-store`. Client errors are JSON `{ "error": { "code": "...", "message": "..." } }`. Validation failure, provider rejection, malformed output or a 25-second generation deadline selects labelled bundled content; a narration deadline returns HTTP 504. Deadlines start once the request body has arrived. Each read of the body is bounded to five seconds. Four generation/answer/narration workers are allowed; further requests receive HTTP 429 `busy`. Disconnects cancel the provider socket; stale results are discarded. There are no automatic provider retries.

## Privacy and evidence boundaries

No request-body/access/error logging, telemetry, database or story storage is enabled. Only the explicit request and the small approved context inventory are sent to Google during live generation. No private development brief, research file, room image or chat history is loaded. Narration sends an integrity-checked chapter or issued answer to Google. Provider retention and applicable contractual terms require independent review before any privacy promise beyond the service's own behaviour.

A question sent to `/ask` is held in memory for that request only. With a provider configured, the typed question or the recording of the visitor's voice is sent to Google with the pages read so far; without one it goes no further than this process. The reply never repeats the question, and no transcript is produced. A recording of a voice is personal data: the app must ask before recording or sending one, and the provider's terms for audio need the same review as its terms for text.

Validation checks structure, text bounds, approved art IDs, source identity and exact historical wording. The reconstruction disclosure is mandatory. These checks do not independently verify meaning, cultural quality, narrative quality or all assertions in generated fictional prose. No model output is described as self-verified. Runtime/content review remains necessary.

No code or assets were imported from the separate AGPL web project.

## Verification

```sh
python3 scripts/test_personal_service.py
python3 scripts/test_personal_service.py --emit-fixtures /tmp/sankofa-personal-service-fixtures
python3 scripts/test_personal_service.py --emit-ask-fixtures dotnet/Sankofa.Tests/Fixtures/ask
```

The offline suite uses synthetic requests and deterministic providers, including real loopback HTTP checks for coverage, unsupported input, size limits, explicit fallback, no logging, timeouts, disconnect cancellation and narration-token tampering. The app's actual .NET reader/validator accepted both emitted modes and their round trips.

November 10: 66 checks pass, the earlier 32 and 34 for `/ask`: typed and recorded questions, both-or-neither, recording format and length, story proofs, the three labels, refused output, provider failure and timeout to the bundled reply, both limits, answer narration proofs, and a search of every reply, log record and console line for the question text and recording. The suites also check each other: `dotnet/Sankofa.Tests/Fixtures/ask/` holds request bodies written by the app's own writer, which this suite sends to the real service, and the replies this service emits for them, which the .NET suite reads with the app's reader. `authored_tellings.json` is written and checked by the .NET suite; after a deliberate change to a prepared telling, rewrite it with `SANKOFA_UPDATE_FIXTURES=1 dotnet test dotnet/Sankofa.Tests --filter PersonalServiceContractTests` and then the replies with the third command above. No real provider received a question or a recording, so the live request form for audio is unexercised.

Only after authorizing ordinary provider testing against an explicit project:

```sh
python3 scripts/test_personal_service_live.py --project YOUR_EXPLICIT_PROJECT --backend vertex --model gemini-2.5-flash --narration
```

This makes two synthetic public requests with the same interest and different fragments/questions, checks that both imagined chapters differ, optionally synthesizes one chapter, and prints bounded status metadata. It never prints credentials or plays audio. `--fixtures-dir` optionally exports only these synthetic generated story results for contract/content inspection; raw inputs and audio are not exported. Do not add personal data to this script.

Official API/model references checked October 18, 2026: [Gemini structured output](https://ai.google.dev/gemini-api/docs/generate-content/structured-output), [Gemini 3.8 Flash](https://ai.google.dev/gemini-api/docs/models/gemini-3.8-flash), [Gemini OAuth](https://ai.google.dev/gemini-api/docs/oauth), [Vertex AI quickstart](https://docs.cloud.google.com/vertex-ai/generative-ai/docs/start/quickstart), [Vertex JSON output](https://docs.cloud.google.com/vertex-ai/generative-ai/docs/samples/generativeaionvertexai-gemini-controlled-generation-response-schema-2), [Cloud Gemini TTS](https://docs.cloud.google.com/text-to-speech/docs/gemini-tts).


## Unity connection and final smoke

The scene's `PersonalStoryApp` accepts an HTTPS service root and a limited `serviceAccessToken`; both creation and narration use the same Bearer header. It does not call `/ask`; `PersonalAnswerJson` in the engine-free core writes those requests and reads the replies for the view that will. For local Editor checks, `SANKOFA_STORY_SERVICE` and `SANKOFA_STORY_ACCESS_TOKEN` supply them without changing a scene. Android requires HTTPS; localhost on a headset is not the developer computer. The checked-in scene contains neither an endpoint nor a live access credential. A client token is extractable from a distributed APK and is not a replacement for server quotas, access expiry/rotation and proper deployment controls. Never use a Google credential as a client token.

The opt-in `Sankofa.Tests.PersonalTableLiveHttpTests` uses two public-synthetic question-only requests with the same interest through the actual Unity transport. It also fetches/decodes one real narration clip into memory with narration disabled. Run it separately with `SANKOFA_TEST_LIVE_SERVICE=1`, the local token environment and Unity `-noaudio`; do not run it implicitly in the regular offline suite. Its ignored response artifacts permit manual comparison of realised actions. The final local token-protected service is stopped after verification and its temporary token deleted. This is not a deployed judge service.
