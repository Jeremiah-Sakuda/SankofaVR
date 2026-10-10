# Sankofa — delivery and validation plan

Version 1.0 · September 30, 2026 · Planning estimates; no implementation or device acceptance implied

The [PRD](prd.md) defines the product. This plan sequences the fire narrator, personalised story system and competition evidence. The [backlog](../backlog.md) is the execution checklist.

## 1. Starting point

This fresh SankofaVR repository has no runtime implementation yet. Two external foundations may inform deliberate reuse:

- **Web Sankofa:** public source for heritage context, narrative planning, narration, live conversation, sessions and the cinematic intro. Inspected read-only; not imported or run in this workspace. Reuse must be assessed against its AGPL-3.0-or-later licence, ownership/contributors and individual asset terms.
- **Previous Nafasi workspace:** a Unity project, platform setup, build/privacy protections and tested music-specific core. It contains prior simulator evidence but no recorded headset validation. Capture fidelity and musical performance gates are historical, not dependencies of Sankofa.

The previous Nafasi project uses Unity 6000.3.25f1, Meta XR Core 207.0.0, OpenXR 1.18.0 and the built-in renderer. MRUK and Meta Interaction SDK are not currently declared. New SDKs or a renderer change need an explicit need, compatible versions and a device build. Neither a Unity application, generated-world import nor live narration is implemented here.

The user requested this separate SankofaVR folder and repository after approving the PRD. The previous repository and its music scenes remain intact. Introduce the Sankofa Unity project, scene, content schema and application identifiers here during implementation; record any deliberate imports from the previous workspace. See [workspace provenance](../workspace-provenance.md).

## 2. Capacity and delivery gates

Weekly hours, current Quest access and tool/art/audio/API budget remain open. The following estimate is a planning judgement for the complete proposed P0, assuming meaningful reuse and existing assets. It excludes waiting for hardware, external contributors and organiser replies.

| Work | Focused hours |
|---|---:|
| Device setup, hands and application shell | 12–20 |
| Fire, entry/return transition and visual integration | 20–34 |
| Reviewed context, scene assets, narration languages and audio | 30–50 |
| Story planning, evidence/asset validation and narration preparation | 24–40 |
| Live conversation, interruption and resume | 12–22 |
| Memory, replay, offline flow and accessibility | 16–28 |
| Device testing, content evaluation and revisions | 18–30 |
| Submission and external access check | 6–10 |
| **Base estimate** | **138–234** |
| **Including 20% reserve, rounded up** | **166–281** |

September 30 to the November 16 internal target is approximately 6.7 weeks. A 10–15-hour week supplies roughly 67–101 hours, below this estimate. Full P0 currently implies roughly 25–42 focused hours/week including reserve. Do not present the whole scope as a confirmed fit for a solo part-time schedule. Use the first two probes to replace speculation with measured effort.

If capacity is insufficient, remove P1 first, simplify scenic detail and source reusable assets. Then make an explicit product decision about live conversational depth or language coverage. Do not silently turn personalised stories into a fixed film, drop the hands path, or claim untested coverage to meet a deadline. A reduced entry must be described honestly and reflected in the PRD.

## 3. Milestones

Dates are target windows, not established commitments. A failed gate changes subsequent scope rather than compressing acceptance work into the final day.

| Gate | Target window | Deliverable | Evidence required to proceed |
|---|---|---|---|
| G0 — foundation | September 30–October 4 | Select context candidate; confirm hardware/capacity; record web baseline and reuse plan | Named device-access plan, source/reviewer availability, minimum budgets and documented licence path |
| G1 — fire presence | October 1–7 | 30–45-second seated hearth, invitation, entry and return; no humanoid host | User review plus actual Quest depth, motion, hands and performance evidence; simulator alone leaves gate open |
| G2 — personalisation | October 1–9 | Two contrasting inputs become valid, observably different scene plans | Paired plan review; source and asset checks; unsupported requests handled explicitly; no expensive geometry generation at runtime |
| G3 — integrated visit | October 8–20 | Hearth → personalised story → hand consequence → ending → hearth | Hands-only and supported speech paths; first complete device visit; sound and visuals reviewed together |
| G4 — release breadth | October 21–November 2 | Three routes, second narration language, replay/continuation and full fallback | PRD personalisation, language and memory checks; permission-denied and offline completion |
| G5 — acceptance | November 3–12 | Content, novice-user, reliability and sustained-device evaluation; fixes | Recorded targets and exceptions; all P0 paths accepted; P1 stays excluded unless independently qualified |
| G6 — submission | November 13–16 | Frozen candidate, adaptation comparison, captions/credits, testing instructions and genuine footage | External tester can install/use the actual candidate; rights and eligibility questions resolved |

