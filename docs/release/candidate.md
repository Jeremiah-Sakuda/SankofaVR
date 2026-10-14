# Gallery candidate — October 14 completion pass

Package **0.6.0**, LivingGallery. This pass follows the remaining [judging findings](../reviews/2026-10-13-independent-gallery-panel.md). It does not claim new judge scores or a winning outcome.

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
