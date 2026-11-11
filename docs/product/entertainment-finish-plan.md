# Sankofa: finish the Entertainment experience

November 10, 2026 · PM audit and proposed implementation sequence · Companion to Claude's active griot work

**Recommendation:** finish the personal, intimate storytelling experience already taking shape. Keep the fire, prepared pop-up theatre, two perspectives and kept strand. Make the visitor care about the question before adding another capability. The decisive moment is understanding what the same exchange costs each woman.

This is a plan, not an implementation record or a prediction of winning. It addresses the user's corrected mandate: **Sankofa, Entertainment, targeting the Adapted / Significantly Updated award**. It complements the [griot rework](../reviews/2026-11-10-winning-rework-audit.md); it does not restart the product or take over Claude's voice integration. The [PRD](prd.md), [decision record](../decisions.md) and final release manifest must be reconciled when these proposals land.

## Audit boundary and ownership

The stable baseline is `b860e31` on `feature/griot-visit`, whose recorded review APK was built from `f7ec6d8`. I read the composer, catalog, narration manifest, strand, question contract/service, app/view, release records and user-supplied original Devpost submission. I inspected existing desktop intake and self-turning-choice captures. The intake capture includes a configured-service interface; it does not establish what the offline APK shows.

During this audit Claude was editing the Android manifest, `PersonalStoryApp`, `StoryTableView`, recorder/voice files and PlayMode tests. Those edits were left alone. No Unity build, simulator, sound playback, device test or new model call was run. Previously reported tests and recording hashes establish software checks, not listener response or hardware acceptance.

Before this document was committed, Claude landed `087e653` (spoken questions behind a switch). Its client sends the current chapter ID and records up to twelve seconds; it is a question path, not voice-to-story creation. That checkpoint does not itself establish hosted, listening or device acceptance. The baseline findings below remain pinned to `b860e31`; use E3 to evaluate the integrated voice feature.

| Workstream | Owner for this pass | Boundary |
|---|---|---|
| Recorder, microphone permission, ask/send/answer flow, cancellation, fallback, integration tests and APK | Claude | In progress; re-audit the completed commit, not intermediate files |
| Story relevance, editorial brief, priorities, acceptance cases and submission argument | This PM pass | These documents only; no runtime or audio edits |
| Story/core/narration changes recommended below | Next implementation owner, after Claude's checkpoint | Composer/catalog, backend copies, voice manifest and tests change together |
| Direct object touch and page simplification | Same runtime owner after voice checkpoint | Shares view/app/input with Claude; do not build a competing interaction path concurrently |
| Listening, community review, device access, account/signing/hosting and submission | Entrant, with implementation support | Existing [manual steps](../release/manual-steps.md); no contact, purchase or publication occurs through this plan |

The user has approved building voice in the supplied Claude transcript. That settles development scope. It does not establish a hosted service, accepted retention terms, an operating budget or release acceptance. The old documents saying voice is deferred need a scoped update after integration.

## What to carry forward from original Sankofa

