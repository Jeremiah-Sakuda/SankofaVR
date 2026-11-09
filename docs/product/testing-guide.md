# Testing Sankofa — the pop-up story table

November 9, 2026 · Scene `StoryTable` · Read the [candidate record](../release/candidate.md) for the exact build. This guide doubles as the judges' testing instructions in the [Devpost draft](../release/submission-draft.md).

Each telling runs about 2 to 3 minutes; the walkthrough below, which visits both sides of the gourd telling, takes about 6 to 8. It is seated and hands only. Controllers are optional. No account, network or microphone is needed. The prepared tellings are complete offline.

## Hands

| To | Do |
|---|---|
| Press a plaque or button | Touch it with a fingertip, or point and pinch. The press lands when you release the pinch |
| Begin a story | Touch an heirloom on the stage: a bundle, a gourd or a water pot |
| Set the gourd down | When the plaque **Set the gourd down** appears and the gourd glows, touch or pinch the gourd, or press the plaque |
| Pause, adjust, leave | **Pause**, **Options** and **Leave** stay at the front of the page. Options holds Recenter, table fitting, stand height, voice, motion and text size |

## Judge walkthrough

1. Sit, set the controllers aside and let the table settle in front of you. If it sits too high or far, open **Options** and use **Recenter** or the stand height.
2. Touch the **gourd**. The fire takes it and a paper theatre rises from the table: dusk, a river and two women.
3. Read or listen. At the turning point, choose whose side to follow. Plaques above each woman and buttons on the page do the same thing. Choose **Follow the one who brought less**. The stage folds, turns and brings her forward. (If you simply turn the page, the fire chooses, and the ending says so.)
4. When **Set the gourd down** appears, move the gourd between the women with your hand. If you wait, the story continues and the women set it down themselves. Either way, they make their own agreement.
5. At the ending, the fire names the side you followed and the side you have not heard. Press **Keep this story**.
6. Touch the gourd again and choose **Wait with the one who expected more**. The stage turns to her side, and the same meeting shows what accepting less costs her.
7. Optional: **Explore context** answers a question beside the page and returns to the same place. **About this story** shows what is documented and what is imagined. From the first page, **Reopen kept story** replays exactly what you kept, without generating anything new; **Forget kept story** deletes it.

The bundle and the water pot begin the other two prepared tellings. Each has its own choice of side.

## What is real and what is imagined

Pages marked **Documented context** quote a sourced passage (source details on request). Pages marked **Imagined scene** are fiction set in Enkare Nairobi in the 1850s. The two women are invented; the story is not an account of anyone's ancestors. The [source register](../content/enkare-nairobi/sources.md) holds the citations.

## Optional checks

- **Table fitting** (Quest 3/3S with a real table): Options → **Fit my table** asks for scene permission, then moves the theatre onto the detected surface. Denying permission or having no table leaves the adjustable stand in place.
- **Pause and recovery:** Pause holds the narration and the stage. Taking the headset off pauses too.
- **Comfort:** Options offers larger text, reduced motion (the stage appears without folding or turning) and voice off (captions only).

## Known limits

Tested in software and in the Meta XR Simulator; no physical Quest was available during development, so real hand-tracking feel and sustained frame rate on device are unmeasured. The story's cultural review is still open, which is why each telling says it is a draft. Online story generation exists in the codebase but is not part of the judged build.

---

The following walkthrough is preserved for the earlier gallery build only.

---

# Preserved living-gallery review build

October 14, 2026 · Package **v0.7.0** · Local APK: `artifacts/Sankofa-LivingGallery.apk`. Read the [candidate identity](../release/candidate.md) before installation. Competition-channel access and installation on a separate Quest are not yet established. This walkthrough describes implemented software, not accepted hardware behaviour.

On October 15 the user authorised simulator verification, superseding the earlier no-simulator restriction. **No sound playback** remains in force. Keep audio muted until a listening session is authorised. Simulator and silent batch captures cannot establish headset comfort, real-hand reliability, sustained Quest frame rate or cultural acceptance. The [initial simulator record](../reviews/2026-10-15-gallery-simulator.md) preserves scripted results; the [input/layout follow-up](../reviews/2026-10-15-gallery-input-and-layout.md) records manual controller selection, clear context labels and recovery from delayed initial tracking.

## Install and start

For a local developer review, use the Android/Quest commands in the [Unity guide](../../unity/README.md). For judges, the Competition invite/install link remains pending in the [access handoff](../release/access-handoff.md). Do not assume an APK on disk supplies judge access.

