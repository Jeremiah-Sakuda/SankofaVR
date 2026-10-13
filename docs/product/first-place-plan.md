# Sankofa: the plan for first place

**Superseded planning proposal:** the October 13 [illustrated theatre product scope](illustrated-theatre-scope.md) now controls proposed feature priorities, estimates and acceptance gates. This document remains a record of the earlier analysis. In particular, its predicted scores/winning odds, no-device strategy, attention-triggered speech, evidence glow and cut order are not implementation commitments. No behavior below is marked implemented by this notice.

October 13, 2026 · Proposal for the user's decision · Replaces the week-by-week allocation of the [five-sprint plan](delivery-plan.md#current-sprint-plan); keeps its gates, rules and cuts unless stated. Read with the [judging-criteria audit](../reviews/2026-10-13-judging-criteria-audit.md), the [PRD](prd.md) and the [decision record](../decisions.md).

The objective is the Entertainment first prize, nothing less. This document says what the winning version of Sankofa is, which moves get there, what each costs in the user's hours, and what is cut. The probabilities and scores in it are estimates from repository-only reviews, not measurements; the field is unknown.

## 1. Where we stand

Five independent repository-only reviews on October 13 (no simulator, no device) scored the build as it is and estimated a realistic ceiling by November 18:

| Criterion (25% each) | Today | Ceiling | What holds it down |
|---|---|---|---|
| Innovation & Creativity | 6 | 7.5–8 | Acting changes wording, not events; looking changes nothing heard |
| Experience Design | 6 | 7.5–8 | 33–47 s listening stretches; no visible consequence; first gesture at ~60 s |
| Technical Implementation | 5.5 | 6.5–7 | No device evidence can exist; everything else is sound |
| Polish & Presentation | 5 | 7–7.5 | Painted middle, 3D bookends, toy sheep, no video yet |

The potential review put first place at roughly 3–6% today and 8–12% with a strong finish. Its "no headset means zero" clause was wrong: the official rules accept demo footage "via XR Simulator or another equivalent emulator" (`references/hackathon/rules.txt:189`, local snapshot). What remains true is that judges install the APK on real Quests and the Technical criterion names 60 fps on Quest hardware (`rules.txt:282`). We cannot measure that, so this plan makes the build *defensible by design* and says so honestly.

Velocity: code is not the bottleneck. In fourteen days the repository took 58 commits and fifteen polish rounds, all verified by batch tests and desktop captures. Sprint 1 and most of Sprint 2 landed early. What has not moved is everything only the user can do: decisions, art generation, review outreach, release setup, narration approval, the video. **This plan is organised around the user's hours, not the agent's.**

## 2. What wins, and what the winning Sankofa is

### What the evidence says wins Entertainment

