# Sankofa — submission draft

Prepared October 18, 2026 · Personal story table MVP · Local draft only; not a published entry or eligibility declaration

This copy follows the [current scope](../product/personal-story-table-mvp.md). Unity integration and release acceptance are in progress. Confirm every implemented/device statement against the final candidate before publishing. The available Devpost browser session is logged out; no live entry has been edited or submitted. The prior gallery [release record](candidate.md) remains evidence for that earlier build.

## Tagline

**Sankofa — For the stories you never got to hear.**

## Short description

Bring an inherited fragment or a question to a warm fire. Sankofa turns your curiosity into an illustrated telling beside you, with sourced history, openly imagined scenes and a story you can choose to keep.

## About the experience

A place name. A fragment of a family story. A question you never got to ask.

Sankofa begins with what you bring. A warm griot, present as a small fire, welcomes you to a story table. Choose an interest or type a question with your hands. The telling unfolds as illustrated pages, with historical context and imagined scenes clearly distinguished. You can pause, open a contextual question, look at the sources and continue at your own pace.

The first context is Enkare Nairobi in the 1850s. Journeys and encounters, trade and household life, and place and water lead to different tellings. When the optional online service is configured and available, Gemini creates reconstructed scenes around the visitor's curiosity inside a bounded historical and visual inventory. Otherwise complete prepared tellings are available and clearly labelled bundled. No fallback is presented as a newly generated story.

On compatible Quest hardware, the journal and fire can sit in the visitor's room. Local table fitting is intended to adjust the experience to a nearby surface; an adjustable virtual stand keeps the visit available without room data. This hardware behaviour is still awaiting actual-device acceptance.

At the end, keeping the story is your choice. Reopening a kept telling uses its saved words, illustration references, answers and sources without asking a model to rewrite it.

Sankofa does not recover ancestors, recreate a real relative or claim that an invented scene happened to your family. A personal account remains an account. Historical passages have bounded source references; imaginative scenes are labelled. The title's Akan origin and the Kenyan setting are distinct contexts, and cultural review remains an explicit release gate.

## What makes this spatial

The story has a place beside the visitor: a nearby journal, earlier pages and a small fire. Hands select, type, turn pages and open questions without a required microphone. A real table should affect height and usable space, while the virtual stand provides seated recovery. The submission demonstration must show these behaviours in the actual candidate, including their fallbacks; a desktop composition does not establish real-room placement or hand reliability.

## Implemented checkpoint and open integration

- An engine-free personal-story model, strict JSON boundary, exact historical/source checks and approved local illustration IDs.
- Three complete bundled tellings with different openings and developments; optional configured Gemini service adapter for reconstructed scenes, controlled-provider tests and loopback contract checks.
- Session rules for request cancellation, stale replies, pause, contextual questions and reopening saved realised content.
- New Unity story-table builder/view, hands keyboard and local MRUK placement adapters; application/persistence integration and final runtime checks are in progress.

At this checkpoint 342 .NET tests passed, and service fixtures passed the actual .NET parser/validator and roundtrip. These are software checks. Two actual Gemini responses to different public-synthetic fragments/questions also passed the .NET validator and roundtrip. This narrow live smoke, the final Unity suite, current APK, actual Quest use and participant/cultural acceptance remain separate evidence. Current checks are muted; narration-service code does not establish playback or listening acceptance.

## Adapted Experience lineage

The earlier [web Sankofa](https://github.com/Jeremiah-Sakuda/Sankofa) established the invitation to bring fragments to an AI griot. SankofaVR is a separate Unity implementation. No web code, backend or media were imported for this MVP. The [adaptation summary](../product/adaptation.md) records the predecessor and earlier Quest gallery experiments.

The proposed additions are a nearby illustrated story table, hands-accessible typed input, meaningful local surface fitting with recovery, bounded validated generation, clear offline capability labels and explicit reopening of realised content. The prior gallery's tangible exchanges remain comparison work. Confirm division/eligibility with the final evidence; this draft does not claim organiser approval.

## Technologies and asset disclosure

Unity 6000.3.25f1, Meta XR/OpenXR, MR Utility Kit 207, TextMeshPro, engine-free C# modules and an optional Python/Google Gemini service. Historical context and local illustration IDs are constrained by a shared contract. Existing illustrations were requested through the built-in OpenAI image-generation tool; an exact model identifier was not exposed. Dynamic image generation, live voice conversation, speech input and a second language are deferred. An optional narration adapter requires configuration and separate rights/listening acceptance. No AI-generated music is used. Retain applicable asset and font attributions from the [asset register](../asset-register.md).

## Judge instructions draft — verify against the final build

Begin seated at the story table. Choose Enkare Nairobi, the 1850s, and an interest. Use the hands keyboard to add a short question if desired. Choose the prepared telling, or deliberately consent to online creation when a configured service is available. Read the generated/bundled label, turn a page and open a contextual question and its source details. Pause and resume. At the ending, deliberately Keep, then reopen the saved telling. Try a different interest to see the change before the ending.

Use Fit my table only if supported room data is available; test Recenter and manual height controls as the fallback. Room geometry is not sent to the story service. The bundled visit does not require a microphone, personal identity or provider credential. Fully hands-only use, denied permissions, independent installation and actual device performance still require verification on the release device.

## Fields still requiring real evidence

| Field | Current status |
|---|---|
| Devpost edit destination and authenticated session | Draft only; available browser is logged out |
| Eligibility, Meta Start membership and selected division | User/account/organiser confirmation pending |
| New-scene APK, App ID, Competition channel and judge access | Final build/install/access record pending; older gallery APK is not the current MVP |
| Real-provider generation and deployment | Two public-synthetic Gemini results pass .NET validation; deployed consent, access, reliability and retention review remain pending |
| Public demonstration URL | Not published; use honest current headset or clearly identified simulator footage |
| Quest version, table fitting, hands, comfort and sustained performance | Actual-device evidence pending |
| Cultural, pronunciation/listening and unfamiliar-visitor review | No completed acceptance implied by software tests |
| Asset/narration rights and applicable provider terms | Existing provenance retained; release clearance remains open |
| Launch date | User decision pending |

Consult the current [official rules](https://start-developer-competition-26.devpost.com/rules) and [FAQ](https://start-developer-competition-26.devpost.com/details/faqs) before publishing. This draft does not replace those requirements or authorise an upload.
