using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Sankofa.Core.Personal;
using Sankofa.Runtime.Personal;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Sankofa.Tests
{
    /// <summary>Desktop software evidence only: rendering contracts and actual hand/pointer panel actions.</summary>
    public sealed class PersonalStoryTableTests
    {
        StoryTableView view;

        [UnitySetUp]
        public IEnumerator LoadTable()
        {
            LogAssert.ignoreFailingMessages = true; // Meta logs unsupported native calls without a connected headset.
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/Sankofa/Scenes/StoryTable.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene("StoryTable");
#endif
            yield return null;
            view = UnityEngine.Object.FindFirstObjectByType<StoryTableView>();
            Assert.That(view, Is.Not.Null);
            // The application integration has separate state tests; isolate this view's accepted callback contract.
            foreach (var app in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Where(x => x.GetType().Name == "PersonalStoryApp")) { app.enabled = false; app.StopAllCoroutines(); }
            view.FitToSurface(new Vector3(0, 0.68f, 0.72f), Quaternion.identity, 1.25f, 0.72f);
            view.ShowIntake();
            yield return null;
        }

        [UnityTest]
        public IEnumerator PreparedAndOnlineStartsAreDistinctAndConsentIsDeliberate()
        {
            var prepared = 0; var online = 0;
            view.StartLocalRequested = () => prepared++;
            view.StartRequested = () => online++;
            var start = view.Panels.Single(p => p.name == "Start choices");
            start.PressFromPointer("local"); yield return null;
            Assert.That(prepared, Is.EqualTo(1)); Assert.That(online, Is.Zero);
            yield return new WaitForSecondsRealtime(0.32f);
            start.PressFromPointer("create"); yield return null;
            Assert.That(online, Is.Zero, "opening the online notice does not send visitor material");
            var consent = view.Panels.Single(p => p.name == "Online consent actions");
            consent.PressFromPointer("create"); yield return null;
            Assert.That(online, Is.EqualTo(1));
            if (Environment.GetEnvironmentVariable("SANKOFA_CAPTURE") == "1") { view.ShowIntake(); Capture("01-story-table-intake"); }
        }

        [UnityTest]
        public IEnumerator ChapterReplacementKeepsOneFocalImageAndTruthfulCapability()
        {
            view.RenderStory("A question about home", "Enkare Nairobi • around the 1850s", "Prepared story • works offline");
            view.RenderChapter(new StoryTableChapterViewData
            {
                Index = 1, Count = 4, Title = "An exchange remembered", Caption = "A visitor account is a starting question, not proof of ancestry.",
                ArtKey = "cast-atlas", ImageLabel = "Illustrated reconstruction", PreviousPageTitles = new[] { "The river" },
                Questions = new[] { "What is documented?", "What is imagined?" }
            });
            view.SetControlState(true, true, true, false);
            yield return null;
            Assert.That(view.Caption, Does.Contain("not proof of ancestry"));
            Assert.That(view.CapabilityLabel, Is.EqualTo("Prepared story • works offline"));
            Assert.That(view.Stage.ChapterId, Is.EqualTo("An exchange remembered"), "a chapter without staging is still performed, holding its arrangement");
            var question = ""; view.QuestionSelected = id => question = id;
            view.Panels.Single(p => p.name == "Questions beside the story").PressFromPointer("1"); yield return null;
            Assert.That(question, Is.EqualTo("1"));
            if (Environment.GetEnvironmentVariable("SANKOFA_CAPTURE") == "1") Capture("02-story-table-reading");
        }

        [UnityTest]
        public IEnumerator PerspectiveChoicesUseTheSidePanelAndNeverCountAsQuestions()
        {
            var chosen = ""; var asked = "";
            view.ChoiceSelected = id => chosen = id; view.QuestionSelected = id => asked = id;
            view.RenderStory("Where paths meet", "Enkare Nairobi • around the 1850s", "Prepared story • works offline");
            view.RenderChapter(new StoryTableChapterViewData
            {
                Index = 1, Count = 3, Title = "A region of overlapping lives", Caption = "Documented regional context.",
                ArtKey = "scenery-atlas", ImageLabel = "Documented context • sources available",
                Questions = new[] { "What supports the regional context?" },
                Choices = new[] { "Stay with the one who waits", "Find the one who is late" },
            });
            view.SetControlState(true, true, true, false);
            yield return null;
            var side = view.Panels.Single(p => p.name == "Questions beside the story");
            Assert.That(side.Ids, Is.EqualTo(new[] { "choice:0", "choice:1" }));
            Assert.That(side.Hidden, Is.False);
            Assert.That(view.Caption, Does.StartWith("Documented regional context.").And.EndWith(StoryTableView.DefaultChoicePrompt));
            Assert.That(view.Panels.Single(p => p.name == "Turn a page").Ids, Does.Contain("next").And.Contain("ask"), "context stays available and continuing remains possible");
            if (Environment.GetEnvironmentVariable("SANKOFA_CAPTURE") == "1") Capture("09-perspective-choice");
            side.PressFromPointer("choice:1"); yield return null;
            Assert.That(chosen, Is.EqualTo("1")); Assert.That(asked, Is.Empty);
            view.RenderChapter(new StoryTableChapterViewData
            {
                Index = 2, Count = 3, Title = "A promise still walking", Caption = "An imagined ending.", ArtKey = "scenery-atlas",
                ImageLabel = "Imagined scene", Questions = new[] { "What could I ask?" },
            });
            yield return null;
            Assert.That(side.Ids, Is.EqualTo(new[] { "0" }));
            Assert.That(view.Caption, Is.EqualTo("An imagined ending."));
        }

        [UnityTest]
        public IEnumerator PopUpStageRisesTurnsToTheChosenSideAndHoldsStillWhenAsked()
        {
            var stage = view.Stage;
            Assert.That(stage, Is.Not.Null, "the table performs on its pop-up stage");
            var capture = Environment.GetEnvironmentVariable("SANKOFA_CAPTURE") == "1";
            const string imagined = "Imagined scene";
            view.SetReducedMotion(false);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(stage.Rise, Is.Zero, "between tellings the scene lies flat");
            Assert.That(view.TableRoot.GetComponentsInChildren<Transform>().Count(t => t.name.StartsWith("Heirloom ")), Is.EqualTo(3), "three heirlooms wait to be chosen");
            if (capture) Capture("11-stage-intake", 72);

            var load = new StoryTableChapterViewData { Id = "trade-household.load", Index = 0, Count = 3, Title = "A household's small decisions", Caption = "Imagined scene: Come closer to the fire. A woman tips her flour gourd and listens. Less than she thought.", ArtKey = "cast-atlas", ImageLabel = imagined };
            view.RenderChapter(load);
            for (var deadline = Time.realtimeSinceStartup + 3f; stage.Rise < 1 && Time.realtimeSinceStartup < deadline;) yield return null;
            Assert.That(stage.Rise, Is.EqualTo(1));
            Assert.That(stage.FigureShown("wambui"), Is.True); Assert.That(stage.FigureShown("naserian"), Is.False, "she has not reached the meeting yet");
            Assert.That(stage.GourdPlace, Is.EqualTo(TableStagingPlace.With)); Assert.That(stage.GourdHolder, Is.EqualTo("wambui"));
            stage.Seek(0.2f); yield return null;
            Assert.That(stage.FigurePose("wambui"), Is.EqualTo(1), "she tips her gourd when the words say so");
            yield return new WaitForSecondsRealtime(0.6f);
            if (capture) { Capture("12-stage-first-page-72", 72); Capture("12-stage-first-page-82"); }
            view.RenderChapter(load);
            Assert.That(stage.Progress, Is.GreaterThanOrEqualTo(0.2f), "returning to the same page keeps its performance");

            var vessel = new StoryTableChapterViewData
            {
                Id = "trade-household.vessel", Index = 1, Count = 3, Title = "A vessel with a source", Caption = "A National Museums of Kenya exhibit describes a Maasai gourd.",
                ArtKey = "cast-atlas", ImageLabel = "Documented context • sources available", Questions = new[] { "What does the museum example tell us?" },
                Choices = new[] { "Follow the one who brought less", "Wait with the one who expected more" },
                ChoiceIds = new[] { "trade-household.follow.less", "trade-household.follow.more" },
            };
            view.RenderChapter(vessel); view.SetControlState(true, true, true, false);
            stage.Seek(0.1f); yield return null; yield return null;
            var wambui = view.Panels.Single(p => p.name == "Follow wambui");
            var naserian = view.Panels.Single(p => p.name == "Follow naserian");
            Assert.That(wambui.Ids, Is.EqualTo(new[] { "choice:0" }));
            Assert.That(naserian.Ids, Is.EqualTo(new[] { "choice:1" }));
            Assert.That(naserian.Hidden, Is.False, "each perspective stands at the feet of the figure it follows");
            yield return new WaitForSecondsRealtime(0.7f);
            if (capture) { Capture("13-stage-choice-72", 72); Capture("13-stage-choice-82"); }
            if (capture)
            {
                // The headset presentation: pages turn themselves, so the page carries no turning buttons or second choice.
                view.SetSelfTurning(true); view.RenderChapter(view.LastChapter); yield return null; yield return null;
                Capture("16-self-turning-choice-72", 72);
                view.SetSelfTurning(false); view.RenderChapter(view.LastChapter); yield return null;
            }
            var chosen = ""; view.ChoiceSelected = id => chosen = id;
            yield return new WaitForSecondsRealtime(0.33f);
            naserian.PressFromPointer("choice:1"); yield return null;
            Assert.That(chosen, Is.EqualTo("1"));

            var expected = new StoryTableChapterViewData { Id = "trade-household.expected", Index = 2, Count = 3, Title = "The one who expected more", Caption = "Imagined scene: The other woman reached the water first.", ArtKey = "cast-atlas", ImageLabel = imagined, Questions = new[] { "What did accepting less cost her?" } };
            view.RenderChapter(expected);
            Assert.That(stage.Turning, Is.True, "choosing a side turns the stage");
            for (var deadline = Time.realtimeSinceStartup + 4f; (stage.Turning || stage.Rise < 1) && Time.realtimeSinceStartup < deadline;) yield return null;
            Assert.That(stage.Mirrored, Is.True);
            Assert.That(stage.FigurePlace("naserian").z, Is.LessThan(stage.FigurePlace("wambui").z), "the chosen side stands nearest the visitor");
            Assert.That(naserian.Hidden, Is.True, "no choice is open once the side is chosen");
            // Shortly before the words set the gourd down, the visitor may do it themselves.
            stage.Seek(0.8f); yield return null; yield return null;
            var pass = view.Panels.Single(p => p.name == "Pass the gourd");
            Assert.That(stage.GourdInvited, Is.True); Assert.That(pass.Hidden, Is.False);
            Assert.That(stage.GourdPlace, Is.EqualTo(TableStagingPlace.With));
            yield return new WaitForSecondsRealtime(0.33f);
            pass.PressFromPointer("pass"); yield return null; yield return null;
            Assert.That(stage.VisitorPassedGourd, Is.True);
            Assert.That(stage.GourdPlace, Is.EqualTo(Sankofa.Core.Personal.TableStaging.GourdBetween), "the visitor's hand sets it between them");
            Assert.That(pass.Hidden, Is.True, "the invitation closes once accepted");
            stage.Seek(0.95f); yield return new WaitForSecondsRealtime(0.9f);
            Assert.That(stage.GourdPlace, Is.EqualTo(Sankofa.Core.Personal.TableStaging.GourdBetween), "the narration's own moment does not move it again");
            Assert.That(stage.FigureShown("wambui"), Is.True);
            if (capture) { Capture("14-stage-other-side-72", 72); Capture("14-stage-other-side-82"); }

            view.RenderQuestion("What did accepting less cost her?", "A smaller meal at home tonight.", imagined);
            for (var deadline = Time.realtimeSinceStartup + 1.5f; stage.Rise > 0 && Time.realtimeSinceStartup < deadline;) yield return null;
            Assert.That(stage.Rise, Is.Zero, "the scene lies down while an answer covers it");
            view.RenderChapter(expected);
            for (var deadline = Time.realtimeSinceStartup + 3f; stage.Rise < 1 && Time.realtimeSinceStartup < deadline;) yield return null;
            Assert.That(stage.Rise, Is.EqualTo(1)); Assert.That(stage.Mirrored, Is.True, "returning keeps the chosen side");
            // The application's own closing: the side followed, the side still unheard, and Keep.
            view.ShowClosing("You chose to wait with the one who expected more. Another time, you could follow the one who brought less.\n\nKeep these exact pages on this device, or return to your choices without saving.");
            view.SetControlState(true, false, false, true);
            yield return new WaitForSecondsRealtime(0.3f);
            if (capture) Capture("15-stage-ending-72", 72);

            view.SetReducedMotion(true);
            view.RenderChapter(vessel);
            Assert.That(stage.Turning, Is.False, "reduced motion turns back at once");
            Assert.That(stage.Mirrored, Is.False); Assert.That(stage.Rise, Is.EqualTo(1));
            var figure = stage.FigureRoot("wambui"); var still = figure.position;
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(Vector3.Distance(figure.position, still), Is.LessThan(0.00001f), "reduced motion keeps the stage still");

            view.SetReducedMotion(false);
            view.ShowIntake();
            Assert.That(view.TableRoot.GetComponentsInChildren<Transform>().Count(t => t.name.StartsWith("Heirloom ")), Is.EqualTo(3), "a new visit offers every heirloom again");
            yield return new WaitForSecondsRealtime(0.5f);
            view.RenderChapter(load);
            yield return null;
            view.SetPaused(true);
            var held = stage.Rise;
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(stage.Rise, Is.EqualTo(held), "pause holds the stage where it is");
            view.SetPaused(false);
        }

        static class TableStagingPlace { public const string With = Sankofa.Core.Personal.TableStaging.GourdWith; }

        /// <summary>
        /// Review frames of the stage in motion at a fixed 12 frames per second: a telling rises, its figures take their
        /// poses, a side is chosen and the stage turns. Desktop renders for review, not a performance measurement.
        /// </summary>
        [UnityTest]
        public IEnumerator CapturePopUpStageMotion()
        {
            if (Environment.GetEnvironmentVariable("SANKOFA_CAPTURE") != "1") { Assert.Ignore("Set SANKOFA_CAPTURE=1 to write the stage motion study."); yield break; }
            var stage = view.Stage;
            view.SetReducedMotion(false);
            var frame = 0;
            IEnumerator Film(float seconds)
            {
                for (var end = frame + Mathf.RoundToInt(seconds * 12); frame < end; frame++)
                {
                    yield return null;
                    Capture($"motion/stage-{frame:000}", 72, 960, 840);
                }
            }
            Time.captureFramerate = 12;
            try
            {
                yield return Film(0.8f);
                view.RenderChapter(new StoryTableChapterViewData { Id = "trade-household.load", Index = 0, Count = 3, Title = "A household's small decisions", Caption = "Imagined scene: Come closer to the fire. A woman tips her flour gourd and listens. Less than she thought. At home, a small portion still waits beside the hearth. She could add it to the gourd and arrive with the full load she promised. Instead she leaves that portion where it is.", ArtKey = "cast-atlas", ImageLabel = "Imagined scene" });
                stage.SetNarration(0, 6f);
                for (var t = 0f; t < 6f; t += 1f / 12) { stage.SetNarration(t, 6f); yield return null; Capture($"motion/stage-{frame++:000}", 72, 960, 840); }
                view.RenderChapter(new StoryTableChapterViewData
                {
                    Id = "trade-household.vessel", Index = 1, Count = 3, Title = "A vessel with a source", Caption = "A National Museums of Kenya exhibit describes a Maasai gourd.",
                    ArtKey = "cast-atlas", ImageLabel = "Documented context", Choices = new[] { "Follow the one who brought less", "Wait with the one who expected more" },
                    ChoiceIds = new[] { "trade-household.follow.less", "trade-household.follow.more" },
                });
                yield return Film(2.5f);
                view.RenderChapter(new StoryTableChapterViewData { Id = "trade-household.expected", Index = 2, Count = 3, Title = "The one who expected more", Caption = "Imagined scene: The other woman reached the water first.", ArtKey = "cast-atlas", ImageLabel = "Imagined scene" });
                for (var t = 0f; t < 8f; t += 1f / 12) { if (!stage.Turning) stage.SetNarration(t, 8f); yield return null; Capture($"motion/stage-{frame++:000}", 72, 960, 840); }
            }
            finally { Time.captureFramerate = 0; }
        }

        [UnityTest]
        public IEnumerator DecisionsAndRecoveryStayReachableAlongsidePauseRecenterAndLeave()
        {
            var decision = ""; view.DecisionSelected = id => decision = id;
            view.ShowDecision("Replace this kept story?", "The new story replaces the one kept on this device.", new[] { ("replace", "Replace kept story"), ("cancel", "Keep the earlier story") });
            var actions = view.Panels.Single(p => p.name == "Return and recovery actions");
            actions.PressFromPointer("cancel"); yield return null;
            Assert.That(decision, Is.EqualTo("cancel"));
            view.ShowError("The online service is unavailable.");
            Assert.That(actions.Ids, Is.EquivalentTo(new[] { "retry", "local", "cancel" }));
            view.SetPaused(true);
            var controls = view.Panels.Single(p => p.name == "Always available controls");
            Assert.That(controls.Ids, Is.EquivalentTo(new[] { "pause", "options", "leave" }), "pause, comfort and leaving are always within reach");
            Assert.That(controls.Hidden, Is.False);
        }

        [UnityTest]
        public IEnumerator SurfaceFitPreservesAspectAndReducedMotionKeepsPageStill()
        {
            var center = new Vector3(0.2f, 0.8f, 0.9f); var rotation = Quaternion.Euler(0, 23, 0);
            view.FitToSurface(center, rotation, 1f, 0.6f);
            Assert.That(Vector3.Distance(view.TableRoot.position, center), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(view.TableRoot.rotation, rotation), Is.LessThan(0.001f));
            Assert.That(view.TableRoot.localScale.x, Is.EqualTo(view.TableRoot.localScale.z));
            view.SetReducedMotion(true);
            var page = view.TableRoot.Find("Story page"); var before = page.localRotation;
            view.RenderChapter(0, 2, "A river", "The story can be read without voice or sound.", "scenery-atlas");
            yield return new WaitForSecondsRealtime(0.16f);
            Assert.That(Quaternion.Angle(page.localRotation, before), Is.LessThan(0.001f));
            Assert.That(view.GetComponentsInChildren<AudioSource>(true), Is.Empty, "the tabletop view must not start sound playback");
            foreach (var text in view.GetComponentsInChildren<TMP_Text>(true).Where(x => x.name != "Label"))
                Assert.That(text.richText, Is.False, "visitor and generated text cannot inject presentation markup");
        }

        [UnityTest]
        public IEnumerator LongGeneratedSizeParagraphIsFullyReadableAcrossLocalPages()
        {
            var words = string.Join(" ", Enumerable.Repeat("A remembered journey can lead to a new question about the places people call home.", 8));
            view.SetReducedMotion(true);
            view.RenderChapter(0, 3, "A question about a journey", words);
            view.SetControlState(false, true, true, false);
            var text = view.GetComponentsInChildren<TMP_Text>(true).Single(x => x.name == "Narrative captions");
            var pages = new System.Collections.Generic.List<string>();
            var nextChapter = 0; view.NextRequested = () => nextChapter++;
            var transport = view.Panels.Single(p => p.name == "Turn a page");
            var parts = view.ReadingPartCount;
            Assert.That(parts, Is.GreaterThan(1), "long text must use more reading space, rather than become smaller type");
            for (var part = 0; part < parts; part++)
            {
                text.ForceMeshUpdate(true, true);
                Assert.That(text.isTextOverflowing, Is.False, "reading part " + part + " must fit the actual caption font and bounds");
                pages.Add(view.Caption);
                transport.PressFromPointer("next"); yield return null;
                if (part + 1 < parts) Assert.That(nextChapter, Is.Zero, "reading continuation must not skip to the next chapter");
                yield return new WaitForSecondsRealtime(0.32f);
            }
            Assert.That(string.Join(" ", pages), Is.EqualTo(words));
            Assert.That(nextChapter, Is.EqualTo(1));
            view.SetLargeText(true);
            Assert.That(view.ReadingPartCount, Is.GreaterThanOrEqualTo(parts));
            var retained = view.ReadingPart;
            view.RenderChapter(0, 3, "A question about a journey", words);
            Assert.That(view.ReadingPart, Is.EqualTo(retained), "pause/options/source refresh preserves the part already being read");
            text.ForceMeshUpdate(true, true);
            Assert.That(text.isTextOverflowing, Is.False);
        }

        [UnityTest]
        public IEnumerator LabelsMadeWhileTheirPanelIsAsideStayInsideTheirButtons()
        {
            // At the intake the reading actions are made while their page is set aside; showing them must not let
            // TextMeshPro's first-use defaults turn a one-line label into lettering metres tall.
            var transport = view.Panels.Single(p => p.name == "Turn a page");
            Assert.That(transport.gameObject.activeInHierarchy, Is.False);
            view.RenderChapter(new StoryTableChapterViewData { Id = "journeys.arrival", Index = 0, Count = 3, Title = "Two paths toward a meeting", Caption = "Short.", ArtKey = "scenery-atlas", ImageLabel = "Imagined scene" });
            yield return null; yield return null;
            foreach (var panel in view.Panels.Where(p => p.gameObject.activeInHierarchy && !p.Hidden))
            foreach (var face in panel.GetComponentsInChildren<Renderer>().Where(r => r.name == "Face"))
            {
                var label = face.transform.parent.GetComponentInChildren<TMP_Text>();
                label.ForceMeshUpdate(true, true);
                var size = label.GetComponent<Renderer>().bounds.size;
                Assert.That(Mathf.Max(size.x, size.y, size.z), Is.LessThan(face.bounds.size.magnitude), panel.name + ": " + label.text);
            }
        }

        /// <summary>
        /// Static cost at the busiest moment of a telling: both figures, every plaque and a full page of actions. These are
        /// conservative counts for a mobile headset, not a frame-time measurement; device profiling is not available.
        /// </summary>
        [UnityTest]
        public IEnumerator TheBusiestStageMomentStaysWithinAConservativeQuestBudget()
        {
            view.SetReducedMotion(true);
            view.RenderChapter(new StoryTableChapterViewData
            {
                Id = "trade-household.vessel", Index = 1, Count = 3, Title = "A vessel with a source", Caption = "A National Museums of Kenya exhibit describes a Maasai gourd.",
                ArtKey = "cast-atlas", ImageLabel = "Documented context • sources available", Questions = new[] { "What does the museum example tell us?" },
                Choices = new[] { "Follow the one who brought less", "Wait with the one who expected more" }, ChoiceIds = new[] { "trade-household.follow.less", "trade-household.follow.more" },
            });
            view.SetControlState(true, true, true, false);
            view.Stage.Seek(0.5f);
            yield return null; yield return null;
            var renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r.enabled && r.gameObject.activeInHierarchy).ToArray();
            long triangles = 0;
            foreach (var renderer in renderers)
            {
                var mesh = renderer is SkinnedMeshRenderer skinned ? skinned.sharedMesh : renderer.GetComponent<MeshFilter>()?.sharedMesh;
                if (mesh == null) continue;
                for (var sub = 0; sub < mesh.subMeshCount; sub++) triangles += mesh.GetIndexCount(sub) / 3;
            }
            var text = renderers.Count(r => r.GetComponent<TMP_Text>() != null);
            var blended = renderers.Where(r => r.GetComponent<TMP_Text>() == null && r.sharedMaterials.Any(m => m != null && m.renderQueue >= 3000)).Select(r => r.name).ToArray();
            var materials = renderers.SelectMany(r => r.sharedMaterials).Where(m => m != null).Distinct().Count();
            var report = $"Busiest table moment: {renderers.Length} renderers ({text} text), {triangles} triangles, {materials} materials, blended: {string.Join(", ", blended)}";
            TestContext.WriteLine(report); Debug.Log("[Sankofa budget] " + report);
            Assert.That(renderers.Length, Is.LessThanOrEqualTo(160), report);
            Assert.That(triangles, Is.LessThanOrEqualTo(100000), report);
            Assert.That(blended.Length, Is.LessThanOrEqualTo(8), "blended surfaces cost overdraw on a tiled mobile GPU: " + report);
            Assert.That(renderers.Where(r => r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off).Select(r => r.name), Is.Empty, "no realtime shadow casters");
            Assert.That(UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.isActiveAndEnabled && l.shadows != LightShadows.None).Select(l => l.name), Is.Empty, "no realtime shadows");
        }

        [UnityTest]
        public IEnumerator CaptionsKeepPaceWithTheVoiceAndNeverTurnBackOnTheReader()
        {
            var words = string.Join(" ", Enumerable.Repeat("A remembered journey can lead to a new question about the places people call home.", 6));
            view.RenderChapter(new StoryTableChapterViewData { Id = "journeys.arrival", Index = 0, Count = 3, Title = "Two paths toward a meeting", Caption = words, ArtKey = "scenery-atlas", ImageLabel = "Imagined scene" });
            var parts = view.ReadingPartCount;
            Assert.That(parts, Is.GreaterThan(1));
            view.SetNarrationProgress(0.5f, 40f);
            Assert.That(view.ReadingPart, Is.Zero, "the first words are still being spoken");
            view.SetNarrationProgress(39f, 40f);
            Assert.That(view.ReadingPart, Is.EqualTo(parts - 1), "the captions reach the part being spoken");
            Assert.That(view.Stage.Progress, Is.GreaterThan(0.95f), "the stage performs to the same voice");
            view.SetNarrationProgress(1f, 40f);
            Assert.That(view.ReadingPart, Is.EqualTo(parts - 1), "a reader who is ahead is never pulled back");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ActualPreparedStoryAndEntryFitComfortableForwardLenses()
        {
            var app = UnityEngine.Object.FindFirstObjectByType<PersonalStoryApp>();
            Assert.That(app, Is.Not.Null);
            UnityEngine.Object.FindFirstObjectByType<StoryTablePlacement>().Recenter();
            app.Session.Cancel();
            foreach (var fov in new[] { 60, 72, 82 }) AssertControlsInForwardView(fov);
            if (Environment.GetEnvironmentVariable("SANKOFA_CAPTURE") == "1")
                foreach (var fov in new[] { 60, 72, 82 }) Capture("03-actual-entry-" + fov, fov);
            app.SetSeed("What did a household carry for trade?"); app.StartPrepared();
            yield return null;
            Assert.That(app.Session.Story.Interest, Is.EqualTo("trade-household"));
            // The page shows the words the griot speaks; the stored disclosure is the page's label, not part of the caption.
            Assert.That(app.Session.Chapter.Narration, Does.StartWith(PersonalStoryCatalog.ReconstructionPrefix));
            Assert.That(view.Caption, Does.StartWith(PersonalStoryVoice.Spoken(app.Session.Chapter.Narration).Substring(0, 30)));
            Assert.That(view.Caption, Does.Not.Contain("Imagined scene"));
            Assert.That(view.GetComponentsInChildren<TMP_Text>(true).Single(t => t.name == "Illustration context").text, Is.EqualTo("Imagined scene"));
            foreach (var fov in new[] { 60, 72, 82 }) AssertControlsInForwardView(fov);
            if (Environment.GetEnvironmentVariable("SANKOFA_CAPTURE") == "1")
                foreach (var fov in new[] { 60, 72, 82 }) Capture("04-actual-story-" + fov, fov);
        }

        [UnityTest]
        public IEnumerator SeedPreviewSourcesAndOptionsStayReadableAndKeepTheirActionsClear()
        {
            var app = UnityEngine.Object.FindFirstObjectByType<PersonalStoryApp>();
            UnityEngine.Object.FindFirstObjectByType<StoryTablePlacement>().Recenter();
            app.SetSeed(string.Join(" ", Enumerable.Repeat("What did a household carry when journeys brought people to the river?", 7)));
            app.Session.Cancel(); yield return null;
            var preview = view.GetComponentsInChildren<TMP_Text>(true).Single(x => x.name == "Your question preview");
            Assert.That(preview.gameObject.activeInHierarchy, Is.True);
            Assert.That(preview.text, Does.Contain("preview").And.Contain("Full question"));
            preview.ForceMeshUpdate(true, true); Assert.That(preview.isTextOverflowing, Is.False);
            if (Environment.GetEnvironmentVariable("SANKOFA_CAPTURE") == "1") Capture("05-question-preview-60", 60);

            view.Panels.Single(p => p.name == "Start choices").PressFromPointer("create"); yield return null;
            var consent = view.Panels.Single(p => p.name == "Online consent actions");
            var inspected = new System.Collections.Generic.List<string>();
            do
            {
                inspected.Add(view.ModalText); AssertControlsInForwardView(60);
                if (view.ModalPart + 1 >= view.ModalPartCount) break;
                consent.PressFromPointer("modal-next"); yield return null; yield return new WaitForSecondsRealtime(0.32f);
            } while (true);
            var fullRequest = System.Text.RegularExpressions.Regex.Replace(string.Join(" ", inspected), "\\s+", " ");
            Assert.That(fullRequest, Does.Contain("Place: Enkare Nairobi").And.Contain("Period: before the railway").And.Contain("Interest: Trade & household").And.Contain(app.Seed));
            if (Environment.GetEnvironmentVariable("SANKOFA_CAPTURE") == "1") Capture("08-full-cloud-request-60", 60);

            var sourceText = string.Join("\n", Enumerable.Range(1, 24).Select(i => "Source " + i + ": documented context and remembered places."));
            view.ShowSourceDetails("Sources and accounts", sourceText);
            var body = view.GetComponentsInChildren<TMP_Text>(true).Single(x => x.name == "Context answer");
            var actions = view.Panels.Single(p => p.name == "Return and recovery actions");
            Assert.That(view.ModalPartCount, Is.GreaterThan(1));
            var parts = new System.Collections.Generic.List<string>();
            do
            {
                body.ForceMeshUpdate(true, true); Assert.That(body.isTextOverflowing, Is.False);
                AssertControlsInForwardView(60); parts.Add(view.ModalText);
                if (view.ModalPart + 1 >= view.ModalPartCount) break;
                actions.PressFromPointer("modal-next"); yield return null; yield return new WaitForSecondsRealtime(0.32f);
            } while (true);
            var realised = System.Text.RegularExpressions.Regex.Replace(string.Join(" ", parts), "\\s+", " ").Trim();
            Assert.That(realised, Does.Contain(System.Text.RegularExpressions.Regex.Replace(sourceText, "\\s+", " ")));
            if (Environment.GetEnvironmentVariable("SANKOFA_CAPTURE") == "1") Capture("06-source-context-60", 60);
            view.ComfortRequested?.Invoke(); yield return null;
            Assert.That(actions.Ids, Is.SupersetOf(new[] { "recenter", "room", "lower", "raise", "listen", "motion", "text", "return" }), "placement and comfort live in Options");
            AssertControlsInForwardView(60);
            var controls = view.Panels.Single(p => p.name == "Always available controls");
            foreach (var action in actions.GetComponentsInChildren<Renderer>().Where(r => r.name == "Face"))
            foreach (var control in controls.GetComponentsInChildren<Renderer>().Where(r => r.name == "Face"))
                Assert.That(action.bounds.Intersects(control.bounds), Is.False, "options cannot overlap the essential controls");
            if (Environment.GetEnvironmentVariable("SANKOFA_CAPTURE") == "1") Capture("07-options-60", 60);
        }

        void AssertControlsInForwardView(float fieldOfView)
        {
            var cameraRoot = new GameObject("Forward lens geometry probe");
            var eye = cameraRoot.AddComponent<Camera>(); eye.enabled = false;
            eye.transform.SetPositionAndRotation(SeatedEye, Quaternion.Euler(TableGaze, 0, 0));
            eye.fieldOfView = fieldOfView; eye.aspect = 1600f / 1400;
            try
            {
                foreach (var panel in view.Panels.Where(p => p.gameObject.activeInHierarchy && !p.Hidden))
                foreach (var face in panel.GetComponentsInChildren<Renderer>().Where(r => r.name == "Face"))
                {
                    var bounds = face.bounds;
                    foreach (var x in new[] { bounds.min.x, bounds.max.x })
                    foreach (var y in new[] { bounds.min.y, bounds.max.y })
                    foreach (var z in new[] { bounds.min.z, bounds.max.z })
                    {
                        var point = eye.WorldToViewportPoint(new Vector3(x, y, z));
                        Assert.That(point.z, Is.GreaterThan(0));
                        Assert.That(point.x, Is.InRange(0.015f, 0.985f), panel.name + " horizontal view at " + fieldOfView);
                        Assert.That(point.y, Is.InRange(0.015f, 0.985f), panel.name + " vertical view at " + fieldOfView);
                    }
                }
            }
            finally { UnityEngine.Object.Destroy(cameraRoot); }
        }

        static readonly Vector3 SeatedEye = new Vector3(0, 1.15f, 0);
        /// <summary>
        /// A seated visitor watching the tabletop stage and reading the page in front of it looks down about 25 degrees;
        /// lenses and captures are centred there rather than on the horizon.
        /// </summary>
        const float TableGaze = 25f;

        static void Capture(string name, float fieldOfView = 82, int width = 1600, int height = 1400)
        {
            var directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "artifacts", "story-table"));
            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(directory, name)));
            var cameraRoot = new GameObject("Silent desktop review camera");
            var eye = cameraRoot.AddComponent<Camera>(); eye.stereoTargetEye = StereoTargetEyeMask.None;
            eye.transform.SetPositionAndRotation(SeatedEye, Quaternion.Euler(TableGaze, 0, 0));
            eye.fieldOfView = fieldOfView; eye.clearFlags = CameraClearFlags.SolidColor; eye.backgroundColor = new Color(0.024f, 0.028f, 0.038f);
            var target = new RenderTexture(width, height, 24) { antiAliasing = 4 };
            var previousActive = RenderTexture.active;
            eye.targetTexture = target; eye.Render(); RenderTexture.active = target;
            var pixels = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); pixels.Apply();
            File.WriteAllBytes(Path.Combine(directory, name + ".png"), pixels.EncodeToPNG());
            RenderTexture.active = previousActive; target.Release();
            UnityEngine.Object.Destroy(pixels); UnityEngine.Object.Destroy(target); UnityEngine.Object.Destroy(cameraRoot);
        }
    }
}