- **2025 winners built on foundations and delivered one pleasure superbly.** DreamSpace (first place) turned the room into handcrafted atmospheric environments; Photon (runner-up) made familiar spatial video better; (m)ORPH (honourable mention) let hands move sound objects with an immediate, audible result; Into the Mind (Judges' Choice) used spatial spectacle to change the meaning of its story. No narrative or cultural story placed. The lesson is not "avoid story"; it is that the panel scores a product with a clear promise, concentrated craft, and hands whose effect is felt at once.
- **The 2026 rubric adds repeat use and the first minutes.** Innovation asks for device capabilities that "drive repeat usage"; Experience Design asks for a "rewarding, habit-forming purpose"; the video must "lead with the story and what makes your project distinct". The special-award lenses (first five minutes, boldest concept, agentic interaction, reason to come back) are the same questions asked four ways. Only one prize per entry; they are lenses, not targets.
- **Web Sankofa won on a personal promise, not on polish.** The Gemini Live Agent Challenge entry turned a few family seeds into an illustrated, narrated story in acts, labelled each segment Historical, Cultural or Reconstructed, and let you talk to the griot. Its magic was *this story is about what you brought, and it tells you what is known and what is imagined*. The VR build kept the labels in the source register and the illustrated look, and dropped the personal input and the conversation. The Adapted division will ask what the update added; the honest answer must be: the storybook became a theatre you sit inside, your hands change it, and the fire listens.

### The winning version, in a judge's ten minutes

> You put on the headset and there is a fire, close. Your hands are lit by it; sparks drift toward your palm when you hold it out. "Come. Sit with me." The fire asks you to feed it: you lift a stick from the pile with an open palm and lay it on the coals; the fire flares and knows your hands. "What do you carry?" Three small things wait on the hearth stone; you take one, and the fire follows that thread. An ember settles in your palm, and the fire *paints* the world around you: sparks fly out and lay the painting in brushstrokes, far shore to near reeds, beside you and behind you. By the water, Wambui turns to look at you when you look at her. Pinch toward anything and the fire tells you about it, and what is documented glows while what is imagined stays paint. You take Wambui's gourd from her painted palm; tilt it, and you hear how little flour is in it. You set it on the hide and the hide crosses into the painting to rest between the women. Because you helped, Naserian hands you her milk to share; had you waited, she would have set it by the fire. The sheep comes to sniff your hand. The bead. The world folds into the fire; what you did rises as light, one shape per action, with what it caused, and one whisper of what would have happened otherwise. You place the bead on the hearth stone to keep the visit. "Did Wambui come back? When you return, I will tell you." The same water, today, is painted too, and your bead hangs where you left it.

Everything in that paragraph is reachable with the existing engine, the existing recorded voice, image generation we already use, and the user's hours. The rest of this plan is how.

## 3. The moves, ranked by score per hour

Effort is given as **user hours** (decisions, writing, art curation, listening, outreach, video) plus **agent cycles** (one cycle is roughly an hour of unattended build, test and capture that ends in a commit). Tags: [software-now] needs nothing from the user beyond a yes; [needs-art] needs image generation and curation; [needs-narration] needs new recorded lines, which the user approves; [needs-decision].

### Move 1. The fire that listens — make the title true

*Innovation, Experience. SK-08, SK-09 (hand-selectable questions). Modules: input, flow, stage, shaders.*

- **Look → ask → act becomes one embodied grammar.** Looking at something makes it glow and a painted woman turn (exists). **Pinching toward a thing asks the fire about it**: the pointer that already presses panels is extended to story objects, and the object→question map that already exists (`q.e1.flour` → gourd, `q.e1.milk` → milk, `q.e1.trust` → Naserian, `q.e1.bead` → bead, `q.today.who` → soil) plays the recorded answer. A spark flies from the fire to the thing as it answers. The panel stays as the one-hand and controller alternative. Consent-safe: a deliberate hand action, never a glance. [software-now]
- **The fire leans toward what you look at.** The flame's tip bends a little toward the current attention candidate and straightens when you look back; narration ducks slightly while you lean toward something. A visible "it noticed" that costs one shader uniform. [software-now]
- **Asides from attention.** In the story's existing rests (`e1.after`, `e1.gift` rest, the 3 s in `e1.problem`), if the visitor lingered on something whose answer they have not asked for, the fire offers it as an aside, journalled as *offered by attention*, never as a choice. Uses recorded answers only. [needs-decision: this bends "looking never advances the story"; it never resolves an invitation]
- **Evidence glow.** When the fire answers about something with a source (`R1`, `C15`), the thing glows gold for a moment; something invented keeps its brush texture. Web Sankofa's three labels become a thing you can see. [software-now]

Effort: user 2 h (decision, pick which answers may be asides, one listening pass). Agent 5–6 cycles. Expected: Innovation +0.75, Experience +0.5.

### Move 2. The first five minutes — hands before words

*Experience (onboarding, time to agency), Innovation (personalisation by hand, SK-03/04 lite). Modules: flow, stage, content.*

- **Sparks answer your hands** from the first second: hold a palm toward the fire and embers drift to it; the hand glows warm. Zero narration. [software-now]
- **Feed the fire.** The first invitation, within 20 s of Begin: an open palm near the wood pile lifts a stick; carry it over the coals and it drops in; the fire flares and thanks you. It teaches receive and carry before the story needs them, and it is the griot's hospitality made physical. Passive path: the fire feeds itself and carries on. Three short new lines. [needs-narration]
- **"What do you carry?"** Three small things wait on the hearth stone: the flour gourd, the bead, a stone from the water. Taking one chooses the thread the fire follows: which asides it offers, which question glows first, how the hearth recap and the return hook are framed. This is personalisation by hand with one episode: two observable dimensions (perspective and closing thread), honestly described. Passive path: the fire chooses the gourd. About eight new lines. [needs-narration] [needs-decision]
- **Silence begins the only story** after 15 s (done, `2a04c7b`).

Effort: user 3 h (approve and listen to ~11 lines, written by the agent for review). Agent 6–8 cycles. Expected: Experience +0.75, Innovation +0.25.

### Move 3. One painted world — and the fire paints it

*Polish, Innovation. SK-11, SK-07. Modules: theatre builder, shaders, art.*

- **Paint the bookends.** A painted night hearth (two scenery layers: far hills and trees, near grass and stones) replaces the low-poly trees and white rocks; the fire, logs and stones get the painted-prop shader; the lap buttons get one hide-and-ink style and move 10° inward. The first and last frames stop being the weakest. [needs-art: 2 layers] [software-now: shaders, buttons]
- **A painted sheep card** with two poses (standing, head to the visitor), replacing the procedural animal. [needs-art]
- **A painted present-day river**, two or three layers, so the continuation stops being the 3D city. [needs-art]
- **Higher-resolution cast cells and a look-at-you row.** Regenerate the cast at 1024 px per cell; add a fourth row (head turned to the viewer) for acknowledgement. Consistency risk is real; keep the current atlas as fallback. [needs-art] [needs-decision: art provider and output terms]
- **The fire paints the world.** The unfold becomes brushstrokes: sparks fly from the fire and paint appears where they land, far to near, including wing cards beside and behind the seat (mirrored reed regions from the existing atlas). This is the hero shot of the video. [software-now]
- **Alpha-to-coverage and silhouette-trimmed cards** so edges are soft and overdraw drops (see Move 6).

Effort: user 10–12 h (prompting and curating five or six generations, deciding provider terms). Agent 8–10 cycles. Expected: Polish +1.5, Innovation +0.25.

### Move 4. Consequence you can see, and a reason to come back

*Experience (clarity of influence), Innovation (events change), reason-to-return. SK-04, SK-08, SK-12. Modules: content, flow, stage.*

- **One real divergence after the hide.** If you helped set the flour down, Naserian hands you her milk gourd to share (a receive invitation that exists only on the helped path); if you waited, she sets it by the fire. An object changes hands only because of what you did. The bead stays the keepsake on every path so continuity is safe. About six new lines. [needs-narration] [needs-decision]
- **The recap becomes a performance.** Echoes rise one by one in the order you acted, each with the line it caused; one caption whispers the road not taken ("Had you waited, Wambui would have set it down herself"), taken from the existing default-line text. Pinching an echo replays its line. [software-now]
- **Keep is physical.** Placing the bead on the hearth stone keeps the visit; the plaque remains. Relabel "Something new" when there is only one story; show Keep from the start of the closing. [software-now]
- **The hook.** The last hearth line: "Did Wambui come back? When you return, I will tell you." The continuation answers it in one line, labelled as imagined. Two new lines. [needs-narration]
- **The return remembers in the world, not only in words.** What you lingered on and what you did appear in the painted garden: the bead where you hung it, the gourd's echo by the gate if you carried the flour. [software-now, after Move 3's river]

