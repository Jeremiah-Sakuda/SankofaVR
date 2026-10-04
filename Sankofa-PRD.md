# Sankofa — product requirements

Version 1.0 · September 30, 2026 · Product direction selected; implementation and validation pending

**A story that notices what you notice.**

Sankofa welcomes you around a living fire, listens to the histories and questions that brought you there, and brings a personalised story into the space around you. The fire is the griot: its voice, movement, light and pauses create the sense of someone being present. You enter its stories through the flame and return to the same hearth carrying something that now has meaning.

This is the current product contract. It supersedes the Nafasi concert proposal. The [delivery plan](Sankofa-Delivery-Plan.md), [backlog](docs/backlog.md) and [decision record](docs/decisions.md) distinguish proposed work from evidence. Existing music code and research remain preserved in the previous Nafasi workspace. A first Unity slice with a placeholder story now exists in software; no on-device result exists yet.

## 1. Product decisions and planning assumptions

| Item | Status |
|---|---|
| Product name: **Sankofa** | User selected |
| Fire as the warm storyteller; entering stories through the fire | User-selected direction developed in this PRD |
| Personalised stories based on visitor input; immersive surroundings | Core product requirement |
| Broad histories, backgrounds, familiar voices and languages | Long-term ambition; supported coverage must be explicit |
| No AI-generated music | User constraint; applies to production experiments and shipped content |
| Generated narrative, synthetic narration and generated visual assets | Proposed production approach from the discussion; each output still requires its relevant checks |
| Entertainment; Adapted / Significantly Updated | Recommended submission baseline for developing the existing Sankofa; organisers retain classification authority |
| Unity; standalone Quest 3 as initial test device | Planning baseline; no headset validation or new package choice implied |
| Seated, hands-first, voice optional; full VR story scenes | Competition baseline |
| Room-aware MR hearth | Conditional extension; a complete experience cannot depend on a table, wall or room permission |
| One deeply supported setting, three story routes, two narration languages | Proposed competition scope; reviewed location/era and second language remain to be selected |
| Solo, part-time; new to VR | User-confirmed capacity profile; weekly hours, budget and current headset access unresolved |

The user authorised this PRD. Quantities, timings, language candidates and gates below are planning defaults, not independently approved or measured facts. Revising them must retain the core promise or explicitly revise the product claim.

## 2. Problem, audience and desired outcome

People often encounter history as disconnected information. Others have fragments of cultural or family context but lack an engaging way to explore the places, daily life and experiences around those fragments. A visitor may want connection, curiosity, companionship or an enjoyable short story. They should not need historical expertise, a known genealogy, fluency in a heritage language, or gaming experience.

Primary audiences are curious adults and diaspora or mixed-background visitors seeking an accessible connection to history. The competition experience is designed for an individual seated visitor. Broader family use and shared visits are future work.

The desired outcome is a complete, emotionally legible encounter: the visitor understands what their interest changed, remembers one story moment, and has a specific thread worth returning to. Learning can emerge from exploration; formal educational efficacy is not a release claim.

### Product principles

- **Hospitality before explanation.** The opening establishes a welcoming presence and teaches through a simple action.
- **Personalisation must change what happens.** Names inserted into a fixed narration are insufficient.
- **Attention is an invitation.** Looking around should not constantly interrupt or redirect a story.
- **Embodiment must have meaning.** Holding, illuminating, revealing or receiving something changes a narrative beat.
- **Specificity earns familiarity.** Language, voice, historical context and visual design need separate review.
- **Evidence remains distinguishable from imagination.** Immersion must not manufacture certainty about the past.
- **Silence is a complete way to participate.** Hands can complete the entire flow; optional invitations never trap a passive listener.
- **The hearth offers continuity.** A session ends at a familiar place and leaves a meaningful route back.

## 3. Ambitious end state and competition scope

The long-term product is a growing collection of histories that the fire can tell through different voices, languages and perspectives. Visitors can bring interests and permitted personal material, revisit threads, and discover connections across places and periods. New content expands what the world can represent. Shared visits and contributor tools may follow after the solo experience works.

