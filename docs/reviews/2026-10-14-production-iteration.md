# Production iteration — October 14

Implementation response to the [current-experience audit](2026-10-14-current-experience-audit.md). Production quality is the target; software verification, desktop presentation and external acceptance remain distinct. No sound playback, simulator, new narration export or AI music is authorised by this pass.

## Implemented and verified so far

| Requirement / criterion | Owning module | Change | Evidence |
|---|---|---|---|
| SK-06/08, technical implementation | Stage / caption adapter | Selection and replay use a live carry anchor; caption avoidance recognises both, including after height adjustment | Actual selection and replay regression passes at all three text sizes |
| SK-16, experience design | Application diagnostics | Remove journals and per-action cues from logs; retain coarse health diagnostics | Completed no-Keep/Keep/Forget route leaves no logged interaction trail |
| SK-13/15, experience design | Application settings | Preferences pause the visit, suppress competing input, preserve prior/system pauses and handle closing during transition | Two corrected Settings regressions pass; test presses respect real UI debounce |
| SK-02/06/15, experience design | Settings / narration / placement | Forward control row, modal preferences/credits, separate voice/ambience, caption-only timing and persisted reach | Two accessibility regressions pass; visual inspection exposed text occlusion, which is undergoing another pass |
| SK-04/12/19, innovation and experience design | Authored content | Keep the earlier reversal exclusive to Naserian's perspective; return directly to the new memory with disclosure, agreement and bead handover | 306 core checks pass, including all legal outcomes, old-save replay and new timing/reveal assertions |

The return version does not repeat the introductions, shortfall explanation, promises, departure or closing promotion. It retains the reconstruction disclosure, contextual water line, selected memory, independent agreement, bead invitation and return to the painting. The physical bead still gets its full twelve-second invitation window; shortening does not come from rushing a hand action.

Current v0.7 declared passive return schedules: **126.37 seconds** for Wambui and **135.27 seconds** for Naserian. Both reach their memory after **23.61 seconds** of authored speech/pauses. The memory occupies **29.6% / 29.8%** of those schedules, up from roughly 15–16%. Transitions, questions and visitor/system pauses are excluded; these are not measured participant durations. Original recordings are reused unchanged, with legacy plans and audio retained.

## Remaining verification in this pass

Calibrate poses/prop ownership and focal materials, complete Settings text visual checks, correct replay/default contributor entry points, refresh current captures/reviewer packets/demo and rebuild the APK. Run the final core, editor and complete runtime suites after the final changes. Record actual results here before merging.

## External acceptance

No Quest installation, real-hand/stereo/reach test, sustained device profile, listening/pronunciation review, cultural review, applicable-rights confirmation or participant comprehension result has been obtained. An APK, packet or passing synthetic test does not satisfy those gates. Use the [evidence protocol](../testing/gallery-next-evidence-session.md) and [release handoff](../release/access-handoff.md); do not report the product as externally accepted.