Effort: user 3 h (writing review, approve and listen to ~8 lines). Agent 6–8 cycles. Expected: Experience +0.75, Innovation +0.5.

### Move 5. Touch anything — agency density

*Experience, Innovation (the (m)ORPH lesson). SK-08. Modules: stage, shaders, audio.*

- **The painting answers every touch.** A hand crossing a card's plane ripples it like water at the touch point; reeds rustle, water sounds, the sheep bleats and turns when touched. Immediate, local, no story consequence, no narration. [software-now; one or two new field-recorded clips with rights recorded]
- **The gourd you can feel.** Tilt it and hear the little flour shift; the sound tells you what Wambui could not say. [software-now]
- **The sheep comes to your hand.** Hold a palm out low and the ewe walks over to sniff it (it already walks). A comic beat judges will show each other. [software-now]
- **Touch rings** for touch invitations (water, hide, gate), matching the palm ring; move the gate within 45° of forward. [software-now]

Effort: user 1 h (listen, record clip rights). Agent 5–6 cycles. Expected: Experience +0.5, Innovation +0.25.

### Move 6. Defensible by design — the answer to "no device"

*Technical. SK-13, SK-14, SK-15. Modules: shaders, app, input, tests.*

- **A quality governor.** The app already logs its frame times. On Quest it will measure its first ten seconds at the hearth and, if over budget, step down a tier (MSAA 4→2, sky noise off, card mip bias) before the story begins, and log which tier it chose. We cannot measure a Quest; the app can. [software-now]
- **Cheaper painted cards.** Trim each card's quad to its painted silhouette (computed from the atlas alpha at build time) to cut overdraw; alpha-to-coverage for edges; a written render budget per scene. [software-now]
- **No stall when the fire becomes the world.** Pre-warm the story scene's shaders behind the title card. [software-now]
- **Hands that forgive.** The 0.18 s tracking grace and 0.15 s pinch slip are in; add a hand-tracking self-test on the title card that shows the visitor their own hands are seen before Begin. [software-now]
- **Pause and lifecycle.** A "Paused — touch Resume" caption, including for a pause requested during a transition; PlayMode tests for headset removal and focus loss; journey tests on a scaled clock so they cannot flake on wall time. [software-now]
- **Height and recentre.** Anchor height from the measured head height at placement; a recentre hint on the title card. [software-now]