"All backgrounds" is an inclusion goal, not a claim of universal historical or language coverage. Every released context has a declared place, period, source base, visual inventory and review status. An unfamiliar context is never silently mapped to a nearby country or a generic culture.

| Scope | Required for competition baseline (P0) | Conditional or later |
|---|---|---|
| Setting and stories | One reviewed place/period, at least two interest themes, at least three materially different scene plans, one complete closing beat per plan | A second distinct cultural setting is P1; universal historical coverage is a roadmap goal |
| Personalisation | Freeform voice request within supported coverage; equivalent hands-based context/theme choices; generated wording and selection of compatible scene beats | Arbitrary worlds and unrestricted plot invention |
| Languages | English plus one reviewed narration language, with English subtitles; English live conversation initially | Second-language live conversation and code-switching require separate acceptance; broader language catalogue later |
| Narrator | One fire persona with coherent voice and expressive states | Voice catalogue and multiple storyteller traditions |
| Visuals | One polished hearth, one fire-entry transition, a reusable scene kit, two inspectable story objects, one consequential hand moment | Crowds, complex humanoid performances, live generation of new geometry |
| Return | Local visit memory, one meaningful continuation, explicit replay and deletion | Accounts, cloud sync, social visits, genealogy services |
| Reliability | Complete bundled story and hands path without network; transparent recovery from generation failure | Offline generative model |
| Presentation | Full immersive seated story; adjustable forward placement and reduced-motion transition | MR hearth with meaningful room behaviour, after device proof |

**Initial content candidate:** a specific Swahili-coast place and period, with English and Kiswahili narration. This is a proposal because of the existing East African context and earlier language discussion. Choose the actual place, era and language pair after checking sources, reviewer availability, voice quality and asset feasibility. Do not treat an entire coast or language community as one uniform setting.

One authored demonstration remains useful for visual testing, but it cannot by itself satisfy the personalisation requirement. The release needs genuinely different generated performances inside supported boundaries.

## 4. Experience and journey

### First visit

| Moment | Visitor experience | Product behaviour |
|---|---|---|
| Arrive | A fire, close crackling, space to settle; “Come. Sit with me.” | Local assets start immediately; no network or room scan needed to see the hearth |
| Settle | Adjust placement, captions, language and sound; use a simple nearby control | Hands tutorial is one meaningful interaction; one-hand path available |
| Bring a question | Choose a context and interest, or optionally speak | Show what the system understood; ask only for missing essentials; keep ancestry and language choices independent |
| Gather the story | Shapes hint at the supported setting in smoke or embers | Prepare and validate the story; display a clear waiting state and an immediate bundled-story option |
| Enter | Fire grows into a soft curtain of light; an ember becomes a light in the story | Scene transforms around a stationary viewpoint; narration and sound remain continuous |
| Discover | Observe, inspect an object, ask a question, or accept an invitation | Respond locally to hands immediately; adapt a permitted next beat to explicit interest |
| Become consequential | Receive, place or hold an object with a clear narrative consequence | Confirm intent; show the consequence and preserve it in story state |
| Understand differently | An earlier object or scene gains new significance | Deliver a coherent reveal grounded in what actually occurred |
| Return | The world folds into the hearth; a keepsake remains | Offer save, replay, a specific next thread or leave; no account needed |

The baseline visit targets 5–8 minutes without extended questions, with a satisfying stopping point by 10 minutes. Visitor-controlled pauses are excluded from this timing. A return visit can skip the welcome monologue. The original web intro's timing is a reference; the VR introduction continues through interaction rather than imposing a fixed 98-second gate.

### Returning visit

The fire recalls a chosen object or question only when it was saved. The keepsake offers **Replay this visit** and **Follow this thread** as different actions. A continuation introduces a new perspective, fact or consequence tied to that saved interest. An empty shelf offers a new beginning without claiming a prior relationship.

### Optional MR hearth

