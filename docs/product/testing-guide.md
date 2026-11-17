# Testing Sankofa — the pop-up story table

Updated November 15, 2026 · Scene `StoryTable` · Read the [candidate record](../release/candidate.md) for the exact build. This guide doubles as the judges' testing instructions in the [Devpost draft](../release/submission-draft.md). The water telling leads the demonstration; the gourd is an additional interaction check. Actual software results are in the [November 15 verification record](../testing/2026-11-15-story-table-verification.md); headset and listening acceptance remain open.

One sitting can hear both sides of one meeting. It is designed for seated hands use; controllers are optional. No account, network or microphone is needed for the prepared tellings, which are complete offline. Choose **Use my table** to request room data; the adjustable stand works without it. The short demo is an edited visit, not a measurement of full-session duration.

## Hands

| To | Do |
|---|---|
| Press a plaque or button | Touch it with a fingertip, or point and pinch. The press lands when you release the pinch |
| Begin a story | Close a hand on an heirloom on the stage (a bundle, a gourd or a water pot), touch its label card, or point and pinch at the card from your seat |
| Turn the stage | When a page says "This meeting has another side", sweep an open hand sideways in front of the stage, about a hand's length. The stage leans with your hand, then folds and turns. Touching the plaque over the other woman does the same; so does waiting ten seconds |
| Use your own table | **Use my table** on the first page, or in Options. If your room scan holds a table near you the story moves onto it. Otherwise rest an open hand on your table and hold it still for a moment |
| Set the gourd down | When the gourd glows and the plaque **Set the gourd down** appears over it, touch or pinch the plaque. It stays for a few seconds; if you wait, the story sets the gourd down itself |
| Pause, adjust, leave | **Pause**, **Options** and **Leave** stay at the front of the page. Options holds Recenter, **Use my table**, stand height, voice, motion and text size |

## Judge walkthrough

1. Sit, set the controllers aside and let the table settle in front of you. If it sits too high or far, open **Options** and use **Recenter** or the stand height.
2. Reach for the **water pot**, labelled **Place & water**, and close a hand on it, or select its card. The fire takes the object and a paper theatre rises from the table. A woman brings a younger traveller to a remembered riverbank and becomes unsure of the place.
3. Listen or read the captions. After the documented page, choose **Stay with the one who remembers** at her figure. The stage turns to bring her forward. Follow her admission of uncertainty and her invitation to listen. If you only watch, the fire chooses a side after a few seconds.
4. When her side is told, the page offers the other side. Sweep a hand across the stage, or select **Walk with the younger traveller**. The stage turns toward her. **End here** ends after one side instead.
5. Let the younger traveller's entire final sentence finish before continuing. The telling moves from receiving a memory to carrying it herself in the future. The two women are imagined characters; no family relationship is asserted.
6. At the ending, press **Keep this story**. Two beads, one for each side you heard, join the strand on the stage's rim. A strand holds six: two sides of each of the three tellings.
7. Optional: when a page has been told its questions appear beneath it for a few seconds, and **Explore context** answers one and returns to the same place without telling the page again. **About this story** shows what is documented and what is imagined. From the first page, **Reopen kept story** replays exactly what you kept, without generating anything new; **Forget kept story** deletes it.

The bundle and the gourd begin the other two prepared tellings. Each has its own choice of side. The water telling has no **Set the gourd down** invitation.

## What is real and what is imagined

Pages marked **Documented context** quote a sourced passage (source details on request). Pages marked **Imagined scene** are fiction set in Enkare Nairobi in the 1850s. The two women are invented; the story is not an account of anyone's ancestors. The [source register](../content/enkare-nairobi/sources.md) holds the citations.

## Optional checks

- **Gourd interaction:** Begin the gourd telling and choose **Follow the one who brought less**. When **Set the gourd down** appears, select it and confirm that the vessel moves once. Hear the other side to discover what accepting less costs the woman who expected more; let that side's prop invitation resolve by itself. The opening already explains the portion left at home.
- **Your own table** (optional; not yet tried on a headset): **Use my table** shows your room, a little dimmed, and asks for scene permission. If your room scan holds a table near you, the theatre moves onto it, at the edge you sit at. If not, or if you decline, rest an open hand on your table for a moment and the theatre stands there. **Recenter** in Options always brings the virtual stand back.
- **Pause and recovery:** Pause holds the narration and the stage. Taking the headset off is designed to pause as well.
- **Comfort:** Options offers larger text, reduced motion (the stage appears without folding or turning) and voice off (captions only).

## Known limits

Verified with automated tests and desktop renders. No physical Quest was available during development, so real hand-tracking feel, the sweep, table fitting, the room behind the story and sustained frame rate on a headset are unmeasured. The story's cultural review is still open, which is why each telling says it is a draft. Online story generation exists in the codebase but is not part of the judged build.

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
