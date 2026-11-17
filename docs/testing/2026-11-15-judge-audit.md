# Painted river: judge-path audit and iteration

November 15, 2026. Windows, Unity 6000.3.25f1, Direct3D 11 desktop rendering. The entrant requested thorough tests, iteration and independent agent scoring. This record concerns the selected `RiverThresholdPainted` review experience and its shared StoryTable integration. It does not promote the river experiment to the default release or establish Quest acceptance.

The affected requirements are SK01/02/05/07/08/10/12/15/16/18: seated and hands-first access, meaningful personalisation, reveal boundaries, recovery, explicit keeping/replay, placement and evidence. Changes remain in presentation and application orchestration; story rules and realised story text are preserved. The [independent review](../reviews/2026-11-15-independent-judge-review.md) records the four official criteria and separates initial scores from reassessment.

## Reproduced problems and corrections

Five new regressions failed against the original implementation:

| Observed failure | Correction and observable evidence |
|---|---|
| Self-turning river had no reachable perspective choice IDs | The view owns whether the small stage is visible and keeps choices on the reading page when it is hidden. Both sides remain selectable with the same pointer/pinch adapter. The other-side prompt describes the visible controls. |
| A question about a household/gourd entered the river after choosing the water pot | Resolve and validate the prepared composition before selecting its presentation. The inverse water-question/gourd-selection case is also checked. |
| Moving the head and then raising the stand moved the river by 0.20976 m | A separate recenter event moves the surrounding scene; a height adjustment changes the stand. |
| Two Pause buttons were visible | Riverside and return-to-table controls supply one context action; Pause and comfort remain on the existing page controls. Caption size was preserved. |
| Use my table left the presentation in Riverside | The river withdraws before room or resting-hand placement begins. The intake backdrop also withdraws during room placement. |

Additional regression coverage rejects a stale provider response during a deferred prepared-story reveal and preserves the completed caption part when switching table/river presentation. The latter matters particularly when a completed voice clip prevents caption-only clock following from repairing the position on the next frame.

The painted figures now use the same analytical ground height as the generated riverbank. Read-only alpha inspection found their opaque feet within 1.4% of the six textures' lower edge; the earlier fixed card height placed them above the ground. No approved portrait was regenerated. The builder's ground formula is unchanged, now shared with the runtime placement. No saved comparison scene was regenerated or edited.

The asset audit also exposed two Windows portability problems: JSON reads used the system encoding, and Git converted the bundled OFL notice to CRLF. Reads/writes now specify UTF-8; the notice was restored to its recorded bytes and licence texts are protected from line-ending conversion. The provenance hash was not changed to accept the altered file.

## Executed verification

Logs and machine-readable results are under the ignored `artifacts/logs/2026-11-15-audit/` directory. Counts below describe separate runs; they must not be added as though reruns were different tests.

| Check | Result |
|---|---|
| Core, initial and after flow correction | 445 passed, zero failed in each run; `core-initial.trx`, `core-final.trx` |
| Python, initial and recheck | 81 passed, zero failed in each run; `python-tests.log`, `python-final.log` |
| Initial Unity EditMode | 49 passed; `editmode-initial.xml` |
| Entire initial Unity PlayMode assembly | 111 total: 100 passed, one failed, ten explicit opt-in skips; `playmode-initial.xml` |
| New judge regressions before correction | Five failed as recorded above; optional capture skipped; `judge-reproduction.xml` |
| Focused corrections plus Hearth check | Six passed, zero failed, optional capture skipped; `judge-fixes.xml` |
| Affected runtime suite and complete visit | 65 total: 60 passed, one test-setup failure, four opt-in skips; `runtime-final.xml`. The complete visit passed. The intake room test expected a headset-presentation button while configured for manual pages; its setup was corrected to use self-turning presentation. |
| Final painted regressions, caption preservation, grounding and full capture | Nine passed, zero failed/skipped; `judge-final.xml` |
| Final EditMode | 49 passed, zero failed; `editmode-final.xml` |
| Android review build | Succeeded, zero errors, one optional OpenXR input-polling recommendation; `android-river-build.log`. Manifest, v2 debug signature and all 1,171 ZIP entry CRCs verified. |
| Interactive Meta XR Simulator | Selected scene launched muted and XR render textures initialized. Windows Security prompt blocks screen interaction; no interactive pass claimed. |
| Legacy input/narration integrity | 31 declared assets and 92 narration lines; zero integrity problems after the encoding/line-ending corrections |
| Selected river asset inventory | All 13 images have source-manifest entries; current hashes recorded as a new audit baseline, not independent provenance or rights clearance |
| NuGet advisory query | No vulnerable packages reported for the test project's resolved direct/transitive packages; does not cover Unity/Meta SDK advisories |
| Local credential-pattern scan | 1,715 tracked text files checked; no matches for the selected private-key, GitHub-token and Google-key patterns. No values retained; not an exhaustive security assessment. |

