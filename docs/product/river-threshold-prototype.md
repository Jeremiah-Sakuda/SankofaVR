# Water pot to riverside — cinematic prototype

November 15, 2026 · User-authorised experiment · Separate from the StoryTable baseline

The question is whether **stories you step into** can become an observable experience: choosing an ordinary water pot opens a human-scale illustrated encounter around a visitor who remains seated. The introduction gives the griot a presence and the object a reason to matter before asking for a choice.

This prototype does not replace the [personal story table MVP](personal-story-table-mvp.md), alter the authored water story or establish device or participant acceptance. StoryTable, LivingGallery, IllustratedTheatre and Hearth remain comparison scenes. The affected [PRD](prd.md) requirements are SK-01 (welcome), SK-02 (hands), SK-05 (integrity), SK-07 (spatial presentation), SK-08 (participation), SK-15 (comfort) and SK-18 (release evidence). Judging relevance is principally Experience Design and Polish & Presentation; the selected object causing a spatial reveal also supports Innovation & Creativity. These are product interpretations, not new official rules.

## The proposed visit

| Time / action | What happens | What is being tested |
|---|---|---|
| Opening, 0–4 seconds | A painted fire emerges: “Come. Sit with me.” | An invitation before an interface |
| 4–12 seconds | “THE GRIOT — A keeper of stories. A voice between generations.” Supporting text identifies inspiration from West African griot traditions, and the fire as this app's storyteller | Clear narrator identity without presenting one tradition as the historical practice of every setting |
| 12–18 seconds | The fire dissolves into a close-up of hands holding a clay pot: “Some stories leave only a thing someone carried.” | A connection between the object and the coming choice |
| 18–27 seconds | The imagined-person / documented-context distinction is stated; its existing 8.8-second recording fits before the title | An inviting opening that communicates the truth boundary |
| 27–32 seconds | Sankofa's title and invitation | A closing beat before agency returns |
| Choose | Existing hand-reach and point-and-pinch heirloom controls appear, with an invitation to choose the water pot | Deliberate selection, never gaze or a timed automatic choice |
| Crossing, 6 seconds | The pot is isolated; an ink dissolve reveals full-scale scenery, river and illustrated people around the seat | Presence without tracked-camera travel, zooming the surrounding world, walking or standing |
| Riverside | The existing water story starts at its first chapter | Whether human-scale people, depth and water make the telling feel inhabited |
| At the table | The small stage returns with the same realised telling and chapter | A direct comparison and comfort exit, with no generation or lost progress |

The opening can be skipped. Pause holds the presentation clock and its audio. Cancelling a crossing invalidates its pending start. Reduced motion removes illustration drift and pot lift and holds the water animation still; reading and dissolve time remains. No camera dolly, head rotation, head-locked story panel or eye-tracking gate is introduced.

## Implementation boundary

`SankofaSceneBuilder.BuildRiverThresholdPrototype` copies the saved StoryTable scene to `RiverThresholdPrototype`, then creates only its own generated materials and curved scenic mesh. It does not invoke comparison-scene builders. Three original generated paintings complement the existing cast/scenery atlases. A curved painted river surrounds the forward view, with separate figures, foreground reeds and side/rear scenery at different depths. It is an illustrated environment, not a physically simulated river or a historical site scan.

The user approved the paintings' visual direction and requested supplemental effects and animation in the same style. The original images are preserved: application-clock shader motion adds local flame movement, changing firelight and small water ripples. Twelve moving ember cards sample warm brush strokes from the fire painting itself. These effects pause with the sequence and are suppressed by reduced motion. There is no generated music or video-model dependency.

`RiverThresholdSequence` owns the opening/crossing clock and cancellation. `RiverThresholdPrototype` presents those phases. `PersonalStoryApp` supplies an optional presentation hold and prepared-start notification; it still owns accepted content, narration start, questions, perspectives, cancellation, saving and replay. The default table has no prototype component and keeps its normal welcome.

The small stage remains alive while visually hidden at the river: its current chapter/pose progression drives the larger figures. No future story turn is added to the opening. Other table stories retain their existing presentation. Extra controls use the same `PokePanel` / `PointerSource` path; the underlying stand, options and story controls remain available. Large figures are illustrative: their labelled perspective alternatives remain on the reading page.

## Nearby-object continuity pass

The entrant identified a quality gap between the painted introduction and the story's procedural fire and plain vessels, and explicitly required three-dimensional nearby objects. The follow-up replaces those objects only in this prototype: rounded water-pot and gourd meshes have rolled lips, open mouths and inner walls; the small passing gourd uses the same mesh. The travelling parcel has modelled folds and a knot. A painted material atlas wraps the actual geometry, so its surface, silhouette and occlusion remain three-dimensional when the visitor moves. The generated object cutout studies were visual references, not installed as flat runtime props.

