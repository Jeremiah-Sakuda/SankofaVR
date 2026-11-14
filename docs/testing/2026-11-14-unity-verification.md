# Unity verification after the November 14 changes

For the next session with the Unity editor. The [decision record](../decisions.md#november-14-2026--both-sides-in-one-sitting-a-hand-that-turns-the-stage-and-the-visitors-own-table) says what changed and why. The changes were written without Unity: they compile against reference assemblies and stand-ins for the Meta SDK, and nothing below has run. Work down the list in order; each step says what a failure most likely means.

The scene file is unchanged and does not need rebuilding: no builder code and no serialized field changed. Do not regenerate the comparison scenes.

## 1. Compile

Open the project once, or run any batch command, and read the console.

- `StoryTablePlacement.cs` uses only Meta SDK members the earlier version already used, plus `Behaviour.enabled` on the passthrough layer; each was found in the 207 source. A compile error anywhere is more likely a Unity 6 difference from the 2021 reference assemblies the type-check used.
- `Flame.shader` now reads `Blend One One, Zero One`. A pink fire means the shader failed to compile.

## 2. Tests

```sh
UNITY="/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity"
"$UNITY" -batchmode -projectPath "$PWD/unity" -buildTarget Android -runTests -testPlatform EditMode -assemblyNames Sankofa.Tests.Editor -testResults "$PWD/artifacts/logs/editmode.xml"
"$UNITY" -batchmode -noaudio -projectPath "$PWD/unity" -buildTarget Android -runTests -testPlatform PlayMode -testFilter 'Sankofa.Tests.PersonalStoryAppTests;Sankofa.Tests.PersonalStoryTableTests' -testResults "$PWD/artifacts/logs/story-table-playmode.xml"
dotnet test dotnet/Sankofa.Tests
```

Run them with `SANKOFA_ROOM` and `SANKOFA_PRESENT` unset. New or changed tests, and how to read a failure:

| Test | Checks | If it fails |
|---|---|---|
| EditMode `TheStoryFacesAwayFromWhicheverEdgeTheVisitorSitsAt`, `TheStorySitsInFrontOfTheVisitorAndWhollyOnTheTable`, `ALongTableIsStillTheVisitorsWhenTheySitAtOneEndOfIt` | Seating on a scanned table | A real fault in the seating maths: these passed in a numeric replica |
| EditMode `TheShippedManifestAsksNothingOfTheVisitorAtLaunch` | `unityplayer.SkipPermissionsDialog` is `true` | The manifest was made afresh from Meta's template, which writes `false`; set the value back to `true` and restore the three microphone removals |
| PlayMode `PagesTurnThemselvesOnceToldAndAnUnansweredChoiceGoesToTheFire` (changed) | A watching visitor hears four pages and both sides | If the page count is three, the fire did not turn the stage: look at `TurnPageWhenTold` |
| PlayMode `OnceASideIsToldAHandSweptAcrossTheStageTurnsItToTheOther` | Offer, lean, sweep, turn, both beads kept | It moves a simulated hand in real time. The fire holds its own turn while that hand is at the stage, so the sweeps are not racing it; if it still fails on timing, rerun it alone before changing code |
| PlayMode `AVisitorMayEndAfterOneSideAndKeepOnlyThatBead` | **End here**, one bead | |
| PlayMode `TheFireDoesNotTurnTheStageFromUnderAHandThatIsAtIt` | The fire's own turn waits while a hand is at the stage | |
| PlayMode `AHandThatReachesAnHeirloomBeginsItsTelling` | Heirloom rises, then begins | |
| PlayMode `TheStagesShortSoundsAreInTheBuildAndShort` | The two stage sounds import and load | If a sound was deleted on purpose, delete its name from this test and from the core test `TableSoundTests` |
| PlayMode `WherePagesTurnThemselvesAPagesQuestionsWaitUntilItIsToldAndAnAnswerDoesNotTellItAgain` | Questions appear when the page is told; an answer does not restart the page | |
| PlayMode `RestingAHandOnATableSetsTheStoryThereAndTheStandCanAlwaysBeRestored`, `WherePagesTurnThemselvesTheFirstPageOffersTheVisitorsOwnTable` | Hand placement, recovery, first-page action | They assume the editor has no room: `SANKOFA_ROOM` must be unset |
| PlayMode `TheOtherSideOfAMeetingTurnsTheStageAllTheWayRoundAndTheStageLeansWithAHand`, `OnceASideIsToldItsPageOffersTheOtherAndAWayToEnd` | Stage and page on their own | |
| PlayMode `EveryEndingIsReadableInFullAtBothTextSizes` | No ending is clipped; one page at the standard size | If an ending needs two parts at the standard size, shorten its sentence in `PersonalStoryApp.KeepInvitation` |
| PlayMode `ChoosingASideChangesTheEndingAndContinuingLetsTheFireChoose` (changed) | Hand-turned pages offer the other side on the page | |
| PlayMode `KeepingThreadsABeadAndTheGriotNamesAnUnheardSideOnReturn` (changed) | Back at the fire the griot names an unheard side without the greeting | |

## 3. Look

```sh
SANKOFA_CAPTURE=1 "$UNITY" -batchmode -projectPath "$PWD/unity" -buildTarget Android -runTests -testPlatform PlayMode -testFilter Sankofa.Tests.PersonalStoryTableTests -testResults "$PWD/artifacts/logs/story-table-captures.xml"
```

New frames `17-other-side-offered-72`, `17-other-side-offered-82` and `18-other-side-self-turning-72` land in `artifacts/story-table`. Check that:

- the plaque over the woman not yet heard is readable and not behind scenery, and the page's last line ("Sweep a hand across the stage to turn it.") is whole;
- **End here** sits in the page's action row without crowding it;
- the first page shows **Add my question**, **How it works** and **Use my table** in one row, with the line "Reach for an heirloom, or point and pinch its card." whole above them.

Then, in the editor with `SANKOFA_PRESENT=1`, watch one telling without touching anything: each page's questions should appear as its voice ends and the page turn five seconds later; the fire should choose a side after fourteen seconds, turn the stage again ten seconds after that side is told, and reach the ending with both sides heard. Judge the pauses by ear: `QuestionWaitSeconds`, `ChoiceWaitSeconds` and `OtherSideWaitSeconds` in `PersonalStoryApp` set them. Watch the second turn in particular. It is a fold, one whole turn of the turntable and a rise; if a whole turn reads as too much, `PopUpStage.BeginPhase` is where a half turn and a mirrored restaging would go.

## 4. Listen

- `p-145300bc4d3cdf9c.mp3` is the water telling's documented page, cut without re-encoding from the longer recording at 9.07 seconds. Listen to its last word: it should end cleanly after "cool waters".
- Play any quiet clip after any loud one from before (`p-76313a1b54b65b12`, the return greeting, was the loudest; `p-e92d128aa0306221` among the quietest). They should now sit together. `python3 scripts/level_table_narration.py --dry-run` prints the levels.
- With `SANKOFA_PRESENT=1`, choose a side and listen to the stage turn: a flap as it folds (`Resources/Sounds/stage-fold.wav`) and a rustle as the scenery stands (`stage-rise.wav`). Both are cuts of the tent recording, chosen by measurement. If either sounds wrong, delete the file and its `.meta`, and its line in `Art/Recorded/derived-sounds.json`: the app is then silent there. A better CC0 paper sound under the same name needs no code change, only a new record of its source. Their volumes are `.32f` and `.28f` in `PersonalStoryApp.SoundTheTable`.
- The fire's loop is very quiet by measurement (about −45 LUFS at its volume) but its crackles peak near full scale, so only ears can say whether it is present enough. Its volume is `FireVolume` in `PersonalStoryApp`; it now rises for a moment when the fire answers a hand or takes an heirloom.

## 5. Simulator

`SANKOFA_PRESENT=1` with **Sankofa → Play Personal Story Table in XR Simulator** gives the headset presentation. With the simulator's hands:

- reach an heirloom itself;
- at the offer, sweep a hand sideways in front of the stage, about twenty centimetres in under a second. The fire waits up to twenty seconds more while a hand is held in front of the stage, so there is time to place the simulator's hand first;
- choose **Use my table**. Without `SANKOFA_ROOM=1` the editor goes straight to the resting hand.

`SANKOFA_ROOM=1` lets the editor show the simulator's synthetic room and read its scan, for recording. This path has never run. If the scan throws or finds no table, the request to rest a hand should appear with the reason in the page's status line.

## 6. What only a headset can answer

Whether anything is asked at launch; whether the sweep and the resting hand read real hands; whether the story lands on a scanned table as intended (its axis conventions match the MRUK source, read on November 14, but no scan has been tried); whether the room shows correctly behind the story and at a sensible brightness; reach to the heirlooms and plaques from a real seat; frame rate. None of these has evidence. If a headset becomes available for even ten minutes, launch, **Use my table**, and one sweep are the three things to try.
