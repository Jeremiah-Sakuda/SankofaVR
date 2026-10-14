# Independent agent judging panel — Sankofa living gallery

October 13, 2026 · Repository reviewed: `b3fa0d6` · APK source: `d0a17bd` · `LivingGallery`, authored package v0.4.0

## Assessment

The panel sees a coherent Entertainment concept with a substantive embodied interaction, but a prototype whose dramatic performance, focal assets and platform evidence have not caught up with its premise. The strongest mechanism is the gourd crossing from a painted woman's palm into the visitor's reach, followed by hospitality that reflects actual participation. The gallery provides a recognizable beginning and return. Those are worth protecting.

The three reviewers' overall editorial ratings fall between 6.2 and 6.5 out of ten, with an aggregate of about 6.4. These are diagnostic opinions of the current evidence, not official scores, competitor rankings or win probabilities. They do not rate hypothetical perfect execution. Hardware and audio unknowns make the numbers especially provisional.

## Method and official calibration

Three fresh-context agents received the same factual scope, artifact identity, evidence limits and official source URLs. They reviewed independently without sharing findings or reading previous panel reports. Each evaluated all four criteria, with a different emphasis: visitor experience, XR engineering, or story/art. Repository instructions required access to the local competition brief, which contains the developer's ambitions; reviewers were explicitly instructed to exclude those ambitions and strategy from scoring evidence. This was not a double-blind process, a human judging panel, a community review or a user study.

