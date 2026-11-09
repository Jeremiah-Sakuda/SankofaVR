# Sankofa candidate builds

## Active pop-up theatre review APK — November 8, 2026

Built the approved pop-up theatre at **90891cd**, the current working branch, with Unity 6000.3.25f1 in muted batch mode. This replaces the October 18 flat-table APK at the usual local path; the earlier file is preserved as `artifacts/release/archive/Sankofa-StoryTable-7fc1988-dirty.apk`. These are local review builds.

| Identity | Active local review artifact |
|---|---|
| Output / scene | `artifacts/Sankofa-StoryTable.apk` / `StoryTable.unity` |
| Source / package | `90891cd` / `com.sankofa.quest` |
| Android version | `0.1.0+90891cd`, version code 1, ARM64/IL2CPP |
| Signing | Signature verified; Android Debug certificate. Confirm release signing and next version code with the dashboard owner. |
| Size | 65,710,082 bytes (62.67 MiB) |
| SHA-256 | `92b0b6eade58d5204dadf6797ec7653fd382b4db77623a98128d6cd85cc8f692` |
| Build result | Succeeded, zero errors, one optional OpenXR input-polling recommendation |
| Local evidence | `artifacts/logs/nov08-pop-up-build.log`; `artifacts/release/pop-up-theatre-90891cd-apk.json` |

The APK guard passed, and the archive-name scan found no private brief/reference/LocalOnly/credential paths. This is not a general audit of arbitrary data. ADB found no connected Quest. No install, sound playback, simulator capture, upload or submission occurred today.

**Evidence for this source:** November 8: 371/371 core and 32/32 service checks, plus text/audio hash matches for 12 current and six retained narration clips. October 30 logs for the pop-up/gourd changes: 36/36 editor, 25/25 focused table runtime and 73 whole-project runtime checks passed (seven opt-in skips). Desktop stills and motion studies establish software presentation only. Hosted generation is outside the proposed judged path; prepared tellings are the offline product.

Use the [headset and release handoff](access-handoff.md). Device install, hands-only completion, readability/reach, performance, room recovery, listening, cultural and rights review, dashboard access and valid final footage remain open.

---

## Previous flat story table — October 18–19, 2026

Local review APK built from source **7fc1988 with a dirty working tree**. Eight pre-existing gallery/font/animation modifications from another session were preserved and excluded from these commits; documentation edits were also pending. In particular the font files used by this build were modified. This is a review artifact, not a clean-source competition freeze. The earlier gallery builds below remain historical records.

| Identity | Active local artifact |
|---|---|
| Output | `artifacts/Sankofa-StoryTable.apk` |
| Scene | `Assets/Sankofa/Scenes/StoryTable.unity` |
| Android package/version | `com.sankofa.quest` / `0.1.0+7fc1988-dirty` / code 1 |
| Architecture | ARM64 / IL2CPP |
| Size | 65,108,788 bytes (62.09 MiB) |
| SHA-256 | `86abd891dcfd77e8a6035ab73e244137dc91ae2553bb27d63b560e6db293792a` |
| Result | Succeeded; zero errors, one optional OpenXR input-polling warning |
| Build record | `artifacts/release/story-table-apk.json`; `artifacts/logs/story-table-mvp-final-build.log` |

Verified APK permissions include INTERNET and USE_SCENE; Meta scene/passthrough support is optional. The development-credential sanitizer passed and editor settings were restored, including the original bundle version. Archive-name checks found no private brief/reference/LocalOnly/credential filename matches; filename checks are not a full data audit. No Google credential or live service endpoint/token is committed to the scene.

**Software evidence:** 349 core, 32 service, 36 editor and 16 unique table-runtime checks pass. The last combined runtime suite passed 16/16; after the final saved-replay change all nine app cases passed again. An opt-in authenticated actual Unity HTTP test passed two public-synthetic question-only generations and a real MP3 download/decode/cache with narration disabled. A separate three-case direct-service review assessed role fidelity after a prompt fix. None establishes general model, device or listening quality. Exact reports and review limits are in the [implementation record](../reviews/2026-10-18-personal-table-iteration.md).

The APK completes prepared tellings offline. Online creation on a headset needs a separately configured HTTPS deployment with limited access and judge availability. The temporary local test service was stopped and its token removed. Kept generated stories reopen their exact text/art/questions/sources locally and may remain caption-only; no remote speech is requested on replay.

**Open release gates:** clean-source freeze, independent installation and Competition-channel access; HTTPS service/access/retention if online creation is included; actual Quest hands, seated reach, room fitting/denial, stereo readability and sustained minimum 60 fps; cultural/source, rights and pronunciation/listening acceptance; unfamiliar-visitor relevance and return intent; eligibility/division and honest final footage. No Quest session, sound playback, channel upload or final entry submission occurred in this iteration.

---

# Preserved gallery candidate — v0.7 pavilion

