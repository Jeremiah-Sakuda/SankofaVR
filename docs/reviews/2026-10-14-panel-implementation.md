# Judging-panel implementation pass

Response to the [independent panel](2026-10-13-independent-gallery-panel.md). Product scope remains one living-gallery exhibit with authored English narration, two perspectives, and a modern continuation. This pass implements bounded improvements; it does not claim official scores, hardware acceptance or cultural review.

## What was folded in

| Panel finding | Implemented response | Observable evidence |
|---|---|---|
| Off-button pinch can press a null target | Resolve the selector's returned panel/button **revision**. Hidden, changed and inactive panels cannot receive that press. Last-known hand poses cannot start fingertip/pointer selection. | Unity adapter exercises off-button release, hidden controls and a changed label with the same ID. Existing selector/hand tests remain. |
| Physical Keep hides replacement | Guidance names the previous visit and its removal. First dwell/close confirms replacement intent; a second open/dwell/close commits. Pause/exit resets confirmation. Failed writes retain the prior save and carried object. | Runtime replacement test retains the previous record after the first gesture, then saves the new shorter route only after the second. Existing failure/retry check remains. |
| Reduced-motion coverage bypasses settings | Select reduced motion through the real Settings panel and assert the persisted preference, transition mode and zero canvas expansion throughout the passive visit. Tests restore the previous preferences. | Corrected runtime test, rather than direct assignment alone. |
| One continuation parent missing in Unity | Follow the modern continuation inside **each** historical route iteration. Assert parent ID and continuation route. | Both parents complete through runtime; existing exhaustive core outcome paths remain. |
| Late transition could swap scenes after watchdog | Core emits recovery before subsequent cues. The adapter cancels the old coroutine and settles the requested scene/frame. | Core timeout test and runtime cancellation/late-swap check. |
| Constant smiles flatten the shortfall | New sibling atlas adds concerned/listening/waiting poses. Authored narration cues perform the admission and remembered promise; resolved invitations still own visitor consequences. | Shortfall capture and pose test; original atlas and gift poses retained. |
| Memories are static diagrams | Wambui appears in the provisions miniature as flour separates and the travelling portion diminishes. Naserian approaches a stationary Wambui in the earlier meeting, then lifts her face to listen. | Narration bindings, transform/pose checks, pause and reduced-mode tests, and updated stills. These remain miniature theatre, not rigged performance. |
| First action arrives too late | Move the existing context/disclosure recordings after the ember invitation, still before entering the spatial story. | Authored timing places the offer action about 11.7 seconds after entry; complete invitation line ends around 21.8 seconds. Actual novice performance is unmeasured. |
| Other perspective repeats the whole visit | Returning menu names the unvisited memory and composes a shorter authored telling. It skips introductory water/flour/milk gestures, keeps the women’s independent agreement and bead invitation, and retains the original full/replay paths. | Four core short-visit cases cover both routes and accepted/passive bead paths, save/replay and continuation recall. Skipped invitations produce no outcomes or false recollections. |
| Props feel schematic | Gourds gain a curved shoulder, open rim, inset interior and cord in a combined mesh. The gate gains weathered vertex pigment. Existing painterly shading, sheep face and pierced bead remain. | Current captures; no extra renderer for these prop details. Sheep/bead acceptance and caption/object overlap during handling remain focal-art/reach review items. |
| Return layers compete | Closing choices wait until the final line finishes; the final caption then clears. Keep guidance remains beside the recess. | Runtime/capture checks. Actual headset field of view and reading comfort remain untested. |
| Art register lacks provider clarity | Record OpenAI/Codex tool, exact prompt/reference/hash, public terms link and the fact that the model identifier is not exposed. | [Source register](../../unity/Assets/Sankofa/Art/IllustratedTheatre/sources.json). Applicable contract and release clearance remain open. |

No new story words were sent to a narration provider. Existing approved recordings are reordered/reused. One new expression atlas was generated through the built-in image tool; it remains a fictional theatrical/cultural-review draft. No AI music was introduced.

## Repeat pacing

Declared passive schedules, including line pauses and invitation waits but excluding transitions, questions and visitor/system pauses:

| Perspective | Full encounter | Focused memory revisit |
|---|---:|---:|
| Before her journey | 499.04 s (8.32 min) | 249.18 s (4.15 min) |
| Their earlier meeting | 501.94 s (8.37 min) | 252.08 s (4.20 min) |

These are authored schedule calculations, not measured user session durations. Short visits preserve stable route IDs but save their own realised beat list. A modern continuation recalls only the invitation outcomes present in that saved visit. Old saved plans still replay without inserting the new script.

## What remains a real gate

Use the [next evidence session](../testing/gallery-next-evidence-session.md) for first-time viewers, cultural reviewers and actual Quest checks. No headset, novice comprehension, stereo comfort, hand-tracking reliability, thermal performance, listening/pronunciation, applicable art contract, eligibility decision, release upload or organiser acceptance was established by this pass. Additional exhibits, live AI, MR anchoring, full rigs and extra languages were not added: they do not resolve the panel’s immediate weaknesses.

## Verification record

Final verification includes **301 .NET**, **29/29 Unity editor** and **38/38 Unity runtime** checks; the material-only final capture and build identity are recorded in the [decision log](../decisions.md). Early attempts exposed a builder type error, a test method-name error, an invalid shortened ember withdrawal, an unwired memory list, a test-only wrong sheep ID and cross-test error accumulation. These were repaired; early failed reports remain local artifacts and are not counted as passing evidence. The comparison-scene wiring test now explicitly permits the optional gallery performance adapter, while the gallery-specific test requires its cues and both memory bindings.
