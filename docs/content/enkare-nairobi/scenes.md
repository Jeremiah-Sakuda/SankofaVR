# Enkare Nairobi: scene scope

October 4, 2026 · Scoping draft; the three water scenes are built in software (`SankofaWaterScenes`), with desktop captures only · This turns the [stories](stories.md) into buildable scenes for the existing Unity toolkit and story system. Coordinates are metres relative to the seat: x is to the visitor's right, y is up, z is ahead. The seated eye is at (0, 1.15, 0).

## Rules for every scene

- **Stationary viewpoint.** The world changes around the visitor, never by moving the camera. Transitions are the fire curtain, or the reduced-motion dissolve.
- **Reach.** Every touch point and resting place is within 0.61 m horizontally and between 0.5 and 1.1 m high, on the right or ahead of the visitor. The controls sit on the left. The Edit-mode reach test enforces this for every touch point.
  - **So nothing is touched on the ground.** Water, rails and soil are brought up to waist height in a believable way: a rock lip, a stack of rails on sleepers, a raised bed.
- **One warm light per scene.** It is always the ember's continuation: the traders' fire, the camp lantern, the *jiko* coal. Moonlight or city glow provides the cool fill. Shadows are baked, as at the hearth.
- **People and animals as ember-and-smoke figures.**
  - **Why:** the griot "shows" the people of the story as shapes in its own light, rather than as realistic humans. This avoids uncanny human models, identifiable likenesses and costume errors, and is cheap on Quest.
  - **Spike needed:** half a day to a day.
  - **Fallback:** objects and sound only (footsteps, a bleating ewe, a voice described by the griot).
- **Sound.**
  - A recorded ambience bed for each era, with the narration ducking it, as at the hearth.
  - No AI-generated music. Music only from licensed recordings of real performers, decided later.
  - The griot's voice comes from where the fire stood, ahead of the visitor.
- **Budget, until device measurements replace it.** About 15 draw calls and 200k triangles for each scene. One transparent effect layer (smoke or the curtain) at a time. Sustained 72 fps target.

## Hearth (existing scene, changes only)

- **Choices.** The interest choices become era choices: **Before the railway** and **When the railway came**, plus **The same water, today** once a keepsake is kept.
- **Greeting.** A Gĩkũyũ and a Maa greeting, chosen by the family.
- **New flow features.**
  - **Hearth asides:** a single "No hurry" line after a long wait at the choice, never interrupting.
  - **A hand question at the hearth:** "Is this true?"
- **Keepsakes on the hearth stone.** The bead (Era 1), the seed bundle (Era 2) and the ember (today). Each needs a kept-visit display version, as `shard.kept` has now.
- **Fix outstanding:** the kept object rests slightly above the stone; the road scene is replaced, so its clarity issue lapses.

## `story.water.1850s`: Before the railway

**Look.** Late afternoon into dusk at the end of a dry season.
- **North and behind-left:** a forest edge rising upslope.
- **South and right:** an open plain of dry grass running to low hills on the horizon. Whether the Ngong Hills can be seen from the stream, and in which direction, is to be verified.
- **Ahead:** a small stream crossing in front of the visitor and spilling over a rock shelf on the right.

**Light.** The traders' fire between three stones is the warm light. The dusk sky gives a violet fill, with mist low over the plain.

| Element | Object ID | Position | Interaction and notes |
|---|---|---|---|
| Traders' fire (the ember's continuation) | `fire.1850s` | (−0.5, 0.08, 1.05) | Continues `ember`. The only light source; glow and sparks at a smaller scale than the hearth |
| Water spilling over the rock lip | `water.spill` | Touch point (0.32, 0.6, 0.42) | **Touch:** the hand enters the falling water. Ripple, splash sound, cool-tinted hand highlight. Needs the water shader |
| Stream bed | (static) | About 2–2.8 m ahead, crossing left to right, behind the traders | Animated water surface in a narrow channel whose banks hide its edges; stones and grass at the edges |
| Gourd of banana flour | `gourd.flour` | Home at Wambui's figure; offered to (0, 0.85, 0.42) | **Receive.** Turned gourd (lathe), stopper, carrying strap |
| Trading hide on a flat rock | `hide` | Touch point (0.26, 0.56, 0.5) | **Touch** (offer). The gourd is placed on it. Hide mesh draped over a scanned stone |
| Milk gourd | `gourd.milk` | Beside Naserian's figure, about (0.9, 0.5, 1.2) | Reveal and illuminate; a soot-dark interior visible at the mouth |
| The bead | `bead` | Offered from Naserian to (0, 0.85, 0.42) | **Receive.** Keepsake; continues as `bead.kept` at the hearth. Colour and material from family review |
| Wambui | `figure.wambui` | (−0.7, 0, 1.5) | Ember-and-smoke figure. Walks down the forest path on arrival, kneels at the hide for the trade (built: arrival walk, `move` to the hide, leaving walk) |
| Naserian and the ewe | `figure.naserian`, `figure.ewe` | (0.8, 0, 1.4) | Ember-and-smoke figures that walk in from the plain; Naserian kneels across the hide. The ewe bleats now and then (built); the tether rope is not |