November 17–18 is reserved for access or submission problems, not new features. The official deadline is November 18 at noon Pacific / 3 p.m. Eastern. Verify the current [submission rules](https://start-developer-competition-26.devpost.com/rules) before upload.

## 4. The first two probes

### Fire and transition

Prepare one visual direction and a small scene: textured ground/seating, authored fire, opening voice, two fire states, one inspectable object and one stationary transition. Evaluate one generated environment candidate against an editable mesh scene if both can be obtained within the experiment allowance. Cap initial exploration at 8–12 focused hours before reviewing results; this is a discovery allowance, not a promise of finished art.

Inspect real depth, scale, fire transparency cost, object handling, voice location, hand visibility and return control. Stop expanding the scene when a basic presence or performance issue remains. Record whether a reviewer actually used a headset or only viewed captured footage.

### Generated performance

Define the reviewed source facts, two interests and a small legal scene vocabulary. Generate structured plans and narration without needing the final environment. Cap this first screen at 4–6 focused hours before evaluating. Compare outputs for scene-level differences, grounding, continuity and complete endings. Reject descriptions of unavailable objects and transformations.

The probes can be developed independently. Integration follows once both establish a credible direction. Neither is a substitute for the full PRD acceptance sample.

## 5. Proposed architecture and responsibilities

The [modular architecture](../architecture.md) defines dependency direction, adapter boundaries and replacement tests for these responsibilities. Read the local competition brief through [agent instructions](../../AGENTS.md) before implementation; it guides prioritisation without becoming a runtime dependency.

| Component | Responsibility | Boundary |
|---|---|---|
| Unity client | Fire, hands, scene state, audio/caption playback, local journal and controls | Owns what actually happens; feedback does not wait for inference |
| Context and content service | Approved source packages, coverage, asset capability catalogue and versions | No silent generic-cultural fallback |
| Story planner | Confirmed intent to bounded plan, then narration and captions | Must pass schema, evidence, continuity and asset checks |
| Conversation adapter | Supported live questions, interruptions and current-scene context | Cannot override story facts or trigger arbitrary code/URLs |
| Validation layer | Stable IDs, legal transitions, locale support, evidence links and cancellation | Reject stale, malformed and missing-asset actions |
| Production pipeline | Reference art, meshes/splats, objects, sound and language review | Generates assets before release; every import has provenance and a performance review |
| Saved visit store | Realised plans, cues, narration, selected content versions and keepsakes | Replay uses saved content; continuation creates a new visit |

Assess reusable Sankofa modules before porting. Extend or replace its text/image/audio segment representation with scene entities, stable IDs, beat state and source links. Existing historical/cultural/reconstructed labels do not establish independent verification. Do not migrate old personal histories or user data by default.

Create a standalone story core with no engine dependencies where useful for plan validation, state transitions and journal tests. Keep music fixtures and tests separate. Select a renderer/importer through a measured sample, not a vendor's export-format list.

## 6. Provider and content decisions

- Evaluate a current conversational model for English dialogue and a narration pipeline for the two proposed languages. Measure pronunciation, interruption, voice continuity, captions and latency together. Current provider documentation is a shortlist, not acceptance.
- Set a per-visit service-cost ceiling and monthly development allowance before paid integration. Record measured calls, retries, audio duration, generation and storage cost. No numerical budget is assumed here.
- Ship locally cached welcome/fallback assets and retain the realised narration needed for permitted saved replay. Confirm output-storage and redistribution terms before selecting a provider.
- Select the place/period, themes, story objects and fluent/content reviewers before producing many visuals. Source human-made music if used; narration and ambience can carry the first proof without a score.
- Prepare a focused organiser clarification about identifiable historical/family people and any actual unresolved rights issue. Sending it requires the user's instruction; preparing the question does not block unaffected prototype work.

## 7. Verification plan

Documentation checks run now. Runtime checks below belong to implementation:

| Level | Meaningful checks |
|---|---|
| Pure logic | Schema/evidence validation; incompatible objects; irreversible reveals; stale reply rejection; exact replay; explicit migration |
| Service adapter | Real or recorded representative replies, disconnection, invalid output, timeouts, cancellation and cost limits |
| Unity/simulator | Scene wiring, hand alternatives, pause/resume, captions and local fallback; label as simulator evidence |
| Standalone hardware | Fire/transition frame cost, warm-session stability, reach, input, audio, text and headset removal |
| Content review | Cultural specificity, grounded claims, reconstruction disclosure, pronunciation and translated meaning |
| User review | Welcome, first hand action, recognised consequence, story clarity and a later return visit |

The PRD's sample sizes and target timings define the intended acceptance evidence. Keep software results, device measurements and participant feedback separate. Run focused tests after behaviour changes; do not repeatedly run the historical music suite for prose-only edits.

## 8. Release evidence and adaptation record

Record the original Sankofa revision and capture its existing welcome/interaction when authorised to run it. Keep a changelog of new Unity presence, hand interactions, spatial staging, personalisation changes and visit continuity. Archive tested builds and media locally; commit source, synthetic fixtures and concise evidence summaries only.

Before submission, confirm the supported-device list, actual language/coverage matrix, credits, failure instructions, installer access and public descriptions. The demo must show real implemented behaviour and identify simulator footage when used. The entry should present the visitor experience first, followed by clear before/after adaptation evidence.

### Freeze checklist

Each item needs its evidence recorded in [decisions](../decisions.md) before the candidate is frozen. Owners: **repo** can be done in this repository; **human** needs the user, a reviewer or a headset.

| Item | Owner | Evidence |
|---|---|---|
| All .NET, EditMode and PlayMode tests pass on the frozen commit | repo | Test result files for that commit |
| Release APK signed with the project's own keystore, not the debug key | human (keystore) | `apksigner verify --print-certs` output |
| Supported devices and Meta telemetry setting decided and set in the manifest | human (decision) | Manifest diff |
| Hands-only run of both eras and the continuation on a Quest | human (headset) | Notes and real footage, labelled as device evidence |
| Matatu and other city recordings listened through in full | human | Note in the [asset register](../asset-register.md) |
| Gemini preview-model terms checked for the narration voice | human | Link and date in the asset register |
| Family or community review status stated accurately in the app and documents | human | Review notes, or the draft label retained |
| Original music source resolved, or the build kept without music | human | Licence record, or no music in the build |
| Competition release channel created, APK uploaded, invite link tested from a second account | human | Channel name and the date of the test |
| Testing guide updated with the invite link and checked against the frozen build | repo, after the channel exists | [Testing guide](testing-guide.md) |
| Frozen commit tagged; the APK and its hash archived outside Git | repo | Tag name and SHA-256 |
| No changes to the submitted build or its channel after the deadline | human | Channel history |

## Current evidence

At document creation, only repository/source inspection and planning have occurred for this direction. No Sankofa code has been imported, no generation API has been called, no assets purchased, and no new Unity scene or headset test has been performed. Earlier Nafasi tests establish historical software behaviour only.

October 1, 2026: the first presence slice exists in software, with a placeholder story, .NET tests of the core and flow, Unity EditMode and PlayMode tests and a successful Android build (see the [Unity README](../../unity/README.md) and [decisions](../decisions.md)). No generation API has been called, no assets purchased and no headset, simulator-with-hands or participant test performed. The two probes' measured effort should now replace the capacity estimate once device testing begins.
