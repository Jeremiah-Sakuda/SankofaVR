# The first session with Unity: what to do, in order

November 15, 2026 · Written by the cloud session, for the session on the entrant's Windows laptop · A plan. Nothing in it has run

Unity has compiled nothing since `6f40764`. `main` at `e48b1e8` adds 22 commits and about 3,100 lines that only a type-check has seen. This page is the order of work for the first session that has the editor. The [checklist](2026-11-14-unity-verification.md) says what each step checks and how to read a failure. The [PM pass](../reviews/2026-11-14-assumptions-for-challenge.md#pm-pass) holds the call on each change. Build nothing new: the PM froze behaviour on November 14.

## Before Unity opens

1. **Licence.** Unity reported no active licence on November 14. The entrant signs in to Unity Hub; batch commands fail without one.
2. **Line endings.** Git for Windows converts text files to CRLF on checkout. Three files are compared with the app's exact output and fail when converted; `.gitattributes` on this branch now pins them. To make the whole tree match the repository byte for byte, with nothing uncommitted in the clone:

   ```sh
   git config core.autocrlf false
   git rm -r --cached . -q
   git reset --hard
   ```

3. **Branch.** Work on a branch of your own, started from the cloud session's branch, which is `main` plus documents and the line-ending rule:

   ```sh
   git fetch origin
   git switch -c unity-verification origin/claude/nifty-hawking-vjpp52
   ```

4. **The private brief.** `HACKATHON.local.md` is not in a fresh clone. Follow [local competition setup](../development.md#local-competition-context) before competition work, and never commit it.
5. **Paths.** The commands in the checklist and the [Unity README](../../unity/README.md) are written for macOS; in Git Bash only the editor's path differs. The usual locations, to confirm in Unity Hub → Installs:

   ```sh
   UNITY="/c/Program Files/Unity/Hub/Editor/6000.3.25f1/Editor/Unity.exe"
   DOTNET="$LOCALAPPDATA/Microsoft/dotnet/dotnet.exe"
   ```

## Order of work

Time each step. How long things take decides what fits before the deadline.

| # | Step | Done when | If it is red |
|---|---|---|---|
| 1 | First import and compile: the EditMode command in the checklist's section 2 does both | The log shows no compile error | Fix narrowly. The type-check used 2021 reference assemblies and stand-ins for the Meta SDK, so expect API differences, not design faults |
| 2 | EditMode, the two story-table PlayMode suites, then the core tests | All green, with `SANKOFA_ROOM` and `SANKOFA_PRESENT` unset | Read the checklist's row for that test. Rerun a timing failure alone before changing code. Never skip or disable a test to get green: fix it, or cut the change it covers and remove its tests with it |
| 3 | One whole gourd visit in the editor, pages turned by hand: choose the gourd, follow one side, hear the other, keep, leave, reopen | The kept telling reopens with both beads | This is the core route. Red here with step 2 green means the tests miss something: say so before fixing |
| 4 | Captures at standard and large text (checklist section 3) | The named frames are readable | Fix readability only where a frame shows a real problem |
| 5 | One untouched run with `SANKOFA_PRESENT=1`, timed with a clock | A whole sitting plays both sides with no dead end. Record the total and each wait | The waits are provisional. Change constants, not flow |
| 6 | Listening, with the entrant: the welcome, the whole gourd route, the cut water line, both stage sounds, the fire | The entrant has heard them | A bad stage sound is deleted, not replaced. A pronunciation or factual problem goes to the entrant |
| 7 | Simulator with hands: reach an heirloom, sweep, **Use my table**. The Meta XR Simulator for Windows must be installed first | Each either works or is written down as not working | See the cuts below |
| 8 | Signed release build from the frozen commit: `SankofaBuild.BuildRelease`, with the variables in [the manual steps](../release/manual-steps.md). Then read the merged manifest | No microphone permission in it; the APK's identity recorded in [the candidate record](../release/candidate.md) | Do not upload a build whose manifest differs from the checked-in one without saying why |

## Gates

The PM's go/no-go was 2 p.m. Eastern on November 15, with steps 1 to 3 done by noon. That hour passed before Unity had opened the project. These are the same gates moved later, proposed by the cloud session and the entrant's to confirm.

| When (Eastern) | Gate |
|---|---|
| Sunday 9 p.m. | Steps 1 to 3 green on `main` or a narrowly fixed candidate. If compile, saving or the core route is still red, stop fixing and switch to `6f40764` |
| Sunday, before stopping | Whatever the state, leave a second checkout of `6f40764` importing, so the fallback is ready on Monday |
| Monday noon | Steps 4 to 7 done, and behaviour frozen at one commit |
| Monday 6 p.m. | Step 8 built from that commit, uploaded to the Competition channel, and the invite link opened from a second account |
| Tuesday | Video and entry text checked against the frozen build, then submit. Wednesday morning is spare; the entry closes at 3 p.m. |

## Cutting instead of fixing

For an isolated failure, cut in the PM's order: a bad stage sound, unreliable room placement, an unreliable sweep, a disruptive automatic second turn. Keep the labelled pinch route and both sides wherever they pass. After each cut, rerun the affected test and the whole gourd route. [How to cut each change](../reviews/2026-11-14-assumptions-for-challenge.md#cutting-a-change) is one line per change.

Room placement has a rule of its own. The PM keeps it in the judged build only if placement and the return to the stand are seen working on a Quest, and no Quest is available. The entrant decides between removing **Use my table** entirely and keeping it in Options alone; either way it leaves the first page unless a headset session happens.

`6f40764` is not a free fallback. Its recorded APK is debug-signed and declares microphone permissions with voice off, so it needs a fresh signed build, a look at its merged manifest, and video and copy recut to what it does.

## What to write down

Put results on a new page beside this one: what ran, on which commit, pass or fail, how long it took, and what kind of evidence it is (software, desktop capture or simulator). None of it is device evidence. Update the candidate record only for a build that exists.

## Two sessions at once

The cloud session that wrote the November 14 changes is still open. It has no Unity and cannot record or listen. It keeps to the documents under `docs/release` and, only if the entrant approves new words for a telling, the engine-free story text with its tests. This session owns whatever Unity must compile or run, the builds and the results page. Fetch the other session's branch before touching a file it owns.
