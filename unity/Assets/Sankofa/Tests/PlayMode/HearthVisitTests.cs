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
            yield return WaitFor(() => app.Flow.Phase == VisitPhase.Welcome || app.Flow.Phase == VisitPhase.Choosing, 10, "the hearth");
        }

        IEnumerator AwaitInvitation(string id, float seconds = 30) =>
            WaitFor(() => app.Flow.OpenInvitation?.Id == id, seconds, "invitation " + id);

        IEnumerator AwaitClosing(float seconds = 60) =>
            WaitFor(() => app.Flow.Phase == VisitPhase.Closing && app.Flow.CurrentLine == null, seconds, "the closing at the hearth");

        [UnityTest]
        public IEnumerator AVisitIsChosenPlayedKeptContinuedAndReplayed()
        {
            yield return Open(1);
            var captions = Object.FindFirstObjectByType<CaptionBoard>();
            yield return null;
            Assert.That(captions.Caption, Is.EqualTo("Come. Sit with me."));
            // Accessibility: a medium caption line subtends at least a degree from a seated eye.
            var eye = Object.FindFirstObjectByType<WorldAnchor>().transform.TransformPoint(new Vector3(0, 1.15f, 0));
            var line = captions.CaptionText.GetPreferredValues("Hg").y * captions.CaptionText.transform.lossyScale.y;
            var degrees = Mathf.Atan2(line, Vector3.Distance(eye, captions.CaptionText.transform.position)) * Mathf.Rad2Deg;
            Assert.That(degrees, Is.GreaterThanOrEqualTo(0.95f), $"caption line is {line * 100:0.0} cm, {degrees:0.00} degrees");
            Assert.That(captions.Status, Does.Contain("placeholder"));
            // The night is audible: every ambience source has a synthesised loop that is not silent.
            foreach (var source in Object.FindFirstObjectByType<Sankofa.Runtime.Audio.NightAmbience>().GetComponentsInChildren<AudioSource>())
            {
                Assert.That(source.clip, Is.Not.Null, source.name);
                var data = new float[source.clip.samples];
                source.clip.GetData(data, 0);
                var energy = data.Sum(s => (double)s * s);
                Assert.That(Math.Sqrt(energy / data.Length), Is.GreaterThan(1e-3), source.name + " is silent");
            }
            Assert.That(stage.ActiveScene, Is.EqualTo("hearth"));
            Time.timeScale = 6;

            // Hands that make: the ember, a question, a touch and the piece of the bowl.
            Assert.That(app.Flow.Choose("makers"), Is.True);
            yield return AwaitInvitation("receive-ember");
            yield return new WaitForSeconds(1.5f);
            Assert.That(stage.Find("ember").Visible, Is.True, "the offered ember is shown");
            Assert.That(app.Flow.Accept("receive-ember"), Is.True);
            yield return WaitFor(() => stage.ActiveScene == "story.yard" && app.Flow.Phase == VisitPhase.Story, 40, "entry into the yard");
            Assert.That(stage.Find("lantern").Visible, Is.True, "the ember continued as the lantern");
            Assert.That(stage.Find("ember").Visible, Is.False);

            yield return AwaitInvitation("touch-bowl");
            Assert.That(app.Flow.Ask("q.bowl.maker"), Is.True);
            yield return WaitFor(() => app.Flow.Answering == null, 30, "the answer");
            Assert.That(app.Flow.Accept("touch-bowl"), Is.True);
            yield return AwaitInvitation("receive-shard");
            Assert.That(app.Flow.Accept("receive-shard"), Is.True);
            yield return AwaitClosing();
            Assert.That(stage.ActiveScene, Is.EqualTo("hearth"));
            yield return new WaitForSeconds(3f);
            var keepsake = stage.Find("shard.kept");
            Assert.That(keepsake.Visible, Is.True, "the piece came home");
            Assert.That(stage.Find("ember").Visible, Is.False, "the fire took its ember back");
            Assert.That(app.Flow.Journal.Completed, Is.True);

            // Kept on this device, then continued as a new child visit.
            Assert.That(app.Flow.Keep(), Is.True);
            Assert.That(Directory.GetFiles(folder, "*.json").Length, Is.EqualTo(1));
            var kept = app.Flow.KeptVisit;
            Assert.That(app.Flow.BeginAgain(), Is.True);
            yield return null;
            Assert.That(app.Flow.Menu, Is.EqualTo(ChoiceMenu.Returning));
            Assert.That(app.Flow.Follow(), Is.True);
            yield return AwaitInvitation("touch-mend", 60);
            Assert.That(stage.ActiveScene, Is.EqualTo("story.yard"));
            Assert.That(stage.Find("shard").Visible, Is.True, "the kept piece came along");
            Assert.That(app.Flow.Accept("touch-mend"), Is.True);
            yield return new WaitForSeconds(2f);
            var bowl = stage.Find("bowl");
            Assert.That(Vector3.Distance(stage.Find("shard").transform.position, bowl.PlacePosition), Is.LessThan(0.01f), "the piece sits in its chip");
            yield return AwaitClosing();
            Assert.That(app.Flow.Plan.Plan.Parent, Is.EqualTo(kept.Id));

            // The kept visit, replayed exactly as it was.
            Assert.That(app.Flow.BeginAgain(), Is.True);
            yield return null;
            Assert.That(app.Flow.Replay(), Is.True);
            yield return AwaitClosing(90);
            Assert.That(app.Flow.Journal.Mode, Is.EqualTo(VisitMode.Replayed));
            Assert.That(app.Flow.Journal.Outcomes, Is.EquivalentTo(kept.Outcomes));
            Assert.That(app.Flow.Journal.Heard.Where(id => !id.StartsWith("q.")), Is.EqualTo(kept.Heard.Where(id => !id.StartsWith("q."))),
                "every story line as it was heard");
            Assert.That(problems, Is.Empty);
        }

        [UnityTest]
        public IEnumerator APassiveVisitAlongTheRoadCompletesAndReturns()
        {
            yield return Open(8);
            Assert.That(app.Flow.Choose("travellers"), Is.True);
            yield return WaitFor(() => stage.ActiveScene == "story.road" && app.Flow.Phase == VisitPhase.Story, 60, "entry onto the road");
            Assert.That(stage.Find("road.lantern").Visible, Is.True, "the ember continued as the road's lantern");
            yield return AwaitInvitation("touch-marker", 60);
            Assert.That(stage.Find("waymarker").Visible, Is.True);
            yield return AwaitClosing(120);
            yield return new WaitForSeconds(3f);
            Assert.That(stage.ActiveScene, Is.EqualTo("hearth"));
            Assert.That(stage.Find("waystone.kept").Visible, Is.True, "the stone the fire carried came home");
            Assert.That(app.Flow.Journal.Outcomes.Values.Distinct(), Is.EqualTo(new[] { Outcome.Default }), "no choice was claimed");
            Assert.That(app.Flow.Controls.Select(c => c.Kind), Has.Member(ControlKind.Keep));
            Assert.That(problems, Is.Empty);
        }

        [UnityTest]
        public IEnumerator ReturningEarlyFromTheStoryEndsAtTheHearthWithoutKeeping()
        {
            yield return Open(8);
            Assert.That(app.Flow.Choose("makers"), Is.True);
            yield return WaitFor(() => stage.ActiveScene == "story.yard" && app.Flow.Phase == VisitPhase.Story, 60, "entry into the yard");
            Assert.That(app.Flow.ReturnToHearth(), Is.True);
            yield return AwaitClosing();
            Assert.That(stage.ActiveScene, Is.EqualTo("hearth"));
            Assert.That(app.Flow.Journal.ReturnedEarly, Is.True);
            Assert.That(app.Flow.Controls.Select(c => c.Kind), Has.No.Member(ControlKind.Keep));
            Assert.That(problems, Is.Empty);
        }
    }
}
