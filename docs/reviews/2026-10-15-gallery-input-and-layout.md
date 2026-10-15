# Simulator selection and gallery context follow-up

October 15, 2026 · Gallery package v0.7.0 · Muted simulator and software verification; actual Quest acceptance remains open

This pass follows the [initial simulator review](2026-10-15-gallery-simulator.md). It resolves the manual controller review workflow and corrects the wall context/control overlap. A fresh launch also exposed late tracking after fallback placement, addressed in `WorldAnchor`.

## Input diagnosis

The existing `PointerSource` and release-to-select gesture were not changed. A read-only local observer showed that click-only native automation supplied a trigger but left the simulated ray pointing elsewhere. Mouse movement delivered by a short native drag updated the ray. A separate press/release then selected the highlighted plaque through the normal controller adapter.

Before the layout change, five manual selections succeeded: Enter painting, Settings, Done, request Return to the gallery, confirm Return to the gallery. No direct panel/flow commands or simulated hand sample overrides were used. The observer recorded Choosing → Story → Paused → Story → Paused → Closing; it did not control those transitions or save a visit.

The working simulator configuration uses Controller sources, Point & click and Compatibility mode on, and ISDK pointer offset off. Meta [documents compatibility mode](https://developers.meta.com/vr/documentation/unreal/xrsim-point-and-click/) for imprecise aiming or apps without submitted depth. This application does not submit depth and does not use the ISDK Ray Interactor. The system-wide OpenXR runtime was not changed. Acquisition of an actual highlighted target must precede the selecting press; sweeping onto it while held intentionally does not select.

## Product corrections

**Wall context:** the Akan title origin, Kenyan setting and reconstruction/review disclosure now form two larger, left-aligned blocks beside the frame. Essential actions remain near the seat. The generator was changed and LivingGallery regenerated through Unity; story data, recordings and the canonical painting were not rewritten. The comparison scenes and generated controller/font cache churn are excluded from this change.

**Late first tracking:** previously, the five-second untracked fallback set `Placed` permanently. A late simulator head pose could then leave the gallery below the seated eye and move the close controls out of view. An initial zero-height fallback now waits for a settled first usable seated position and corrects placement once. Height/reach preferences survive; the initial placement event is not emitted twice. Ordinary head movement after valid placement does not move the stage. No camera movement is introduced.

| Requirement | Owner | Relevant quality criterion | Observable evidence |
|---|---|---|---|
| Legible context alongside accessible choices | Scene builder / gallery presentation | Experience Design; Polish & Presentation | Projected text bounds clear entry and larger return-menu controls and the artwork |
| Seated controls remain reachable after delayed tracking startup | WorldAnchor | Experience Design; Technical Implementation | Late pose recovers the intended eye-relative height once, retaining preferences and a stationary world |
| Deliberate selection rather than accidental sweep | Existing pointer/gesture adapter | Technical Implementation; Experience Design | Manual controller trigger release starts entry and operates modal/return controls |

## Verification and evidence boundaries

The final focused runtime run passes **10/10** checks, including the new late-tracking regression, the wall-context projection matrix, entry waiting, pointer revision/slip cancellation, settings/credits, seated heights and existing recenter/acting checks. The projection matrix covers **108 combinations**: entry and representative expanded return menus × three seated heights × three reach settings × three lenses × two eye offsets. The representative return-menu stress layout is not a newly authored product menu. Its review capture restores the real entry menu.

The first label revision failed the closest-reach/FOV edge check; the labels were narrowed and moved before the passing run. The corrected layout passed a nine-check run before adding the independently discovered late-tracking regression. The final ten-check run includes both fixes. Earlier failed reports remain local; these counts are not participant or hardware tests. A separate final entry-capture check also passed before the placement recovery change.

A fresh actual simulator launch then recorded the initial stage at `(0,0,0)` and its automatic recovery to `(0,0.55,0)` with the head at `(0,1.70,0)`. No manual recenter was issued. The corrected wall labels and nearby controls were inspected in the simulator, and another native trigger press/release selected **Naserian · their earlier meeting**, changing Choosing to Story through `PointerSource`. This final observation uses a read-only observer, not scripted application commands. The docked Unity game view still crops the lower view; the simulator frontend displays the controls, and device stereo/reach acceptance remains open.

All **31 final Unity EditMode checks pass**, zero skipped/failed, editor exit 0, after the placement change and removal of the observer. Results: `artifacts/tests/gallery-recovery-editmode-final.xml`; corresponding logs are under `artifacts/logs/`. Source whitespace and the workspace/link/exclusion checks also pass.

The preceding 306 core and 46 complete functional runtime results remain recorded in the [October 14 production iteration](2026-10-14-production-iteration.md); this pass does not present them as fresh full-suite results. No engine-free flow/story/gesture data changed. Asset integrity still verifies 31 declared inputs and 92 speech fingerprints with zero problems. This pass does not involve new provider exports, art generation, music or sound playback.

Local evidence includes `artifacts/simulator-input-fix/` (manual trace and read-only observer), `artifacts/simulator-input-fix-final/` (the launch that exposed low placement), `artifacts/simulator-recovery-verified/` (fresh recovered-placement observation), `artifacts/living-gallery/wall-context-clear.png`, and `artifacts/tests/gallery-placement-recovery-final.xml`. These raw artifacts and input traces are ignored, removed from Assets after use and not packaged.

Simulator controller evidence does not verify real hands, complete hands-only use, physical reach, stereo comfort or Quest thermal/frame-rate performance. Cultural, pronunciation/listening, applicable rights, account and judge-access acceptance remain separate release gates.

## Updated review APK

Built from clean source **5f0d2ce**, `com.sankofa.quest`, version `0.1.0+5f0d2ce`, code 1, ARM64/IL2CPP. Build succeeded with zero errors and one reported optional OpenXR input-polling warning. File `artifacts/Sankofa-LivingGallery.apk`: **84,142,748 bytes**, SHA-256 `6331e36a74bbabf808ce33806cca4f2de22217c8769a1cbc6246aa149995d33f`. The existing guard verified captured development credentials absent and restored editor settings/version. Archive-name inspection found no private-reference/LocalOnly/credential filename matches; this is not a complete arbitrary-data audit. Source was clean after build.

Identity and retained acceptance gates are in the [candidate record](../release/candidate.md). The old APK/identity are archived locally; no headset installation, channel upload or submission occurred. Existing October 14 preview footage was not regenerated or passed off as footage of this APK.