If implemented, room geometry determines the placement and size of the hearth and entry opening. A detected surface must contribute to staging or reach, rather than merely serving as a background. Denied permission, missing surfaces and changed rooms retain the full seated experience through manual placement or a virtual hearth. Full immersion does not require artificial travel toward the fire.

## 5. User stories

All stories below are planned; none is marked implemented by inclusion in this document.

| ID | As a visitor, I want… | So that… | Priority | Requirements |
|---|---|---|---|---|
| US-01 | to feel welcomed before learning controls | I can settle into the experience | P0 | SK-01, SK-02 |
| US-02 | to participate entirely with my hands | I can use it without speech or controllers | P0 | SK-02, SK-08 |
| US-03 | to select histories and interests without providing a surname | my curiosity is enough to begin | P0 | SK-03, SK-04 |
| US-04 | my choices to change the places, objects and moments I encounter | the story feels relevant to me | P0 | SK-04, SK-08 |
| US-05 | to know when a requested setting is unsupported | I can choose a supported experience knowingly | P0 | SK-03, SK-05 |
| US-06 | narration in a familiar language with clear subtitles | I can understand and connect with the story | P0 | SK-06 |
| US-07 | to enter the story while remaining comfortably seated | the transformation feels inviting | P0 | SK-07, SK-15 |
| US-08 | to inspect something and see a meaningful response | my physical participation matters | P0 | SK-08, SK-10 |
| US-09 | to interrupt with a question and resume naturally | exploration does not lose the narrative | P0, in accepted live languages | SK-09, SK-10 |
| US-10 | to simply listen when I choose | I can enjoy the story without performing tasks | P0 | SK-02, SK-08 |
| US-11 | to understand what is documented or imagined | I can enjoy the story without confusing reconstruction with fact | P0 | SK-05 |
| US-12 | to return to an unfinished interest | a later visit offers meaningful continuity | P0 | SK-12 |
| US-13 | to replay a saved experience | I can revisit what happened without it being rewritten | P0 | SK-12, SK-13 |
| US-14 | to pause, recover or continue when a service fails | a technical interruption does not strand me | P0 | SK-10, SK-13, SK-14 |
| US-15 | to control what is saved or sent | personal exploration remains under my control | P0 | SK-16 |
| US-16 | to adjust motion, text, reach and audio | I can find a comfortable way to participate | P0 | SK-02, SK-06, SK-15 |
| US-17 | the fire to fit my actual room | its physical presence feels grounded | P1 | SK-17 |
| US-18 | different histories to have their own visual and storytelling specificity | expansion feels respectful and substantial | P0 in released coverage; broader P1 | SK-05, SK-06, SK-11 |

## 6. Functional requirements and acceptance

### SK-01 — Fire presence and opening

The fire is the narrator. No humanoid host is required. Its states include welcoming, listening, gathering, telling, holding for a question, transitioning and resting. Voice, light and motion share explicit cues. The opening line is preserved in English; approved equivalents serve other narration languages.

**Accept when:** the hearth starts without network; welcome, listen and tell states are recognisable in review; the visitor can act during the opening; repeated visits can skip it; no state requires a humanoid animation or cloud call to animate the fire.

### SK-02 — Hands and onboarding

Start, preferences, input choices, confirmation, inspection, questions, pause, resume, save, delete and exit work with hands. Provide one-hand operation and a nearby selection alternative to any special pose. Essential interactions remain within a comfortable forward reach, approximately 2 feet from the seated visitor. Voice can accelerate these actions but is optional.

**Accept when:** a first-time tester completes the visit with microphone permission denied and no controller paired; required objects do not demand standing or leaning beyond the configured reach; declining an invitation still leads to a coherent ending.

### SK-03 — Input, understanding and coverage

Collect context, period when relevant, interests, narration language and optional voice preference. Do not infer ethnicity, preferred language or genealogy from a name. A surname is not required. Freeform requests are summarised for confirmation before generation. A request outside released coverage offers supported alternatives with an explicit explanation; it is never silently substituted.

