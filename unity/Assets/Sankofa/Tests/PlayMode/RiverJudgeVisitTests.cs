using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Sankofa.Core.Personal;
using Sankofa.Flow;
using Sankofa.Runtime.Input;
using Sankofa.Runtime.Personal;
using Sankofa.Runtime.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Sankofa.Tests
{
    /// <summary>Selected painted river: real panels and pinch adapter, synthetic input; never device evidence.</summary>
    public sealed class RiverJudgeVisitTests
    {
        PersonalStoryApp app;
        RiverThresholdPrototype river;
        string shelf;
        readonly List<string> errors = new List<string>();
        [UnitySetUp] public IEnumerator Load()
        {
            errors.Clear(); LogAssert.ignoreFailingMessages = true; Application.logMessageReceived += Error;
            shelf = Path.Combine(Path.GetTempPath(), "sankofa-judge-" + Guid.NewGuid().ToString("N"));
            PersonalStoryApp.StoreFolderOverride = shelf;
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/Sankofa/Scenes/RiverThresholdPainted.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene("RiverThresholdPainted");
#endif
            yield return null; yield return null;
            river = Object.FindFirstObjectByType<RiverThresholdPrototype>();
            river.AutomaticClock = false; app = river.App; app.ServiceEndpoint = "";
            foreach (var p in Object.FindObjectsByType<PointerSource>(FindObjectsSortMode.None)) p.enabled = false;
        }
        [UnityTearDown] public IEnumerator Clean()
        {
            Time.captureDeltaTime = 0;
            var scene = SceneManager.GetSceneByName("RiverThresholdPainted");
            if (scene.IsValid() && scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Judge cleanup " + Guid.NewGuid().ToString("N")));
                yield return SceneManager.UnloadSceneAsync(scene);
            }
            PersonalStoryApp.StoreFolderOverride = null;
            if (Directory.Exists(shelf)) Directory.Delete(shelf, true);
            Application.logMessageReceived -= Error; LogAssert.ignoreFailingMessages = false;
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }
        void Error(string message, string trace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Assert && type != LogType.Exception) return;
            if (type == LogType.Error && message == "Unable to process a controller whose SampleRateHz is 0 now.") return;
            errors.Add(message + "\n" + trace);
        }
        PokePanel Panel(string name) => app.View.Panels.Single(p => p.name == name);
        IEnumerator Pinch(PokePanel panel, string id)
        {
            Assert.That(panel.gameObject.activeInHierarchy && !panel.Hidden, Is.True, panel.name + " must be visible");
            Assert.That(panel.Ids, Does.Contain(id));
            yield return new WaitForSecondsRealtime(.34f);
            var button = panel.transform.Find("Button " + id);
            var ray = new Ray(button.position - button.forward * .15f, button.forward);
            var pointer = Object.FindFirstObjectByType<PointerSource>();
            pointer.ProcessPointer(ray, 0, true, 1, .02f);
            pointer.ProcessPointer(ray, 1, true, 1, .02f);
            pointer.ProcessPointer(ray, 0, true, 1, .02f);
            yield return null; yield return null;
        }
        IEnumerator Water()
        {
            yield return Pinch(river.Controls, "primary");
            yield return Pinch(Panel("Interest choices"), "place-water");
            for (var i = 0; i < 62; i++) river.Advance(.1f);
            yield return null; yield return null;
        }
        IEnumerator ChooseAtFigure(string choice)
        {
            var choices = Object.FindFirstObjectByType<RiverPerspectiveChoices>();
            yield return WaitFor(() => choices.Panels.Any(p => !p.Hidden && p.Ids.Contains("river:" + choice)));
            var panel = choices.Panels.Single(p => !p.Hidden && p.Ids.Contains("river:" + choice));
            var id = "river:" + choice;
            yield return new WaitForSecondsRealtime(.34f);
            var at = panel.transform.Find("Button " + id).position;
            var from = Camera.main.transform.position;
            var ray = new Ray(from, (at - from).normalized);
            var pointer = Object.FindFirstObjectByType<PointerSource>();
            pointer.ProcessPointer(ray, 0, true, 1, .02f);
            Assert.That(pointer.Hovered, Is.EqualTo(id));
            yield return new WaitForSeconds(.45f);
            pointer.ProcessPointer(ray, 1, true, 1, .02f);
            pointer.ProcessPointer(ray, 0, true, 1, .02f);
            yield return null; yield return null;
        }
        IEnumerator WaitFor(Func<bool> condition, float seconds = 22)
        {
            var until = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < until) yield return null;
            Assert.That(condition(), Is.True, "Expected visit state within " + seconds + " seconds");
        }

        IEnumerator ReviewStill(string name)
        {
            var folder = Environment.GetEnvironmentVariable("SANKOFA_ROUND_STILLS");
            if (string.IsNullOrEmpty(folder)) yield break;
            var settled = false;
            var marker = new GameObject("After presentation capture").AddComponent<RiverReviewFrame>();
            marker.Capture = () => { WriteReviewStill(folder, name); settled = true; };
            while (!settled) yield return null;
            Object.Destroy(marker.gameObject);
        }

        void WriteReviewStill(string folder, string name)
        {
            Directory.CreateDirectory(folder);
            var cameraObject = new GameObject("Round review camera");
            var eye = cameraObject.AddComponent<Camera>(); eye.enabled = false; eye.stereoTargetEye = StereoTargetEyeMask.None;
            eye.transform.SetPositionAndRotation(new Vector3(0, 1.15f, 0), Quaternion.Euler(8, 0, 0));
            eye.fieldOfView = 60; eye.nearClipPlane = .03f; eye.clearFlags = CameraClearFlags.SolidColor;
            eye.backgroundColor = new Color(.008f, .012f, .022f);
            var target = new RenderTexture(1600, 900, 24) { antiAliasing = 4 };
            var pixels = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                eye.targetTexture = target; eye.Render(); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); pixels.Apply();
                File.WriteAllBytes(Path.Combine(folder, name + ".png"), pixels.EncodeToPNG());
                File.WriteAllText(Path.Combine(folder, "evidence.txt"), "Actual Unity mono desktop stills, synthetic single-pointer input; test may seek story time. Not continuous gameplay, XR, listening or performance evidence.");
            }
            finally { RenderTexture.active = previous; target.Release(); Object.Destroy(target); Object.Destroy(pixels); Object.Destroy(cameraObject); }
        }

        [UnityTest] public IEnumerator ChoicePacingIsDisclosedAndManualModeWaitsForTheVisitor()
        {
            app.TurnsPages = true; yield return Water();
            app.View.Stage.Seek(1); yield return WaitFor(() => app.Session.OffersChoice);
            app.View.Stage.Seek(1); yield return WaitFor(() => app.PageToldOnce);
            app.View.ShowLastReadingPart(); yield return null; yield return null;
            Assert.That(app.View.Caption, Does.Contain("let the fire guide you"));
            Assert.That(app.View.Caption, Does.Not.Contain("a name"));
            yield return ReviewStill("01-guided-choice");
            yield return Pinch(Panel("Always available controls"), "options");
            yield return Pinch(Panel("Return and recovery actions"), "pace");
            Assert.That(app.TurnsPages, Is.False);
            app.View.ShowLastReadingPart(); yield return null; yield return null;
            Assert.That(app.View.Caption, Does.Contain("Take your time"));
            yield return ReviewStill("02-manual-choice");
            var chapter = app.Session.Chapter.Id;
            yield return new WaitForSeconds(15);
            Assert.That(app.Session.Chapter.Id, Is.EqualTo(chapter));
            yield return Pinch(Panel("Questions beside the story"), "choice:1");
            Assert.That(app.Session.FollowedByChoice, Is.True);
        }

        [UnityTest] public IEnumerator KeptEndingReopensExactlyAndLeaveTellsTheTruth()
        {
            yield return Water();
            while (app.Session.State == PersonalStoryState.Reading) app.Session.Next();
            yield return null; yield return null;
            Assert.That(Panel("Turn a page").Ids, Does.Not.Contain("reopen-closing"));
            yield return Pinch(Panel("Always available controls"), "leave");
            Assert.That(app.View.ModalText, Does.Contain("stays unsaved"));
            yield return Pinch(Panel("Return and recovery actions"), "cancel");
            var exact = PersonalStoryJson.Write(app.Session.Story);
            yield return Pinch(Panel("Turn a page"), "save");
            Assert.That(app.View.Caption, Does.Contain("One telling"));
            yield return new WaitForSeconds(.4f); yield return ReviewStill("01-kept-replay-action");
            yield return Pinch(Panel("Always available controls"), "leave");
            Assert.That(app.View.ModalText, Does.Contain("is kept on this device"));
            Assert.That(app.View.ModalText, Does.Not.Contain("stays unsaved"));
            yield return new WaitForSeconds(.4f); yield return ReviewStill("02-kept-leave");
            yield return Pinch(Panel("Return and recovery actions"), "cancel");
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Closing));
            yield return Pinch(Panel("Turn a page"), "reopen-closing");
            Assert.That(app.Session.IsReplay, Is.True);
            Assert.That(PersonalStoryJson.Write(app.Session.Story), Is.EqualTo(exact));
            Assert.That(File.ReadAllText(Path.Combine(shelf, "kept.story")), Is.EqualTo(exact));
        }

        [UnityTest] public IEnumerator EndingInvitesAnotherTellingWithoutSavingOrDiscardingTheQuestion()
        {
            yield return Water();
            while (!app.Session.OffersChoice) app.Session.Next();
            app.Session.Choose(0); app.Session.HearOtherSide(0, true);
            while (app.Session.State == PersonalStoryState.Reading) app.Session.Next();
            yield return null; yield return null;
            Assert.That(app.View.Caption, Does.Contain("Who followed the path"));
            yield return new WaitForSeconds(.4f); yield return ReviewStill("01-next-curiosity");
            var strandField = typeof(PersonalStoryApp).GetField("strand", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            foreach (var count in new[] { 0, 1, 2, 6 })
            {
                var strand = new KeptStrand();
                var sides = KeptStrand.Sides("place-water").Select(s => new KeptBead("place-water", s.Id))
                    .Concat(KeptStrand.Tellings.Where(t => t != "place-water").SelectMany(t => KeptStrand.Sides(t).Select(s => new KeptBead(t, s.Id))));
                foreach (var bead in sides.Take(count)) strand.Add(bead.Telling, bead.Side);
                strandField.SetValue(app, strand);
                app.Session.Pause(true); app.Session.Pause(false); yield return null;
                var label = Panel("Turn a page").transform.Find("Button again/Label").GetComponent<TMPro.TMP_Text>().text;
                Assert.That(label, Is.EqualTo(count == 6 ? "Revisit journeys" : "Choose journeys next"));
            }
            Assert.That(app.HasKept, Is.False, "suggestions never save the current telling");
            yield return Pinch(Panel("Turn a page"), "again"); river.Advance(.1f); yield return null; yield return null;
            yield return ReviewStill("02-next-table");
            Assert.That(app.View.Caption, Does.Contain("choose the bundle"));
            app.StartPrepared(); yield return null;
            Assert.That(app.Session.Story.Interest, Is.EqualTo("journeys"));
            Assert.That(app.HasKept, Is.False);
            while (app.Session.State == PersonalStoryState.Reading) app.Session.Next();
            app.SetSeed("Why was the river called Enkare Nairobi?");
            app.Session.Pause(true); app.Session.Pause(false); yield return null;
            Assert.That(Panel("Turn a page").transform.Find("Button again/Label").GetComponent<TMPro.TMP_Text>().text, Is.EqualTo("Return to my question"));
            yield return Pinch(Panel("Turn a page"), "again");
            app.StartPrepared();
            for (var i = 0; i < 62; i++) river.Advance(.1f);
            yield return null;
            Assert.That(app.Session.Story.Interest, Is.EqualTo("place-water"), "the explicit visitor question is preserved");
        }

        [UnityTest] public IEnumerator NarratedChoicesBothSidesKeepAndExactReopenRemainReachable()
        {
            app.TurnsPages = true;
            yield return Water();
            var realised = PersonalStoryJson.Write(app.Session.Story);
            // Seek only performance time, as if each caption has been read; application still owns page/route changes.
            app.View.Stage.Seek(1);
            yield return WaitFor(() => app.Session.OffersChoice);
            app.View.Stage.Seek(1); yield return null; yield return null;
            var choices = Panel("Questions beside the story");
            Assert.That(choices.Hidden, Is.False, "hiding the table stage must leave reachable page choices in narrated mode");
            yield return Pinch(choices, "choice:1");
            Assert.That(app.Session.FollowedByChoice, Is.True);
            var firstSide = app.Session.Chapter.Id;
            app.View.Stage.Seek(1);
            yield return WaitFor(() => app.OtherSideOffered);
            Assert.That(app.View.Caption, Does.Not.Contain("across the stage"), "river instructions describe its reachable choices");
            app.View.ShowLastReadingPart();
            app.View.SetSpatialStageVisible(true);
            Assert.That(app.View.ReadingPart, Is.EqualTo(app.View.ReadingPartCount - 1), "switching presentation preserves the completed ending");
            app.View.SetSpatialStageVisible(false);
            Assert.That(app.View.ReadingPart, Is.EqualTo(app.View.ReadingPartCount - 1));
            yield return Pinch(choices, choices.Ids.Single());
            Assert.That(app.Session.Chapter.Id, Is.Not.EqualTo(firstSide));
            app.View.Stage.Seek(1);
            yield return WaitFor(() => app.Session.State == PersonalStoryState.Closing);
            Assert.That(app.HasKept, Is.False);
            yield return Pinch(Panel("Turn a page"), "save");
            Assert.That(app.HasKept, Is.True);
            Assert.That(File.ReadAllText(Path.Combine(shelf, "kept.story")), Is.EqualTo(realised));
            yield return Pinch(Panel("Turn a page"), "again"); river.Advance(.1f); yield return null;
            yield return Pinch(Panel("Kept story choices"), "reopen");
            Assert.That(app.Session.IsReplay, Is.True);
            Assert.That(PersonalStoryJson.Write(app.Session.Story), Is.EqualTo(realised));
        }

        [UnityTest] public IEnumerator RaisingStandAfterLeaningDoesNotMoveTheRiver()
        {
            yield return Water();
            var world = GameObject.Find("River threshold world").transform;
            var before = world.position; var heading = world.rotation;
            var head = Camera.main.transform; var original = head.position; var rotation = head.rotation;
            try
            {
                head.position = new Vector3(.12f, 1.28f, .1f); head.rotation = Quaternion.Euler(0, 12, 0);
                app.GetComponent<StoryTablePlacement>().AdjustHeight(.06f); yield return null;
                Assert.That(Vector3.Distance(world.position, before), Is.LessThan(.001f));
                Assert.That(Quaternion.Angle(world.rotation, heading), Is.LessThan(.01f));
            }
            finally { head.position = original; head.rotation = rotation; }
        }

        [UnityTest] public IEnumerator PersonalQuestionCannotPutAGourdTellingInTheRiver()
        {
            yield return Pinch(river.Controls, "primary");
            app.SetSeed("What did a household carry in a gourd?");
            yield return Pinch(Panel("Interest choices"), "place-water");
            for (var i = 0; i < 62; i++) river.Advance(.1f);
            yield return null;
            Assert.That(app.Session.Chapter.Id, Does.StartWith("trade-household."));
            Assert.That(river.RiverVisible, Is.False, "environment follows the realised telling, including question-driven personalisation");
            app.Session.Cancel(); river.Advance(.1f); yield return null;
            app.SetSeed("Why was the river called Enkare Nairobi?");
            yield return Pinch(Panel("Interest choices"), "trade-household");
            for (var i = 0; i < 62; i++) river.Advance(.1f);
            yield return null;
            Assert.That(app.Session.Chapter.Id, Does.StartWith("place-water."));
            Assert.That(river.RiverVisible, Is.True, "the inverse override must also match the resolved telling");
        }

        [UnityTest] public IEnumerator DeferredPreparedStoryRejectsThePreviousProviderResponse()
        {
            yield return Pinch(river.Controls, "primary");
            app.View.SetOnlineAvailable(true); // Select a pot without starting; no service or network request is made.
            yield return Pinch(Panel("Interest choices"), "place-water");
            var request = new PersonalStoryRequest(PersonalStoryCatalog.Region, PersonalStoryCatalog.Period, "place-water");
            var stale = app.Session.Begin(request);
            app.StartPrepared();
            Assert.That(river.Sequence.Phase, Is.EqualTo(RiverThresholdPhase.Crossing));
            Assert.That(app.Session.Accept(stale, BundledPersonalStoryComposer.Compose(request).Story), Is.False);
            Assert.That(app.Session.Story, Is.Null, "no old response can appear beneath the deferred reveal");
            for (var i = 0; i < 62; i++) river.Advance(.1f);
            yield return null;
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Reading));
        }

        [UnityTest] public IEnumerator IntakeRoomPlacementAlsoWithdrawsTheCinematicBackdrop()
        {
            app.TurnsPages = true;
            yield return Pinch(river.Controls, "primary");
            yield return Pinch(Panel("Start choices"), "room");
            river.Advance(.1f); yield return null;
            Assert.That(app.GetComponent<StoryTablePlacement>().AwaitingHand, Is.True);
            var opening = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(t => t.name == "Cinematic opening");
            Assert.That(opening.gameObject.activeInHierarchy, Is.False);
        }

        [UnityTest] public IEnumerator RoomPlacementReturnsToTheTableBeforeAskingForAHand()
        {
            yield return Water();
            yield return Pinch(Panel("Always available controls"), "options");
            yield return Pinch(Panel("Return and recovery actions"), "room");
            Assert.That(app.GetComponent<StoryTablePlacement>().AwaitingHand, Is.True);
            Assert.That(river.Sequence.Phase, Is.EqualTo(RiverThresholdPhase.Table));
            Assert.That(river.RiverVisible, Is.False, "room-placement guidance must not be covered by river scenery");
        }

        [UnityTest] public IEnumerator RiversideHasOnePauseControlAndKeepsComfortOptions()
        {
            yield return Water();
            var visible = app.View.Panels.Append(river.Controls).Where(p => p.gameObject.activeInHierarchy && !p.Hidden);
            Assert.That(visible.Sum(p => p.Ids.Count(id => id == "pause")), Is.EqualTo(1));
            Assert.That(Panel("Always available controls").Ids, Does.Contain("options"));
        }

        [UnityTest] public IEnumerator CompactReadingPreservesTextAndRevealBoundedContext()
        {
            var caption = app.View.TableRoot.Find("Story page/Narrative captions").GetComponent<TMPro.TMP_Text>();
            var originalSize = caption.fontSize;
            app.TurnsPages = true;
            yield return Water();
            var layout = Object.FindFirstObjectByType<RiverReadingLayout>();
            Assert.That(layout, Is.Not.Null);
            Assert.That(layout.CaptionBackingHeight, Is.LessThan(.15f), "empty opaque desk space is removed");
            Assert.That(caption.fontSize, Is.EqualTo(originalSize), "the story remains readable at its authored size");
            caption.ForceMeshUpdate(); Assert.That(caption.isTextOverflowing, Is.False);
            Assert.That(Panel("Always available controls").Ids, Does.Contain("context"));
            Assert.That(Panel("Turn a page").Ids, Does.Not.Contain("sources"));
            yield return Pinch(Panel("Always available controls"), "context");
            Assert.That(Panel("Return and recovery actions").Ids, Does.Contain("sources"));
            Assert.That(Panel("Return and recovery actions").Ids.Any(id => id.StartsWith("question:")), Is.False,
                "context menu does not reveal the untold page's questions");
            yield return Pinch(Panel("Return and recovery actions"), "return");
            app.View.Stage.Seek(1); yield return WaitFor(() => app.PageToldOnce);
            yield return Pinch(Panel("Always available controls"), "context");
            Assert.That(Panel("Return and recovery actions").Ids.Any(id => id.StartsWith("question:")), Is.True);
            yield return Pinch(Panel("Return and recovery actions"), "sources");
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Sources));
        }

        [UnityTest] public IEnumerator LargeCaptionsAndTableReturnRestoreTheirOriginalLayout()
        {
            var paper = app.View.TableRoot.Find("Story page/Page paper");
            var oldScale = paper.localScale;
            yield return Water();
            app.View.SetLargeText(true); yield return null; yield return null;
            var caption = app.View.TableRoot.Find("Story page/Narrative captions").GetComponent<TMPro.TMP_Text>();
            caption.ForceMeshUpdate(); Assert.That(caption.isTextOverflowing, Is.False);
            app.View.ShowLastReadingPart();
            var laterPhrase = app.View.Caption.Split(' ').TakeLast(5).ToArray();
            Assert.That(app.View.ReadingPart, Is.GreaterThan(0), "test a later source phrase, not only the first part");
            yield return Pinch(Panel("Always available controls"), "options");
            yield return Pinch(Panel("Return and recovery actions"), "table");
            Assert.That(app.View.CinematicReading, Is.False);
            Assert.That(paper.localScale, Is.EqualTo(oldScale));
            Assert.That(app.View.Caption, Does.Contain(string.Join(" ", laterPhrase)), "expanding the page retains the later source phrase");
            Assert.That(Panel("Turn a page").Ids, Does.Contain("sources"));
        }

        [UnityTest] public IEnumerator TheTravellerKeepsHerWholeFinalSentence()
        {
            yield return Water();
            app.TurnsPages = true;
            Assert.That(app.Session.Next(), Is.True);
            Assert.That(app.Session.Choose(1), Is.True); yield return null; yield return null;
            const string payoff = "Years from now, the traveller will remember that sound better than any bend, and she will be the one who is not quite sure where it was.";
            app.View.ShowLastReadingPart();
            Assert.That(app.View.Caption, Is.EqualTo(payoff), "the emotional payoff fits as one complete caption");
            app.View.SetLargeText(true); yield return null;
            app.View.ShowLastReadingPart();
            var caption = app.View.TableRoot.Find("Story page/Narrative captions").GetComponent<TMPro.TMP_Text>();
            caption.ForceMeshUpdate(); Assert.That(caption.isTextOverflowing, Is.False);
            Assert.That(app.View.Caption.Split(' ').Length, Is.GreaterThanOrEqualTo(8), "large text does not strand a five-word tail");
        }

        [UnityTest] public IEnumerator CreditsAreReachableAndCompleteWithoutLeavingTheStory()
        {
            yield return Water();
            var story = app.Session.Story; var chapter = app.Session.Chapter.Id;
            yield return Pinch(Panel("Always available controls"), "context");
            yield return Pinch(Panel("Return and recovery actions"), "credits");
            var pages = new List<string>();
            for (var i = 0; i < 60; i++)
            {
                pages.Add(app.View.ModalText);
                if (app.View.ModalPart + 1 == app.View.ModalPartCount) break;
                yield return Pinch(Panel("Return and recovery actions"), "modal-next");
            }
            var full = System.Text.RegularExpressions.Regex.Replace(string.Join(" ", pages), "\\s+", " ");
            Assert.That(full, Does.Contain("freesound.org/s/541572").And.Contain("creativecommons.org/licenses/by/3.0"));
            Assert.That(full, Does.Contain("Braille Institute").And.Contain("Red Hat").And.Contain("SIL OPEN FONT LICENSE"));
            Assert.That(full, Does.Contain("THE FONT SOFTWARE IS PROVIDED"), "the complete font notice, including its final terms, is accessible");
            yield return Pinch(Panel("Return and recovery actions"), "return");
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Reading));
            Assert.That(app.Session.Story, Is.SameAs(story)); Assert.That(app.Session.Chapter.Id, Is.EqualTo(chapter));
        }

        [UnityTest] public IEnumerator SpokenOpeningFitsItsExactCaptionWindows()
        {
            var windows = new[] { 4f, RiverThresholdPrototype.DisclosureAt - 4, RiverThresholdPrototype.TitleAt - RiverThresholdPrototype.DisclosureAt };
            for (var beat = 0; beat < 3; beat++)
            {
                var line = PersonalStoryVoice.Welcome[beat];
                var clip = Resources.Load<AudioClip>("Narration/personal-table/" + PersonalStoryVoice.ClipName(line));
                Assert.That(clip, Is.Not.Null);
                Assert.That(clip.length, Is.LessThanOrEqualTo(windows[beat]), "the next title must not cut the exact recording short");
                Assert.That(System.Text.RegularExpressions.Regex.Replace(river.OpeningWords, "\\s+", " "), Is.EqualTo(line));
                for (var i = 0; i < Mathf.RoundToInt(windows[beat] * 10); i++) river.Advance(.1f);
                river.Advance(.01f); yield return null;
            }
        }

        [UnityTest, Timeout(420000)] public IEnumerator CaptureACompleteVisitAtAuthoredPace()
        {
            if (Environment.GetEnvironmentVariable("SANKOFA_JUDGE_CAPTURE") != "1") Assert.Ignore("Opt-in complete desktop visit capture");
            var capture = new GameObject("Judge visit recorder").AddComponent<RiverJudgeRecorder>();
            capture.Begin(); Time.captureDeltaTime = 1f / 24;
            river.AutomaticClock = true; app.TurnsPages = true;
            try
            {
                yield return WaitFor(() => river.Sequence.Phase == RiverThresholdPhase.Choosing, 75);
                capture.Still("01-choose");
                yield return Pinch(Panel("Interest choices"), "place-water");
                yield return WaitFor(() => river.Sequence.Phase == RiverThresholdPhase.Riverside, 30);
                yield return new WaitForSeconds(2); capture.Still("02-river");
                yield return Pinch(Panel("Always available controls"), "pause");
                Assert.That(app.Session.Paused, Is.True); yield return new WaitForSeconds(1);
                yield return Pinch(Panel("Always available controls"), "pause");
                yield return WaitFor(() => app.Session.OffersChoice && app.PageToldOnce, 120);
                capture.Still("03-perspective-choice");
                yield return ChooseAtFigure("place-water.follow.remembers");
                Assert.That(app.Session.FollowedByChoice, Is.True);
                var first = app.Session.Chapter.Id;
                yield return WaitFor(() => app.OtherSideOffered, 100);
                capture.Still("04-first-side");
                yield return ChooseAtFigure("place-water.follow.traveller");
                Assert.That(app.Session.Chapter.Id, Is.Not.EqualTo(first));
                yield return WaitFor(() => app.PageToldOnce, 100); capture.Still("05-other-side");
                yield return WaitFor(() => app.Session.State == PersonalStoryState.Closing, 60);
                capture.Still("06-ending");
                var exact = PersonalStoryJson.Write(app.Session.Story);
                yield return Pinch(Panel("Turn a page"), "save");
                Assert.That(app.HasKept, Is.True);
                Assert.That(Panel("Turn a page").Ids, Does.Not.Contain("save"));
                yield return new WaitForSeconds(1.5f); capture.Still("06b-kept"); yield return null; yield return null;
                yield return Pinch(Panel("Always available controls"), "context");
                Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Sources));
                yield return Pinch(Panel("Return and recovery actions"), "continue");
                Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Closing));
                Assert.That(app.View.Caption, Does.StartWith("Kept on this device."));
                Assert.That(Panel("Turn a page").Ids, Does.Not.Contain("save"), "the successful-save receipt survives context");
                yield return Pinch(Panel("Turn a page"), "again");
                yield return Pinch(Panel("Kept story choices"), "reopen");
                Assert.That(PersonalStoryJson.Write(app.Session.Story), Is.EqualTo(exact));
                yield return WaitFor(() => river.Sequence.Phase == RiverThresholdPhase.Riverside, 30);
                yield return new WaitForSeconds(2); capture.Still("07-exact-reopen"); yield return null; yield return null;
            }
            finally { capture.Finish(); Object.Destroy(capture.gameObject); Time.captureDeltaTime = 0; }
        }
    }

    [DefaultExecutionOrder(1100)]
    public sealed class RiverReviewFrame : MonoBehaviour
    {
        public Action Capture;
        void LateUpdate() { var take = Capture; Capture = null; take?.Invoke(); }
    }

    /// <summary>Test-only mono recorder. Fixed simulation time, silent frames; no performance/hardware claim.</summary>
    [DefaultExecutionOrder(1000)]
    public sealed class RiverJudgeRecorder : MonoBehaviour
    {
        Camera eye;
        RenderTexture target;
        Texture2D pixels;
        System.Diagnostics.Process encoder;
        string folder;
        string pendingStill;
        int frames;
        public void Begin()
        {
            folder = Path.GetFullPath(Environment.GetEnvironmentVariable("SANKOFA_JUDGE_OUTPUT")
                ?? Path.Combine(Application.dataPath, "../../artifacts/judge-visit")); Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "milestones.txt"), "Seconds in silent fixed-clock desktop video\n");
            var ffmpeg = Environment.GetEnvironmentVariable("SANKOFA_FFMPEG"); Assert.That(File.Exists(ffmpeg), Is.True);
            eye = gameObject.AddComponent<Camera>(); eye.enabled = false; eye.stereoTargetEye = StereoTargetEyeMask.None;
            eye.transform.SetPositionAndRotation(new Vector3(0, 1.15f, 0), Quaternion.Euler(8, 0, 0));
            eye.fieldOfView = 60; eye.nearClipPlane = .03f; eye.clearFlags = CameraClearFlags.SolidColor; eye.backgroundColor = new Color(.008f, .012f, .022f);
            target = new RenderTexture(1600, 900, 24) { antiAliasing = 4 }; eye.targetTexture = target;
            pixels = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            encoder = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = ffmpeg, UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true,
                Arguments = "-v error -y -f rawvideo -pix_fmt rgb24 -s 1600x900 -r 24 -i - -vf vflip -an -c:v libx264 -preset fast -crf 20 -pix_fmt yuv420p \"" + Path.Combine(folder, "complete-painted-visit.mp4") + "\""
            });
        }
        void Render()
        {
            var previous = RenderTexture.active; eye.Render(); RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); pixels.Apply(); RenderTexture.active = previous;
        }
        void LateUpdate()
        {
            if (encoder == null) return;
            Render(); var bytes = pixels.GetRawTextureData<byte>().ToArray(); encoder.StandardInput.BaseStream.Write(bytes, 0, bytes.Length);
            if (pendingStill != null)
            {
                File.WriteAllBytes(Path.Combine(folder, pendingStill + ".png"), pixels.EncodeToPNG());
                File.AppendAllText(Path.Combine(folder, "milestones.txt"), pendingStill + ": " + frames / 24f + "\n");
                pendingStill = null;
            }
            frames++;
        }
        // Capture after all presentation LateUpdates, never the intermediate state seen by a resumed test coroutine.
        public void Still(string name) { pendingStill = name; }
        public void Finish()
        {
            if (encoder == null) return;
            encoder.StandardInput.Close();
            if (!encoder.WaitForExit(15000)) { encoder.Kill(); Assert.Fail("Judge visit encoder did not finish"); }
            var exit = encoder.ExitCode; encoder.Dispose(); encoder = null;
            File.WriteAllText(Path.Combine(folder, "capture.txt"), "Actual Unity painted river, synthetic single-pointer pinch actions, complete authored caption/stage timing.\nSilent mono desktop render; 24 fps fixed simulation clock, not measured performance or headset evidence.\nFrames: " + frames + "; duration seconds: " + frames / 24f);
            target.Release(); Object.Destroy(target); Object.Destroy(pixels); Assert.That(exit, Is.Zero);
        }
        void OnDestroy() { Finish(); }
    }
}
