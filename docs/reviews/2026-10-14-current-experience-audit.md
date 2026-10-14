# Current experience audit — October 14

**Verdict:** the living-gallery direction is coherent and worth continuing. The current candidate should not be submitted unchanged. Its strongest qualities are the authored relationship, honest visitor role, painted-to-tangible handover and explicit return to the artwork. Its largest remaining weaknesses are inconsistent handling of alternative input, accessibility during a timed story, and a second perspective that reveals less new information than the pitch suggests.

This audits **LivingGallery, package v0.6.0**, at clean source **2f439b2**, with the review APK built from **da85045**. It supersedes broad acceptance interpretations of the [completion pass](../release/candidate.md), not its historical test results. No production behaviour was changed during this audit.

## Evidence and limits

Reviewed the current story, application flow, input/presentation/settings/storage adapters, tests, generated scene composition, provenance, release documents and desktop captures. Rechecked the official judging/rules/FAQ pages. Ran fresh core/editor checks and a temporary silent runtime observation test using the **real selection panel**. The test used a temporary visit store, restored preferences, and was removed from the Unity project after its reproduction source was retained locally.

| Fresh check | Result | What it establishes |
|---|---|---|
| .NET core suite | 303 passed | Existing story/state/input/storage assertions still hold |
| Unity EditMode | 30/30 passed | Existing editor/content/wiring assertions still hold |
| Targeted runtime observation | 1 completed, no project errors | Reproduced the caption, settings and logging findings below; completion is **not** acceptance of those behaviours |
| Asset integrity | 30 declared inputs, 86 current line fingerprints, zero problems | Declared inputs match; not artistic, cultural or rights clearance |
| APK identity | Hash matches the recorded candidate | Existing artifact identity; no new build or device installation |

Local evidence: `artifacts/logs/current-audit-core.log`, `artifacts/tests/current-audit-editmode.xml`, and `artifacts/experience-audit/` (probe source, observations, three captures and a synthetic journal log). These stay ignored. Earlier 14-case gallery and historical runtime results remain historical evidence; the entire runtime suite was not rerun for this audit.

**No sound was played, no simulator was used, and no Quest or participant was tested.** Visual findings concern current desktop renders and code, not binocular comfort. Voice, pronunciation, mix and emotional delivery were not evaluated by listening. Accelerated synthetic progression is useful for state inspection, not human comprehension or session timing.

## What is working

- The women own the agreement on every path. Helping changes the visitor's address and remembered actions without making an outsider the author of their trust. Passive outcomes do not falsely credit the visitor.
- The shortfall, admission and remembered waiting give the encounter a dramatic turn. Historical context and fictional reconstruction remain distinguished, with disclosure before entry and *Enkare Nairobi* retained.
- The same painting frames entry and return. The gourd crosses from a painted palm into reachable space; the keepsake places a visit beside the exhibit rather than rewriting its canonical story.
- Bundled narration and authored questions work without a service. There is no runtime music generation or invented claim of live AI conversation.
- Application authority, cancellation, saved realised plans, replacement confirmation and reduced-motion coverage provide a solid foundation. Most problems below are concentrated enough to repair without changing the concept.

## P1 — Caption clearance misses the actual selection carry

**Confirmed runtime defect.** Receiving the flour gourd through the selection button resolves the invitation and places the object at the visitor carry point. [StoryStage.Carry](../../unity/Assets/Sankofa/Runtime/Stage/StoryStage.cs) returns after `MoveTo` when no receiving hand exists. [StageObject.IsCarried](../../unity/Assets/Sankofa/Runtime/Stage/StageObject.cs) only recognises a follow anchor. [GalleryCaptionPlacement](../../unity/Assets/Sankofa/Runtime/UI/GalleryCaptionPlacement.cs) then excludes the object because it is neither carried by that definition nor still invited.

Reproduction: enter the exhibit, press `Accept:receive-flour` through `PokePanel`, advance to the divided-provisions line. The capture shows text crossing the received gourd. Observations record `Accepted`, `IsCarried=false`, no receiving hand and a positive projected overlap. The same null-hand path is used by replay; replay overlap itself was not separately captured in this probe.

The earlier regression created a follow anchor directly, so it tested a carried object but missed the actual selection alternative.

