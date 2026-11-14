# Winning rework: audit and plan

November 10, 2026 · Eight days before the entry closes · Audit and proposal; no product code changed; no device, listening or participant evidence

The entrant chose Sankofa as their one solo entry and asked for a deeper audit and a rework aimed only at winning, carrying over what made the [original web Sankofa](https://devpost.com/software/sankofa-y47f9p) win its category. Nothing here is approved scope until the entrant says so. The [PRD](../product/prd.md) and [delivery plan](../product/delivery-plan.md) still govern.

## Verdict

The build is sound and the judged visit is weak. The engine, content pipeline and release path are in good order. What a judge would meet is a page of text and buttons with a small paper stage behind it, and a fire that does not speak until a story has been chosen.

The web version won on the things this build has since set aside: a spoken welcome, a storyteller you talk to, and words, pictures and sound arriving together. Most of those already exist in this repository, in the earlier scenes. The rework is mostly bringing them back to the front and removing what stands in their way.

My estimate for the current judged path is about 5.5 out of 10 on each criterion, with Technical nearer 5 because nothing has run on a headset. That is lower than the comparison I gave earlier today, which was made before reading the captures and code closely.

## What was checked today

| Check | Result |
| --- | --- |
| Core story and flow tests (`dotnet test dotnet/Sankofa.Tests`) | 371 of 371 passed |
| Story service tests (`scripts/test_personal_service.py`) | 32 of 32 passed |
| Unity EditMode, batch, muted, at `a1afacf` | 36 of 36 passed |
| Workspace check with the competition brief | Passed |
| PlayMode, XR Simulator, sound, headset | Not run |

The judged path was read in `PersonalStoryApp`, `StoryTableView`, `PopUpStage` and `StoryTablePlacement`, and in the November 9 captures under `artifacts/story-table`. The earlier Hearth, illustrated theatre and gallery scenes were reviewed from their captures and records.

## What a judge meets today

| Moment | What happens | Problem |
| --- | --- | --- |
| Launch | A cream deck of text with three to ten dark buttons fills the lower half of the view. Three rough heirloom shapes and a small flame sit behind it. Only the fire recording is heard | No welcome and no voice. It reads as a menu |
| Choosing | Touch a labelled card under an heirloom | A button press, where the heirloom itself could be picked up |
| The telling | Three pages, about two minutes of narration. The stage rises behind the deck; the page text repeats what the voice says | The stage is the best thing in the build and it is the smallest thing in view |
| Each page | The recorded voice begins "Imagined scene:" | Honest, and it breaks the telling every forty seconds |
| Whose side | Two buttons on the deck, and two small labels above the figures | The stage turns well. The choice that turns it is another button |
| The gourd | A labelled panel invites setting it down | The gesture code exists; the invitation is a panel |
| Questions | Authored questions as buttons; typing on a virtual keyboard for your own | Typing is the weakest input in a headset |
| Ending | Text, then Keep this story or Another question | Keep saves a file. Nothing in the room changes, and nothing calls the visitor back |

Other findings:

- **The room.** On a headset passthrough turns on by default and the table sits on a virtual stand. Table fitting is a separate choice. This is a sensible arrangement and it has never been seen on a device.
- **Voice input.** There is no microphone permission in the manifest and no speech path.
- **Hands.** Custom hand input on Unity XR Hands. Palm-receive, deposit and touch gestures from the earlier scenes are still in `Interaction/`. Every control in the table scene is a poke panel.
- **Length.** Three tellings with two sides each: six routes of about two minutes.
- **Frame.** The narrator is directed as a West African griot, the name is Akan, and the only setting is Kenyan. The web version covered many regions, so the frame held. With one setting it needs a line that says the griot keeps stories from many fires.
- **Process.** Six weeks, five directions and twelve review documents, with no headset session, no listening session and no first-time visitor. Each change of direction answered an AI review. This plan is a sixth, so it is kept to changes that reuse built parts and can be checked in the simulator.

## What won on the web, and where it is now

| Winning element on the web | In this build | Proposal |
| --- | --- | --- |
| A ritual opening: "Come. Sit with me.", fire and gold particles | The Hearth scene opened this way and the line is recorded in the earlier packages. The table opens on a deck | Bring it back as the first thirty seconds |
| A griot you speak with, by voice | None. Authored buttons and a keyboard | Hold a palm to the fire and speak. Offline, the same gesture raises three questions as embers |
| Your own seeds shape the story | A typed question picks one of three tellings by keyword | Ask aloud what the visitor carries. Say plainly which telling the fire chose and why |
| Words, pictures, voice and ambience arrive together | All exist, behind a page of text | Remove the page. The stage is the picture and the voice is the text |
| Trust labels shown beside the content | Shown, and also spoken on every page | Keep the visible tag. Say it once, in the opening |
| Waiting turned into ritual | Not needed offline | Use it for voice answers: the flame leans in and embers rise while the answer is prepared |
| Follow-up questions | Authored questions | Keep them, as embers |
| Many regions | One | Leave it. One setting told well beats several untested ones |

## Keep, cut, bring back

**Keep.** The pop-up stage and its turn. The three tellings and their two sides. The story engine, validation and saved replay. The recorded ambience. The stand, passthrough and optional table fitting. The release build and its guards.

**Cut from the judged path.** The reading deck. The virtual keyboard. Create my story and its consent pages as a front door. The place and period selectors, which each have one value. Previous page. The spoken label on every page. The code can stay; a judge should not meet it.

**Bring back from the earlier scenes.** The spoken welcome and "You came back. Sit with me." The fuller fire with its stones and embers. Palm and deposit gestures. The bead from the gallery, as the keepsake.

## The reworked visit

About six minutes, seated, hands only. Every invitation also resolves if the visitor waits.

1. **Kindle.** A fire catches on the table. "Come. Sit with me." The griot says once that the people in tonight's story are imagined and that documented pages are marked. Holding hands toward the fire makes it flare, which teaches hands without a word of instruction.
2. **What do you carry?** Three heirlooms rest by the fire. Pick one up and give it to the fire, or touch it.
3. **The telling.** The stage rises, larger and nearer. The voice tells the story; a single caption line sits at the foot of the stage with its paper tag. Pages turn when the voice finishes.
4. **Whose side?** Both women are lit. Touch one, or turn the stage toward her. If the visitor waits, the fire chooses and says so.
5. **The gourd.** The gourd itself is offered and set down by hand.
6. **Ask the fire.** At any point, an open palm toward the fire makes it lean in. Online, the visitor speaks and the griot answers in its own voice, within the story's sources. Offline or without a microphone, three question embers rise to be touched.
7. **Keep.** The stage folds into a bead. Threading it on a strand by the fire keeps the telling. Six beads are possible, one for each side. On the next visit the fire says "You came back" and names a side not yet heard.

## Priorities

| Tier | Work | Why |
| --- | --- | --- |
| Must | Opening ritual; deck removed and stage enlarged; captions as one line; pages that turn themselves | The first two minutes decide most of the score |
| Must | Choices made on the objects: heirloom, side, gourd; one small menu for pause, comfort and leave | Hands used expressively is the point of the competition |
| Must | Entrant listens to every clip; re-record without the spoken label; record the welcome and return lines | Sound design is scored and has never been heard |
| Should | Ask the fire by voice, turn by turn, with the ember fallback and a hosted service with limits | The element that carried the web win, and the route to Best Agentic Interaction |
| Should | Bead strand and return greeting | A reason to come back |
| Could | Better heirloom models; the fuller fire; turning the stage by hand | Polish |

Voice ships only if it works in the simulator and on one real headset. Otherwise it is switched off in the build and the embers stand in for it. It needs the entrant's decision on the microphone permission, hosting until December 11, a spending limit and provider terms.

If time runs short, cut in this order: voice, turning by hand, the strand, the new models. Do not cut the opening, the deck removal, the listening pass, one real install or the video.

## With no headset

The entrant has no Quest for now. The plan allows for that in three ways.

- Everything is checked in PlayMode tests, desktop captures and the Meta XR Simulator, and labelled as such.
- Targets are large, every gesture has a touch alternative, and nothing blocks if a gesture is missed.
- One remote install is treated as a release gate. A friend, a university lab or another Start member installs through the Competition channel and reports whether it launches, whether hands work and whether the voice is heard. Upload the first signed build by Friday so there is time to act on what they find.

## Schedule

| Day | Build | Entrant |
| --- | --- | --- |
| Tue Nov 10 | Direction approved | Start membership, dashboard app, Competition channel, signing key. Listen to the twelve clips. Find a tester |
| Wed Nov 11 | Opening ritual. Deck removed, stage enlarged, caption line, self-turning pages | Approve the opening script and the new recordings |
| Thu Nov 12 | Choices on objects. Small menu. New recordings in place | |
| Fri Nov 13 | Voice path and ember fallback. First signed upload | Decide on hosting and limits. Send the invite to the tester |
| Sat Nov 14 | Bead strand and return greeting. Fire and heirlooms. Sound pass. **Feature freeze** | Listen again |
| Sun Nov 15 | Fixes from the tester and the simulator only. Record the video in the simulator, labelled | |
| Mon Nov 16 | Final signed build. Video cut. Devpost copy. **Submit** | Check every link signed out |
| Tue Nov 17 – Wed Nov 18 | Buffer until 3 p.m. Eastern | |

## Decisions for the entrant

| Decision | Default if unanswered |
| --- | --- |
| Approve the reworked visit as the judged path | Nothing is built |
| Say the imagined-scene disclosure once and show it on every page, in place of speaking it on every page | Stays as it is |
| Add the microphone and a hosted voice service | Embers only |
| Approve new recordings of the welcome, the return line and the re-read pages | Existing clips stay |
| Cultural review: send the packet, or ship with the draft label | Ship with the label |

## Limits of this audit

Captures are desktop renders. No sound was played. The scores are one reviewer's estimates against an unknown field. The earlier scenes were judged from their records, so the effort to bring their parts into the table scene is an estimate until the first day's work is done.
