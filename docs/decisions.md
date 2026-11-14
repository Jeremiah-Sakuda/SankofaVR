# Decision record

## Separate SankofaVR workspace — September 30, 2026

The user explicitly requested a new folder and repository named **SankofaVR**. It is a separate private repository with a `main` branch. Product naming remains **Sankofa**. Current planning documents were copied and their workspace references updated. Previous Nafasi files, code and Git history remain intact in the original workspace.

This repository starts with documents and repository checks. No Unity implementation, web backend, model output, media asset or participant data is imported. The complete supplied competition references and original chat are copied locally under ignored folders. See [provenance](workspace-provenance.md).

## Product direction

The user requested modular implementation and a Git-ignored hackathon brief on September 30, 2026. `HACKATHON.local.md` holds competition-specific working context; tracked [agent instructions](../AGENTS.md) require reading it and explain recovery in a fresh checkout. [Architecture](architecture.md) separates the story core, application, XR, services, content, storage and production tooling. The brief is not packaged or read at runtime. Workspace checks verify exclusion and optional presence; implementation and competition acceptance remain unverified.

| Decision | Status |
|---|---|
| Sankofa product name | User selected |
| Fire acts as the griot; stories entered through the flame | User-selected direction |
| Personalised spatial stories from visitor interests | Core requirement; observable differences required |
| Broader histories and familiar languages/voices | Long-term ambition with explicit reviewed coverage |
| AI-generated music | Excluded by user |
| Generated narrative, voice and visual production | Proposed methods; suppliers and outputs unaccepted until evaluated |
| Adapted Entertainment | Recommended competition division for developing existing Sankofa; organisers retain authority |
| Full seated VR with hands throughout | Planning baseline; voice optional |
| MR hearth | P1, pending room benefit and device evidence |
| One setting, three scene plans, two narration languages | Proposed competition boundary |
| Swahili-coast setting and English/Kiswahili | Candidates; precise context, assets, reviewers and final pair unresolved |
| Solo, part-time; new to VR | Known profile; weekly hours, budget and current hardware access unresolved |
| Runtime technology | Unity/Quest proposed; no project created here yet |
| Repository visibility/licence | Private; no new source licence selected |

The [PRD](product/prd.md) and [delivery plan](product/delivery-plan.md) describe planned behaviour, not achieved results. Their acceptance gates remain open. The first proofs are the fire/transition and bounded personalisation, then their integration.

## Reuse and evidence

The original web Sankofa's source was inspected, including its cinematic intro, timing, context loader and narrative planning. The reported earlier judge response is the user's account. No live app, source import or model benchmark was performed as part of the new workspace setup.

The previous Nafasi workspace has a Unity technical baseline and music-specific tests. These remain possible reuse sources; their success does not validate this product. Record any deliberate import and associated rights when it happens.

## Stress test — October 1, 2026

The user requested a stress test of the framing and current assumptions. The [product review](reviews/2026-10-01-product-stress-test.md) challenges audience breadth, novelty, fire presence, personalisation, retention, content production and capacity. It recommends formative comparisons, not a confirmed positioning change. The competition-specific audit is local in ignored `references/hackathon/stress-test-2026-10-01.md`.

No prototype, participants, independent judge panel, provider benchmark or device acceptance is implied by this review. The broader cultural-story invitation remains a hypothesis alongside heritage-led positioning; no P0 requirements or user constraints were removed. Test evidence and any subsequent scope decision must be recorded separately.

## Implementation foundation — October 1, 2026

The user asked to begin development after the stress test. The first slice serves the PRD's fire and transition proof (G1) and builds the instruments for the stress test's fire-presence and embodied-consequence probes (T3, T4). No episode is selected, so a development placeholder exercises the structure. This does not change the PRD scope.

| Decision | Status and reason |
|---|---|
| Unity 6000.3.25f1, Meta XR Core 207.0.0, OpenXR 1.18.0, XR Hands 1.9.0, built-in renderer, Vulkan | Nafasi's previously configured package set; no new SDK added |
| Hands only through Meta's OpenXR hand skeleton; passthrough off | Full seated VR baseline; the room-aware hearth remains P1 |
| Engine-free `Sankofa.Core`, `Sankofa.Flow` and `Sankofa.Interaction`, also built and tested by .NET in CI | Architecture boundaries with deterministic tests that need no headset |
| Bundled episode JSON (schema 1) is validated before it can play; the application flow owns timing, permitted controls and cancellation tokens | SK-10 and SK-14 behaviour in software |
| One receive invitation: an open-palm gesture with a nearby select alternative; the passive default is recorded as no choice | SK-02 and SK-08 behaviour in software |
| Scene, materials and noise generated from code; single-light analytic shaders; synthesised fire sound | Reproducible scenes, low Quest cost, no third-party media to clear |
| Directed versus steady fire toggle in development builds | Instrument for T3; a fair comparison needs recorded narration |
| `com.sankofa.quest` application id | Provisional until confirmed before the first store upload |
| Development placeholder episode | Not a selected setting; makes no historical or cultural claims; the validator flags it as unreviewed |

Not established: device, simulator-with-hands, comfort, sound, performance or participant evidence. Episode selection, narration voice and recording, and the second language remain open.

## Hearth presentation — October 3–4, 2026

The user asked for a visual standard beyond the blockout. The hearth (present in every version of the story) was rebuilt procedurally: generated geometry, baked fire and moon shadows, two-tone lighting, a shared filmic grade in place of post-processing, synthesised night ambience, and TextMeshPro captions and labels with a fitted backing and a one-degree minimum line height. On October 4 the user approved downloading four 1K CC0 Poly Haven materials (rock, burned ground, dry ground, bark) and the OFL Atkinson Hyperlegible typeface, listed by file, source and size beforehand; their provenance is in the [asset register](asset-register.md). The story yard stays a blockout until an episode is chosen. Desktop renders and mixes are development aids, not Quest evidence.

## Setting and structure: Enkare Nairobi — October 4, 2026

The user asked for an evaluation of the proposal against the competition. Four parallel reviews were commissioned: a simulated judging panel, an advocate for depth, an advocate for breadth, and a research brief on comparable work. All four favoured specific content with a broad invitation over coverage of many cultures. These are judgements, not predictions of a result.

The user then chose to set the experience in **East Africa, their own history**. Their family is Maasai and Kikuyu. The chosen frame is **one place, many times**: the cool-water stream whose Maa name gave Nairobi its name, where the two peoples traded and intermarried. The visitor chooses *when* to go:

- before the railway (mid-1800s);
- when the railway came (1899);
- today, opened by a kept keepsake.

The user decided to keep the words *griot* and *Sankofa*, and to keep the narrator from the original web Sankofa. That repository is the user's copyright; its other contributors are AI agents acting for the user. The original's TTS configuration was Gemini `gemini-2.5-flash-preview-tts` with the voice Kore and a griot delivery direction. The adaptation of that direction to this world is still open.

On the user's instruction, Vertex AI was enabled on their "Sankofa Project" Google Cloud project for voice generation. No voice has been generated yet.

| Decision | Status |
|---|---|
| Setting: Enkare Nairobi across eras ([content](content/enkare-nairobi/README.md)) | User selected; replaces the Swahili-coast candidate and the placeholder package once built |
| Era choice replaces interest choice at the hearth | Proposed in the scene scope; satisfies observable personalisation through era and hand paths |
| Facts only from the [source register](content/enkare-nairobi/sources.md); medium-confidence claims gated | Adopted |
| People as fictional composites shown as ember-and-smoke figures | Proposed; needs a technical spike |
| The visitor's own fragments, woven in by a live model | Deferred beyond the MVP |
| Second narration language | Deferred; Gĩkũyũ and Maa words at the hearth, chosen by family |
| Family as reviewers | Requested; questions are in the source register |

This does not establish content review, cultural acceptance, voice quality or device results.

## The griot's voice and the first Enkare Nairobi build — October 4, 2026

The user asked to keep the original griot's voice using the gcloud CLI. The Gemini API rejected gcloud credentials, and Vertex AI did not serve the text-to-speech models. With the user's approval, the Cloud Text-to-Speech API was enabled on the same "Sankofa Project", and narration is generated through it with the user's own gcloud sign-in. No API key is stored in the repository.

The user compared the newer models and chose **Gemini 3.1 Flash TTS** (`gemini-3.1-flash-tts-preview`, a preview model) over 2.5 Flash. The voice is Kore, with the original web Sankofa's griot direction used verbatim; the user asked to keep that direction. `scripts/generate_narration.py` renders every line of the package ahead of time. The bundled build makes no network call to speak.

On the first pass the provider's safety filter refused two harmless lines; both rendered on retry. The script reports such failures rather than silently skipping a line.

The three water scenes, ember-and-smoke figures and synthesised story ambience replace the placeholder yard and road. The placeholder package remains only as a .NET test fixture.

