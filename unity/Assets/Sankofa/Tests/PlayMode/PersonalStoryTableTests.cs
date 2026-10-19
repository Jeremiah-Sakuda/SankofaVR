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
            SceneManager.LoadScene("StoryTable");
            yield return null;
            view = UnityEngine.Object.FindFirstObjectByType<StoryTableView>();
            Assert.That(view, Is.Not.Null);
            // The application integration has separate state tests; isolate this view's accepted callback contract.
            foreach (var app in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Where(x => x.GetType().Name == "PersonalStoryApp")) { app.enabled = false; app.StopAllCoroutines(); }
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
        public IEnumerator ChapterReplacementKeepsOneFlatFocalImageAndTruthfulCapability()
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
            Assert.That(controls.Ids, Is.EquivalentTo(new[] { "pause", "recenter", "room", "leave" }));
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

        static void Capture(string name)
        {
            var directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "artifacts", "story-table"));
            Directory.CreateDirectory(directory);
            var cameraRoot = new GameObject("Silent desktop review camera");
            var eye = cameraRoot.AddComponent<Camera>(); eye.stereoTargetEye = StereoTargetEyeMask.None;
            eye.transform.SetPositionAndRotation(new Vector3(0, 1.15f, 0), Quaternion.Euler(10, 0, 0));
            eye.fieldOfView = 82; eye.clearFlags = CameraClearFlags.SolidColor; eye.backgroundColor = new Color(0.024f, 0.028f, 0.038f);
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
