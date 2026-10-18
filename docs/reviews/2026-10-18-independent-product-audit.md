# Sankofa independent product audit

October 18, 2026. Ground-up assessment of the complete visitor proposition, current software and presentation evidence. Companion: [experience transformation plan](../product/experience-transformation-plan.md).

## Product judgment

Sankofa has a credible complete prototype and a promising human subject: two women preserving a relationship when an exchange cannot be completed. Its strongest opportunity is **being welcomed into that relationship through an object placed in the visitor's care**. The pavilion, illustrated staging, local story engine and saved visits can support that experience.

The evidence does not yet establish a category-leading entertainment product. The dramatic problem arrives late, important motives are explained in small inset scenes, and the principal visitor actions often acknowledge a predetermined event. A more elaborate gallery would not resolve those limitations. The next investment should establish whether physical presence makes this particular story meaningfully more affecting.

The recommended experiment is an encounter in which an object connects the present exchange to a spatial memory. The strongest alternative is a simpler, exceptionally directed short with fewer interactions. Both deserve comparison before another production pass. Neither is a guaranteed winner.

## Independent review method

Four subagents began with fresh conversation contexts. They did not inherit the development discussion, earlier panel conclusions or the lead agent's preferred direction. Their assignments were deliberately different:

| Reviewer | Initial evidence and remit | Independent conclusion |
|---|---|---|
| Competition judge | Official rubric, current content, selected implementation, release identity and five stills | Potentially affecting prototype; stronger first-minute stakes and embodied memories needed; no defensible first-prize claim from present evidence |
| Experience critic | Actual story/flow/input/storage, narration metadata and six stills; no prior strategy documents | Hospitality is the heart; a roughly six-minute chamber play would improve the arc; test whether interaction helps rather than assuming it does |
| Competitive strategist | Neutral product description, current status, three stills and primary precedent research | Tangible memory theatre is the strongest experiment; a directed short is the fallback; gallery expansion and generative conversation have weak near-term justification |
| Delivery critic | Actual modules, input, build setup, release record and recent test artifacts | Reuse the foundation, validate the current layout and real hands, and resolve device/release evidence before extending technical scope |

Repository policy required all reviewers to read `AGENTS.md` and the ignored local competition brief. Those contain existing preferences, so this was **limited-context independent review, not fully blinded research**. Reviewers were told to treat strategic preferences as hypotheses. Two initial reads of the release document also exposed historical release metadata; those reviewers disclosed this and did not treat it as current evidence. No reviewer consulted another reviewer before returning a conclusion. They are AI reviewers, not four independent human judges or participants.

The lead inspected current content, source, stills, implementation boundaries, official pages and test artifacts, then reconciled the reports. Full original reports are retained locally under `artifacts/product-audit-2026-10-18/`; this document contains the durable synthesis. No sound played, Unity/simulator launched, participant session conducted or product code changed for this audit.

## What the official criteria change about the decision

