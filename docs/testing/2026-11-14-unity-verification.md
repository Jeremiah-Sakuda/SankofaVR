# Unity verification after the November 14 changes

For the next session with the Unity editor. The [decision record](../decisions.md#november-14-2026--both-sides-in-one-sitting-a-hand-that-turns-the-stage-and-the-visitors-own-table) says what changed and why. The changes were written without Unity: they compile against reference assemblies and stand-ins for the Meta SDK, and nothing below has run. Work down the list in order; each step says what a failure most likely means.

The scene file is unchanged and does not need rebuilding: no builder code and no serialized field changed. Do not regenerate the comparison scenes.

## 1. Compile

Open the project once, or run any batch command, and read the console.

- A compile error in `StoryTablePlacement.cs` would be a Meta SDK member that differs from the stand-ins. The file uses only members the earlier version already used, plus `Behaviour.enabled` on the passthrough layer.
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
| EditMode `TheShippedManifestAsksNothingOfTheVisitorAtLaunch` | `unityplayer.SkipPermissionsDialog` is `true` | Meta's manifest tool rewrote the manifest; set the value back to `true` |
| PlayMode `PagesTurnThemselvesOnceToldAndAnUnansweredChoiceGoesToTheFire` (changed) | A watching visitor hears four pages and both sides | If the page count is three, the fire did not turn the stage: look at `TurnPageWhenTold` |
| PlayMode `OnceASideIsToldAHandSweptAcrossTheStageTurnsItToTheOther` | Offer, lean, sweep, turn, both beads kept | It moves a simulated hand in real time and must finish its sweeps within the fire's ten seconds. On a slow or busy machine a timing failure here is the test's, not the feature's: rerun it alone before changing code |
| PlayMode `AVisitorMayEndAfterOneSideAndKeepOnlyThatBead` | **End here**, one bead | |
| PlayMode `AHandThatReachesAnHeirloomBeginsItsTelling` | Heirloom rises, then begins | |
| PlayMode `WherePagesTurnThemselvesAPagesQuestionsWaitUntilItIsToldAndAnAnswerDoesNotTellItAgain` | Questions appear when the page is told; an answer does not restart the page | |
| PlayMode `RestingAHandOnATableSetsTheStoryThereAndTheStandCanAlwaysBeRestored`, `WherePagesTurnThemselvesTheFirstPageOffersTheVisitorsOwnTable` | Hand placement, recovery, first-page action | They assume the editor has no room: `SANKOFA_ROOM` must be unset |
| PlayMode `TheOtherSideOfAMeetingTurnsTheStageAllTheWayRoundAndTheStageLeansWithAHand`, `OnceASideIsToldItsPageOffersTheOtherAndAWayToEnd` | Stage and page on their own | |
| PlayMode `EveryEndingIsReadableInFullAtBothTextSizes` | No ending is clipped; one page at the standard size | If an ending needs two parts at the standard size, shorten its sentence in `PersonalStoryApp.KeepInvitation` |
| PlayMode `ChoosingASideChangesTheEndingAndContinuingLetsTheFireChoose` (changed) | Hand-turned pages offer the other side on the page | |

## 3. Look

```sh
SANKOFA_CAPTURE=1 "$UNITY" -batchmode -projectPath "$PWD/unity" -buildTarget Android -runTests -testPlatform PlayMode -testFilter Sankofa.Tests.PersonalStoryTableTests -testResults "$PWD/artifacts/logs/story-table-captures.xml"
```

New frames `17-other-side-offered-72`, `17-other-side-offered-82` and `18-other-side-self-turning-72` land in `artifacts/story-table`. Check that:

- the plaque over the woman not yet heard is readable and not behind scenery, and the page's last line ("Sweep a hand across the stage to turn it.") is whole;
- **End here** sits in the page's action row without crowding it;
- the first page shows **Add my question**, **How it works** and **Use my table** in one row.

Then, in the editor with `SANKOFA_PRESENT=1`, watch one telling without touching anything: each page's questions should appear as its voice ends and the page turn five seconds later; the fire should choose a side after fourteen seconds, turn the stage again ten seconds after that side is told, and reach the ending with both sides heard. Judge the pauses by ear: `QuestionWaitSeconds`, `ChoiceWaitSeconds` and `OtherSideWaitSeconds` in `PersonalStoryApp` set them. Watch the second turn in particular. It is a fold, one whole turn of the turntable and a rise; if a whole turn reads as too much, `PopUpStage.BeginPhase` is where a half turn and a mirrored restaging would go.

## 4. Listen

- `p-145300bc4d3cdf9c.mp3` is the water telling's documented page, cut without re-encoding from the longer recording at 9.07 seconds. Listen to its last word: it should end cleanly after "cool waters".
- Play any quiet clip after any loud one from before (`p-76313a1b54b65b12`, the return greeting, was the loudest; `p-e92d128aa0306221` among the quietest). They should now sit together. `python3 scripts/level_table_narration.py --dry-run` prints the levels.
- The fire's loop is very quiet by measurement (about −45 LUFS at its volume) but its crackles peak near full scale, so only ears can say whether it is present enough. Its volume is `.28f` in `PersonalStoryApp.StartFireSound`.

## 5. Simulator

`SANKOFA_PRESENT=1` with **Sankofa → Play Personal Story Table in XR Simulator** gives the headset presentation. With the simulator's hands:

- reach an heirloom itself;
- at the offer, sweep a hand sideways in front of the stage, about twenty centimetres in under a second;
- choose **Use my table**. Without `SANKOFA_ROOM=1` the editor goes straight to the resting hand.

`SANKOFA_ROOM=1` lets the editor show the simulator's synthetic room and read its scan, for recording. This path has never run. If the scan throws or finds no table, the request to rest a hand should appear with the reason in the page's status line.

## 6. What only a headset can answer

Whether anything is asked at launch; whether the sweep and the resting hand read real hands; whether the scanned table's axes match what the code assumes; whether the room shows correctly behind the story and at a sensible brightness; reach to the heirlooms and plaques from a real seat; frame rate. None of these has evidence. If a headset becomes available for even ten minutes, launch, **Use my table**, and one sweep are the three things to try.
