# Pop-up theatre release handoff

Updated November 15, 2026. **Start with the [manual steps](manual-steps.md)**: accounts, signing, upload, video and entry in order. The water telling now leads the submission and demo; the gourd remains an additional interaction check. No headset is available for this project; the XR Simulator path below is the primary capture path. Use the [current candidate](candidate.md), [delivery plan](../product/delivery-plan.md#final-ten-days--november-818) and [Devpost draft](submission-draft.md). Earlier gallery footage and build IDs below are historical.

## One headset session: 30–45 minutes

1. Connect an authorized Quest 3 or 3S. Install the exact review APK and compare its version with the [candidate record](candidate.md). Sit, enable hands and set the controllers aside. Start with the service unconfigured. Use Options → Recenter or adjust the stand if needed.
2. Pick the water pot. Follow **Stay with the one who remembers**, then sweep to **Walk with the younger traveller** when the other side is offered. Check the selected figure comes forward and the final sentence is heard or read in full. Keep, reopen, then Forget. Try pause/resume, the source panel and leaving hands-only.
3. Separately pick the gourd and follow the woman who brought less. At **Set the gourd down**, use the gourd or its labelled action and confirm it moves once. Check that the hand action, not waiting for the cue, triggers the move. Hear the other side and let its gourd cue resolve automatically. This additional check retains coverage of the interaction absent from the water telling.
4. If room fitting is included in the demo, try it on a real table and confirm what height or footprint changes. Also check denied/missing room data and recovery to the stand. Check text and reach in each tested setup.
5. Record device model, exact version, failed gestures, real frame rate over a sustained representative run and any discomfort. The judging rubric calls for at least 60 fps. Check the narration/ambience mix separately with an authorized listener; silent desktop tests establish neither pronunciation nor balance. If possible, ask a first-time visitor what changed when they chose a side before telling them the intended answer.

`python3 scripts/quest_evidence.py` lists attached devices. With an authorized device, pass `--serial` and the exact local `--apk` to record a fingerprint and device observations. It neither installs nor launches; compare the observed installed version to the candidate. Capture actual app frame-rate data separately.

## Why the water story leads

**Lead with the water telling, “A name beside the water.”** The entrant asked for the stronger emotional payoff after the framing stress test. This is an editorial choice based on the [authored stories](../../unity/Assets/Sankofa/Core/Personal/BundledPersonalStoryComposer.cs), [staging](../../unity/Assets/Sankofa/Content/personal-table-staging.json) and [recording manifest](../../unity/Assets/Sankofa/Content/personal-table.narration.json). It is not a listening review or a measured audience preference.

| Test | Water | Gourd |
|---|---|---|
| Connection to Sankofa's purpose | Someone wants to pass on what a place meant to her despite uncertainty; the listener later carries the memory herself | A household promise meets another household's needs; a strong story of generosity, with a less direct connection to inherited memory |
| Change between perspectives | The older woman's admission becomes the younger traveller's future responsibility: she too will remember imperfectly | The opening already explains the portion left at home. The other side adds the cost of accepting less, not a new explanation of the shortfall |
| Emotional ending | The younger traveller becomes the person who is no longer quite sure where it was; this connects receiving a story with eventually carrying it | The women agree to a smaller exchange; the second side makes that generosity more costly and understandable |
| Visible participation | Select the water pot, choose a person, turn the stage and keep the telling | The same structure, plus the clearer physical action of setting down the gourd; gourd wins on the extra prop interaction |
| Risk in a short edit | Quiet and dependent on words; cutting the final sentence or explaining it early would remove the payoff | Easy to establish the conflict, but the earlier pitch falsely treated an opening fact as a later revelation |

Water wins for this submission's emotional promise. Preserve its complete final sentence and give it room to land. Keep the gourd in the build and the judge's optional checks; do not insert its prop action into the water footage or imply the water telling has it. The first water side already describes the traveller listening, so the reveal is her future role as the person carrying the memory. The characters are not established as relatives. The naming passage supplies bounded historical context, not proof of their meeting or of the exact riverbank.

## Capture one story well — target 2:30

Revised November 15 around the water telling and the stronger promise: everyday lives behind history, explored through an explicitly imagined encounter. The [official judging guidance](https://start-developer-competition-26.devpost.com/details/judging) asks for honest gameplay led by the story and the project's distinctiveness. Judges need not watch past three minutes under the [rules](https://start-developer-competition-26.devpost.com/rules). Showing an actual hand selection within thirty seconds is our editorial choice. The main stage turn follows the first perspective so its narrative purpose is clear.

| Edit | Actual gameplay and audio direction |
|---|---|
| 0:00–0:12 | Fire, a hand selecting **A water pot / Place & water**, and the stage rising. Pitch voice: “A place can matter to you before you know its past. Sankofa offers a story to begin with.” Small title: **Sankofa — Everyday lives behind history**. Context caption: **Three imagined tellings · Enkare Nairobi, 1850s**. |
| 0:12–0:28 | Opening scene and actual captions. Pitch voice establishes only: “A woman brings a younger traveller to a riverbank she remembers.” Then let the prepared narration take over at “She had promised to show a place. Now she is no longer sure she has found it.” Show an edit at the jump; preserve the uncertainty. |
| 0:28–0:40 | The actual **Documented context** page and its short naming passage, then select **Stay with the one who remembers**. No pitch voice. Keep the sourced label legible and distinguish it from the imagined scene that follows. |
| 0:40–1:00 | Older woman's side, her figure forward. Use the prepared recording from “She chooses not to pretend” through “makes room for the traveller beside her.” Let her admission and invitation breathe. The full page's later modern-river coda is omitted from this short edit, not retold as part of the 1850s. |
| 1:00–1:12 | Clearly cut to the actual offer of the other side. Pitch voice: “Now turn toward the younger traveller.” Show the hand sweep and the fold, turn and rise without cutting between input and response. If the recorded input is a plaque selection, say “Now choose the younger traveller” and show the selection. Never call an automatic turn or a pinch a sweep. |
| 1:12–1:50 | Younger traveller's side, her figure forward and actual captions. Use the prepared recording from “Instead she chooses a different question” through the end of the chapter. No pitch voice or feature overlays. Keep **“What did it sound like?”** and the full sentence beginning **“Years from now”**. Her future uncertainty is the payoff. |
| 1:50–2:00 | Hold the stage and the last caption for at least three seconds after the sentence ends. Then pitch voice: “The meeting stays the same. What you understand about it changes.” Do not cut to a menu while the final story line is still speaking. |
| 2:00–2:14 | Ending, deliberate **Keep this story**, two beads, then a clearly cut return and **Reopen kept story**. Pitch voice: “Keep the telling to return to its exact pages. Another story is waiting by the fire.” Beads mark the visit; no claim of ongoing habit or unlimited content. |
| 2:14–2:22 | Show the stand height adjustment in Options. Pitch voice: “The prepared visit works offline, seated, on an adjustable stand.” Only substitute a verified **Use my table** shot if it clearly shows fitting changing the placement; use the line “Use your own table, or keep the adjustable stand.” |
| 2:22–2:30 | Return to the actual stage and product title. Pitch voice: “Sankofa. For the stories you never got to hear.” Small closing caption: **Documented context · Imagined people and meetings**. |

Keep **Meta XR Simulator footage · Edited visit** visible from the first simulator frame. Cuts shorten a visit; they must not imply that omitted story pages or waits disappear in the application. Do not relabel synthetic review-camera PNGs as simulator footage. Include English captions for the pitch; the application's captions stay readable. Keep pitch narration separate from the prepared story audio, with no overlapping speech. Do not use generated video or fabricated family photographs to supply emotion.

### Audio and pacing cues

The existing manifest lists water recordings of 39.32 seconds (opening), 9.072 seconds (documented naming), 45.24 seconds (older woman's side) and 59.08 seconds (younger traveller). Their combined 152.712 seconds already exceed the 150-second target before welcome, hand actions or closing. This is necessarily an excerpted demo. Those are file metadata, not a listening or timing acceptance result; locate edit points by ear in the reviewed recording, not by estimated word offsets.

| Chapter and local file | Required excerpt |
|---|---|
| `place-water.listening` — `p-422bf80a8695a2d8.mp3` | “She had promised to show a place. Now she is no longer sure she has found it.” |
| `place-water.name` — `p-145300bc4d3cdf9c.mp3` | The full short naming passage; check the last word of this previously cut clip |
| `place-water.return` — `p-76c08d22570c2a3c.mp3` | “She chooses not to pretend. 'I remember the water. I may have the bend wrong.' She lifts the bundle and makes room for the traveller beside her.” |
| `place-water.traveller` — `p-e92d128aa0306221.mp3` | “Instead she chooses a different question. 'What did it sound like?' The older woman closes her eyes to answer. Years from now, the traveller will remember that sound better than any bend, and she will be the one who is not quite sure where it was.” |

Files are under `unity/Assets/Sankofa/Resources/Narration/personal-table/`; the [manifest](../../unity/Assets/Sankofa/Content/personal-table.narration.json) records the exact text and assets. The water choice does not authorize regeneration or rewriting of the story or its recordings. No sound was played during this editorial comparison. Listen to the selected excerpts and capture a complete water visit before cutting. If a reviewed excerpt needs longer than its slot, drop the optional stand shot and shorten pitch narration first; retain the final story sentence, the hand action and the three-second pause. Keep the finished edit under three minutes. Do not stretch or accelerate the voice to fit.

The adaptation sentence for a longer text field or optional card is: “The original web Sankofa invited a personal question; this Quest adaptation gives the telling a physical stage, hand-driven perspective changes and a complete prepared offline visit.” The planned demonstration shows that adapted visit; footage and audience acceptance are still to be obtained. Hosted generation, unrestricted heritage coverage and freeform conversation are not part of this edit.

Record actual Quest gameplay where possible. If needed, the [official FAQ](https://start-developer-competition-26.devpost.com/details/faqs) allows XR Simulator footage. Use Unity's **Sankofa → Play Personal Story Table in XR Simulator** with the muted launch command in the [Unity guide](../../unity/README.md#personal-story-table). Capture the prepared route, label the simulator footage and do not infer real-hand reliability from it. October 30 desktop stills and the fixed-cadence motion study are editing references, not submission footage. Capture without external participants' identifiable faces, voices, or personal details. Keep speech legible via captions. No local sound playback or public upload is authorized by this checklist.

## Dashboard and Devpost

- Confirm active Start membership and the app owner now. Check the package, signing identity and next accepted version code against the actual dashboard before producing the upload build. A local debug-signed APK is a review candidate until accepted by that channel.
- Upload the exact release APK to the new **Competition** channel and use its judge-access link. Have a separate authorized account install and cold-launch through that link. A cable install does not verify that access.
- Use the active Devpost draft, **Entertainment / Adapted or Significantly Updated** proposed, and a public YouTube or Vimeo video under three minutes. Complete the confirmed launch plan and verify both links before submitting. Preserve access through the winner announcement. The [official rules](https://start-developer-competition-26.devpost.com/rules) control if dashboard guidance differs.
- Target November 16, with the November 18 **3 p.m. Eastern** hard close. No channel upload, publication or entry submission is implied.

---

# Judge access and final demonstration handoff

Prepared October 14. The [submission draft](submission-draft.md) supplies copy; the [candidate record](candidate.md) supplies source/build evidence. This handoff is prepared, not executed. No account, channel, judge installation or publication has been verified.

## Access inputs and concrete upload target

Confirm Meta Start membership/eligibility, ownership of the selected Meta application, its app ID, package name and intended release date. Do not put account credentials, signing keys or private dashboard records in Git. The local review APK is `artifacts/Sankofa-LivingGallery.apk`, Android package `com.sankofa.quest`; its exact source/version/hash is in the candidate record. It is a review artifact until device, rights and release-access gates pass.

The [official FAQ](https://start-developer-competition-26.devpost.com/details/faqs) and [rules](https://start-developer-competition-26.devpost.com/rules), checked October 14, call for the Unity APK in a channel named **Competition**. In the selected application's dashboard, create/use that channel, upload the verified candidate and record the resulting build identity and judge access. Check existing version codes before upload; the current local review code is 1 and must not be assumed acceptable if that code is already present. Verify signing/application identity against the actual account's requirements instead of generating or replacing signing material blindly.

Have an authorized tester install through the intended access path on a separate Quest. Record installation, cold launch, permission behavior, fully hands-only entry and story completion, and whether any unstated account/server requirement appears. Merely seeing a successful upload is not independent judge-access verification. Retain the exact APK hash, installed version, channel/build and concise access instructions locally, then place the necessary approved identifiers in the final submission.

## Final video shot plan — 2 minutes 35 seconds

| Time | Actual footage and point |
|---|---|
| 0:00–0:12 | Gallery, artwork title and purpose; short fictional/context disclosure |
| 0:12–0:32 | Real hand chooses the painting; frame unfolds while the seated view stays stationary |
| 0:32–0:57 | Gourd visibly begins in the painted palm, crosses into reach, and is received by a tracked hand |
| 0:57–1:17 | Flour crosses back into the encounter; show what actually changes |
| 1:17–1:37 | One memory reinterprets the shortfall; show the women's agreement, not a guest deciding it |
| 1:37–1:52 | Brief accepted/passive hospitality comparison, clearly labelled as separate visits |
| 1:52–2:12 | Same painting returns; deliberate Keep and meaningful other-memory choice |
| 2:12–2:27 | Modern continuation and the water/thread connecting the encounter to today |
| 2:27–2:35 | Product name, current capability and one invitation to revisit |

Use actual Quest or allowed emulator footage for the final demonstration, as the official rules specify. The current **no simulator / no sound playback** instruction remains in force; obtaining footage or listening later requires an appropriate change in the session's constraints and available hardware. The local 41-second desktop/synthetic preview is a software review draft, not qualifying footage. Never represent synthetic input as successful real-hand tracking, a selected excerpt as the whole visit, or desktop frame rate as device performance.

Keep the final video under three minutes, English/captioned as required, and publish it to the accepted public video service only when the concrete edit and rights/access evidence are ready and publication is authorized. No generated music, unreviewed predecessor music, invented participant quotes, community endorsements or claimed eligibility belongs in the edit. The current silent draft contains no audio stream.

## Before filling the final entry

Resolve the adapted-division lineage and baseline, actual launch date, public demo URL, real device test findings and cultural/pronunciation/rights status. Use the working-feature disclosure from the submission draft: authored English, two memories, explicit hand/passive consequences, local save/replay and a modern coda. Runtime generation, live AI conversation and additional languages remain future product work.
