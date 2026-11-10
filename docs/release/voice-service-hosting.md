# Hosting the voice service

November 10, 2026 · Prepared, not executed. Nothing is deployed, no cloud resource exists, no image was built, and no provider has been called for a question. This describes what hosting the optional [question-and-story service](../../backend/README.md) would take, so the entrant can decide.

"Ask the fire" lets a visitor ask one question about the page in front of them. With a hosted service and a provider, the question can be spoken and the griot answers in its own voice. Without either, the app raises the page's authored questions as embers. The second path is complete on its own and needs no network, microphone or account.

## Two decisions that are not made

| Decision | If the answer is no |
|---|---|
| Does the shipped build ask for the microphone? | The app sends typed questions only, or none. Recorded questions are never captured. The service code for recordings is simply unused |
| Is a service hosted for judges? | The build ships with no endpoint. Every question is answered from the authored embers on the device, and the app says so |

Either can be no without the other. A hosted service with no microphone still answers typed questions. Both must be settled before the final build, because the build cannot change afterwards.

## Competition constraints this rests on

These were relayed to this work as facts. The local competition brief was not in the checkout where this was written, so check each against the [official rules](https://start-developer-competition-26.devpost.com/rules) before relying on it.

- Every action must work with hands alone, so a spoken question is always optional.
- The core experience must stand when a third-party service is removed. The embers are that alternative, and must be labelled as prepared rather than generated.
- Nothing may change after the deadline of November 18, 3 p.m. Eastern, and the service must stay available to judges until December 11.

The last point shapes everything below. The service address and its access token are fixed inside the submitted build. After the deadline neither can be changed in the app, so the address must keep working and the token must keep being accepted for the whole judging period.

## What leaves the headset

| Sent | To | When |
|---|---|---|
| The telling in progress, the current page and a typed question | The hosted service | Each question |
| A recording of the visitor's voice, at most 12 seconds | The hosted service | Each spoken question, only if the microphone decision is yes |
| The pages read so far, the approved passages and the question or recording | Google, through the service | Only when a provider key is configured |
| An issued answer, to be read aloud | Google Text-to-Speech, through the service | Only when narration is configured |

The service keeps nothing: no request, recording, answer or log line with content. It cannot speak for what Google retains. A voice recording is personal data. Before the microphone decision is yes, read the provider's current terms for audio, write the consent the visitor sees before the first recording, and check what the store requires a build that records audio to disclose. None of that has been done.

## What must be true before the final build

1. **A stable HTTPS address.** The Android build refuses plain HTTP and the app follows no redirects, so the exact address in the build must answer over TLS with a certificate that stays valid past December 11. A platform-provided address is enough; a custom domain adds a renewal to watch.
2. **TLS in front, not inside.** The Python process speaks plain HTTP and is not hardened for direct exposure. Put it behind the platform's own TLS endpoint or a reverse proxy, and let nothing else reach port 8787.
3. **The service token.** Generate one random value, set it as `SANKOFA_SERVICE_TOKEN` on the host and as the scene's `serviceAccessToken` at build time. The process refuses to listen beyond loopback without it. Anyone can extract it from the installed app, so it keeps out passers-by and nothing more. It cannot be rotated after the deadline without breaking the submitted build. Never use a Google credential as this token.
4. **Provider credentials on the host only.** `GEMINI_API_KEY` enables generated answers. Voiced answers also need `SANKOFA_GOOGLE_PROJECT` and `SANKOFA_ENABLE_NARRATION=1`; see [the limit of the plain image](#the-image-and-its-limit). No key belongs in the repository, the image or the app.
5. **Limits set deliberately.** See below.
6. **A budget set with the provider.** The service's own cap lives in memory and clears on restart. A quota or budget alert in the provider's console is the limit that survives.

```bash
python3 -c "import secrets; print(secrets.token_urlsafe(32))"
```

## Limits

The [service limits](../../backend/README.md#limits) count every accepted request that can reach a provider: story creation, questions and narration.

| Setting | Default | Meaning |
|---|---|---|
| `SANKOFA_RATE_LIMIT_PER_MINUTE` | 20 | Requests per service token in any 60 seconds. With one token in every installed copy, this is a limit on the whole service |
| `SANKOFA_DAILY_CAP` | 500 | Requests per UTC day. `0` closes the provider endpoints |

A spoken question that is answered aloud is two requests, one to answer and one to voice. The defaults therefore allow about 250 voiced answers a day and ten in any minute across all judges. To size the worst case, multiply the daily cap by the provider's current price for one answer and one short narration, then by the days to December 11. No price is quoted here because none was checked.

The limits cannot tell one judge from another. If that matters, add a per-address limit at the proxy. Also set there: a request body limit of 600 KiB, a connection limit, and a timeout of about 40 seconds.

## The image and its limit

`backend/Dockerfile` copies the service's Python files and the app's prepared tellings into `python:3.13-slim` and runs as an unprivileged user. Building it with the `backend` folder as the context keeps the competition brief, references and Unity project out of the image. It was written here and not built: no Docker daemon was available. As a stand-in, the service was started from only the files the image copies, with a clean environment and the image's own command. It refused to start without a token, returned 401 without the header, answered the app's own request file in bundled mode, and returned 429 at a daily cap of two.

```bash
docker build -t sankofa-voice backend
```

```bash
docker run --rm -p 127.0.0.1:8787:8787 -e SANKOFA_SERVICE_TOKEN -e GEMINI_API_KEY -e SANKOFA_RATE_LIMIT_PER_MINUTE=20 -e SANKOFA_DAILY_CAP=500 sankofa-voice
```

**Voiced answers do not work in this image as it stands.** The narration adapter gets its Google access token by running `gcloud auth print-access-token`, and the image has no `gcloud`. Typed and spoken questions are answered as text with only `GEMINI_API_KEY`; the app would show the answer as a caption with no griot voice. Two routes exist and neither has been tried: run the service on a machine that has the Cloud SDK and an attached service account, or base the image on Google's Cloud SDK image and give the container an identity that `gcloud` can use. Either needs a real test before it is counted on.

## Check it before the deadline

From a network outside the host, with the real address and token:

```bash
curl -sS -o /dev/null -w "%{http_code}\n" https://YOUR-ADDRESS/health
```

```bash
curl -sS -H "Authorization: Bearer $SANKOFA_SERVICE_TOKEN" https://YOUR-ADDRESS/health
```

```bash
curl -sS -D - -H "Authorization: Bearer $SANKOFA_SERVICE_TOKEN" -H "Content-Type: application/json" --data-binary @dotnet/Sankofa.Tests/Fixtures/ask/request-text.json https://YOUR-ADDRESS/ask
```

The first must print `401`. The second must show `"answers":"configured"` if a provider key is set, and `"narration":true` only if voiced answers are meant to work. The third sends a public synthetic question about a prepared telling: expect `X-Sankofa-Mode: generated` with a provider, or `bundled` with `X-Sankofa-Fallback-Reason` without one. Then ask a question in the installed build itself, once with the service reachable and once in aeroplane mode, and confirm the embers appear in the second case.

## What to watch until December 11

| Watch | Where | Why |
|---|---|---|
| `limits.used_today`, `answers`, `narration` | `GET /health` with the token | How close the day is to its cap, and whether a restart lost configuration |
| Request counts by status, especially 429 and 5xx, and response time | The platform's or proxy's own metrics | The service logs nothing itself. Keep proxy logs free of request bodies |
| Usage and spend | The provider's console | The only durable spending limit |
| Certificate expiry and restarts | The platform | A restart clears the day's count and makes outstanding story and answer proofs invalid; the app then falls back for that question |

A reply of `mode: bundled` with `provider_rejected` or `provider_auth_unavailable` usually means a key or sign-in has lapsed.

## Switching it off

In order of severity, each taking effect on restart:

1. **Stop provider spending, keep answering.** Remove `GEMINI_API_KEY` and the narration settings. Questions still get the authored bundled reply, labelled as such.
2. **Close the provider endpoints.** Set `SANKOFA_DAILY_CAP=0`. Every question, creation and narration request returns HTTP 429 `daily_cap_reached`.
3. **Stop the service.** Requests fail and the app uses its own embers.

In every case the app must fall back to the authored questions without an error the visitor has to dismiss. Any of these also removes the online feature for a judge who has not yet played, so they are for a real problem, not routine caution. Whether switching the service off during judging is allowed at all is a rules question to check first.

## Not done

- No deployment, account, domain, certificate, key or budget exists.
- The image was not built or run.
- No real provider has received a question or a recording. The request form for audio follows the existing Gemini adapter and is checked only against a fake transport.
- The Unity app does not yet record, send a question, or show an answer. The simulator and a headset have seen none of this.
- Provider terms for audio, the consent wording and store disclosure have not been reviewed.