Effort: user 1 h (read and accept the budget). Agent 6–8 cycles. Expected: Technical +1.0 (to the no-device ceiling), Polish +0.25.

### Move 7. The submission is half the score

*Polish & Presentation, and Stage One viability. Modules: release, docs.*

- **Release now, not last.** Release keystore (with custody noted), permanent app id, auto-incrementing version code, Quest 1 out of the device list, Competition channel created, APK uploaded, invite tested from a second account. [needs-decision; user 3–4 h]
- **Cultural review and rights.** Send the family and listener packets this week; record the image-model output terms and the preview-TTS terms in the asset register; confirm Start membership and eligibility; confirm citations never appear in the runtime UI. [user 4–5 h over the five weeks]
- **The video (2:40).** Captured in the XR Simulator with synthetic hands, labelled as such, at 60 fps, with the real narration and ambience mix. Order: the gourd leaving the painting (hook, 0:00–0:06); fire, hands, feed the fire (to 0:35); the fire paints the world (to 0:55); water, gourd, the women respond, the hide crossing, the milk, the bead (to 1:55); helping versus waiting side by side (to 2:15); the return, echoes, physical keep, the hook, the painted river today (to 2:40); an honest end card. [user 6–8 h; agent produces the captures]
- **Devpost copy and the adaptation story.** Before/after: the web storybook and the theatre. What was added: a hands-first spatial theatre, a fire that notices, embodied consequences, a kept visit that returns. What was removed and why: live conversation and open seeds, in favour of one reviewed world. [user 2 h; agent drafts]
- **Testing guide** for the illustrated route; cover image; screenshots from the strongest frames. [agent; user 0.5 h]
- **Novice sessions** on desktop or simulator: four or five people, the PRD's "explain a consequence" test. [user 3–4 h]

Effort: user 18–22 h. Expected: Polish +0.75 and the difference between being judged and not.

### Considered and set aside