**Accept when:** supported, ambiguous, mixed-background and unsupported requests produce appropriate confirmations; all selected settings correspond to a published coverage record; hand choices can convey the same essential intent as speech.

### SK-04 — Personalised story planning

Generate a structured story from confirmed intent, retrieved approved facts and the available scene inventory. The plan includes a beginning, development, physical invitation, changed understanding and ending. It selects compatible scene beats and objects before producing their narration. A fresh plan need not invent a new building or character model.

**Accept when:** two contrasting interests in the same context change at least two observable dimensions, such as object, event order, perspective, encounter or closing thread; at least one difference occurs before the ending. The three release routes have complete endings and meaningful hand participation. Exact replay never requests a replacement plan.

### SK-05 — Historical integrity

Preserve three distinct evidence states: documented context with source references; visitor-provided accounts labelled as such; and imaginative reconstruction. Cultural statements also require sources and specificity. A model-applied label is not verification. Maintain claim-level evidence links, the reviewed source version and uncertainty in the plan. Offer a concise introduction to reconstruction and an optional hands-accessible source/details view.

**Accept when:** no output asserts a specific ancestor's actions from a surname or location; historical claims in the release evaluation link to reviewed evidence; fictional people/dialogue are labelled reconstruction; unsupported claims are removed, qualified or returned for review. A realistic image never changes an uncertain claim into a documented one.

### SK-06 — Language, voice and captions

Choose narration language independently of context. English and one additional language form the planned baseline. Kiswahili is the first candidate, not an accepted result. Publish separate support for narration, interface, live speech recognition and live replies. Do not imply full conversation support from TTS support alone. Voice identity and volume remain coherent across welcome, narration, fallback and questions.

English subtitles must accompany non-English competition content, including dynamic replies. Captions remain selectable in English narration. Translate approved meaning, preserve uncertainty and names consistently, and review pronunciation with fluent listeners. Voice selection uses short previews and visitor choice; cloning a real person's identity is outside P0.

**Accept when:** fluent reviewers approve the released second-language sample and interface text; captions accurately follow all release paths; unsupported live languages have clear hands-based question options; the app never switches language or voice silently on error.

### SK-07 — Entering and leaving through fire

The signature transition expands light, smoke and scene scale around a stationary viewpoint. One element crosses the transition coherently: an ember becomes a lantern, a pattern becomes a landscape feature, or another content-approved correspondence. Preserve hand visibility or clear interaction status during transitions. Include a restrained dissolve alternative, pause and return-to-hearth control.

**Accept when:** no mandatory camera translation, acceleration or rotation is used; a visitor can interrupt and return safely to the hearth; both transition variants preserve the same story state and spatial orientation; users recognise a shared element on both sides.

### SK-08 — Attention and consequence

Use inspection, selection, object handling or an intentional hand action as strong interest signals. Head direction may identify candidates but cannot establish eye attention or consent. Quest 3 has no eye-tracking hardware. Avoid advancing a consequential choice because someone briefly looked around.

Every release plan has at least one observable consequence tied to a deliberate action, with a corresponding narrative acknowledgement. An open palm can receive a keepsake; a simple nearby select action provides the same result. Passive progression follows an authored default and never claims the visitor chose it.

**Accept when:** the same action is acknowledged once; accidental glances do not trigger commitments; the visitor can explain what changed; hand-only questions and passive paths still complete the story.

### SK-09 — Interruptible conversation

In supported live languages, visitors can interrupt narration, ask about the current scene, and resume. Questions are answered using the current objects, revealed facts and story position. The narrator avoids spoilers, acknowledges uncertainty and redirects unrelated requests gracefully. The fire supplies local listening feedback before remote speech arrives.

**Accept when:** interruption stops the current speech promptly; a reply about a selected object refers to the correct instance; resume does not repeat a completed consequence or skip an unheard reveal; a delayed answer from an earlier scene is discarded. Equivalent contextual questions are available by hand.

### SK-10 — Runtime story control

