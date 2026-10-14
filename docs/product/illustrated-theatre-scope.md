# Sankofa living gallery product scope

October 13, 2026 · Gallery direction selected by the user; implementation and acceptance pending · Runtime baseline inspected: `6c3b356`

**Sankofa — Step inside the story.** A living art gallery whose paintings become spatial experiences. You encounter a work, enter its composition, participate in its story, and return to the same painting with a different understanding of what it holds. The story responds to the perspective you choose and what you actually do.

**Purpose:** make room for stories time nearly left behind: ordinary encounters, relationships and perspectives that a visitor may never have heard. The human situation supplies broad appeal; specific place, material life and reviewed context supply identity. A gallery is the way into those stories. The first authored encounter is openly imagined from documented context, not advertised as a recovered account of actual people.

The first artwork is **What We Carry**, the existing illustrated encounter at Enkare Nairobi. Its two authored threads reveal different aspects of Wambui and Naserian's meeting. The griot remains a warm narrator: welcoming visitors, making unfamiliar context legible and bridging gaps without disclosing the story's turns. Inside this first artwork, the fire is its visible presence. The women retain agency over their agreement.

This is the current planning reference. The user selected the gallery as the organizing premise; implementation details, estimates and acceptance targets below remain to be verified. It supersedes the earlier theatre-only framing and [first-place proposal](first-place-plan.md). The [PRD](prd.md), [delivery plan](delivery-plan.md#current-sprint-plan) and [backlog](../backlog.md#current-illustrated-theatre-sprints) follow this direction. Broader freeform generation, live conversation and additional-language requirements remain open; recorded contextual narration does not complete them. The existing comparison scenes remain available; the implementation checkpoint below records the new candidate.

## Implementation checkpoint

October 13: the new `LivingGallery` scene and v0.4.0 authored package implement the gallery/frame journey, two vignette routes, explicit object question chips, milk hospitality, contextual narration, a painted modern background and a physical kept memento. The older theatre remains a comparison. Core route/outcome/legacy-replay and muted Unity checks establish software behavior only; device, cultural, participant and final art/listening acceptance are still open. See the [decision record](../decisions.md) for exact checks and artifacts. The sections below remain the intended acceptance contract, not a declaration that the full PRD or all eleven work packages have passed.

October 14: v0.5.0 adds authored acting, animated miniatures, earlier ember input, a focused shorter memory revisit, clearer physical replacement and guarded pointer/transition recovery. See the [panel implementation map](../reviews/2026-10-14-panel-implementation.md) for tradeoffs and evidence. These changes do not close the external acceptance gates.

October 14 completion pass: v0.6.0 tightens the full encounter, moves optional explanatory context off the critical path, refines focal props and adds bounded caption avoidance. Review packets, integrity audit and release drafts are prepared. See the [current candidate](../release/candidate.md) for exact timing, checks and remaining gates. These are software refinements, not accepted human/device evidence.

## Product promise and boundaries

**Working public description:** “A living gallery for stories time nearly left behind. Step into a painting and discover the lives within it.” The gallery is the product; the illustrated theatre is how its first artwork performs. “A story that notices what you notice” remains an interaction test: readable acknowledgement of attention, deliberate choices, and truthful consequences. It does not imply eye tracking or unrestricted conversation.

The competition candidate contains **one complete exhibit**: a framed composition, a seated way to enter, two perspectives on the same encounter, and an illustrated contemporary continuation. A modest gallery setting replaces the standalone opening/closing hearth environment. The fire lives inside the artwork and can speak from within its frame before entry. There is one welcome and one transition into the story.

The long-term gallery can be explored through multiple artworks with their own voices, aesthetics and reviewed contexts. The first release must be fully usable while seated: looking around and selecting a painting provides access without compulsory walking, joystick travel or camera movement. Optional physical movement is not required to see any essential detail. Collection navigation and locomotion need their own future scope when another finished exhibit exists.

English narration and captions; Maa names and Kenyan context require appropriate review. Sankofa's Akan origin is named separately. Neither the title nor the griot voice implies that distinct traditions are interchangeable. Visitors need no cultural background, surname, controller or microphone.

| Protect in the candidate | Conditional after the core passes | Outside this iteration |
|---|---|---|
| One gallery setting and framed artwork; warm contextual griot; composition-to-depth entry and return; two meaningful threads; flour hand-off and visible hospitality; object questions; passive and one-hand paths; explicit kept visits; coherent modern coda | Animal approaching a hand; peripheral scenery wings; richer optional echoes; measured adaptive quality control | Additional gallery exhibits; collection browser/marketplace; avatar navigation; live AI conversation; freeform generation; new historical eras; second language; MR room mapping; multiplayer; character rigs; universal touch effects; AI-generated music |

Authored personalisation means selecting different scenes, revelations and their order from reviewed content. It is not generated family history. The gallery must not imply that a contemporary generated composition is a historical painting or that all painted details are documented. Replay of the saved telling and an alternate perspective are visibly different actions. Replay retains text/audio/outcomes while using current staging; it excludes questions and interruption timing.

## Starting foundation and remaining acceptance

Before the gallery implementation, the inspected theatre build had the fire, stationary transitions, three-layer illustrated encounter, two cast columns with three poses each, tangible flour and bead, authored questions, accepted/passive outcomes, a local kept-visit store, replay and a modern continuation. Recent work added visible sheep, painted props, palm alignment during turns/recentering, acknowledgement, better hand-off timing and input grace. The latest code is later than the last documented review APK; rebuild before distribution.

The gallery/frame, exhibit preview, composition-matched entry/return, meaningful thread selection, two vignettes, direct scene questions, milk hospitality, painted modern coda and exhibit-side physical Keep now have an implemented software candidate. Hardware, cultural, participant and release acceptance remain unverified. The vignette art and procedural sheep remain prototype assets for that first probe.

Extend the existing application, story package and editor builders. Retain the original `Hearth` scene as a comparison record. Gallery presentation replaces the current lobby/closing presentation in the candidate; it does not create a second application, visit store or mandatory intermediate hearth room.

## Gallery and exhibit structure

The gallery needs one beautiful, readable viewing position: a forward-facing framed painting, a restrained architectural surround, a small accessible ledge for controls and a kept memento, and enough quiet space for the work to stand out. Palette, light and material choices are tested against the painting. No long corridors, empty locked frames, reception desk, virtual visitors or introductory navigation tour.

The placard names *What We Carry*, its setting and its status as an imagined encounter. Accurate production credits and review details are available through About this artwork. Essential actions are close to the seated visitor; the visitor never has to reach the distant wall. Labels remain legible without leaning.

The framed composition uses the same figures, objects and scenic layers as the world inside it. The viewer recognizes the riverbank, the gourd, the sheep and both women after entering. There is no substitute promotional cinematic. Idle firelight or a small glance can invite interest, but no unplayed plot reveal animates automatically. On return the canonical painting is restored. Personal memory appears as a separate kept memento and concise recollection beside it; it does not silently repaint the source artwork.

The modern river is a continuation within this same exhibit, selected beside the retained work. It is not advertised as a second independent painting. Future exhibits may reuse the gallery/entry contract, but a generic catalogue CMS is outside the first build.

## Visitor journey

Times are design budgets to verify against recorded audio. Browsing the gallery may take as long as the visitor wants. From explicit Enter, target 7–9 minutes without optional questions and under ten minutes for the passive route. The alternate thread takes about 6–8 minutes with tutorial skipped; the modern continuation about 3–4 minutes. Pause and leave remain available throughout.

| Moment | Visitor experience | What the application does |
|---|---|---|
| Gallery preview, visitor-paced | One painting, two women by the water, a quiet fire within it. The griot offers “Come. Sit with me.” | Show Enter and optional perspective controls within reach; subtle local attention response; no automatic entry or saved choice |
| 0:00–0:25 after Enter | An ember responds to an open palm. The chosen perspective is acknowledged, then the reconstruction disclosure is spoken | Enter uses the transparent default thread; a perspective control enters that selected thread. Ember acceptance remains optional |
| 0:25–0:40 | The familiar composition opens into depth around the seat | Firelight guides the far, middle and near layers out of their flat arrangement; no camera travel; only one entry transition |
| 0:40–2:10 | The cool water, two paths and Wambui's arrival | The griot supplies place/material context. Water touch is optional. The gourd thread receives its specific revelation here |
| 2:10–4:40 | Receive flour, hear the admission, set it between the women, hear Naserian's memory | Thread beats surround a shared dramatic spine; no answer or caption reveals an unreached turn |
| 4:40–5:40 | Agreement, then hospitality | Actual help changes how milk is offered; watching receives an equally warm, differently staged welcome |
| 5:40–6:40 | The bead is offered and the women depart | Accepted/passive placements remain truthful; no forced collection task |
| 6:40–8:30 | The world resolves back into its frame. You see the same painting differently | Griot gives one concise recollection; explicit Keep, alternate perspective, continuation and exit are available beside the exhibit |

Edit repeated exposition and idle waits to make room; do not add these stages to every existing line. Preserve the silence before the agreement. Remove the proposed stick-feeding tutorial from this release: the ember and first exhibit interaction already teach receiving. On subsequent visits the gallery is brief if desired; a saved memento provides direct replay or a new perspective.

### Entry and perspective selection

The nearby ledge offers **Enter painting** as the primary action. Two optional painted motifs expose specific perspectives: gourd, “What she brings”; bead, “What she remembers.” Choosing a motif deliberately selects that thread and enters; Enter alone uses the gourd default, with “Let us begin with what Wambui brings.” It records default provenance rather than claiming a personal preference. There is no second selection screen or timer forcing someone out of gallery browsing.

The motifs are miniatures or interface representations, not the character's real gourd or gifted keepsake. A glance highlights, a deliberate touch or nearby pinch control selects. The fire may briefly explain what each thread follows without saying what it reveals. Disclosure and route confirmation occur before the composition unfolds. The existing 15-second auto-start behavior is retired for the gallery preview. After an explicit entry, fully passive progress remains supported through every story invitation.

## The griot as contextual narrator

The griot remains a personality, not an encyclopaedic voice-over. It welcomes you from the painting, introduces place and people, supplies unfamiliar terms when needed, marks a remembered time clearly, answers available explicit questions, and helps you recognize the ending. In *What We Carry*, voice, flame and light share one authored performance. No humanoid narrator, microphone or live model is needed for this role.

| Story position | Useful contextual guidance | Preserve for the drama |
|---|---|---|
| Gallery preview | Title, Kenyan setting, two people meeting, invitation to enter | The shortfall, what was left at home, earlier waiting, final agreement |
| River and arrival | Enkare Nairobi, who is arriving, what a visible vessel carries, specific sourced regional context | Why Wambui made her decision, how Naserian will respond |
| Admission | Clarify what has just been said or the object being handled | “Last time, you waited for me,” the recollection and sheep decision |
| After the route vignette/agreement | Explain the revealed action, clarify invented versus documented context | Unique visual details from the other thread and any unplayed continuation |
| Return to gallery | Briefly connect the visitor's actual participation to the painting | Promises about future events that no shipped scene answers |

**Rule:** the narrator knows only what it is permitted to tell at the current beat, even when an author knows the full plot. Maintain a revealed-fact set or equivalent explicit question availability map per route and beat. Audit narration, captions, question labels, hints, placard text and idle animations for spoilers. A question marked available must have a safe recorded answer for that point; otherwise keep it unavailable until its reveal. The current trust/friendship answers need review because they can disclose earlier waiting.

Use short authored context lines at deliberate pauses or first appearances; one explanatory sentence should normally suffice. Only one voice owns the audio turn. Do not speak because the visitor happened to look at something, repeat context already heard, narrate every gesture or explain an emotional beat immediately after it lands. Essential context is available to passive visitors. Additional detail comes through the explicit object questions. Narrated character dialogue may use the same performer with restrained differentiation; it does not add live character agents.

An optional question can clarify a source or a word without becoming an options list. The gallery placard and About this artwork carry reference details; the griot keeps its conversational warmth. Each thread's reveal remains understandable to outsiders without an advance explanation of its meaning. Review scripts both in isolation and in the actual order heard on every route.

## Two threads with one agreement

All new dialogue and vignettes below are fictional writing directions, not verified historical events. They require editorial and cultural review before final audio/art production. Existing sourced context remains claim-linked. No new fixed exchange ratio, sacred bead meaning or general rule of communal obligation is introduced.

| Element | Gourd thread — What she brings | Bead thread — What she remembers |
|---|---|---|
| Central question | Why did Wambui come knowing the load was short? | Why does Naserian choose to wait? |
| New information | Wambui deliberately reserved food for home and came to admit the shortfall | Naserian recalls a previous meeting when she was the one who needed time and Wambui stayed |
| Distinct scene | A small illustrated preparation tableau: one portion stays, one goes into the travelling vessel | A short recollection at the same water, with the women in their earlier positions and Wambui waiting |
| Placement | After Wambui's arrival, before her admission | Immediately after “Last time, you waited for me,” before the sheep agreement |
| Visitor action | Inspect the offered gourd; the open vessel frames the tableau nearby | Receive/inspect a temporary memory motif; the recollection unfolds beside the live stage |
| Passive staging | Tableau appears beside the resting gourd | Recollection appears beside the visitor, without claiming they summoned it |
| Understanding changed | The shortfall is a difficult decision, not carelessness | Waiting is remembered reciprocity, not a stranger solving the dispute |
| Closing emphasis | Coming to speak despite having less | Making time for someone who once made time for you |

**Each thread must add a scene, not merely another description.** The current script already states that Wambui kept food at home and that Naserian remembers waiting. Replace those early summaries with the corresponding visual revelation on that route, while retaining enough shared dialogue for either visit to stand alone. The existing optional friendship/trust answers must not spoil the unplayed vignette.

### Vignette production limit

Each vignette gets 25–40 seconds, at most two scenic layers, and one small inset or tableau within the comfortable forward view. The main stage remains dimly visible so the visitor understands where they are. A brief narrated time cue distinguishes remembered time from the present encounter. Reduced motion uses a simple fade.

Gourd tableau: objects, a hand detail or silhouette, and portions of flour; no new family characters, interiors to explore or simulated pouring. Draft intent: “Before she left, Wambui divided what remained. Some for the journey. Some to leave at home.” Review the wording and material depiction before recording.

Bead tableau: reuse the two women and the water. Change placement and one reviewed pose per woman; a simple sequence establishes Naserian arriving short and Wambui making room to wait. Draft intent: “Another meeting, at this same water. That time, it was Naserian who needed longer.” Do not specify an invented custom or imply that the bead itself proves the event happened.

The visitor does not decide how much food a household keeps, approve the women's bargain, or travel physically into the recollection. This keeps the authored moral agency with the characters while giving the visitor a meaningful way to encounter it.

### Shared dramatic spine

1. Establish the place and the two people without requiring prior knowledge.
2. Wambui arrives and asks for help holding the flour.
3. Naserian names her own need; Wambui admits the shortfall.
4. The flour crosses back into the illustration, carried by the visitor or set down by Wambui.
5. “Last time, you waited for me.” The bead-thread recollection fits here.
6. “Take the sheep. Bring the rest when you can.” The sheep must be plainly visible.
7. Hospitality makes the visitor's actual participation visible.
8. The bead and departure close this meeting without pretending the remaining flour has already arrived.

Keep the existing closing observation: “Wambui arrived worried about what she owed. Naserian remembered what they had already given each other.” Both routes earn it in different ways.

## Visible consequence through hospitality

The milk beat happens after the women make their agreement. It never causes that agreement.

| Earlier event | Performance | Optional response | What may be remembered |
|---|---|---|---|
| Visitor helped place flour | Naserian turns toward the visitor and offers the milk gourd directly | Receive it, or let her set it nearby | “After you helped set down the flour, she offered you the milk” |
| Wambui placed flour on the passive path | Naserian turns the milk toward the visitor and sets it in an equally reachable shared place | Receive it from the resting place, or leave it there | “She set the milk beside the fire and made room for you” |

Warmth, duration and access to the milk are comparable. The difference is delivery and address, not a reward tier. Either route can then accept or leave the milk. Receiving flour alone must not be mistaken for completing the later placement.

Implement one milk invitation with outcome-specific staging where possible, rather than two independent interaction systems. No drinking mechanic, mouth tracking, liquid simulation or health/cultural claim. A supported single-hand selection counts as the same deliberate participation. The visitor does not need to hold two objects at once; flour ownership ends before milk is offered, and milk rests before the bead arrives.

The prototype must show the two arrangements side by side using temporary materials. If unfamiliar viewers cannot see a meaningful difference, revise choreography and line timing before producing more art.

## Interaction and narrator contract

| Signal or action | Response | State boundary |
|---|---|---|
| Look toward a person | Gentle pose/light acknowledgement; settle rather than continuously swivel | Head direction nominates a target only; no new speech, branch, consent or save |
| Point at a question-bearing object | Nearby prompt reveals the specific available question | No answer until explicitly selected; unavailable or spoiler questions have no active prompt |
| Pinch the visible question prompt | Fire answers the recorded question, with a small light cue to the object | Routes through the existing question authority; never through receive or offer logic |
| Open palm at an offered object | Object crosses to the hand or its accessible equivalent | Only the active invitation can accept it |
| Place an owned object at a marked destination | The intended offer/placement resolves once | Requires current ownership and legal destination; unrelated contact does nothing |
| Touch fire/reeds or tilt a held prop | Small local sensory feedback when enabled | No journalled moral choice or story consequence |
| Place keepsake in the clearly labelled keep location | Attempt explicit local save | Success is shown only after persistence succeeds |

**Asking and taking:** prototype an object-linked question chip, not “pinch anywhere on the object.” The ray selects the labelled chip; nearby open-palm receiving remains a separate affordance. While an invitation is active, its essential control takes priority. An available question may suspend the invitation with its remaining time, then restore it. Never let one pinch both ask and accept. Keep the existing reachable question panel as the fallback and controller equivalent.

At most one question is active; one explicit request is queued only if the current boundary is safe, otherwise show a brief local holding response. Duplicate selections do not stack. Stop/pause/recenter/scene change invalidate stale presentation cues. Resume at an authored audio boundary without replaying completed actions; do not claim sample-exact interruption recovery unless implemented and tested.

The fire may lean subtly toward an intentional question target or acknowledge stable attention, with restrained motion. It must not duck narration merely because the head moved, speak unrequested asides, or follow rapid head movement twitchily. During the agreement it remains still enough to leave the women the focus.

### Three physical pleasures

Core production is limited to: responsive palm/fire embers; a restrained flour movement sound when a held gourd tilts; a clear light and sound response to placing the keepsake. Use licensed recordings or authored effects with provenance, no AI-generated music. Do not suggest physical weight or haptics the device does not deliver. The tilt sound is a material cue, not a reliable measurement of how much flour is inside.

Painting ripples, universal touch behavior, animal approach and decorative echo interaction are deferred. Keep the existing water touch because it belongs to the scene and teaches a simple action.

## The painting becomes the world

The framed composition, spatial reveal and return are one presentation contract. The fire motivates the change from within the painting; it does not lead through another lobby first. Use existing scene layers and deterministic authored effects, independent of network or runtime image generation.

1. Hold the composition long enough to recognize its people and objects. The fire within it brightens and offers the ember through the frame toward reachable space.
2. After disclosure and optional ember receipt, reveal painted depth far to near. Preserve landmark correspondences while the surround dims and the canvas fills the story's view.
3. The bank, reeds and cast resolve around the stationary visitor. Spatial sound broadens from the frame's fire to the water beside the seat.
4. The frame leaves the central story view once orientation is clear. Full-depth objects retain their painted origins; Wambui's later gourd hand-off completes the visual promise.
5. On return, the layers settle back into the same framed composition. The gallery's orientation, ledge and controls return consistently. The actual bead remains temporarily within reach for an explicit keep decision.

Target 6–10 seconds for the main reveal and a restrained reverse transition. Do not physically zoom, rotate or translate the camera toward the wall. “Step inside” describes the experience; it must work from the seat. Prototype a shared render/layout source for the flat preview and spatial presentation, or a derived static composition with controlled registration. Do not build an always-rendering second 3D world just for the preview without profiling its cost. Hands and essential controls retain a clear interaction state. Reduced motion uses a 2–3 second composition-matched dissolve; pause, cancellation and early return share normal flow semantics.

Use one material family across the gallery/frame, first artwork, vignettes and modern continuation. Preserve the quieter gallery and warmer story lighting without switching illustration styles. The current sheep can remain if its silhouette and style pass review. Inspect one face and hand at final scale on the target device before deciding cast resolution/compression. Test that the visitor recognizes the work after returning; no extra decoration can substitute for that recognition.

### Asset and narration budget

| Deliverable | Maximum planned production | Dependency |
|---|---|---|
| Gallery and exhibit preview | One restrained surround, frame, accessible ledge and placard; composition derived from existing encounter art; replaces the planned external hearth layers | Preview/world alignment, legibility, provider/rights record |
| Historical cast | Existing two figures, corrected dress/expression; up to two added poses each, for notice and milk staging | Cultural feedback and stable hand anchors |
| Thread vignettes | Two small layer sets; object detail plus at most two extra cast cells reused where possible | Story comprehension before art |
| Modern river | Two or three coherent layers around retained gate/bead interaction | Preserve documented restoration context and imagined garden distinction |
| Sheep | Existing solid animal initially; one simple art replacement only if needed | Visual comparison, no new locomotion project |
| Narration | Budget 30–40 changed/new short clips across gallery welcome, two vignettes, hospitality, return and safe context/questions | Locked text, source and spoiler checks, listening review |
| Effects | One reveal treatment, restrained sparks, three local feedback sounds | Worst-case device cost and sound balance |

The narration count is a production ceiling to track, not a guarantee. Rough allocation: gallery welcome/thread 5–7; gourd vignette 4–5; bead vignette 5–7; hospitality/outcomes 6–8; closing/return 5–7; context/question corrections 3–6. Total 28–40. Reuse unchanged recordings. Changed words receive new versioned audio keys; retain older assets required by kept plans. English captions follow every released line and sound mix is checked at quiet and louder headset volume.

Keep asset-generation attempts bounded to two review rounds per set before simplifying the brief. Record provider, actual model if recoverable, dated terms, prompts, source images, output files and alterations. Never fill a missing model name by guessing. Approve style on one face, one palm and one prop before producing sheets.

## Keeping and returning

The story ends completely before it returns to the gallery and invites another visit. No cliffhanger about whether Wambui later paid unless a real authored scene answering it ships. Proposed hook after the gourd route: “You have seen what she brought. Another time, we can sit with what Naserian remembered.” After the bead route, invite the visitor to see what Wambui left at home. These are drafts; avoid wording that falsely claims a default thread was their selection.

The modern river is a continuation of this exhibit, not a second gallery work or the payoff for an unfulfilled historical promise. Its pleasure is recognizing the same water in today's city, seeing documented restoration reflected in an explicitly imagined garden, and deciding what to carry onward. It is available immediately after a kept visit; no artificial day lock or streak. The other historical thread is also available without forcing a save, but personal recall requires one.

| Returning option | Behavior |
|---|---|
| Replay this visit | Play the retained plan and actual outcomes with original compatible assets/audio; no new branch or inferred history |
| Follow the other thread | Start a new authored performance, skipping first-use tutorial; retain no invented choices from the prior route |
| Visit the water today | New child visit with truthful parent recall and optional bead placement |
| Forget | Remove the kept visit and linked persisted memory according to explicit deletion semantics |

The exhibit ledge should present one primary next invitation and nearby alternatives; it should not become an achievement grid. A prototype must test whether people understand “replay” versus “another part of the story.” The alternate route initially reuses the full shared encounter; if repeat exposition feels laborious, shorten transitions and already-known setup through authored return variants. A separate abridged episode is outside the initial budget.

**Physical Keep:** use a dedicated recess in the exhibit ledge with a clear “Keep this visit” label and one warm invitation. The historical character's bead gift never constitutes save consent. Require deliberate release/selection in the recess; a resting bead, incidental contact, automatic placement or tracking loss cannot save. The hand panel remains equivalent. On save failure, retain the visit in memory and show retry; do not light a permanent shelf object or say it was saved. Forget and local retention remain discoverable. Declining to receive the story bead does not prevent explicit saving later.

The present-day gate placement and its recall need a saved continuation outcome, not only the initial encounter record. If the visitor leaves without keeping that continuation, future visits cannot claim the bead is still on the gate. No real-world location anchor or room data is required.

## Cultural and factual integrity

Separate three questions in review: Is the documented context supported? Is the fictional encounter plausible and respectful? Do the voice and images feel appropriate? One family review is valuable and specific; it is not certification for all members of either community.

Preserve the pre-entry reconstruction disclosure, spoken Enkare Nairobi with Maa delivery reviewed, specific Kikuyu/Maasai identities, and separate Akan naming context. Review household food allocation, the milk invitation, dress, bead, sheep and invented recollection together. If a reviewer challenges a prop or gesture, revise its staging and language before regenerating all related art.

Provide “About this artwork” from the gallery placard and questions panel: concise distinctions between sourced place/material context and invented characters, actions and dialogue. No whole-object gold truth glow. Exact sources stay linked at claim level in content/review records; public source presentation and identifiable-name handling must follow the final entry review. The narrator does not read the bibliography mid-scene.

Nothing collected for review authorises publishing a reviewer name, private family account or recording. The user controls external outreach. The production tools can prepare packets and incorporate supplied notes without waiting for permission to draft.

## Implementation modules and data

Follow [the modular architecture](../architecture.md). Proposed identifiers below describe intent; introduce only fields/contracts needed by the implementation, after checking current readers, validators and migration requirements.

| Owner | Existing foundation | Change required |
|---|---|---|
| Story core | `StoryModel`, `PlanComposer`, `StoryRun`, package/plan validators | Two stable route IDs, shared beats and distinct vignette order; legal hospitality conditions; no route logic in shaders |
| Application flow | `VisitFlow`, `FlowCues` | Gallery preview, explicit entry, thread default/selection, question suspension, owned objects, reframed return and explicit save outcome |
| Input | `PointerSelect`, palm/touch gestures, `PointerSource`, `HandSource` | Object question chips, action arbitration, one-hand alternatives, tracking-loss safety |
| Stage | `IllustratedTheatre`, `IllustratedFigure`, `StoryTransition` | Shared preview/world composition, frame reveal/return, vignette layout, notice/milk poses, reduced motion and recenter |
| Narration | `NarrationPlayer`, current generation/manifest pipeline | Beat-safe contextual griot, versioned clips, one audio owner, heard-context tracking, safe resume and spoiler gating |
| Persistence | `SavedVisit`, `FileVisitStore` | Preserve selected/default route origin if recalled; continuation compatibility for both routes; save continuation outcomes; legacy replay/deletion behavior |
| Authoring/build | Theatre layout and editor builders | Scene-keyed presentation layouts in one active package; source-only checks; generate scenes/materials through editor |

Gallery metadata needs only a stable exhibit ID, title, placard/credit references, preview/layout reference, supported routes and continuation mapping. Resolve the saved visit's package/route back to its exhibit; preserve fallback behavior for older records without that mapping. A separate catalogue service, new account system and gallery-specific copy of the store are unnecessary. The candidate flow is GalleryPreview → Entering → Story/Question → Reframing → ExhibitClosing; pause can overlay these states, and a new route returns through Entering. Internal class names may remain compatible with the current engine; the display contract must reflect the gallery. Returning mid-story offers exit/restart honestly rather than recording a completed visit.

**Known integration constraint:** `PlanComposer` currently allows a continuation to follow one exact parent route, and `SavedVisit.Next` is a single next route. Two historical threads plus a modern continuation cannot be wired by changing menu labels alone. Prefer a small validated route-eligibility table that accepts the two historical parents for the coda, while deriving the alternate thread from package metadata. Preserve the legacy single-parent reading path. Do not silently accept arbitrary parents or introduce a general branching framework.

Share scene beats by stable IDs. Suggested authored routes: `water.brought`, `water.remembered`, `water.today`; existing saved route IDs keep their original meanings. Choose exact IDs during implementation, before recording their associated clips. The layout may select presentations per scene; it must not duplicate application or storage behavior.

If adding fields to strict saved-plan readers, version the format and keep an explicit legacy reader/migration fixture. Existing saved visits have no thread-selection provenance: treat it as unknown, never fabricate a choice. Keep old embedded plans/audio replayable or show a specific missing-content recovery. The present replay already omits question/interruption timing; do not market full conversational replay or quietly extend that promise here.

Journal accepted outcomes once. Keep transient attention and physical hand samples out of persistent personal records; existing limited semantic attention recall must be accurately described. New thread choice and continuation records persist only through explicit keep or an explicitly disclosed resume setting.

Pause/suspend freezes story time, input acceptance, offers and audio together. Resume validates tracked hands before allowing new actions. A last-known hand pose may maintain visual continuity briefly; it must not manufacture a fresh receive/drop/save event. Recenter keeps stage-relative anchors and question targets aligned. Resume after relaunch requires a separately verified stable checkpoint policy; if this is absent, expose restart/replay honestly rather than claim recovery.

## Hardware and performance plan

Quest access is an open dependency. Seek a borrowed device, collaborator or external tester as early as possible; no purchase or outreach is performed by this scope. First test the current build before adding all effects. Target initial Quest 3 validation, with final supported-device claims limited to evidence and the release decision.

Run three hardware rounds: baseline reach/depth/input; complete new threads and worst-case transition; warm release candidate with independent installation. A remote tester should provide device/model, build hash, full hands-only recording, performance log and structured observations. Such evidence cannot substitute for every comfort review, but it is actual device evidence.

Measure CPU/GPU frame timing, dropped frames, memory, load/transition stalls and warm-session behavior. Test gallery preview/frame transition plus fire/hands/captions, maximum card overlap, the vignette, question while holding, and return. Record chosen refresh rate and matching frame budget; for example, 72 Hz gives about 13.9 ms. The competition's stated 60 fps floor does not establish our device target. See the [official judging guidance](https://start-developer-competition-26.devpost.com/details/judging).

Start with conservative static settings. Profile before choosing silhouette trimming, alpha-to-coverage or MSAA changes. Check transparent edges in both eyes and at actual scale. An adaptive tier may later reduce decorative particles or other measured costs, with conservative thresholds, hysteresis and logs. Do not switch repeatedly during the emotional reveal or reduce readable captions/essential cues. Measuring the first ten gallery seconds is not a full-scene profile, and a governor cannot close a hardware gate.

If hardware remains unavailable, software/art work can proceed with bounded investment, but technical and comfort gates stay open. Do not convert unavailable evidence into a pass or use simulator novice sessions to claim hands/depth acceptance.

## User stories and acceptance

These targets are formative product gates, not probabilities of winning. Use observed failures to change the design, not to coach participants to the expected answer.

| ID | User story | Observable acceptance |
|---|---|---|
| IT-01 | As a first visitor, I understand that this painting can be entered | Enter/perspective controls available on gallery readiness; preview never auto-enters; first local hand response within 20 s after entry; passive story completes |
| IT-02 | I can follow a question that interests me | Each thread includes its own visible vignette and different revelation/order before the ending; either route alone makes the bargain understandable |
| IT-03 | I can ask about what is in front of me | A labelled object question invokes exactly one correct answer; no accidental receive; unavailable reveals are not spoiled; narration resumes coherently |
| IT-04 | Helping changes how I am included | Visitor can point to the changed milk staging; the same agreement and hospitality remain on passive paths |
| IT-05 | I can simply watch | No timeout claims I acted, chose an interest or refused friendship; objects remain readable and the ending complete |
| IT-06 | I recognize the painting becoming a world and returning to its frame | Same composition landmarks before/inside/after; no camera movement; readable hands/captions and reduced-motion equivalent; device findings recorded |
| IT-07 | I understand what is imagined | After a visit, testers distinguish fictional women/bargain from sourced regional context without implying all rendered details are documented |
| IT-08 | I can keep a visit and return to something worthwhile | Relaunch retains only explicitly saved records; alternate thread reveals another scene; coda recalls correct parent and saved placement |
| IT-09 | I can pause, recover and use one hand | Both routes and coda complete hands-only with either hand; tracking loss, recenter, focus loss and failed save cause no false action |
| IT-10 | I trust the submission's promises | Candidate build, guide, footage, coverage and source/rights records agree; independent install and device evidence recorded |
| IT-11 | The griot helps me understand without spoiling the story | Safe context at first need; no premature reveal in narration, hints, questions or captions; quiet beats remain; passive visitors understand the conflict |
| IT-12 | The painting means something different after my visit | In review, visitors identify a previously seen detail and explain its changed meaning; source artwork stays canonical and saved memory is clearly personal |

Prototype evaluation: three unfamiliar readers/viewers see the framed work, temporary-art entry/return and paired paths. At least two understand how to enter without walking, recognize the composition inside, explain a detail differently afterward, and distinguish the two story threads. Also check conflict and participation comprehension. This is a cheap iteration gate, not final validation.

Final formative evaluation: five first-time visitors, counterbalanced initial thread where practical. Target at least four completing unassisted and explaining a visible consequence. Ask “What happened?”, “What did you change?”, “What was imagined?”, and “What would you return for?” before prompting about features. At least three try the second thread in a later session and identify new information without being told it. Record device versus desktop conditions and individual failures; this small sample supports design decisions, not broad statistical claims.

Automated coverage: preview never auto-enters or saves; entry/reframe cancellation; exhibit identity after relaunch; frame/scene registration under recenter; spoiler eligibility by beat/route; both thread defaults/selections; accepted and default outcomes including receiving flour without placing it; milk acceptance/default under both earlier outcomes; pause/question/recenter during offers; obsolete question callbacks; save fail/retry; old/new replay; both parent routes reaching the coda; deletion; all captions and audio references. Enumerate legal outcome combinations in core tests and use representative risk paths for Unity/device visits. Target ten complete hardware visits across the release paths, including passive and one-hand runs; do not confuse that with ten distinct participants.

## Work sequence and effort

Assume 12–15 focused user hours/week until confirmed. Planning separates human acceptance/production effort from assisted engineering effort. Engineering estimates include implementation, integration, meaningful tests and fixes; they are not “unattended agent cycles” and not promises about elapsed time. Some user and engineering time overlaps during review; do not add both columns as a single personal workload.

| Sprint | Dates | Deliverable | User hours | Engineering effort | Exit gate |
|---|---|---|---:|---:|---|
| 1 Prove the gallery promise | Oct 13–19 | One framed work, rough entry/return and gourd crossing; paired milk outcomes; question/context probe; vignette scripts; hardware/review preparation | 10–12 | 15–23 h | Three-person entry/composition/comprehension probe; revised estimate; hardware path or explicit blocker |
| 2 Build the first exhibit | Oct 20–26 | Gallery entry and route selection, two vignettes, context-aware griot, hospitality, matched frame/cast; second device round begins | 13–17 | 23–35 h | Both perspectives complete; meaningful differences and safe reveals; no input/ownership ambiguity |
| 3 Complete the gallery return | Oct 27–Nov 2 | Reframed ending, exhibit-side Keep/replay/alternate, painted modern coda, legacy compatibility and truthful recall | 10–13 | 18–26 h | Gallery → exhibit → gallery plus saved return pass; modern continuation coherent; feature freeze |
| 4 Verify and simplify | Nov 3–9 | Novice/device sessions, profile-driven optimization, accessibility/recovery, final cultural/audio fixes | 12–14 | 18–26 h | Formative targets assessed, ten device visits, major faults closed, rights and release access ready |
| 5 Present and release | Nov 10–16 | Frozen candidate, independent install, demonstration, adaptation copy and completed submission package | 9–12 | 8–12 h | Guide matches build, user accepts release materials, submission completed when authorised |

Totals: **54–68 user hours plus 10 hours contingency = 64–78 hours**; **82–122 hours of engineering effort**, substantially assisted but requiring integration/review. The gallery adds a net 2–3 user hours and 6–10 engineering hours for composition matching, preview/navigation and return integration, while replacing the external hearth art rather than adding another room. The low estimate exceeds a 60-hour envelope by four hours; the high estimate exceeds 75 hours by three. Simplify nonessential art/effects or add capacity after the probe; do not consume the ten-hour reserve silently. Sprint 2 may require redistribution into lighter weeks. Re-estimate after Sprint 1. Current dates reserve November 17–18 for access/submission problems, not planned feature work. Internal target November 16; official deadline November 18 at noon Pacific / 3 p.m. Eastern, checked October 13. [Official rules](https://start-developer-competition-26.devpost.com/rules)

The estimate includes core art/narration review, testing and submission production. Reviewer availability, device shipping/access and approval lead time are external dependencies. Paid asset/tool/API spending has no approved numerical budget; prototype with current assets first, then record cost and terms before paid production. Art generation and editing can be assisted; only the user/reviewers can provide their acceptance, cultural feedback, account decisions and external access.

### Work packages and dependency order

| Ticket | Implementation boundary | Depends on | Done when |
|---|---|---|---|
| IT-P01 | Gallery proof: frame → depth → gourd → frame, contextual question and paired milk consequence | Existing build | Viewers understand entry, recognize the composition and see their consequence; no full gallery architecture needed |
| IT-P02 | Draft exhibit placard, griot context/reveal map, two vignette scripts and review packet | Current source/story | No premature reveal; complete passive story; sources/fiction legible and specific reviewer questions prepared |
| IT-P03 | Hardware baseline and release identity preparation | Current APK, human access | Findings tied to hash/device; signing/app-id choices recorded securely; no credentials committed |
| IT-P04 | Exhibit metadata, explicit gallery entry, route selection and continuation eligibility | P01, P02 | Preview cannot commit accidentally; both plans validate; default origin truthful; legacy fixtures pass |
| IT-P05 | Object questions, pause/resume and input arbitration | P01 | No double action, no spoiler or stale answer, one-hand alternatives pass |
| IT-P06 | Hospitality ownership, staging and recorded lines | P02, P04 | Four milk combinations and mixed flour outcomes pass; guest treatment remains warm |
| IT-P07 | Vignette art/layout/audio and cast responses | P02, P04, first asset review | Two distinct scenes in complete runs; no new rigs/environments needed |
| IT-P08 | Gallery/frame composition, world reveal/reframe, painted coda and local sound | P01, art/rights sample | Same artwork recognized before/after; coherent style, reduced motion and worst-case profile |
| IT-P09 | Exhibit-side physical Keep, gallery navigation, alternate thread and coda memory | P04, P06 | Correct work/memento after relaunch; failed save, legacy replay, both parents and deletion pass |
| IT-P10 | Device/user/content acceptance and optimization | P05–P09 | Recorded failures fixed or scope reduced; no substituted evidence |
| IT-P11 | Build, access, adaptation and demonstration | P03, P10 | Frozen artifact installed independently; materials faithfully show candidate |

P02 and preparation for P03 can proceed while P01 is built. Final art waits for the story/interaction decision. Production narration follows approved text. Critical path: P01/P02 → P04/P06/P07 → P09 → P10 → P11. Hardware and cultural review run alongside it and can block release acceptance independently. This describes work dependencies, not authorisation to spawn agents or message reviewers.

### Cut order and stop conditions

Do not add stick feeding, another lobby or catalogue browsing. First cut sheep approach, peripheral wings, decorative echo interactions and automatic quality tiers. Next simplify vignette art, reveal particles and extra cast poses while preserving the distinct scenes and clear acknowledgement. Retain a simpler coherent modern painting rather than a mismatched 3D coda. Cut redundant narration instead of the silence that makes the agreement land.

If the two-thread structure still does not fit after the first probe, explicitly choose a one-thread release and narrow the claim; do not ship two cosmetic choices advertised as meaningful personalisation. If the milk difference fails comprehension, revise the gesture or choose another small hospitality event before adding more branches. If hardware finds reach or frame problems, simplify effects before adding content. If cultural review disputes the premise, revise the story before commissioning its final art.

Never spend the release/testing reserve to add an untested system. No numerical judge score or winning probability is used to justify scope.

## Submission story and immediate next work

The adaptation case is specific: web Sankofa's illustrated, narrated storytelling becomes a living gallery. Its first artwork opens into a seated spatial encounter with physical objects, authored perspectives, a contextual griot and remembered participation. The painting gains meaning through the visit and frames its return. Preserve the original project's provenance and assess any actual code/asset reuse separately. Recorded questions are not live conversation, and two authored threads are not open-ended generation.

Prepare a 2:30–2:45 demonstration: establish the framed painting, show the same composition opening into depth and the gourd crossing into reach; show two different revelations and one paired hospitality consequence; then return to the framed work with an explicit kept memento. Make the gallery promise legible in the first few seconds. Show implemented runtime behavior and label simulator footage. The final video must not hide an unfinished in-app ending. Release access/signing, eligibility and rights remain independently verified before submission. Do not contact reviewers, publish or submit without the user's authorization for those external actions.

**Next implementation increment:** P01 and P02. Produce a rough 60–90-second gallery → painting → gourd → gallery sequence using existing art, plus safe-context/vignette scripts and a paired hospitality capture. The gallery is the selected direction; compare staging treatments within it rather than retesting whether the product should be a gallery. Reuse the current application and question pipeline, keep it behind an explicit prototype configuration, and review it before expanding final asset production. No new paid generation or complete chapter rebuild is necessary to answer that first product question.

## October 14 v0.7 implementation checkpoint

The [production iteration](../reviews/2026-10-14-production-iteration.md) records the current candidate. Perspective plaques now name Wambui and Naserian. The earlier favour is explained in Naserian's memory after its enactment; the default journey leaves that detail for discovery. Focused returns reach new material after 23.61 seconds of declared speech/pauses and finish in 126.37 / 135.27 seconds excluding transitions/questions/interruptions. They keep disclosure, independent agreement, the bead and the same frame. Earlier timing tables remain planning/history, not current measured sessions.
