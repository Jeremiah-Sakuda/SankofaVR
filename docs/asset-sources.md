# Candidate open asset sources

October 8, 2026 · Research only. **Nothing listed here is approved or downloaded.** Each asset needs the user's approval to download, a licence check on its own page, and an entry in the [asset register](asset-register.md) before it ships. Cultural objects and dress still need family and community review, whatever their licence.

Our current art direction is semi-realistic: CC0 scans for materials, a filmic grade, and faceless ember figures. Low-poly stylised packs would clash with it unless used only for silhouettes or distant shapes.

| Source | Licence | What it offers us | Fit and caveats |
|---|---|---|---|
| [Poly Haven](https://polyhaven.com/models) | CC0 | Photoscanned props (crates, barrels, lanterns, pots, tools), rocks, plants and HDRIs; the textures we already use | Best match for our look. Good for the 1899 camp's stores and the today stall. Check each model's polygon count for Quest |
| [ambientCG](https://ambientcg.com) | CC0 | Scanned materials: canvas, rusted metal, fabric, leather, soil | For the hide, the tent canvas, the *jiko*'s metal and the rails. Matches how we already texture |
| [Smithsonian Open Access 3D](https://3d.si.edu) | CC0 where marked | Scans of museum objects, including some African collections | Possible source of real gourds, calabashes or tools. Each object's CC0 status and cultural context must be checked; a museum scan is not family approval. Our tools could not reach si.edu automatically, so search there by hand |
| [Sketchfab, cultural heritage CC0](https://sketchfab.com/blogs/community/sketchfab-launches-public-domain-dedication-for-3d-cultural-heritage) | CC0 for institutions' heritage scans; other models have mixed licences | Heritage scans from museums | Check every model's licence; many Sketchfab models are CC BY or non-commercial |
| [Quaternius](https://quaternius.com/packs/universalanimationlibrary.html) | CC0 | A universal humanoid rig with 120+ animations ([Universal Animation Library](https://quaternius.com/packs/universalanimationlibrary.html)): locomotion, sitting, emotes; nature and prop packs | Could drive the ember figures with real walk cycles through our faceless shader. Kneel and give clips are not listed, and the free tier has fewer clips than the paid one. The low-poly packs clash with our look |
| [Kenney](https://kenney.nl/assets) | CC0 | Large stylised game-asset library, including some audio | Too stylised for the scenes; possibly useful for UI sounds |
| [Mixamo](https://www.mixamo.com) | Adobe terms: free, royalty-free in projects, not Creative Commons; needs an Adobe account | Rigged characters and animations, including kneeling, picking up and handing over | Good for the actions our story needs. It needs the user's own Adobe sign-in, and the raw files cannot be redistributed. Use only the motion through our faceless shader, never a character's likeness |
| [Unity Asset Store (free)](https://support.unity.com/hc/en-us/articles/205623589-Can-I-use-assets-from-the-Asset-Store-in-my-commercial-game) | Standard Asset Store EULA, unless an asset is marked "Restricted" or "Non-standard" | Environment kits, props, vegetation, skies | Usable in a commercial app under the standard EULA; assets cannot be redistributed outside the built app. Check each package's licence file. Many are for URP or HDRP rather than our built-in pipeline |

## Suggested order

1. **Materials.** Draw canvas, leather, rusted steel and fabric from ambientCG and Poly Haven, applied to our existing generated shapes. This is cheap and consistent with the current look.
2. **Props.** Use Poly Haven props for the 1899 camp and today's stall, after a polygon-count check.
3. **Motion.** Decide between procedural motion and a CC0 or Mixamo rig after the figure and animation review.
4. **Cultural objects.** Find Smithsonian scans of gourds and similar objects, only with family review.

Sources consulted: [game asset guides (2026)](https://app.cinevva.com/guides/free-3d-model-sites); the [Smithsonian Open Access announcement](https://hyperallergic.com/1700-3d-models-of-cultural-heritage-objects-now-available-in-public-domain); [Unity support on Asset Store licences](https://support.unity.com/hc/en-us/articles/205623589-Can-I-use-assets-from-the-Asset-Store-in-my-commercial-game); [Quaternius UAL coverage](https://80.lv/articles/get-your-hands-on-over-120-ready-to-use-game-animations).
