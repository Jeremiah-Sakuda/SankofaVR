# Sankofa — delivery and validation plan

Updated October 11, 2026 · Current sprint proposal for the illustrated-theatre direction

The [PRD](prd.md) records the broader product requirements; the [backlog](../backlog.md#current-illustrated-theatre-sprints) contains sprint tickets. The schedule below replaces the old milestone calendar for planning from the current build. The September baseline is preserved later in this file as history, not a description of the present repository.

## Current sprint plan

**Starting point:** illustrated package v0.3.0, one historical encounter plus a contemporary continuation, recorded narration, honest active/passive outcomes, questions, saved replay and recall. The first chapter is illustrated; the continuation still uses the earlier presentation. Software checks pass. Device performance, hands-only hardware completion, cultural acceptance, released asset rights and external installation remain unverified. The latest story changes are not in the old MVP APK.

**Recommendation:** finish a complete, convincing visit and a rewarding return visit. The next work is a short story/comprehension and device check, followed by visible listening and consistent art. The distinctive moment remains a tangible object crossing from a painted hand into the visitor's space, followed by an acknowledgement and a later memory of the actual action.

### Scope and capacity

Plan around **12–15 focused hours/week**, provisionally, until the user supplies current availability. Headset access is still an open dependency, not assumed absent or purchased. These are solo part-time timeboxes, not promises that unfamiliar Unity/device work fits them. Reviewer and hardware lead time is outside the hour estimates; request access early. No purchases, messages to reviewers or public submissions are authorised by this document.

The five sprints allocate **48–58 hours of planned work**, with **12–17 hours left for fixes and uncertainty** within a 60–75-hour envelope. Use the lower end for initial commitments. Re-estimate after Sprint 1 from observed effort; do not borrow the release buffer to conceal overruns.

| Scope | Proposed competition treatment | Status against the original PRD |
|---|---|---|
| Fire, illustrated encounter, tangible hand-off, truthful responses, questions, replay and contemporary return | Required for this candidate | Implemented in part; user/device/content acceptance remains open |
| English narration and captions; Maa names reviewed in context | Required | Additional-language requirement SK-06 remains unfulfilled |
| Authored scene and outcome variation | Describe exactly what changes | Generated planning and paired freeform-interest requirements SK-03/04 remain unfulfilled; do not claim generation |
| Live conversation | Defer; keep authored, hand-selectable questions | SK-09 remains open, not passed by recorded answers |
| Illustrated 1899 chapter | Conditional expansion after core gates pass | Existing baseline chapter is not an accepted illustrated conversion |
| MR, more cultures/languages, new AI services, rigged-character work | Outside this sprint commitment | Broader roadmap; no automatic promotion to deadline-critical work |

This is an explicit **recommended release profile**, not a declaration that the full PRD is complete. Before committing a release scope, reconcile the profile in the PRD/decision record; any decision to retain generation, conversation or another language requires an added estimate and a compensating scope or capacity change. The ambition is in the quality and embodied response of the experience, not in an untested feature count.

### Sprint calendar and exit gates

| Sprint | Dates / planned hours | Visitor outcome and principal work | Exit gate |
|---|---|---|---|
| **1 — Prove the encounter** | Oct 12–18 / 8–10 h | Review the story with insiders and unfamiliar readers; get the current build onto Quest; inspect the attention/response gap; start asset-rights and eligibility checks | First-device findings recorded; review corrections prioritised; one observable attention/explicit-action response specified; no unresolved major story misunderstanding hidden by new art |
| **2 — Make the theatre convincing** | Oct 19–25 / 12–14 h | Clear sheep silhouette; resolve hide/rock overlap and doubled water; match gourd/bead to painted lighting; implement illustrated acknowledgement; integrate reviewed costume/pose changes and audio/transition polish | User accepts the actual encounter in motion; sheep, hand-off and response read clearly; passive, pause and recenter paths remain truthful; device spot-check shows the art change is affordable |
| **3 — Make returning worthwhile** | Oct 26–Nov 1 / 10–12 h | Bring the contemporary coda into the same visual language; retain specific recall and gate choice; consolidate active story authoring/presentation wiring without breaking old saves | A kept visit survives relaunch, replays faithfully and opens a coherent illustrated continuation that recalls what actually happened; an unsaved visit makes no memory claim |
| **4 — Make it dependable** | Nov 2–8 / 10–12 h | Device profiling, full hands-only use, novice sessions, accessibility/recovery, final narration/cultural corrections | PRD formative target: at least 4/5 visitors finish unassisted and explain a consequence; 10 complete device visits; recorded performance/recovery evidence; no unresolved critical experience faults |
| **5 — Make the submission convincing** | Nov 9–15 / 8–10 h | Candidate APK, independent installation, concise real-footage demo, adaptation evidence, accurate coverage/credits/testing guide | Another person installs and completes the candidate; description matches the build; rights and access are cleared; reviewable submission package ready by Nov 15 |

**Nov 16:** internal submission target, with the user's authorisation for publishing/submitting. **Nov 17–18:** contingency for access/submission problems. The official deadline remains **Nov 18, 2026, noon Pacific / 3 p.m. Eastern**, checked October 11 against the [official rules](https://start-developer-competition-26.devpost.com/rules). Dates are planning targets, not scheduled automations.

### Sprint 1: start here

1. **Current-build/device baseline.** Build the v0.3 episode, record build/commit, and arrange Quest access. On the device, test the fire, seated depth, cutout edges, gourd reach, touch alternative, text, return and worst-looking transparent layers. A desktop capture can prepare this session but cannot close its gate.
2. **Story review before more illustration.** Prepare the current read-through, six cast drawings and a short question sheet for the user's family/community review. Ask an unfamiliar reader to explain what each woman needed, why Naserian waited, what the visitor changed and what is invented. Separately review names and Maa delivery with a fluent speaker. The user handles reviewer outreach unless they authorise sending it. Record specific corrections and review scope, not a blanket claim of authenticity.
3. **Define the response that people can see.** Trace what the visitor looks toward, asks and does against the actual narration/poses. Head direction may offer a question or a gentle acknowledgement; it must not take an object, choose a moral position or interrupt uninvited. Specify one in-scene acknowledgement for the illustrated cast and its passive/pause/recenter behavior before implementing new pose art in Sprint 2. Preserve the current hand-off and outcome-specific address/recall.
4. **Clear expensive uncertainties early.** Identify actual image provider/model and output terms where recoverable; never invent provenance. Decide replacement assets if necessary. Check narration terms, recorded-sound licences/incidental music, reviewer availability and entrant/Start access now, with final clearance before release. Select supported hardware after the first device session. These checks should prevent late rework, not start in submission week.

### Delivery rules and dependencies

- Each sprint ends with a runnable APK or a clearly identified source-only result, a short capture, a status/acceptance note and a small committed change set. Keep cultural, desktop, simulator and actual-device evidence separate.
- Start reviewer/access coordination in Sprint 1 and keep it moving during repository work. If hardware is unavailable at Sprint 1's end, continue reversible story/asset work but keep the device gate open and defer content expansion. Do not spend all remaining time improving a desktop view.
- Sprint 2 cast regeneration depends on reviewer notes and asset rights. Unreviewed placeholders can support code work, but are not accepted release art. If art exceeds its timebox, prioritise readable characters/sheep, material coherence and the physical hand-off over decorative scenery.
- The active release should have one maintained story source with scene-specific presentation. Preserve the earlier baseline as a comparison record. Keep old clip keys and explicit save compatibility; do not merge stores or invalidate kept visits merely to remove a prototype folder.
- Check actual headset performance in Sprints 1–3 as assets change. Sprint 4 is sustained and complete-path acceptance, not the first headset run. Target the selected device refresh rate; investigate stutters, sustained thermal behavior and cutout overdraw. The [judging guidance](https://start-developer-competition-26.devpost.com/details/judging) requires performant device use and names a 60 fps minimum; editor fps is not evidence of it.
- Functional acceptance includes one hand, no input, microphone denial/no controller, questions and resume, pause/headset removal, recenter, offline use, save/relaunch/replay/forget and controlled save-failure testing. Preserve current caption and reduced-motion options. Scope-specific provider tests apply only if a provider actually ships.
- The final demonstration leads with the fire, physical hand-off, observable response and remembered return. Explain web-to-VR lineage and actual additions. Use the required Competition channel and a video under three minutes; final entry/access/freeze requirements are in the [rules](https://start-developer-competition-26.devpost.com/rules). Record candidate hash/version and maintain the submitted experience through judging.

### Expansion and overrun policy

**1899 is the first expansion to consider, not a prerequisite to finish Sprint 3.** Its story spine is the unfinished promise remembered two generations later. Budget an additional **12–20 focused hours** for writing/review, art, narration, integration and checks; this is a provisional estimate to replace with measured pipeline effort. Promote it only after the first chapter and coda pass review/device gates and those extra hours exist beyond the test/submission reserve. The smaller package can still demonstrate replay and a meaningful return visit.

If capacity is 6–10 hours/week, the full proposed list does not fit: retain the first encounter and the already functional coda, limit new art to defects and essential coherence, and seek more access/time rather than claiming the same acceptance for less work. If capacity is 20+ hours/week, complete the core gates sooner before promoting 1899; do not automatically add a live service.

If a sprint slips, cut 1899 and decorative work first. Then explicitly renegotiate breadth or the release profile; never trade away hands-only completion, honest cultural framing, necessary rights, device validation or judge access. Unresolved critical story, device or rights findings mean the candidate is not ready, regardless of a calendar date.

---

## September 30 baseline plan (historical)

The following sections preserve the original broader estimate and milestone proposal. They predate the illustrated implementation and are superseded for current scheduling by the sprint proposal above. Requirements they describe are not automatically accepted or erased.

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

October 10, 2026: the full Enkare Nairobi slice runs in software with 85 pre-rendered narration lines (Gemini text-to-speech, generated October 4), field recordings and generated scenes; .NET, EditMode and PlayMode tests pass, and desktop captures are reviewed each polish round. No headset, simulator-with-hands or participant test has been performed, and no Competition release channel exists. The application logs its own frame timings ("[Sankofa] perf") for the first device run.

October 11, 2026: the user authorised an illustrated-theatre MVP to test a less costly presentation direction. A separate Unity scene keeps the hearth, stationary transitions, existing hand input and keepsakes, but uses two painted characters with three reusable poses, three scenic depth layers and a physical gourd that crosses from a painted palm into reachable space. Accepting it changes the illustration; the next trade beat reuses the same vocabulary. This is a presentation experiment for one bundled chapter and its existing continuation, not completion of personalised generation or live conversation. Draft generated art and software captures require cultural and actual Quest evaluation before committing to a full conversion.


October 11, 2026, story pass: the illustrated package v0.2.0 revises the first encounter and its contemporary continuation with character motives, an earned agreement, outcome-aware address and recall, and a passive gate path that retains the bead. Separate revised recordings preserve older replay audio. The story/core, editor, focused playthrough and offline narration-pipeline checks pass; see the [decision record](../decisions.md#october-11-2026--illustrated-story-and-response-pass). The [current script](../content/enkare-nairobi/stories.md#current-illustrated-episode-what-we-carry) is ready for family/community and unfamiliar-reader review. Software validation does not close those gates or establish headset performance. No new APK was built for this editorial pass.

October 11, 2026, voice pass: v0.3.0 restores the fire's conversational invitations, sensory approach, spoken Maa place-name and documented river-restoration context while retaining the v0.2 agreement and response logic. Twenty new recordings bring coverage to 75 playable lines. The future 1899 thread is documented but not converted. A capture at the promise confirms the ewe exists but does not read clearly against the props; that remains an art acceptance item. See the [voice-pass decision](../decisions.md#october-11-2026--illustrated-voice-pass).