The original failed test was `HearthVisitTests.ExchangePosesAndBothSupportingHandsSurvivePauseAndRecenter`: a hand rotation differed by 0.759 degrees against a 0.5-degree limit. Its coroutine sampled before the already-pending animation/LateUpdate finished. It now captures the completed pose after one frame and asserts that the exchange clock remains frozen both immediately and throughout the pause. The 3 mm/0.5-degree pause and 8 mm recenter tolerances were unchanged. Hearth runtime and scenes were not changed; the focused recheck passed.

The initial ten skips comprise optional captures and explicitly configured HTTP/live-service cases. Ordinary tests used bundled/synthetic data; this audit did not invoke a live model provider. The subsequently added complete-visit capture is separately executed, not a claimed pass for those earlier skipped cases.

## Complete visit and build identity

The full recording is `artifacts/judge-visit/complete-painted-visit.mp4`, with seven PNG milestones and `milestones.txt`: **4,235 frames, 176.458 seconds**, fully decoded by FFmpeg without errors. It uses the actual selected Unity scene, synthetic single-pointer pinch/release input, ordinary caption/stage timing, both deliberate perspectives, Pause/Resume, explicit Keep and exact reopening. It does not seek story time. The fixed 24 fps simulation clock is a capture setting, not measured frame rate. It is a silent mono desktop render, not simulator or headset footage. Stills are sampled after presentation LateUpdates so they do not display a transient mid-frame composition.

The separate review build entry is [SankofaBuild.BuildRiverReview](../../unity/Assets/Sankofa/Editor/SankofaBuild.cs), producing `artifacts/Sankofa-RiverPainted-Review.apk`: **76,345,496 bytes (72.8 MiB)**, package `com.sankofa.quest`, version `0.1.0+e48b1e8-dirty` / code 1, ARM64, minimum SDK 32 and target SDK 34. Its SHA-256 is `a5b5aa472d7ba514fa71310a59d1e1a9909604c7435fb80e2da230d777334b8f`; `apk-receipt.json`, `apk-badging.txt` and `apk-signature.txt` retain the checks. All 1,171 archive entries passed CRC validation and the selected forbidden-path scan found no LocalOnly, private reference or competition-brief paths. It uses a valid Android Debug v2 signature. This is a review artifact from the modified working tree, not an installed or release-signed candidate. The [Unity commands](../../unity/README.md#painted-versus-3d-river-cast) distinguish it from the default/signed StoryTable release and any Competition-channel upload.

The interactive Meta XR Simulator launch uses the selected painted scene and `-noaudio`. A Windows Security firewall request for Node.js JavaScript Runtime blocks the visible screen, so the user was asked to handle it; no actions were taken on that permission request. The launch log contains an editor search-index startup `ArgumentOutOfRangeException` with a `UnityEditor.Search.SearchDatabase` stack, OpenXR/Meta feature-support diagnostics, and an unstacked already-disposed `CancellationTokenSource` message whose origin is not established. These do not replace interaction evidence or justify describing the launch log as error-free. Automated complete-visit evidence above remains separate.

All three independent agent reassessments are **72/100**, from engineering 62, experience 65 and presentation 66. Their allocations differ; the [review record](../reviews/2026-11-15-independent-judge-review.md) preserves initial observations, fixes and confidence limits. The page still occupies much of the foreground. The five-action self-turning capture and earlier eleven-action manual-mode image are different configurations, so that entire reduction cannot be attributed to the correction; removal of duplicate Pause is directly tested.

The provisional intro music remains editor-only and is absent from scene dependencies. A silent capture cannot establish its rights or the distributable build's sound. No AI music was generated.

## Remaining acceptance

No device was attached when the Android tools were checked. Quest frame rate, stereo appearance, physical hands/reach, room-placement behaviour, sustained comfort, listening/pronunciation, cultural review and unfamiliar-visitor comprehension remain unverified. Agent scores are review estimates, not participant results or official judging evidence. Final release selection, rights clearance, signing, judge access and matching submission materials remain separate delivery gates.
