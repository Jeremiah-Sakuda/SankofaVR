# Sankofa product stress test

October 1, 2026 · Adversarial review and proposed experiments; no participant or runtime results

## Assessment

The cultural storytelling direction merits a prototype. Neither ancestry-led positioning nor the broader invitation has demonstrated an advantage yet. The most promising differentiator is a culturally specific story whose response to an intentional physical action changes the visitor's understanding of it. The largest current risk is producing attractive scenery around exposition while spending the available time on generation, languages and infrastructure.

This review challenges the PRD and the previous recommendation to broaden the pitch. It does not approve a new scope or change any user constraint. The [PRD](../../Sankofa-PRD.md), [delivery plan](../../Sankofa-Delivery-Plan.md) and [decisions](../decisions.md) remain the current contracts. Competition-specific findings are local under `references/hackathon/stress-test-2026-10-01.md`; the agent entry point remains `HACKATHON.local.md`.

Evidence used: current repository documents, inspected file inventory and the primary sources linked below. This repository contains planning and a workspace checker; there is no Sankofa runtime to evaluate. This is one adversarial analysis, not an independent judging panel, market study, simulated user trial or winning-probability estimate.

## Three interpretations to compare

| Interpretation | Strongest case | Strongest objection | Current assessment |
|---|---|---|---|
| Heritage/ancestry-led | Personal stakes, memory and belonging give a visitor a specific reason to care | Visitors may expect genealogical truth, need personal information, or assume the experience is only for people investigating family history | Keep as a serious candidate. Ancestry is not inherently too narrow or unsuitable for entertainment |
| Culturally rooted immersive stories, with optional personal connection | A clear dramatic invitation works for visitors connected to the culture and visitors arriving through curiosity | Broad copy can erase the original product's specificity and turn it into an interchangeable story catalogue | Best provisional framing hypothesis, not an established winner |
| Open-prompt generated worlds | Maximum apparent freedom and visible AI capability | Finite scene inventory cannot honour an unrestricted promise; scene quality, grounding, continuity and latency all become harder | Weakest near-term implementation fit. Broad prompt freedom alone does not establish stronger entertainment |

