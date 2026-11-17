# River cast — painted and fully 3D comparison

November 15, 2026. User-requested comparison, within the [river-threshold experiment](river-threshold-prototype.md).

The question is whether the people and nearby environment sustain the introduction's painted, intimate atmosphere. Both scenes share the same opening, water choice, realised story, chapter staging, captions, hand controls, fire, nearby vessels, curved background painting, ground, water, stones and reeds. The visitor remains seated. Only the human-scale cast treatment differs. StoryTable and the earlier RiverThresholdPrototype remain preserved references.

**Decision after comparison: use the illustrated cast.** The entrant selected this direction on November 15. `PlayRiverThreshold` now opens `RiverThresholdPainted`; the 3D cast and earlier prototype remain explicit comparison menu entries. Nearby vessels, fire construction, ground, reeds, stones and water keep their physical depth. This selects the cast treatment for the river experiment; it does not silently replace the StoryTable MVP or establish device acceptance.

## What to compare

| Treatment | What is implemented | Principal tradeoff |
|---|---|---|
| `RiverThresholdPainted` | Two individually painted figures, three poses each, separate positions in depth, original stage reveal/pose cues and restrained pose transitions | Fine facial and cloth detail; each person remains a cutout when viewed from the side |
| `RiverThreshold3D` | Existing rigged CC0 bodies, hair and eyes; fitted skinned garments with new painted pigment; idle, blink and story-directed gestures | Actual volume from every side; existing generic face and garment geometry limits likeness and expression |

The small table uses the new paintings in both versions. The river uses physical ground, irregular stones, curved reed meshes and a shallow water ribbon, with warm umber/ochre surfaces and cool indigo shadows. These are illustrative scenery, not a site scan or a claim that every garment is historically verified. The source painting remains the distant scenic surround.

`RiverCastPerformance` reads the accepted `PopUpStage` cues. It does not choose chapters, generate text, infer intent or run the old Hearth story components. Both versions hide people during the authored context beat and restore them only at its reveal point. Application pause holds presentation time; reduced motion freezes idle, blink, reed and water movement. The comparison camera offsets used for desktop inspection never move the tracked rig in the application.

## Review protocol

Run **Sankofa > Compare River Cast > Painted figures in XR Simulator**, then **3D figures in XR Simulator**. Choose the water pot in each, use the same narration/caption settings and inspect the same chapter. Repeat with the order reversed for another viewer. The desktop review captures use the same 1.15 m eye height, 60° field of view and ±0.65 m diagnostic offsets; the latter test geometry and are not an authored camera move.

Assess whether each person feels present beside the river, whether the expression supports the uncertain remembered path, and whether the introduction and story feel like one work. Inspect side views, feet, clothing deformation, pose changes and text readability. Compare the emotional result before deciding whether added modelling and rigging effort is justified. No preference is inferred from software tests.

Affected requirements: [SK-07, SK-08, SK-15 and SK-18](prd.md#current-mvp-contract). Modules: Unity presentation and editor-generated assets; no story-core or provider contract change. Judging relevance: Experience Design and Polish & Presentation (our interpretation). Observable evidence: matched desktop renders, distinct real geometry, reveal/pause/return checks, then future simulator, headset and participant review kept separate.

## Assets and evidence

The [asset source record](../../unity/Assets/Sankofa/Art/RiverCohesion/sources.json) contains exact image-generation prompts and references. Six transparent character paintings and two material atlases are generated artwork. Bodies, hair and compatible animation reuse the repository's existing Quaternius assets under their recorded CC0 licences; no external web Sankofa assets or code are imported. The earlier local music audition remains unchanged and excluded from scene dependencies. No music is generated.

Use the [verification record](../testing/2026-11-15-river-cast-verification.md) for actual results and limits. Desktop captures establish inspectable implementation, not Quest comfort, performance, cultural acceptance, listening quality or competition readiness.
