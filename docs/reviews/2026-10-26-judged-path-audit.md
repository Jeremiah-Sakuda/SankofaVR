# Judged-path audit and iteration

October 26, 2026 · 23 days before the entry deadline · Software iteration; no device, listening, participant or official score

The user asked for an audit of the implementation and its assumptions, then an iteration toward a winning entry. Nothing can make a judged competition certain. What this pass can do is remove the reasons the current build would lose and spend effort only where the judges will actually look. The official [rules and judging criteria](https://start-developer-competition-26.devpost.com/rules) were re-read from the user's pasted copy of the live page; the separate FAQ and special-awards pages were not reachable from this session.

## The finding that matters most

**Judges score the APK they install on a Quest 3 or 3S, with hands and no controller, and most likely with no story service.** The checked-in scene has no service endpoint, so everything that depends on online generation is invisible to them unless a hosted service is deliberately configured. That path is the product for judging purposes.

On that path, the October 19 build presents a quiet, two-dimensional reading interface. The table pivot kept the original web product's strengths, personal input and truthful labels, and set aside most of what was spatial in the earlier Quest work: the painting that opened around the visitor, objects crossing from a painted palm into the hand, and the fire as a presence. A judge would very likely pass it at Stage One and score it low on Innovation and on Polish.

### What a judge saw in the October 19 build

| Time | What happens | Criterion at risk |
|---|---|---|
| 0:00 | A floating journal and a paper deck with 13 dark buttons, in passthrough. No voice or ambient sound: narration defaulted off on every platform and the table had no sound source | Polish (sound design), first five minutes |
| 0:20 | The most inviting button, **Create my story**, opens a consent page and then *"The online storyteller is not connected"* | Experience, Technical (reads as broken) |
| 1:00 | **Read prepared story**: three pages of about 100 words each, one flat illustration, no decision to make | Innovation, Experience |
| 3:00 | Ending: Keep or Another question. A typed question chose one of three fixed tellings, without saying so | Experience ("clarity of influence") |

### My reading by criterion

These are one reviewer's estimates for the judged offline path, on a 10-point scale. They are not predictions, official scores or measurements; the field is unknown and no device evidence exists.

| Criterion (25% each) | Oct 19 build | After this iteration, if Unity checks pass and the new chapters are recorded | Ceiling with the next steps below |
|---|---|---|---|
| Innovation & Creativity | 4–5 | 5–6 | 6–7.5 with the pop-up illustration and a strong video |
| Experience Design | 4–5 | 6–7 | 7–8 after an observed device session |
| Technical Implementation | 5–6, unknown device behaviour | 5–6 | 6–7.5 with measured 60 fps and a clean first launch |
| Polish & Presentation | 4–5 | 5–6 | 7 with depth, sound review and honest footage |

## Assumptions audit

| Assumption | Finding | Disposition |
|---|---|---|
| Personal input is the visible differentiator | Offline, a question selects one of three fixed tellings by keywords, silently | Partly fixed: the first page now says which words changed the thread. Personalisation offline remains a selection, honestly described |
| Judges will see online Gemini generation | Only with a hosted HTTPS service and an access token in the APK, available until December 11 | **User decision.** Recommended: optimise for offline; host only if it can be kept reliable and its retention terms are reviewed |
| Muted, caption-first is the right default | Correct for verification; wrong for a storytelling entry judged partly on sound design | Fixed for the headset: the voice reads by default, the fire is heard and breathes with the voice. Editor and batch runs stay silent |
| Character autonomy rules out visitor agency | Choosing *whose side to follow* changes what is revealed, never what a character decides | Implemented in all three tellings |
| A kept telling is a reason to come back | Keep preserves content; it does not create curiosity | The ending now names the side the visitor has not heard |
| Table fitting makes passthrough purposeful | It changes the deck's height and scale; tables under 0.9 × 0.55 m fall back to the stand; judges' rooms are unknown | Keep as a benefit, not the headline. Device test in at least two rooms |
| Hands-first means every action is a button | Meets the hard requirement but does not use hands expressively; typing on a virtual keyboard is the weakest input in VR | Kept the keyboard optional; a choice now needs one touch. Further work belongs to the illustration itself (below) |
| More tests impress technical judges | Judges see frame rate, first launch and reliability, not test counts | Tests guard behaviour; the device session is the real technical evidence |
| No headset is acceptable | Footage may come from the simulator, but judges install on Quest and the criterion names 60 fps on Quest hardware | **Highest remaining risk.** Borrow or buy a Quest 3S for at least two sessions before November 13 |
| The Adapted division claim is secure | The rules list "hand interactions as a new feature" and "an MR interaction mode as a new feature" as significant updates; the web original had neither | Name both explicitly in the Adapted summary |
| Visible review notes signal honesty | The C1 page tells every visitor that "spelling and pronunciation still need review by a Maa speaker" | **User decision.** The approved text, recording and validator are unchanged; consider moving the review note to *About this story* once a Maa speaker is consulted |
| "Imagined scene:" belongs in every caption | The same disclosure already labels the illustration directly above the caption | Unchanged: a runtime test pins the verbatim caption and the narration speaks it. Worth a deliberate decision |

## Changes in this iteration

| Change | Requirement | Criterion | Evidence |
|---|---|---|---|
| **Whose side will you follow?** Each telling reaches its documented page and offers two perspectives. Turning the page without choosing lets the fire choose, and the ending says which happened. Three new chapters tell the other side; the nine existing chapters are byte-identical, so their recordings still play | SK-04, SK-08, SK-12 | Experience, Innovation | Core route, validator, JSON and session tests; new PlayMode tests (not run) |
| **What your question changed.** The first page names the recognised words and the thread they chose, or says the words did not point to one. It quotes only catalogue vocabulary, never visitor text, and is not saved | SK-04 acceptance: "a visitor can explain what their input changed" | Experience | Core tests, including that a surname and sentence are never echoed |
| **Voice and fire on the headset.** Prepared narration plays by default beside the captions; the fire's flame follows the voice; a small-fire field recording (CC BY 3.0, credited in the app) plays at the fire. Editor and batch sessions remain silent | SK-06, SK-11 | Polish (sound design) | Type-checked only; listening review required |
| **Offline-first intake.** Without a configured service, Create my story is not shown; the start row becomes Begin, Add my question and How it works. A configured endpoint restores the explicit online path | SK-14, SK-02 | Experience, first five minutes | PlayMode tests (not run) |
| **Narration stays in step with the text.** A test ties every bundled chapter to a manifest entry and its recording hash; new chapters are listed as pending. `scripts/generate_table_narration.py` records them in the existing voice | SK-11, SK-18 | Polish, Technical | Core and script tests |

Not changed: the comparison scenes, the shared `PokePanel`, the backend (whose own linear bundled fallback already differed from the app's tellings before this pass) and any approved historical text.

## Verification actually performed

- **.NET core: 358/358** on a locally installed .NET 8 SDK against the netstandard2.1 libraries; CI runs the same suite on .NET 10.
- **Python service: 32/32**; narration script tests **9/9**, without provider calls.
- **Unity runtime and PlayMode test files were type-checked against hand-written stubs** of the Unity, TextMeshPro and Meta members they use. This catches naming and type errors in the changed code. It is not a Unity compile: **EditMode and PlayMode suites were not run, the scene was not regenerated, no capture or APK was made, and no sound was played.**

## What the user needs to run

1. Regenerate the scene with **Sankofa → Build Personal Story Table** (it now gives the app its fire recording), then run the EditMode and PlayMode suites from the [Unity guide](../../unity/README.md).
2. Record the three new chapters: `python3 scripts/generate_table_narration.py`, then rerun `dotnet test dotnet/Sankofa.Tests`.
3. Listen to all twelve chapters and the fire together on headphones before shipping voice-on by default.
4. Build the APK and put it on a Quest.

## Critical path to November 16

| Order | Work | Why it decides the outcome |
|---|---|---|
| 1 | **Quest access and a first hands-only session** (first launch, permissions, passthrough, readability, OVR Metrics frame rate) | A broken first minute or sub-60 fps loses Technical and Experience regardless of content |
| 2 | Competition release channel, Invite URL and an install from a second account | Judges cannot score what they cannot launch |
| 3 | **Pop-up illustration**: implemented in software (below); needs Unity tests, captures and a device look | The largest remaining Innovation and Polish gain, and the strongest shot in the video |
| 4 | Decide online creation: offline-only, or a hosted service kept up to December 11 | Determines what the video and copy may claim |
| 5 | Declutter the intake: the single-option place and period buttons become one line of text | Fewer buttons in the first view; clearer onboarding |
| 6 | One Kenyan reader and one Maa speaker; record what they change | Cultural specificity is the product's claim |
| 7 | Video under three minutes from real capture: the fire's welcome, a typed question, the choice, the other side | The rules ask to lead with the story and what is distinct |
| 8 | Eligibility, Start membership and division confirmed by the user | Stage One |

Suggested rhythm: week of October 26, Unity checks, recordings, listening and the first device session; week of November 2, pop-up illustration, intake cleanup and a second device session in another room; week of November 9, freeze by November 13, capture, copy, second-account install, submit November 16, leaving November 17–18 in reserve.

## Pop-up illustration

Until this pass each page showed one flat print made of layered atlas parts separated by fractions of a millimetre. In a headset, real stereo depth is the cheapest way to make the page feel like something that could only exist in VR.

**Implemented October 26 in `StoryTableView`, presentation only.** No scene regeneration is needed, and no story data, validator or kept telling changes.

- Each print's existing painted layers stand forward of the page in their authored order: backdrop on the paper, hills 1.2 cm, riverbank 2.4 cm, figures 3.6 cm. The modern river is a single layer and stays flat; generated images remain one flat quad.
- A newly turned page rises from flat over half a second with an ease-out curve. Returning from a question, source detail or the closing page keeps the print standing. Reduced motion shows the final depth at once and holds still; pause holds the rise where it is.
- Each raised layer is drawn in toward the print's centre in proportion to its depth, measured against the visitor's actual viewing distance when the page turns. From the seat, the composition therefore keeps its painted size, and only stereo depth and parallax change. A further 2.5% inset leaves about a centimetre of dusk around the raised layers, like a mat.
- The layers are alpha-tested with depth writes on, so the depth buffer, not transparent sorting, decides what covers what. Wider separation also removes the old sub-millimetre z-fighting risk. Moving existing layers adds no overdraw.

**Geometry check (numerical model, not a render).** I simulated the builder's layer sizes and placement and cast rays from the seated eye and from heads moved ±10–15 cm vertically and ±20 cm sideways. Every raised layer stays inside its backdrop and at least 2 mm above the truth label's box, well clear of the caption and title. The proposed 1.5 cm step let the hills edge rise 0.8 mm past the print's top when the head dropped 15 cm, so 1.2 cm was chosen. Stereo depth on the hills layer may be subtle at Quest resolution, while the figures should read clearly; tune `LayerStep` on a headset.

**Test:** the PlayMode test `PrintLayersRiseInDepthWithinTheirBackdropAndHoldStillWhenAsked` checks the rise, figure depth and layer order, same-page return, reduced motion and pause. It also checks, projected from the seated eye, that every raised layer stays within its backdrop and covers no page text. It was type-checked against stubs only and has not run in Unity.

**Still needed:** capture `10-pop-up-print-60` and `-82` with `SANKOFA_CAPTURE=1`, look at it in a headset, and confirm comfort and frame time on device. Later, the cast atlas's other poses could put each perspective chapter's own person in the foreground.

## Special-award lenses

These are not targets; they show where this iteration helps. *Best First Five Minutes*: voice from the first page, a How it works card instead of a wall of text, a decision by about the second minute. *Best Reason to Come Back*: the unheard side is named at the ending. *Accessibility*: captions always on, one-touch choices, larger text, reduced motion, stand height and recenter.