Current review APK built from clean source **493c3aa** after the [pavilion immersion follow-up](../reviews/2026-10-15-gallery-environment.md), incorporating the [input/layout/placement follow-up](../reviews/2026-10-15-gallery-input-and-layout.md) and [production hardening](../reviews/2026-10-14-production-iteration.md). This supersedes source 5dbf149; that APK/identity and the older review builds are preserved locally under `artifacts/release/archive/`.

| Identity | Current local artifact |
|---|---|
| Output | `artifacts/Sankofa-LivingGallery.apk` |
| Android package/version | `com.sankofa.quest` / `0.1.0+493c3aa` / code 1 |
| Story package | v0.7.0 |
| Architecture | ARM64 / IL2CPP |
| Size | 84,883,472 bytes (80.95 MiB) |
| SHA-256 | `90a0c1f98a5c5ef9bacf2ab9c4fd4f6f86c5fcecdff5f74f6d74d90515c87f55` |
| Build | Succeeded, zero errors and one reported warning (optional OpenXR input-polling preference) |

**Current verification:** 306/306 core, 31/31 final editor and 9/9 final focused runtime checks pass after the smooth-fade replacement. Both story routes/Keep/modern return, captions, pause/reduced motion, true arch/skylight apertures, all five wall labels across 108 combinations, pointer cancellation, recovery and settings pass. The new transition check also verifies that original opaque materials and lettering restore on return. Earlier broader 46-case functional results remain historical evidence; that full suite was not repeated here.

Muted Meta XR Simulator 207 review displayed the pavilion, open star spine and physical plaque bodies. Native simulated-controller selection entered the painting; normal passive opening reached the river scene; an explicit return request and confirmation restored the opaque pavilion. The initial coarse cutout release was rejected during visual inspection and replaced with cached temporary blended materials, used only during transition. The [environment record](../reviews/2026-10-15-gallery-environment.md) retains failed/intermediate checks and their fixes. No sound was played. Real hands, binocular comfort and actual Quest GPU/performance acceptance remain open; editor/simulator frame rates do not establish them.

Integrity verifies 31 declared art/recording/texture/font inputs and 92 current speech fingerprints. Architecture and its material shader are project-authored, reusing the existing grain texture. The build guard verified captured local development credentials absent. Zero private-brief/reference/LocalOnly/credential/key filename matches is a narrower archive-name check, not a complete arbitrary-data audit. Editor-only settings/version were restored, and source remained clean after building.

Current pavilion stills are muted desktop renders. The existing **41-second, audio-free preview** is the October 14 edited desktop/synthetic capture, before the environment and startup/label fixes. It still demonstrates both memories and paired hospitality; it is not new footage of this APK, qualifying headset footage or real-hand evidence. Review packets remain offline drafts and not completed reviews.

No device installation, listening/pronunciation/cultural/participant acceptance, applicable asset/contributor contract confirmation, eligibility/account confirmation, Competition upload, independent judge installation or public submission was performed. Source/APK/simulator existence does not close those gates. Follow the [evidence protocol](../testing/gallery-next-evidence-session.md), [rights review](rights-review.md) and [access handoff](access-handoff.md).

Raw current build log: `artifacts/logs/pavilion-apk.log`; identity: `artifacts/release/gallery-apk.json`. The standard BuildMain / Build APK target is LivingGallery. Historical comparison builds are explicitly named.

---

# Earlier v0.6 completion-pass record

Package **0.6.0**, LivingGallery. This pass follows the remaining [judging findings](../reviews/2026-10-13-independent-gallery-panel.md). It does not claim new judge scores or a winning outcome.

**Subsequent audit:** the [current-experience audit](../reviews/2026-10-14-current-experience-audit.md) reproduced caption overlap through the actual selection-button carry, interaction-journal logging without Keep, and invitation expiry while Settings is open. The verification below remains a record of the tested paths; it is not acceptance of those uncovered paths. No production fixes were made in that audit.

## Product changes

The passive agreement now arrives at **250.85 seconds** in *What she brings* and **253.75 seconds** in *What she remembers*. Hospitality arrives at **267.61 / 270.51 seconds**. Full passive telling schedules are **432.03 / 434.93 seconds**, down from 499.04 / 501.94. These calculations include narration, pauses and default invitation waits, excluding transitions, questions, interruptions and visitor pauses. They are not observed session lengths.

The introductory light line and mandatory flour/hide/household explanations no longer delay the turn. Flour and household context remain optional recorded answers; the forest/plain sensory line follows hospitality. Spoken disclosure remains before story entry. The Maa name and the women’s independent agreement remain on every path. Later water/flour/placement waits are eight seconds after their spoken invitations; first ember remains fourteen. Shorter waits need novice and device validation and may be increased if necessary.

Native focal art gives the ewe a coherent fleece body, pigment head and cloven hooves, the bead a clearer pierced profile and bands, and the gate bevelled uneven wood, bracing and small fasteners. Candidate materials are isolated from the comparison scene. The ewe appears beside the women when named, with the inherited distant arrival path removed. The legacy bead glow card is removed so the pierced opening reads. Renderers and reach anchors are retained. These are fictional theatrical details pending cultural and device review.

