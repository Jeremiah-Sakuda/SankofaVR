# Living gallery review build

October 13, 2026 · Local review APK: `artifacts/Sankofa-LivingGallery.apk`. No Competition channel exists yet. The checks below need a headset for hand/reach/depth acceptance; desktop batch captures only establish staging/flow.

1. Sit comfortably and launch the build. The framed *What We Carry* painting waits; it must not enter by itself. Settings include text, reduced motion, height and recenter.
2. Select **Enter painting** for the default gourd perspective, or **What she remembers** for the other perspective. Enter is the wide primary control; the two smaller controls select a perspective explicitly. Leave remains beside Pause at the lap. You can use fingertips or point and pinch, with either hand.
3. Listen to the reconstruction disclosure. Receive the ember if you wish, or let it rest nearby. The image opens into depth without moving your viewpoint.
4. Follow the women by the water. The gourd route shows food divided before Wambui leaves; the memory route shows an earlier meeting. Ask an available object question with its labelled chip or the nearby questions panel. Looking alone does not ask or accept.
5. Receive or watch the flour hand-off. Help set the flour down, or wait. Check that Naserian offers milk directly after help and places it nearby after waiting; both paths welcome you and let you receive or leave it.
6. Receive or leave the bead. The meeting ends and the same framed painting returns. Keep by the panel, or receive the closing memento, carry an open palm to the labelled recess, hold briefly, then close/release. The recess brightens during the open-palm dwell and the nearby hint asks you to close your hand once ready. Passing over it or losing tracking must not save. If a write fails, the keepsake stays in your hand; opening, dwelling and releasing again retries. The hint disappears after a successful save.
7. **Back to the artwork** opens the saved options: follow the other thread, replay this visit, visit the modern river, or forget. Replay preserves the retained authored plan and outcomes; question timing is not replayed.
8. In today's imagined garden, leave the bead on the gate deliberately or let it stay beside you. Return again. Relaunch should remember only an explicitly kept visit; forgetting removes it locally.
9. Repeat once with no hand actions after Enter, once with one hand, and once with reduced motion. Test pause, headset removal/focus loss, recenter and interrupted questions. Record build/device, failure details and whether reach, depth, text and sound are comfortable.

The current candidate has no live AI dialogue, newly generated personal histories, second narration language or compulsory walking. Historical context and fictional encounters are distinct. Cultural, pronunciation, asset-rights and actual Quest performance review remain pending. These notes do not claim they are accepted.

---

# Testing guide

For judges and testers of Sankofa on Meta Quest. A full visit, with the continuation to today, takes 10 to 15 minutes, seated; one era on its own (*Before the railway*, steps 1 to 6 below) is a complete story in about 7 minutes. Every step works with hands alone. Touch controllers are optional, and the story also moves on by itself if you do nothing.

**Status — October 10, 2026:** written from the software build and desktop captures. The install steps below wait for a Competition release channel and an invite link, and the walkthrough has not yet been confirmed on a headset.

## Install

1. Open the invite link to the Competition release channel (to be added) while signed in to the Meta account linked to your headset.
2. Install **Sankofa** from the headset's library.
3. Sit down in a clear space and start it. It needs no network, account or microphone.

## Hands

| To | Do |
|---|---|
| Press a button | Touch it with a fingertip, or point at it from the seat and pinch. The press lands when you release the pinch |
| Receive something | Hold out an open palm, facing up, under the object being offered. A ring of firelight shows where, and fills as you hold |
| Touch something in the scene | Reach out and touch it: the water, the trading hide, a rail, the gate or the soil. Each also has a button alternative |
| Ask the fire | Press a question on the panel by your right hand. The fire answers, then picks up the story where it left off |
| Pause, return or leave | Use the plaque by your lap. Taking the headset off also pauses |

Touch controllers also work: point and pull the trigger instead of pinching. Everything can be done with hands alone.

## A ten-minute walkthrough

1. **Start.** Press **Begin** on the title card over the fire.
2. **Welcome.** The fire speaks: "Come. Sit with me." Choose **Before the railway**.
3. **Entry.** The fire rises across your view and opens onto a stream at dusk in the 1850s. You have not moved.
4. **Look and act.** Turn your head toward Naserian, the ewe or a gourd and hold it there a moment: the person you face looks back, or follows your look to the thing. (This uses head direction; there is no eye tracking.) When asked, touch the water. Hold out your palm for Wambui's gourd of flour, then touch the hide to set it down between the two women. Naserian then holds a bead out to you: open your palm to take it.
5. **Ask.** At any point, press *Is this a real place?* or another question.
6. **Home.** The world folds back into the fire. Small shapes of light of what you lingered on rise from it, a line under the caption lists what you did by hand, and the bead waits beside **Keep this visit**. Press it.
7. **Return.** Begin again. The fire remembers the bead and offers to follow it to the same water today, a night-time city river, where what you lingered on rises again from the jiko. Hang the bead on the garden gate.

To see the difference your actions make, play the same era again and do nothing. The story carries on passively, and different lines play: Wambui sets the gourd down herself, and the bead is left beside you. The 1899 era (*When the railway came*) gives a seed instead of a bead, and its continuation plants it.

## Comfort and settings

The camera never moves you. Settings sit behind the plaque by your lap:

- caption size;
- reduced motion, which replaces the rising fire with a short dim dissolve;
- ambience level;
- recentre.

Captions are always on.

## What to know

- The people are fictional composites. The baseline uses generic 3D characters; the separate illustrated-theatre MVP uses painted figures in its first chapter. Both are theatrical drafts awaiting review by Kikuyu and Maasai families. Each era says so in a small line under the captions.
- Lines grounded in a source and lines that are reconstruction are kept distinct; see the [source register](../content/enkare-nairobi/sources.md).
- Kept visits are stored only on the headset, and only when you press **Keep**: the story's path, what you did by hand, and up to three things you looked toward most (head direction, not eye tracking). No camera, voice or hand data is stored. **Forget** removes them.
- There is no music. The ambience is field recordings, credited in the app under **Credits**.

## Measuring a run

The application logs its own frame rate and CPU and GPU frame times every ten seconds, with the scene it is in. With the headset connected over USB and developer mode on:

```sh
adb logcat -s Unity | grep "\[Sankofa\] perf"
```

Meta's OVR Metrics Tool can show the same live in the headset. Record the device, the scene and whether the transition was playing; these are device evidence, kept apart from desktop captures.