The hearth has solid irregular charred logs and coals. Five curved, animated flame ribbons occupy depth at different angles; they do not billboard toward the camera or sum into the previous white additive core. This is layered realtime fire, not a fluid simulation. The established narrator still supplies fire state, while the prototype's paused clock and reduced-motion setting drive the new flame animation. The shader retains stereo macros. One atlas and one flame study supply the new artwork, with [exact prompts and provenance](../../unity/Assets/Sankofa/Art/RiverThreshold/objects.sources.json).

This targets SK-01, SK-07, SK-08 and SK-15, principally the Polish & Presentation and Experience Design criteria. Observable checks are the intro-to-choice handoff, three desktop viewing angles, actual open-mouth geometry, preserved pot selection and gourd passing, and pause/reduced-motion behaviour. Detailed nearby meshes and alpha-blended flames trade some geometry and overdraw for visual continuity; no Quest performance acceptance is inferred. The cast and distant scenery retain their existing illustrated construction.

## Sound and provenance

The new explanatory title cards are captioned. Exact existing local welcome/disclosure recordings accompany only their matching lines. Water narration is unchanged. Fire and brook beds reuse [registered field recordings](../asset-register.md). A ceramic tap acknowledges the pot choice; a short water movement follows during the dissolve; a paper sound accompanies the illustration change. The new [CC0 foley records](../../unity/Assets/Sankofa/Art/Recorded/RiverThreshold/sources.json) identify original pages, authors, downloaded preview URLs, edits and hashes. These are illustrative studio sounds, not claims about historical objects. A complete newly voiced introduction and listening review remain open.

The entrant explicitly authorised provisional reuse of the original web intro music. The candidate is `frontend/public/audio/intro-music.mp3` at web repository commit `35fd1d6f9ce32163e9d233aca9453bb521e87d05`, SHA-256 `d1233286612363138ac5044768cc09505d42cb535b071ad09390ed097b5b7763`. Its metadata identifies an encoder, not a composer or licence. It is **unverified**, not claimed copyright-free or confirmed human-composed. No music was generated here.

The MP3 is ignored under `unity/Assets/LocalOnly/RiverThreshold/` and loaded only by an editor conditional. It is not referenced by the saved scene or placed in Resources. The distributable music slot remains empty. Local-audition approval does not establish submission rights: establish provenance or replace it with cleared human-composed music before release. The web repository's AGPL declaration is not treated as proof of third-party asset rights. No web source code or illustrations were imported.

The [original beat list](https://github.com/Jeremiah-Sakuda/Sankofa/blob/35fd1d6f9ce32163e9d233aca9453bb521e87d05/frontend/src/lib/griotBeats.ts) informed the opening's rhythm, rather than its ancestry claims. The griot explanation is an editorial simplification informed by [UNESCO's oral-tradition account](https://ich.unesco.org/en/oral-traditions-00053) and [the Metropolitan Museum's account of West African griots](https://www.metmuseum.org/pt/perspectives/sahel-sunjata-stories-songs). Cultural review remains open. The paintings' exact prompts and provenance are in [their source record](../../unity/Assets/Sankofa/Art/RiverThreshold/sources.json).

## Verification and decision after review

The subsequent [painted-versus-3D cast comparison](river-cast-comparison.md) preserves this scene and adds two matching variants with new character art and physical foreground scenery. It is an explicitly requested comparison, not a promotion of either treatment into the MVP.

The [November 15 verification record](../testing/2026-11-15-river-threshold-verification.md) records the passing software checks, desktop preview and remaining listening, cultural, participant and device questions. Build, play and capture commands are in the [Unity README](../../unity/README.md#cinematic-river-threshold-prototype).

Compare the same water opening in StoryTable and RiverThresholdPrototype with the same caption/narration setting. Ask a newcomer without supplying the intended answer:

- What caused the surroundings to change? Did choosing the pot feel meaningful?
- Did you feel beside the people and river, or still watching a large illustration?
- Who is the storyteller, and which parts are imagined?
- Could you pause, return to the table and continue comfortably from the same seat?
- Which moment would you describe to someone afterwards?

Promotion requires a stronger observed experience without losing hands-only use, caption legibility, comfort or performance. Desktop renders and simulator success cannot decide the headset questions. Submission claims remain based on the table MVP until the new presentation has evidence.
