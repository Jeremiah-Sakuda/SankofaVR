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
