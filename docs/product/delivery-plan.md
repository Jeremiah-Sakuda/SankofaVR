# Sankofa — delivery and validation plan

Updated November 16, 2026 · Submission plan based on the November 15 audit; prior plans are preserved below

The [personal story table scope](personal-story-table-mvp.md) and [current PRD contract](prd.md#current-mvp-contract) guide delivery. The user authorised an illustrated personal telling shaped by inherited fragments/questions, a warm fire narrator, real-table presence where available, contextual follow-up and explicit save/reopen. This replaces the gallery as the primary journey. It does not delete its scenes, art, recordings or historical evidence.

## Submission plan — November 16–18

This is the current execution proposal, replacing the dated schedule below. It responds to the entrant's request for a submission-ready, competitive entry. It does not claim a guaranteed win, promote the river experiment, authorise publication, or mark planned checks complete. All times below are **Eastern**.

**Entrant clarification, November 16:** no Quest or remote headset tester is available; plan around the Meta XR Simulator. Use the [original Sankofa inspiration](https://devpost.com/software/sankofa-y47f9p) as the motivation source.

**Account and channel check, November 16:** the signed-in [Start member page](https://developers.meta.com/vr/start/member/) explicitly confirms active membership. Developer Dashboard access and the Sankofa app are verified; the app was Draft / Not released at inspection. After the entrant completed the setup, Meta displayed age-group self-certification complete, removed the unsigned-agreement warning and enabled channel creation. **Competition is now created and visible in the dashboard:** Private, Email invite only, zero users and no current build assigned. Automatic channel emails were disabled during creation. No APK was uploaded, users invited or public release made. Judge access still needs configuring and independent verification after the final build is ready; the private empty channel is not submission-ready delivery. App ownership/admin role, signing identity, uploaded-build inventory and overall competition eligibility remain separate, unverified checks.

### Starting point and proposed release choice

The [November 15 audit](../testing/2026-11-15-judge-audit.md) corrected five reproduced river-visit failures and checked cancellation, exact replay and figure grounding. Final focused results include 445 core, 81 Python, 49 EditMode and nine painted-experience checks. Earlier broader-run failures, corrections and opt-in skips remain in that record. Three [agent reviews](../reviews/2026-11-15-independent-judge-review.md) converged on 72/100; those estimates are neither official scores nor evidence of emotional impact.

There is a complete silent desktop recording and a structurally checked, debug-signed `Sankofa-RiverPainted-Review.apk` from modified source. Neither is the final submission artifact. The interactive simulator attempt stopped at a Windows Security prompt. No Quest, listening, participant or independent channel-install acceptance has been recorded.

**Recommendation: finish and assess `RiverThresholdPainted` as the lead candidate.** Keep the approved illustrated characters, solid nearby props and water story. This gives the cinematic opening a corresponding destination. StoryTable remains the release default until an explicit selection is recorded. At present [BuildRelease](../../unity/Assets/Sankofa/Editor/SankofaBuild.cs) hardcodes StoryTable: signing alone will not produce the river experience. A river selection requires an explicit signed build target and aligned PRD, candidate record, walkthrough and submission copy. Preserve comparison scenes. The table candidate remains a fallback to be rebuilt and checked, not an automatically accepted older APK.

The proposed visitor promise is: **choose a water pot, sit beside an imagined meeting from Nairobi's past, hear its other side, and keep the telling.** The emotional payoff belongs to the younger traveller eventually carrying a memory imperfectly herself. Preserve that final sentence and the pause after it. The narrator must not explain the payoff in the opening.

The proposed entry remains **Entertainment / Adapted or Significantly Updated**, subject to organiser classification. The adaptation account should pair the documented original web baseline with the actual new Unity experience: spatial staging, hands access, distinct perspectives and complete offline replay. Identify the baseline revision and dates; do not rely on the latest visual polish as the entire account of what changed.

The audience hypothesis remains people curious about Kenya's past who want an approachable encounter with everyday lives behind it. The original motivation is the distance between knowing a family's origins and experiencing the stories, voices and everyday lives around them. Carry that motivation forward without inventing a personal family incident or treating the original app's wider coverage as a Quest capability. Personal questions can steer the prepared thread; this is finite authored coverage, not a recovered family history or unlimited generation. The reason to return is another perspective or one of the other two tellings; a six-bead strand does not demonstrate long-term retention.

### External requirements and evidence boundaries

The [official rules](https://start-developer-competition-26.devpost.com/rules), rechecked November 16, close entry **November 18 at 3 p.m. Eastern / noon Pacific**. Confirm entrant eligibility, Start membership, Developer Access and funding restrictions. Deliver hands-only operation, an APK through a new **Competition** channel, public YouTube/Vimeo footage from Quest or an accepted simulator, and the required description, track/division, adaptation and launch information. Keep judge access available and freeze the submitted materials after the deadline. The entrant or authorised representative performs the final submission; section 3 restricts automated entries.

The [FAQ](https://start-developer-competition-26.devpost.com/details/faqs) does not require headset ownership. It does not convert simulation into hardware evidence. The [judging guidance](https://start-developer-competition-26.devpost.com/details/judging) weights innovation, experience, technical implementation and presentation equally, and calls for at least 60 fps on Quest in high-scoring entries. The thresholds and internal deadlines below are our delivery choices, not extra official rules.

### Work order, owners and exit evidence

Agent work estimates are provisional focused hours, including implementation and local rechecks; waiting for people, accounts or build queues is additional. Plan roughly **14–20 agent hours and 2–4 human hours distributed across the entrant, listener and desktop viewers**, with independent account, rights and review preparation happening alongside the quality pass. Re-estimate after the first two hours. Do not consume the submission buffer to finish optional polish.

| Priority and work | Owner / effort | Done when | If it does not close |
|---|---|---|---|
| **P0: establish the delivery route now** | Entrant: account confirmation and existing signing identity; agent: prepare exact requirements, about 1 hour | Active Start membership and dashboard access are verified; setup warnings are cleared and the empty Competition channel exists. Identify app owner/admin role, accepted package/signing identity, next version code, upload route and a second account for access testing. Secrets stay outside Git and chat. | Account eligibility, missing signing access or unusable delivery is escalated immediately; more art cannot resolve it. Do not invent an accepted version or create a replacement key blindly. |
| **P1: give the story the foreground** | Agent: Unity presentation, 3–4 hours maximum | Reading panel fits its content with less unused dark area; captions retain readable size and large-text pagination. Pause, Options and Leave stay discoverable. Context and perspective actions appear when relevant; both sides remain reachable with one hand. Compare the same mode, chapter, camera and text size before/after. Check the full transition, poses, pause and reduced motion. | Revert the layout change if readability, choices or seated reach worsen. Preserve the approved art; no new cast or environment pass. |
| **P1: make the sound as coherent as the pictures** | Agent: asset/mix preparation, 1–2 hours; authorised listener: 30–45 minutes | A listener checks the opening, both water perspectives, the full ending, all other accessible narration, pronunciation, caption sync, pause/resume and voice-versus-ambience balance in the candidate. The naming clip ends cleanly. Credits and actual shipped audio agree. | Remove distracting effects. Unresolved web music stays out of both APK and submission video; silence between deliberate sounds is preferable to an unverified track. If narration cannot be accepted, use an explicitly caption-only candidate and reassess the reduced presentation claim. Existing verification remains muted until a listening session is authorised. |
| **P1: test whether the story lands** | Entrant arranges 3–5 unfamiliar viewers; agent prepares neutral questions and synthesises anonymous findings, about 1 hour | Aim for five; record the actual sample and medium. Before explaining it, ask what the app offers, what changes after the second perspective, and what they would return for. Internal target: at least four of five understand the changed responsibility for the memory; none expects recovered ancestry. On interactive tests, first deliberate action within 60 seconds without coaching and a complete seated visit. | Revise misleading invitation, choice wording or pacing first. A desktop viewer can assess comprehension but cannot pass hand tracking or presence. No invented quotes, validation or cultural endorsement. |
| **P0: prove the complete candidate** | Agent: software/simulator, 2–3 hours | Same-candidate cold launch, water selection, both sides, questions, Pause/Options, full ending, Keep/reopen/Forget and exit. Exercise single simulated-hand input, large text, reduced motion and no service; distinguish any controller-emulation checks. Check theme overrides, cancellation and return-to-table. Record an actual XR Simulator visit, including its input mode. | Fix any essential-path failure before polish. Recheck the previously blocked simulator launch. Real hands/reach, room fit, sustained Quest frame rate and physical comfort remain unverified; simulator completion does not close them. No headset session is on the critical path. |
| **P0: close content and rights gaps** | Agent: exact asset/claim inventory; entrant and qualified reviewer: review, about 1–2 hours in parallel | Every shipped/public asset has a defensible provenance record; the provisional music is excluded. Review the final history/fiction boundary, griot framing, garments and pronunciation with an appropriate reviewer where available. Verify the applicable generated-art and narration terms. | Cut or replace an asset whose use cannot be supported. Keep cultural-review status accurate; a draft label does not clear third-party rights or establish cultural accuracy. No identifying third-party testimonial footage. |
| **P0: produce a reproducible upload candidate** | Agent: source/build/validation, 2–3 hours plus build time; entrant: signing access | Select one scene, review accumulated changes, preserve unrelated work and create a clean release source revision. Build with the correct scene, package, signing identity and version. Run the final core/backend/EditMode/PlayMode checks with optional cases explicitly accounted for, plus the complete visit. Verify APK signature, manifest, asset exclusions and hash. | No debug/dirty review artifact is relabelled as a release. Any later runtime fix requires a new artifact and affected rechecks. Frozen identity is recorded in [candidate builds](../release/candidate.md). |
| **P0: make every judge-facing surface agree** | Agent: copy/edit package, 2–3 hours; entrant: upload, final review and submission | One build, one set of screenshots, matching controls in the [walkthrough](testing-guide.md), reviewed XR Simulator footage, public video access and tested Competition access. A separate authorised account checks the link. Entrant confirms a realistic launch date and receives the submission confirmation. | Do not show a river video with a table APK or claim a hidden tabletop sweep in the river. Access verification is separate from Quest installation, which remains unverified. Missing required delivery items remain blockers. |

UI changes belong in `StoryTableView` and river presentation (SK-01/02/07/08/15); source-bounded story and application state stay in their existing modules. Audio work belongs in the existing narration and sound adapters (SK-05/06/11). Release selection and evidence belong in build tooling and release documents (SK-18). The visible improvement must be demonstrated in the same candidate, not inferred from a code change or another comparison scene.

### Dates and decisions

| When | Required outcome |
|---|---|
| **Monday Nov 16, tonight** | Start membership and dashboard access are verified; setup warnings are cleared and Competition is created. Finish ownership/signing checks. Use the original published motivation. Prepare simulator capture, desktop comprehension review and the rights inventory. Start the bounded panel/choice polish and a raw capture of the exact candidate. |
| **Tuesday Nov 17, noon — scene decision** | Review the complete simulator river visit, available listening/desktop-viewer findings and unresolved risks. Recommend promotion only with a coherent complete visit and no known essential-path failure; record the entrant's decision and the remaining device acceptance exceptions. If the river still cannot be demonstrated reliably, choose a freshly checked StoryTable build. Do not blend their footage or claims. |
| **Tuesday Nov 17, 4 p.m. — source freeze** | Finish only reproducible release-blocking fixes. Record selected scene and scope; complete source checks and build identity. Begin the signed build and final same-build capture. Earlier takes are rehearsal unless they still match the frozen runtime. |
| **Tuesday Nov 17, 8 p.m. — target submission** | Complete channel/access checks, final public video, copy, credits and entrant review. Entrant submits and verifies the received entry and both links. |
| **Wednesday Nov 18, noon — internal last check** | Reserved for upload/access or genuinely blocking corrections, with three hours before the official close. Recheck the exact public materials and record outstanding evidence honestly. If required access or eligibility still fails, report it plainly. |
| **Wednesday Nov 18, 3 p.m. onward** | Keep the submitted build, media and access stable; preserve receipts and availability through judging. Do not use post-deadline changes to repair the submitted candidate. |

The highest-risk dependencies are account eligibility/signing, qualifying simulator footage and usable judge access. They start now. The entrant has confirmed the simulator route; absence of a headset is not itself a disqualification under the FAQ, but leaves the device questions unresolved. A StoryTable fallback also needs its own final checks and does not remove those dependencies.

### The short demonstration

Target **2:30–2:45**, edited from a complete XR Simulator visit of the selected release. Label the footage, simulated input mode and excerpts accurately. The existing fixed-clock desktop video is an internal review aid, not the qualifying capture. This proposed river edit supersedes the table shot order only if the river is selected:

| Time | What the judge sees and understands |
|---|---|
| 0:00–0:12 | One concrete invitation tied to the actual audience; fire, a hand selecting the pot and the start of the river transition. No long logo sequence. |
| 0:12–0:35 | The seated river reveal and the older woman's uncertainty. Establish documented context and imagined meeting concisely. |
| 0:35–1:30 | Excerpts of the two perspectives, with one unbroken shot from a real selection to the resulting change in the telling. Show the controls that actually exist in this scene. |
| 1:30–2:05 | Protect the younger traveller's full concluding sentence and a quiet three-second pause. Adjust earlier slots after listening rather than accelerate or cut her voice. |
| 2:05–2:25 | Explicit Keep, reopen and the strand; make the finite reason to return visible. |
| 2:25–2:45 | Brief truthful adaptation evidence and final product identity. Connect to the original motivation of making fragmented heritage approachable through storytelling; SDK names belong mainly in the written entry. |

If the selected narration needs longer, shorten the pitch and optional closing shots first. Do not insert the gourd's prop interaction into the water encounter. Do not promise fresh generation, a recovered ancestor, live conversation or a room feature on the strength of unrelated code or footage.

### What this plan protects

Keep the illustrated direction, full water payoff, accessible controls, meaningful question/thread selection, truthful sources, offline completion and explicit replay. Defer new histories, languages, live voice, hosted generation, new character models and additional mechanisms. Repair existing MR recovery if it blocks use; do not add a new room feature to chase a judging bullet.

The winning argument to test is that hearing another person's side changes what the visitor carries away, and that sitting beside the encounter makes that act of listening feel personal. If viewers remember only the scenery, the next change is to the storytelling and interaction clarity. More generated art does not answer that finding. An increased agent score is useful criticism, not a substitute for a working judge build or an unfamiliar person's response.

## Final ten days — November 8–18

Historical schedule and scope proposal, retained for provenance. The November 16 submission plan above now controls scheduling; the release scene still requires the decision described there.

**November 15 prototype addition:** the entrant requested testing “stories you step into” through a cinematic introduction and water-pot transition into a seated riverside. The [isolated prototype brief](river-threshold-prototype.md) records the implementation and comparison questions. The entrant approved the original paintings' style, authorised provisional local reuse of the web intro music, and requested recorded effects and matching animation. This is a reviewable experiment alongside StoryTable; promotion, listening, device acceptance and submission rights remain separate decisions.

**Ship the pop-up theatre.** Following the November 15 framing comparison, use the prepared, offline water telling as the judge and video path. Choose the water pot, follow **Stay with the one who remembers**, then turn to **Walk with the younger traveller** in the same sitting. Preserve her full final sentence: she will eventually carry the remembered sound and uncertainty herself. The visitor chooses whose side to hear; the characters make their own decisions. The gourd remains an additional judge check for its prop interaction, and all three tellings remain in the build. This replaces the earlier gourd-first demonstration choice, not any application behaviour.

**November 15:** the [verification record](../testing/2026-11-15-story-table-verification.md) now contains actual Unity results and corrections. The revised [submission draft](../release/submission-draft.md) promises an encounter with everyday lives behind history, using openly imagined people and bounded documented context. The [story comparison and demo script](../release/access-handoff.md#why-the-water-story-leads) select water for its connection to passing on memory, while acknowledging gourd's stronger prop action. This is an editorial choice, not participant validation or proof of emotional effect. The founder's own account, selected-recording listening review and newcomer review remain open. The dated schedule below is the original target, not evidence that an upload or submission happened.

| Date (Eastern) | Work | Exit evidence |
|---|---|---|
| Nov 8–9 | Build the approved pop-up branch; confirm Start membership, app ownership, signing/version requirements, Devpost draft and Quest 3/3S access | Installable review APK with recorded identity; named device slot and dashboard owner, or explicit missing access |
| Nov 10–12 | Do a seated hands-only visit on a Quest 3/3S, including gourd interaction and a denied-room run; listen once; profile sustained gameplay; get one unfamiliar visitor if practical | Device/build notes, measured frame rate, one uncoached completion if available, and only reproducible fixes in scope |
| Nov 13 | Freeze experience and build; prioritize hands, readability, crashes, permission recovery or audio issues actually found | Exact release APK signed as the dashboard requires, retested after changes and bound to its source/hash |
| Nov 14–15 | Install through the Competition release link; cut the two-perspective demo from real headset footage or clearly labeled XR Simulator footage | Independent access and install; public YouTube/Vimeo link; Devpost draft with track, division, adaptation, testing instructions, and confirmed launch plan |
| Nov 16 | Submit after the account owner has verified the final links and approved the entry | Recorded submission state; preserve artifact and judge access through the announcement |
| Nov 17–18 | Reserve for access, upload or release-blocking fixes | Hard close **Nov 18, 3 p.m. Eastern / noon Pacific**; no changes after close |

**Scope cut:** skip the life-size scene and hosted generation for this submission plan. Keep the pop-up stage and the adjustable stand. Review Quest footage first; if hardware access cannot be scheduled by Nov 10, prepare the allowed simulator capture path in parallel so the video does not become a last-day dependency. Simulator footage does not verify real hands, fitting, listening or performance. Revisit the earlier book fallback only if the stage prevents a reliable complete visit after a focused fix, and verify it as its own build before switching.

The [official rules](https://start-developer-competition-26.devpost.com/rules), [FAQ](https://start-developer-competition-26.devpost.com/details/faqs) and [judging criteria](https://start-developer-competition-26.devpost.com/details/judging) were rechecked November 8. Use the [handoff](../release/access-handoff.md), [candidate identity](../release/candidate.md) and [submission draft](../release/submission-draft.md) for execution. The remaining gates are real device/participant review, content and rights, account eligibility, upload/access and publishable footage. Software checks do not close them.

## October 30 baseline — historical

The user selected the pop-up stage from captures and a motion study on October 30, before the original November 5 decision date. The upright-book build at `47b8cee` is the comparison fallback. Week 2 implemented the quieter controls, ambience and gourd invitation. See the [October 30 decision and checks](../decisions.md#october-30-2026--week-2-a-quieter-page-the-stages-sound-the-gourd-in-the-visitors-hand).

## Earlier personal-story evidence sequence

| Step | Current state | Exit evidence |
|---|---|---|
| Bounded core and content | Implemented; three distinct bundled tellings, exact approved historical passages, strict JSON/source/art validation | 349 core checks passed; generated/bundled fixtures accepted by actual .NET; 32 service checks pass |
| Optional generation service | Gemini adapter and controlled-provider/loopback tests exist; two actual public-synthetic responses pass .NET validation | Retain the narrow live smoke separately from UI/deployment and content acceptance; verify invalid output, unavailable service, timeout and cancellation choose a clearly labelled fallback |
| Unity table visit | Integrated builder/view, keyboard, room-placement adapters, application state and local storage | 36 editor and 16 table runtime checks pass; actual authenticated Unity generation/narration smoke passes; device/human whole-visit acceptance remains open |
| Actual room and hands | MRUK 207 fitting, passthrough and manual-stand fallback require hardware acceptance | Test seated hands input, denied/no-room data, table scale/orientation/reach, changed rooms, readability and sustained Quest performance |
| Content and release | Existing art/source/narration provenance retained; current rights and cultural gates remain open | Independent content/rights review, clean-source new-scene APK, independent install, judge access and honest current footage |

Protect input-to-story relevance, a complete ending, truthful evidence/capability labels, hands-only operation and offline completion. Freeform voice input, live conversation, dynamic images and a second language are explicitly deferred. Do not spend the remaining acceptance effort expanding unsupported places or eras. The configurable narration adapter does not establish listening acceptance; current implementation checks remain muted.

The development service is optional and credentials stay server-side. Online creation requires consent to sending selected context and the question/fragment. Room geometry stays local. Save realised content only after Keep; reopening must never request a replacement model output.

The [submission draft](../release/submission-draft.md) is prepared local copy. No Devpost update is claimed; the available browser is logged out, so publishing needs the user's accessible edit session/link and explicit publishing instruction. Existing gallery APK hashes and previous simulator checks stay attached to those builds. No new final APK, Quest or participant evidence is implied by this plan.

## Preserved gallery delivery plan — October 13–15

The sections below retain the prior gallery schedule and its reported checkpoints. References there to the current candidate, selected direction or next step describe that historical iteration. Their work packages and time estimates do not silently become the personal-story table plan.

## Current sprint plan

October 13: the [living gallery product scope](illustrated-theatre-scope.md) reflects the user-selected direction: Sankofa is the gallery and *What We Carry* is its first exhibit. The authored software candidate is implemented; the acceptance gates and remaining refinements below remain open. It supersedes the October 11 allocation and the later [first-place proposal](first-place-plan.md). It preserves hardware, cultural, participant and release gates. The original PRD remains the broader contract; this narrower authored candidate does not complete generated planning, live conversation or additional-language requirements.

**Starting point:** package v0.3.0 plus the October 12–13 illustrated polish and interaction fixes. The encounter, questions, passive paths, saved replay and contemporary continuation exist in software. The new v0.4.0 candidate implements gallery/frame entry and return, two vignette threads, reveal-aware contextual questions, milk hospitality, a painted coda background and exhibit-side Keep in software. Art/listening, participant and device acceptance remain open. The October 13 local review APK now carries source d0a17bd; see the decision record for checks and its fingerprint. Rebuild after further source changes before distributing.

**Next acceptance step:** review the implemented gallery → framed painting → spatial encounter → framed return, contextual question and hospitality consequence with unfamiliar viewers and cultural reviewers, then obtain device findings before expanding production. Preserve one complete silent route, one-hand use and a coherent ending. See the scope for journey timing, storyboards, interaction/state rules, asset limits, user stories and test cases.

### Scope and capacity

Provisional capacity remains 12–15 focused user hours/week. The revised estimate is **54–68 user hours plus 10 hours contingency**, or **64–78 hours**, and **82–122 hours of assisted engineering effort**. Engineering effort includes integration/testing and is not an unattended-cycle promise; some review time overlaps the two columns. The gallery replaces external hearth art but adds an estimated 2–3 user hours and 6–10 engineering hours for frame/world continuity and navigation. The low plan exceeds 60 hours by four; the high plan exceeds 75 by three. Simplify nonessential production or add capacity while preserving the reserve. Re-estimate after the first probe. Hardware and reviewer lead times remain independent dependencies.

### Sprint calendar and exit gates

| Sprint | Dates | User hours | Result and gate |
|---|---|---:|---|
| 1 Prove the gallery promise | Oct 13–19 | 10–12 | Frame/entry/return prototype, paired outcomes, safe context/vignette scripts, three-person comprehension probe and hardware/review preparation |
| 2 Build the first exhibit | Oct 20–26 | 13–17 | Both perspectives have distinct revelations; contextual griot preserves surprises; entry and ask/receive ownership are clear |
| 3 Complete the gallery return | Oct 27–Nov 2 | 10–13 | Same painting reframes; exhibit-side Keep/alternate/coda and both-parent/legacy persistence pass |
| 4 Verify and simplify | Nov 3–9 | 12–14 | Formative novice targets assessed, ten complete device visits, meaningful profile/recovery evidence and review corrections |
| 5 Present and release | Nov 10–16 | 9–12 | Frozen candidate installed independently; real-runtime demo and accurate materials; submission when authorised |

Internal target remains November 16. November 17–18 is contingency, with the official November 18 noon Pacific / 3 p.m. Eastern deadline checked October 13 in the [official rules](https://start-developer-competition-26.devpost.com/rules). No automation or external submission has been scheduled.

### Delivery rules and dependencies

The current ticket sequence is IT-P01–IT-P11 in the [backlog](../backlog.md#previous-illustrated-theatre-sprints). Critical path: prototype and story → route/interaction integration → kept return → user/device acceptance → release. Review and hardware preparation proceed alongside development; a quality governor or simulator cannot close their gates.

Stick feeding, a second lobby and a catalogue browser are outside this release. Cut animal approach, peripheral scenery and decorative effects first. Simplify vignette art before deleting its unique revelation. Preserve recognizable frame/world continuity, the illustrated modern ending, spoiler-safe context, hospitality without penalty, truthful passive outcomes, cultural review and device testing. If only one thread fits, record that reduction and narrow the product claim explicitly. Do not consume the acceptance/submission reserve to disguise overruns.

A checked software ticket is not a cultural, participant or device pass. Save concise evidence tied to build/commit and retain private raw material outside Git. No purchases, reviewer messages or public submissions are authorised merely by this plan.

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


October 14 panel response: the v0.5 candidate adds authored acting and memory actions, focused revisits and bounded input/save/recovery fixes. The [implementation map](../reviews/2026-10-14-panel-implementation.md) separates delivered software changes from the [next human/device evidence](../testing/gallery-next-evidence-session.md). Sprint acceptance remains open where it depends on hardware, participants, cultural review, listening or release terms.

## October 14 production hardening checkpoint

The [production iteration](../reviews/2026-10-14-production-iteration.md) records v0.7: corrected carry/logging/modal preferences, eye-relative placement and comfort controls, shorter memory returns, registered painted performance and animal, protected caption focus, current default gallery commands and refreshed review materials. Its software results do not close the hardware, cultural/language/listening, participant or release-access gates above.