The [original submission](https://devpost.com/software/sankofa-y47f9p), supplied by the user, records a Creative Storytellers win. It does not disclose the judges' causal reasoning. These are product strengths to preserve, not proven reasons for the award:

| Original strength | What the Quest version should preserve | Current gap |
|---|---|---|
| Sparse personal fragments become an invitation into history | A visitor understands why this telling relates to their curiosity | Offline keywords select one of three prepared tellings; touching an object alone is preference selection |
| “Come. Sit with me.” establishes hospitality | Immediate welcome, a comfortable place to watch, freedom to begin | Welcome is recorded; sound and pacing remain unheard |
| Words, pictures and sound form one performance | Illustrations carry the scene while voice guides attention | Context passages interrupt the dramatic setup; page and plaques still compete with the stage |
| Conversation creates presence | Ask about this moment, receive an intelligible answer, return without losing the scene | Claude is implementing turn-based questions; that is not full-duplex conversation or story generation from speech |
| Trust labels make imagination legible | Clear source/reconstruction distinction without bureaucratic narration | Labels improved, but review notes and museum wording are still spoken |
| The user's history matters | Make room for both inherited fragments and simple curiosity | Do not imply a fixed Kenyan fiction reconstructs the visitor's actual family |

The public promise should have two levels: **Sankofa's ambition is to help people approach histories they did not get to hear; this edition offers three source-bounded, imagined tellings in one Kenyan setting.** Personal connection can be emotional and exploratory without pretending to verify genealogy. Broader appeal comes from recognizable human stakes inside specific context, not from erasing the context.

## Findings that change the remaining work

| Finding | Evidence at the baseline | Product consequence |
|---|---|---|
| The gourd story has the strongest immediate conflict | `BundledPersonalStoryComposer.Trade`: one woman keeps food at home; the other counted on that flour | Use it as the first demonstration. The second perspective earns its spatial turn |
| All three stories put exposition between tension and resolution | Each second chapter is `History`. The gourd clip is 24.648 s; water narrates a pronunciation-review note | Repair the bridge before rewriting endings or adding art |
| The source object and story object can be confused | `VesselFact` concerns a museum milk/blood gourd, while the scene uses a flour gourd | Move C7 to optional context; it is not evidence for the exact depicted flour vessel |
| The existing questions mostly explain limitations | Ancestors, exact location, universal custom, source support, or questions to ask relatives | Add a small set of dramatic questions; keep source details accessible |
| Six perspectives are not six saved performances | `KeptStrand` holds identifiers; `PersonalStore` keeps one telling with replacement confirmation | Describe beads as remembered sides, not a six-story replay library |
| The return is currently an instruction | `ReturnInvitation` names an heirloom and side | The reason to return is the other person's stake. A bead alone does not create curiosity |
| The whole current chapter can be visible to the answer service | `backend/ask.py:revealed_pages` includes the current chapter, regardless of audio progress | Page-bounded answers do not establish protection against a spoiler later in that same recording |
| Early entry can interrupt the sole spoken disclosure | `TellWelcome` ends when intake ends; objects remain selectable throughout | Keep the on-page label and test early entry. “Always heard once” is not established |
| Software and public descriptions have drifted | PRD/README still say voice deferred and retain older input/presentation descriptions | Reconcile capabilities against the frozen build before recording the pitch |

Duration estimate, not a playback measurement: the four welcome recordings total 35.08 s; the current 0.6 s gaps yield about 37.5 s if uninterrupted. A passive gourd route after selection is about 129 s, using recorded lengths, two 1.6 s page rests and the 14 s choice wait. That is already a compact visit. Do not add a compulsory reflection form, another lobby or a second onboarding sequence.

## The judged journey to protect

1. **Invitation.** A warm fire, a short welcome and three understandable interests. A visitor may start while the welcome is speaking. Selecting early still leaves the setting and imagined status legible.
2. **A reason to care.** The gourd telling establishes the shortfall and food left at home. The visitor recognizes the conflict without historical expertise.
3. **A change of perspective.** Following the woman expecting more brings her forward and reveals what accepting less costs her. The same encounter acquires another meaning; the visitor does not decide its moral outcome.
4. **A hand moment.** Touching the invited gourd visibly moves it into the exchange, with a reachable alternative. Watching is equally complete.
5. **Curiosity answered.** One optional question deepens what has already happened. Voice and touch lead back to the same moment. An answer does not start a second app inside the story.
6. **Something kept.** Keep remembers the telling and adds the appropriate bead. The return invitation points to a specific unheard perspective. A passive route can earn a bead when deliberately kept: it records an experience, not a skill achievement.

The [official rubric](https://start-developer-competition-26.devpost.com/details/judging) gives equal weight to innovation, experience, technical implementation and polish. Our evidence should be a spatial perspective change, an understandable complete visit, reliable hands/recovery, and a coherent audiovisual performance. Actual Quest performance and unfamiliar-room behaviour remain separate from desktop evidence. These are our priorities, not numerical judge-score predictions.

## Ranked implementation briefs

Effort estimates are engineering/editorial working hours after orientation, not elapsed promises. Listening, recording approval, hosting and device availability are separate dependencies. Do not add the rows together as an assumed solo capacity commitment.

| ID / priority | Change and acceptance | Requirement / module | Estimate and dependency |
|---|---|---|---|
| E1 · P0 | Replace the long spoken context bridges with short reviewed formulations; preserve sources and exact validation. Put three dramatic questions beside their appropriate beats. See the [editorial handoff](../content/enkare-nairobi/table-editorial-pass.md) | SK-04/05/06/09/11; content/core/backend/narration; Experience and Polish | 3–5 h integration, plus review and fresh audio. First scope: gourd; no full-story rewrite |
| E2 · P0 | Make object selection physically honest. Touch the heirloom itself to choose, and touch the invited gourd itself to act. Keep existing pinch/panel alternatives. One contact causes one accepted action; pause/tracking loss never accepts an action | SK-02/08/15; view/input/app; Innovation and Experience | 3–6 h after Claude checkpoints voice. No free physics, pickup/drop simulation or new model required |
| E3 · P0 | Run the completed voice path against the cases below; fix same-chapter spoilers and lifecycle interruptions. Keep captions and authored questions complete without permission or network | SK-09/10/13/16; Claude's voice flow/service; Technical and Experience | 2–4 h audit/fixes after integration, not a second voice implementation |
| E4 · P0 | Listen to the actual welcome, gourd route and mix first, then every shipped current clip. Check meaning, name pronunciation, warmth, audible clipping and speech over ambience | SK-06/11/18; content/audio; Polish | 45–60 min first review, then targeted fixes. Hash checks cannot close this |
| E5 · P0 | Test hands, reach, captions, pause, room denial and performance on the exact candidate; establish independent install/access | SK-02/15/17/18; release; Technical | 45–60 min initial device session plus fixes; access logistics start immediately |
| E6 · P1 | Give object labels human interests and strengthen the existing return invitation. No personal-data form. Test that choosing a thread makes sense without knowing the object | SK-01/03/04/12; content/view; Experience and repeat use | 1–3 h plus any changed recording. Reuse the current mapping and strand |
| E7 · P1 | Tighten visual hierarchy: stage and people first, captions second, optional context last. Keep essential controls and text accessible. Correct obvious prop mismatch only after reach is checked | SK-07/15; view/art; Polish | 2–3 h with matched captures; avoid another scenery/character overhaul |
| E8 · P0 | Reconcile PRD, README, adaptation, testing guide, Devpost draft and APK identity against final observed capabilities; record a truthful demo and complete release | SK-18; docs/release; Presentation and entry access | 3–5 h preparation plus account/video work; no old capture as proof of new features |

E1–E5 and E8 come before richer fire effects, grab-to-rotate, six individual bead interactions or more settings. If the budget cannot cover them, cut E6/E7 and disable unaccepted voice; do not spend the remaining time on another product direction.

## Voice acceptance handoff for Claude

Building voice is approved. Enabling it in the submission is conditional on observed operation. The fallback must preserve the whole entertainment experience.

| Case | Expected observable result |
|---|---|
| Warm hands near fire without deliberate Ask | Fire responds visually; microphone does not start. Warming and recording must be distinguishable |
| Decline microphone, decline transmission, no endpoint, or service unavailable | Touch questions and story remain usable; no dead end or repeated permission loop |
| Ask midway through `trade-household.load`, “Will she be turned away?” | No advance revelation of the smaller exchange. Current chapter text alone is too coarse a reveal boundary |
| Ask on the other perspective | Answer respects the chosen route, not an unvisited ending |
| Pause, Leave, start another telling, lose application focus, or cancel while recording/waiting | Recording stops, late answers are ignored, no answer or speech attaches to a new visit |
| Ask while a chapter is being narrated | One speaker at a time; after the answer, resume the paused moment or a clearly indicated short recap. Do not silently skip the rest of the chapter |
| Silent recording, noisy recording, request limit, provider error, timeout, or failed answer audio | A short recoverable state; cancel always available; accepted text/captions or authored questions remain usable |
| Ask about a real relative or an unsupported place | No biography inference or generic culture substitution; accurate scope and a relevant available alternative |
| Keep/reopen after an answer | State what is retained. No promise that generated questions/answers are saved unless the actual storage implementation proves it |

For same-chapter spoilers, the cheapest reliable first release is questions at completed narrative beats, or deterministic authored replies during a beat. If asking mid-sentence is essential, send only completed beat content and validate against that boundary. Do not treat prompting or a few phrase checks as proof.

Proposed UX targets, not rules: show recording/waiting feedback within 250 ms; retain an immediate Cancel action; after roughly 8 s waiting offer authored questions, and terminate an unresolved request by 15 s. Measure on the intended hosted path. Agree a budget and keep the required service reachable through judging before enabling it. Do not claim real-time/full-duplex dialogue for a recorded-question implementation.

## Small human check, not another simulated score

Use three unfamiliar visitors if possible: one with a connection to the setting, one without, and one primarily interested in entertainment. This is formative research, not representative evidence or community approval. Give only: “Sit by the fire and try a story.” Do not coach them toward the desired reaction.

Record task outcomes without names or personal histories:

- Can they begin, choose a side, ask or decline, and finish without instruction?
- Ask, “What was each woman trying to protect?” A useful answer identifies both households rather than a generous/sad stereotype.
- Ask, “What did your action change?” They should distinguish following a perspective from deciding the women's bargain.
- Ask, “What was documented, and what was imagined?” Any belief that the characters are their ancestors needs correction.
- Ask, “Would you choose another telling now? Which, and why?” Specific curiosity is more useful than a polite rating.
- Ask what they would remove. Repeated complaints about a passage, wait or control become the next fix.

A practical release target is all three completing the route and at least two explaining the perspective change without prompting. Failure guides a small revision; passing does not predict a prize. Community and pronunciation review remain their own work, and a draft label does not resolve an identified cultural problem.

## Remaining eight days

The [rules](https://start-developer-competition-26.devpost.com/rules) close entry on **November 18, 2026, at 3 p.m. Eastern**. Maintain the November 16 internal submission target. The APK needs a Competition-channel link; the video must be under three minutes and publicly available on YouTube or Vimeo. Accepted simulator footage is an alternative for the video, not evidence of real hands or device performance. [FAQ](https://start-developer-competition-26.devpost.com/details/faqs)

| Date | Outcome |
|---|---|
| Nov 10 | Claude completes voice checkpoint; PM/content handoff ready. Start listening, account/access and tester arrangements immediately |
| Nov 11 | Integrate E1 on gourd first and check its two perspectives. Run the voice cases. Finalize any short rerecording list before generating audio |
| Nov 12 | Direct object actions, device/remote tester session and first-time visitor check. Fix observed confusion before cosmetic work |
| Nov 13 | Decide whether voice is accepted for release. Build/upload candidate and begin independent access verification; freeze new features |
| Nov 14–15 | Fix release blockers, review final audio/captions, record and edit actual Quest or clearly labelled simulator footage |
| Nov 16 | Verify the exact release, links, adaptation and launch date; entrant submits |
| Nov 17–18 | Contingency before deadline; no speculative features |

The new candidate supersedes earlier debug APKs only when its signature, version and source are recorded. If device access remains unavailable, preserve that limitation in evidence and remove unsupported device claims. A simulator video is permitted; it does not close the hardware acceptance risk.

## Submission argument and claim boundaries

**Keep the tagline:** For the stories you never got to hear.

**Proposed short pitch:** The original Sankofa welcomed fragments of family history to a storyteller. Now that invitation has a place in your room: a small fire, a paper theatre and a choice of whose side to follow. In our first setting, a woman arrives with less than she promised. Turn the story toward the woman waiting for her, and the same meeting means something different.

Lead the demo with an actual stage turn and the two women's stakes. Then show the welcome, one hand action, one accepted question if available, and the kept return. Briefly show the original web work to make the adaptation legible. Keep platform logos, architecture and test totals out of the dramatic opening.

| Safe claim when verified in the final build | Claim to avoid |
|---|---|
| Three prepared tellings with two perspectives each | Six fully distinct episodes or unlimited history |
| Your interest selects a telling; your choice changes whose experience is revealed | A new story generated from your family history on the offline path |
| Optional spoken questions about the current story, if enabled and tested | Voice automatically creates a personal narrative; continuous live conversation |
| A bead remembers a side you kept; one saved telling reopens offline | Every bead is a saved personalised story you can replay |
| Source-linked context and explicitly imagined people | Recovered oral testimony or authenticated ancestors |
| Local table fitting when device-verified, with a virtual stand fallback | Universal room compatibility or measured Quest performance without evidence |

The winning argument to pursue is **personal curiosity becoming a shared, spatial storytelling moment**. Our proof is that someone cares about the other side enough to return. The plan succeeds if it strengthens that proof while delivering a build judges can actually use.
