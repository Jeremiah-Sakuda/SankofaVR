# Adaptation summary

October 8, 2026 · How Sankofa for Quest adapts the original web Sankofa. This summary is for the Adapted / Significantly Updated division; the organisers decide the classification. Software, simulator and device evidence are kept distinct, as elsewhere.

## The original

[Sankofa](https://github.com/Jeremiah-Sakuda/Sankofa) is the user's own web project ([submission](https://devpost.com/software/sankofa-y47f9p)), built for the Gemini Live Agent Challenge and licensed AGPL-3.0. Its README describes the following, as read on October 8, 2026:

- **What the visitor gives:** a few seeds, such as a family surname, a region, a time period and any fragments they know.
- **What the griot makes:** an AI griot plans a narrative arc, writes text labelled Historical, Cultural or Reconstructed, makes watercolour-style period images, and narrates each segment over an ambient soundscape. It also answers follow-up questions and holds live voice conversations.
- **The welcome:** a skippable 98-second pre-generated voice-over over a dark cinematic overlay, with timed lines, gold particles and a rotating Sankofa bird.
- **The stack:** Gemini models orchestrated with Google's Agent Development Kit, a FastAPI backend, a Next.js front end and Cloud Run.

## Timeline

| Date | Event | Evidence |
|---|---|---|
| March 7, 2026 | Web Sankofa repository created and first commit | GitHub repository metadata and commit history, read October 10, 2026 |
| April 29, 2026 | Last commit to the web repository's default branch | Same |
| September 24, 2026 | Competition entry period opens | Official rules |
| September 30, 2026 | This repository's first commit | `git log` |

The web project existed before the entry period, and the Quest adaptation has been built during it.

## What carries over

| From the web | In Quest |
|---|---|
| The griot as narrator, and the *Sankofa* idea of going back for what was left | The fire is the griot. The visit ends with a keepsake carried home, and the continuation to today names the word |
| Labelling what is documented, cultural or reconstructed | Each line cites the [source register](../content/enkare-nairobi/sources.md) or is reconstruction. Medium-confidence claims are held back until verified. In each era a visitor can ask "Is this a real place?" |
| A cinematic welcome with a timed voice | A start screen, then the fire's welcome. The 98-second timing is a reference, not a gate ([PRD](prd.md)) |
| Gemini text-to-speech narration | The same family of voice, pre-rendered for all 85 lines, so a visit needs no network |
| Ambient soundscapes | Field recordings from Freesound, credited in the app. Nothing is synthesised and there is no music |

## What is new

- **Spatial staging.** The visitor sits at a night fire. Each era is staged around them without moving them: the 1850s trading place at dusk, the 1899 railway camp and today's city river. They enter through the flame.
- **Hands.** The visitor holds out a palm, carries and receives things, touches the rail, the gate or the soil, and points and pinches at panels. Every essential step also progresses passively, and controllers are optional.
- **People as firelight.** Faceless figures of ember and smoke walk in, kneel, hand things over and leave. Each family line has its own colour.
- **Consequence and continuity.** The visitor's actions change what the griot says. A kept visit opens the continuation to today, carrying the visitor's own bead or seed, and replays its path and outcomes exactly (questions asked are not replayed). What the visitor lingered on rises back out of the fire at home and again as the continuation opens. Saving is explicit.
- **Offline by design.** The full story is bundled, with no backend.

## What was not reused

No source code, images, audio or music from the web repository are in this repository. Two pieces of the user's own writing carry over: the welcome's opening lines, and the griot's voice direction (voice Kore, delivered as a griot), which the narration script uses with a newer model; see the [asset register](../asset-register.md). Quest tooling and a few small engine-free helpers (a JSON reader, the poke gesture and panel, project setup and build guards) were ported from the user's own earlier Nafasi project; see [workspace provenance](../workspace-provenance.md). The Quest application, its story core and its content were written for this competition; see [workspace provenance](../workspace-provenance.md). Under the PRD's rules, any later import of web code would need its AGPL, contributor and dependency terms assessed first.

## Deliberately deferred

The web project's live generation from a visitor's own family seeds is not in this build. The PRD keeps it out of the MVP: a model would weave only the visitor's own fragments into the framing, labelled as theirs, and never infer ancestry. Live voice conversation is also deferred. Quest personalisation today comes from what the visitor chooses and does.