The application owns state, timing and permitted scene actions. Model output is a proposal validated against available assets, current beat and evidence permissions. Use explicit story, beat, object and request identifiers. Permit bounded actions such as reveal object, illuminate target, offer object, choose next beat and return to hearth. Models cannot execute arbitrary code, load arbitrary URLs or overwrite confirmed history.

**Accept when:** malformed, unavailable, contradictory, stale and duplicate actions fail without changing the world; only one narrator owns the audio turn; interrupted speech cannot later cause a queued visual reveal; validated story assets are ready before the corresponding line is spoken.

### SK-11 — Visual and sound production

Use one art direction across fire, setting, objects and transitions. Nearby interactable objects have full geometry, believable scale and consistent material response from supported viewing positions. The scene inventory records provenance, cultural review, performance cost and allowed uses. Background generation must not embed a second copy of an object that is meant to be separately movable.

No AI-generated music is used. Human-made music is optional and needs a concrete source, production allowance and permissions. Recorded or procedural environmental sound and authored fire effects support the experience. Each shipped audio asset has a provenance record; the existing intro music is not cleared merely because it is in the older repo.

**Accept when:** objects withstand close inspection and turning; lighting and sound placement agree; scene changes retain object identity; the actual Quest presentation passes visual/listening review; all release assets have recorded rights. A desktop render cannot pass this requirement.

### SK-12 — Memory, replay and return

With save enabled, store the visitor's accepted choices, realised story plan, asset/content versions, narration/captions required for replay, chosen language, consequence history and next thread. Exact replay refers to the saved realised visit and does not regenerate wording. A follow-up is a new child visit, labelled as such, which can acknowledge a retained interest without fabricating memory.

**Accept when:** a second visit offers a genuinely new perspective tied to the first; replay preserves the earlier content; missing cached content is reported and cannot silently become a new version; deletion removes the keepsake and the local remembered thread.

### SK-13 — Pause, resume and save recovery

Pause audio, scene progression and pending input together. Save at stable beat boundaries and record heard/completed events separately. Resume a partially heard line from a safe boundary without replaying its completed action. On application restart, recover the last committed checkpoint; do not imply exact mid-sentence crash recovery. Failed saves leave the current experience usable and offer retry.

**Accept when:** headset removal, app suspension, crash/relaunch and save failure preserve a coherent state; old content versions are migrated explicitly or replayed from their retained package; story history survives failed or changed spatial placement.

### SK-14 — Network and generation fallback

Bundle a complete authored story, narration, captions, hand interactions and ending. Live personalisation may use a service; the fallback clearly explains when that capability is unavailable. Local interaction feedback and already-loaded story playback do not wait for inference. Provide a visible escape from loading from the outset; one failed generation cannot trigger an unbounded retry loop.

**Accept when:** offline cold start reaches the hearth and a full story; failure during a question returns to the current beat; incomplete or invalid generation cannot enter the stage; users can retry or select the bundled story without losing confirmed preferences. Do not describe the bundled path as a newly generated personal story.

### SK-15 — Comfort, accessibility and performance

Provide adjustable reach/placement, caption size and contrast, separate voice/ambience/music volume, reduced motion, one-hand selection, recenter and a persistent way to pause/leave. Important content stays in a comfortable forward view; behind-the-visitor sound cannot carry essential information alone. Fire state is understandable without colour alone.

Profile actual supported standalone hardware through a complete warm session, including fire, transition and worst-case simultaneous audio/interaction. Select and record the display refresh target and matching frame budget; the contest's stated minimum is a floor, not a substitute for a stable chosen refresh rate.

**Accept when:** all supported devices have recorded frame, memory and sustained-session evidence; no mandatory travel or uncomfortable reach remains; tested users can find pause and return; performance and comfort failures are resolved before adding P1 effects.

### SK-16 — Data and permissions

Before microphone use, explain the intended speech processing and relevant third-party transfer. Denial preserves a complete hands path. Raw microphone recordings are not retained by the application by default. Session content stays ephemeral unless the visitor opts to save; expose local deletion and clear any server-side session data under a documented retention policy. Verify provider retention independently before making privacy promises.

