# Story-table verification and finishing pass — November 15, 2026

Windows laptop, Unity 6000.3.25f1, Android build target, Direct3D 11 editor rendering. Base source `e48b1e8` plus the local changes described here. The entrant asked for a competition-quality experience and a full test run. This record separates software results from presentation and device acceptance.

## Baseline and corrections

The first full run passed 440 core tests, 67 service tests, 14 narration-script tests and 43 Unity EditMode tests. Whole-project PlayMode reported 87 passed, 3 failed and 8 opt-in skips. All three failures reproduced in isolation; the passing comparison scenes were not changed.

| Finding and change | Product requirement / module | Judging relevance | Observable check |
|---|---|---|---|
| A single-side ending needed another page to reach its Keep invitation. Shortened the invitation and the suggestion of an unheard side, and removed the blank line between them. Font size and pagination remain unchanged | SK-12, SK-15; `PersonalStoryApp` presentation | Experience Design; Polish & Presentation | Actual app endings across all three interests, either chosen side, the fire's default and both sides fit one standard page. Larger text retains every word and reachable Keep/return actions |
| A test expected a historical citation for the first question, which the November 14 editorial pass deliberately made fictional. It now verifies the fictional answer has no borrowed C6 citation, then asks the documented page and checks its C6 source and return position | SK-05, SK-09; app integration tests | Experience Design; Technical Implementation | Both kinds of answer keep their truth label, source boundaries, answer text and reading position |
| A test required three different historical paragraphs, although journeys and trade now intentionally share the approved trade passage. It now checks approved text/source pairing and distinct openings, perspective labels and opening questions | SK-04, SK-05; app integration tests | Innovation & Creativity; Technical Implementation | Observable differences precede the ending; approved history is not rewritten for cosmetic variety |
| The actual-app capture exposed an implicit localhost service default in the editor. With nothing configured, the welcome advertised online creation and displaced **Use my table**. Removed that default; two consent-view tests now explicitly supply the online availability they exercise | SK-13, SK-14; app configuration and consent view fixtures | Experience Design; Technical Implementation | An unconfigured start exposes the offline welcome and begins a bundled telling. An explicitly configured endpoint still exposes online consent; consent and request-preview tests pass |

No story narration, recording, saved format, scene-builder code or serialized field changed. No scene regeneration was needed. LivingGallery, IllustratedTheatre and Hearth were not regenerated. Unity's dynamic font atlas writes are local editor side effects, separate from the intended source changes.

## Verification

The four focused checks pass across the initial focused run and the ending recheck. A new integration test exercises the actual application-generated ending copy, preventing the view's representative text fixtures from being the sole layout evidence. Core tests pass again: 440/440.

| Suite | Recorded result and scope |
|---|---|
| Core after the ending fix | 440 passed, 0 failed; later changes affect Unity configuration and test fixtures only |
| Full selected table regression after the endpoint correction | App integration: 29 passed, 0 failed, 1 local-service opt-in skipped. View/layout: 13 passed, 2 failed because online fixtures had assumed the removed implicit endpoint |
| Final view/layout and actual-app capture recheck | 16 passed, 0 failed, 0 skipped: all 15 selected view/layout tests plus the app capture. Online fixtures now declare their configuration; captures wait for figure transitions after seeking |
| EditMode after all source/test changes | 43 passed, 0 failed, 0 skipped |

Across the final relevant results, 44 distinct selected PlayMode checks pass and one local-service integration remains skipped. The fixed-cadence motion-study test was deliberately excluded; live-provider HTTP testing was not selected. This is not a new whole-project green run: the baseline report remains 87/3/8, and the changed table suites were rechecked separately. Comparison scenes were not modified or rerun after the focused fixes.

Inspected actual initialized-app captures `20`, `23`, `25` and `26`, including the refreshed second-side capture after its figure had risen. The offline welcome offers **Use my table** and no unconfigured online action. The chosen woman changes foreground position; captions and reconstruction/capability labels remain visible. The both-sides ending and Keep action fit together. Earlier representative view captures are distinct from these app-driven images and contain fixture copy; they are not the submission story.

The images still show a large pale control page and a bright fire competing with the figures. These deserve a moving, seated presentation review; the captures alone do not establish visual polish, comfort or emotional effect. The [revised submission script](../release/access-handoff.md#capture-one-story-well--target-230) depends on actual footage showing both women's stakes clearly, not merely a successful turn animation. No UI redesign or cultural acceptance is inferred from this review.

Local-only evidence:

- Baseline: `artifacts/logs/test-run/core-tests.trx`, `editmode.xml`, `playmode.xml` and `playmode-failures-recheck.xml`.
- Finishing pass: `artifacts/logs/quality-pass/core-tests.trx`, `focused.xml`, `endings.xml`, `story-table.xml`, `actual-app-captures.xml`, `story-table-final.xml`, `view-final.xml`, `editmode.xml` and accompanying logs. The initial actual-app capture failed on the unconfigured-service assertion before any review images; the final capture passes.
- Review images: `artifacts/story-table/`. These are muted desktop renders with synthetic input and specified review cameras, not headset or simulator footage.

## Acceptance still open

The earlier Windows simulator launch rendered the story table with Direct3D 11 after Direct3D 12 showed a black view. That observation establishes launch/display only. It does not establish a complete manual simulator visit, simulated hand success, real hands, room fitting or performance.

Listening, pronunciation/cultural review, newcomer comprehension, Quest hands/reach/room/performance, final APK identity, signing, independent judge access and submission remain open. Tests of synthetic gesture samples do not close those gates. The [November 14 checklist](2026-11-14-unity-verification.md) retains the concrete listening and hardware checks; the [release handoff](../release/access-handoff.md) describes the intended demonstration and access path.
