# November 14 work: assumptions and departures, for PM challenge

November 14, 2026 · Written by the cloud session that made the changes, for the next PM pass · A list of what was assumed, not a decision

The entrant has asked for a product-management pass over this work so that its assumptions are challenged. This page is that pass's starting point. For each assumption it gives what it rests on, the case against it, the cheapest way to check it and what it costs to undo. Where the work departs from the [November 10 PM plan](../product/entertainment-finish-plan.md), it says so. Nothing here has device, listening or participant evidence.

## Audit boundary

| | |
|---|---|
| Head | `main` at `e48b1e8`, the commit before this page |
| Last code Unity has compiled, tested and built | `6f40764`. `9027f20` adds only its record, and the recorded candidate APK is from it |
| Since then | 22 commits touching 66 code and asset files, about 3,100 lines added. **Unity has run none of them** |
| Checked without Unity | 440 core tests; 67 service tests; a type-check of the Unity scripts against reference assemblies with stand-ins for the Meta SDK (`dotnet/Sankofa.UnityCheck`); the table-seating maths in a numeric replica; the room-scan and passthrough conventions read from the Meta SDK 207 source |
| Not possible in that session | Compiling or running Unity, the simulator, a headset, hearing any sound |
| What the entrant asked for | A fresh audit against the competition's official pages with no change ruled out; then its code-side fixes, with the microphone and the model kept as a stretch goal; then merging to `main` and iterating |

