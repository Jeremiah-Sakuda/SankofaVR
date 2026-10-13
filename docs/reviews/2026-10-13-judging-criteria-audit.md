# Judging-criteria audit of the illustrated theatre

October 13, 2026 · four read-only audits of `8c169bc`, one for each equally weighted judging criterion: Innovation & Creativity, Experience Design, Technical Implementation, and Polish & Presentation. The auditors read the code, the story package, the scene builder and the desktop captures. This is software and desktop evidence only. Nothing was run on a headset, reviewed by family or community, or observed with participants.

## Where the auditors agreed

- **Looking never changes what is heard.** Head direction lights a question, turns a painted woman toward the visitor, adds a closing caption and raises the remembered shapes. It never changes a spoken line. That is consistent with SK-08, but it undersells "a story that notices what you notice". *(Innovation 1, Experience 2.)*
- **The hand-off is the most distinctive moment, but it doesn't read yet.** The gift appeared at the hip of the pose being left, then flew. The bead was a glint at Naserian's hip. The flour, once set down, stayed within the visitor's reach, nowhere near the women it was meant to be "between". *(Innovation 3, Experience 6, Polish 1, 3, 9.)*
- **The opening asks for a choice that isn't one.** With one first story, the visitor waits at the hearth indefinitely until they press it. The draft notice also overlapped the title card's hint. *(Experience 1, 8; Polish 4.)*
- **Judges can't install it yet.** The APK is debug-signed, its version code is fixed at 1, its app id is provisional, and the device list includes Quest 1. *(Technical 1.)*

## Done in this pass

| Finding | Change | Evidence |
|---|---|---|
| A passive visitor stalls at the opening choice | When only one story can be chosen and the visitor does nothing for 15 s after the welcome, it begins. A visitor returning with a kept visit is still asked | .NET test `ASilentVisitorIsTakenIntoTheOnlyStoryButAReturningOneIsAsked` |
| Draft notice overlaps the title card | The notice is set once, under the title card's hint; the caption status line returns when the visit begins | Capture `01-hearth` |
| Gift leaves from the wrong place | The gift appears in the painted palms only once the offering pose shows, rests there for half a second with a light pulse, then crosses. Taking it early by button or hand cancels the crossing | PlayMode `GourdCrosses…`, `AGiftTakenWhileStillInThePaintedPalmsStaysWithTheVisitor`; capture `07` shows the bead in Naserian's cupped hands |
| The flour is not "between them" | After it settles on the reachable hide, the hide and the flour cross into the painting and rest at Wambui's side of the sheep, between the two women | PlayMode `FlourSetDownWithinReachCrossesToRestBetweenTheWomen`; captures `06b`, `07` |
| A brief tracking flicker undoes a hold, touch or pinch | A hand that drops below confident tracking for less than 0.18 s keeps its last pose, and the pointer keeps its aim and pinch. A pinch may slip off its button for up to 0.15 s; moving onto another button still cancels at once | .NET test `ABriefSlipOffTheButtonIsForgivenButALongOneOrAnotherButtonIsNot`. Thresholds need tuning on a headset |
| Photographic moon halo over a painted sky | The theatre's sky has its own material with the halo at 35% | Captures |

## Open, by what it waits for

**Needs a decision from you**
- Release signing and keystore custody, version code policy, the permanent app id, and the supported-device list.
- Whether the fire may speak an already-recorded answer about what the visitor lingered on, during the story's existing rests. The Innovation audit's top suggestion; the current rule is that looking never advances the story.
- Whether to add a real plot fork, which would need new narration.

**Needs a headset**
- Frame rate. The risks are about 1.6 screens of alpha-tested painted cards, 4× MSAA, the ground and sky shaders, and the full-sphere transition curtain.
- Shader and pipeline warm-up before the fire becomes the world.
- The grace thresholds against real OpenXR hands.
- Reach to the hide and to the gate, which sits about 79° to the right in the continuation.
- Whether the story fire by the visitor's feet reads as the griot.

**Needs art**
- A painted look-at-you pose.
- A painted sheep. The current one is a theatrical stand-in.
- A painted present-day river; the continuation is still the 3D city.
- Hearth art in the painted style. The opening and closing frames are the weakest.
- Higher-resolution cast cells. At about 10 px per degree, the painting is magnified roughly 2.5× on Quest.

**Software, next**
- Alpha-to-coverage on painted edges.
- A touch-target ring for touch invitations.
- An end-of-visit recap that pairs each action with what it caused, instead of a log.
- Showing **Keep** earlier.
- Not repeating the ember offer in the continuation.
- Relabelling "Something new" when there is only one story.
- A pause caption, and PlayMode tests for headset removal and focus loss.
- A testing guide for the illustrated route.
- Longer motion captures at 30–60 fps for the video.
