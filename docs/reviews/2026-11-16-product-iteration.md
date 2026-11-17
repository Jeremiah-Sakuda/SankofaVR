# Product iteration — November 16, 2026

The entrant asked for product iteration against the delegated reviewers' feedback before submission work. This record covers the selected illustrated river treatment. It is an agent review and software verification record, not official judging, participant research, listening acceptance or Quest evidence. The entrant separately authorised an audible preview; the captures below remain silent.

## Scope and evidence

The required local competition context was read and the competition workspace preflight passed for 2,073 tracked files. The reviewers were given the same reading requirement. The four equally weighted criteria remain those in the [official judging guidance](https://start-developer-competition-26.devpost.com/details/judging). The [prior review](2026-11-15-independent-judge-review.md) remains intact.

Affected requirements are SK-01/06 (welcome, narration and captions), SK-02/08 (hand choices), SK-07/11 (spatial and visual coherence), SK-12/13 (explicit saving, replay and recovery), SK-15 (seated controls) and SK-18 (accurate evidence). Work is confined to Unity presentation and its connection to the accepted story session, plus the two water choice labels and generated service fixture. Historical passages, story routes and recorded speech remain unchanged.

Only `RiverThresholdPainted` is regenerated through `BuildRiverPaintedCandidate`. StoryTable, the earlier river prototype, the solid-character variant and the earlier comparison scenes retain their saved scene assets. Shared runtime fixes still apply where those components are used. StoryTable remains the release target until a separate candidate decision is recorded.

## Review and iteration

Fresh baseline scores were 72/100 for experience, 72 for presentation and 71 for engineering. The main findings were the large foreground reading panel, miniature-stage character movement carried into a surrounding landscape, invisible perspective actions, a keepsake hidden with the table, and saved water stories reopening in the table presentation.

| Change | Observable evidence sought | Tradeoff |
|---|---|---|
| Stationary painted portraits with a short pose dissolve and accepted-perspective emphasis | Both perspective handoffs retain the characters' places and authored reveal boundaries | Illustrated figures remain deliberately limited in viewpoint; no claim of fully modelled characters |
| Choices beside the actual river figures, plus caption alternatives | One accepted pinch chooses the labelled woman; pause and modal changes cancel stale selections | Duplicate choices retain an accessible near-field alternative |
| Smaller caption backing and one fixed utility dock | People, their feet and the river stay visible; Context, Pause, Options and Leave remain in consistent positions | More caption parts; sentence boundaries must preserve the ending |
| Context holds the same narration position | Return resumes the exact clip position with matching caption and stage, including nested source/answer views | Local presentation checkpoint only; no provider or story authority changes |
| Visible connected strand after successful Keep | A real successful save shows the correct coloured beads; failure and an older saved story do not count | Six neutral/coloured slots remain a small reward, not a new inventory system |
| Exact saved water replay returns to the river | Reopen preserves realised JSON and performs no generation | Other tellings retain their table staging |
| Three complete existing welcome recordings | Spoken text matches captions and each recording fits its allotted window | The normal preview excludes the unverified provisional music; no new music added |

Iteration 1 built successfully. Its PlayMode partition had **62 passes, 4 failures and 3 optional skips out of 69 tests**. The complete main-path capture passed, including both figure choices, Keep and exact riverside replay. The four failures were the new perspective fixture using an untracked camera at floor level; the fixture now supplies an explicit 1.15 m seated pose while preserving actual ray-hit and reach checks.

Iteration 2 had **62 passes, 4 failures and 3 optional skips out of 69 tests**. The four perspective checks passed. Failures were a stale ending-label expectation, two tests still using the replaced table-return control (one initial skip had also been changed incorrectly), and the capture using `return` instead of the source panel's actual `continue` action. These are retained as failed runs, not counted as completed checks.

Iteration 2's silent desktop movie reaches both perspectives and successful Keep at 177.75 seconds; it stops during the source-panel return and **does not demonstrate replay**. Reviewers inspected the actual stills and sampled transitions. The cleaned dock, stable portraits and connected strand improved visibly. They also found a four-line orphan in the opening title and a five-word tail at the traveller's ending. Those findings prompted a newline correction and sentence-aware pagination. The experience reviewer scored this incomplete pass **79/100 (20/21/17/21)**, up from 72; final scores are pending the corrected capture.

The engineering review found a separate active-narration problem: Context stopped the voice, then Return restarted it beneath a later caption. Muted visual tests could not establish that path. A focused active-audio-clock regression, with listener output muted, is required for its fix. Switching from narration to captions must also release the narration-owned stage clock without rewinding it.

## Verification status

- Core: **445 tests passed**, including a further run after the recovery changes; exact recorded return invitations are kept independent of the shorter button wording.
- Python service: **67 tests passed**, including regenerated authored-telling fixture parity. This is the backend suite, not a claim that all repository Python checks ran.
- Narration tools: **14 offline tests passed**. The asset audit checked **31 declared inputs and 92 speech lines with zero integrity problems**; this checks declarations and files, not independent rights clearance or listening quality.
- Scene generation: iteration 2 and the final revised scene regenerated successfully after a test-assembly dependency correction. The new Editor-only simulator recording helper compiled with the final scene.
- Focused third pass: **21 passed, 1 failed, 1 optional skip**. All four initial active-audio recovery checks, the complete final-sentence check, exact intro clip-window check and later-caption table-return check passed. The remaining sweep fixture started before the stage finished rising after its modal; the real standing gate was retained and awaited.
- Final recovery partition: **10/10 passed**, including six active-narration-clock checks with listener output muted and four keepsake/sweep checks. This includes paused source return without cancellation, repeated comfort changes while paused, exact clip-position resume, completion after resume, captions-only continuation, new-visit invalidation, explicit-save receipt survival, failure/replacement and actual visible-stage sweep gating.
- Full revised capture, EditMode checks, actual audible simulator preview and candidate decision: pending at this revision.

Local evidence is under `artifacts/logs/2026-11-16-product/`, `artifacts/product-iteration-1/` and `artifacts/product-iteration-2/`. Movies use actual Unity mono desktop rendering and synthetic pointer input at a fixed 24 fps simulation clock. That cadence is not measured performance. These are not automatically qualifying XR Simulator footage.

No Quest is available. Physical hand reliability, reach, comfort, device frame time, unfamiliar-room fitting, listening quality, cultural/pronunciation review and participant emotional response remain separate open evidence. No score predicts a prize. The [delivery plan](../product/delivery-plan.md) continues to track distribution and submission work.