The same session wrote the [decision record](../decisions.md#november-14-2026--both-sides-in-one-sitting-a-hand-that-turns-the-stage-and-the-visitors-own-table), the [Unity verification list](../testing/2026-11-14-unity-verification.md), the [Devpost draft](../release/submission-draft.md) and the [changelog](../release/changelog.md). Read them as that session's claims, not as findings.

## Departures from the November 10 PM plan

| The plan said | November 14 did | The session's reason | Open to the PM |
|---|---|---|---|
| The proof is that someone cares about the other side enough to return; the return invitation points to the unheard perspective | Both sides are heard in one sitting, and the fire turns the stage itself after ten seconds. Return now rests on the other two heirlooms and the strand | A judge who plays once would otherwise hear half of "one meeting, two sides" | Whether the first sitting should end after one side, with the other as the reason to return |
| Grab-to-rotate ranks below E1–E5 and E8 | A hand sweep that turns the stage was built | Beyond pressing plaques and warming a hand at the fire the build asked nothing of a hand, and the competition asks for hands first | Whether it stays, is offered more quietly, or is cut before verification |
| Feature freeze on November 13; contingency only on November 17–18 | New behaviour was written on November 14 and merged unverified | The entrant asked for the audit's fixes and the merge | Which of it ships. See "Cutting a change" |
| E2: touch the heirloom itself, and the invited gourd itself | The heirloom answers a hand. The gourd is still set down by its plaque | Not reached | Still open |
| Local table fitting is a claim only when device-verified | The draft copy describes the visitor's own table, by scan or by a resting hand; the caveat sits in its private notes | It is the entry's only use of the room | Whether public copy may mention it without a headset |
| E3: decide whether voice is accepted for release | The entrant decided it is off. The manifest now removes the microphone | The entrant's decision | Closed unless the entrant reopens it |
| E1: short reviewed context passages | The water telling says the short C1 sentence, cut from its existing recording. The other two tellings both say the full C6 passage; the shorter C6 wording was not adopted | No recording could be made | Whether one 17-second passage on two tellings is acceptable |
| E6: human labels on the heirlooms; stronger return lines | Labels unchanged. Back at the fire, the griot says the existing return line for an unheard side | New labels need a look at the cards; new lines need recordings | Still open |

## Assumptions, most consequential first

**1. One Unity session is enough to verify all this.** *Rests on:* the type-check, and each new test traced by hand. *Against:* the first Unity compile of about 3,100 lines has not happened. There are 11 new and 3 changed PlayMode tests and 4 new EditMode tests that have never run, plus captures to look at and sound to hear, and every hour they take comes out of the build, the upload, the video and the entry. *Check:* run the compile and the two test commands first and time them. *Undo:* build from `6f40764`, or cut changes one at a time as below. **A cut-off time for that choice is the first thing worth deciding.**

**2. The room should be off until asked.** *Rests on:* a reading of the competition's line that passthrough should change the experience, and a guess that a fire-lit story suits a dark room. *Against:* judges on a Quest 3 may expect their room at once and never press **Use my table**, so the entry could read as not using the room at all. Nobody has seen the dark room. *Check:* look at the first screen both ways in Unity. *Undo:* a few lines in `StoryTablePlacement.Configure`; the earlier build showed the room at launch.

**3. The fire should make both choices for a visitor who only watches.** *Rests on:* wanting a complete sitting in about four minutes with no input after the first touch. *Against:* it makes the central hand interaction optional, so a judge may finish having used a hand once. The waits (14 seconds to choose a side, 10 before the stage turns, 5 after each told page) are guesses and may read as dead air, or as too short to act. *Check:* watch one untouched run with a clock; one unfamiliar visitor. *Undo:* three constants in `PersonalStoryApp`.

**4. A mid-air sideways sweep is something people do, and tracking reads it.** *Rests on:* reasoning about a seated reach and six core tests on synthetic hand paths. *Against:* no hand has tried it. People may sweep slower than 20 cm in 0.7 seconds, or higher, or reach into the stage. It is offered once per telling, so a first failure may never be retried. *Check:* the simulator's hand, or ten minutes on any headset. *Undo:* thresholds in `TurnSettings`; one line disarms it and leaves the plaque and the fire.

**5. Hearing both sides at once is worth more than keeping one back.** *Rests on:* the view that the idea only lands when both are heard. *Against:* it spends the strongest reason to return, which the November 10 plan named as the proof. The second side starts seconds after the first with no pause to wonder. *Check:* ask one visitor what each woman was protecting, and whether they would come back. *Undo:* one line in `PersonalStorySession.OffersOtherSide`, with its five core tests.

**6. The entry stands without a model.** The decision is the entrant's. *The assumption inside it:* that judges will not miss what made the original Sankofa itself, and that a typed question which routes between three fixed tellings is honestly "personal". *Against:* the original was generative and conversational, and an adaptation that drops both may read as a different product. *Check:* read the draft copy as someone who knows the original.

**7. This is an adaptation.** *Rests on:* the competition's FAQ saying ported experiences belong in the Adapted division. *Against:* the Quest app shares the name, the griot, the fire and the invitation with the web app, and no code, media or generated content; the sponsor decides what counts. *Check:* reread the official division wording against [the changelog](../release/changelog.md); the entrant may ask the organisers. Start membership, which eligibility also needs, is unconfirmed.

**8. Questions should wait until their page is told.** *Rests on:* the reveal rule; the three first-page questions name details their page has not reached when it opens. *Against:* questions that appear late may never be seen, and the pause adds seven seconds a sitting. *Check:* watch it. *Undo:* one property and one constant.

**9. Sound can be set by measurement.** The 22 recordings were levelled to −19.3 LUFS, the water telling's documented page is a cut at 9.07 seconds placed by syllable count, two stage sounds are cuts of a tent recording, and the fire's volume is unchanged. *Against:* nobody has heard any of it; canvas may not read as paper. *Check:* ten minutes of listening. *Undo:* each clip's `level_steps` undoes its gain exactly; a deleted sound file is silent.

**10. A resting hand shows a table.** *Rests on:* four core tests on synthetic positions. *Against:* hand tracking is at its worst with a hand flat on a surface; a hand on a lap or armrest 30 cm ahead would be accepted; most headsets may hold no table in their scan, so most visitors would meet a permission prompt and then the hand request. *Check:* a headset only. *Undo:* `PlaceByHand` can return to the stand in a few lines.

**11. The draft copy's claims.** "About four minutes" is computed from recording lengths. "Everything works with hands alone, seated and offline" and the hands paragraph describe code, not use. The copy must be cut back to whatever survives verification.

## Still open from the November 10 plan

E2's gourd touched directly; E4 listening; E5 a headset session and independent install; E6 labels; E7 visual hierarchy; the three-visitor check; and E8, which was done against unverified code and must be redone against the frozen build.

## Numbers chosen by reasoning, not trial

| Where | Values |
|---|---|
| `PersonalStoryApp` | Page turns 1.6 s after it is told, 5 s if it has questions; choice of side waits 14 s; the fire's own turn 10 s, up to 20 s more while a hand is at the stage; heirloom touch 7 cm for 0.2 s; stage sounds at 0.32 and 0.28, fire at 0.28 |
| `TurnSettings` | 20 cm sideways within 0.7 s; other movement under 0.6 of that; a box 1 m wide, from 55 cm in front of the stage centre to 15 cm beyond it, 6 to 60 cm above the stage floor |
| `RestSettings` | Palm still within 2.5 cm for 1 s; 30 to 85 cm ahead of the eyes, 45 cm to either side, 18 to 95 cm below them |
| `StoryTablePlacement` | Room shown at 0.7 opacity; palm taken as 1.5 cm above the surface; story centred 30 cm beyond the hand, between 62 and 92 cm away; scanned table at least 0.90 by 0.55 m with its nearest edge within 0.9 m; 8 s scan and 25 s hand waits |
| `PopUpStage` | Leans up to 12° with the hand; side to side is one whole turn at 1.5 times the half turn's time |

## Cutting a change

Each can go alone. None has a single named switch yet; the session can add one per change if the PM wants cuts to be a one-word edit.

| Change | To cut it |
|---|---|
| Both sides in one sitting, with everything that hangs on it | `PersonalStorySession.OffersOtherSide` returns false; remove its five core tests and the PlayMode tests that expect two sides |
| Only the fire's own turn | `OtherSideWaitSeconds` set very large |
| The sweep | First line of `PersonalStoryApp.TurnByHand` returns |
| The heirloom answering a hand | First line of `ReachForHeirlooms` returns |
| **Use my table** on the first page | Remove the third item in `StoryTableView.RefreshIntake`; it stays in Options |
| The resting hand | `StoryTablePlacement.PlaceByHand` recentres and reports instead |
| Questions waiting for their page | `QuestionsOffered` returns true while reading; `QuestionWaitSeconds` equal to the page turn |
| Stage sounds | Delete the two files in `Resources/Sounds`, their record and their names in two tests |
| The microphone's removal | Delete three lines in the Android manifest and the core test that holds them |
| Everything | Build from `6f40764` |

## Not known at all

Whether the build launches on a headset. Whether Start membership, the app, the Competition channel and a signing key exist. Whether the Meta XR Simulator can make the sweep for the video. Whether a first-time visitor understands what changed between the two sides. How any of it looks or sounds.