| Decision | Status |
|---|---|
| Narration: Gemini 3.1 Flash TTS, voice Kore, original griot direction, pre-rendered and bundled | User selected; output terms (including the preview model's) to confirm before release |
| People as additive ember-and-smoke figures with no face or likeness | Implemented in software; not reviewed by family or on a headset |
| Story ambience synthesised in code (stream, insects, wind, hammers, city) | Superseded on October 7 by field recordings (see below) |
| The status line shows "Draft story · not yet reviewed by family or community" | Adopted until review |

Not established: pronunciation review, fluent-listener or community review, headset listening, or Quest performance with 85 compressed clips.

## Start screen, pointing and the original music — October 5, 2026

In the simulator, the user could not get past the choice of era. The panels accepted only a fingertip pushed through a button, and the simulator starts with controllers. Every panel can now also be used by pointing: a hand's system aim with an index pinch, or a Touch controller's trigger, with a select landing on release over the same button. This keeps every flow usable by hand, as the competition requires, with controllers optional. It also gives seated visitors a way to choose without reaching. The user asked for a start screen; a title card now waits for **Begin** before the fire speaks.

The user asked to use the music from the original web Sankofa. Its history records `intro-music.mp3` as "Traces of Old Hands" by Djovan, which replaced an earlier cover of "Papaoutai". It records `drums.mp3` as an "African drums loop". Neither has a recorded source or licence, and a web search did not identify the track. The music is **not imported**. Its source and licence, and confirmation that it is not AI-generated, are needed first.

| Decision | Status |
|---|---|
| Start screen before the welcome | User requested; implemented in software |
| Point-and-pinch and controller selection alongside fingertip presses | Implemented in software; simulator and device checks pending |
| Original Sankofa music | User requested; blocked on source, licence and the no-AI-music rule |

## Moving figures, scene sounds and the gated claims — October 6, 2026

The user asked to implement three things: the held-back claims, Wambui walking and kneeling, and the missing sounds.

**Claims.** Sources were read for the held-back claims (see the [source register](content/enkare-nairobi/sources.md)):
- C6 and C14 have stronger sources and higher confidence.
- C9 stays as narrated, in general terms.
- C16 (Indian labourers who stayed) is new, so the line "Many never went back" is now narrated.
- C5 (the women traders' protection) is still not narrated. A verbatim Leakey passage appears in secondary press, but its page is not confirmed.
- *Ng'aragu ya Ruraya* has a published gloss but stays unnarrated until the family confirms it.

No family review has happened; the questions remain the user's to send.

**Moving figures.** Stories gain a `move` action. It sends a figure to an object's approach point, where it stands or kneels, or home without a target. It is validated like `place`: the figure must be standing in the scene, and the target must be visible on every path. Presentation adds arrival and leaving walks. The kneel is a shader fold of the same silhouette.

**Sound.** All new sound is synthesised in code:
- birds, a distant sheep bell and the traders' crackle in the 1850s;
- the work gang and canvas in 1899;
- voices today;
- the ewe's bleat.

Voices are a wordless murmur until the people whose languages they represent are consulted. There is no radio and no music.

| Decision | Status |
|---|---|
| `move` story action with approach points | Implemented, validated and tested in software |
| Synthesised scene sounds, with wordless voices | Implemented; not listening-reviewed |
| C5 and the famine's name | Still gated, on the printed page and on family confirmation |

## Field recordings and gestures — October 7, 2026

The user did not want code-generated sound. They chose to replace all of it with public recordings under CC0 or CC BY, and approved downloading twelve Freesound previews, listed by file, source, licence and size. The recordings replace every synthesised sound, and the synthesis code is deleted. The hearth's sparks now follow cracks found in the recorded fire.

The 1899 work-gang murmur is removed rather than replaced: every available crowd recording speaks a real language, which would misrepresent who was there. Today's voices are a recording made at a matatu stage in Nairobi.

The figures now hold and hand over what they carry, through arm swings in the figure shader.

| Decision | Status |
|---|---|
| All sound from CC0/CC BY field recordings, credited in the app; no synthesis | Implemented; an EditMode test enforces it; not listening-reviewed |
| No voices in past scenes until the relevant languages are checked | Adopted |
| Figure gestures: carry, hold out until taken, set down, present | Implemented in software |

## Polish round 2: skinned figures — October 8, 2026

The user asked for a reviewer of the figures and animation. It scored them 4/10: hand-overs did not connect, the arms were rods and the kneel read as "sunk to the waist". It recommended a one-day spike with a CC0 rigged mannequin. The user approved downloading Quaternius's free *Universal Animation Library* (CC0, 15 MB) for it.

The spike is adopted. Re-shot motion strips show the change: real steps, a kneel at the hide, reaching and talking gestures, and carried things in the hands. The figures stay faceless and drawn only as ember light, with a plain wrap and shawl; clothing and gestures still need family review.

Also changed in this round:
- **Lineage:** each family line has its own ember colour.
- **Opacity:** a smoky core replaces pure additive glass.
- **Staging:** Wambui's and Njeri's places and the ewe's spot are re-staged, so paths clear the fire and the lantern.
- **Panels:** choices come up into view, while pause and return stay at the lap.

| Decision | Status |
|---|---|
| Skinned CC0 mannequin driven by our paths and timing, drawn in ember light | Adopted on desktop motion-strip evidence; Quest cost unmeasured |
| Kikuyu and Maasai family colours across eras | Implemented; family review pending |
| Procedural ewe | Kept with a better shape; leg motion not yet built |

## Polish round 3: leaving, keepsake, props and the city — October 8, 2026

- **Leaving:** a finished visit that is not kept is no longer lost with one stray press of **Leave**. The flow asks first, offering **Leave without keeping**, **Keep this visit** or **Stay**. .NET tests cover this.
- **The keepsake:** the bead or seed waiting on the hearth stone from a kept visit is larger and glows more.
- **Props:** the trading hide drapes over its rock.
- **Today:** the city's rooflines vary with set-back storeys, water tanks and stair heads. River ripples are no longer stretched across the width.

Desktop captures only.

## Polish round 4: bodies, the ewe, the city, rails and jiko — October 8, 2026

The user asked for sharper designs in four places, then repeated reviews until only human review remains.

- **Bodies:** the mannequin's build was a muscular man's, and its sculpted brow and nose showed through the ember rim. A reshaped copy of its mesh is now generated at import: arms drawn in toward their bones, a narrower and flatter chest, and the head smoothed to a plain ovoid. The skinning is unchanged, so every clip still fits. The shawl now slopes from the neck rather than squaring the shoulders.
- **The ewe:** her legs, neck and head are separate pivots. She walks in diagonal pairs in time with the distance she covers, and when she stands she lowers her head to graze now and then.
- **The skies were never drawn.** The camera's far clip was 80 m, but the story skies are spheres of 85 to 210 m, so every era showed only the background colour, and most of the city was cut off. The far clip is now 300 m. The dusk, the overcast morning and the city glow are visible for the first time.
- **Today's city:** each building has its own tone, window pattern and occupancy. There are slab edges at each storey, dark glass, half-curtained rooms, lit shops at street level and glass towers. Balconies face the river, and red aviation lights top the towers. Cars move on the far road, shown as headlights and tail lights. Trees frame both banks, keeping the view ahead open. The ribbed tower and conical hall of the Kenyatta International Convention Centre stand on the skyline as a recognisable landmark; their distance is compressed.
- **Rails:** an extruded flat-bottomed rail section replaces the boxes. The rails show mill scale rusting where rain has been, a smoother running surface, and fishplates with their bolts stacked across the far end.
- **The jiko:** a ceramic jiko as sold along Nairobi's roads. It has galvanised cladding pinched at the waist and sooted toward the lip, a fired-clay liner holding the coals, three pot rests and an ash door left open on the glow. Two cobs of maize roast on a wire grill over the back half, so the coal still shows.
- **Trees:** the forest crowns are many small rough clumps rather than a few smooth masses.
- **Test sound:** the user heard sped-up sound coming from the computer with nothing open. It came from the batch test runs, which play the story at several times real speed. Batch runs now mute the editor's output, and tests check sound from the clips' data. A new test checks that every recording and narration line imports at its true length; all do.
- **Checks added:** a geometry budget per scene (our own: 300,000 triangles and 175 renderers until a device profile replaces it), and wiring checks for the ewe's gait and the traffic.

| Decision | Status |
|---|---|
| Generated slender, featureless body mesh from the CC0 mannequin | Adopted on desktop captures; family review of the figures still pending |
| Far clip raised to 300 m | Adopted; Quest depth precision and GPU cost unmeasured |
| KICC silhouette on today's skyline | Adopted as a recognisable landmark with compressed distance; for the Nairobi review |
| Roasting maize on the jiko | Adopted as an everyday stall; part of the *jiko* review check |

Desktop captures only.

## Polish round 5: one silhouette, the flame entry, staging and hands — October 9, 2026

A panel of five judges and a figure reviewer scored round 4 on desktop captures: Innovation 6.5, Experience 6.5, Technical 7.5, Polish 5, figures 5.5 and props 6. The rules reviewer found the content viable but the delivery not yet started. This round acts on their findings.

- **Figures:** every figure part's depth is drawn before any figure's light, so body, wrap and shawl read as one silhouette and no anatomy shows through the cloth. Bodies are denser smoke with a brighter rim, the shawl is narrower, the head a little fuller and the fists slimmer.
- **Entering a story:** flame now spreads across the view from the ember the visitor carries, and the new place opens out from its own light. Behind the fronts the light settles to a dark ember rather than a flat bright field. Reduced motion keeps the short dim dissolve.
- **Personal space:** kneeling and standing places are over 1.1 m from the seat, and a test checks it. The trade is pushed back, the ewe stands between the women and dimmer, and the bead is offered from the hand.
- **Props:**
  - a faceted bead on a cord, and a leaf-wrapped seed;
  - an irregular, flatter hide;
  - tents with seams, doors and guy ropes;
  - rails turned to run away from the seat, with mill scale and rust patches;
  - a fenced garden gate;
  - the spring now falls from its rock's lip.
- **The city:** tall, narrow windows, wide shopfronts and a few taller towers. Lamp glows sit in front of their heads.
- **Hands and reading:**
  - a ring of firelight under an offered object shows where to hold an open palm and fills as it holds;
  - captions glide to stay within 25° of the visitor's gaze;
  - the settings fold behind one plaque;
  - the start text is larger.
- **Engineering:**
  - skies draw at the far plane, and the near clip is 5 cm;
  - pause stills the figures, the ewe, traffic and incidental sounds;
  - a PlayMode test presses Begin and an era with a simulated fingertip through the panels' own poke detection;
  - the shader test reads include files;
  - the head-vertex guard is fixed.
- **Credits:** complete and paged in the app, with source links for every CC BY recording and the OFL notices.

| Decision | Status |
|---|---|
| Kneeling figures at least 1.1 m from the seat | Tested in EditMode |
| Entry as a directional burn | Adopted on a desktop motion strip; comfort unverified in a headset |
| Hearth idle aside and "Is this true?" answer | Not built: they need new narration, which needs the user's approval to generate |

Desktop captures only.

## Polish round 6: hand-overs, captions over the scene, safer leaving — October 9, 2026

A second panel scored the round 5 build on desktop captures: Innovation 7, Experience 7, Technical 7, Polish 5, figures 6 and props 6. The rules reviewer found the content viable and the delivery not yet started.

- **Hand-overs:** a giver steps to the edge of the visitor's space and holds the gift out in her hand, with a two-bone arm reach. The hand-over places are tested.
- **Captions** draw over the scene, so heads and posts cannot cover them, and follow the gaze down to the hands as well as across.
- **Leaving:** Leave on a paused story asks first (tested), and the fire says a farewell line before the app closes.
- **Pause:** a palm or touch made while paused is no longer spent; receivers hold still until the story resumes.
- **The entry:** behind the burn the light is a darker, see-through ember rather than a flat bright field; the sky draws after opaque objects.
- **Figures and props:** cloth hems tucked, the wrap gathers when kneeling, heads filled, denser smoke; the ewe in clear sight; rails spaced; tent doors and hemp ropes; a wider spring; the gate set back; a shorter bead cord; fewer canopy clumps.
- **No engine splash or logo** (tested), as the competition's no-logos rule requires; the geometry budget counts render passes.

Desktop captures only.

## Polish round 7: gifts in reach, figures as firelight, a look answered — October 10, 2026

A third panel scored the round 6 build on desktop captures: Innovation 7, Experience 7, Technical 7, Polish 5, figures 5.5 and props 6. The rules reviewer again found the content viable and the delivery not started. They agreed on what held the scores: the figures still read as jointed mannequins, the gifts were small and handed over at the edges of the view, noticing changed nothing visible, the water read as static, and figures covered the captions.

- **Gifts in reach:** the giver keeps over a metre away and holds the gift out; it then leaves her hand and drifts on the firelight to a point in front of the visitor, at lap height and within reach (tested in PlayMode). The flour is handed over nearer the front. The bead is a larger trade bead and the seed bundle fuller, both glowing more.
- **The keepsake** waits in the firelight beside **Keep this visit** while the visitor decides (tested), and settles on the hearth stone once kept.
- **A look answered:** someone the visitor rests their gaze on turns her head to look back; when the visitor looks at a thing, the nearest person of the story looks at it too. This is presentation only: nothing advances, and head direction is still not treated as eye tracking or consent.
- **Figures:** the arms are smooth, tapering sleeves and the hands closed mittens, with the finger bones bound to the hand, so no ball joint or knuckle catches the rim. The edge burns with rising tongues rather than a smooth contour. The wrap flares to the hem, the walk is slower, and paths bend around the seat rather than crossing in front of it.
- **Captions** were still covered by figures: their always-on-top depth test lived on a property the text and backing shaders do not declare, so the saved materials never kept it. The caption board now sets it on its own copies at run time, and a PlayMode check confirms it.
- **Water:** ripples use only the broadest noise and calm with distance, the surface mirrors a sky gradient, and a touch brightens the water's sheen rather than tinting it pale blue. The spring falls as a rounded stream.
- **The hearth:** charcoal glows along a network of fine cracks and on its underside, rather than in round spots. The lap controls moved clear of the standing stone. At a waiting choice the buttons glow in turn every few seconds.
- **Engineering:** SpaceWarp stays off (Meta's setup fixes had turned it back on) and a test checks the Quest rendering settings; an offered object no longer stays half-lit through a pause; the application has its own generated icon instead of the engine's (tested).
- **Documents:** a [testing guide](product/testing-guide.md) for judges, a freeze checklist in the [delivery plan](product/delivery-plan.md), and the original's dates in the [adaptation summary](product/adaptation.md).

| Decision | Status |
|---|---|
| Gifts drift the last half metre on firelight | Adopted; tested in PlayMode; reach unverified on a headset |
| Figures answer the visitor's look | Adopted as presentation; not a story signal |
| Project-generated application icon | Adopted; replaces the engine default |

Desktop captures only.

## Polish round 8: what you lingered on, what you did, figures as fire — October 10, 2026

A fourth panel scored the round 7 build on desktop captures: Innovation 7, Experience 7, Technical 7.5, Polish 5.5, figures 5.5 and props 6. The rules reviewer found no hard failure and the launcher icon fixed, but the icon's flame missing. Innovation and Experience asked for the same thing: what the visitor does and notices should visibly change what follows, without needing a replay to see it. The figure reviewer found sparks settling where eyes would be on a head, and a kneeling head left hollow.

- **What you lingered on:** the journal ranks the story objects the visitor's head rested on, and a kept visit stores up to three. At home after a visit, small shapes of firelight of those things rise from the fire; when the kept visit is continued, they rise again from the scene's own light (the jiko today). Each object's echo is baked from its own shape at build time, people in their idle pose. Tested in .NET (ranking, keeping, the echo at home and in the continuation).
- **What you did:** at the closing, a line under the caption lists the steps the visitor took by hand, in the words of their buttons, or says the story went on without their hands. When the visitor does what the story asks, the nearest person of the story turns to look at them.
- **Heads:** each figure tells its shader where its neck is, standing or kneeling; above it the head is even, calm light with no sparks or flicker, so nothing reads as a face. The kneeling head is lifted, and a glance turns it.
- **Figures as fire:** flame rises through the body in two drifting layers, with thinner smoke between the tongues; the arms are fuller at the shoulder; the shawl's hem tucks under the wrap.
- **Gifts:** givers hold a gift out from a step beyond the visitor's space, and it drifts to rest a little toward their side and low, so it shows against the ground. The bead is banded glass; the seed's leaf is lighter; both glow more. Carried gourds hang from the hand by their necks.
- **Layout:** the questions step aside while the visitor is asked to touch or receive; the keepsake waits beside **Keep this visit**; captions default to Large; a button never flashes white before its colour.
- **Paths:** a way that would pass inside the visitor's personal space follows an arc around it, on arriving and on leaving.
- **Props:** the spring falls down the outside of its rock and soaks into the ground, and the half-buried rivulet is gone; the hide drapes four to seven centimetres; the traders' flame is narrower; the gate turns away from the seat.
- **Engineering:** the palm ring no longer flashes full when a pause closes it, and a hold begun before a pause starts again after it; a PlayMode test drives an open palm through the gesture with a simulated hand, paused and then resumed. The application logs its frame rate and CPU and GPU frame times every ten seconds. An EditMode test compiles every Sankofa shader, after a missing declaration turned the figures magenta in a capture that no other test noticed. The icon's flame is drawn. Hands and Touch controllers are both supported (hands first, every step with hands alone), and the manifest asks for hand tracking at the higher rate.
- **Documents:** the adaptation summary names the two pieces of the user's own writing that carry over; the testing guide explains looking, the echoes, what a kept visit stores and how to measure a run.

| Decision | Status |
|---|---|
| Kept visits store up to three objects the visitor faced most (head direction only) | Adopted; local only and only on Keep; listed in the testing guide |
| Echoes of what was lingered on at home and in the continuation | Adopted as presentation; no story line depends on it |
| Hands and controllers supported, hands first | Adopted; replaces hands only |
| Captions default to Large | Adopted |

Desktop captures only.

## Polish round 9: lasting echoes, a calm head, a gentler fire — October 10, 2026

A fifth panel scored the round 8 build on desktop captures: Innovation 7.5, Experience 7.5, Technical 7.5, Polish 5.5, figures 5.5 and props 6. The rules reviewer found no hard failure; the new APK carries its commit (`0.1.0+9250460`) and the icon its flame. The figure reviewer found that the round 8 head mask, a height cut at the neck, caught a kneeling figure's bowed back; the Polish and figure reviewers both still read the bodies as glass.

- **The head** is calm light inside a sphere about the head bone, posed each frame after the clip, the lift and any glance, so a bowed back is never mistaken for it and no dark gap settles in the face. Cloth never has a head.
- **The bodies:** a first attempt at near-opaque smoke with tongues of flame read as people on fire, which is wrong for this story; the adopted look is an even, warm glow through the body with gentler rising tongues, a narrow ember edge, a fuller head and a less flared wrap.
- **Echoes stay:** what the visitor lingered on rises from the fire to just below the captions and stays, dim and turning slowly, until the scene changes or a new visit begins. At the welcome back, the kept visit's echoes wait small beside its keepsake on the hearth stone, so keeping a visit visibly keeps them.
- **What you did** is said back plainly: "You took the ember. You touched the water. You carried the flour."
- **Gifts** come to rest on the side away from the giver, clear of her and of anything she set down; the bead turns its hole to the visitor, so it reads as a bead everywhere.
- **The entry:** once the flame covers the view, the place being entered already glimmers through the dark ember.
- **Return to the fire** asks first during a story: the story pauses and offers *Return to the fire* or *Stay in the story* (tested in .NET).
- **Height:** a setting raises or lowers the whole world by 8 cm, so every touch point, gift and panel moves together for the visitor's height and reach (PRD SK-15).
- **Engineering:** the touch receiver starts a hold again after a pause; the frame-timing log averages every frame of its ten seconds and runs from the start screen.
- **Props:** tent doors are part of the canvas.
- **Documents:** the adaptation summary states what replays exactly and names the Nafasi tooling; the Keep notice says what is kept; the testing guide points to the shortest complete visit.

| Decision | Status |
|---|---|
| Even warm glow rather than smoke and flame on the bodies | Adopted after a desktop comparison; family review of the figures pending |
| Echoes stay until the scene changes, and wait with the kept keepsake | Adopted as presentation |
| Return to the fire asks first | Adopted; tested |
| Height setting (±8 cm) | Adopted; untested on a headset |

Still needing the user: one new narration line so the bead's passive path does not say "to the one who carried the flour" when the visitor did not (the passive path currently claims the visitor's act).

Desktop captures only.

## Polish round 10: kneel, echoes in proportion, a clear closing — October 10, 2026

A sixth panel scored the round 9 build on desktop captures: Innovation 7.5, Experience 7.5, Technical 7.5, Polish 5.5, figures 6 and props 6. The rules reviewer found no hard failure. The technical reviewer found that echoes still waiting out their delay rose after being cleared, into a new visit or after a visit was forgotten.

- **Echoes:** a showing that is cleared ends its waiting echoes too (tested in PlayMode). Each echo is drawn after its own depth, so it reads as one silhouette; it adds light and never darkens the flame behind it; sizes are relative (a woman taller than a ewe, a ewe than a gourd, a hand's height at least); at home they stand in a row to the right of the fire, clear of the choices.
- **What you looked at longest** is named in the closing line, by the name the story gives it ("You looked longest at the ewe"); story objects the visitor can notice now carry such a name in the package, kept with a kept plan (tested in .NET).
- **The closing:** what the visitor did is set nearly at caption size; the keepsake waits just above the choice to keep the visit, clear of the fire.
- **Return to the fire,** while it asks, says what returning means: the story stops, and a visit left early cannot be kept.
- **Gifts** are offered from the seat's forward direction rather than the head's, so a turned head never moves them out of the reach the scene is built to, and rest on whichever side is clear of every person of the story. What the visitor carries without a tracked hand rests low and to the right.
- **Figures:** the wrap stays over the knees when kneeling; the shawl is a little fuller; the head is brighter where it faces the visitor, so it has volume, and lifts further when kneeling; bodies are nearly opaque.
- **The entry:** the place being left never shows through the burn; only the place being entered glimmers through, once it is shown.
- **Scenes:** the 1899 sky has heavier, uneven cloud and a thin warm dawn; the kept bead's glass holds a little of the firelight; the spring falls as a round stream.
- **Engineering:** a hold begun just before a pause must be made again after it (tested in PlayMode with a simulated hand); the perf log adds the slowest frame and the frames that missed 72 Hz.

Desktop captures only.

## Polish round 11: the kept visit stands again — October 10, 2026

A seventh panel scored the round 10 build on desktop captures and held every score: Innovation 7.5, Experience 7.5, Technical 7.5, Polish 5.5, figures 6 and props 6. All six named the same remaining levers that only the user can pull: honest passive narration lines, a run on a Quest, and the delivery steps. This round takes the cheap fixes they listed and one new idea from the innovation judge.

- **The kept visit stands again:** as a continuation opens on the same water, the people of the kept visit appear faint and life-size where they stood, over the place it has become, and fade after a few seconds. One place across time is seen as well as told. Presentation only; no story line depends on it.
- **Figures:** bodies are opaque at full smoke, so no horizon shows through them; the head is led by its outline rather than a bright centre and lifts less when kneeling; the kneeling wrap widens over the knees; the shawl is flatter front to back with a higher collar, and the arm is slimmer at the shoulder, so no hump shows behind it.
- **Echoes:** no part frays into smoke; sizes follow the things themselves more closely; the row is laid out from the seat once per showing, not from wherever the head is turned. The bead's glow is smaller and its pale band wider.
- **Tests:** the pause test holds most of the dwell before pausing, so it fails if a hold carried over a pause.

Desktop captures only.


## Polish round 12: fitted figures and a directed exchange — October 11, 2026

This pass responds to the October 9 independent panel and the user's request for a substantial improvement to figures and immersion. It changes presentation and recovery within the existing Enkare Nairobi slice. SK-11 (visual production), SK-14 (recovery), SK-15 (comfort/performance) and SK-12 (accurate memory) are affected; improved polish, readable embodied interaction and reliable technical behavior are the relevant judging goals. Desktop captures and software checks are evidence of implementation, not acceptance on Quest.

- **Figures:** replace the mannequin body with Quaternius's CC0 Universal Base Characters female and short hair, retain the compatible CC0 animation clips and expose anatomical faces and individual fingers, replace rigid lathed garment shells with a fitted skinned torso and folded skinned wrap, clip clean hem/neck/sleeve edges with interpolated skin weights instead of selecting ragged bone boundaries, and distinguish warm bronze bodies from quieter woven fabric. The figure shader uses the scene's actual lantern/fire and ambient light, with a restrained ember outline. Reveal opacity is separate from light intensity, so flicker no longer makes clothing transparent. Echoes retain their own brighter memory treatment. Plain clothing is a theatrical draft awaiting cultural review.
- **Sharing:** the giver approaches, both women raise and settle their vessels through two-arm posing, the giver tilts hers, a small visible flour stream joins the mouths, and both attend to the exchange before release. Authored actions start this performance; presentation cannot create a new story outcome. Pause freezes its clock.
- **Environment:** smaller, more numerous foliage clusters break the tree and acacia silhouettes; greener canopy material reveals some volume; a cooler, less uniformly overcast dawn separates the lantern and figures from the camp. The first foliage version exceeded 300,000 triangles and was reduced without raising that budget.
- **Truthful address:** the bead remains a hospitable gift on both paths, without claiming a passive visitor carried flour. The return line promises the saved story and choices and offers a continuation only if one exists. Three voice clips were regenerated with the existing approved voice to match their captions.
- **Recovery:** figure paths, reaches, delayed handovers, moving props and rising echoes keep coordinates in the stage when it is recentered or its height changes. Pointer recovery and hand/controller switching require a neutral sample before a fresh pinch can act.
- **Keeping and forgetting:** replacement writes one active shelf atomically. A durable empty shelf prevents an obsolete legacy JSON from reappearing after a crash or failed cleanup. Cleanup failures remain reported; corrupt legacy records are not silently deleted. Old per-visit records can be read for migration, and the generic legacy `Save` API remains available, but application Keep uses `Replace`.
- **Replay and attention:** the README now states the implemented path/outcome replay contract. Saved interrupted questions and timing are still not replayed; full SK-12 remains open. The closing describes returning attention rather than falsely claiming a duration-based “looked longest” measurement.

Verification: **114 .NET checks, 19 EditMode checks and 11 PlayMode checks passed** on the final implementation. The full PlayMode run covers both eras, passive completion, input, keep/continue/path replay, stale echoes, early return, stage recovery and pouring, and renders the journey plus close-up standing, kneeling and sharing studies. EditMode checks additionally validate every skin weight, animation binding and the complete garment after Unity baking. The fit check exposed a read-only mesh clone that rejected geometry writes; generation now owns writable meshes and imports the required CPU geometry. Garments are authored against the weighted bind matrices.

Visible scene budgets, counting both figure passes, are 123,877 triangles at the hearth, 261,968 before the railway, 222,536 in 1899 and 149,748 today (17–63 enabled renderers). These are geometry proxies, not measured device frame rates. No new judging panel or score was obtained. Actual Quest performance, cultural/context review, custom grip/facial acting and full realised question replay remain open. The changed figures are a new production base, not accepted historical costume or participant evidence.

## Polish round 13: clothing, hands and living foliage — October 11, 2026

The user asked to continue the visual work after round 12. This pass addresses SK-11 through the editor figure/mesh/material builders and `FigurePresence`, with SK-15 constraints enforced by the existing scene budget. The observable goals are cleaner animated clothing, a hand that closes around a carried object and opens on release, and crowns with visible leaf gaps and depth. These support Polish & Presentation and Experience Design; no device acceptance or new judging score is implied.

- **Clothing:** collars and clean sleeve cuffs retain the source character's skin weights, and cloth hangs below the chest. The neckline follows the visible mesh rather than the lower neck joint. The full body remains available during garment derivation; only afterwards are covered torso and leg surfaces removed, preventing both skin intersections and a missing hip seam. A separate torso loft was rejected after the kneeling capture exposed a neck mismatch. Source weights proved more reliable than guessed joint blends. Historical costume and bespoke garment modelling remain open.
- **Acting:** `FigurePresence` owns staggered blink deformations and finger poses over the library motion. Fingers curl toward the held object's opening or centre, the supporting hand participates during the exchange, and the hand opens when the object leaves. The stage clock freezes the acting on pause. The derived eye mesh includes project-authored pigment and a blink shape; this is limited acting, not a new facial performance rig.
- **Materials:** fabric weave fades below pixel resolution to reduce distance shimmer. The artist's faces and anatomy remain the generic fictional base. No identifiable likeness or cultural review is claimed.
- **Environment:** three-dimensional sprig clusters replace faceted crown solids. Original leaf artwork uses two-sided cutout rendering with depth writes, small wind motion and coverage-preserving mipmaps; it is not a camera-facing panorama. Leaf rectangles are excluded from the editor shadow bake, where they would otherwise cast solid rectangular shadows. The tradeoff is more foliage vertices and cutout overdraw, requiring real device profiling despite the lower triangle total.
- **Generation:** figure caches reset for each scene build, including repeated menu use in one editor session. Imported assets, source licences and the local-only exclusion rules remain in place. No new paid dependency, runtime service, music or external asset download was added.

Verification: **114 .NET checks, 20 EditMode checks and 12 PlayMode checks passed** on the final implementation. The added checks independently ray-test garment coverage around the waist, check the authored eye shape, pause during an observed blink, and verify that the held hand releases. The full journey captures and existing recovery/keep/replay checks also pass. Visible scene costs, counting both figure passes, are **126,985 triangles at the hearth, 243,292 before the railway, 204,980 in 1899 and 118,156 today**, with 17–63 enabled renderers. The review gallery compares the same journey view with round 12 (`e3788de`), with separate standing, kneeling and exchange pose studies. Software renders cannot establish Quest immersion, sustained performance, hand-tracking quality or cultural accuracy. Full question/interruption replay and the PRD's remaining personalised-story requirements remain open.

## Polish round 14: environment depth and signs of use — October 11, 2026

The user asked for an environment-focused pass because the spaces felt bare. This addresses SK-11 through the editor scene/material builders and a new presentation-only `SankofaEnvironment` module, with the existing SK-15 geometry limits retained. The visible goal is a sheltered hearth, an ecologically distinct riverbank and a camp/garden that reads as used. This supports Polish & Presentation and Experience Design; it does not add story branches or establish device acceptance.

- **Depth:** seeded low shrubs, fallen limbs, leaf litter and edge outcrops frame the clearing and historical water scenes. Bank sedges follow the stream outside the exchange corridor. The seat, near hand targets and central performer routes remain open. Details move in the existing stage frame.
- **Ground:** UV.x retains the fire's ash blend; UV.y now carries an authored organic-cover mask. Damp banks and woodland suppress the parched scan's cracks and normal relief; exposed and trampled earth retain the scan. The mask has smooth spatial variation, using the existing texture for grain. This adds one fragment texture lookup, requiring hardware profiling.
- **Use:** the camp adds slatted store crates, a simple awning, canvas sacks and hooped wooden barrels. Today's far bank gains planted timber beds. These are generic reconstructions, not verified period artefacts, species or a surveyed garden. No new external asset, paid service, music or runtime integration was added.
- **Lighting:** canopy material is brighter to preserve readable foliage; the existing overcast sky has stronger broad cloud variation. Combined static meshes receive the existing editor lighting bake, with alpha-cutout foliage excluded from rectangular shadow occlusion. Cutout overdraw, vertex memory and fragment cost remain device questions.
- **Evidence:** the gallery compares actual seated journey captures against round 13 (`a52337a`). A separate environment capture study looks toward the camp stores with captions and story objects hidden; it is labelled as staged, rather than visitor evidence.

Verification on implementation `75f9107`: **114 .NET checks, 20 EditMode checks and 13 PlayMode checks passed** (the PlayMode total includes the environment, figure and complete-journey capture studies). Existing pause, recenter, sharing, keep/forget and replay checks passed. Regenerated scene costs, counting both figure passes, are **148,009 triangles / 21 renderers at the hearth, 265,666 / 43 before the railway, 223,174 / 48 in 1899 and 132,780 / 70 today**. All remain below the unchanged 300,000-triangle / 175-renderer proxy limits. The competition workspace and document-link checks pass. These are software tests and desktop captures; no headset, sustained device performance or cultural acceptance is established. Tree forms, repeated tent silhouettes and city facades remain visibly procedural; bespoke environment art is still an opportunity.


## Polish round 15: contact and shared acting — October 11, 2026

This pass addresses PRD SK-11's embodied staging through the scene-presentation module, and keeps SK-15's existing geometry limits. Its judging relevance is experience and polish: the women should appear to handle the exchange together rather than slide to a mark and gesture independently. It adds no runtime model, service or story branch.

- **Walking:** the editor samples the registered CC0 walk's low-foot stance movement at each figure's scale. Measured reference speeds are 0.694–0.729 m/s, replacing the guessed 1.1 m/s playback divisor. Route movement accelerates, turns toward travel before advancing, and brakes on the last waypoint. `FigureGrounding` corrects planted stance feet in the stage's coordinate frame over authored route heights; this is not physical terrain sensing. Dedicated turning steps remain absent.
- **The exchange:** `FigureExchange` resolves the two actors after both base animations and before the fingers. Both look toward the vessels, lean slightly into the handling, then look up with a small acknowledgment. Explicit story/visitor glances take priority. The receiver's supporting hand now follows the exchange weight, and the giver's wrist follows the vessel tilt. The stream still joins actual gourd openings. The application retains ownership of when an exchange starts.
- **Kneeling:** a generated constant pose replaces the repeated generic repair action. It preserves the source posture and garment fit; it is quiet resting, not a bespoke listening performance.
- **Evidence:** new PlayMode checks observe planted-foot movement while the body advances, and both actors' hand/head/foot poses and vessel endpoints through pause and stage recenter. A separate silent 22-second approach/exchange/return study captures the final poses at a fixed 12 fps, with controls hidden. This is an inspection cadence, not measured headset performance.

Generic base motions, limited facial expression and procedural clothing remain visible limitations. No Quest run, cultural acceptance or prize outcome is established. The same 300,000-triangle / 175-renderer proxy limits apply; no new render geometry is added by these motion components.

Verification on implementation `f6c7d73`: **114 .NET, 20 EditMode and 16 PlayMode checks passed**, including the new contact and shared-pose checks and runtime capture studies. Full kept/replayed, passive and early-return journeys passed. The geometry proxy counts remain **148,009 / 21 at the hearth, 265,666 / 43 before the railway, 223,174 / 48 in 1899 and 132,780 / 70 today** (triangles / renderers). The refreshed gallery and motion study use the tested scene. No simulator-with-hands or device run occurred in this pass.

## Illustrated-theatre MVP — October 11, 2026

The user authorised a prototype of spatial illustrated theatre before committing to a full conversion. This affects SK-08's physical consequence, SK-11's visual production, SK-12's saved outcomes and SK-15's stationary, accessible presentation. The owning module is Unity scene presentation; accepted actions remain in the application/core. Its judging goals are an immediately legible physical interaction and better presentation with less bespoke animation work.

- **One reusable chapter:** a separate `IllustratedTheatre` scene and bundled package retain the 1850s story and its existing continuation. The hearth, fire transition, field recordings, narration, hands/controllers, passive flow and keepsakes reuse the foundation. The baseline `Hearth` scene is preserved.
- **Physical crossing:** the gourd begins at Wambui's illustrated palm and moves into reachable space. Receiving it changes her painted pose; setting it on the trading hide changes both women. The second beat uses the same three-pose vocabulary and layout configuration. Neither gaze nor scenery silently accepts a choice. Painted memories replace legacy humanoid silhouettes for these characters.
- **Depth and production:** three alpha-cutout scenery cards unfold far-to-near. Depth writes and stereo macros preserve occlusion and multiview rendering; Android imports use ASTC 6×6. Pose dissolves and scene reveal use the pause-aware stage clock. Reduced motion reveals the scenic composition together. Two generated atlases have full prompts, hashes and draft status in their adjacent `sources.json`, with in-app credits. They are fictional theatrical art, not verified dress, portraits or terrain. No AI music, runtime model, backend or paid asset was added.
- **Tradeoffs:** cards remain flat when viewed from the side; painterly people do not have rigged facial acting. This prototype tests that form from a seated vantage and does not establish immersion or comfort on Quest. The remaining personalised-generation/live-conversation PRD requirements stay open. The MVP APK shares the baseline application ID, but its kept visits use a separate local folder.

Implementation checkpoint: `fbed1b3`. The editor checks validate the bundled package, asset wiring, alpha/compression and absence of humanoid rigs in the illustrated chapter. Counting additional material passes in the same way as the baseline, its geometry proxy is **70,918 triangles / 28 enabled renderers** versus **265,666 / 43** in the baseline chapter. Large cutout coverage, fragment cost, stereo depth and sustained performance still require actual device profiling. The preview is a silent desktop runtime capture driven by the test flow, with a fixed 12 fps inspection cadence; it does not demonstrate visitor hand tracking.

Verification: **114 .NET, 23 EditMode and 15 PlayMode checks passed**. The PlayMode selection covers every behavior/recovery test in `HearthVisitTests`, `StageRecenterTests` and `IllustratedTheatreTests`, including the new capture, passive response, moving gift through pause/recenter, painted memories and full keep/replay. The four older baseline capture studies were not rerun. The five-second handover preview and three gallery stills were exported from the final runtime captures; the competition workspace, local-only exclusions and document-link checks pass. No actual headset or cultural acceptance is established.

The MVP APK built from `f9b3bfc` as **0.1.0+f9b3bfc**, 81.2 MB, ARM64, with zero build errors. The APK privacy scan confirms local development credentials are absent. The Meta XR Simulator activated and rendered the MVP's hearth/start screen and simulated hand models in the editor; Play mode was stopped after this launch/visual smoke check. This was not a complete simulator gesture journey and adds no device tracking, comfort or performance evidence.


## October 11, 2026 — illustrated story and response pass

The user requested an iteration focused on a compelling story, meaningful response to participation, cultural respect and clarity for an unfamiliar visitor before further visual/animation work. The illustrated package is now **v0.2.0, What We Carry**. Requirements: SK-08 (accepted actions and consequences), SK-12 (truthful kept outcomes), SK-15 (passive/accessible participation), plus the PRD's evidence/reconstruction distinction. Owning modules: authored content and existing core conditions/recall, with the narration production adapter. Experience design and presentation are the relevant judging lenses. No new runtime AI integration is claimed.

- **Dramatic change:** Wambui admits bringing less flour than promised; Naserian's decision follows a remembered occasion when Wambui waited for her. Each has household needs. The visitor participates as a guest while the women make their own agreement. The bead recalls this particular meeting.
- **Observable reactions:** the offer prompt distinguishes a gourd held from one set beside the visitor. Naserian's address, the return narration and a saved continuation distinguish helping at the trade from witnessing it. Receiving the bead is remembered separately. At today's gate, a passive visitor retains the bead rather than having it hung automatically. Existing hands, select alternatives, attention-question cues and pause/replay mechanics own all actions; head direction never accepts an invitation.
- **Curiosity and pacing:** character questions explain motives; answers about the agreement become available after its reveal, and a short rest lets the visitor ask about the bead. Redundant setup was removed. The main first visit is roughly seven minutes with recorded speech, before optional questions and response time. This remains an authored, offline episode.
- **Cultural boundaries:** the spoken disclosure precedes scene entry, including an early route selection that skips the welcome. The package removes a fixed traditional price, a general deferred-payment custom and invented modern descendants. The modern garden is explicitly imagined. The Akan title's Ghanaian origin is named. The [source register](content/enkare-nairobi/sources.md#illustrated-theatre-story-pass--october-11-2026) records fresh source inspection and its limits; the printed Leakey page, family/community judgment and pronunciation remain open. The baseline 3D script is retained as a labelled comparison, not the revised illustrated content.
- **Narration integrity:** 65 recordings were produced in the separate `enkare-nairobi.illustrated-v2` namespace; 64 are used after the final pacing trim, alongside 10 unchanged baseline clips. Earlier clip keys remain intact for kept-plan replay. The manifest records provider, model, voice, direction, exact text, timing and fingerprints. Unity rejected an initial 103-second render of an eight-word line; it was replaced, and the pipeline now checks duration before replacing a recording and never attaches mismatched speech after a failed regeneration. These checks do not establish that speech or pronunciation sounds right.

Verification: **152 .NET checks, 23 Unity EditMode checks, 3 focused Unity PlayMode checks and 4 offline narration-pipeline checks passed**. The .NET suite exercises all 32 first-chapter outcome combinations, mixed-path recall, both modern bead placements, attention without consent, optional questions after the reveal, pause/stale completion and exact saved path/word/audio replay. Unity plays the full accepted/replayed story and the fully passive first visit plus continuation with recorded clips; it checks the passive bead position. The capture-only PlayMode test was intentionally skipped. No new images, animation, Quest build, headset run, participant acceptance or cultural approval is established by this pass.

Local evidence is in `artifacts/logs/story-editmode.xml`, `story-playmode.xml`, and `illustrated-story-dotnet.log`. `artifacts/story-pass/helping-guest.mp3` and `quiet-guest.mp3` are recorded script read-throughs that omit input waits, transitions, questions and ambience; they are not captured VR sessions. The [current script and response map](content/enkare-nairobi/stories.md#current-illustrated-episode-what-we-carry) provide concrete family/community and outsider comprehension questions. Existing gallery images and the previous APK predate this story revision.

## October 11, 2026 — illustrated voice pass

The user's review asked for a warmer storyteller without undoing the reciprocal promise or truthful visitor responses. Package **v0.3.0** keeps the agreement, all acceptance/default conditions, and saved recall intact. Requirements: SK-08 (actual action consequences), SK-12 (replay), SK-15 (passive and one-hand participation), and the evidence/reconstruction distinction. Modules: authored content and recorded narration; no new runtime mechanism. Experience design and presentation are the judging lenses, with the script and recorded paths as review evidence.

Twenty lines were recorded under `enkare-nairobi.illustrated-v3`; all 75 playable lines have audio, retaining earlier clip keys for unchanged text and old saved visits. Spoken bibliography and either/or invitations are replaced with bounded accounts, character invitations and a patient-fire aside. Forest/plain texture and Enkare Nairobi return. C15 is restored from a directly read UN-Habitat account, with ongoing work distinguished from the fictional garden. Its optional answer now describes fictional neighbours planting and tending it. The script records the unfinished promise as the writing direction for the future 1899 conversion; that chapter remains unconverted.

**Stage finding:** a fresh desktop capture at `e1.rest` (`artifacts/illustrated-theatre/06b-sheep-promise.png`) confirms the 3D ewe is present during “Take the sheep,” but its translucent body overlaps rock shapes and reads poorly between the women. Object existence is not visual acceptance. A legible sheep silhouette must be resolved in the next art pass; this voice-only pass does not claim that fix or alter the atlases. The rocks/hide and double-water issues also remain open.

Review audio is in `artifacts/voice-pass/helping-guest.mp3` and `quiet-guest.mp3`. The helping path lasts 6m39s with the export's small inter-line pauses; both omit response windows, optional questions, menus, transitions and ambience. These are recorded script review aids, not VR session recordings. Maa pronunciation, performance quality and cultural acceptance still require listening and community review. No APK or headset evidence is added.

Verification: **152 .NET, 23 Unity EditMode and 4 focused PlayMode tests passed**. The PlayMode run includes accepted/kept/replayed and passive/continuation paths, pause/recenter, and the new promise-frame capture. A final wording review shortened the carried-gourd invitation to “Touch the hide” so it does not suggest requiring a second hand; its regenerated recording passes the repeated .NET manifest/text/duration checks and a final Unity recording-import test (`voice-final-recordings.xml`). Logs: `artifacts/logs/illustrated-voice-dotnet.log`, `voice-editmode.xml`, and `voice-playmode.xml`. No participant or cultural acceptance follows from these software results.

## October 11, 2026 — remaining-work sprint proposal

The user requested a sprint breakdown. The [current delivery plan](product/delivery-plan.md#current-sprint-plan) now sequences five weekly sprints from October 12 to November 15: encounter proof, theatre cohesion, return visit, device/user acceptance and submission preparation. The [backlog](backlog.md#current-illustrated-theatre-sprints) carries the remaining tickets. Weekly capacity is provisionally 12–15 focused hours; current hardware access and actual availability are not assumed confirmed.

The recommendation commits planning capacity to the first illustrated encounter and contemporary continuation, with 1899 conditional on additional capacity after core gates. It explicitly exposes the gap between the authored build and the original PRD's generated planning, live conversation and language requirements; those requirements are not marked complete or silently removed. This is a release-profile proposal, not user approval of that reduction. Device access, cultural review and rights work start in the first sprint, with final acceptance protected from feature expansion.

The owning modules are authored content, stage/presentation, flow/storage, platform integration and release documentation. SK-08/11/12/15/18 connect the plan to observable response, visual/sound coherence, remembered return, reliable accessible use and submission evidence. Innovation/Experience are tested by an intelligible embodied consequence and a worthwhile return; Technical/Polish by actual-device use and the reviewed presentation. No code, assets, purchases, outreach, channel uploads or scheduled automations were performed for this planning pass.

## Sprint 1: look, ask and act audit — October 12, 2026

S1-03 audited how the illustrated episode answers each kind of participation ([audit](reviews/2026-10-12-look-ask-act-audit.md)). Requirement SK-08; modules: input (attention), stage presentation. Judging lenses: experience design (the promise that the story notices what you notice) and technical robustness.

- **Looking at a person never registered,** in either presentation. Attention candidates were centred on each stage object's transform, which for a person is the ground at her feet; from the seat, facing her body fell outside the tracker's allowance for all four women, and the spring missed by 12°. Earlier captures called `Notice` directly, so no test or render exercised the real path. Attention now centres on the middle of what shows of the object. An EditMode test checks, in both scenes, that facing the visible centre of every noticeable object counts as facing it; it failed for the women and the spring before the fix.
- **Painted figures answer.** A shared `IGlancer` lets the stage ask a 3D or painted person to look. A painted woman turns her card up to 15° toward the visitor (when looked at, or when the visitor acts) or toward what the visitor looked at, her light lifts, and she settles back; pause holds the turn. This is an interim acknowledgement until a painted look-at-you pose exists (S2-02). Looking still never advances the story or accepts an invitation.

Desktop and batch software evidence only; whether the turn reads as attention, and whether head-direction noticing feels attentive or twitchy, needs a headset.

**Build for the first device session (S1-01):** the illustrated APK built from `977c78b` as `0.1.0+977c78b`, 84.6 MB, zero errors, development credentials verified absent (SHA-256 `9af2ed7a…0334`). It is debug-signed and has not run on a headset; Quest access, the supported-device decision and the first hands, depth, reach and performance findings remain open. Review packets for family and first-time listeners are built by `scripts/make_review_packet.py`; they have not been sent.

## Sprint 2: one painted world — October 12, 2026

Captures of the illustrated chapter showed modelled stand-ins doubling or contradicting the painting. Requirement SK-12 (visual coherence); module: stage presentation, through the theatre scene builder only, so the 3D baseline is unchanged. Judging lens: polish and experience design.

- **One river.** The painted water is the stream. The modelled stream ribbon and its pale bank stones are off in the theatre; the touchable water spill stays.
- **No double image on a pose change.** Changing pose used to cross-fade two silhouettes, which showed two heads. The figure's light now dips, the pose swaps at the bottom, and the light returns, like a stage lamp; one pose is drawn at a time. A change during a change continues from what is showing at the same brightness. Tested in PlayMode.
- **Props in the paintings' language.** The gourds, bead and hide use a new painted-prop shader: the fire's direction, lifted, gives three tone bands with brush-broken edges, violet-leaning shadows and an ink edge. The ground is tinted toward the painting's earth, so the women no longer stand on a near-black floor.
- **The sheep is heard, not seen.** The translucent ewe read as a blob between the women. Until a painted sheep exists it is hidden in the theatre; it stays in the story and its bleats play, but it cannot be noticed or remembered. Tradeoff: the narration still says Naserian "has brought the sheep" and "Take the sheep" with no sheep in view, carried by sound alone. A painted sheep (S2-01) waits on the art provider decision.

Desktop captures and batch tests only. Whether the dip reads as theatre or as a flicker, and whether the props sit in the painting at Quest resolution and lens distortion, needs a headset.

## October 12, 2026 — illustrated polish passes

The user requested further product polish. SK-08/11/15 own observable acknowledgement, coherent presentation and reduced-motion/recovery behavior; modules: scene builder, stage presentation and shaders. Experience Design and Polish are the main judging lenses, with Technical Implementation protected by regression checks.

- **Read the story on stage:** the ewe now uses solid fleece and dark face/leg pigments, while retaining its existing walk, graze, bleats, pause and departure. It is visible during the promise. Its memory mesh includes head and legs instead of an isolated body. The solid shader respects story reveal/withdraw and ignores the ewe's legacy black vertex masks. This replaces the earlier sound-only workaround; it is a theatrical animal, not a claim about a historical breed.
- **Keep the hand-off believable:** when an illustrated woman turns, the physical palm anchor turns with the card and follows the actually displayed pose. Stage-relative glance targets survive recenter, including during pause. A new regression test checks the palm stays at the same position inside the painting.
- **Make the painting sit in the world:** softer ink edges and warm brown pigments replace the bright vessel treatment, and small soft contact patches ground the two figures. Reduced-motion mode keeps the light acknowledgement but removes card turning and the pose-change dip; a focused test checks both the stationary figure and retained highlight.

Verification for this checkpoint: **152 .NET, 26 EditMode and 13 PlayMode tests passed**, including the full accepted/kept/replayed encounter, fully passive encounter/continuation, sheep visibility/departure, new palm/recenter and reduced-motion cases, and existing 3D stage-recenter checks. Capture frames are real desktop runtime renders in `artifacts/illustrated-theatre`; the updated promise frame was inspected. Geometry proxy: **45,368 triangles / 28 renderers** in the illustrated chapter. Logs: `artifacts/logs/polish-dotnet.log`, `polish-editmode.xml`, `polish-playmode.xml`. This establishes software and desktop staging evidence; Quest performance/comfort, cultural review and final cast art remain open.

The follow-up staging refinement adds small authored eye, highlight and muzzle marks to the animal's moving neck, using one extra painted surface. All seven animal surfaces participate in reveal/withdraw, and its full memory mesh includes the face. The final promise, unfolded and hand-over frames were inspected; the README gallery and preview now show the v0.3 story after these polish changes. Final geometry proxy: **46,968 triangles / 29 renderers**. The final scene again passed **26 EditMode checks**, and the two targeted final PlayMode checks passed the new capture and complete sheep reveal/departure (`polish-final-editmode.xml`, `polish-final-capture.xml`). The 13-test playthrough/recenter regression run above covers the unchanged interaction logic. The animal remains a modest procedural theatrical form; character expression/clothing and actual Quest presentation still need review.

**Updated review APK:** built from `de7cc00` to `artifacts/Sankofa-IllustratedTheatre.apk`, **84.6 MB, zero errors**, with development credentials verified absent. SHA-256: `f55524ed3b59fdd28ec2d6dc47a173b744ae6e5d5cc0b3a7683cde5dba3d2106`. The single build warning is Meta's optional recommendation to prioritise input polling; its device tradeoff remains for the first headset profile. Build log: `artifacts/logs/polish-apk-build.log`. The APK has not been installed on Quest or uploaded to a release channel. The refreshed hand-over preview is saved in `docs/media/illustrated-handover.mp4`; opening it in Codex was queued by the app.

## Judging-criteria audit and first fixes — October 13, 2026

At the user's request, four read-only subagents audited the illustrated theatre at `8c169bc`, one per judging criterion ([audit](reviews/2026-10-13-judging-criteria-audit.md)). Requirements SK-08 (attention and hand participation), SK-11 (the hand-off), SK-15 (recovery and hands-only use) and SK-12 (coherent presentation). Modules: flow, interaction, input, stage and the theatre builder.

- **Silence begins the only story.** With one first story and no kept visit, 15 s of silence after the welcome begins it, consistent with the PRD's passive participation. A returning visitor is still asked, because their choices differ. Reversible through `FlowSettings.SingleStoryWait`.
- **Gifts leave from the painted palms.** The gift waits for the offering pose to show, rests in the palms, then crosses. Receiving it early cancels the crossing.
- **The flour crosses back into the painting.** After it settles on the reachable hide, the hide and the flour move to rest between the two women. Tradeoff: the narration still introduces the hide as already "between them" while it lies within the visitor's reach. It is true only after the crossing.
- **Tracking flickers are forgiven.** A hand keeps its last confident pose for 0.18 s, and a pinch may slip off its button for 0.15 s. Both are opt-in and engine-free where tested; the values are guesses until a headset run.
- **Title card and sky.** The draft notice sits once on the title card, not over its hint; the theatre's moon halo is reduced.

Release signing, the app id, version codes and the supported-device list remain user decisions (Technical audit item 1). So does letting attention play an existing recorded answer. Desktop and batch evidence only.

## Plan for first place — October 13, 2026 (proposal)

After five repository-only scoring reviews (one per judging criterion and one on potential), the user asked for a comprehensive improvement plan aimed at the Entertainment first prize, drawing on the earlier sprint estimates, the 2025 winners and the original web Sankofa. The [plan](product/first-place-plan.md) is a proposal awaiting the user's decisions D1–D6; it changes no requirement by itself. It keeps the five-sprint gates and cut order, reallocates the device-session hours to art, novice sessions and the video, and organises work around the user's hours (56–66 h over five weeks) rather than build cycles. Its seven moves: a fire that visibly listens (pinch-to-ask, the leaning flame, attention asides), hands before words in the first minute (feed the fire, "what do you carry?"), one painted world that the fire paints, a visible consequence and a hook to return, touch that answers everywhere, a build defensible without a device (quality governor, trimmed cards, warm-up, lifecycle tests), and the submission itself. Mixed reality, live conversation, the 1899 chapter and a second language are set aside with reasons. The rules accept XR Simulator footage; no headset exists for this project, and the plan says so.


## October 13, 2026 — detailed illustrated theatre scope

The user requested detailed scoping after reviewing the first-place proposal. The [illustrated theatre product scope](product/illustrated-theatre-scope.md) is now the current implementation proposal, with delivery/backlog links reconciled. It proposes two short authored revelations (what Wambui brings, what Naserian remembers), stationary fire-painted entry, deliberate object questions, milk hospitality tied to actual flour placement, and explicit kept return with a painted modern coda. Both women retain agency over the same agreement, and quiet participation receives equal hospitality.

Automatic spoken attention asides, whole-object factual-status glow, universal touch effects and a one-line cliffhanger payoff are excluded. Device access/testing is restored as an open dependency, not replaced by quality settings. Optional effects are cut before coherent bookends, story meaning or acceptance work. Planned effort is 52–65 user hours plus 10 hours contingency and 76–112 assisted engineering hours, to be re-estimated after the first prototype. Estimates are not judge scores or winning probabilities.

Requirements affected: SK-01/02/04/05/07–13/15/18. Modules: story composition/validation, flow, input, illustrated presentation, narration and persistence. Observable evidence sought: distinct pre-ending revelations, unambiguous ask/receive, visible hospitality difference, faithful return and complete hardware-tested passive/one-hand paths. A current integration constraint is the single exact parent route in `PlanComposer` and single `Next` in saved visits; multiple perspectives need deliberate continuation metadata and legacy compatibility.

This is documentation-only scoping, not implementation, release acceptance or external-action authorisation. Broader generation, live conversation and additional-language requirements remain open in the PRD. Runtime scripts, packages, recordings and scenes were not changed. The earlier first-place plan remains as a superseded proposal. The next proposed implementation increment is IT-P01/P02 using existing assets and temporary staging, before final asset production.


## October 13, 2026 — living gallery direction selected

The user asked to fold the gallery into the main scope, then retained the griot as a narrator filling contextual gaps without giving the story away. Sankofa is now framed as the gallery and *What We Carry* as its first exhibit. The [scope](product/illustrated-theatre-scope.md) integrates preview, explicit seated entry, recognizable frame-to-world transformation, two authored perspectives and return to the canonical painting with an explicitly saved memento beside it. The standalone opening/closing hearth becomes an exhibit-level fire presence, not an extra mandatory lobby.

The griot's authored context is gated by route and revealed beat; placards, hints and questions follow the same spoiler boundary. Quiet participation remains complete after deliberate entry. Gallery browsing never triggers the former 15-second auto-entry. Looking alone never chooses a perspective, asks a question or saves. The gallery metaphor does not require locomotion or a multi-exhibit catalogue for this release.

PRD v1.1, delivery plan, backlog, architecture, adaptation note, README and agent guidance now reflect the selected direction while keeping implemented theater behavior distinct. Estimated net integration increase: 2–3 user hours and 6–10 engineering hours; totals 54–68 user hours plus ten-hour reserve, and 82–122 assisted engineering hours. Re-estimate after the frame-to-depth-to-frame prototype; no acceptance time is silently removed to make it fit.

Affected requirements: SK-01/02/05/07–13/15/18. Modules: flow, exhibit/content metadata, presentation, narration and saved-visit mapping. Evidence sought: visitors understand entry without walking, recognize the same composition inside and after the story, explain a detail differently on return, and receive helpful context without unreached reveals. This change updates planning only; no Unity assets, package behavior, narration or release configuration changed. Generated-story/live-language requirements remain open.


## October 13, 2026 — first living gallery implementation

The user authorised execution of the selected gallery scope, then requested no sound playback and no simulator. A generated `LivingGallery` scene now reuses the theatre's story/input/store foundation: static canonical painting derived from the actual staged scene, explicit entry, stationary expansion and reframing, seated controls, and a memento beside the exhibit. Two authored routes add bounded flour/preparation and earlier-waiting vignettes. Milk hospitality follows actual flour placement, with equal access on the quiet path. Object question chips use the flow's existing beat availability; the friendship question moves after the agreement to avoid spoiling the turn. Physical Keep requires receiving the memento, an open-hand dwell over the recess, then release; tracking loss cancels arming.

Two continuation route definitions share the modern coda beats and use the existing single-parent contract. This avoids changing saved-file schema; a focused .NET check verifies v0.3 replay is not replaced with v0.4 content. The receive validator permits a complete pair of prior outcomes to offer a vessel directly or set it within reach; a negative test rejects a missing presentation path.

New narration: twenty lines, generated through the existing Google Cloud TTS pipeline with the user's explicit export approval after automatic approval review initially rejected the export. Versioned namespace `enkare-nairobi.gallery-v4` preserves old clips. Files were generated without playback; no simulator was launched. Cultural/voice listening review remains pending.

First checkpoint: **292 .NET, 28 Unity EditMode, four gallery PlayMode tests passed**. PlayMode is muted batch software evidence with synthetic input, not a simulator/headset session. Tests cover both thread revelations/continuations, preview waiting, passive/reduced-motion stationary flow and an explicit synthetic-hand keep. Runtime captures expose remaining presentation issues (vignette scale/placement and closing caption clutter), which are the next polish increment. No headset, cultural or participant acceptance is claimed.


## October 13, 2026 — keep the original storytelling purpose

During implementation, the user asked to retain Sankofa's focus on stories that may have been lost to time while keeping broad appeal. The gallery's purpose is now expressed as “stories time nearly left behind.” An accessible human situation leads each exhibit; specific cultural context supplies identity. *What We Carry* remains an imagined encounter grounded in documented context, not a claimed rediscovered account of real people. This framing changes gallery/product copy; it adds no runtime generation, new historical claim or additional narration export.


## October 13, 2026 — gallery polish and software verification

The frame now sits in a charcoal gallery with its purpose, “Stories time nearly left behind,” above *What We Carry*. Entry remains explicit. Smaller vignette cards leave the women visible; closing caption clutter is removed. The candidate excludes the unused 1899 rigs while retaining them in the comparison scene. A saved visit offers the other thread directly without replacing old kept plans.

The modern coda uses a provenance-recorded OpenAI-generated river painting, expanded to cover the forward view, with a softly fading nearby bank and tangible gate, soil and bead. The old modelled river and distant city are hidden so there is one water. Its gate touch point and gallery controls/recess stay within the conservative seated reach used in software checks; real hands, stereo depth, overdraw and comfort are still device gates. This image was generated with the built-in image tool; its model name was not exposed and is not inferred. Output terms and cultural review remain pending.

Tracking grace can retain a last-known hand visually, but palm/touch receivers now reject that pose as a new act. A focused PlayMode test verifies that stale tracking cannot complete a receive and that live input can. Reduced-motion return uses a dissolve without a scale jump.

Verification: **292 .NET tests**, **28 Unity EditMode tests**, **seven distinct gallery PlayMode tests**, and **four offline narration-pipeline tests** passed. The full five-test gallery run covered both routes and continuations, passive/reduced-motion flow, explicit synthetic-hand keep and an object question that neither accepts an invitation nor reveals the later memory. Additional runs covered the modern composition and stale-hand rejection; final focused runs repeated entrance and keep after layout edits. These are muted batch tests with synthetic input. No audio was played and no simulator was launched.

Current review stills: [gallery](media/living-gallery.jpg), [remembered meeting](media/gallery-remembered.jpg), [modern river](media/gallery-modern-river.jpg). The latter is a staged composition check; full route flow is verified separately. Vignette illustrations remain prototypes. Hardware, participant, cultural, pronunciation/listening, rights and release-channel acceptance remain open. The implementation is the scoped authored candidate, not completion of the broader generated-story/live-language PRD.


## October 13, 2026 — living gallery review APK

Built from committed source **389070f** with Unity 6000.3.25f1, Android ARM64/IL2CPP, in muted batch mode. Output: ignored `artifacts/Sankofa-LivingGallery.apk`, **84,129,058 bytes (80.2 MiB)**. Build succeeded with zero errors and one reported warning. Package metadata: `com.sankofa.quest`, version code 1, version name `0.1.0+389070f`, minimum SDK 32 and target SDK 34. This is the provisional local review build, not a Competition-channel release or an installed/tested Quest build.

SHA-256: `d11358c1e2313de43f486b10abe13c876624dec19c6d90ba55735c4d6029576c`. The build's privacy guard reported local development credentials absent. An additional archive-filename check found no local competition brief, raw reference directories, LocalOnly folder, environment file or keystore. The guard scans the actual captured development credential values; the filename check is narrower and is not a general security audit. Project settings were restored by the build tool; the source tree was clean afterward. No sound playback, simulator, headset installation, upload or submission occurred.


## October 13, 2026 — distinguish default entry from a chosen perspective

The next iteration found that gallery Enter used the same chosen-route method as a deliberate perspective selection. The core now exposes `BeginDefault` under the same route/phase/continuation checks. The application uses it only for Enter; perspective controls still choose explicitly. Journals and newly kept visits retain `perspective_selection` as default or explicit; legacy records without the optional field remain unknown and are not rewritten to imply a preference. Replay carries the saved provenance without altering its plan/outcomes. Schema 1 remains readable by the current application; older application binaries that reject unknown fields cannot read the new optional field, so use the new build for new records.

Requirements SK-04/12/16 and gallery entry contract; modules core, flow, storage and application. Observable evidence: **295 .NET tests passed**, including default/explicit persistence, replay, illegal entry rejection and legacy unknown-origin round-trip. No new historical claims, narration export, playback or simulator run. This is software evidence for truthful interaction, not user or device acceptance.


## October 13, 2026 — entrance, memory windows and deliberate Keep polish

Requirements SK-01/02/07/12/15/16; modules Unity UI/stage/build tooling and deposit feedback. The entrance has one full-width Enter action and two smaller perspective controls; Leave stays with Pause at the lap. Button hit bounds match the wider primary action. Compact rows leave the full return menu visible in the review capture. The draft/reconstruction placard remains separate from the controls. General UI/UX skill defaults informed hierarchy and stable feedback; its search produced no verified VR-specific match, so no mobile pixel guideline is treated as a Quest acceptance measurement.

The former blank memory cards now have warm painted borders, scene-consistent materials, reused shore/water/cast layers and larger time/portion labels. They preserve the two authored revelations and the same agreement; no new narration or generated art is introduced. Memory backing now uses the story visibility shader. The new stills remain desktop software evidence and the art remains a cultural-review draft.

Physical Keep now gives contextual receive/dwell/close guidance only when a completed visit can be saved. A brightening recess provides dwell feedback. The object moves into the recess only after persistence succeeds; a failed write retains hand ownership and an open/dwell/release can retry. Guidance hides after success. The first runtime run passed six of seven checks but the new hint assertion inspected the same frame the final line finished, before presentation updated. The corrected test waits for actual guidance visibility, then deliberately blocks the save directory, verifies no saved visit and continued hand ownership, removes the obstruction and retries. The final focused three-test run passed, covering that recovery, both routes/continuation and preview/hit targeting. All seven distinct gallery tests passed across these runs; they are not seven fresh headset tests. **295 .NET** and **28 EditMode** checks also passed.

Review stills: [entry](media/living-gallery.jpg), [preparation](media/gallery-brings.jpg), [earlier meeting](media/gallery-remembered.jpg), [deposit guidance](media/gallery-keep-guide.jpg). Captures use synthetic input; no actual hands, listening review or Quest performance is established. No sound was played, simulator launched, third-party material imported or text sent to a provider during this iteration.


## October 13, 2026 — refreshed iteration review APK

Local review APK rebuilt from clean committed source **d0a17bd**, with zero build errors and one reported OpenXR validation warning (optional input-polling latency preference). Package `com.sankofa.quest`, version `0.1.0+d0a17bd`, version code 1; ARM64/IL2CPP. Output is the ignored `artifacts/Sankofa-LivingGallery.apk`, **84,134,918 bytes (80.2 MiB)**, SHA-256 `f3d7c11736e75ca297921324cfe0476b1e8c3086ffdf75fa786ed8d7742a9073`. The build privacy guard again verified captured local development credentials absent; archive-name checks found no private reference or credential filenames. No headset installation, sound playback, simulator launch, upload or submission occurred.

The final placard-spacing adjustment passed all 28 EditMode checks and the focused preview/hit-targeting test; its refreshed entry image shows both disclosure lines without control overlap. The other current route, question, passive/reduced-motion, stale-hand, modern-scene and physical-save checks are described above. Cultural, listening, participant, rights and Quest acceptance remain open.


## October 13, 2026 — independent gallery judging panel

At the user's explicit request, three fresh-context agents independently judged repository b3fa0d6 / APK source d0a17bd against the freshly read official criteria. Each scored all four categories, without prior panel reports or inter-reviewer discussion. The factual brief did not advocate a desired verdict; required repository context exposed the prize objective, which reviewers were instructed to exclude as scoring evidence. This was not double-blind or an official/human jury.

The [panel synthesis](reviews/2026-10-13-independent-gallery-panel.md) records editorial ratings around 6.2–6.5/10, strengths in embodied exchange and truthful agency, weaknesses in visible dramatic performance/focal art/repeat value, and source-checked pointer, test-coverage and physical-save replacement findings. The reduced-motion test's direct property assignment is overwritten by application settings, so prior coverage claims must be narrowed to passive stationary flow until the authoritative mode is tested. Both continuation parents have core coverage, but the Unity loop follows only the second retained parent. These are evidence corrections, not observed failures of the two features.

No app edits, fresh executable tests/builds, sound playback, simulator, external messages, uploads or submission occurred during this panel. Detailed raw notes remain ignored local review artifacts. Hardware, participant, listening, cultural, rights and release acceptance remain open. The panel provides a proposed priority order; it does not revise the PRD or establish a win probability.


## October 14, 2026 — judging-panel implementation, gallery v0.5

Implemented the bounded suggestions in the [panel response](reviews/2026-10-14-panel-implementation.md): revision-aware pointer release and confidence gating, replacement-aware physical Keep with a second deliberate deposit, authoritative reduced-mode testing, both-parent runtime continuation coverage, cancelled/settled transition recovery, earlier ember invitation, focused memory revisits, authored concern/listening/reassurance poses, moving imagined miniatures, detailed gourd rims/cord and weathered gate pigment, and a quieter closing hierarchy. The original gift atlas/poses and realised legacy replay content remain available. The selected shorter telling saves its own beat list and only its actual invitation outcomes; continuation never invents skipped flour/water/milk acts.

Existing approved audio is reordered/reused. No new narration text was exported. One fictional expression atlas was generated using the built-in OpenAI image tool, referenced to the original cast; exact prompt, reference/hash/dimensions and unexposed model identifier are recorded in its source register. Public terms were read; applicable account/service contract, cultural suitability and release rights remain unverified. No AI music, sound playback or simulator launch occurred.

Verification: **301 .NET tests passed** in `artifacts/logs/panel-core.log`; **all 38 Unity PlayMode tests passed in one clean final run**, `panel-final-playmode.xml` (including all eleven gallery checks and older comparison/replay flows). The separate corrected performance probe also passed. Early failed attempts remain local artifacts: an invalid shorter ember withdrawal was rejected by validation, a memory binding was absent, a test used the wrong sheep ID and its error list persisted across cases; these and compilation/wiring issues were repaired. The first editor pass also exposed the optional gallery adapter in a comparison-scene wiring check; its explicit exemption is backed by gallery-specific mandatory binding validation. **All 29 final EditMode checks passed**, `panel-final-editmode.xml`. The final visual review corrected overbright gate pigment; the separate final modern-scene runtime check passed (`panel-final-modern.xml`), with an updated capture. That material-only change does not alter the tested flow or input code. Build identity is recorded below.

Updated silent software captures: [shortfall](media/gallery-shortfall.jpg), [provisions](media/gallery-brings.jpg), [earlier meeting](media/gallery-remembered.jpg), [return](media/gallery-return.jpg), [modern river](media/gallery-modern.jpg), [Keep guidance](media/gallery-keep-guide.jpg). These use a desktop camera and synthetic/direct inputs, not a Quest or participant. They show the revised performance and composition, not actual stereo depth, hand-tracking reliability, device frame rate or listening acceptance. Caption/object overlap and sheep/bead legibility remain practical items in the prepared [next evidence session](testing/gallery-next-evidence-session.md).

Declared passive schedule for a focused revisit is 249.18–252.08 seconds versus 499.04–501.94 for the full encounter, excluding transitions/questions/system pauses. The first ember offer action is authored around 11.7 seconds after entry while cultural/fiction disclosure still occurs before entering the spatial story. These are schedule calculations; novice comprehension, sensory quality and actual visit duration have not been established.


## October 14, 2026 — gallery v0.5 review APK

Rebuilt the local review APK from clean committed source **eed1d4e** after the panel implementation and final material correction. Build succeeded with **zero errors and one reported OpenXR validation warning** (optional input-polling latency preference); IL2CPP also emitted TextMeshPro compilation-partition notices. Output `artifacts/Sankofa-LivingGallery.apk`, package `com.sankofa.quest`, version `0.1.0+eed1d4e`, version code 1, ARM64/IL2CPP. **84,319,508 bytes (80.41 MiB)**; SHA-256 `27d93bab6c6ef6b15eeb37ea828b73fb1ceec0a6b8f3f0aefa81f30ca28b9576`. Build log: `artifacts/logs/panel-build.log`.

The existing privacy guard verified local development credentials absent and restored editor-only settings afterward. Archive-name checks found no private-brief/reference/LocalOnly/credential filename matches. The workspace check confirms private context remains excluded; filename checks alone are not a complete data audit. No headset install, sound playback, simulator, upload or submission occurred. Final software evidence is 301 core checks, 29 editor checks, all 38 runtime checks in one clean run, plus a passing final modern-scene capture after the pigment-only correction. Device, participant, cultural, listening, applicable asset terms and release access remain open.


## October 14, 2026 — complete available gallery judging follow-up

Package v0.6.0 advances the full passive agreement to 250.85/253.75 seconds and hospitality to 267.61/270.51 seconds of authored scheduling. The total full passive schedules are 432.03/434.93 seconds, excluding transitions, questions and user/system pauses. Sensory context follows hospitality; flour and household explanations become optional. Disclosure and the Maa name remain, and the women retain their independent agreement. No new narration request or playback occurs.

Native focal work refines the ewe, bead and gate, isolates candidate pigments, removes the ewe’s inherited long arrival and removes the bead’s obscuring glow card. Captions choose bounded clear placements for offered/carried objects; long recorded captions are tested at Medium and Large. Context questions are presented once, object-side when possible or centrally as a fallback; questions about one subject occupy separate rows. The unchecked street recording is excluded from the candidate. Cultural and actual headset acceptance remain open.

Verification: 303 .NET checks, 30 editor checks, a final clean 14-check gallery runtime pass and one preserved Hearth fingertip/shared-caption check pass. Earlier failures (shared material, demo API name, synthetic-anchor cleanup) were repaired and failed reports retained locally. This pass has not repeated the entire older 38-case runtime suite; the previous broad pass remains recorded above. Muted current captures and a 27-second labelled silent desktop/synthetic software preview are prepared. No Quest, real-hand, participant or listening evidence is implied.

Current cultural and blind first-time offline packets include exact recorded read-throughs/transcripts, all other current contextual/question/modern speech for pronunciation review, draft art, and explicit local notes export. Phone/landscape/desktop QA verifies layout, labels, no autoplay and no external requests. No reviewers were contacted or actual responses collected. The integrity audit checks 30 declared inputs and 86 current line fingerprints with no integrity problems; applicable provider agreements and contributor/cultural clearance remain separate gates.

The [release record](release/candidate.md), [rights review](release/rights-review.md), [submission draft](release/submission-draft.md) and [judge access/video handoff](release/access-handoff.md) make the remaining account/hardware/reviewer steps concrete. A read-only ADB inventory reported no connected device. No simulator, sound playback, upload or public submission occurs. Source/build identity follows after the review APK rebuild.


## October 14, 2026 — v0.6 review build and handoff

The review APK built from clean source **da85045**, package `com.sankofa.quest`, version `0.1.0+da85045`, version code 1, ARM64/IL2CPP. Build succeeded with zero errors and one reported OpenXR validation warning concerning optional input-polling latency preference. Output: `artifacts/Sankofa-LivingGallery.apk`, 84,335,456 bytes (80.43 MiB); SHA-256 `f3d189ec76b67be60b82798dcb2af40be2ef58a0547341821d1672291d234b8e`. The existing guard verified captured development credentials absent and restored editor-only settings; archive-name inspection found no private-context/LocalOnly/credential/key filename matches. Source remained clean after build.

Final evidence is 303 core checks, 30 editor checks, 14 gallery runtime checks in a clean final run, one preserved Hearth fingertip/caption check, 30 declared asset hashes and 86 current speech fingerprints, plus passing offline packet QA at phone/landscape/desktop sizes. Current packets and a 27-second silent labelled software preview are local review deliverables. No sound playback, simulator, Quest installation, reviewer session, public video, upload or submission occurred. The [candidate record](release/candidate.md) links the review, rights, account-access and demonstration handoffs.

## October 14 — production hardening of the living gallery

**Implemented/software-verified:** [production iteration](reviews/2026-10-14-production-iteration.md), package v0.7. Selection/replay carry has logical state and visible placement; captions protect memories, the named animal and essential controls. Release diagnostics no longer log interaction journals/cues. Modal preferences preserve visitor/system pauses, with caption-only narration retaining recording length. Eye-relative placement, height/reach, first-visit context and explicit replay boundaries are implemented. Registered cutout feet/palms, bank-rested vessels, transient acknowledgements and a provenance-recorded painted ewe replace visible prototype defects. The earlier reversal is exclusive to Naserian's perspective; focused returns use original recordings and reach new material quickly. BuildMain and guides target LivingGallery.

**Evidence:** 306 core, 31 editor and 46 complete functional runtime checks pass, with no skipped final runtime cases; 31 declared inputs and 92 narration fingerprints match. The first full run's clipped carry failed, was fixed and passes the final run. A labelled 41-second silent desktop preview now includes frame expansion, both memories and paired hospitality; capture and encoding cadence are separately recorded. Review packets were checked at three widths with no autoplay/external requests. No sound playback, simulator, new narration export, participant/cultural review or actual Quest acceptance occurred. Final APK identity is in the candidate record; account/access/rights and device/human gates remain open.

## October 15 — requested muted simulator verification

The user authorised simulator use while retaining the no-sound restriction. Added the direct LivingGallery launcher and ran the actual Meta OpenXR Simulator 207 with Unity `-noaudio`. An isolated editor-only probe passed 29 assertions across default/other-memory routes, accepted/passive outcomes, modal pause/resume, Keep, replay, modern continuation, Forget and confirmed early return. It restored comfort preferences and was removed from Assets after the run. The existing APK and runtime content were unchanged.

The [verification record](reviews/2026-10-15-gallery-simulator.md) separates these scripted commands from unsuccessful manual point-and-click attempts. Manual hands/controller selection remains unresolved, and the default view confirms that perspective controls obscure the wall context placard. Accelerated captures also retain a real-time save notice briefly; this is not evidence of persistent normal-speed overlap. Actual Quest performance, physical reach/tracking, listening and human/content acceptance remain open.

## October 15 — simulator input workflow and layout recovery

The [follow-up](reviews/2026-10-15-gallery-input-and-layout.md) resolves manual controller review: native movement followed by a deliberate trigger press/release selects through the unchanged pointer adapter. Click-only automation had supplied a trigger with stale aim. Five controls were exercised manually, and explicit Naserian entry was verified again after the fixes. Hands and actual Quest acceptance remain open.

The regenerated gallery places title-origin/setting/disclosure text beside the frame, clear of close entry/expanded-return controls across 108 projection combinations. A fresh launch exposed tracking arriving after the five-second fallback: WorldAnchor now recovers that first untracked placement once a seated pose settles, preserving reach/height without firing startup twice or following later head motion. Ten focused runtime checks pass. The first narrower-FOV label attempt failed and was corrected; failed artifacts remain local. Existing speech/story/gesture data is unchanged, with 31 declared assets and 92 speech fingerprints still matching. The previous broad suite is historical evidence; final editor and APK identity follow in the candidate record.

Final validation: **31/31 editor checks passed** after the recovery change. A fresh muted simulator launch recorded automatic stage-height correction from 0 to 0.55 m for the 1.70 m head and successful manual explicit Naserian entry. Clean-source **5f0d2ce** APK rebuilt with zero errors and one optional OpenXR validation warning: 84,142,748 bytes, SHA-256 `6331e36a74bbabf808ce33806cca4f2de22217c8769a1cbc6246aa149995d33f`. The [candidate](release/candidate.md) records exact identity and limitations; old APK preserved locally. No sound, new provider export, actual-device installation, upload or submission occurred.

## October 15 — architectural museum pass

The user requested a better museum environment. The living gallery now has a complete contemporary surround: warm plaster exhibit recess, walnut bays and ceiling ribs, a mineral ceiling, stone slab floor, bronze joints/cove lights, a rear bench and a supported ledge. A single-pass architectural shader reuses project grain, with no new art downloads or dynamic lights. The stepped frame and shadow gap withdraw with the painting; the room withdraws with its home root. This is presentation work under the existing seated gallery scope, not another exhibit or a claimed historical building.

The [environment review](reviews/2026-10-15-gallery-environment.md) records 306 core checks, 31 editor checks, seven targeted runtime checks and two final presentation rechecks. Both story routes/Keep/modern return, passive reduced motion, pointer cancellation, settings, placement and enclosure/withdrawal pass. Projection checks now cover all five wall text blocks across 108 combinations. Muted simulator manual entry succeeded; its clipped right-hand branding prompted a central wordmark and left context column. Captures are desktop renders. Actual Quest GPU/comfort/hand tracking and content acceptance remain open. No sound playback or new provider export occurred.

Final museum APK: clean source **5dbf149**, `com.sankofa.quest`, `0.1.0+5dbf149`, ARM64/IL2CPP, 84,149,396 bytes, SHA-256 `a1856a019042c111f150e14519307521cc268b62f00a08504d39def2339d0f5a`. Build succeeded with zero errors and one optional OpenXR validation warning. Privacy guard passed; narrow archive-name scan found no private context/credential matches. Source was clean after settings restoration. Previous APK preserved locally; no installation or upload. See the [candidate record](release/candidate.md).


## October 15 — immersion follow-up: open garden pavilion

The user found the first museum pass insufficiently immersive. Replaced the rectangular enclosure with a curved timber vault, open sky spine, real garden arcades and freestanding exhibit wall. Decorative foreground planting, distant trees/hills and analytic cool floor light add spatial depth from the existing seat. Architecture/text now dissolve with painting expansion and restore on return/recovery through `GalleryAtmosphere`; pause holds presentation and reduced motion keeps its existing fade without painting expansion. Optional shallow plaque bodies retain the existing button hit planes and default off in comparison scenes. No camera locomotion, new story content, generated imagery or audio was added.

The [current environment record](reviews/2026-10-15-gallery-environment.md) preserves the initial roof intersections/horizon seam and their correction. Nine final runtime checks pass, covering both paths, captions, input, apertures, labels, pause/motion, recovery and return. Generated geometry is modest but new cutout overdraw and architectural shading still require Quest profiling. This remains fictional contemporary staging with software/desktop evidence; cultural, participant and actual device acceptance stay open.

Final smooth-fade verification passes **306 core, 31 editor and 9 runtime checks**. A paused visual probe exposed coarse cutout noise; it was replaced by cached temporary blended materials, with original opaque assets restored on return/recovery. Original-material identity and standard/reduced/pause behavior pass. The optional legacy plaque reference is permitted only with disabled body depth. A muted native simulator session verified manual entry, normal passive progression to the river and confirmed early return to the restored pavilion. Actual Quest and human/content acceptance remain open.

Final pavilion review APK: clean source **493c3aa**, `com.sankofa.quest`, version `0.1.0+493c3aa`, ARM64/IL2CPP, 84,883,472 bytes (80.95 MiB), SHA-256 `90a0c1f98a5c5ef9bacf2ab9c4fd4f6f86c5fcecdff5f74f6d74d90515c87f55`. Build succeeded with zero errors and one optional OpenXR input-polling warning. Privacy guard verified local development credentials absent; narrow archive-name scan found no private context/credential matches. Source was clean after settings restoration. The previous 5dbf149 APK/identity remain archived locally. No sound, actual device installation, provider export or release-channel upload occurred.


## October 18–19, 2026 — personal story table MVP

**Implemented/software-verified:** the user selected a return to personal curiosity, expressed through an illustrated story table beside the fire. `StoryTable` is now the default scene/APK; earlier gallery/theatre/3D scenes remain comparison records. Typed questions and interests shape either a clearly prepared telling or bounded Google-generated fiction. Historical passages stay exact/source-linked, illustrations use approved local IDs, and unknown imagined questions receive fiction context rather than a failed-fact notice. The application owns consent, accepted state, cancellation and stale-response rejection.

Measured text pages, full request confirmation naming the service/provider, source/question return, pause, local failure recovery, closing reread, explicit Keep/replacement/reopen/Forget, manual stand and optional MRUK surface fitting are implemented. Room permission follows Fit my table and has bounded recovery. Surface axes preserve containment. Setup now preserves networking and optional passthrough/scene support. Reopening clears the transient narration proof, so saved replay cannot fetch fresh speech; local prepared recordings remain available. Generated text is saved, generated audio is not. No original web source/media or AI-generated music was imported.

**Verification:** 349 core checks, 32 service checks, 36 editor checks, and 16 unique table-runtime checks pass; after the final local-replay patch all nine actual-app checks were repeated successfully. An opt-in actual authenticated Unity HTTP test passed two question-only Google generations with the same interest and a real MP3 download/decode/cache while narration remained disabled. The final retained Unity pair changes fictional dilemmas and outcomes; both choose waiting. A later direct-service three-case semantic review separately supports better role preservation after an observed giver/receiver reversal. Neither sample set establishes a general adherence rate.

Independent limited-context simulated judging found and helped resolve input confirmation, overflow, explanatory prose, delayed personal relevance, hosted authentication, local-error consent bypass, first-install room permission, rotated surface containment, ending navigation, capability labels, known scene mismatch and role fidelity. Subsequent review retains editorial/pacing/spatial-value concerns and human/device/release gates. See the [iteration and panel record](reviews/2026-10-18-personal-table-iteration.md) and [candidate identity](release/candidate.md) for exact final outcomes; no official score or prize likelihood is implied.

The build uses the existing privacy sanitizer, with development credentials excluded and editor settings restored. A failed initial Android compile exposed an unsupported SDK setter; the corrected build succeeded. A further build-identity fix prevents repeated commit suffixes and persists restoration. Eight unrelated pre-existing dirty gallery/font/animation files are preserved and excluded from these commits. Device hands/reach/stereo/passthrough/performance, unfamiliar visitors, cultural/source/listening acceptance, rights, eligibility, public HTTPS deployment and independent Competition-channel access remain open. No sound played, no new XR Simulator or Quest session ran, and no upload or final competition submission occurred. The new Devpost copy is prepared locally; the available Devpost session is signed out and its edit destination is unconfirmed.

## October 26, 2026 — judged-path audit and perspective choice

**Implemented/software-verified:** the user asked for an audit of the implementation and its assumptions and an iteration toward a winning entry. The [judged-path audit](reviews/2026-10-26-judged-path-audit.md) reviews the build as judges would install it: on Quest, hands only, and most likely without a story service, because the checked-in scene has no endpoint. On that path the October 19 build was silent, offered an online action that could only fail, and gave the visitor no decision inside the story.

| Decision | Status and reason |
|---|---|
| Each bundled telling offers *whose side to follow* at its documented page | Implemented. Agency over what is revealed, never over a character's decision, preserving character autonomy. Three new chapters; the nine existing chapter texts and recordings are unchanged |
| Turning the page at a choice follows the authored default | Implemented. The default is the fire's choice and is never recorded or described as the visitor's (SK-08) |
| Story chapters may carry two or three forward-only choices; a chapter a choice leads to ends the telling | Implemented in the engine-free contract and validator; linear tellings, provider output and earlier kept files serialise exactly as before |
| The first page names the recognised words that chose the thread | Implemented. Catalogue vocabulary only, never visitor text; presentation only, never saved |
| Voice reads by default on the headset; editor, simulator and batch sessions stay silent | Implemented in the app; listening review of all chapters is required before release |
| The fire's flame follows the voice; a small-fire field recording plays at the fire on the headset, owned by the app, credited in How it works | Implemented. The tabletop view still creates no sound source |
| Without a configured service the intake offers Begin, Add my question and How it works | Implemented; a configured endpoint restores Create my story and its consent page |
| The narration manifest lists every bundled chapter; unrecorded chapters are `pending-recording` | Implemented with a sync test and `scripts/generate_table_narration.py` |
| Not changed: the C1 page's visible review note, the "Imagined scene:" caption prefix, the backend's own fallback tellings, the comparison scenes and `PokePanel` | Recorded in the audit as decisions for the user or as later work |

**Verification:** 358 core checks pass locally on a .NET 8 SDK (CI uses .NET 10), 32 service checks and 9 narration-script checks pass. The changed Unity runtime files and PlayMode tests were type-checked against hand-written stubs only: Unity did not compile them, EditMode/PlayMode suites did not run, the scene was not regenerated and no capture, APK, sound playback or device session occurred. The restored local competition brief came from the user's pasted official overview and rules; the FAQ and special-awards pages were not reachable from this session.

## October 26, 2026 — pop-up illustrations

**Implemented/type-checked only:** at the user's request, the next item from the [judged-path audit](reviews/2026-10-26-judged-path-audit.md#pop-up-illustration) was done in the same session. `StoryTableView` raises each print's existing painted layers toward the reader by 1.2 cm per layer, to 3.6 cm for the figures. A newly turned page rises from flat over half a second; reduced motion shows final depth at once; pause holds it; returning to the same page keeps it standing. Raised layers are drawn in toward the print's centre in proportion to their depth against the measured viewing distance, plus a 2.5% inset, so from the seat the composition keeps its painted size and stays within its backdrop. No scene regeneration, story data, validator or saved-telling change is involved. The view test formerly named for a "flat" focal image is renamed `ChapterReplacementKeepsOneFocalImageAndTruthfulCapability`; its assertions are unchanged.

**Verification:** a numerical geometry model of the builder's layout, ray-cast from the seated eye and from heads moved ±10–15 cm vertically and ±20 cm sideways, keeps every raised layer within its backdrop and clear of the label, caption and title. The new PlayMode test `PrintLayersRiseInDepthWithinTheirBackdropAndHoldStillWhenAsked` and the view changes were type-checked against stubs only. Unity did not compile or run them, and no capture, headset view or performance measurement occurred.

## October 30, 2026 — Unity verification of the judged-path iteration; pop-up stage direction

**Software-verified (desktop, muted):** the October 26 perspective choice, offline-first intake, voice/fire changes and pop-up print now compile in Unity 6000.3.25f1. `StoryTable.unity` was regenerated. Results: EditMode 36/36; table PlayMode 21/21; whole-project PlayMode 70 passed, 0 failed, 6 opt-in skips. The three pending chapters were recorded with the existing voice settings, and .NET reports 358/358. Details and capture notes are in the [judged-path audit](reviews/2026-10-26-judged-path-audit.md#unity-verification-october-30). Nobody has listened to the recordings, and nothing has run on a simulator or headset.

| Decision | Status and reason |
|---|---|
| The table becomes a **pop-up story theatre**: painted scenery and figures stand up from a turntable on the table, perform each chapter to its narration, and turn to show the other side when the visitor chooses a perspective | User-approved direction on October 30, to be built as a Week 1 vertical slice and judged at a **November 5 go/no-go** on captures. The book layout committed here is the fallback |
| Heirlooms (a bundle, a gourd, a water pot) replace the interest buttons as the way to begin | Part of the slice; the typed question stays optional |
| Online generation stays off the judged path unless the user decides to host it | Recommended; user decision open |
| Uncommitted gallery-layout and font changes from `main` were stashed, not discarded | A local stash named "main WIP Oct 30: LivingGallery layout fixes…" on the user's machine; user decision open |

## October 30, 2026 — pop-up stage vertical slice

**Implemented and checked on the desktop, muted.** This is the Week 1 slice for the November 5 go/no-go. In `StoryTable`, a pop-up paper theatre on the table replaces the upright book's flat print:

- **Stage.** It sits on a low drum and turntable. Its painted scenery stands up from flat, back layers first: a dusk back cloth with stars, hills, a riverbank and reed wings. The two painted figures stand on their own hinges, and a small gourd travels between them.
- **Staging data.** `Content/personal-table-staging.json` says who stands where and in which painted pose at each fraction of a chapter's narration. It covers all twelve prepared chapters and has no effect on story text, choices or state. `Core/Personal/TableStaging` parses and validates it; .NET tests check that every prepared chapter is staged, every perspective maps to a different figure, and the chosen figure stands nearest after the turn.
- **Choosing a side.** Each perspective shows as a plaque above the figure it follows. Choosing one turns the stage: the scenery folds, the turntable spins and the scene rises mirrored, with the chosen woman in front. The lectern-free page keeps the same choices as buttons.
- **Heirlooms.** A bundle, a gourd and a water pot replace the interest buttons. Without a story service, touching one begins its telling; the chosen heirloom arcs into the fire.
- **Narration and captions.** While the voice plays, it drives the staging clock, and the captions turn to the part being spoken.
- **Pauses and options.** While an answer, source or notice covers the stage, the scene lies down and waits. Reduced motion shows every arrangement at once, and pause holds everything.
- **Layout.** The words and every action sit on one open page in front of the stage. The fire moves to the front left, larger. Lens checks and review captures now centre on a seated tabletop gaze, 25 degrees down instead of 10.

| Check | Result |
|---|---|
| EditMode | 36/36. `RecordingsAndNarrationKeepTheirDuration` failed after recording: `place-water.traveller` is 60.3 s against a flat 60 s cap. The test now bounds each table chapter by its own word count, the same bound `scripts/generate_table_narration.py` applies (between 15 and 75 s), and keeps 60 s for other narration |
| Table PlayMode | 24/24, including `PopUpStageRisesTurnsToTheChosenSideAndHoldsStillWhenAsked` and `CaptionsKeepPaceWithTheVoiceAndNeverTurnBackOnTheReader`. Two app tests are renamed or reordered because an heirloom now begins a telling with one touch |
| Whole-project PlayMode after the panel fix | 72 passed, 0 failed, 7 opt-in skips (capture studies, the live HTTP test and the new motion study) |
| .NET | 368/368, including 10 staging checks |
| Captures | `artifacts/story-table/11`–`15` (local): intake with heirlooms, first page, choice with figure plaques, the turned other side, and the ending. `CapturePopUpStageMotion` writes a fixed 12 fps study, encoded locally as `stage-motion.mp4` (about 17 s) |
| Shared `PokePanel` fix | The motion study showed a label lettered metres tall over the stage, and the book layout had the same blank button. A button made while its panel is set aside has its label sizes reset by TextMeshPro on first activation, so it auto-sized at 18 units. `PokePanel` now applies the label sizes again the first time each label is shown. Regression test: `LabelsMadeWhileTheirPanelIsAsideStayInsideTheirButtons`. Comparison-scene suites were rerun because the panel is shared |

**Limits.** No simulator or headset run. Rendering cost has not been measured, though the stage adds only alpha-tested layers already used elsewhere. Nobody has listened to the narration. The pose and position timings are estimates against recorded clip lengths, not reviewed performance.

## October 30, 2026 — go decision on the pop-up stage

The user reviewed the before/after captures and the motion study and chose **go** on October 30, ahead of the planned November 5 gate. The pop-up stage is now the product; the upright-book layout at `47b8cee` remains the recorded fallback. Week 2 starts early with these items, in order: fewer controls and shorter labels on the page, a river ambience under the stage, a gourd hand-off the visitor can make, a rendering budget check, then the decision on a life-size moment.

## October 30, 2026 — Week 2: a quieter page, the stage's sound, the gourd in the visitor's hand

**Implemented and checked on the desktop, muted.**

| Change | Requirement and criterion | Evidence and limits |
|---|---|---|
| The always-visible row is Pause, Options and Leave; Recenter and Fit my table move into Options | Experience (first minutes); hands-first controls stay reachable | Table tests assert the three controls and that Options holds recenter, table fit, stand height, voice, motion and text |
| The page's truth label reads **Imagined scene** or **Documented context • sources available**. The ancestry statement now appears once, in full, in How it works ("not an account of anyone's ancestors") | Truthful labelling kept; repeated disclaimers removed from every page | The narration's own "Imagined scene:" opening is unchanged; changing it would need new recordings and a validator change, and remains a user decision |
| While a telling stands, the stage plays a brook (celldroid, CC BY 4.0) and night insects recorded in the Maasai Mara (selcukartut, CC0). They rise with the scenery, fall to half while the griot speaks, and pause with the story. Headset only, like the fire | Polish (sound design) | How it works credits every attributed recording, and a test checks it. Nobody has listened to the mix; the volumes are starting values |
| **Set the gourd down.** In the gourd's own telling, shortly before the words set the gourd between the two women, a plaque invites the visitor to do it, and the gourd glows. Touching or pinching it moves the gourd; otherwise the narration does, as written. Offered on both sides of the choice and nowhere else | Hands-first participation (Entertainment track); character autonomy kept, since the visitor's act matches what the women decide | Staging contract: `invite` is valid only on a gourd beat that sets it between them; .NET tests check the shipped staging. A PlayMode test presses the plaque and checks that the narration's own moment does not move the gourd again |
| Rendering budget at the busiest moment: 72 renderers (18 of them text), about 5,900 triangles, 23 materials; blended surfaces are only the three flame cards and the two system hands | Technical (Quest performance) | New PlayMode check: at most 160 renderers, 100,000 triangles and 8 blended surfaces; no shadow casters or shadowed lights. It found and fixed blended sky and earth materials (now depth-written), button faces casting shadows (fixed in `PokePanel`) and the hands casting shadows (off in this scene). These are static counts, not device frame time |

**Verification:** whole-project PlayMode 73 passed, 0 failed, 7 opt-in skips; EditMode 36/36; .NET 371/371; table PlayMode 25/25 with captures. No simulator, headset or listening review.

## November 10, 2026 — the griot's welcome, pages that turn themselves, and a lower page

First slice of the [winning rework](reviews/2026-11-10-winning-rework-audit.md), on `feature/griot-visit`. The entrant chose Sankofa as their entry and asked for the rework; the remaining decisions in that audit (voice, new recordings, saying the disclosure once) are still open.

| Change | Requirement | Criterion | Evidence |
|---|---|---|---|
| **A spoken welcome.** On the headset the intake opens with "Come. Sit with me.", a line saying the griot keeps stories from many fires, and one disclosure that the people are imagined and documented pages are marked. A visitor with a kept telling hears "You came back. Sit with me." The heirlooms can be chosen throughout | SK-01 | Experience, first five minutes | New PlayMode test: every line shown once in order; choosing mid-welcome begins the telling |
| **Pages turn themselves.** A page turns shortly after its recording ends, or after the stage has performed it when there is no voice. An unanswered choice waits fourteen seconds, then the fire chooses, recorded as the fire's choice. Pause holds the page. Hand turning remains | SK-08, SK-15 | Experience | New PlayMode test: a visitor who only watches reaches the ending with every caption part shown; nothing saves or closes by itself |
| **A lower page and a fuller fire.** The page loses its title rows and leans with its far edge just over the stage rim, so the scenery and both figures are seen whole. The fire is larger and stands beside the stage, inside a 60° forward view | SK-07 | Polish, field of view | November 10 captures under `artifacts/story-table`; existing lens-fit tests pass unchanged |

The welcome lines after the first have no recording yet, so they are captions only until the entrant approves new recordings. The per-page spoken "Imagined scene:" is unchanged for the same reason. `SANKOFA_PRESENT=1` turns the headset presentation on in the editor, for recording in the simulator.

Verified in batch mode, muted: 27 of 27 story-table PlayMode checks, 36 of 36 EditMode and 371 of 371 core checks. An earlier attempt placed the controls and questions at the table's sides; the 60° lens test failed it, and they returned to the page. A flatter page slid under the stage and hid its caption; the capture caught it. No sound was played and nothing ran in the simulator or on a headset.

## November 10, 2026 — the griot's recorded welcome, the label said once, and the kept strand

Second slice of the [winning rework](reviews/2026-11-10-winning-rework-audit.md). The entrant approved new recordings and removing the spoken "Imagined scene:" from every page.

| Change | Requirement | Criterion | Evidence |
|---|---|---|---|
| **The disclosure is said once and shown always.** A story's stored text still opens with "Imagined scene: " and validation still requires it. The caption and the recording use the words without it. The page's label marks every imagined page, and the welcome says the people are imagined | SK-05, SK-06 | Experience, Polish | Core test ties each recording to the words spoken; a PlayMode test checks the caption omits the label and the page shows it |
| **Recorded welcome and return lines.** Eleven lines and nine re-read chapters, same model, voice and direction as before. Clips recorded with the label are retained for tellings kept earlier | SK-01, SK-11 | Polish (sound) | 20 clips generated and hash-checked; lengths within the script's bounds. **Nobody has listened to them** |
| **A thinner page on the headset.** With self-turning pages there are no page-turning buttons, the choice stands only at the figures, and an heirloom is the only way to begin | SK-02, SK-08 | Experience, field of view | PlayMode test and the `16-self-turning-choice-72` capture |
| **A hand held to the fire makes it swell.** | SK-01 | First five minutes | Compiles and runs; no test and no device evidence |
| **The kept strand.** Keeping a prepared telling threads a bead for the side followed, whoever chose it. Six beads sit on the stage rim beside the fire, coloured by heirloom. A returning visitor is told which heirloom leads to a side they have not heard. Forgetting removes the telling and the strand | SK-12 | Innovation (repeat use) | PlayMode test: keep, persisted strand, return greeting, no second bead for the same side, forget |

The strand and answer types and the `/ask` service came from the cloud session's pull request, merged into this branch after its checks passed here. The pull request against `main` stays open for the entrant's review. Nothing in the scene calls `/ask` yet, the build has no microphone permission, and no service is hosted.

Two choices here are the entrant's to reverse: a side the fire chose earns a bead once kept, and Forget clears the strand as well as the telling.

Verified in batch mode, muted: 27 story-table PlayMode checks with one capture-only case skipped, 36 EditMode, 416 core and 66 service checks. One EditMode check failed first: it bounded clip length by current chapters only, and a retained 60-second clip fell outside; it now covers retained clips and lines. No sound was played and nothing ran in the simulator or on a headset.

## November 10, 2026 — asking the fire aloud, behind a switch

The entrant approved adding the microphone and the spoken question path. It is built and off: the scene has no service endpoint and the switch is unset, so the shipped behaviour is unchanged until the entrant hosts a service and turns it on.

| Change | Requirement | Criterion | Evidence |
|---|---|---|---|
| **Ask the fire.** With the switch, a service and a microphone, the page's question button becomes Ask the fire. The visitor agrees once in a visit, the fire listens for up to twelve seconds, and the recording and the pages reached go to `/ask`. A generated answer shows with its label and is voiced when the service offers it | SK-09, SK-16 | Innovation, Experience | PlayMode tests with a fake microphone; an opt-in test posts Unity's own recorded request to the real service process on this machine, with no provider, and reads its reply |
| **The page's own questions are always the fallback.** No service, no microphone, a refusal, a silent or too-short recording, a failed or late request, and any reply the validator rejects all show the authored questions. Cancelling, pausing or leaving discards the recording unsent | SK-13/14 | Experience, Technical | PlayMode tests for each of these |
| **A spoken question waits for its page.** The service receives the current page in full, so a question asked while it is still being told could be answered with what it has not said. The fire offers to listen when the page is told | SK-09 | Experience | PlayMode test |
| **One prompt.** A refused microphone permission is not asked for again in the visit. Online creation has its own switch, so a question-only service adds nothing to the intake | SK-02, SK-16 | Experience | Permission flow is Android-only and unexercised; the creation switch is covered by the opt-in test |

Limits worth knowing. Returning from an answer tells the page again from its beginning; it does not resume mid-sentence. Answers are not saved with a kept telling. The recorder, the permission prompt, the answer's voice and every model reply are untried on a headset or against a real provider. A service test that failed about one run in three was traced to the service refusing an oversized body unread; it now drains a slightly oversized body first, and the suite passed fifteen runs in a row.

Verified in batch mode, muted: 31 story-table PlayMode checks with one capture-only case skipped, 36 EditMode, 416 core and 66 service checks. The PRD and MVP scope still describe voice as deferred and need a scoped update once the entrant decides whether it ships enabled.

## November 14, 2026 — both sides in one sitting, a hand that turns the stage, and the visitor's own table

The entrant asked a cloud session to audit the build against the competition's official pages and then to make the code-side fixes. The microphone and model answers stay a stretch goal behind their switches; the judged path is the prepared tellings, hands and the room. That session has no Unity editor. Everything below that lives in Unity is type-checked against Unity reference assemblies and covered by new tests, and **none of it has been run**: not the scene, not the new PlayMode or EditMode tests, not the simulator, not a headset. The [Unity verification list](testing/2026-11-14-unity-verification.md) says what to run, look at and listen to first.

| Change | Requirement | Criterion | Evidence |
|---|---|---|---|
| **Both sides in one sitting.** Each telling is one meeting seen by two women, and until now a visitor heard one of them per visit. Once a side is told, its page offers the other at her figure. Left alone for ten seconds, the fire turns the stage itself, recorded as the fire's choice. **End here** ends after one side. The opening is not told again | SK-04, SK-08 | Innovation, Experience (a complete moment in a short sitting) | 5 core tests of the session; 3 new and 2 revised PlayMode tests, not yet run. By the recordings' lengths a watching visitor's sitting is three and a half to four minutes, welcome included |
| **A hand turns the stage.** While the other side is offered, a palm swept sideways in front of the stage turns it. The stage leans with the hand, then folds, turns the way the hand went and rises on the other woman. Reaching, lifting, drifting and holding still do nothing | SK-02, SK-08 | Technical (hand tracking), Experience (hands first) | 5 core tests of the gesture; a PlayMode test with a simulated hand, not yet run. No tracked hand has tried it |
| **A hand on an heirloom begins its telling.** Closing a hand on the bundle, gourd or water pot does what touching its card does, and the heirloom rises a little to meet the hand | SK-02 | First five minutes | PlayMode test with a simulated hand, not yet run |
| **Every bead heard is kept.** Keep threads a bead for each side heard in the sitting, and the new beads swell once onto the strand. The ending says how many beads keeping will thread | SK-12 | Innovation (repeat use) | PlayMode tests, not yet run |
| **The griot points to what is unheard.** Back at the fire after an ending, the griot names a side the strand has not threaded and the heirloom that leads to it, in the return welcome's existing words and recordings. Nothing is said when nothing, or everything, has been kept | SK-01, SK-12 | Innovation (repeat use) | PlayMode check added to the strand test, not yet run |
| **The story begins in its own dark room.** Passthrough no longer turns on at launch, where the room changed nothing. **Use my table** is on the first page and in Options | SK-15, SK-17 | Experience (purposeful passthrough) | PlayMode test of the first page, not yet run |
| **A table from the room scan, seated where the visitor sits.** The scan's table has no front, so the story now faces away from the edge the visitor is beyond, sits at that edge in front of them, and stays wholly on the top. A long table counts when its nearest edge is within reach. Before, a visitor at the table's end got the story sideways, and one at the end of a long table got it in the far middle | SK-17 | Technical (scene understanding in rooms never tested) | EditMode tests, not yet run; the same checks pass in a numeric replica of Unity's rotation maths written for this session |
| **A table from a resting hand.** With no scan, no permission or no table found, the visitor rests a hand on their table. The story takes that height, stays straight ahead, and begins just beyond the hand. A hand in the lap, in the air or still moving sets nothing; after 25 seconds the stand returns | SK-02, SK-17 | Experience, Technical | 4 core tests of the resting hand; a PlayMode test, not yet run. No tracked hand on a real surface has tried it |
| **Nothing is asked at launch.** Unity's own permission dialog is switched off in the manifest; the room scan is asked for when a visitor chooses their table, the microphone when they agree to ask aloud | SK-02, SK-16 | First five minutes | Core and EditMode tests read the manifest. Whether a dialog appeared before is not known: no headset has launched the app |
| **No microphone in the judged build.** Asking the fire aloud stays in the code behind its switch. Unity adds `RECORD_AUDIO`, `MODIFY_AUDIO_SETTINGS` and `BLUETOOTH` to any build whose scripts name the microphone, so the manifest removes all three, as the October build did for the first. Deleting three lines restores them | SK-02, SK-16 | Polish; the entry says no microphone is needed | Core test reads the manifest. The merged APK's permissions must be read from the next build |
| **The fire no longer covers the room.** The flame cards added to alpha, which would have drawn a black box around each flame once the room shows behind the story | SK-17 | Polish | Read from the shader source; a core test holds it. Never seen on a headset, before or after |
| **Documented pages told plainly.** The gourd telling's documented page is now the trade passage (C6) its meeting belongs to; the museum's gourd (C7) is a question a visitor may ask. The water telling says the naming in one sentence, with the full passage as its question. First-page questions ask about the scene | SK-05, SK-09 | Experience | Core and service tests; the service accepts the shorter approved sentence only under its own source |
| **One loudness.** The 22 current recordings were 5.8 dB apart and are now within 1.5 dB, by MP3 gain steps that change no audio data and undo exactly | SK-06 | Polish (sound) | Measured with ffmpeg; **nobody has listened** |
| **Questions wait for their page.** Where pages turn themselves, a page's questions and **Explore context** appear once it has been told, the fire turns to the visitor, and the page waits five seconds before turning. Coming back from an answer or a notice no longer tells the page again. The first-page questions added by the editorial pass name details the page has not reached when it opens; shown from the start they broke the reveal rule | SK-09 | Experience | PlayMode test, not yet run. Hand-turned pages are unchanged |
| **An ending that always fits.** The ending is measured like any page, so at the larger text size it continues on a second part instead of being clipped | SK-15 | Polish | PlayMode test at both sizes, not yet run |

Choices the entrant can reverse: the fire turning the stage by itself (set `OtherSideWaitSeconds` long to wait for the visitor); the room staying off until asked; the room being shown at 70 per cent (`RoomOpacity`), which is a guess at dusk that nobody has seen.

What this does not establish. The sweep's distances and timings come from reasoning about a seated reach, not from a hand. The room scan's axis conventions are taken from the code already here and from memory of the SDK, whose source this session could not reach; a table that fails the level check simply falls back to the resting hand. Passthrough compositing, the permission prompts and frame rate are device questions. The new documented narration for the water telling is a lossless cut of the existing recording at 9.07 seconds, chosen by syllable count and not by ear.

Verified in this session: 437 core checks, 67 service checks and the 4 levelling-script checks pass; the Unity runtime in device and editor configurations, the PlayMode tests and the two story-table EditMode files compile against reference assemblies with stand-ins for the Meta SDK. The comparison scenes were not regenerated and the story table's scene file is unchanged: nothing here adds a serialized field.
