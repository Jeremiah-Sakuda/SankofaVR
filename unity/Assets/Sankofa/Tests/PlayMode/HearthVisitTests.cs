using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sankofa.Core.Story;
using Sankofa.Flow;
using Sankofa.Runtime.App;
using Sankofa.Runtime.Stage;
using Sankofa.Runtime.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Sankofa.Tests
{
    /// <summary>
    /// Runs the real hearth scene in Play mode without a headset: composition, cue dispatch, staging, transitions
    /// and kept visits work together. Commands go straight to the flow because there are no tracked hands, and time
    /// runs faster than real. This is software integration evidence, not simulator or device evidence.
    /// </summary>
    public class HearthVisitTests
    {
        string folder;
        List<string> problems;
        Application.LogCallback watch;
        SankofaApp app;
        StoryStage stage;

        [SetUp]
        public void KeepVisitsInATemporaryFolder()
        {
            folder = Path.Combine(Path.GetTempPath(), "sankofa-playmode-" + Guid.NewGuid().ToString("N"));
            SankofaApp.StoreFolderOverride = folder;
            // Tests run faster than real time; keep their sped-up voice out of the developer's speakers.
            AudioListener.volume = 0;
            // Without a headset the XR runtime and Meta's plugin log their own errors; Sankofa's must not.
            LogAssert.ignoreFailingMessages = true;
            problems = new List<string>();
            watch = (message, stack, type) =>
            {
                if ((type == LogType.Exception || type == LogType.Error || type == LogType.Assert) &&
                    (message.Contains("[Sankofa]") || (stack ?? "").Contains("Sankofa.")))
                    problems.Add(message);
            };
            Application.logMessageReceived += watch;
        }

        [TearDown]
        public void CleanUp()
        {
            Time.timeScale = 1;
            AudioListener.volume = 1;
            Application.logMessageReceived -= watch;
            SankofaApp.StoreFolderOverride = null;
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }

        static IEnumerator WaitFor(Func<bool> condition, float seconds, string what)
        {
            var end = Time.realtimeSinceStartup + seconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > end) Assert.Fail("Timed out waiting for " + what);
                yield return null;
            }
        }

        IEnumerator Open(float pace)
        {
            SceneManager.LoadScene("Hearth");
            yield return null;
            app = Object.FindFirstObjectByType<SankofaApp>();
            Assert.That(app.Flow, Is.Not.Null, app.LoadProblem);
            stage = Object.FindFirstObjectByType<StoryStage>();
            Time.timeScale = pace;
            // The start screen waits: nothing is spoken and no visit begins until the visitor presses Begin.
            yield return WaitFor(() => app.AtStart, 10, "the start screen");
            yield return new WaitForSeconds(1f);
            Assert.That(app.Flow.Phase, Is.EqualTo(VisitPhase.Idle));
            Assert.That(app.Flow.CurrentLine, Is.Null);
            app.Begin();
            Assert.That(app.AtStart, Is.False);
            yield return WaitFor(() => app.Flow.Phase == VisitPhase.Welcome || app.Flow.Phase == VisitPhase.Choosing, 10, "the hearth");
        }

        /// <summary>
        /// A fingertip pressing the panel button with this label: it approaches from in front, then pushes through
        /// the face. The same path a tracked hand takes, through the panel's own poke detection.
        /// </summary>
        static IEnumerator Poke(string label)
        {
            PokePanel panel = null;
            Transform button = null;
            yield return WaitFor(() =>
            {
                foreach (var p in Object.FindObjectsByType<PokePanel>(FindObjectsSortMode.None))
                    foreach (var t in p.GetComponentsInChildren<TMPro.TextMeshPro>())
                        if (t.text == label)
                        {
                            panel = p;
                            button = t.transform.parent;
                            return true;
                        }
                return false;
            }, 20, "a button labelled " + label);
            var depth = 0.06f;
            panel.AddFingertip(() => button != null ? button.TransformPoint(new Vector3(0, 0, -depth)) : (Vector3?)null);
            for (var i = 0; i < 6; i++) yield return null;
            depth = -0.01f;
            for (var i = 0; i < 6; i++) yield return null;
            depth = 0.06f;
        }

        [UnityTest]
        public IEnumerator AFingertipBeginsAVisitAndChoosesAnEra()
        {
            SceneManager.LoadScene("Hearth");
            yield return null;
            app = Object.FindFirstObjectByType<SankofaApp>();
            Assert.That(app.Flow, Is.Not.Null, app.LoadProblem);
            yield return WaitFor(() => app.AtStart, 10, "the start screen");
            // Begin and the era are chosen only by a fingertip on the panels, never by calling the flow.
            yield return Poke("Begin");
            yield return WaitFor(() => !app.AtStart, 5, "Begin pressed by a fingertip");
            Time.timeScale = 8;
            yield return WaitFor(() => app.Flow.Phase == VisitPhase.Choosing, 60, "the era choice");
            Time.timeScale = 1;
            yield return Poke("Before the railway");
            yield return WaitFor(() => app.Flow.Phase != VisitPhase.Choosing, 5, "an era chosen by a fingertip");
            // Captions draw over the scene, so no figure, post or tree can cover a line.
            var board = Object.FindFirstObjectByType<CaptionBoard>();
            Assert.That(board.CaptionText.fontSharedMaterial.GetInt("unity_GUIZTestMode"), Is.EqualTo((int)UnityEngine.Rendering.CompareFunction.Always));
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        /// <summary>
        /// A right hand held open and palm up, its palm a little under a point: the pose the receive gesture asks
        /// for, built from joint positions as the hand skeleton reports them.
        /// </summary>
        static Sankofa.Interaction.HandSample OpenPalmUnder(Vector3 point)
        {
            System.Numerics.Vector3 N(Vector3 v) => new System.Numerics.Vector3(v.x, v.y, v.z);
            var c = point + Vector3.down * 0.1f;
            return new Sankofa.Interaction.HandSample
            {
                Wrist = N(c + new Vector3(0, 0, -0.045f)),
                MiddleProximal = N(c + new Vector3(0, 0, 0.045f)),
                IndexProximal = N(c + new Vector3(0.025f, 0, 0.04f)),
                LittleProximal = N(c + new Vector3(-0.03f, 0, 0.035f)),
                IndexTip = N(c + new Vector3(0.03f, 0, 0.12f)),
                MiddleTip = N(c + new Vector3(0, 0, 0.13f)),
                RingTip = N(c + new Vector3(-0.015f, 0, 0.125f)),
            };
        }

        [UnityTest]
        public IEnumerator AnOpenPalmReceivesTheEmberButNotWhilePaused()
        {
            yield return Open(4);
            Assert.That(app.Flow.Choose("era.1850s"), Is.True);
            yield return AwaitInvitation("receive-ember");
            Time.timeScale = 1;
            var ember = stage.Find("ember");
            yield return new WaitForSeconds(1.5f);
            Assert.That(ember.Visible, Is.True, "the ember is offered");
            var right = Object.FindObjectsByType<Sankofa.Runtime.Input.HandSource>(FindObjectsSortMode.None).First(h => !h.IsLeft);

            // Paused, a palm held under the ember takes nothing and shows nothing; the gesture is not spent.
            Assert.That(app.Flow.Pause(), Is.True);
            yield return null;
            right.Simulated = () => OpenPalmUnder(ember.transform.position);
            yield return new WaitForSeconds(1.5f);
            Assert.That(app.Flow.Phase, Is.EqualTo(VisitPhase.Paused));
            Assert.That(app.Flow.OpenInvitation?.Id, Is.EqualTo("receive-ember"), "nothing was taken while paused");
            Assert.That(ember.Feedback, Is.EqualTo(0f), "no feedback while paused");
            Assert.That(ember.CarriedBy, Is.Null);

            // Resumed, the same open palm takes it, through the gesture, into that hand.
            Assert.That(app.Flow.Resume(), Is.True);
            yield return WaitFor(() => ember.CarriedBy == right, 10, "the ember received by the open palm");
            right.Simulated = null;
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        IEnumerator AwaitInvitation(string id, float seconds = 30) =>
            WaitFor(() => app.Flow.OpenInvitation?.Id == id, seconds, "invitation " + id);

        IEnumerator AwaitClosing(float seconds = 60) =>
            WaitFor(() => app.Flow.Phase == VisitPhase.Closing && app.Flow.CurrentLine == null, seconds, "the closing at the hearth");

        [UnityTest]
        public IEnumerator AVisitBeforeTheRailwayIsPlayedKeptContinuedAndReplayed()
        {
            yield return Open(1);
            var captions = Object.FindFirstObjectByType<CaptionBoard>();
            yield return null;
            Assert.That(captions.Caption, Is.EqualTo("Come. Sit with me."));
            Assert.That(Object.FindFirstObjectByType<Sankofa.Runtime.Narration.NarrationPlayer>().CaptionOnly, Is.False, "the griot speaks with its recorded voice");
            // Accessibility: a medium caption line subtends at least a degree from a seated eye.
            var eye = Object.FindFirstObjectByType<WorldAnchor>().transform.TransformPoint(new Vector3(0, 1.15f, 0));
            var line = captions.CaptionText.GetPreferredValues("Hg").y * captions.CaptionText.transform.lossyScale.y;
            var degrees = Mathf.Atan2(line, Vector3.Distance(eye, captions.CaptionText.transform.position)) * Mathf.Rad2Deg;
            Assert.That(degrees, Is.GreaterThanOrEqualTo(0.95f), $"caption line is {line * 100:0.0} cm, {degrees:0.00} degrees");
            Assert.That(captions.Status, Does.Contain("not yet reviewed"));
            // The night is audible: every ambience source has a recorded loop that is not silent.
            foreach (var source in Object.FindFirstObjectByType<Sankofa.Runtime.Audio.NightAmbience>().GetComponentsInChildren<AudioSource>())
            {
                Assert.That(source.clip, Is.Not.Null, source.name);
                var data = new float[source.clip.samples];
                source.clip.GetData(data, 0);
                var energy = data.Sum(s => (double)s * s);
                Assert.That(Math.Sqrt(energy / data.Length), Is.GreaterThan(1e-3), source.name + " is silent");
            }
            Assert.That(stage.ActiveScene, Is.EqualTo("hearth"));
            Time.timeScale = 8;

            // Before the railway: the ember, the water, the flour, a question, the offer and the bead.
            Assert.That(app.Flow.Choose("era.1850s"), Is.True);
            yield return AwaitInvitation("receive-ember");
            yield return new WaitForSeconds(1.5f);
            Assert.That(stage.Find("ember").Visible, Is.True, "the offered ember is shown");
            Assert.That(app.Flow.Accept("receive-ember"), Is.True);
            yield return WaitFor(() => stage.ActiveScene == "story.water.1850s" && app.Flow.Phase == VisitPhase.Story, 60, "the water before the railway");
            Assert.That(stage.Find("fire.1850s").Visible, Is.True, "the ember continued as the traders' fire");
            Assert.That(stage.Find("ember").Visible, Is.False);
            Assert.That(Object.FindObjectsByType<Sankofa.Runtime.Audio.StoryAmbience>(FindObjectsSortMode.None).Length, Is.EqualTo(1), "only the shown scene's sound plays");

            yield return AwaitInvitation("touch-water");
            Assert.That(app.Flow.Accept("touch-water"), Is.True);
            yield return AwaitInvitation("receive-flour", 60);
            // Wambui came down carrying the flour and holds it out from her own hands.
            var giver = stage.Find("figure.wambui").GetComponent<FigureMotion>();
            yield return WaitFor(() => Mathf.Max(giver.Arms.Negative, giver.Arms.Positive) > 0.6f, 10, "Wambui holding out the flour");
            Assert.That(app.Flow.Accept("receive-flour"), Is.True);
            yield return WaitFor(() => app.Flow.Questions.Any(q => q.Id == "q.e1.friends"), 60, "a question about the traders");
            Assert.That(app.Flow.Ask("q.e1.friends"), Is.True);
            yield return WaitFor(() => app.Flow.Answering == null, 60, "the answer");
            yield return AwaitInvitation("offer-flour", 60);
            Assert.That(app.Flow.Accept("offer-flour"), Is.True);
            yield return new WaitForSeconds(2.5f);
            Assert.That(Vector3.Distance(stage.Find("gourd.flour").transform.position, stage.Find("hide").PlacePosition), Is.LessThan(0.02f), "the flour sits on the hide");
            // The two women came to the hide for the trade and knelt there, one each side.
            var wambui = stage.Find("figure.wambui").GetComponent<FigureMotion>();
            var naserian = stage.Find("figure.naserian").GetComponent<FigureMotion>();
            Assert.That(wambui.Walking || naserian.Walking, Is.False, "both have arrived at the hide");
            Assert.That(wambui.Kneel, Is.GreaterThan(0.9f), "Wambui kneels at the hide");
            Assert.That(naserian.Kneel, Is.GreaterThan(0.9f), "Naserian kneels at the hide");
            Assert.That(Vector3.Distance(wambui.transform.position, stage.Find("hide").ApproachFrom(stage.Find("figure.wambui").HomePosition).Place), Is.LessThan(0.03f));
            Assert.That(Vector3.Distance(wambui.transform.position, naserian.transform.position), Is.GreaterThan(0.4f), "they take a side each");
            yield return AwaitInvitation("receive-bead", 60);
            yield return WaitFor(() => Mathf.Max(naserian.Arms.Negative, naserian.Arms.Positive) > 0.6f, 10, "Naserian holding out the bead");
            // She keeps her distance; the bead leaves her hand and drifts to just in front of the visitor, within reach.
            yield return WaitFor(() => Vector3.Distance(stage.Find("bead").transform.position, stage.GiftPoint(stage.Find("bead").HandOverPlace.Value)) < 0.05f, 15, "the bead drifting to the visitor");
            Assert.That(Vector3.Distance(naserian.transform.position, Camera.main.transform.position), Is.GreaterThan(1f), "the giver stays out of the visitor's space");
            Assert.That(app.Flow.Accept("receive-bead"), Is.True);
            yield return AwaitClosing(90);
            Assert.That(stage.ActiveScene, Is.EqualTo("hearth"));
            yield return new WaitForSeconds(1f);
            Assert.That(stage.Find("bead.kept").Visible, Is.True, "the bead came home");
            // While the visitor decides whether to keep the visit, the bead waits in plain view beside the choice.
            var view = Camera.main.transform;
            var flat = Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized;
            var toBead = Vector3.ProjectOnPlane(stage.Find("bead.kept").transform.position - view.position, Vector3.up);
            Assert.That(Vector3.Angle(flat, toBead), Is.LessThan(25f), "the keepsake waits in front of the visitor");
            Assert.That(stage.Find("ember").Visible, Is.False, "the fire took its ember back");
            Assert.That(app.Flow.Journal.Completed, Is.True);

            // Kept on this device, then continued as a new child visit to the same water today.
            Assert.That(app.Flow.Keep(), Is.True);
            Assert.That(Directory.GetFiles(folder, "*.json").Length, Is.EqualTo(1));
            var kept = app.Flow.KeptVisit;
            Assert.That(app.Flow.BeginAgain(), Is.True);
            yield return null;
            Assert.That(app.Flow.Menu, Is.EqualTo(ChoiceMenu.Returning));
            Assert.That(app.Flow.Follow(), Is.True);
            yield return AwaitInvitation("hang-bead", 90);
            Assert.That(stage.ActiveScene, Is.EqualTo("story.water.today"));
            Assert.That(stage.Find("jiko").Visible, Is.True, "the ember continued as the jiko's coal");
            Assert.That(stage.Find("bead.today").Visible, Is.True, "the kept bead came along");
            Assert.That(app.Flow.Accept("hang-bead"), Is.True);
            yield return new WaitForSeconds(2.5f);
            Assert.That(Vector3.Distance(stage.Find("bead.today").transform.position, stage.Find("gate").PlacePosition), Is.LessThan(0.02f), "the bead hangs on the gate");
            yield return AwaitClosing(90);
            Assert.That(app.Flow.Plan.Plan.Parent, Is.EqualTo(kept.Id));

            // The kept visit, replayed exactly as it was.
            Assert.That(app.Flow.BeginAgain(), Is.True);
            yield return null;
            Assert.That(app.Flow.Replay(), Is.True);
            yield return AwaitClosing(120);
            Assert.That(app.Flow.Journal.Mode, Is.EqualTo(VisitMode.Replayed));
            Assert.That(app.Flow.Journal.Outcomes, Is.EquivalentTo(kept.Outcomes));
            Assert.That(app.Flow.Journal.Heard.Where(id => !id.StartsWith("q.")), Is.EqualTo(kept.Heard.Where(id => !id.StartsWith("q."))),
                "every story line as it was heard");
            Assert.That(problems, Is.Empty);
        }

        [UnityTest]
        public IEnumerator APassiveVisitWhenTheRailwayCameCompletesAndReturns()
        {
            yield return Open(8);
            Assert.That(app.Flow.Choose("era.1899"), Is.True);
            yield return WaitFor(() => stage.ActiveScene == "story.water.1899" && app.Flow.Phase == VisitPhase.Story, 60, "the depot camp");
            Assert.That(stage.Find("lantern.1899").Visible, Is.True, "the ember continued as the camp lantern");
            yield return AwaitInvitation("touch-rail", 60);
            Assert.That(stage.Find("rails").Visible, Is.True);
            yield return AwaitClosing(150);
            yield return new WaitForSeconds(1f);
            Assert.That(stage.ActiveScene, Is.EqualTo("hearth"));
            Assert.That(stage.Find("seed.kept").Visible, Is.True, "the seed Njeri set beside you came home");
            Assert.That(app.Flow.Journal.Outcomes.Values.Distinct(), Is.EqualTo(new[] { Outcome.Default }), "no choice was claimed");
            Assert.That(app.Flow.Controls.Select(c => c.Kind), Has.Member(ControlKind.Keep));
            Assert.That(problems, Is.Empty);
        }

        [UnityTest]
        public IEnumerator ReturningEarlyFromTheStoryEndsAtTheHearthWithoutKeeping()
        {
            yield return Open(8);
            Assert.That(app.Flow.Choose("era.1850s"), Is.True);
            yield return WaitFor(() => stage.ActiveScene == "story.water.1850s" && app.Flow.Phase == VisitPhase.Story, 60, "the water before the railway");
            Assert.That(app.Flow.ReturnToHearth(), Is.True);
            yield return AwaitClosing();
            Assert.That(stage.ActiveScene, Is.EqualTo("hearth"));
            Assert.That(app.Flow.Journal.ReturnedEarly, Is.True);
            Assert.That(app.Flow.Controls.Select(c => c.Kind), Has.No.Member(ControlKind.Keep));
            Assert.That(problems, Is.Empty);
        }
    }
}