**Sound.** Stream babble (close, right), dusk birds and insects, wind in the dry grass, a distant sheep bell. The fire's crackle, small and close. All built from field recordings (see the asset register); the bell and the sheep are European recordings, for family review.

**New technology.**
- Stream and spill water shader.
- Terrain where forest meets plain, with a stream channel.
- Ember figures.
- Flat-topped acacia variant on the plain side, and a dense canopy line for the forest.

**Review checks.** The gourd forms, the hide, the bead, the figure poses (kneeling, carrying), where the forest and plain lie, and the names.

## `story.water.1899`: When the railway came

**Look.** The same terrain and stream, with the vegetation trampled and cut back. A grey morning after a cold night.
- **Across the stream:** three or four bell tents, a flagless pole, smoke.
- **On the right bank:** rails stacked on timber sleepers, and a cart.
- **Further off:** the new line on its embankment.
- The forest edge is thinner.

**Light.** A paraffin lantern on a tent pole near the visitor is the warm light. Cool overcast fill.

| Element | Object ID | Position | Interaction and notes |
|---|---|---|---|
| Camp lantern (the ember's continuation) | `lantern.1899` | (−0.35, 0.95, 0.9) on a pole | Continues `ember`. A period hurricane lantern (lathe and frame) |
| Stack of rails on sleepers | `rails` | Touch point on the top rail end (0.3, 0.58, 0.45) | **Touch.** Cold steel: a cool highlight and a metallic ring sound. Built: four extruded flat-bottomed rails with mill scale and rust, and fishplates and bolts across the far end |
| Tents, stores, cart | (static) | 3–8 m across the stream | Procedural canvas tents, crates, a wooden cart |
| Njeri's gourd | `gourd.empty` | With Njeri's figure | Reveal; a nearly empty gourd |
| Naisula's gourd | `gourd.old` | With Naisula's figure | Reveal; an old, soot-dark gourd |
| Seed bundle | `seed` | Offered to (0, 0.85, 0.42) | **Receive.** Keepsake; a leaf-wrapped pinch of millet seed. Continues as `seed.kept` at the hearth |
| Njeri, Naisula | `figure.njeri`, `figure.naisula` | (−0.6, 0, 1.4), (0.75, 0, 1.4) | Ember-and-smoke figures. No animals |

**Sound.**
- Hammers ringing on iron at a distance; a work-gang's voices as a low murmur. The workers' languages are to be checked before any words are recorded.
- Canvas flapping and a camp kettle. The stream, quieter.
- Built from field recordings: hammering on metal spikes, canvas and the stream. There are no voices until the workers' languages are checked, and no kettle.
- No birdsong at first, then one bird later.
- No train whistle until it is confirmed that working trains were running at the depot that week. Paukwa describes hand-propelled trolleys at the railhead.

**New technology.** Canvas and timber materials, and a rail-steel material. Camp props come from the toolkit (lathe, extrusion, scan materials).

**Review checks.**
- How famine is shown: through absence only.
- The workers are depicted with dignity, not as a menace.
- The tents and lantern are period-plausible.
- Nothing suggests specific named officials.

## `story.water.today`: The same water, today

**Look.** Night on the bank of the Nairobi River in the city.
- **Across the river:** apartment blocks and towers with lit windows; a streetlight.
- **On the near bank:** a community garden reclaimed from dumping, with raised beds, a painted gate and a footpath.
- The water is honest: cleaner in the cleared stretch, with litter caught at the edges.

**Light.** The *jiko* coal at a small stall on the left is the warm light. Sodium-orange city glow and a cool sky.

| Element | Object ID | Position | Interaction and notes |
|---|---|---|---|
| *Jiko* (the ember's continuation) | `jiko` | (−0.4, 0.45, 0.85) | Continues `ember`. Built: a ceramic jiko with waisted galvanised cladding, a clay liner holding the coals, three pot rests, an open ash door, and two cobs of maize roasting on a grill |
| Garden gate | `gate` | Touch point (0.3, 0.8, 0.45) | **Touch** (bead path). The bead is placed on a nail on the gate |
| Raised bed | `soil` | Touch point (0.32, 0.56, 0.42) | **Touch** (seed path). The seed is placed in the soil, and a small sprout appears in the passive and accepted endings alike |
| Bead and seed (continuing the kept ones) | `bead.today`, `seed.today` | Arrive from the kept versions | Continue `bead.kept` and `seed.kept` |
| City across the river | (static) | 55–240 m | Built: blocks with their own tone, windows, balconies and lit shops; glass towers with aviation lights; the KICC tower and hall on the skyline, at a compressed distance; car lights on the far road and trees on both banks |

**Sound.** Traffic, *matatu* horns at a distance, voices and a radio from a stall; any radio music must be a licensed recording of real performers, or it is cut. The river under it all. Built: a recorded matatu stage in Nairobi, a far city and the river; there is no radio. The matatu recording has not yet been listened to for incidental music.

**Review checks.** The garden and its sign, how present-day Nairobi is portrayed (neither poverty spectacle nor brochure), and the *jiko*'s form.

## Content model and engine changes

- **Package.** Replace `placeholder.hearth-stories` with the `enkare-nairobi` package, with status `draft` until review.
  - **Routes:** `era.1850s`, `era.1899`, `today.from-1850s` (follows `era.1850s`) and `today.from-1899` (follows `era.1899`).
  - The two "today" routes share their beats except the recall lines and the bead or seed beat.
- **Claims in the package.** Each claim in the source register becomes a package claim, with its state and source URLs. Lines cite them. Medium-confidence claims stay out until confirmed. The validator already requires sources for documented claims.
- **New flow features.**
  - Hearth asides: idle lines triggered only when the flow is idle, once each.
  - Hearth-level hand questions.
  - An era choice label that reads naturally ("When shall we go?").
- **New presentation.**
  - Water shader and spill.
  - Ember-and-smoke figures.
  - Per-scene ambience beds.
  - The 1899 camp props, the *jiko* and the city skyline.
  - Kept displays for the bead and the seed.
- **Voice.** The griot's lines are generated with Gemini TTS (voice Kore, the original griot direction adapted for this world), bundled, and the captions are timed to the clips.

## Suggested build order

Order, roughly 6 weeks:

1. **Write the package for Era 1 and validate it.** Build the boundary terrain, the stream and the water shader. Spike the ember figures. Generate the griot voice for the hearth and Era 1. *First headset test.*
2. **Finish Era 1:** gourds, hide, bead, sound, keepsake display. Device profiling. Send the family the review questions.
3. **Build Era 2:** camp, rails, lantern, seed, sound. Incorporate the review.
4. **Build "today":** river, garden, city, *jiko*, and both continuation routes.
5. **Polish:** comfort, captions, accessibility, failure paths. Capture footage on the device.
6. **Freeze, then submit:** freeze the candidate, run an external install test, write the description and credits, submit.

## Environment dressing implemented October 11, 2026

`SankofaEnvironment` generates presentation assets independently of story rules. The hearth now has clustered low shrubs, fallen limbs, leaf litter and rock outcrops at the edge of its clearing. The two historical scenes share the watercourse and ecological ground mask: damp organic cover replaces the dry scan near the water and beneath woodland, while the railway camp keeps a trampled central area. Sedge-like blades follow the stream outside the exchange corridor. The camp adds slatted stores, a simple awning, canvas sacks and hooped wooden barrels; today adds planted timber beds on the far bank. These are generic authored reconstructions, not reviewed period objects, botanical species or a surveyed garden layout.

Dressing stays out of the visitor's seat, near hand targets and the central performer routes. Static pieces are combined by material, receive the existing vertex lighting bake, and move with the stage. Foliage cards retain real three-dimensional placement and depth writes; their rectangular bounds do not become opaque bake occluders. The sky's existing cloud deck has stronger broad variation. Sound, narration and story consequences are unchanged. Desktop captures and the geometry proxy do not establish Quest appearance, comfortable visibility or sustained frame rate.