Caption placement projects offered/carried objects into the current camera view and chooses a bounded clear band when possible. Placement holds for each line until obstruction changes; the board's head-following anchor remains intact. Text and backing move together. The fallback retains readable text when no tested candidate fits; it does not hide captions. Context questions now appear once, beside their subject when available or on the accessible central fallback. Two questions about the same object occupy separate rows. This protects focal objects, not every possible face or background detail. Stereo comfort, all caption sizes and real moving hands remain device gates.

The unreviewed street-recording layer is removed from this candidate. Existing recorded narration is reused without new provider requests or playback.

## Prepared evidence and deliverables

- `python3 scripts/make_gallery_review_packet.py`: current offline cultural and blind first-time packets, ZIPs, line/time/claim references, draft art and recorded read-throughs under ignored `artifacts/gallery-review`. Audio has explicit controls and no autoplay. Read-throughs omit interaction waits/transitions; they are not actual visits. Optional notes export locally; no responses are auto-saved or uploaded.
- `python3 scripts/audit_gallery_assets.py`: hash/fingerprint evidence under ignored `artifacts/release`. Integrity is distinct from rights acceptance; see [rights review](rights-review.md).
- `python3 scripts/quest_evidence.py`: read-only device inventory. With an explicit connected `--serial`, saves device/package/display/thermal observations and Unity logs locally. It does not install, launch, play audio or use a simulator. Pair it with the [human/device protocol](../testing/gallery-next-evidence-session.md); logs alone do not demonstrate comfort or sustained performance.
- Silent software preview captures use a batch runtime test with synthetic input. Its draft video must remain labelled desktop/synthetic and cannot replace the required final headset/emulator demonstration. No public video has been uploaded.
- [Judge access/video handoff](access-handoff.md): concrete upload identity, independent installation and final shot plan, without claiming access or publication.
- [Submission draft](submission-draft.md): product pitch, feature disclosure, adaptation lineage and judge instructions, with account/device/reviewer fields explicitly pending.

## External acceptance still open

No Quest was connected when queried. No unfamiliar participant or cultural/language reviewer has supplied new results. No listening review occurred, no Meta Start/app access was confirmed, no Competition upload was performed, and no public submission/video was published. The current no-playback/no-simulator restriction remains in effect. These gates require real hardware, reviewers or account access; software tests cannot substitute for them.

## Verification

Core verification: **303 .NET checks passed**, including passive agreement/hospitality timing for both full threads. Unity editor verification: **30/30 checks passed** after the final bindings/material changes. The final gallery runtime run passed **14/14** in one clean run, including long captions, unique/spaced questions with central fallback, both threads/continuations, save/replacement/replay and the silent synthetic-input preview. A separate shared-caption/fingertip check in the preserved Hearth scene also passed **1/1**. These are software checks, not real hands or participant evidence.

The asset integrity audit now covers **30 declared inputs** including the original font files/notices, and **86 current narration lines**, with no integrity problems. Offline packet layout and local export were checked at 375, 812 and 1280 px in a muted headless browser, with no autoplay, unlabelled fields, horizontal overflow or external requests. Synthetic QA notes are not participant findings. The source generator and recorded read-throughs use no new provider calls.

Early editor attempts exposed a shared comparison material and a wrong demo-test anchor API; both were corrected. The first runtime pass exposed test-anchor cleanup that disrupted subsequent cases; the anchor is released before destruction and later checks run cleanly. These failed reports remain local artifacts and are not counted as accepted checks.


## Review APK identity

Built from clean committed source **da85045**. Package `com.sankofa.quest`, version `0.1.0+da85045`, version code 1, ARM64/IL2CPP, story package v0.6.0. The local output is `artifacts/Sankofa-LivingGallery.apk`: **84,335,456 bytes (80.43 MiB)**, SHA-256 `f3d189ec76b67be60b82798dcb2af40be2ef58a0547341821d1672291d234b8e`. Build succeeded with **zero errors and one reported warning** (optional OpenXR input-polling latency preference). Log: `artifacts/logs/finish-build.log`; structured identity: `artifacts/release/gallery-apk.json`.

The build privacy guard verified captured local development credentials absent and restored editor-only settings. Archive-name checks found no private brief/reference/LocalOnly/credential/key filename matches; those name checks are not a complete arbitrary-data audit. The private competition file and raw references remain Git-ignored and outside runtime assets. The working tree was clean after the build. The APK was not installed, uploaded or submitted.

Current deliverables: `artifacts/gallery-review/cultural.zip` and `first-time.zip`, their offline `index.html` pages, and `artifacts/gallery-demo/Sankofa-software-preview.mp4` (**27 seconds, no audio stream**). Packet and preview manifests identify source/content/capture limits. The preview uses edited synthetic-input desktop excerpts and does not show a complete real-hand visit or qualifying headset/emulator demonstration. Actual human/device/release gates remain open as stated above.
