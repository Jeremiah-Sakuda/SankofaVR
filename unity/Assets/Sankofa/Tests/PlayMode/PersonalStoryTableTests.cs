using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
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
            Assert.That(view.TableRoot.Find("Upright illustrated journal/One flat illustrated page/Cast and exchange").gameObject.activeSelf, Is.True,
                "an alias cannot disable the same illustration root a second time");
            Assert.That(view.TableRoot.Find("Upright illustrated journal/One flat illustrated page/River landscape").gameObject.activeSelf, Is.False);
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
                ImageLabel = "Imagined scene • not an account of your ancestors", Questions = new[] { "What could I ask?" },
            });
            yield return null;
            Assert.That(side.Ids, Is.EqualTo(new[] { "0" }));
            Assert.That(view.Caption, Is.EqualTo("An imagined ending."));
        }

        [UnityTest]
        public IEnumerator PrintLayersRiseInDepthWithinTheirBackdropAndHoldStillWhenAsked()
        {
            var print = view.TableRoot.Find("Upright illustrated journal/One flat illustrated page/Cast and exchange");
            var backdrop = print.Find("Printed dusk");
            var figure = print.Find("Printed trader left");
            var toward = print.TransformDirection(Vector3.back).normalized;
            float Raised(Transform layer, Transform page) => Vector3.Dot(layer.position - page.position, toward);
            var scale = view.TableRoot.lossyScale.z;
            const string imagined = "Imagined scene • not an account of your ancestors";

            view.SetReducedMotion(false);
            var chapter = new StoryTableChapterViewData { Index = 0, Count = 3, Title = "A household's small decisions", Caption = "A print rises from the page.", ArtKey = "cast-atlas", ImageLabel = imagined };
            view.RenderChapter(chapter);
            Assert.That(view.PopUpProgress, Is.Zero, "a newly turned page begins flat");
            Assert.That(Raised(figure, backdrop), Is.LessThan(0.002f));
            for (var deadline = Time.realtimeSinceStartup + 3f; view.PopUpProgress < 1 && Time.realtimeSinceStartup < deadline;) yield return null;
            Assert.That(view.PopUpProgress, Is.EqualTo(1));
            Assert.That(Raised(figure, backdrop), Is.EqualTo(StoryTableView.MaximumLayerDepth * scale).Within(0.002f), "the figures stand nearest the reader");
            Assert.That(Raised(print.Find("Printed forest and hills"), backdrop), Is.GreaterThan(0.005f * scale).And.LessThan(Raised(print.Find("Printed riverbank"), backdrop)));
            AssertPrintInsideBackdropAndClearOfText(print, backdrop);
            if (Environment.GetEnvironmentVariable("SANKOFA_CAPTURE") == "1") { Capture("10-pop-up-print-60", 60); Capture("10-pop-up-print-82"); }

            view.RenderChapter(chapter);
            Assert.That(view.PopUpProgress, Is.EqualTo(1), "returning from a question or source to the same page keeps the print standing");

            view.SetReducedMotion(true);
            view.RenderChapter(new StoryTableChapterViewData { Index = 1, Count = 3, Title = "A region of overlapping lives", Caption = "Documented context.", ArtKey = "scenery-atlas", ImageLabel = "Documented context • sources available" });
            var scenery = view.TableRoot.Find("Upright illustrated journal/One flat illustrated page/River landscape");
            var river = scenery.Find("Printed riverbank"); var still = river.position;
            Assert.That(view.PopUpProgress, Is.EqualTo(1), "reduced motion shows the final depth at once");
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(Vector3.Distance(river.position, still), Is.LessThan(0.00001f), "reduced motion keeps the print still");
            Assert.That(Raised(river, scenery.Find("Printed dusk")), Is.GreaterThan(0.01f * scale));

            view.SetReducedMotion(false);
            view.RenderChapter(new StoryTableChapterViewData { Index = 2, Count = 3, Title = "The one who expected more", Caption = "The print rises again.", ArtKey = "cast-atlas", ImageLabel = imagined });
            view.SetPaused(true);
            Assert.That(view.PopUpProgress, Is.Zero);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(view.PopUpProgress, Is.Zero, "pause holds the print where it is");
            view.SetPaused(false);
            for (var deadline = Time.realtimeSinceStartup + 3f; view.PopUpProgress < 1 && Time.realtimeSinceStartup < deadline;) yield return null;
            Assert.That(view.PopUpProgress, Is.EqualTo(1));
        }

        /// <summary>From the seated eye, every raised layer stays within its print's backdrop and covers no page text.</summary>
        void AssertPrintInsideBackdropAndClearOfText(Transform print, Transform backdrop)
        {
            var cameraRoot = new GameObject("Pop-up print probe");
            var eye = cameraRoot.AddComponent<Camera>(); eye.enabled = false;
            eye.transform.SetPositionAndRotation(new Vector3(0, 1.15f, 0), Quaternion.Euler(10, 0, 0));
            eye.fieldOfView = 60; eye.aspect = 1600f / 1400;
            try
            {
                Rect Viewport(System.Collections.Generic.IEnumerable<Vector3> world)
                {
                    var points = world.Select(w => eye.WorldToViewportPoint(w)).ToArray();
                    return Rect.MinMaxRect(points.Min(p => p.x), points.Min(p => p.y), points.Max(p => p.x), points.Max(p => p.y));
                }
                System.Collections.Generic.IEnumerable<Vector3> Quad(Transform quad) =>
                    new[] { new Vector3(-.5f, -.5f, 0), new Vector3(-.5f, .5f, 0), new Vector3(.5f, -.5f, 0), new Vector3(.5f, .5f, 0) }.Select(c => quad.TransformPoint(c));
                System.Collections.Generic.IEnumerable<Vector3> Box(Bounds b) =>
                    new[] { -1f, 1f }.SelectMany(x => new[] { -1f, 1f }.SelectMany(y => new[] { -1f, 1f }.Select(z => b.center + Vector3.Scale(b.extents, new Vector3(x, y, z)))));
                var frame = Viewport(Quad(backdrop));
                var texts = view.GetComponentsInChildren<TMP_Text>(true)
                    .Where(t => (t.name == "Illustration context" || t.name == "Narrative captions" || t.name == "Chapter title") && t.gameObject.activeInHierarchy && t.text.Length > 0).ToArray();
                Assert.That(texts, Has.Length.EqualTo(3));
                foreach (var text in texts) text.ForceMeshUpdate(true, true);
                foreach (var layer in print.Cast<Transform>().Where(t => t != backdrop && t.GetComponent<Renderer>() != null))
                {
                    var area = Viewport(Quad(layer));
                    Assert.That(area.xMin >= frame.xMin - .001f && area.xMax <= frame.xMax + .001f && area.yMin >= frame.yMin - .001f && area.yMax <= frame.yMax + .001f,
                        Is.True, layer.name + " stays within the print's backdrop from the seat");
                    foreach (var text in texts)
                        Assert.That(area.Overlaps(Viewport(Box(text.GetComponent<Renderer>().bounds))), Is.False, layer.name + " must not cover " + text.name);
                }
            }
            finally { UnityEngine.Object.Destroy(cameraRoot); }
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
            Assert.That(controls.Ids, Is.EquivalentTo(new[] { "pause", "recenter", "room", "options", "leave" }));
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
            var page = view.TableRoot.Find("Upright illustrated journal"); var before = page.localRotation;
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
            Assert.That(view.Caption, Does.StartWith(app.Session.Chapter.Narration.Substring(0, 30)));
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
            Assert.That(actions.Ids.Count(), Is.EqualTo(6)); AssertControlsInForwardView(60);
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
            eye.transform.SetPositionAndRotation(new Vector3(0, 1.15f, 0), Quaternion.Euler(10, 0, 0));
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

        static void Capture(string name, float fieldOfView = 82)
        {
            var directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "artifacts", "story-table"));
            Directory.CreateDirectory(directory);
            var cameraRoot = new GameObject("Silent desktop review camera");
            var eye = cameraRoot.AddComponent<Camera>(); eye.stereoTargetEye = StereoTargetEyeMask.None;
            eye.transform.SetPositionAndRotation(new Vector3(0, 1.15f, 0), Quaternion.Euler(10, 0, 0));
            eye.fieldOfView = fieldOfView; eye.clearFlags = CameraClearFlags.SolidColor; eye.backgroundColor = new Color(0.024f, 0.028f, 0.038f);
            var target = new RenderTexture(1600, 1400, 24) { antiAliasing = 4 };
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
