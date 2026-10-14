# Production iteration — October 14

Implementation response to the [current-experience audit](2026-10-14-current-experience-audit.md). Production quality is the target; software verification, desktop presentation and external acceptance remain distinct. No sound playback, simulator, new narration export or AI music is authorised by this pass.

## Implemented responses

| Requirement / criterion | Owning module | Change | Evidence |
|---|---|---|---|
| SK-06/08, technical implementation | Stage / caption adapter | Selection and replay use a live carry anchor; caption avoidance recognises both, including after height adjustment | Actual selection and replay regression passes at all three text sizes |
| SK-16, experience design | Application diagnostics | Remove journals and per-action cues from logs; retain coarse health diagnostics | Completed no-Keep/Keep/Forget route leaves no logged interaction trail |
| SK-13/15, experience design | Application settings | Preferences pause the visit, suppress competing input, preserve prior/system pauses and handle closing during transition | Two corrected Settings regressions pass; test presses respect real UI debounce |
| SK-02/06/15, experience design | Settings / narration / placement | Forward control row, modal preferences/credits, separate voice/ambience, caption-only timing and persisted reach | Runtime checks cover persisted voice/reach, silent line duration, every credits page, forward controls and separately owned pauses; text/backing depth corrected after capture inspection |
| SK-11, polish/presentation | Illustrated cast / focal art | Register feet, height and palms per pose; rest vessels on the bank until offered; replace native ewe with a painted cutout; temporary neutral acknowledgements return to the authored pose and never replace concern with a smile | Pose/palm and current animal captures/checks pass; UV registrations are approximate presentation measurements, not cultural authentication |
| SK-06/15, experience design | Caption / placement adapters | Protect visible memories, named animal and essential controls; keep selection carry visible at a narrower field of view; normalise placement to the designed seated eye | Eight focused checks pass, including all text sizes, 60-degree carry view and three simulated eye heights; actual Quest/stereo remains unverified |
| SK-05/12, experience design | Content / application / editor entry | First-visit Akan attribution and paged artwork context, precise replay boundary, default gallery build and current contributor/tester instructions | Context is a bundled content asset; old realised plans retain text/audio/outcomes and use current staging |
| SK-04/12/19, innovation and experience design | Authored content | Keep the earlier reversal exclusive to Naserian's perspective; return directly to the new memory with disclosure, agreement and bead handover | 306 core checks pass, including all legal outcomes, old-save replay and new timing/reveal assertions |

The return version does not repeat the introductions, shortfall explanation, promises, departure or closing promotion. It retains the reconstruction disclosure, contextual water line, selected memory, independent agreement, bead invitation and return to the painting. The physical bead still gets its full twelve-second invitation window; shortening does not come from rushing a hand action.

Current v0.7 declared passive return schedules: **123.37 seconds** for Wambui and **135.27 seconds** for Naserian. Both reach their memory after **23.61 seconds** of authored speech/pauses. The memory occupies **30.3% / 29.8%** of those schedules, up from roughly 15–16%. Transitions, questions and visitor/system pauses are excluded; these are not measured participant durations. Original recordings are reused unchanged, with legacy plans and audio retained.

## Visual inspection and provenance

The first accessibility captures exposed header/credits occlusion despite passing state assertions. Text now sits in front of a complete modal backing, and every credits page is checked for fit. Later captures exposed a gourd clipped in the narrower view and captions laid over the miniature. The carry position and focus policy were changed and rechecked through actual selection/replay. Protected controls remain usable; no story outcome is changed by layout.

The animal now uses [ewe-v1.png](../../unity/Assets/Sankofa/Art/IllustratedTheatre/ewe-v1.png), generated through the **built-in OpenAI image_gen tool**, rather than a procedural fleece/leg model. The [exact prompt](../../unity/Assets/Sankofa/Art/IllustratedTheatre/ewe-v1.prompt.txt) and [provenance register](../../unity/Assets/Sankofa/Art/IllustratedTheatre/sources.json) record generation, alpha, hash, provider and the unavailable model identifier. It is a fictional animal, not an authenticated historical breed. The gourd and bead remain tangible geometry. Original/native scenes remain comparison records.

## Verification checkpoint

The first complete functional runtime run passed **43/44** checks; its only failure was the selection carry clipping the narrower view. After the placement/focus correction, **8/8 focused checks** pass, covering that case, memories/named animal/controls, preferences, simulated eye heights and all recenter checks. The earlier final core run passed **306**, editor run **31/31** before the last layout adjustment, and asset integrity currently verifies **31 declared inputs / 92 narration fingerprints** with zero problems.

The complete final runtime recheck, updated editor verification, silent preview and clean-source APK are in progress. Raw reports/captures remain ignored. Desktop/synthetic footage is a review artifact; it does not replace actual Quest or qualifying emulator footage. Preview metadata separates encoded frame rate from actual capture timing. No sound is played or simulator launched.

## External acceptance

No Quest installation, real-hand/stereo/reach test, sustained device profile, listening/pronunciation review, cultural review, applicable-rights confirmation or participant comprehension result has been obtained. An APK, packet or passing synthetic test does not satisfy those gates. Use the [evidence protocol](../testing/gallery-next-evidence-session.md) and [release handoff](../release/access-handoff.md); do not report the product as externally accepted.