Store credentials server-side or use short-lived session credentials. Keep personal stories, transcripts and real recordings out of Git. Send semantic interaction events rather than room images or continuous hand traces unless a later, explicitly designed feature needs them. Account creation and cloud history sync are outside P0.

**Accept when:** the permission-denied flow works; saved versus unsaved behaviour matches the notice; deletion and expiry are exercised; shipped files contain no development secrets; no undisclosed telemetry captures personal content.

### SK-17 — Optional room-aware hearth

MR is P1. If selected, use supported room understanding and anchoring; confirm comfortable placement and retain manual correction. Keep persistent room placement separate from visit memory. Real geometry must change useful staging, scale or interaction. Passthrough alone is not acceptance evidence.

**Accept when:** room permission denial, no suitable plane, anchor loss and an unfamiliar room preserve the complete experience; at least three distinct room setups have device evidence; a comparison explains the benefit over the virtual hearth. Otherwise ship the full VR baseline and describe it accurately.

### SK-18 — Release provenance and entry

Maintain the existing web Sankofa baseline, an adaptation changelog, source/assets rights ledger, release coverage and a truthful testing guide. Treat the older AGPL repository's code, contributor rights, third-party assets and provider terms separately. Document what was reused, rewritten and added during this competition. The workspace's old directory and identifiers do not determine the entry division.

**Accept when:** the actual released build and access path work for an external tester; the demonstration shows the implemented product; public claims match device/language/history evidence; known rights or eligibility questions affecting the entry are resolved. Full competition text remains in ignored references.

## 7. Visual and audio direction

The target is material richness and coherent depth: convincing fire, textured objects, readable silhouettes, believable scale, spatial sound and controlled changes in light. The fire has personality through authored motion and narrative cues. It does not require eyes, a mouth or a human body. Characters inside stories are separate production decisions and must not become a prerequisite for proving the fire narrator.

| Layer | Production approach | Acceptance focus |
|---|---|---|
| Hearth and fire | Authored Unity geometry, effects, lighting and recorded/procedural ambience | Warm presence, legible states, restrained smoke/transparency cost |
| Setting | Reviewed reference art; editable mesh kit; generated spatial background only if qualified | Correct context, near/far consistency, stable depth during head movement |
| Story objects | Modelled/scanned assets or image-to-3D candidates cleaned up in Blender | All sides, pivots, scale, handling, identity across scenes |
| Story people | Limited authored or licensed performances where needed | Appropriate clothing, posture and timing; no untested crowd dependency |
| Transition | Coordinated light, particles, scene scale, sound and narration | Recognition of the connecting element and stationary viewpoint |
| Music | Human-made score or rights-cleared source, if used | Supports speech and place; provenance and budget recorded |

Marble, Meshy, reference-conditioned imagery and existing asset libraries are evaluation candidates. They are not selected suppliers or demonstrated Quest integrations. A panorama supplies distant imagery; it is not complete close-range 3D. A collision mesh is not a finished visual model. Splats require a compatible renderer and device profiling; their availability in another SDK does not establish compatibility with this Unity project. Final render technique follows evidence.

## 8. Story system and AI boundaries

Proposed flow: **confirmed intent → approved context and capability inventory → structured plan → evidence/asset validation → narration and subtitle preparation → staged performance → saved realised visit**.

The planner is constrained by what the application can stage. It can vary focus, sequence, point of view, permitted relationships and narration. Historical facts remain fixed by reviewed evidence. Scene composition must be validated before narration describes visible objects or actions. Deep research or new-world generation occurs outside the live critical path.

At runtime, a small state machine owns Hearth, Preparing, Ready, Entering, Story, Question, Paused, Returning and Closing. Errors return to a recoverable state. Playback uses explicit narrative cues tied to audio completion; translated audio duration is measured rather than reusing English timestamps. Word-level timing is not assumed from a provider unless verified.