These differ in the invitation and content promise, not just marketing language. An ancestry experience needs evidence about an actual family if it claims to reconstruct one. A culturally grounded fictional episode can state its relationship to reviewed history clearly. The original [Sankofa submission](https://devpost.com/software/sankofa-y47f9p) describes family-history inputs, a cinematic welcome and live conversation, and records its prior award. That record supports continuity; it does not establish which element caused its success or predict this product's reception.

## Assumptions under pressure

| Assumption | What could break it | Decision status / next evidence |
|---|---|---|
| A broader framing improves the product's appeal | People who want emotional connection lose a clear reason to care; curious visitors find the pitch vague | Untested. Compare matched invitations to the same episode |
| Spatial storytelling is mostly unexplored | Existing theatre and VR cinema already offer embodied discovery and narrative presence | Reject as a blanket novelty claim; define the specific new response and experience |
| The fire can carry a warm relationship | A voice over animated fire feels like an audio player; flame changes are illegible or mechanical | Untested. Compare a directed fire with a simple fire using identical speech |
| A fire-entry transition produces durable engagement | The opening is impressive but the next scene has no dramatic question | Untested. Evaluate the scene after the transition and the ending, not just entry |
| Noticing an object means wanting the story to follow it | Head movement can be exploration, discomfort or distraction; narration feels intrusive | Use deliberate inspection or a hand action for consequential changes; test accidental attention separately |
| More generated variation makes a better story | Variation damages foreshadowing, pacing, consistency or the reveal; visitors prefer the same well-directed sequence | Untested. Compare bounded adaptive and authored versions with equivalent production quality |
| More live conversation makes the narrator feel present | Long explanations interrupt the drama; generic answers reveal weak scene understanding | Untested. Keep a coherent passive performance; evaluate live questions for added value |
| Three routes establish meaningful personalisation | Assets differ but the emotional arc and consequence remain identical, or choices are unrecognised | Existing route count is a planning default. Judge observable relevance and consequence, not counts alone |
| A keepsake establishes retention | A shelf is pleasant once; a second visit offers only repetition or more exposition | Mechanism specified, demand untested. Observe an actual later visit |
| Familiar languages create familiarity | Pronunciation, cadence, voice persona or translation feels culturally wrong | Require review independently of language-list support; don't infer the preferred language from heritage |
| Generated visuals lower production cost | Cleanup, object extraction, cultural corrections, lighting and device optimisation exceed manual work | Run one measured asset comparison, including cleanup time and actual headset review |
| A modular design makes new stories cheap | Software is reusable but each setting needs research, writing, assets, performance and review | Architecture can reduce code coupling; content-production cost remains unmeasured |
| Existing Sankofa substantially reduces VR effort | The UI is rewritten, stream formats lack scene cues, and labels need stronger evidence | Reuse is a candidate, not measured savings. Audit one reusable module before assuming the whole backend transfers |
| We can deliver the current P0 part-time | The estimated scope already needs substantially more than 10–15 hours/week | Capacity is unresolved. Measure a complete small slice before adopting the full schedule |

## Counterexamples to our novelty argument

[The Under Presents](https://tenderclaws.com/theunderpresents) combines VR theatre, narrative exploration and performance. [Wolves in the Walls](https://www.fable-studio.com/wolves-in-the-walls) offers a participatory VR story involving Lucy. [The Invisible Hours](https://store.steampowered.com/app/582560/The_Invisible_Hours/) lets a visitor follow different characters and discover a narrative through observation, and supports use with or without VR.

These are design precedents, not proof of current demand, sales rankings or direct competitors in this competition. They defeat the broad claim that a story surrounding the visitor or responding to participation is itself new. The non-VR version of The Invisible Hours also cautions against claiming that an attention-led story is logically impossible on another platform.

Sankofa's candidate distinction is the combination of cultural specificity, the fire relationship, conversational responsiveness and a physical consequence that is later reinterpreted. Even that combination must be demonstrated. A video can communicate its idea, but the headset should add materially stronger depth, scale, spatial sound or object handling. That experiential gain is more defensible than claiming nobody could make a screen version.

## The experience can undermine itself

**Lean-back pleasure versus required participation.** Hands-only completion does not imply frequent puzzles or choices. A visitor might want to listen. If every beat needs input, the story becomes an interface task. If the visitor can only listen, the attention promise becomes vague. Test one optional, consequential hand invitation with a satisfying default path. Passive progression must not claim that the visitor made a choice.

**Presence versus inference.** Quest 3 and 3S have no eye-tracking hardware according to [Meta's input documentation](https://developers.meta.com/vr/essentials/horizon-os-input/). Head direction is an incomplete signal. The current PRD already respects this; the pitch must also avoid promising mind reading. Local feedback can acknowledge inspection; story commitment needs deliberate input. A narrator that repeatedly announces what someone looked at can feel performative rather than perceptive.

**A speaking fire versus a dramatic cast.** Removing a humanoid host reduces animation work but also removes facial acting, gesture and mutual gaze. The fire must earn presence through specific timing, voice position, waiting and responsive cues. A story without visible characters may still work, but sound, objects, environmental change and narration must carry conflict and relationships. This is a creative-production demand, not a problem solved by the state machine.

**Personalisation versus dramatic structure.** Swapping an object and rearranging beats meets an observable variation test but does not prove a better experience. A reveal needs preparation. Define which parts can vary without destroying causal continuity. Ask whether a visitor remembers what happened and why, and whether the variation felt relevant. Narration that merely names a chosen object is insufficient.

**Conversation versus pacing.** Contextual questions are promising when they deepen a moment. They can weaken it if exposition precedes every reveal, the speaker gives spoilers, or the voice/latency changes. Prototype an authored contextual answer first to establish desirable behaviour, then see whether a service can produce it. A scripted benchmark is test scaffolding, not a claim that the generation requirement is fulfilled.

**Retention versus repeated novelty.** A new story might require costly new content. Another generated wording of the same world may feel repetitive. A saved keepsake is a route to a second visit, not evidence that someone wants it. Separate understanding of the return feature, stated interest and an actual later choice to return.

**Cultural breadth versus a single storyteller template.** Familiarity is not established by translating one narrator into several languages. Traditions differ in cadence, humour, participation and the relationship between narrator and audience. The griot is a specific cultural reference, not evidence that one presentation fits every history. Keep the original influence explicit and review any later adaptation with people familiar with its context. A departure or migration story is a candidate; heritage can also involve joy, wit, craft, invention and ordinary life. Do not make loss the default dramatic meaning of ancestry.

**A journey diagram versus an actual episode.** The PRD defines arrival, discovery, consequence and reveal, but no selected episode currently establishes whose desire drives the story, what prevents it, what a visitor's action changes, and how the ending earns its meaning. A coherent interaction sequence is not yet compelling drama. Answer those questions in T2 before commissioning final scenery or a larger catalogue.

## What the current evidence cannot support

We cannot say that broad framing will outperform ancestry, that the fire will feel socially present, that generation improves the narrative, that any visual pipeline runs well on Quest, or that the full build fits a solo part-time schedule. Prior music-project tests and the original web award do not establish those results. A small interview sample cannot predict a category outcome.

The delivery plan estimates 166–281 hours with reserve for its proposed P0. That is a planning judgement, not a measured estimate. From October 1 to November 16 there are about 6.6 weeks; at 10–15 hours/week, roughly 66–99 hours would be available. Even removing every P1 feature leaves a capacity problem. Actual weekly hours remain unknown. The choice is to establish a smaller complete competition scope, increase resources, or accept a different finish level; the PRD should change explicitly if a core requirement is reduced.

Visual coherence and sound are likely critical work given the user's rejection of the earlier music prototype's presentation. That is a prioritisation inference from the conversation, not a performance diagnosis of this unbuilt product. An expensive new service cannot substitute for a reviewed scene and a well-directed story.

## Proposed experiments, in dependency order

These are formative tests, not release acceptance or statistically representative studies. No participants have been recruited and no tests below have run. Do not send recruitment messages without authorisation. Log failures, first exposure, device and assistance; separate connected and curiosity-led visitors without inferring ethnicity from names. Begin with 6–8 visitors for framing and small 5–6-person device iterations when hardware is available. Revise samples if practical; do not turn counts into confidence claims.

| Probe | Controlled comparison / method | Decision it should settle |
|---|---|---|
| T0 — capacity and content feasibility | Confirm recurring device access, weekly hours and budget. Select a precise candidate setting, one dramatic question and sources/reviewer availability | Whether the current schedule and content pipeline can exist; unknowns remain open until answered |
| T1 — invitation | Three short, equally concrete descriptions of the same supported episode: heritage connection, curiosity-led cultural story, and bounded AI-personalised story. Rotate first exposure, record unaided expectations, then compare preference. No version promises verified ancestors or unlimited worlds | Which invitation is clear and desirable, and which false expectations it creates. If ranking is inconsistent, keep the positioning decision open |
| T2 — story before spectacle | Tell a short episode with a real beginning, tension, consequential object and reveal using inexpensive storyboards/audio. Ask visitors to explain what changed and whether they want to continue | Whether the narrative works beyond the opening. If interest depends entirely on the transition, rewrite the story before making more scenery |
| T3 — fire presence | Same 30–45-second audio and environment; compare simply animated fire with intentionally cued listening/waiting/telling. Use counterbalanced order on the headset | Whether direction makes the fire feel responsive and its states understandable. If both read as a screensaver, revise staging/voice before expanding the persona |
| T4 — embodied consequence | A short stationary scene with one real-depth object, deliberate action and later reveal; include a passive path and accidental-looking cases. Ask what the participant caused without first naming it | Whether the visitor understands their influence and gains something from physical presence. A preliminary target of 4/5 explaining it is a revision trigger, not population evidence |
| T5 — personalisation and live-AI value | Compare two intentional inputs in one content kit; review actual visible differences. Separately compare an authored performance with an adaptive version of equal quality, holding the opening and ending intent constant. Compare contextual scripted answers with live ones only when the adapter exists | Whether variation and conversation improve relevance without harming coherence or pacing. If the authored version is preferred, redesign the adaptive boundary; any release-scope change is explicit |
| T6 — one complete visit and later return | Integrate one episode on Quest, then observe a subsequent available visit with a concrete next thread. No rewards for choosing to return | Whether the whole loop is pleasurable, performant and coherent, and whether continuity motivates anything beyond polite interest |

T1 and a storyboard T2 can begin without a headset. T3–T6 require real device access for presence and comfort conclusions. Simulated responses or operator-directed cues can test the intended design if disclosed as a prototype, but cannot establish implemented AI reliability. Full PRD acceptance still follows a selected implementation.

Record for every probe: hypothesis, comparison, controlled variables, observed behaviour, failures, practical limits and resulting decision. Do not favour the new concept with better assets, a warmer voice or a shorter wait and then attribute the gain to framing or AI. A visitor's comprehension of the promise is separate from enjoyment of the product.

## Recommended direction, pending evidence

Keep cultural specificity, the fire invitation and optional personal connection. Test the broader invitation instead of adopting it as fact. Give the first episode a named dramatic question and an earned revelation. Establish one complete path with meaningful spatial behaviour before investing in additional routes, languages or services.

Treat two narration languages, three routes, exact saved replay and continuation as current proposed P0 commitments with unresolved capacity, not features automatically justified by the prize. The user's no-AI-music constraint remains firm. The eventual release must retain genuine personalisation and hands-only completion unless the user explicitly changes that product contract.

The next useful evidence is a visitor describing what the story made them feel and what their action changed. More detailed architecture, provider lists or a more ambitious trailer will not settle that question.
