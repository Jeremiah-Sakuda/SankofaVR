# Sankofa illustrated theatre product scope

October 13, 2026 · Detailed implementation proposal requested by the user · Baseline inspected: `6c3b356`

Sankofa invites you into a painting, makes room for you as a guest, and reveals a story through what you choose to follow. The fire tells one complete encounter through two authored threads. Your hands affect the performance and how its characters include you; the women's agreement remains their own. A kept visit opens the other perspective and a contemporary continuation.

This is the current planning reference for the next iteration. It supersedes the feature priorities and estimates in [the earlier first-place proposal](first-place-plan.md). It scopes work, not completed behavior or an instruction to publish. The [PRD](prd.md) remains the broader product contract; its generated-story, freeform-input and additional-language requirements remain open. The [delivery plan](delivery-plan.md#current-sprint-plan) and [backlog](../backlog.md#current-illustrated-theatre-sprints) point here for the current implementation sequence. Runtime content remains in the existing package until a separately tested content revision lands.

## Product promise and boundaries

**Visitor-facing working description:** “Sit with the fire. Step inside a painted story. Discover what its people carry.” Keep the established “a story that notices what you notice” as a design test: noticing must produce readable acknowledgement, while deliberate selection determines questions and story threads. It must not imply eye tracking or unrestricted conversation.

The release candidate contains one Kenyan setting, two fictional women, two interpretations of their meeting, one shared agreement, and an illustrated modern river coda. English narration and captions; Maa names and Kenyan context require appropriate review. Sankofa's Akan origin is named separately. Neither the title nor the inherited griot voice implies that these distinct traditions are interchangeable.

| Protect in the candidate | Conditional after the core passes | Outside this iteration |
|---|---|---|
| Warm fire; stationary painted entry; two meaningful threads; tangible flour hand-off; visible hospitality outcome; explicit object questions; passive and one-hand paths; truthful kept visits; coherent modern coda | Feeding the fire with a stick; animal approaching a hand; peripheral scenery wings; richer optional echoes; measured adaptive quality control | Live AI conversation; freeform generation; new historical eras; second narration language; MR room mapping; multiplayer; character rigs; universal touch effects; AI-generated music |

Authored personalisation means selecting different scenes, revelations and their order from reviewed content. It is not generated family history. No ancestry is inferred or requested. The experience must remain enjoyable without knowing the cultural background or using a controller.

## Current foundation and missing work

The inspected build already has the fire, stationary transitions, a three-layer illustrated encounter, two cast columns with three poses each, tangible flour and bead, authored questions, accepted/passive outcomes, a local kept-visit store, replay and a modern continuation. Recent work added visible sheep, painted props, palm alignment during turns/recentering, acknowledgement, better hand-off timing and input grace. The latest code is later than the last documented review APK; rebuild before distributing it.

Missing from this scope: selecting a meaningful thread, the two vignette beats, direct questions on scene objects, a milk hospitality outcome, painted hearth/coda continuity, the new entry treatment, and the revised physical keep flow. Hardware, cultural review, participant acceptance and release access remain unverified. Existing procedural sheep is sufficient for the first probe; replacing it is an art decision after visual review, not a prerequisite.

The implementation should extend the existing engine, package and scene builder. Keep the baseline `Hearth` scene as a comparison record; do not build another application or duplicate the visit store for each route.

## Visitor journey

Timing below is a design budget to verify against recorded audio, not measured runtime. A first visit should take about 7–9 minutes without optional questions; the complete passive route should remain within the ten-minute target. The alternate thread takes about 6–8 minutes with onboarding skipped. The modern continuation takes about 3–4 minutes. Questions may extend a visit; pause and leave remain available throughout.

| Time from Begin | Visitor experience | What the application does |
|---|---|---|
| 0:00–0:20 | “Come. Sit with me.” An open palm catches the warmth and a few embers | Immediate local response, no saved choice, no required gesture; caption/recenter/pause controls available |
| 0:20–0:50 | Two small painted motifs rest on the hearth stone: a gourd and a bead. The visitor chooses what to follow | Deliberate selection chooses a route. A nearby named control provides the same choice. After an announced quiet wait, the fire begins the gourd thread without claiming it was chosen |
| 0:50–1:10 | Brief reconstruction disclosure, then the ember paints a riverbank around the seated visitor | Existing ember hand-off retained; far, middle and near layers reveal in sequence; no camera motion |
| 1:10–2:20 | The cool water, the two paths, and Wambui's arrival | Names and needs become clear. Water touch is optional. Gourd thread receives its first specific revelation here |
| 2:20–4:40 | Receive flour, hear the admission, set it between the women, hear Naserian's memory | Thread-specific beats surround a shared dramatic spine. Questions about the agreement remain unavailable until revealed |
| 4:40–5:40 | The agreement is made. Naserian makes room for the visitor to stay | Helping changes how the milk is offered; watching receives an equally warm, differently staged welcome |
| 5:40–6:40 | The bead is offered; the women depart | Accepted and passive placements remain truthful; no forced collection task |
| 6:40–8:30 | Return to the same painted hearth, one concise recollection, a choice to keep | An explicit save action persists the visit. The unplayed thread and modern river are offered when available; leaving never saves implicitly |

Do not add these durations to every existing line. Edit repeated exposition and idle waits to make room. Preserve the pause before the agreement. The opening needs one successful interaction grammar, not a series of tutorials. The optional stick-feeding ritual is deferred until the opening fits this budget and novices understand it.

### Opening selection without false ownership

The hearth gourd and bead are small illuminated motifs or miniatures of the story's objects. They are not the actual gourd or the keepsake later offered by a character. The fire can say, as draft copy: “The gourd follows what Wambui brings. The bead follows what Naserian remembers.” This is one brief explanation, with readable labels, not a repeated spoken menu.

Receiving/touching a highlighted motif or selecting its nearby control commits that thread. A highlight or glance alone does not. Freeze the chosen route before the disclosure and ember transition. A quiet visitor hears “Let us begin with what Wambui brings”; the default is recorded as automatic, never as their expressed interest. Proposed choice wait: 10 seconds after the explanation; pause, settings and tracking loss suspend it. Actual timing follows the first listening and device tests.

## Two threads with one agreement

All new dialogue and vignettes below are fictional writing directions, not verified historical events. They require editorial and cultural review before final audio/art production. Existing sourced context remains claim-linked. No new fixed exchange ratio, sacred bead meaning or general rule of communal obligation is introduced.

| Element | Gourd thread — What she brings | Bead thread — What she remembers |
|---|---|---|
| Central question | Why did Wambui come knowing the load was short? | Why does Naserian choose to wait? |
| New information | Wambui deliberately reserved food for home and came to admit the shortfall | Naserian recalls a previous meeting when she was the one who needed time and Wambui stayed |
| Distinct scene | A small illustrated preparation tableau: one portion stays, one goes into the travelling vessel | A short recollection at the same water, with the women in their earlier positions and Wambui waiting |
| Placement | After Wambui's arrival, before her admission | Immediately after “Last time, you waited for me,” before the sheep agreement |
| Visitor action | Inspect the offered gourd; the open vessel frames the tableau nearby | Receive/inspect a temporary memory motif; the recollection unfolds beside the live stage |
| Passive staging | Tableau appears beside the resting gourd | Recollection appears beside the visitor, without claiming they summoned it |
| Understanding changed | The shortfall is a difficult decision, not carelessness | Waiting is remembered reciprocity, not a stranger solving the dispute |
| Closing emphasis | Coming to speak despite having less | Making time for someone who once made time for you |

**Each thread must add a scene, not merely another description.** The current script already states that Wambui kept food at home and that Naserian remembers waiting. Replace those early summaries with the corresponding visual revelation on that route, while retaining enough shared dialogue for either visit to stand alone. The existing optional friendship/trust answers must not spoil the unplayed vignette.

### Vignette production limit

Each vignette gets 25–40 seconds, at most two scenic layers, and one small inset or tableau within the comfortable forward view. The main stage remains dimly visible so the visitor understands where they are. A brief narrated time cue distinguishes remembered time from the present encounter. Reduced motion uses a simple fade.

Gourd tableau: objects, a hand detail or silhouette, and portions of flour; no new family characters, interiors to explore or simulated pouring. Draft intent: “Before she left, Wambui divided what remained. Some for the journey. Some to leave at home.” Review the wording and material depiction before recording.

Bead tableau: reuse the two women and the water. Change placement and one reviewed pose per woman; a simple sequence establishes Naserian arriving short and Wambui making room to wait. Draft intent: “Another meeting, at this same water. That time, it was Naserian who needed longer.” Do not specify an invented custom or imply that the bead itself proves the event happened.

The visitor does not decide how much food a household keeps, approve the women's bargain, or travel physically into the recollection. This keeps the authored moral agency with the characters while giving the visitor a meaningful way to encounter it.

### Shared dramatic spine

1. Establish the place and the two people without requiring prior knowledge.
2. Wambui arrives and asks for help holding the flour.
3. Naserian names her own need; Wambui admits the shortfall.
4. The flour crosses back into the illustration, carried by the visitor or set down by Wambui.
5. “Last time, you waited for me.” The bead-thread recollection fits here.
6. “Take the sheep. Bring the rest when you can.” The sheep must be plainly visible.
7. Hospitality makes the visitor's actual participation visible.
8. The bead and departure close this meeting without pretending the remaining flour has already arrived.

Keep the existing closing observation: “Wambui arrived worried about what she owed. Naserian remembered what they had already given each other.” Both routes earn it in different ways.

## Visible consequence through hospitality

The milk beat happens after the women make their agreement. It never causes that agreement.

| Earlier event | Performance | Optional response | What may be remembered |
|---|---|---|---|
| Visitor helped place flour | Naserian turns toward the visitor and offers the milk gourd directly | Receive it, or let her set it nearby | “After you helped set down the flour, she offered you the milk” |
| Wambui placed flour on the passive path | Naserian turns the milk toward the visitor and sets it in an equally reachable shared place | Receive it from the resting place, or leave it there | “She set the milk beside the fire and made room for you” |

Warmth, duration and access to the milk are comparable. The difference is delivery and address, not a reward tier. Either route can then accept or leave the milk. Receiving flour alone must not be mistaken for completing the later placement.

Implement one milk invitation with outcome-specific staging where possible, rather than two independent interaction systems. No drinking mechanic, mouth tracking, liquid simulation or health/cultural claim. A supported single-hand selection counts as the same deliberate participation. The visitor does not need to hold two objects at once; flour ownership ends before milk is offered, and milk rests before the bead arrives.

The prototype must show the two arrangements side by side using temporary materials. If unfamiliar viewers cannot see a meaningful difference, revise choreography and line timing before producing more art.

## Interaction and narrator contract

| Signal or action | Response | State boundary |
|---|---|---|
| Look toward a person | Gentle pose/light acknowledgement; settle rather than continuously swivel | Head direction nominates a target only; no new speech, branch, consent or save |
| Point at a question-bearing object | Nearby prompt reveals the specific available question | No answer until explicitly selected; unavailable or spoiler questions have no active prompt |
| Pinch the visible question prompt | Fire answers the recorded question, with a small light cue to the object | Routes through the existing question authority; never through receive or offer logic |
| Open palm at an offered object | Object crosses to the hand or its accessible equivalent | Only the active invitation can accept it |
| Place an owned object at a marked destination | The intended offer/placement resolves once | Requires current ownership and legal destination; unrelated contact does nothing |
| Touch fire/reeds or tilt a held prop | Small local sensory feedback when enabled | No journalled moral choice or story consequence |
| Place keepsake in the clearly labelled keep location | Attempt explicit local save | Success is shown only after persistence succeeds |

**Asking and taking:** prototype an object-linked question chip, not “pinch anywhere on the object.” The ray selects the labelled chip; nearby open-palm receiving remains a separate affordance. While an invitation is active, its essential control takes priority. An available question may suspend the invitation with its remaining time, then restore it. Never let one pinch both ask and accept. Keep the existing reachable question panel as the fallback and controller equivalent.

At most one question is active; one explicit request is queued only if the current boundary is safe, otherwise show a brief local holding response. Duplicate selections do not stack. Stop/pause/recenter/scene change invalidate stale presentation cues. Resume at an authored audio boundary without replaying completed actions; do not claim sample-exact interruption recovery unless implemented and tested.

The fire may lean subtly toward an intentional question target or acknowledge stable attention, with restrained motion. It must not duck narration merely because the head moved, speak unrequested asides, or follow rapid head movement twitchily. During the agreement it remains still enough to leave the women the focus.

### Three physical pleasures

Core production is limited to: responsive palm/fire embers; a restrained flour movement sound when a held gourd tilts; a clear light and sound response to placing the keepsake. Use licensed recordings or authored effects with provenance, no AI-generated music. Do not suggest physical weight or haptics the device does not deliver. The tilt sound is a material cue, not a reliable measurement of how much flour is inside.

Painting ripples, universal touch behavior, animal approach and decorative echo interaction are deferred. Keep the existing water touch because it belongs to the scene and teaches a simple action.

## The fire paints a coherent world

Entry is authored choreography, independent of network or runtime image generation:

1. The ember and fire share a brief brightening.
2. A small number of sparks travel toward the far layer; a painted reveal mask exposes the horizon.
3. The middle bank appears, then near reeds and cast. Sound broadens from the hearth to water and distant environment.
4. The final near object resolves in full depth and the first story line begins.

Target 6–10 seconds within the journey budget; test rather than assume spectacle needs more time. Never translate, rotate or zoom the camera. Maintain a stable ground reference and avoid a full-view white flash. Hands or a clear temporary interaction state remain visible. Essential story information stays forward; side wings are decorative and conditional. Reduced motion uses a 2–3 second dissolve with the same event completion semantics.

Use the same material family for the opening hearth, story, vignettes, modern river and return. The current solid sheep can remain if its silhouette and style pass review; budget at most a simple replacement with standing/head-turn poses. Do not generate high-resolution cast blindly: first inspect one final-scale face and palm on the target device, then decide resolution and compression.

### Asset and narration budget

| Deliverable | Maximum planned production | Dependency |
|---|---|---|
| Hearth surround | Two illustrated layers; reuse existing fire/log geometry with compatible materials | Provider/rights record and first lighting sample |
| Historical cast | Existing two figures, corrected dress/expression; up to two added poses each, for notice and milk staging | Cultural feedback and stable hand anchors |
| Thread vignettes | Two small layer sets; object detail plus at most two extra cast cells reused where possible | Story comprehension before art |
| Modern river | Two or three coherent layers around retained gate/bead interaction | Preserve documented restoration context and imagined garden distinction |
| Sheep | Existing solid animal initially; one simple art replacement only if needed | Visual comparison, no new locomotion project |
| Narration | Budget 30–40 changed/new short clips across opening, two vignettes, hospitality, return and questions | Locked text, source checks, listening review |
| Effects | One reveal treatment, restrained sparks, three local feedback sounds | Worst-case device cost and sound balance |

The narration count is a production ceiling to track, not a guarantee. Rough allocation: opening/thread 5–7; gourd vignette 4–5; bead vignette 5–7; hospitality/outcomes 6–8; closing/return 5–7; source/question corrections 3–6. Total 28–40. Reuse unchanged recordings. Changed words receive new versioned audio keys; retain older assets required by kept plans. English captions follow every released line and sound mix is checked at quiet and louder headset volume.

Keep asset-generation attempts bounded to two review rounds per set before simplifying the brief. Record provider, actual model if recoverable, dated terms, prompts, source images, output files and alterations. Never fill a missing model name by guessing. Approve style on one face, one palm and one prop before producing sheets.

## Keeping and returning

The visit ends completely before an invitation to return. No cliffhanger about whether Wambui later paid unless a real authored scene answering it ships. Proposed hook after the gourd route: “You have seen what she brought. Another time, we can sit with what Naserian remembered.” After the bead route, invite the visitor to see what Wambui left at home. These are drafts; avoid wording that falsely claims a default thread was their selection.

The modern river is a separate continuation, not the payoff for an unfulfilled historical promise. Its pleasure is recognizing the same water in today's city, seeing documented restoration reflected in an explicitly imagined garden, and deciding what to carry onward. It is available immediately after a kept visit; no artificial day lock or streak. The other historical thread is also available without forcing a save, but personal recall requires one.

| Returning option | Behavior |
|---|---|
| Replay this visit | Play the retained plan and actual outcomes with original compatible assets/audio; no new branch or inferred history |
| Follow the other thread | Start a new authored performance, skipping first-use tutorial; retain no invented choices from the prior route |
| Visit the water today | New child visit with truthful parent recall and optional bead placement |
| Forget | Remove the kept visit and linked persisted memory according to explicit deletion semantics |

The shelf should present one primary next invitation and nearby alternatives; it should not become an achievement grid. A prototype must test whether people understand “replay” versus “another part of the story.” The alternate route initially reuses the full shared encounter; if repeat exposition feels laborious, shorten transitions and already-known setup through authored return variants. A separate abridged episode is outside the initial budget.

**Physical Keep:** use a dedicated hearth recess with a clear “Keep this visit” label and one warm invitation. The historical character's bead gift never constitutes save consent. Require deliberate release/selection in the recess; a resting bead, incidental contact, automatic placement or tracking loss cannot save. The hand panel remains equivalent. On save failure, retain the visit in memory and show retry; do not light a permanent shelf object or say it was saved. Forget and local retention remain discoverable. Declining to receive the story bead does not prevent explicit saving later.

The present-day gate placement and its recall need a saved continuation outcome, not only the initial encounter record. If the visitor leaves without keeping that continuation, future visits cannot claim the bead is still on the gate. No real-world location anchor or room data is required.

## Cultural and factual integrity

Separate three questions in review: Is the documented context supported? Is the fictional encounter plausible and respectful? Do the voice and images feel appropriate? One family review is valuable and specific; it is not certification for all members of either community.

Preserve the pre-entry reconstruction disclosure, spoken Enkare Nairobi with Maa delivery reviewed, specific Kikuyu/Maasai identities, and separate Akan naming context. Review household food allocation, the milk invitation, dress, bead, sheep and invented recollection together. If a reviewer challenges a prop or gesture, revise its staging and language before regenerating all related art.

Provide “About this telling” from the hearth and questions panel: concise distinctions between sourced place/material context and invented characters, actions and dialogue. No whole-object gold truth glow. Exact sources stay linked at claim level in content/review records; public source presentation and identifiable-name handling must follow the final entry review. The narrator does not read the bibliography mid-scene.

Nothing collected for review authorises publishing a reviewer name, private family account or recording. The user controls external outreach. The production tools can prepare packets and incorporate supplied notes without waiting for permission to draft.

## Implementation modules and data

Follow [the modular architecture](../architecture.md). Proposed identifiers below describe intent; introduce only fields/contracts needed by the implementation, after checking current readers, validators and migration requirements.

| Owner | Existing foundation | Change required |
|---|---|---|
| Story core | `StoryModel`, `PlanComposer`, `StoryRun`, package/plan validators | Two stable route IDs, shared beats and distinct vignette order; legal hospitality conditions; no route logic in shaders |
| Application flow | `VisitFlow`, `FlowCues` | Thread selection/default, question suspension, owned-object transitions, correct follow-up choices and explicit save outcome |
| Input | `PointerSelect`, palm/touch gestures, `PointerSource`, `HandSource` | Object question chips, action arbitration, one-hand alternatives, tracking-loss safety |
| Stage | `IllustratedTheatre`, `IllustratedFigure`, `StoryTransition` | Vignette layout, notice/milk poses, stationary reveals, matching reduced motion and recenter |
| Narration | `NarrationPlayer`, current generation/manifest pipeline | Versioned clips, one audio owner, safe resume and question spoiler gating |
| Persistence | `SavedVisit`, `FileVisitStore` | Preserve selected/default route origin if recalled; continuation compatibility for both routes; save continuation outcomes; legacy replay/deletion behavior |
| Authoring/build | Theatre layout and editor builders | Scene-keyed presentation layouts in one active package; source-only checks; generate scenes/materials through editor |

**Known integration constraint:** `PlanComposer` currently allows a continuation to follow one exact parent route, and `SavedVisit.Next` is a single next route. Two historical threads plus a modern continuation cannot be wired by changing menu labels alone. Prefer a small validated route-eligibility table that accepts the two historical parents for the coda, while deriving the alternate thread from package metadata. Preserve the legacy single-parent reading path. Do not silently accept arbitrary parents or introduce a general branching framework.

Share scene beats by stable IDs. Suggested authored routes: `water.brought`, `water.remembered`, `water.today`; existing saved route IDs keep their original meanings. Choose exact IDs during implementation, before recording their associated clips. The layout may select presentations per scene; it must not duplicate application or storage behavior.

If adding fields to strict saved-plan readers, version the format and keep an explicit legacy reader/migration fixture. Existing saved visits have no thread-selection provenance: treat it as unknown, never fabricate a choice. Keep old embedded plans/audio replayable or show a specific missing-content recovery. The present replay already omits question/interruption timing; do not market full conversational replay or quietly extend that promise here.

Journal accepted outcomes once. Keep transient attention and physical hand samples out of persistent personal records; existing limited semantic attention recall must be accurately described. New thread choice and continuation records persist only through explicit keep or an explicitly disclosed resume setting.

Pause/suspend freezes story time, input acceptance, offers and audio together. Resume validates tracked hands before allowing new actions. A last-known hand pose may maintain visual continuity briefly; it must not manufacture a fresh receive/drop/save event. Recenter keeps stage-relative anchors and question targets aligned. Resume after relaunch requires a separately verified stable checkpoint policy; if this is absent, expose restart/replay honestly rather than claim recovery.

## Hardware and performance plan

Quest access is an open dependency. Seek a borrowed device, collaborator or external tester as early as possible; no purchase or outreach is performed by this scope. First test the current build before adding all effects. Target initial Quest 3 validation, with final supported-device claims limited to evidence and the release decision.

Run three hardware rounds: baseline reach/depth/input; complete new threads and worst-case transition; warm release candidate with independent installation. A remote tester should provide device/model, build hash, full hands-only recording, performance log and structured observations. Such evidence cannot substitute for every comfort review, but it is actual device evidence.

Measure CPU/GPU frame timing, dropped frames, memory, load/transition stalls and warm-session behavior. Test fire plus hands/captions, maximum card overlap, the vignette, question while holding, and return. Record chosen refresh rate and matching frame budget; for example, 72 Hz gives about 13.9 ms. The competition's stated 60 fps floor does not establish our device target. See the [official judging guidance](https://start-developer-competition-26.devpost.com/details/judging).

Start with conservative static settings. Profile before choosing silhouette trimming, alpha-to-coverage or MSAA changes. Check transparent edges in both eyes and at actual scale. An adaptive tier may later reduce decorative particles or other measured costs, with conservative thresholds, hysteresis and logs. Do not switch repeatedly during the emotional reveal or reduce readable captions/essential cues. Measuring the first ten hearth seconds is not a full-scene profile, and a governor cannot close a hardware gate.

If hardware remains unavailable, software/art work can proceed with bounded investment, but technical and comfort gates stay open. Do not convert unavailable evidence into a pass or use simulator novice sessions to claim hands/depth acceptance.

## User stories and acceptance

These targets are formative product gates, not probabilities of winning. Use observed failures to change the design, not to coach participants to the expected answer.

| ID | User story | Observable acceptance |
|---|---|---|
| IT-01 | As a first visitor, I know I am welcome and can begin without learning controls | First local hand response within 20 s; useful thread action within 60 s; silent route still completes; no forced speech |
| IT-02 | I can follow a question that interests me | Each thread includes its own visible vignette and different revelation/order before the ending; either route alone makes the bargain understandable |
| IT-03 | I can ask about what is in front of me | A labelled object question invokes exactly one correct answer; no accidental receive; unavailable reveals are not spoiled; narration resumes coherently |
| IT-04 | Helping changes how I am included | Visitor can point to the changed milk staging; the same agreement and hospitality remain on passive paths |
| IT-05 | I can simply watch | No timeout claims I acted, chose an interest or refused friendship; objects remain readable and the ending complete |
| IT-06 | I feel the story unfold around me comfortably | No camera movement, stable orientation, readable captions/hands, reduced-motion equivalent; on-device comfort findings recorded |
| IT-07 | I understand what is imagined | After a visit, testers distinguish fictional women/bargain from sourced regional context without implying all rendered details are documented |
| IT-08 | I can keep a visit and return to something worthwhile | Relaunch retains only explicitly saved records; alternate thread reveals another scene; coda recalls correct parent and saved placement |
| IT-09 | I can pause, recover and use one hand | Both routes and coda complete hands-only with either hand; tracking loss, recenter, focus loss and failed save cause no false action |
| IT-10 | I trust the submission's promises | Candidate build, guide, footage, coverage and source/rights records agree; independent install and device evidence recorded |

Prototype evaluation: three unfamiliar readers/viewers see temporary-art paired paths. At least two can explain the conflict, what participation changes, and one thing that differs between threads. This is a cheap iteration gate, not final validation.

Final formative evaluation: five first-time visitors, counterbalanced initial thread where practical. Target at least four completing unassisted and explaining a visible consequence. Ask “What happened?”, “What did you change?”, “What was imagined?”, and “What would you return for?” before prompting about features. At least three try the second thread in a later session and identify new information without being told it. Record device versus desktop conditions and individual failures; this small sample supports design decisions, not broad statistical claims.

Automated coverage: both thread defaults/selections; accepted and default outcomes including receiving flour without placing it; milk acceptance/default under both earlier outcomes; pause/question/recenter during offers; obsolete question callbacks; save fail/retry; old/new replay; both parent routes reaching the coda; deletion; all captions and audio references. Enumerate legal outcome combinations in core tests and use representative risk paths for Unity/device visits. Target ten complete hardware visits across the release paths, including passive and one-hand runs; do not confuse that with ten distinct participants.

## Work sequence and effort

Assume 12–15 focused user hours/week until confirmed. Planning separates human acceptance/production effort from assisted engineering effort. Engineering estimates include implementation, integration, meaningful tests and fixes; they are not “unattended agent cycles” and not promises about elapsed time. Some user and engineering time overlaps during review; do not add both columns as a single personal workload.

| Sprint | Dates | Deliverable | User hours | Engineering effort | Exit gate |
|---|---|---|---:|---:|---|
| 1 Prove the behavior | Oct 13–19 | Paired greybox milk outcomes; direct question; rough painted entry; two vignette scripts/storyboards; baseline APK/tester and review packets | 9–11 | 12–18 h | Three-person comprehension probe, documented implementation estimate, hardware access path or explicit unresolved blocker |
| 2 Build the complete telling | Oct 20–26 | Thread selection/composition, two vignettes, reviewed narration, hospitality, matched hearth/cast; device round two begins | 12–15 | 20–30 h | Both historical routes complete with meaningful differences; no ownership/input ambiguity; review corrections staged |
| 3 Make the return complete | Oct 27–Nov 2 | Painted modern coda, alternate-route offer, physical Keep, saved compatibility/recall; coherent entry/return art and sound | 10–13 | 18–26 h | Save/relaunch/replay/alternate/coda/forget all work; ending matches opening quality; content freeze for new features |
| 4 Verify and simplify | Nov 3–9 | Novice/device sessions, profile-driven optimization, accessibility/recovery, final cultural/audio fixes | 12–14 | 18–26 h | Formative targets assessed, ten device visits, major faults closed, rights and release access ready |
| 5 Present and release | Nov 10–16 | Frozen candidate, independent install, demonstration, adaptation copy and completed submission package | 9–12 | 8–12 h | Guide matches build, user accepts release materials, submission completed when authorised |

Totals: **52–65 user hours plus 10 hours contingency = 62–75 hours**; **76–112 hours of engineering effort**, substantially assisted but requiring integration/review. At exactly 12 user hours/week the low estimate already exceeds a 60-hour envelope by two hours; remove optional work or add capacity rather than pretend it fits. Re-estimate after Sprint 1. Current dates reserve November 17–18 for access/submission problems, not planned feature work. Internal target November 16; official deadline November 18 at noon Pacific / 3 p.m. Eastern, checked October 13. [Official rules](https://start-developer-competition-26.devpost.com/rules)

The estimate includes core art/narration review, testing and submission production. Reviewer availability, device shipping/access and approval lead time are external dependencies. Paid asset/tool/API spending has no approved numerical budget; prototype with current assets first, then record cost and terms before paid production. Art generation and editing can be assisted; only the user/reviewers can provide their acceptance, cultural feedback, account decisions and external access.

### Work packages and dependency order

| Ticket | Implementation boundary | Depends on | Done when |
|---|---|---|---|
| IT-P01 | Greybox proof: reveal, object ask and milk consequence | Existing build | Paired captures and comprehension notes identify whether each behavior is understandable |
| IT-P02 | Draft two vignette scripts, claim map and review packet | Current source/story | Neither route spoils the other; complete passive story; specific reviewer questions prepared |
| IT-P03 | Hardware baseline and release identity preparation | Current APK, human access | Findings tied to hash/device; signing/app-id choices recorded securely; no credentials committed |
| IT-P04 | Route metadata, thread selection and continuation eligibility | P01, P02 | Both route plans validate; default origin truthful; one package; legacy fixtures pass |
| IT-P05 | Object questions, pause/resume and input arbitration | P01 | No double action, no spoiler or stale answer, one-hand alternatives pass |
| IT-P06 | Hospitality ownership, staging and recorded lines | P02, P04 | Four milk combinations and mixed flour outcomes pass; guest treatment remains warm |
| IT-P07 | Vignette art/layout/audio and cast responses | P02, P04, first asset review | Two distinct scenes in complete runs; no new rigs/environments needed |
| IT-P08 | Unified hearth/coda, reveal and local sound | P01, art/rights sample | Coherent first-to-last frames; reduced motion; worst-case hardware profile |
| IT-P09 | Explicit physical Keep, alternate thread and coda memory | P04, P06 | Relaunch, failed save, legacy replay, both parent routes and deletion pass |
| IT-P10 | Device/user/content acceptance and optimization | P05–P09 | Recorded failures fixed or scope reduced; no substituted evidence |
| IT-P11 | Build, access, adaptation and demonstration | P03, P10 | Frozen artifact installed independently; materials faithfully show candidate |

P02 and preparation for P03 can proceed while P01 is built. Final art waits for the story/interaction decision. Production narration follows approved text. Critical path: P01/P02 → P04/P06/P07 → P09 → P10 → P11. Hardware and cultural review run alongside it and can block release acceptance independently. This describes work dependencies, not authorisation to spawn agents or message reviewers.

### Cut order and stop conditions

First remove stick feeding, sheep approach, peripheral wings, decorative echo interactions and automatic quality tiers. Next simplify vignette art, reveal particles and extra cast poses while preserving the distinct scenes and clear acknowledgement. Retain a simpler coherent modern painting rather than a mismatched 3D coda. Cut redundant narration instead of the silence that makes the agreement land.

If the two-thread structure still does not fit after the first probe, explicitly choose a one-thread release and narrow the claim; do not ship two cosmetic choices advertised as meaningful personalisation. If the milk difference fails comprehension, revise the gesture or choose another small hospitality event before adding more branches. If hardware finds reach or frame problems, simplify effects before adding content. If cultural review disputes the premise, revise the story before commissioning its final art.

Never spend the release/testing reserve to add an untested system. No numerical judge score or winning probability is used to justify scope.

## Submission story and immediate next work

The adaptation case is specific: web Sankofa's illustrated, narrated storytelling becomes a seated spatial theatre with physical objects, authored perspective selection, contextual questions and remembered participation. Preserve the original project's provenance and assess any actual code/asset reuse separately. Recorded questions are not live conversation, and two authored threads are not open-ended generation.

Prepare a 2:30–2:45 demonstration: a real gourd crossing from paint into reach; the fire painting the scene; two glimpses of different revelations; one paired hospitality consequence; then the keepsake and coherent return. Show implemented runtime behavior and label simulator footage. The final video must not hide an unfinished in-app ending. Release access/signing, eligibility and rights remain independently verified before submission. Do not contact reviewers, publish or submit without the user's authorization for those external actions.

**Next implementation increment:** P01 and P02. Produce a rough 60–90-second diagnostic sequence using existing art, plus the two vignette scripts and a paired outcome capture. Reuse the current application and question pipeline, keep it behind an explicit prototype configuration, and review it before expanding final asset production. No new paid generation or complete chapter rebuild is necessary to answer that first product question.

## Gallery framing exploration

After this scope was drafted, the user proposed Sankofa as an art gallery whose paintings can be entered and experienced, with this encounter as its first artwork. This is a framing proposal under discussion, not an implemented gallery or an approved expansion to the estimates above.

Working promise: **“Step inside the painting. Discover the story within.”** The gallery makes the catalogue understandable; the fire supplies the warmth and voice inside each telling. The existing two-thread encounter becomes the first complete exhibit, provisionally titled *What We Carry*. The framed image should correspond to the actual encounter rather than advertise a different cinematic treatment.

The smallest experiment is one painting in a restrained gallery setting: notice a detail, deliberately select Enter, and see the flat image separate into its spatial layers around the stationary visitor. The familiar gourd then crosses from a painted hand into reach. On return, the work becomes a framed painting again; a nearby explicitly kept memento can lead to replay or the alternate perspective. No mandatory walking, camera zoom or artificial travel is needed to express stepping inside. Optional movement is not a condition of use.

This framing could replace the opening selection presentation rather than add another tutorial before the hearth. Preserve the fire's welcome, reconstruction disclosure and invitation grammar. Compare a framed painting at the hearth with a spare gallery foyer before deciding whether a separate room adds enough value to justify its art and transition costs. The existing fire-painted entry and the frame-to-depth transition should form one authored sequence, not two spectacles in succession.

Only one complete artwork is in the proposed first release. Avoid empty locked wings, a collection browser, avatar navigation, a marketplace or claims of a catalogue that does not exist. Artworks can become reusable content packages later; this iteration only needs one coherent entry, experience and return. The two authored threads remain the concrete meaning of adaptation, not unrestricted live-generated plot.

Presentation should identify the piece as contemporary authored work imagining a historical encounter, with its actual production and review status. It should not imply that the generated composition is a historical painting or that invented actions are documented events. Existing cultural and provenance review applies inside the gallery frame as it does at the hearth.

Test before revising the scope: does the frame make entry clearer; does seeing the same composition acquire depth strengthen the reveal; does returning to it make visitors notice a detail differently; and do they still understand the characters and their own influence? If it passes, update IT-P01, opening/return staging and asset estimates together. Keep the detailed story, hands, saved-visit and acceptance work above as the first exhibit's foundation.
