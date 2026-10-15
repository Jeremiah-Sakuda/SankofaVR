# LivingGallery simulator verification — October 15, 2026

**Verdict: scripted flow verified in the actual Meta OpenXR Simulator; manual input acceptance remains open.** The user explicitly authorised this simulator session, superseding the earlier no-simulator restriction. No sound was played. No APK, scene, story package, runtime behaviour or artwork was changed by this verification.

## Environment and method

Source baseline `1947516`, gallery package v0.7.0, Unity 6000.3.25f1, Meta XR Core/Simulator 207. The installed simulator used its Meta Quest 3 profile. Unity launched with `-noaudio` and the new `SankofaSimulator.PlayLivingGallery` entry point. The runtime JSON, registered display/input providers and OpenXR session reaching `FOCUSED` are recorded in the local log. This is actual simulator execution, rather than the previous desktop batch-only captures.

The simulator frontend ran from a macOS App Translocation path. Attaching to `/Applications/MetaXRSimulator.app` stalled; identifying and attaching to the already-running translocated instance resolved access. Its system-wide runtime switch displayed “disabled”, but Unity's process-scoped runtime was active and rendering. A proposed system-wide runtime change was cancelled; no extra OS permission was granted. Webcam head/hand tracking remained off.

Manual attempts used hand mode, controller mode, point-and-click, compatibility mode and the documented trigger binding. **Visible plaque selection did not succeed in this setup.** This does not prove the device input adapter is faulty, but it prevents a claim of successful hands/controller acceptance. Simulator inputs/configuration, the pointer adapter and actual Quest interaction require a follow-up check.

An ignored editor-only probe then reloaded the gallery with an isolated temporary visit store and muted settings. It issued scripted application/panel commands while the real OpenXR simulator remained active. Story time ran at 12× for route coverage; settings checks used normal speed. Captures came from the Unity game view. The probe restored original comfort settings, stopped Play mode and was removed from `Assets` after completion. The normal visit store was not changed.

## Results

**29 assertions passed; zero probe failures.** These are scripted checks, not 29 manual gestures or participant trials:

- OpenXR display active, Meta simulator selected, current framed artwork waits for deliberate entry, and persistence is isolated.
- Default Wambui entry, visible provisions memory, accepted invitations and milk hospitality, completed return to the framed artwork, explicit Keep.
- Settings opens during an active story, pauses the flow, freezes its clock and resumes the pause it owns.
- Kept visit exposes replay/continuation; Naserian's focused return displays the remembered meeting and records passive/default outcomes truthfully.
- Replay completes using the retained original route; the modern river continuation completes and returns to the frame.
- Forget requires confirmation and removes the isolated saved visit. Fresh Naserian entry works; early return requires confirmation, and an unfinished visit cannot be kept.

The probe observed no Sankofa runtime errors/exceptions/asserts. The broader log contains Meta/OVR diagnostics, including unavailable optional extension functions, device enumeration without reachable ADB and simulator compositor initialization/shutdown messages. Zero probe failures does not mean the entire vendor log was error-free. No script compilation error was found.

After removing the probe and closing the session, all **31 Unity EditMode tests passed** in a separate muted batch run (zero failed/skipped; editor exit 0). The workspace/link/exclusion check and diff whitespace check also passed. No core/story change required repeating the preceding .NET suite.

## Visual findings and limits

1. **Confirmed layout defect:** the entry/perspective plaques obscure the lower artwork context text, including portions of the title-origin/setting placard, in the default simulated view. The text is available separately in About this artwork, but the wall presentation still needs repositioning and a projected overlap regression check.
2. **Input acceptance unresolved:** repeated manual point-and-click/trigger attempts did not select Enter painting. Scripted commands intentionally bypass that unresolved path. No physical hand-off was verified; the accepted path resolves invitations programmatically.
3. The saved-visit notice appears in a captured second-memory frame during the accelerated run. It expires using `Time.unscaledTime` after seven real seconds; advancing story time at 12× brings the memory forward before that timer expires. This is not evidence of a persistent normal-speed defect. Likewise, captures taken on the first frame of a new line can show the caption's intentional 0.18-second fade. Settled, normal-speed captures are needed for a fair text review.
4. Unity's docked game view crops some lower controls/settings at its current aspect ratio. This is not proof that the Quest eye view clips them. Check both eyes, head movement and settled captions with a representative field of view and actual hardware.

Editor/simulator frame-time telemetry varied substantially and includes initialization, screenshot/UI overhead and accelerated playback. It is not a Quest benchmark or proof of the competition frame-rate target. No listening/pronunciation, stereo comfort, physical reach, real tracking, novice comprehension, cultural or rights acceptance was performed.

## Local evidence

Raw artifacts remain ignored and are not shipped:

- `artifacts/logs/gallery-simulator-2026-10-15.log`: launch, OpenXR state, vendor diagnostics and probe completion.
- `artifacts/simulator-2026-10-15/report.json`: all 29 assertions and zero failures.
- `artifacts/simulator-2026-10-15/*.png`: gallery, settings, both memories, hospitality and completed returns.
- `artifacts/simulator-2026-10-15/probe-source/`: the removed editor-only probe for reproducibility.
- `artifacts/tests/simulator-launcher-editmode.xml`: 31 passing editor tests; corresponding log in `artifacts/logs/simulator-launcher-editmode.log`.

The tracked change adds a direct current-gallery simulator launcher and corrects instructions. It does not revise the previously built APK identity or close device/reviewer/release gates.