- **Mixed-reality hearth (passthrough).** DreamSpace won on the room, and "the painting rises in your living room" is a fine first-five-minutes image. But the 2026 judging guidance expects scene understanding or anchoring that works in unfamiliar rooms, which cannot be verified without a device; passthrough alone reads as a badge. Set aside unless the XR Simulator validates a floor-anchored hearth with a guaranteed VR fallback and capacity exceeds 20 h/week.
- **Live conversation with the fire** (reusing web Sankofa's Gemini Live backend). The most "agentic" addition and the closest tie to the original. It needs the microphone, the network, retention terms and latency tuning on a device we do not have, and the rules want the core to stand without it. Set aside until the core gates pass; a bounded version (questions only about the current beat) is the first stretch if time appears.
- **The 1899 chapter.** 12–20 h of writing, review, art and narration. The continuation already gives a reason to return. Promote only if Moves 1–7 are done by November 2.
- **A second narration language, generation, splat worlds, WebXR.** Out.

## 4. Schedule and hours

Capacity is planned at **12–15 user hours a week** (unconfirmed), five weeks to the November 16 internal target. The five-sprint plan allocated 48–58 h and left 12–17 h of reserve inside a 60–75 h envelope; this plan reallocates the device-session hours to art, novice sessions and the video.

| Week | User hours | User does | Agent builds (cycles) | Gate, with evidence |
|---|---|---|---|---|
| **1 · Oct 13–19** | 12–14 | Decisions D1–D6; release setup; send both packets; approve ~11 lines for Move 2; art round 1 (hearth layers, sheep) | Move 1 (6); Move 6 governor, A2C, warm-up (4); Move 2 flow once lines exist (4) | The fire listens and leans in captures; title card shows hands; channel invite works from a second account; packets sent |
| **2 · Oct 20–26** | 12–14 | Art round 2 (cast re-render, look-at-you row, today river); approve Move 4 lines; listen to new narration | Move 3 integration (6); Move 5 (5); Move 2 "what do you carry" (3) | One painted world from title to return in captures; touch answers in captures; first five minutes under 60 s to first gesture |
| **3 · Oct 27–Nov 2** | 10–12 | Review notes arrive: cultural corrections, name delivery; approve the hook; novice sessions 1–2 | Move 4 (7); Move 3 fire-paints unfold and wings (3); corrections (3) | Helped and quiet paths diverge visibly; recap pairs cause and effect; the return reflects actions; corrections in |
| **4 · Nov 3–9** | 10–12 | Novice sessions 3–5; rights records closed; decide the draft label wording from review | Dependability: lifecycle tests, scaled-clock journeys, hands-only simulator runs, fixes (8); release candidate build | 4 of 5 novices explain a consequence unprompted; all suites green; candidate APK on the channel |
| **5 · Nov 10–16** | 12–14 | Video edit and voice mix; Devpost copy; freeze; submit Nov 16 | Capture set at 60 fps (3); testing guide, cover, screenshots (2); tag and archive | Entry complete; another person installs the frozen build; nothing changes after |
| Nov 17–18 | reserve | Contingency for access or submission problems | none | |

Totals: **user 56–66 h** against a 60–75 h envelope; **agent ~65 cycles**, comfortably inside five weeks of unattended builds. If actual capacity is 20 h/week, add the bounded live-conversation stretch in week 3 after Move 4's gate, nothing earlier.

**If a week slips, cut in this order:** the today-river repaint (keep the 3D coda out of the video instead); the look-at-you row; the sheep-to-hand beat; "what do you carry" (keep feed-the-fire); the evidence glow. Never cut: release setup, the video, the cultural review, Move 1's pinch-to-ask, Move 4's divergence, Move 6's governor.

Expected standing after the plan, by my estimate: Innovation 7.5, Experience 7.5, Technical 6.5–7, Polish 7–7.5. That is the reviewers' ceiling, not a guarantee of a place; the field is unknown.

## 5. Decisions needed this week

| # | Decision | Why now |
|---|---|---|
| D1 | Release keystore and custody; permanent app id; version-code policy; supported devices without Quest 1 | A debug-signed APK cannot be judged. First upload fixes the app id |
| D2 | Attention may play an already-recorded answer as an aside (never resolves an invitation) | Move 1's largest effect |
| D3 | Approve new narration: ~11 lines (Move 2), ~8 lines (Move 4), 2 (the hook). The agent drafts; you approve each; the existing pipeline records them | Lines gate three moves; listening review takes an evening |
| D4 | Art provider and its output terms for release; keep the current provider or switch | Move 3 and the rights record |
| D5 | "What do you carry?" as the hand-chosen thread (Move 2) | Changes the opening's shape |
| D6 | Who receives the review packets and by when; whether the draft label may change after their notes | The label is in every frame of the video |

## 6. Kill risks and how each is retired

| Risk | Retirement |
|---|---|
| The build stutters on a judge's Quest | Move 6: governor, trimmed cards, A2C, warm-up, a written budget; the video is labelled simulator footage; the Devpost text states what was and was not measured |
| Broken install or judge access | D1 this week; a second-account install test by November 1; a frozen tag and hash |
| Stage One viability (hands-first) | Hands-only simulator runs each week from title to exit, including passive and one-hand paths; the title card's hand self-test |
| "Draft story" in every frame | Packets out this week; corrections in week 3; the wording after review is the user's decision (D6); if unreviewed, the label stays and the video says so |
| Rights | Image-model and TTS terms recorded by week 4; field-recording attributions already in credits |
| §4.17 identifiable people | Fictional composites only; citations stay in the register; generated faces checked against the rule before the cast re-render ships |
| Division reassignment | The adaptation story names platform integration and hand interactions, which the rules list as examples; lineage is documented |

## 7. What each move proves, and how

| Move | Requirement | Criterion | Observable evidence |
|---|---|---|---|
| 1 | SK-08, SK-09 | Innovation, Experience | Capture: a glance turns a woman; a pinch lights a thing and the fire answers; the flame leans. Tests: pinch-to-ask resolves the mapped question; a glance never resolves an invitation |
| 2 | SK-01, SK-02, SK-03/04 lite | Experience, Innovation | Capture: stick into fire at ~20 s; three things on the stone; thread chosen by hand. Tests: passive path feeds itself and picks the gourd |
| 3 | SK-11, SK-07 | Polish, Innovation | Captures from title to return in one style; the unfold strip; render budget table |
| 4 | SK-04, SK-08, SK-12 | Experience, Innovation | Tests: milk changes hands only on the helped path; recap pairs each action with its line; the garden shows the bead where it was hung |
| 5 | SK-08 | Experience | Capture sequence of a touch ripple; tests for touch feedback without story change |
| 6 | SK-13–15 | Technical | Governor log line; lifecycle tests; A2C edge captures; scaled-clock journeys |
| 7 | Entry conditions | Polish, viability | Channel install from a second account; the video; Devpost text; testing guide |

All evidence is software, desktop and simulator evidence, labelled as such. Cultural review and participant evidence are recorded separately when they exist.