The minimum proposed data contract is:

| Record | Necessary information |
|---|---|
| Visitor intent | Confirmed context, period, themes, language, input mode and save preference |
| Context package | Coverage limits, claim/source IDs, cultural review, terminology and asset inventory version |
| Story plan | Stable IDs, beats, assets, facts/reconstruction markers, legal actions, reveals, endings and continuation |
| Narration | Text, locale, voice/version, audio, captions and scene cues |
| Visit journal | Heard beats, completed actions, accepted/declined choices, pending cancellation IDs and checkpoint |
| Saved visit | Realised plan, retained content references, language, keepsake and parent/child links |

External replies must be validated as data. User input and retrieved material cannot modify system permissions. Cache and retain accepted content for replay. Exact model IDs, costs, available voices, account access and deployment terms are pinned during the provider trial, not inferred from this document.

## 9. Acceptance evidence and success measures

These are proposed targets to test, not achieved results or statistically validated thresholds.

| Measure | Initial acceptance target | Evidence |
|---|---|---|
| Local cold start | Interactive hearth within 10 seconds on test hardware | 10 cold starts with device/build recorded |
| First participation | A meaningful hands action available within 30 seconds after readiness | Observed first-time sessions |
| Generation wait | Valid story ready within 20 seconds at p95 on the selected test network; at 20 seconds offer explicit recovery | At least 30 requests, timing boundaries and network conditions recorded; revise provider/scope if unmet |
| Live response | Local acknowledgement within 150 ms; first meaningful reply audio within 2 seconds median and 4 seconds p95 after confirmed end of input | At least 30 turns; distinguish speech detection, provider time and playback |
| Interruption | Active speech stops within 250 ms of locally detected interruption/stop | Device log plus independent audio check |
| Personalisation | Contrasting intents change at least two observable story dimensions | At least 12 paired requests, plans and recorded walkthroughs |
| Grounding and validation | No unsupported factual claims or missing-asset actions in the release sample | Human review of 30 generated plans, including all release routes and languages |
| Onboarding and consequence | At least 4 of 5 formative participants complete unassisted and explain one consequence | Observed sessions; directional sample, not a population claim |
| Familiar language | Two fluent reviewers accept intelligibility, pronunciation and tone for the released additional language | Recorded rubric and corrections; disclose sample limits |
| Replay and return | Preserved replay plus a follow-up visibly tied to a saved choice | Save/relaunch/replay/delete and second-visit walkthroughs |
| Reliability | 10 consecutive complete visits; tested denial, disconnect, stale reply, pause and save failure | Device/software evidence recorded separately |
| Visual quality | Close inspection, transition and worst-case fire pass both user review and device performance checks | Actual headset capture, profiling and review notes |

Optional feedback collection is opt-in and uses minimal local events. Do not collect raw conversations simply to populate metrics. A smaller or incomplete sample is reported as such and does not silently pass a gate.

## 10. Competition fit and submission constraints