**Fix/acceptance:** represent logical carried state consistently, or pass the fallback carried object explicitly to caption placement. Test real button acceptance, tracked-hand acceptance, loss of tracking, placement and saved replay. Check every text size and retain readable fallback captions when no clear position exists. Device movement/stereo remains a separate check.

## P1 — An unkept visit still leaves a diagnostic interaction trail

**Confirmed local logging defect relative to the saving promise.** [SankofaApp.LogJournal](../../unity/Assets/Sankofa/Runtime/App/SankofaApp.cs) writes the entire journal unconditionally on closing/end; individual cues are also logged. There is no development-build or consent guard.

The probe completed without Keep: zero visit-store files, `KeptVisit=null`, but one journal log containing route, outcomes and head-direction notices. Explicit saving is correct in the dedicated store; it does not cover diagnostic retention. This is not evidence of remote transmission or identity collection. Release-device log visibility/retention was not tested, but the unguarded source is included in the candidate.

**Fix/acceptance:** remove interaction trails from normal release logs. Keep coarse health diagnostics and make detailed journals development-only or explicitly opt-in. Verify a no-Keep visit and Forget do not leave a retained trail through the supported logging mechanism. Qualify deletion promises if any diagnostic boundary remains outside application control.

## P2 — Settings let an invitation expire while the visitor adjusts accessibility

**Confirmed runtime behaviour.** `OnSetting("settings")` opens preferences without pausing the flow. The probe opened Settings during the milk invitation, waited eleven real seconds, and observed a default outcome while Settings remained open. This is an honest passive outcome, but an avoidable loss of opportunity while the visitor attends to the interface.

**Fix/acceptance:** suspend story and invitation time during modal preferences; preserve whether the visitor was already paused. Cover open/close, recenter, credits, handover cancellation and repeated settings changes. If deliberately nonmodal, the continuing story must be obvious and essential opportunities recoverable. Current source does neither.

## P2 — Essential controls and caption-only use need attention

**Source/capture finding; device severity unmeasured.** Settings and pause/return controls sit low beside the lap. At the default seated eye, the Settings centre is approximately 0.70 m away, 68 degrees to the side and 40 degrees down; the main controls sit lower. They are absent from the forward 70-degree audit capture. That does not prove Quest invisibility or physical unreachability: headset field of view, posture and pointer use differ. It does show that discovering them is an assumed behaviour.

Settings expose text size, motion, ambience, height, recenter and credits. There is **no in-app narration mute/level or caption-only setting**: `ApplySettings` changes ambience but not the narrator. OS volume is the current alternative. Height adjustment is bounded to eight centimetres each way; essential forward reach is not configurable.

**Fix/acceptance:** introduce a discoverable, deliberate controls location or opening hint, independent narration/ambience controls and a caption-only mode. Test a first-time seated user finding pause, captions and recenter without instruction, plus one-hand selection and physical Keep. Do not describe general “sound controls” as a narrator mute until implemented.

## P2 — The other perspective needs a stronger new revelation

**Editorial judgment, supported by the script.** The first route already tells the visitor “Last time, you waited for me.” The second dramatizes that earlier meeting. It deepens empathy, but partly illustrates information the visitor already knows rather than rewriting their understanding.

Calculated from current declared narration/pause/wait schedules, the focused *brings* revisit is about **248.18 seconds**, including **37.44 seconds** of its route-specific memory; the *remembered* revisit is **251.08 seconds**, including **40.34 seconds** of its memory. That is roughly 15–16% route-specific memory duration. Transitions, questions and visitor pauses are excluded. This is a pacing diagnostic, not a claim that all other images or subjective experiences are identical.

**Fix/acceptance:** give each perspective a fact, intention or misunderstood detail the other does not already explain. Reach that material sooner on return, while retaining one physical beat and enough context to stand alone. In a blind review, ask what changed in the visitor's interpretation, not merely which menu they chose. One artwork with two memories and a coda is a valid MVP; sustained return interest remains unproven.

## P2 — Painted people and tangible props do not always share the same world

**Visual judgment from current captures.** The offering pose is the strongest ownership moment. Other poses leave gourds floating beside hips without an apparent supporting hand. The native ewe has a pale, simplified fleece body and thin legs; its placement can cover Naserian's hands. Props, painted people and miniature memory panels still use visibly different material languages. The large dark buttons compete with the artwork in the opening view.

