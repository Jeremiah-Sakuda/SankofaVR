using System;
using System.Collections;
using System.Collections.Generic;
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
    /// Runs the real hearth scene in Play mode without a headset: the composition, cue dispatch, staging and
    /// transitions work together. Commands go straight to the flow because there are no tracked hands; this is
    /// software integration evidence, not simulator or device evidence.
    /// </summary>
    public class HearthVisitTests
    {
        static IEnumerator WaitFor(Func<bool> condition, float seconds, string what)
        {
            var end = Time.realtimeSinceStartup + seconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > end) Assert.Fail("Timed out waiting for " + what);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator AVisitPlaysThroughTheSceneAndReturnsToTheHearth()
        {
            // Without a headset the XR runtime and Meta's plugin log their own errors; Sankofa's must not.
            LogAssert.ignoreFailingMessages = true;
            var problems = new List<string>();
            Application.LogCallback watch = (message, stack, type) =>
            {
                if ((type == LogType.Exception || type == LogType.Error || type == LogType.Assert) &&
                    (message.Contains("[Sankofa]") || (stack ?? "").Contains("Sankofa.")))
                    problems.Add(message);
            };
            Application.logMessageReceived += watch;
            try
            {
                SceneManager.LoadScene("Hearth");
                yield return null;
                var app = Object.FindFirstObjectByType<SankofaApp>();
                Assert.That(app.Flow, Is.Not.Null, app.LoadProblem);
                var stage = Object.FindFirstObjectByType<StoryStage>();
                var captions = Object.FindFirstObjectByType<CaptionBoard>();

                yield return WaitFor(() => app.Flow.Phase == VisitPhase.Welcome, 10, "the welcome");
                yield return null;
                Assert.That(captions.Caption, Is.EqualTo("Come. Sit with me."));
                // Accessibility: a medium caption line subtends at least a degree from a seated eye.
                var eye = Object.FindFirstObjectByType<WorldAnchor>().transform.TransformPoint(new Vector3(0, 1.15f, 0));
                var line = captions.CaptionText.GetPreferredValues("Hg").y * captions.CaptionText.transform.lossyScale.y;
                var degrees = Mathf.Atan2(line, Vector3.Distance(eye, captions.CaptionText.transform.position)) * Mathf.Rad2Deg;
                Assert.That(degrees, Is.GreaterThanOrEqualTo(0.95f), $"caption line is {line * 100:0.0} cm, {degrees:0.00} degrees");
                Assert.That(captions.Status, Does.Contain("placeholder"));
                // The night is audible: every ambience source has a synthesised loop that is not silent.
                var ambience = Object.FindFirstObjectByType<Sankofa.Runtime.Audio.NightAmbience>();
                foreach (var source in ambience.GetComponentsInChildren<AudioSource>())
                {
                    Assert.That(source.clip, Is.Not.Null, source.name);
                    var data = new float[source.clip.samples];
                    source.clip.GetData(data, 0);
                    var energy = 0.0;
                    foreach (var s in data) energy += s * s;
                    Assert.That(Math.Sqrt(energy / data.Length), Is.GreaterThan(1e-3), source.name + " is silent");
                }
                Assert.That(stage.ActiveScene, Is.EqualTo("hearth"));

                Assert.That(app.Flow.Begin(), Is.True);
                yield return WaitFor(() => app.Flow.OpenInvitation != null, 20, "the ember to be offered");
                yield return new WaitForSeconds(1.5f);
                Assert.That(stage.Find("ember").Visible, Is.True, "the offered ember is shown");
                Assert.That(app.Flow.Accept("receive-ember"), Is.True);

                yield return WaitFor(() => stage.ActiveScene == "story.yard" && app.Flow.Phase == VisitPhase.Story, 40, "entry into the story yard");
                Assert.That(stage.Find("lantern").Visible, Is.True, "the ember continued as the lantern");
                Assert.That(stage.Find("ember").Visible, Is.False);

                Assert.That(app.Flow.ReturnToHearth(), Is.True);
                yield return WaitFor(() => app.Flow.Phase == VisitPhase.Closing && app.Flow.CurrentLine == null, 40, "the closing at the hearth");
                Assert.That(stage.ActiveScene, Is.EqualTo("hearth"));
                Assert.That(stage.Find("ember").Visible, Is.True, "the keepsake came home");
                Assert.That(app.Flow.Journal.ReturnedEarly, Is.True);
                Assert.That(problems, Is.Empty);
            }
            finally
            {
                Application.logMessageReceived -= watch;
            }
        }
    }
}