Sit comfortably in a clear space. Sankofa needs no runtime network, account or microphone. It opens before *What We Carry*. The painting waits indefinitely for deliberate entry; browsing is not a choice and nothing is automatically saved.

## Hands and alternatives

| To | Do |
|---|---|
| Select a plaque | Touch with a fingertip, or point and pinch with either hand; selection commits on pinch release |
| Receive an offered object | Hold an open palm facing up beneath it; the ring fills as the hold completes |
| Use an alternative | Select the labelled invitation plaque; the received object stays visible in a nearby carry position |
| Help set down the flour | Touch the trading hide when invited, or select its labelled alternative |
| Ask a question | Select the labelled object question or its central fallback; looking alone never asks or receives |
| Pause or adjust comfort | Use the visible Pause and Settings row below the story |
| Keep a finished visit physically | Receive its memento, hold an open palm over the recess, then close/release deliberately |

Controllers are optional. Waiting through an invitation is a complete, truthful way to participate. An illustrated acknowledgement can answer sustained head direction; this is not eye tracking or consent.

## First visit

1. Before entering, open **Settings** if needed. Placement follows your seated eye height. Choose text size, reduced motion, voice/ambience, height/reach or recenter. **Voice: captions only** does not start narration audio and retains its recorded timing. **About this artwork** explains the Akan title, Kenyan encounter, fiction/context and replay boundary. **Credits** provides paged attribution. Done closes preferences.
2. Select **Enter painting** for the default route, or explicitly choose **Wambui · before her journey** or **Naserian · their earlier meeting**. The narrator welcomes you, offers an ember and discloses the reconstruction before the painting opens. Your viewpoint stays stationary.
3. At the water, receive Wambui's flour or watch. Wambui's perspective shows what she divided before leaving. Naserian's perspective reveals whose promise fell short at their earlier meeting. The two women make their agreement on every path.
4. Touch the hide to help place the flour, or wait. Helping changes how Naserian addresses you and offers milk. Waiting receives a different, equally warm welcome. You may receive or leave the milk on either path.
5. Receive or leave the bead. The encounter returns to the same framed painting. The historical telling is about seven minutes in authored passive timing; questions, transitions and real actions add time. Actual novice duration is unmeasured.
6. After the final line, choose Keep, or use the physical recess. Passing over it or losing tracking must not save. If a write fails, the keepsake remains available for retry. No-Keep visits leave no visit-store file or diagnostic interaction journal.

## Return, replay and forgetting

**Back to the artwork** offers the other memory, **Replay saved telling**, the modern river continuation and Forget. The focused other-memory visit retains disclosure, agreement, bead and return while omitting repeated introductory gestures. Its authored passive speech/wait schedule is about 2:03 / 2:15; neither is an observed participant duration.

Replay retains the realised words, recordings and resolved actions, using the current presentation. It does not regenerate wording, repeat questions or reproduce interruption timing. A continuation is a new child visit, not a replay. In the imagined modern garden, touch the gate to leave the bead or let it stay with you.

Only a deliberately kept visit survives relaunch. Replacing it names what will be removed: the first physical deposit confirms intent, and a second deliberate deposit commits replacement. A failed write retains the previous visit. Forget requires confirmation and removes the local saved telling/memento without removing the artwork.

## Recovery and acceptance checks

Opening Settings pauses the story and invitation clock. Done resumes only a pause Settings caused; it preserves an existing visitor/system pause. A pause requested during entry takes effect when the transition lands. Return during a story asks for confirmation; an unfinished visit cannot be kept. Headset removal and focus loss also pause.

Run one fully passive visit, one one-hand visit, accepted/passive hospitality, moving-hand caption clearance at every text size, selection/replay carry, tracking loss, modal settings, recenter, interrupted questions, failed save/retry, replacement cancellation, relaunch and Forget. Record actual device/build, assistance, failures, discomfort and interpretation. Use the [full evidence protocol](../testing/gallery-next-evidence-session.md).

The people, dialogue, gifts, garments and contemporary garden are imaginative theatre, with documented context distinguished in the telling and [source register](../content/enkare-nairobi/sources.md). Cultural, Maa-pronunciation, voice/mix, applicable rights and actual Quest acceptance remain pending. The candidate has authored English narration and bounded recorded answers; it has no live AI dialogue, newly generated personal histories, second narration language or compulsory walking.

Historical Hearth and IllustratedTheatre scenes remain comparison records, with explicitly named commands in the Unity guide. They are not the default APK or current walkthrough.
