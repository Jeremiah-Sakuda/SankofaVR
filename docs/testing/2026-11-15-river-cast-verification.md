# River cast comparison verification — November 15, 2026

Scope: the user-requested [painted and fully 3D cast comparison](../product/river-cast-comparison.md). Two new scenes share the saved cinematic prototype's story, opening, fire, vessels and controls, plus new foreground scenery. No existing comparison scene is regenerated. This record is software and desktop-render evidence, not headset or participant acceptance.

## Environment and results

Windows workstation, Unity 6000.3.25f1, D3D11 batch rendering with `-noaudio`, .NET 10. No Quest connected. Competition-context preflight passed before implementation. Logs remain local in `artifacts/logs/river-cohesion/`.

| Check | Result |
|---|---|
| Engine-free and repository contracts | `core.trx`: 445 passed, zero failed, including custom-shader stereo contracts |
| Unity EditMode | `editmode.xml`: 49 passed, zero failed |
| Focused cast PlayMode | `playmode-accepted.xml`: both scene integration/capture tests passed, zero failed |
| Workspace and whitespace | `workspace-final.txt`: 2,069 tracked files checked; local document links and exclusions pass. Both working-tree and staged whitespace checks pass |

The checks exercise real local app startup and water selection, original chapter staging, hidden cast during the documented context beat, the reveal after its authored boundary, paused presentation and rig poses, reduced motion, table/river return with the same realised story, and an unchanged tracked viewpoint. Scene checks require actual skinned geometry and bones for the 3D version, all three portrait textures for the painted version, shared physical foreground geometry, no historical gameplay scripts on the imported rigs, no LocalOnly dependency, and no new adapter on the preserved baselines.

## Problems found during review

The first scripted test advanced before the deferred water-choice event had run; the test now yields through the real application event before ticking the transition. Visual inspection rejected a hard foreground seam and a noisy dither blend; the bank now uses a limited alpha blend into the distant painting. These tradeoffs add foreground blending cost that remains unmeasured on device.

The imported historical rigs initially retained StageObject and FigureMotion: Unity refused to remove required components before their dependants. Their old behaviour hid and repositioned the new cast. The builder now removes components in reverse dependency order and throws if any historical MonoBehaviour survives; the saved-scene test enforces that boundary. Legacy carried props and colliders are removed. Presentation explicitly owns visibility and placement, and retains the fully composed skeletal pose during pause. Pose stability is checked within 0.00001 m to allow floating-point round-off.

Earlier draft logs and media are iteration evidence, not the final comparison. The final results above identify the accepted software run.

## Matched desktop studies

The review camera uses the same 1.15 m eye height, 60° field of view, composition and ±0.65 m diagnostic side offsets for both treatments. The eight-second clips hold the same early water chapter; this is a pose/depth study with scripted inputs, not a recording of an uninterrupted interactive visit. The real tracked rig stays still. Videos are silent; no new listening acceptance is claimed.

Local artifacts under `artifacts/river-cohesion/`:

- `painted-front.png`, `painted-left.png`, `painted-right.png`.
- `3d-front.png`, `3d-left.png`, `3d-right.png`.
- `painted-comparison.mp4` and `3d-comparison.mp4`, 1600 × 900 at 24 fps.

The generated portraits retain finer facial expression and folded-cloth detail; the rigged alternative provides real changing volume from the sides, with generic faces and existing garment geometry. Inspected front and side renders show that the latter still looks substantially more generic than the opening. This is an implementation assessment, not a participant-study result. The entrant subsequently selected the illustrated treatment; the default river simulator launcher now opens it. The original cinematic scene, StoryTable and Hearth are preserved, along with the 3D comparison.

## Still open

There was no new manual XR Simulator visit, Quest run, device frame-time or memory measurement, real-hand reach check, stereo-comfort test, cultural review, listening review or newcomer study. Remaining 3D facial/garment art direction and the emotional value of either treatment must be assessed from the actual comparison. No claim of intro/story quality parity or competition readiness follows from passing tests.