The cast swaps among fixed atlas cells without per-pose scale, baseline or hand-anchor calibration. The atlases also contain edge glow and some boundary contamination. Alpha bounds vary between cells, but those bounds include fringes: they do **not** establish a measured foot jump. Current acknowledgement uses card movement/expression vocabulary rather than a convincingly directed look at the visitor. Memory panels communicate their meaning, but still resemble explanatory cards more than a scene unfolding.

**Fix/acceptance:** calibrate each pose's registration and prop anchors, establish one pigment/light treatment for focal objects, keep the sheep away from hands, and choreograph one legible reaction per important act. Preserve the painted approach rather than returning to expensive rigs. Capture identical-camera before/after states and inspect them in stereo. Garments, objects and staging remain review drafts; attractive rendering cannot establish cultural suitability.

## P2 — Cultural attribution should be available on the first visit

The Akan origin of *Sankofa* is acknowledged in `today.word`, but that line belongs to the modern continuation behind Keep → return → Follow. Many first-time visitors will never reach it. The opening places the encounter among Kikuyu and Maasai people in Kenya without explaining the title's distinct origin. The inherited West African griot voice direction also warrants review for this Kenyan telling; its appropriateness was not judged by listening here.

**Fix/acceptance:** make concise title attribution accessible from the initial exhibit or context/credits, without turning narration back into a bibliography. Have reviewers assess framing, garments, reciprocal relationship, Maa pronunciation and voice together. Maintain the reconstruction disclosure and source register.

## P2 — “Exact replay” needs a precise boundary

Saved realised plans preserve the written route, recordings and accepted outcomes. That is valuable. Presentation binds saved line IDs to the **current** [GalleryPerformance](../../unity/Assets/Sankofa/Runtime/Stage/GalleryPerformance.cs), poses and scene assets. A legacy plan can therefore acquire newer choreography. Questions and interruption timing are already documented as excluded.

**Fix/acceptance:** describe replay as the same realised telling/path/outcomes unless presentation is versioned too. If performance fidelity is part of the promise, store a presentation profile and retain its assets. Test compatibility against representative older saves. This finding does not invalidate the current saved-plan architecture.

## P2 — Contributor entry points and submission evidence lag the product

The README's opening identifies LivingGallery correctly, but its later unqualified Status still describes the historical 85-line, rigged-character slice. Its Try it command opens `PlayHearth`; the default build entry also targets Hearth. A contributor following the familiar default can evaluate or build the comparison instead of the candidate.

The 27-second preview is honestly labelled synthetic desktop footage. It is edited excerpts, not an actual entry transition, paired perspective demonstration or real-hand visit. Prepared reviewer packets are not completed reviews.

**Fix/acceptance:** make gallery commands/status the defaults and label every comparison section clearly. Demonstrate the painting becoming space, a real hand receiving an object, a truthful passive alternative, and the second perspective's consequence. The final access/video/install evidence must use the official permitted path; a desktop montage cannot establish it.

## Judging implications and remaining gates

The official rubric has four equally weighted criteria. This audit assigns no official scores or win probability. The concept's originality is strongest when the painting becomes reachable space; innovation depends on visible consequences rather than provider count. Experience is currently limited by control discovery, timed settings and repeated exposition. Technical implementation has useful software evidence, but headset performance and hands remain unestablished. Full VR is valid; MR, live generation and multiplayer are not necessary repairs. [Official judging criteria](https://start-developer-competition-26.devpost.com/details/judging).

Before calling this release-ready: repair the two P1 defects and settings timing; validate one novice's comprehension and the revised second memory; obtain cultural/language/listening review; run the actual candidate on Quest for hands, reach, stereo, installation, sustained minimum frame rate and thermal behaviour. Confirm applicable asset/contributor terms, account/division eligibility and judge access, then deliver the required video and Competition-channel build. These are open evidence gates, not findings that the current assets are unlawful or the entrant ineligible. [Official rules](https://start-developer-competition-26.devpost.com/rules), [official FAQs](https://start-developer-competition-26.devpost.com/details/faqs).

The next implementation pass should address **carry/captions, release logging and modal settings first**, followed by the second-memory reveal and control discovery. Art polish should then target ownership and reactions rather than adding more scenery. Use the [existing evidence protocol](../testing/gallery-next-evidence-session.md) to decide what actually improved.
