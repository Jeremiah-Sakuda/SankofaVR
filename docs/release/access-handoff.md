# Pop-up theatre release handoff

November 8, 2026. **Start with the [manual steps](manual-steps.md)** (November 9): accounts, signing, upload, video and entry in order. No headset is available for this project; the XR Simulator path below is the primary capture path, not a fallback. Use the [current candidate](candidate.md), [ten-day plan](../product/delivery-plan.md#final-ten-days--november-818) and [Devpost draft](submission-draft.md). Earlier gallery footage and build IDs below are historical.

## One headset session: 30–45 minutes

1. Connect an authorized Quest 3 or 3S. Install the exact review APK and compare its version with the [candidate record](candidate.md). Sit, enable hands and set the controllers aside. Start with the service unconfigured. Use Options → Recenter or adjust the stand if needed.
2. Pick the gourd. Watch the stage rise. Follow the woman who brought less. At **Set the gourd down**, use the gourd or its labeled action and confirm it moves once. Finish, Keep, reopen the same telling, then Forget. Check that using a hand, not waiting for the cue, actually triggers the move.
3. Return to the gourd and choose the other perspective. Check that the chosen woman comes forward and the scene shows what accepting less costs her. Let the gourd cue resolve on its own this time. Try a brief pause/resume, the source panel and leaving hands-only.
4. If room fitting is included in the demo, try it on a real table and confirm what height or footprint changes. Also check denied/missing room data and recovery to the stand. Check text and reach in each tested setup.
5. Record device model, exact version, failed gestures, real frame rate over a sustained representative run and any discomfort. The judging rubric calls for at least 60 fps. Check the narration/ambience mix separately with an authorized listener; silent desktop tests establish neither pronunciation nor balance. If possible, ask a first-time visitor what changed when they chose a side before telling them the intended answer.

`python3 scripts/quest_evidence.py` lists attached devices. With an authorized device, pass `--serial` and the exact local `--apk` to record a fingerprint and device observations. It neither installs nor launches; compare the observed installed version to the candidate. Capture actual app frame-rate data separately.

## Capture one story well — target 2:30

Revised November 14 for the one-sitting flow. The organisers ask for real play that makes the hands-first design clear in the first thirty seconds, and judges need not watch past three minutes.

| Edit | Record from the final reviewed build |
|---|---|
| 0:00–0:10 | Open on the moment itself: a hand sweeps across the stage, it leans, folds, turns and rises on the other woman. Title over it: “One meeting. Two sides. Turn the story with your hand.” |
| 0:10–0:30 | The fire: “Come. Sit with me.” A hand warms at the fire and it flares. A hand closes on the gourd; the fire takes it and the paper scenery stands up. |
| 0:30–0:55 | The first page, then the documented page with its label. Choose a side at her figure. Hold on the fold, the turn and her coming forward. |
| 0:55–1:20 | Her side. A hand sets the gourd down between the women. Let one caption carry the point: she left a portion at home. |
| 1:20–1:50 | “This meeting has another side.” The sweep again, in full this time, and the other woman's side: the meal she had planned around the flour. Let one caption land. |
| 1:50–2:10 | The ending. Keep, and two beads swell onto the strand. Cut to a return: the griot names a side not yet heard. |
| 2:10–2:22 | Only if it works in the simulator's room: **Use my table**, the room appears, the theatre settles on the table; or a hand rests on the table and the theatre stands there. |
| 2:22–2:30 | Title, Enkare Nairobi around the 1850s, and the disclosure: documented context is marked, the people are imagined. “Captured in Meta XR Simulator.” |

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
