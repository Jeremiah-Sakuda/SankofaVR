# Sankofa — Devpost draft

October 18–19, 2026 · Current personal story table MVP · Prepared copy; not published or submitted

This draft replaces the earlier gallery pitch. It reflects the [current scope](../product/personal-story-table-mvp.md) and [implementation record](../reviews/2026-10-18-personal-table-iteration.md). The Devpost session available during this work is signed out, and the edit destination remains unconfirmed. No live entry has been changed. The proposed division remains Adapted / Significantly Updated Entertainment, subject to eligibility and organiser classification.

## Tagline

**For the stories you never got to hear.**

## Short description

Bring a question to the fire. Explore an illustrated telling of history on a table beside you, shaped by your curiosity, with sourced context and imagined scenes.

## Inspiration

A story does not always survive as a story. Sometimes all that remains is a place name, an object, or a question nobody thought to ask.

The original web Sankofa invited people to bring those fragments to a warm storyteller. We wanted to give that invitation a physical place: a small fire, a journal within reach, and a telling you can spend time with in your own room.

Sankofa takes its name from the Akan idea of going back to retrieve what matters. Our first researched setting is Kenyan: Enkare Nairobi in the 1850s. The name's origin and the story's setting remain distinct. The ambition is broad; this prototype deliberately begins with one context.

## What it does

Choose journeys, household life and exchange, or place and water. Add a question with a hands-accessible keyboard, or begin with a complete prepared telling.

With the optional online service connected, your curiosity shapes the tension and outcome of two imagined scenes. The telling begins with the fictional situation, brings in documented context, and returns to the people and their decision. Changing only the question has produced different fictional situations and outcomes in the actual app: waiting for a late companion in one telling, facing a smaller flour delivery in another.

Every telling reaches a turning point where you choose whose side of the meeting to follow: the woman who waits or the one who is late, the one who brought less or the one who expected more, the one who remembers or the one being shown. Turn the page without choosing and the fire chooses for you, and says so. Each side ends differently, and the ending names the side you have not heard yet.

Illustrations keep the experience coherent. You turn pages, explore bounded contextual questions and sources, pause, and read at your own pace. The fire is the griot's visible presence: on the headset a warm voice reads each prepared page while the flame moves with it, and captions stay on throughout. There is no required microphone or live voice conversation.

On supported Quest hardware, passthrough can keep your room present. Fit my table requests local scene access and uses a nearby surface's height and dimensions to place the journal. An adjustable stand, height controls and Recenter provide a complete fallback. Actual room fitting and hands comfort still require headset acceptance.

At the ending, Keep is a deliberate choice. Reopening uses the same realised words, illustration references, questions and sources, without generating a replacement. You can replace or forget the one story kept on that device. Generated voice is transient, so a reopened generated telling may be caption-only.

Historical passages use a bounded source inventory. Imagined scenes are labelled as fiction; the experience does not recover relatives or establish ancestry. Wider places, languages and personal accounts need their own sources and review.

## How we built it

The Unity application separates the story contract, application session, local persistence, XR input and presentation. A strict shared boundary checks supported context, exact historical passages, source identity and local illustration IDs before accepting a telling. The application owns navigation and cancellation; a late provider response cannot replace a newer visit.

An optional Python service connects to Google Gemini and Cloud Text-to-Speech. Provider credentials remain on the server. Both client transports support limited service access credentials. The service uses fixed historical material and a small illustrated setting; it does not generate arbitrary 3D worlds or load model-selected URLs. Prepared stories remain complete when generation is unavailable, with accurate capability labels.

MR Utility Kit supports optional local table fitting. Room geometry stays in that adapter and never enters a story request. A kept story is separate from room placement.

## What changed from the original Sankofa

The [original web project](https://github.com/jeremiah-Sakuda/sankofa) established personal curiosity and the warm griot. SankofaVR is a separate Unity implementation; this MVP imports no web code, backend or media. The additions are spatial presentation, hands-accessible intake, local surface fitting and recovery, bounded validated generation, transparent offline completion and explicit realised-content replay. Earlier Quest gallery/theatre experiments are preserved as comparison records. The [adaptation summary](../product/adaptation.md) records that lineage.

## Assets and tools

Unity 6000.3.25f1, Meta XR/OpenXR and MR Utility Kit 207, TextMeshPro, engine-free C# modules, and the optional Python/Google service. Existing painted illustrations were produced with the OpenAI image-generation tool; the tool did not expose the exact model identifier. Prepared narration uses Google Cloud Text-to-Speech, `gemini-3.1-flash-tts-preview`, Kore. Provenance and unresolved release terms are recorded in the [asset register](../asset-register.md). No AI-generated music is used.

## Current verification — update before publication

358 core and 32 service checks pass after the October 26 iteration; the Unity editor and table-runtime suites must be rerun on the new scene before this line is published. An additional actual authenticated Unity HTTP smoke uses two public-synthetic question-only requests under the same selected interest and fetches/decodes real narration while remaining muted. These checks establish bounded software and transport behaviour. They do not establish listening quality, unfamiliar-visitor relevance, cultural acceptance or Quest usability/performance.

[Current build identity and limitations](candidate.md) must accompany a published entry. Public hosting, independent installation, judge access, rights, cultural/pronunciation review and actual-device acceptance remain open. The checked-in Android scene has no live endpoint/access credential and can complete prepared tellings offline.

## Judge instructions — draft

Begin seated. Choose an interest and optionally add a short question. Read a prepared telling, or inspect the complete online confirmation and deliberately choose Agree & create when the release service is configured. Turn a page, explore a contextual question and its sources, return to the same reading position, and pause/resume. At the ending, re-read or Keep. Reopen the same telling, then try another interest or question.

Try Fit my table when room access is available, then Recenter and the adjustable stand. Declining scene access must leave the visit usable. Narration is optional and begins disabled in the current review build.

## Fields requiring actual release evidence

| Field | Current status |
|---|---|
| Devpost destination and authenticated edit session | Unconfirmed / signed out; this is prepared copy only |
| Eligibility, Start membership and final division | User/account/organiser confirmation pending |
| Competition release channel, independent installation and judge access | Not uploaded or verified |
| Configured HTTPS service | Local authenticated Google transport verified; public deployment/access/retention review pending |
| Demonstration URL | Not published; use current actual headset or clearly identified simulator footage |
| Quest hands, surface fitting, readability, reach and sustained minimum 60 fps | Actual-device evidence pending |
| Cultural, source, pronunciation/listening and unfamiliar-visitor review | Acceptance pending |
| Voice on every prepared page | Nine pages recorded; the three new perspective chapters are pending recording and play as captions until then |
| Applicable asset/provider terms | Provenance recorded; release clearance pending |
| Launch date and freeze/access plan | Final release decision pending |

The [official rules](https://start-developer-competition-26.devpost.com/rules) and [FAQ](https://start-developer-competition-26.devpost.com/details/faqs) control submission and adaptation requirements. This draft is neither an eligibility declaration nor authorisation to submit the final entry.