The four criteria are equally weighted: Innovation & Creativity, Experience Design, Technical Implementation, and Polish & Presentation. Entertainment includes immersive storytelling. End-to-end hands use is mandatory; seated short visits are guidelines. Neither live AI nor MR is a mandatory feature. The technical criterion expects at least 60 fps on Quest; the submission needs honest device or qualifying emulator footage. The entry deadline is November 18, 2026, noon Pacific / 3 p.m. Eastern. [Official rules](https://start-developer-competition-26.devpost.com/rules), checked October 18.

The FAQ specifies Quest 3/3S judging, permits custom/OpenXR input solutions, and says headset ownership is not required. Practical access for testing remains a separate product need. Ports belong in Adapted; no evidence shows an easier field there. The rules' two-foot-radius guideline and FAQ's two-foot-diameter explanation differ: treat neither as a reason to demand the visitor's maximum reach; verify a conservative, adjustable seated envelope. [Official FAQ](https://start-developer-competition-26.devpost.com/details/faqs).

Our interpretation of [judging guidance](https://start-developer-competition-26.devpost.com/details/judging): each proposed addition must improve a recognizable experience, its usability, its demonstrated reliability or its craft. Counting APIs, tests, branches or triangles is insufficient. Special awards are secondary design references, not extra requirements or a reason to add AI.

| Criterion | Current strength | Main constraint | Evidence that would change this assessment |
|---|---|---|---|
| Innovation | Objects cross between illustrated figures and the visitor; a painting frames the visit | These ingredients have precedents; attention mostly changes acknowledgement and question highlighting | Unfamiliar visitors identify a specific spatial/physical discovery that changes their understanding |
| Experience | Coherent passive outcomes, character autonomy, seated flow, explicit saving and recovery | Choice before acquaintance; delayed tension; repeated receives; ending becomes navigation | Cold-start completion, early understanding of both women's stakes, emotionally legible ending without explanation |
| Technical | Deterministic story state, local media, cancellation, replay and narrow presentation boundaries | Recent layout failures; no real-hand or sustained headset evidence | Same-candidate installation, complete hands-only journeys, measured rendering and interruption recovery |
| Polish | Painted composition and restrained setting have a recognizable direction | Memories read as explanatory panels; props/UI compete with figures; voice and cultural portrayal are unaccepted | Reviewed performance, coherent focal assets, binocular inspection, actual listening and honest final footage |

The judge's evidence-limited ranges on a reviewer-created five-point scale were 2.5–3.5 for Innovation, Experience and Polish, and 2.0–3.5 for Technical. They are not official scores, a panel average, or probabilities. Missing audio/device evidence is uncertainty, not a finding that those features fail. The useful result is the identified constraint in each row.

## Artifact identity and current defects

The audited source started at `bd80fb7`. The verified local APK remains source `493c3aa`, story v0.7.0, package `com.sankofa.quest`; the [candidate record](../release/candidate.md) identifies its hash. A reviewer independently checked the APK's size/hash against the manifest. Uncommitted editor/layout, PlayMode-test, generated scene, controller and font changes existed before this audit and were preserved.

The October 15 reports confirm 31 editor and nine focused runtime passes for the earlier pavilion work. The release record also reports 306 core passes. These checks were not rerun here and cannot certify later edits.

**Newer evidence matters:** `artifacts/logs/polish1-playmode.xml`, October 17, records **25 passes and two failures out of 27**. Its companion EditMode report records 31 passes. The failures are:

1. `EntryAndReturnPlaquesStayClearOfTheArtworkAndEachOther`: the larger `gallery-other` return menu overlaps the controls at eye height 0.85, reach offset 0 and a 60-degree review lens.
2. `SettingsAndCreditsRemainInTheForwardViewWithoutStoryControlsCompeting`: the Settings control projects outside its required forward viewport, approximately x=-0.11, y=-0.01.

These are recorded software failures associated with the newer workspace work, not demonstrated defects in the older APK or newly reproduced headset failures. Exact identity of the dirty source at that run is not pinned. Resolve and rerun them on a named candidate before treating the working layout as accepted. Fixing overlap by moving essential settings off-screen is not a successful trade.

## Complete journey audit

Timings below are independently calculated from current JSON line durations, pauses and conditional default outcomes, with normal transitions added. They are **scheduled timings, not observed participant latency**. Entry selection is time zero; gallery browsing, optional questions and pauses are excluded. Playback can use actual clip durations.

| Stage | Current behavior | Product finding |
|---|---|---|
| Before entry | Enter plus two named perspectives, wall context, Pause/Leave/Settings | Names precede relationships. First-time visitors are asked to choose an editing perspective without knowing why they care. Preserve deliberate entry; remove the premature decision in the experiment. |
| First minute | Welcome, ember receive, place and reconstruction disclosure | First interaction is early, but the spatial scene opens at about 52 seconds with immediate acceptance or 66 seconds passively. The ember teaches receiving without revealing the human problem. |
| Meeting and memory | Water touch; flour receive; either Wambui's scarce portions or Naserian's earlier meeting | Good content competes with onboarding, controls and an inset scene. Explicit shortfall starts at approximately 3:04 on Wambui's passive route, 2:26 on Naserian's. |
| Agreement | Visitor may help place flour; women decide to defer part of the exchange | Protect their autonomy. The visitor contributes socially without deciding a culture's moral or economic outcome. Agreement begins approximately 4:05 / 4:17. |
| Hospitality | Direct thanks/offered milk after helping, milk placed nearby after waiting | This is genuine observable response, although both paths remain welcoming. It is the strongest candidate for the emotional peak. |
| Farewell | Landscape narration, milk put down, bead receive, lingering and departure | Several explanations and receiving beats follow the resolution. A quieter, shorter farewell may carry more weight. |
| Return | Same picture; narration interprets it; keep/return/leave controls | The unchanged picture can become newly meaningful. About 48 seconds of home narration risks explaining away that discovery. |
| Revisit | One replaceable saved visit, shorter other perspective, exact retained telling/actions, modern coda | Useful bounded content, not established retention. Continuing currently depends on a retained visit; an immediate optional epilogue should not require storage consent. |

Current passive schedules are about **7:09 / 7:21** including normal transitions. Six invitation waits contribute 60 seconds; instant acceptance yields roughly six-minute schedules. The shortened perspectives are approximately **2:03 / 2:15 before transitions**. The modern continuation adds roughly 2:19–2:46 before transitions, depending on prior outcomes. A full visit plus coda can exceed ten minutes, but the first story is independently complete; optional further viewing is not itself a breach of the guideline.

## Assumptions that should no longer be treated as settled

| Assumption | What inspection supports | Consequence for the plan |
|---|---|---|
| A more immersive museum makes the product compelling | The pavilion improves surroundings; its effect on emotional engagement is unknown | Freeze architecture except for proven comfort/performance problems; test the encounter |
| Entering a painting is the differentiator | It is recognizable and useful, but established in VR | Make a specific interpersonal/spatial action memorable; avoid a genre-invention claim |
| More agency means changing the bargain | The fixed agreement preserves character autonomy and truthful passive participation | Improve what visitors discover and how they are included; do not add a morality puzzle |
| Two perspectives mean two substantially different experiences | They share 16 beats out of 17 and 18; one adds portions, the other waiting and its answer | Test meaning, not branch count. A short alternate can be valuable; do not describe these as two different worlds |
| The default telling fully earns the reciprocal-trust resolution | Wambui's route omits `gallery.remember.answer`; explanation is mainly optional questioning or closing summary | Every complete first visit must make Naserian's motive legible, even when a fuller memory stays optional |
| The story notices attention by adapting its plot | Head direction pulses objects/figures/questions and contributes to retained echoes | Say acknowledgement or explicit authored choice; do not imply eye tracking or intent inference |
| Lost-history framing is already supported by the story | The meeting, people, bargain and gifts are explicitly invented; documented context is separate | Choose a verified, specific historical anchor and truthful framing; never advertise the invented encounter as recovered testimony |
| Cultural review is a final accuracy check | Review could change garments, crops, gift, name delivery, character framing or narrator | Give reviewers influence before final art/audio production; identify what each person is qualified to review |
| A warm griot is culturally neutral | Metadata directs a generalized West African griot voice for a Kenyan encounter; the Akan title is Ghanaian | Make an accountable curatorial choice. Do not assume a single pan-African voice; actual sound remains unheard |
| Passive progression proves accessible participation | A person trying to act can still miss a timed invitation | Distinguish choosing to watch from needing more time; test timing/one-hand alternatives |
| Keep and replay create a reason to return | They preserve data and provide access to finite additional material | Test voluntary return for a specific unanswered question; avoid manufacturing habit through unlocks |
| More AI would recover the original product's advantage | There is no runtime provider/backend; freeform dialogue introduces a new acceptance surface | Only consider bounded questions after observed unmet demand; do not put it on the critical path |
| Another chapter is cheap because its layout is short | Data authoring is only one part; writing, provenance, sound, art, culture and device review recur | Measure one production increment end to end before promising a catalogue |
| Passing tests means this is ready to win | Tests establish narrower behavior; newer failures also exist | Obtain user, listening, device and installation evidence tied to the actual candidate |

## What precedent teaches

These are relevant productions, not a sales or retention ranking. Published descriptions establish precedent; none proves how Sankofa's target visitors will react.

| Primary source | Relevant precedent | Implication |
|---|---|---|
| [The Book of Distance, NFB](https://mediaspace.nfb.ca/comm/hailed-as-a-landmark-in-personal-vr-storytelling-randall-okitas-powerful-nfb-virtual-reality-production-the-book-of-distance-available-free-starting-october-8/) | Personal historical storytelling combining family archives, 2D/3D staging and participation | Cultural memory in spatial theatre is established. Specific authorship and evidence matter as much as the medium. |
| [The Line, ARVORE](https://arvore.io/games/the-line) | A miniature story about ordinary people in a specific place, advanced through physical mechanisms | Miniatures and hand-operated storytelling alone are not distinctive. |
| [Wolves in the Walls, director interview](https://www.unrealengine.com/blog/wolves-in-the-walls-it-s-all-over-comes-to-life-at-the-tribeca-film-festival) | Responsive character performance, object exchanges and interruption-aware direction | “Notices me” needs convincing performance, not only event handling. |
| [Gloomy Eyes, VIVE Originals](https://viveoriginals.com/portfolio/gloomy-eyes/index.html) | Narrated real-time spatial storytelling | A directed short is a credible artistic path; adding choices is not inherently an improvement. |
| [Museum of Other Realities](https://www.museumor.com/) | Immersive art, scale transformations and a social VR museum | The gallery is a familiar format; Sankofa needs a specific experience within it. |

Meta's [2025 results](https://developers.meta.com/vr/blog/meta-horizon-start-developer-competition-meet-the-winners/) name DreamSpace as Entertainment winner, Photon as runner-up and (m)ORPH as honorable mention. Its categories differed from 2026. [DreamSpace's entrant description](https://devpost.com/software/dreamspace-6g8rzm) emphasizes transforming a room through spatial windows and crafted environments. Our inference is that a simple, immediately visible spatial benefit is a useful benchmark. It does not establish that MR, reuse or AI caused the win, or that an illustrated narrative cannot compete.

## Where the reviewers differ

The competition and strategy reviewers favor object-triggered spatial memories. The experience reviewer favors a strongly directed encounter that makes both motives intelligible in the first visit. The distinction matters: exploration can interrupt emotion. Compare these forms; do not combine every recommendation into one larger feature list.

The experience reviewer also proposed two people privately seeing different memories before a shared meeting. That is a real alternative product, suited to a paired cultural installation. A cheap two-person tabletop/screen experiment could test it later. Networking, a second headset and shared object authority make it unsuitable as the default one-month plan.

Reviewers proposed different study sizes and return thresholds. The companion plan uses a small directional comparison followed by eight new participants for acceptance. These are practical decision gates, not statistically validated demand estimates. Nobody has yet run them.

## Technical implications

Reuse `VisitFlow`, the content validators, input adapters, local narration and explicit persistence. They are assets. No finding justifies a renderer migration, general engine rewrite, catalogue service or simultaneous SDK replacement. A real-hand probe should decide whether any narrow input behavior needs an ISDK replacement.

`LivingGallery` currently scales the framed image and transitions to the scene; it does not render a continuous stereo portal. `MemoryPerformance` reacts to authored narration IDs. A new gesture-driven memory therefore needs explicit flow/state ownership, cancellation, question/reveal rules and saved semantics, not a visual effect that independently advances the story.

Profile the risky moments rather than assuming the geometry is cheap: cutout-card overdraw, pavilion alpha fade, first material/audio loads, hand rendering and large captions. The code's ten-second frame summaries are useful but are not a sustained GPU capture or a percentile budget. Establish the target headset refresh rate, measure missed frames and warm behavior, and reduce actual bottlenecks. Do not use higher-resolution atlases or a quality governor as a substitute for measurement.

## Evidence inventory

- Current story/route/claims: [living-gallery.json](../../unity/Assets/Sankofa/Content/living-gallery.json), especially route lists, `e1.portions`, `e1.waiting`, `gallery.remember.answer`, `e1.after`, and `home.1850s`.
- Voice direction: [gallery narration manifest](../../unity/Assets/Sankofa/Content/enkare-nairobi.gallery-v4.narration.json). This audit did not listen.
- Interaction semantics: [VisitFlow](../../unity/Assets/Sankofa/Flow/VisitFlow.cs), [AttentionSource](../../unity/Assets/Sankofa/Runtime/Input/AttentionSource.cs), [PalmReceiver](../../unity/Assets/Sankofa/Runtime/Input/PalmReceiver.cs), [SankofaApp](../../unity/Assets/Sankofa/Runtime/App/SankofaApp.cs).
- Presentation: [LivingGallery](../../unity/Assets/Sankofa/Runtime/Stage/LivingGallery.cs), [MemoryPerformance](../../unity/Assets/Sankofa/Runtime/Stage/MemoryPerformance.cs), [GalleryAtmosphere](../../unity/Assets/Sankofa/Runtime/Stage/GalleryAtmosphere.cs), [GalleryKeep](../../unity/Assets/Sankofa/Runtime/Stage/GalleryKeep.cs).
- Inspected imagery: [entry](../media/living-gallery.png), [pavilion](../media/gallery-environment-side.png), [portions](../media/gallery-brings.png), [waiting](../media/gallery-remembered.png), [keep](../media/gallery-keep-guide.png), [today](../media/gallery-modern-river.png). Mixed October 14/15 desktop captures; not a continuous current build or device review.
- Build and release limits: [candidate](../release/candidate.md), [rights review](../release/rights-review.md), [adaptation summary](../product/adaptation.md), [workspace provenance](../workspace-provenance.md).
- Recorded checks inspected: `artifacts/tests/pavilion-final-editor.xml`, `pavilion-smooth-final-runtime.xml`; `artifacts/logs/polish1-editmode.xml`, `polish1-playmode.xml`; `artifacts/release/gallery-apk.json`. Raw artifacts stay local.

The [transformation plan](../product/experience-transformation-plan.md) turns these findings into one bounded experiment, a fallback, production priorities and evidence gates. It is a recommendation for the next iteration, not a claim that the changed product has been built or accepted.