The official criteria each carry 25%: Innovation & Creativity, Experience Design, Technical Implementation, and Polish & Presentation. Full-VR work should be evaluated on its relevant capabilities; room-understanding requirements are conditional on passthrough use. The rubric expects Quest performance, including its 60 fps minimum, and coherent art, sound, interaction and honest demonstration. [Official judging](https://start-developer-competition-26.devpost.com/details/judging)

Stage One checks baseline viability; Stage Two scores the criteria. Hands-first completion is required, while seated use and short satisfying visits are listed as guidelines. Engine submissions need Competition-channel access and the required public demonstration; the rules allow Quest or emulator footage. Missing headset footage alone is not an automatic Stage-One rejection. Eligibility, rights, distribution and actual operation remain separate unresolved readiness matters. [Official rules, §§4–6](https://start-developer-competition-26.devpost.com/rules)

Reviewers read current code/content and existing test/build records, and personally viewed the five current gallery, preparation, remembered-meeting, Keep and modern-river stills. No runtime, simulator, new test/build, sound playback, external contact or product change was performed during judging. Audio files and their durations were inspected only as data. The modern still is a staged composition check with the application disabled, not proof of the complete playable coda. Other stills use synthetic input and desktop rendering, not actual hands or stereo observation.

## Scores

The /10 scale is an editorial convention introduced for this panel; it is not a published official scale. Each review's mean and the panel aggregate use equal weighting.

| Criterion | Experience reviewer | Technical reviewer | Editorial reviewer | Panel mean |
|---|---:|---:|---:|---:|
| Innovation & Creativity | 7.2 | 7.0 | 7.0 | 7.1 |
| Experience Design | 6.8 | 6.5 | 6.8 | 6.7 |
| Technical Implementation | 5.7 | 5.5 | 6.4 | 5.9 |
| Polish & Presentation | 6.2 | 5.8 | 5.8 | 5.9 |
| Equal-weight overall | 6.5 | 6.2 | 6.5 | **6.4** |

Confidence is moderate for code, script and still-image observations, and low for delivered headset experience. The technical reviewer rates platform readiness more cautiously and identified adapter/verification defects the other reviews did not isolate. The editorial reviewer gives more credit to the deterministic architecture, while keeping hardware reliability unaccepted. Their scores should not be treated as interchangeable measurements.

## Findings shared across the panel

### Preserve the embodied exchange and the women's agency

The painted-to-physical hand-off is more convincing as a VR-specific argument than the gallery metaphor by itself. The visitor can participate in a material exchange; the performance/address and later recall reflect what happened. The quiet path receives equal hospitality. The women make their own agreement, so the guest does not become the person who decides their trust.

The two routes contain different revelations before the ending. Saving and replay preserve outcomes rather than inventing participation. The offline authored core provides a complete experience without relying on a live service. Absence of runtime AI is not intrinsically a rubric defect for this Entertainment candidate.

### Let the memories perform their information

The preparation window explains two portions through objects and labels. The remembered window explains waiting through reused standing figures and narrated text. Both are understandable tableaux, but little happens inside them. They can carry nearly the same information on a flat interactive screen.

The shortage and reciprocal patience need a visible action or reaction: division, hesitation, an unfinished offer, or waiting that can be perceived before a concluding explanation. The source does not need an outsider-controlled ending to gain drama. Preserve the independent agreement and make its emotional preparation more legible.

### Bring the focal assets to one standard

The detailed painted faces, fabric and environment coexist with a schematic sheep, simple gourds/portion vessels and a blocky modern gate. The women's supplied expressions remain warm during the tense material. These are observed differences in the current images, not proof of historically incorrect clothing or a verdict about unheard narration.

The highest-value art work is on the few objects and reactions the visitor looks at closely. Extra scenery does not resolve the discrepancy. Cultural/material review and a later authorized listening pass remain separate acceptance tasks.

### Strengthen entry, return and the next-visit offer

Enter now has a clear hierarchy and records an authored default truthfully. However, the experience reviewer estimates the first story gesture begins around 40–50 seconds after Enter, excluding the entry selection itself. The shared emotional agreement comes later than five minutes on the passive script estimate. Those are calculations from content/durations and invitations, not measured user sessions.

The return still competes with captions, instructions, options and a small placard. Give the painting a quiet recognition beat and stage the next actions in a clearer sequence. The reviewers consider the alternate route and coda honest but finite return reasons. The editorial/experience analyses estimate roughly 37–40 seconds of route-specific vignette material within a largely shared eight-minute telling. Give a returning visitor a specific unanswered interest and a faster path to that new content, while keeping exact replay distinct.

### The overlooked-stories mission is partly earned

The editorial reviewer credits ordinary women's work, scarcity, promises and reciprocity as meaningful subjects that history can overlook. The imagined encounter is not recovered testimony. The current disclosure is therefore essential. Deepen the individuality of the two fictional women and their specific actions without inventing generalized cultural customs. Keep documented context and imagined scene detail distinguishable.

## Specific findings checked against source

These findings are stronger than general impressions, but remain static/source-level observations unless noted otherwise. They were not repaired during this review.

| Finding | Evidence | Consequence and next verification |
|---|---|---|
| Off-button pinch release can dereference a null panel | [PointerSource](../../unity/Assets/Sankofa/Runtime/Input/PointerSource.cs), line 61, discards the key returned by `PointerSelect.Update` and presses the currently hit target. [PointerSelect](../../unity/Assets/Sankofa/Interaction/PointerSelect.cs), lines 65–69, can retain an armed key through a briefly null hover; [selector test](../../dotnet/Sankofa.Tests/InteractionTests.cs), line 295, explicitly expects that selection. | Pinch on a button, move just off all faces, release inside 0.15-second grace while tracked: the selector can return a key while the current panel is null. Resolve the returned armed target only while still valid, or deliberately cancel; test the Unity adapter, including hidden/replaced controls. This is a statically established null-reference path, not a reproduced app-process crash. |
| The reduced-motion test does not establish its claimed mode | [gallery test](../../unity/Assets/Sankofa/Tests/PlayMode/LivingGalleryTests.cs), line 95, sets the transition property directly; [application](../../unity/Assets/Sankofa/Runtime/App/SankofaApp.cs), line 382, overwrites it from saved settings. Camera stationarity also passes in full mode. | Correct earlier coverage wording: passive stationary flow is tested; authoritative reduced-motion selection is not reliably demonstrated by this test. Set/assert the actual preference and verify no scale expansion. This is a test-evidence defect, not proof the feature is broken. |
| Both parent continuations are not covered by the Unity route loop | [gallery test](../../unity/Assets/Sankofa/Tests/PlayMode/LivingGalleryTests.cs), lines 67–89, calls Follow once after keeping the second route. [core tests](../../dotnet/Sankofa.Tests/LivingGalleryStoryTests.cs) do compose/follow both parents. | Historical routes have runtime coverage; both parents have core coverage; the gourd-parent coda needs its own Unity flow. Do not claim two-parent Unity coverage from the loop. |
| Physical Keep does not disclose replacing the previous saved visit | [GalleryKeep](../../unity/Assets/Sankofa/Runtime/Stage/GalleryKeep.cs), line 33, always describes keeping this visit; [flow](../../unity/Assets/Sankofa/Flow/VisitFlow.cs), lines 421–438, replaces the active shelf and deletes the earlier record. The button can say “Keep this instead.” | Make replacement visible before the physical commit as well as on the button path; verify retaining an older visit, entering another thread, and using the gesture. Failure/retry truthfulness is already tested, but replacement consent is a different issue. |

The coordinator independently checked the pointer, reduced-motion, continuation and replacement paths after receiving the reports. Additional technical concerns—stale fingertip UI, late transition/watchdog races, device FoV/reach and thermal rendering—are proposed tests, not observed failures.

## Recommended order

1. **Repair the pointer adapter, strengthen the two named coverage gaps, and disclose physical replacement.** These are specific, bounded reliability/consent improvements. Success means no null-reference or stale selection on off-button release; an asserted reduced-motion transition; both parent codas through runtime; and visible replacement intent on both save paths.
2. **Establish a Quest and first-time-user baseline for the existing encounter.** Observe controller-free entry through exit, one hand/passive participation, asking versus receiving, flour-to-milk recognition, Keep/relaunch/replay/forget, pause/recenter and sustained rendering. Record exact build/device and actual failures. Software tests cannot answer these questions.
3. **Make one memory and its main-scene reaction dramatically readable.** Prefer a clear action/reaction to a larger collection. Unfamiliar viewers should explain each woman's need and the changed hospitality without treating the visitor as the author of their bargain.
4. **Unify the focal props and simplify the return hierarchy.** Revise the gourd, sheep, bead and gate to belong to the painting; protect faces and the canonical return from competing explanation layers. Obtain cultural and later listening feedback before final asset/voice production.
5. **Reduce repeat setup and demonstrate the actual embodied payoff.** Give the other thread a concrete, spoiler-safe promise, then show actual frame/depth/hand crossing/consequence/return in an honest short demo. Keep external access, eligibility, terms and release evidence separate from creative scores.

This sequence separates inexpensive software corrections from changes that need sensory or human evidence. It is a proposed response to the panel, not a new PRD, accepted cultural review, release approval or automatic instruction to implement.

## Detailed independent notes

The full reviewer notes are retained locally in the ignored `artifacts/judging/` directory: `2026-10-13-experience.md`, `2026-10-13-technical.md`, and `2026-10-13-editorial.md`. They include individual scores, evidence, confidence and ranked suggestions. They are not added to the published runtime or used as official organiser decisions.
