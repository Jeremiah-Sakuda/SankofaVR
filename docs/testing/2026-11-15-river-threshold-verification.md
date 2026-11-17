# River-threshold prototype verification — November 15, 2026

Windows laptop, Unity 6000.3.25f1 with Direct3D 11, base source `e48b1e8` plus local changes. The [prototype brief](../product/river-threshold-prototype.md) identifies the affected product requirements, presentation experiment and asset provenance. This is a separate user-authorised comparison scene; the saved StoryTable, LivingGallery, IllustratedTheatre and Hearth scenes were not changed or regenerated.

## Software evidence

| Check | Recorded result |
|---|---|
| Engine-free core and repository contracts | 445 passed, zero failed in `core-final.trx`; includes four presentation-state checks and recorded-foley provenance checks |
| Unity EditMode | 44 passed in `editmode-final.xml`; the final two-test `audio-boundary.xml` rechecks local-music exclusion and adds the disclosure-duration check. Across these reports: 45 distinct passing tests |
| Selected table app/view and prototype PlayMode | `playmode-v2.xml`: 50 passed, one opt-in local-service test skipped. Final prototype suite `presentation-final.xml`: seven passed; final timing/capture recheck `capture-final.xml`: one passed. Across the final relevant results: 52 distinct passing tests, one skipped |

The PlayMode results cover the actual water choice and prepared-story start, unchanged tracked viewpoint, focus pause, cancelled pending start, returning to the same chapter at the table, one-hand point-and-pinch controls, one-shot foley cues, paused visual clocks, reduced motion, and stand-height changes without lifting the riverbank. The timed capture uses the initialized app and its presentation clock. It does not seek a model-generated story or substitute a fixture for the opening.

The saved prototype scene has no `Assets/LocalOnly/` dependency, its release music field is empty, and the baseline StoryTable has no prototype component. The existing 8.8-second disclosure recording fits in the nine-second disclosure beat before the title. Audio timing and references are software checks, not listening acceptance.

These are focused regression results, not a new whole-project PlayMode run or live-provider test. The earlier [table verification record](2026-11-15-story-table-verification.md) retains its baseline failures and subsequent fixes. Test logs remain local under `artifacts/logs/river-prototype/`.

## Visual and sound preview

The final 50-second, 1600 × 900, 24 fps desktop movie follows the opening, water-pot choice, six-second crossing and start of the riverside story. Inspected captures show the painted fire and hands, readable opening titles, curved river painting, separate illustrated people and foreground scenery. The people appear with the first story page rather than disappearing briefly at the handoff. The river view keeps the story page and table-return controls nearby.

Local review artifacts:

- `artifacts/river-threshold/01-invitation.png` through `08-beside-the-river.png`.
- `artifacts/river-threshold/river-threshold-silent.mp4`: scripted Unity desktop render, not simulator or headset footage.
- `artifacts/river-threshold/river-threshold-preview.mp4`: that render with an editorial audio mix made by `scripts/render_river_preview.py`. It is not a recording of Unity's audio output.
- `artifacts/river-threshold/preview-mix.json`: input hashes, timing, mix recipe and evidence limitations.

The preview includes the provisionally authorised original music, existing fire/brook recordings, new CC0 ceramic/water effects, a registered paper effect and the two exact existing welcome/disclosure voice clips. New explanatory titles are captioned; a complete newly voiced introduction remains open. The original music's composer, human/AI origin and release rights remain unverified, so it is an ignored editor-only audition asset. No music was generated.

## Follow-up: nearby objects in three dimensions

The entrant identified the intro-to-story quality gap and clarified that nearby objects must remain three-dimensional. The prototype now uses textured rounded vessel meshes with open interiors, a folded cloth parcel, solid charred logs/coals and five curved flame layers placed at different angles. Generated pot/gourd/parcel cutouts were used only as visual studies. Original comparison scenes and their generated assets remain unchanged.

The first follow-up run passed all 46 EditMode tests and seven of eight prototype PlayMode tests. Its new gourd-interaction test tried to choose a figure before the chapter's reveal reached her; the fixture now waits for that visible state. The captures also exposed a real one-frame issue: the prototype forced every stage panel visible when leaving the intro, briefly showing **Set the gourd down** during intake. The application view now restores its own invitation and choice visibility. Close review cameras also use a suitable near clipping plane, and material wrapping mirrors pigment to remove a hard texture seam.

Final follow-up results under `artifacts/logs/river-objects/`:

- `core.trx`: 445 passed, zero failed, including shader stereo contracts.
- `editmode.xml`: 46 passed, zero failed. A geometry ray check confirms real depth and an open path through each vessel's mouth to its inner bottom; saved-scene music exclusion remains checked.
- `playmode-final.xml`: eight passed, zero failed/skipped. Includes water selection, deferred/cancelled start, one-hand alternatives, table/river return, stand placement, reduced-motion/paused flame clocks, hidden legacy flames, and the actual prepared trade story's accepted gourd pass.

Inspected `11-objects-front.png`, `12-objects-left.png` and `13-objects-right.png`, plus the refreshed riverside image and 50-second capture. These are desktop composition views with synthetic inputs, not headset footage. `09-before-object-pass.png` and `10-before-story-fire.png` retain the previous look locally. The refreshed preview keeps the same separately mixed audio and its existing limitations. The new materials improve local visual continuity; these checks do not establish full-scene visual acceptance, stereoscopic comfort or Quest performance.

## Acceptance still open

The user's approval establishes the paintings' preferred visual direction. It does not establish newcomer comprehension, cultural review, listening quality or emotional payoff. There was no new manual simulator visit, headset test, real-hand reach check, room-fit test or device-performance measurement in this pass. Quest comfort, caption legibility and whether the curved painting feels like being beside a river remain device questions. Review the same telling in the baseline and prototype before promoting this presentation into the MVP or submission claims.