The recommended entry is **Adapted Entertainment**. The FAQ explicitly places ports in the adapted division. Both Entertainment divisions offer a $100,000 first prize; no evidence establishes an easier competitor pool. New requires conception and development within the competition window without a pre-existing codebase. [Rules](https://start-developer-competition-26.devpost.com/rules), [FAQ](https://start-developer-competition-26.devpost.com/details/faqs)

Hands-first completion is required. Seated use, short satisfying visits and a substantive entrant-built core guide the design. Non-English content needs English subtitles. Section 4.17 restricts identifiable people other than entrants: seek clarification before actual family or named historical-person reconstructions; the planned baseline uses reviewed historical contexts and clearly fictional characters. [Rules](https://start-developer-competition-26.devpost.com/rules)

Document Start membership, eligibility, provenance and rights before submission. Target November 16; the official deadline is November 18, 2026 at noon Pacific / 3 p.m. Eastern. Provide the required release-channel access and an honest video under three minutes. Keep the entry available through judging and respect the deadline freeze. [Rules](https://start-developer-competition-26.devpost.com/rules)

The adaptation narrative is the change from a web-based heritage narrative with a cinematic welcome to a personalised spatial performance: fire presence, embodied entry, hands consequences and retained story threads. Retain the original opening and before/after evidence. Reusing only a name or schema is insufficient product continuity.

## 11. Risks and decisions still needed

| Risk / decision | Treatment and next evidence |
|---|---|
| Breadth overwhelms cultural and visual quality | One reviewed context first; publish actual coverage; second context only after the first proves repeatable production |
| Personalisation becomes a cosmetic rewrite | Paired requests must change staged content and a later consequence |
| Familiar voice feels wrong or changes mid-session | Fluent review and one tested welcome/narration/live/fallback chain |
| Story generation invents certainty | Claim evidence, reconstruction labels, adversarial samples and a reviewed fallback |
| Fire looks like a screen saver | Direct its states and relationship to speech; test presence and transitions with sound |
| Scene generation exceeds performance or asset budget | Compare one generated candidate with editable meshes on device; choose the accepted representation |
| Network delay breaks presence | Local fire/input feedback, prepared assets, timed escape from waiting and a complete bundled visit |
| MR adds scope without meaning | P1 gate; full VR is complete; promote only after room benefit and reliability evidence |
| Part-time capacity is too low | Estimate after two short proofs; protect core experience and visibly revise the claim if P0 cannot fit |
| Story/content package remains unspecified | Select place/period, two themes, three scene plans, reviewers and sources before production expansion |
| Costs and hardware unknown | Confirm recurring Quest access, weekly hours, tool/API/art/audio budget and service hosting before committing to paid dependencies |
| Reuse rights and contest content interpretation | Record original ownership/contributors and asset terms; prepare focused organiser questions; no messages sent without authorisation |

## 12. Completion definition and next work

The competition build is complete only when a new visitor can enter the hearth, supply an interest by hands or supported speech, experience a coherent personalised spatial story, recognise a consequence of participation, return with a keepsake, and revisit a remembered thread. It must also complete its documented fallback, accessibility, privacy and recovery paths, with device and content evidence.

First prove the fire and transition in a 30–45-second visual slice. Separately, prove structured personalisation using two contrasting inputs and approved scene capabilities. Combine them before expanding content.

See the [delivery plan](Sankofa-Delivery-Plan.md) for staged gates and capacity, and the [backlog](docs/backlog.md) for actionable work.

## Sources and evidence boundaries

Primary sources inspected September 30, 2026. Provider capabilities are documented claims, not our own quality, latency or Quest benchmarks. The older web code was inspected without running its app; its historical review response comes from the user's account.

- [Existing Sankofa](https://github.com/Jeremiah-Sakuda/Sankofa), [licence](https://github.com/Jeremiah-Sakuda/Sankofa/blob/main/LICENSE), [opening beats](https://github.com/Jeremiah-Sakuda/Sankofa/blob/main/frontend/src/lib/griotBeats.ts), [context loader](https://github.com/Jeremiah-Sakuda/Sankofa/blob/main/backend/app/knowledge/loader.py).
- [Gemini Live](https://ai.google.dev/gemini-api/docs/live-api) documents conversational speech, interruptions and tool use. [Speech generation](https://ai.google.dev/gemini-api/docs/speech-generation) has separate language/voice capabilities; neither establishes familiar pronunciation for our audience.
- [Marble quickstart](https://docs.worldlabs.ai/api) documents asynchronous spatial generation and exports; its approximate five-minute generation estimate is not a runtime guarantee.
- [Meshy multi-image generation](https://docs.meshy.ai/en/api/multi-image-to-3d) and [Poly Haven](https://polyhaven.com/) are asset candidates; outputs require context, rights and runtime review.
- [Meta input](https://developers.meta.com/horizon/essentials/horizon-os-input/) distinguishes hand, head and eye capabilities. [Performance guidance](https://developers.meta.com/horizon/documentation/unity/unity-perf/) informs testing; actual device profiles determine our budget.
