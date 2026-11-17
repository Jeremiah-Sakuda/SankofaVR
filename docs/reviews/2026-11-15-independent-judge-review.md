# Independent judge review — November 15, 2026

Three delegated agent reviews, requested by the entrant. This is an initial assessment of implementation, story, desktop visuals and release evidence, followed by a reserved record of the iteration. It is not an official judging result, a prediction of placement, participant research or headset acceptance.

## Scope and method

The reviewers were directed to read the required local competition context, repository instructions, README, [current PRD contract](../product/prd.md#current-mvp-contract), [delivery plan](../product/delivery-plan.md) and relevant [decisions](../decisions.md). The local brief was present in the shared checkout; the coordinating agent reported a successful competition-context workspace check covering 2,069 tracked files before review. The private brief is not reproduced here.

The selected illustrated treatment is [RiverThresholdPainted](../product/river-cast-comparison.md), within the [river-threshold experiment](../product/river-threshold-prototype.md). StoryTable remains the release default. Neither this review nor its scores promotes the river experiment into the MVP, replaces the original comparison scenes or authorizes publication.

The experience reviewer inspected the story, flow, submission draft, demo script and actual desktop stills, including `artifacts/river-cohesion/painted-front.png` and `artifacts/river-threshold/02-griot.png`. Engineering and presentation reviewers supplied separate assessments to the coordinating agent. The existing eight-second cast clips hold an early chapter with a diagnostic review camera; the earlier 50-second preview has a separately mixed soundtrack. These are not recordings of a complete hands-only visit or accepted runtime listening. Their scope is recorded in the [cast verification](../testing/2026-11-15-river-cast-verification.md) and [threshold verification](../testing/2026-11-15-river-threshold-verification.md).

The coordinating agent reported initial results of 445 core, 49 EditMode and 81 Python checks passing. Those results are software evidence, not independent reproduction by each reviewer. The full PlayMode run and fixes were still pending when the initial scores below were recorded.

The four criteria carry equal weight under the [official judging guidance](https://start-developer-competition-26.devpost.com/details/judging). This review expresses each as a score out of 25. The guidance includes Quest performance and honest gameplay-led presentation; the [rules](https://start-developer-competition-26.devpost.com/rules) separately require the Unity APK through the Competition release channel and a public demonstration link. A software review does not establish compliance with those delivery conditions.

## Initial independent scores

These scores are frozen before this audit's iteration. They assess the available implementation and evidence, including the selected river prototype's unresolved journey, rather than assuming that the most attractive capture is the delivered application. The reviewers share a workspace and product context; their separate assessments are not independent samples of an audience.

| Reviewer | Innovation & Creativity /25 | Experience Design /25 | Technical Implementation /25 | Polish & Presentation /25 | Total /100 |
|---|---:|---:|---:|---:|---:|
| Experience and story | 19 | 14 | 15 | 17 | **65** |
| Engineering | 18 | 14 | 14 | 16 | **62** |
| Presentation | 19 | 15 | 15 | 17 | **66** |

Confidence is moderate for inspected story, framing, source-level defects and desktop composition, and low for headset behavior and sound. No reviewer established real-hand reliability, physical reach, comfort, Quest frame rate, unfamiliar-room fitting, listening quality, cultural acceptance or participant response. No actual competition judge was consulted. Test counts do not substitute for those observations.

The experience assessment credited a distinctive narrative use of perspective, complete offline content, source boundaries, explicit replay and stronger painted art. It withheld higher marks because the central second-side journey could lose its controls, the foreground interface competed with the encounter, the current release artifact did not contain the latest work, and device and listening evidence remained absent.

## Actionable findings before iteration

The table records the reviewed state and required evidence. An engineering report or source inspection is identified as such; it is not silently upgraded into an observed headset failure. None of the findings is marked addressed in this initial record.

| Priority and finding | Evidence and consequence | Focused improvement and observable check |
|---|---|---|
| **P1 — Narrated riverside can hide both routes to a perspective choice** | Source inspection: [RiverThresholdPrototype](../../unity/Assets/Sankofa/Runtime/Personal/RiverThresholdPrototype.cs), `LateUpdate`, hides stage panels while riverside. [StoryTableView](../../unity/Assets/Sankofa/Runtime/Personal/StoryTableView.cs), `SetChoices`, suppresses page alternatives in self-turning mode when figure-choice IDs exist. The coordinating agent was reproducing the combined failure. The visitor risks missing the second side that gives the story its purpose. | Make available choices depend on the currently visible presentation. Complete the narrated route through both sides, ending, explicit Keep and exact reopen, using visible controls. Also check manual captions and return to the table. |
| **P1 — A selected water pot can lead to a different telling inside the river** | Engineering reported that typed input can override the selected interest after the threshold has decided to cross. [PersonalStoryApp](../../unity/Assets/Sankofa/Runtime/Personal/PersonalStoryApp.cs), `StartPrepared`, emits the prepared-story event; [BundledPersonalStoryComposer](../../unity/Assets/Sankofa/Core/Personal/BundledPersonalStoryComposer.cs) resolves question/fragment themes. [RiverThresholdPrototype](../../unity/Assets/Sankofa/Runtime/Personal/RiverThresholdPrototype.cs), `BeforePreparedStory`, uses the event to request water. A trade-household result could therefore inherit the water-only presentation. | Decide crossing from the resolved telling while retaining meaningful question influence. Check a water-pot selection with trade-related input and verify the accepted story receives the intended table or river presentation. |
| **P1 — Physical-table placement can begin while the river is still visible** | Engineering reported that **Use my table** can invoke [PersonalStoryApp](../../unity/Assets/Sankofa/Runtime/Personal/PersonalStoryApp.cs)'s room-placement handler without leaving riverside. [StoryTablePlacement](../../unity/Assets/Sankofa/Runtime/Personal/StoryTablePlacement.cs) owns room fitting, but the river presentation has its own state. This can obscure the room during a task that asks the visitor to locate a physical surface. | Return to a coherent table presentation before physical placement. Check the request, missing/denied room-data recovery and return to the story with the same chapter. Simulator checks remain separate from physical-room acceptance. |
| **P1 — Stand adjustment can reposition the river using the visitor's changed head position** | Engineering reported the issue; source inspection shows `PlacementChanged` calling `AnchorToSeat` in [RiverThresholdPrototype](../../unity/Assets/Sankofa/Runtime/Personal/RiverThresholdPrototype.cs). That method samples the current camera position. A stand-height event after a lean can therefore change the surrounding river's anchor. | Separate a deliberate recenter from stand-height changes. Move the test viewpoint, adjust the stand and require the river's placement to remain stable; then separately verify explicit recenter. No tracked-camera movement should be introduced. |
| **P2 — Foreground controls break visual continuity with the opening** | The inspected `painted-front.png` shows 11 visible buttons, including two Pause controls, across roughly the lower third. The intro still has three controls. [StoryTableView](../../unity/Assets/Sankofa/Runtime/Personal/StoryTableView.cs), `RefreshControls`, and [RiverThresholdPrototype](../../unity/Assets/Sankofa/Runtime/Personal/RiverThresholdPrototype.cs), `Paint`, each supply Pause. This is demonstrated desktop composition, not a measured headset legibility result. | Consolidate duplicate controls while preserving immediate pause and one-hand alternatives. Consider contextual questions behind Explore context. Capture the same chapter after the change and verify every essential action remains available. |
| **P1 release gate — Build, copy and showcased experience do not yet identify one current candidate** | [SankofaBuild](../../unity/Assets/Sankofa/Editor/SankofaBuild.cs), `BuildMain`, targets StoryTable. The [candidate record](../release/candidate.md) identifies a build preceding these changes; the [submission draft](../release/submission-draft.md) describes the table experience. The selected river scene remains an experiment. | Produce a clearly identified review artifact and complete capture for the scene actually under review. Any eventual submission build, testing instructions, copy and video must agree. Preserve the default until an explicit scope decision; do not treat a successful local build as verified channel access. |

The affected requirements are SK-02 (hands and alternatives), SK-04 (meaningful question influence), SK-07/08 (spatial presentation and participation), SK-12 (replay), SK-15 (seated comfort), SK-17 (room placement) and SK-18 (honest evidence). The affected modules are Unity presentation, its connection to the existing personal-story flow, placement and release records. These are targeted corrections to the implemented journey; they do not justify expanding content, providers or supported cultures.

## Story and presentation judgment

**Keep the water telling as the lead.** The [authored younger traveller chapter](../../unity/Assets/Sankofa/Core/Personal/BundledPersonalStoryComposer.cs) turns the older woman's uncertain location into the traveller's eventual responsibility to carry the memory, imperfectly, herself. That is a stronger connection to Sankofa's purpose than the gourd's exchange. This is editorial judgment from the actual text, not demonstrated emotional impact. Preserve the full final sentence and a quiet pause in the [demo edit](../release/access-handoff.md#capture-one-story-well--target-230).

The illustrated people and dusk palette materially improve continuity with the introduction. In the inspected front capture, the two figures appear similar in age; clear role labels and staging should carry the difference between the woman who remembers and the younger traveller. This is a lower-priority subjective concern, not a finding that requires regenerating the approved cast.

The presentation reviewer additionally identified three checks that a still image cannot close:

- **Ground contact:** the painted actors use a fixed vertical offset in [RiverCastPerformance](../../unity/Assets/Sankofa/Runtime/Personal/RiverCastPerformance.cs), and the reviewed [river builder](../../unity/Assets/Sankofa/Editor/SankofaRiverCohesion.cs) supplies no contact-shade patch at their feet. The reviewer described the cutouts as appearing to float despite the strong portraits. This is a subjective visual finding; inspect the complete movement before deciding whether a small contact shade or registration adjustment is needed.
- **Perspective and pose changes:** the river cast copies the tabletop stage's positions and mirroring. With the table hidden, a perspective change could read as people sliding or reflecting rather than the theatre turning. The [figure shader](../../unity/Assets/Sankofa/Art/Shaders/CohesiveFigure.shader) also switches pose textures at the blend midpoint while darkening. These are inspectable implementation choices, not demonstrated motion defects. Review the full authored pace, including both perspectives, before changing approved art or animation.
- **Opening sound:** [RiverThresholdPrototype](../../unity/Assets/Sankofa/Runtime/Personal/RiverThresholdPrototype.cs) plays two matching welcome clips; the new griot/title lines are primarily captioned. The provisional music is editor-only under LocalOnly. The preview's editorial mix must not imply that the APK contains the same score or complete spoken opening. Evaluate actual permitted runtime audio separately; muted captures cannot establish its quality.

The [threshold sequence](../../unity/Assets/Sankofa/Flow/RiverThresholdSequence.cs) has a skippable 32-second opening and six-second crossing. Keep the cinematic invitation, but use honest excerpts in the short submission video so an actual meaningful selection appears early. Do not imply that edited waits disappear in the application. Do not spend the remaining iteration on another mechanic before the complete two-perspective path is reliable.

The [submission draft](../release/submission-draft.md) already distinguishes imagined meetings from documented context and avoids promising recovered ancestors or unlimited personal stories. Its six possible perspective beads are finite content, not evidence of lasting engagement. The founder's personal motivation, newcomer understanding and actual listening remain open rather than being supplied by invented testimony.

## Post-iteration evidence — software, desktop review and local APK

The coordinating agent supplied the following confirmed software, capture and local APK results. The experience reviewer then inspected the six final river, choice, perspective, ending and reopen stills independently and reassessed the available evidence below. Initial scores remain preserved above. The APK is built and structurally checked; installation remains unverified. The interactive simulator launched but its check is blocked by a Windows Security prompt and is not complete.

The first full Unity PlayMode run reported **111 cases: 100 passed, one failed and ten optional skips**. The failure was in the preserved Hearth comparison: its test sampled before `LateUpdate` had finished. Engineering changed the [Hearth test](../../unity/Assets/Sankofa/Tests/PlayMode/HearthVisitTests.cs) to sample after the frame completed and added frozen-clock assertions. The existing 3 mm, 0.5 degree and 8 mm tolerances were retained. No Hearth runtime or scene change was made for that failure.

All five new [river judge regressions](../../unity/Assets/Sankofa/Tests/PlayMode/RiverJudgeVisitTests.cs) failed against the reviewed implementation before correction, confirming the reported source-level risks in software:

| Regression | Observed failure before correction |
|---|---|
| Narrated perspective choice | The visible choice ID set was empty |
| Typed theme overriding the water-pot selection | The trade-household telling entered the river |
| Stand height after a changed head position | The river world moved **0.20976 m** |
| Riverside controls | Two Pause controls were present |
| Request to use a physical table | The presentation remained in Riverside |

After a narrow five-file implementation correction, the focused run reported **six passed, zero failed and one optional capture skip**, including the Hearth check. The core suite (**445 passed**) and Python checks (**81 passed**) also passed again. These results establish focused software regression coverage; they do not establish whole-build acceptance or any of the missing device and human evidence.

The broader affected Unity run subsequently reported **65 cases: 60 passed, one failed and four optional skips**. The remaining failure was test setup: an intake-room assertion expected the self-turning interface while the test had manual mode selected. The test was corrected to select `TurnsPages = true` and was included in the final focused painted run: **nine passed, zero failed**. That final run includes the complete caption-paced visit, stale-provider protection, intake-room recovery, caption preservation and cast grounding. The reported final companion results are **49 EditMode, 445 core and 81 Python checks passed**. These are the actual run partitions; they are not presented as a single replacement full-suite total.

The grounding correction follows the generated surface, approximately 5 cm below the old figure baseline. Approved art was not regenerated, and the caption font was preserved. This corrects placement; it does not establish a physically lit contact shadow or headset depth acceptance.

The final local media is `artifacts/judge-visit/complete-painted-visit.mp4`, **176.458 seconds**, with 4,235 frames at a fixed 24 fps simulation clock. The coordinating agent reports a successful complete FFmpeg decode. The capture is silent, mono desktop rendering with synthetic single-pointer pinch actions and complete authored caption/stage timing. Neither 24 fps nor a successful video decode measures application frame rate. This is not a listening review, real-hand run or qualifying XR Simulator capture merely because it renders the Unity application.

### Local Android review artifact

The coordinating agent completed [BuildRiverReview](../../unity/Assets/Sankofa/Editor/SankofaBuild.cs) and reported the following artifact checks. The default release target remains StoryTable; this named comparison artifact does not promote the river into the release MVP.

| Property | Confirmed local result |
|---|---|
| File and scene | `artifacts/Sankofa-RiverPainted-Review.apk` / `RiverThresholdPainted` |
| Size | 76,345,496 bytes, approximately 72.8 MiB |
| Package and version | `com.sankofa.quest`, `0.1.0+e48b1e8-dirty`, version code 1 |
| Android target | ARM64; minimum SDK 32, target SDK 34 |
| Build result | Zero errors; one warning recommending the optional OpenXR Prioritize Input Polling setting |
| Signature | Valid APK v2 signature using the Android Debug certificate |
| Archive checks | ZIP CRC verification passed for 1,171 entries; no LocalOnly, HACKATHON or reference-path matches reported |
| SHA-256 | `a5b5aa472d7ba514fa71310a59d1e1a9909604c7435fb80e2da230d777334b8f` |
| Device and distribution | No attached device; not installed or verified through a Competition release channel |

The coordinating agent also reported the workspace check passing for 2,073 tracked files. These checks establish a concrete local artifact and archive integrity. They do not establish APK launch behavior, accepted dashboard signing/version identity, device performance or judge access.

### Interactive simulator attempt — blocked, not accepted

The coordinating agent launched `RiverThresholdPainted` muted in the XR Simulator and observed XR render textures initialize. A desktop screenshot then showed a **Windows Security firewall prompt for Node.js JavaScript Runtime**. The prompt prevented further permitted computer interaction. The entrant was asked to handle or dismiss it; no interactive hands-only visit or acceptance is claimed.

The coordinating agent's log review identified the exception at line 2442 as Unity Editor `Search.SearchDatabase` startup indexing, with no Sankofa stack. An already-disposed `CancellationTokenSource` entry at line 3371 has no accompanying stack and remains unattributed. The latter is not classified as harmless or assigned to Sankofa without further evidence. These observations do not turn the interrupted interactive check into a pass or establish that the APK fails.

| Follow-up | Latest confirmed status |
|---|---|
| Choice visibility, resolved-story routing, room transition and stable stand adjustments | The original five regressions and the final affected painted checks pass, including recovery and theme-override coverage |
| Quieter controls and matching desktop comparison | Duplicate-Pause regression passes; final river still shows one Pause with caption size retained. The five-versus-eleven action comparison also changes from manual to self-turning mode and cannot all be attributed to the fixes |
| Full authored-pace cast and pose review | Complete silent capture and decode pass. This reviewer inspected milestone stills, not every motion frame; no new claim of acceptable sliding, mirroring or pose transitions follows from those stills |
| Complete two-perspective visit, Keep and exact reopen | Final caption-paced capture reaches both sides, explicit ending and exact reopen; separate actual listening remains open |
| Software verification | Final nine painted checks, 49 EditMode, 445 core and 81 Python pass; wider-run failures and optional skips are disclosed above |
| Review APK identity and the exact scene captured | `Sankofa-RiverPainted-Review.apk` is built, fingerprinted and structurally checked as recorded above; it is not installed. The default release target remains StoryTable |
| Interactive XR Simulator visit | Launched muted with XR render textures initialized; blocked by the Windows Security prompt before an interactive pass. The unattributed disposed-token log entry remains unresolved |
| Reviewer reassessment against changed behavior | Experience reviewer: 72/100 on the evidence below. Initial assessments remain unchanged; other reviewers' reassessments are not inferred |
| Quest, listening, cultural, participant and judge-access acceptance | Not established by these agent reviews or software tests |

### Experience review after the iteration

The reviewer opened `02-river.png`, `03-perspective-choice.png`, `04-first-side.png`, `05-other-side.png`, `06-ending.png` and `07-exact-reopen.png` from `artifacts/judge-visit/`. The initial river panel shows five visible actions rather than eleven in the earlier still, but the final capture uses self-turning mode and the earlier image uses manual mode. The entire reduction cannot be credited to the fixes; removal of the duplicate Pause is separately demonstrated by the regression check. The perspective choice explicitly exposes both routes; the end of the first side says to choose below to hear the other side. The younger traveller's final sentence is visible in full. The ending explicitly states that both sides were heard and offers a deliberate Keep. The reopened table shows the opening again and two colored beads. Exact saved-content equality is supported by the automated check, not proved by that image alone.

| Criterion | Initial /25 | After iteration /25 | Reason for the change or limit |
|---|---:|---:|---|
| Innovation & Creativity | 19 | **19** | The underlying narrative idea remains strong and unchanged. Reliability fixes do not add novelty or demonstrate sustained return use. |
| Experience Design | 14 | **18** | Both perspectives now have visible controls and explicit instructions; the complete captured path reaches its ending and reopen. Routing and placement failures have focused regression evidence. Uncoached hands use remains unverified. |
| Technical Implementation | 15 | **17** | Reproduced failures were corrected and checked, including recovery and state boundaries. The named Android review artifact now builds and passes structural checks; Quest launch, performance, real hands and physical-room behavior remain unmeasured. |
| Polish & Presentation | 17 | **18** | Duplicate controls are removed, captions remain readable and the final payoff is presented intact. The overall button-count comparison also changes modes. Sound, whole-motion acceptance and submission/build alignment still limit the score. |
| **Total** | **65** | **72** | An evidence-based improvement in completion and clarity, not a claim of competition readiness. |

Confidence remains moderate for story, visible UI and software behavior, and low for headset and auditory experience. The narrower complete visit is now better supported than the initial attractive stills. Completing the APK closes artifact uncertainty but does not add installation or device evidence, so the experience review remains **72/100**. The remaining highest-value evidence is actual listening, installation and independent access, and a newcomer completing the intended interaction without explanation. The unresolved motion observations should be assessed from the full video rather than promoted to defects or dismissed because screenshots look good.

### Comparison of agent reassessments

These are agent estimates supplied after the targeted iteration and verified local APK, not official scores or measured user outcomes. The initial scores remain frozen. All criterion scores are out of 25 and totals are out of 100; agreement between agents does not substitute for independent participants or competition judges.

| Reviewer | Initial total | Innovation | Experience | Technical | Polish | Final total | Evidence boundary |
|---|---:|---:|---:|---:|---:|---:|---|
| Experience and story | 65 | 19 | 18 | 17 | 18 | **72** | Final milestone stills independently inspected; software and completed local APK results reviewed; no hardware or listening acceptance |
| Engineering | 62 | 19 | 18 | 18 | 17 | **72** | Final assessment supplied after verification of the APK; installation, real hands, device performance and interactive simulator acceptance remain open |
| Presentation | 66 | 19 | 18 | 17 | 18 | **72** | Post-fix assessment reported by the coordinating agent; its completed-build condition is met, with audio and participant response still unverified |

The estimates improved from 62–66 to 72 because visible actions, recovery, completion and presentation now have stronger evidence. They do not estimate the probability of winning or settle the remaining interactive, release and human-acceptance gates.
