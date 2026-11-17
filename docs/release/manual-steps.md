# Your manual steps to submit

November 9, 2026; revised November 14. These are the steps only the entrant can do: accounts, signing keys, uploads, the video and the entry. Where the official [rules](https://start-developer-competition-26.devpost.com/rules) or [FAQ](https://start-developer-competition-26.devpost.com/details/faqs) say something specific, it is quoted or linked. Meta's dashboard wording changes from time to time, so the menu names below may differ slightly. Never put passwords, keystores or account details in this repository.

Hard close: **Wednesday November 18, 3 p.m. Eastern / noon Pacific.** After that nothing may change: not the entry, the build, the video or the link. Internal target: Monday November 16.

**First, on November 15:** the November 14 changes were written without Unity. Before anything below, have the Unity session work through the [verification list](../testing/2026-11-14-unity-verification.md): compile, run the tests, look at the captures and listen. Build and record only after that.

## 1. Confirm eligibility and Start membership (today)

The rules require a Meta account with developer access, and the person submitting must be "a member of the Meta VR Start Program by the time of submission".

1. Sign in at [developers.meta.com](https://developers.meta.com/horizon/) with the Meta account you will submit from. If it asks you to set up a developer organization or verify the account, finish that first.
2. Check whether you are already a Start member. Signs that you are: an acceptance email from the Start program, access to Start member resources or the Start community, or the program shown on your developer profile. If you can't find any of these, apply through the "apply now" link on the [competition page](https://start-developer-competition-26.devpost.com/). The page estimates five minutes for the application. Acceptance is not instant, so apply today.
3. On Devpost, open the [competition page](https://start-developer-competition-26.devpost.com/) and click **Join Hackathon** if you have not already.
4. Re-read rules §2 (eligibility) and the Meta-funding rule in §4 against your situation.

## 2. Create the app and the Competition channel (today or tomorrow)

1. In the [developer dashboard](https://developers.meta.com/horizon/manage/), create a new app for Meta Quest (Horizon Store) named **Sankofa**, or open it if it already exists. Note its **App ID**.
2. The build's Android package name is `com.sankofa.quest`. The first upload ties that name to this app permanently. Change it now if you want a different one, and tell me so I can rebuild.
3. Open the app's **Distribution → Release channels** page and create a new channel named exactly **Competition**, as the rules require.

## 3. Make the release signing key (tomorrow)

The dashboard rejects debug-signed APKs, and the key you sign the first upload with must sign every later update. Keep it, and its password, somewhere safe outside this repository, such as a password manager plus a backup copy.

Run this once, choosing your own passwords when asked. The tool ships with Unity:

```bash
"/Applications/Unity/Hub/Editor/6000.3.25f1/PlaybackEngines/AndroidPlayer/OpenJDK/bin/keytool" -genkeypair -v -keystore ~/sankofa-release.keystore -alias sankofa -keyalg RSA -keysize 2048 -validity 10000
```

Then build the signed APK from a clean checkout. The version code must be higher than any build already uploaded; use `2` for the first upload, then `3`, `4` and so on. Close the Unity editor first. Put the passwords in your shell's environment, not in a file in the repository:

```bash
SANKOFA_KEYSTORE="$HOME/sankofa-release.keystore" SANKOFA_KEY_ALIAS=sankofa SANKOFA_KEYSTORE_PASS='your-password' SANKOFA_VERSION_CODE=2 "/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity" -batchmode -noaudio -quit -projectPath "$PWD/unity" -buildTarget Android -executeMethod Sankofa.EditorTools.SankofaBuild.BuildRelease -logFile "$PWD/artifacts/logs/release-build.log"
```

The output is `artifacts/Sankofa-StoryTable-release.apk`. The build refuses to run from uncommitted changes, and it leaves no key details in the project. Tell me when it finishes, and I'll verify the signature and record the hash.

## 4. Upload and create the judge link (Monday Nov 16)

1. Upload `Sankofa-StoryTable-release.apk` to the **Competition** channel. The dashboard's release channel page has an upload option; Meta Quest Developer Hub and Meta's `ovr-platform-util` command-line tool can also upload.
2. If the upload is rejected, copy the exact message to me. Common causes are the version code or the signing key.
3. Create the channel's invite link, which the rules call an "Invite URL". Open it from a second Meta account to confirm it grants access; the organisers call this the single most valuable check. Without a headset that confirms access, not installation.
4. Do not change or delete this build or channel after the deadline. It must stay available until the winners are announced.

## 5. Listen once (Sunday Nov 15, about 30 minutes)

Nobody has listened to the narration yet. There are 22 current clips for 12 chapters and 11 lines; two documented pages now share one recording. On November 14 they were brought to one loudness without re-encoding, and the water telling's documented page became a cut of its longer recording: listen to the end of `p-145300bc4d3cdf9c.mp3` for a clean last word. Two new short sounds, `stage-fold.wav` and `stage-rise.wav` in `unity/Assets/Sankofa/Resources/Sounds/`, play as the stage folds and rises; they are cuts of the tent recording chosen without ears, so play both and say whether they stay. `personal-table.narration.json` maps each file to its chapter (`chapters`) or line (`lines`); ignore `archived_chapters`. Play them from `unity/Assets/Sankofa/Resources/Narration/personal-table/` and note any mispronounced names, odd pacing or clipping. Listen to the welcome first: `welcome.come`, `welcome.fires`, `welcome.imagined`, `welcome.invitation`. It is the first thing a judge hears. Then play the brook and night recordings together at the same time and judge the balance. Tell me what is wrong; I fix it in the build, but new recordings need your approval.

## 6. Record the video (Sunday–Monday Nov 15–16)

The rules accept XR Simulator footage and require a public YouTube or Vimeo demonstration. They recommend less than three minutes, and judges need not watch beyond that. Our edit targets 2:30.

1. Start the simulator build with the headset presentation on: launch Unity with `SANKOFA_PRESENT=1` in its environment, then choose **Sankofa → Play Personal Story Table in XR Simulator**. For the recording session, leave sound on, because the narration carries the story. Add `SANKOFA_ROOM=1` to record **Use my table** in one of the simulator's synthetic rooms; that path has never run, so try it before planning a shot around it.
2. Record the actual simulator window and application audio using a recorder available on the recording machine. The current workspace is on Windows; the earlier macOS capture directions do not apply. First inspect a short test recording for readable captions, captured narration and unrelated desktop content. Recorder installation and audio routing must be checked on that machine rather than assumed from the earlier setup.
3. Follow the revised [water-story script](access-handoff.md#capture-one-story-well--target-230): select the water pot, establish the uncertain riverbank, choose the older woman's side, turn toward the younger traveller, preserve her final sentence and a pause, then Keep and reopen. The gourd action is a separate judge check and is not part of this telling. Keep **Meta XR Simulator footage · Edited visit** visible. The sweep needs the simulator's hand input, not its controllers: bring a hand in front of the stage first (the fire then waits up to twenty seconds more before turning the stage itself), then move it sideways about a hand's length in under a second. Practise it before the take. If the simulator's hand cannot be moved that fast, film the plaque over the other woman being pinched instead and use the script's selection wording. Do not caption it as a sweep. Capture the whole visit before making the documented excerpts; the narration alone is longer than the target edit.
4. Upload to YouTube or Vimeo as **Public** (not Unlisted), and check the link while signed out.

I can trim the clip and add titles with `ffmpeg` if you give me the raw recording.

## 7. Fill in and submit the Devpost entry (Monday Nov 16)

1. On the competition page choose **Enter a submission**. This creates a project draft you can save and return to.
2. Copy the text from the [submission draft](submission-draft.md): name, tagline (140 characters), inspiration, what it does, how we built it, what's next, what changed from the original Sankofa, and the optional paragraph on hand interactions. Paste the judge walkthrough from the [testing guide](../product/testing-guide.md) as the testing instructions.
3. Choose **Track: Entertainment** and **Division: Adapted / Significantly Updated Experience**. Adapted entries must include "a summary description of the new features/capabilities added"; the "What changed" section covers this.
4. Add the **Competition channel invite link**, the **public video link** and a **target launch date**. The launch date is your decision; it is required unless the app is already on the store.
5. Add screenshots from `docs/media/` (for example `story-table-perspective-turn.png`), and paste or link the [changelog](changelog.md): the division recommends one.
6. Submit, then reopen the entry and check every link while signed out. A screenshot of the form does not count as proof of entry under the rules.

## Decisions only you can make

| Decision | Why it matters | Needed by |
|---|---|---|
| Keep `com.sankofa.quest` as the package name? | Permanent after the first upload | Before step 4 |
| Cultural review: send the review packets, or ship with the draft label | The label appears on the pages and in the video | Thu Nov 12 |
| Target launch date | Required field on the entry | Mon Nov 16 |
| Say in the entry that no headset was available? | The build has never run on a Quest. The entry must not imply device testing; whether to state it outright is yours | Mon Nov 16 |
| Borrow a Quest 3 or 3S for ten minutes | Launch, **Use my table** and one sweep are untested on hardware, and judges test on hardware | Before the upload |
| Provider terms: confirm your OpenAI (images) and Google (narration) account terms permit this use | Rights to the shipped art and voice | Fri Nov 13 |
