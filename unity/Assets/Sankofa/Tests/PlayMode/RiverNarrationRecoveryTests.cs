using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
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
    /// <summary>Active local narration clock with output muted; validates recovery, not listening quality.</summary>
    public sealed class RiverNarrationRecoveryTests
    {
        PersonalStoryApp app;
        RiverThresholdPrototype river;
        AudioSource voice;
        string shelf;
        float listenerVolume;
        readonly List<string> errors = new List<string>();

        [UnitySetUp] public IEnumerator Load()
        {
            listenerVolume = AudioListener.volume;
            AudioListener.volume = 0;
            errors.Clear(); LogAssert.ignoreFailingMessages = true; Application.logMessageReceived += Error;
            shelf = Path.Combine(Path.GetTempPath(), "sankofa-narration-recovery-" + Guid.NewGuid().ToString("N"));
            PersonalStoryApp.StoreFolderOverride = shelf;
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/Sankofa/Scenes/RiverThresholdPainted.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene("RiverThresholdPainted");
#endif
            yield return null; yield return null;
            river = Object.FindFirstObjectByType<RiverThresholdPrototype>();
            river.AutomaticClock = false; app = river.App; app.ServiceEndpoint = ""; app.TurnsPages = true;
            voice = (AudioSource)typeof(PersonalStoryApp).GetField("voice", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(app);
            foreach (var pointer in Object.FindObjectsByType<PointerSource>(FindObjectsSortMode.None)) pointer.enabled = false;
            yield return Press(river.Controls, "primary");
            if (!app.NarrationEnabled)
            {
                yield return Press(Panel("Always available controls"), "options");
                yield return Press(Panel("Return and recovery actions"), "listen");
            }
            yield return Water();
        }

        [UnityTearDown] public IEnumerator Clean()
        {
            var scene = SceneManager.GetSceneByName("RiverThresholdPainted");
            if (scene.IsValid() && scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Narration recovery cleanup " + Guid.NewGuid().ToString("N")));
                yield return SceneManager.UnloadSceneAsync(scene);
            }
            PersonalStoryApp.StoreFolderOverride = null;
            if (Directory.Exists(shelf)) Directory.Delete(shelf, true);
            AudioListener.volume = listenerVolume;
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
        IEnumerator Press(PokePanel panel, string id)
        {
            Assert.That(panel.isActiveAndEnabled && !panel.Hidden, Is.True, panel.name);
            Assert.That(panel.Ids, Does.Contain(id));
            yield return new WaitForSecondsRealtime(.34f);
            panel.PressFromPointer(id); yield return null; yield return null;
        }
        IEnumerator Water()
        {
            yield return Press(Panel("Interest choices"), "place-water");
            for (var i = 0; i < 62; i++) river.Advance(.1f);
            yield return null; yield return null;
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Reading));
            Assert.That(voice.clip, Is.Not.Null);
            Assert.That(voice.isPlaying, Is.True, "This regression needs an active narration clock even though its output is muted.");
        }
        IEnumerator SeekIntoPage()
        {
            voice.timeSamples = voice.clip.samples / 2;
            yield return null; yield return null;
            Assert.That(app.View.ReadingPart, Is.GreaterThan(0), "Exercise a later caption, not the first words.");
            Assert.That(app.PageToldOnce, Is.False);
        }

        [UnityTest] public IEnumerator ContextReturnsToTheSameRecordingPositionCaptionAndNarrativeBeat()
        {
            yield return SeekIntoPage();
            var clip = voice.clip; var seconds = voice.time; var caption = app.View.Caption;
            var progress = app.View.Stage.Progress;
            app.View.ContextRequested?.Invoke();
            Assert.That(voice.isPlaying, Is.False);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(app.View.Stage.Progress, Is.EqualTo(progress).Within(.005f), "The narrative clock waits behind context.");
            app.View.DecisionSelected?.Invoke("return");
            Assert.That(voice.clip, Is.SameAs(clip));
            Assert.That(voice.time, Is.EqualTo(seconds).Within(.12f), "The voice resumes rather than starting at zero under a later caption.");
            Assert.That(app.View.Caption, Is.EqualTo(caption));
            Assert.That(app.View.Stage.Progress, Is.EqualTo(progress).Within(.005f));
            yield return null; yield return null;
            Assert.That(voice.isPlaying, Is.True);

            // Completion still owns the reveal boundary after resuming through the notice.
            voice.timeSamples = clip.samples - Mathf.CeilToInt(clip.frequency * .12f);
            var until = Time.realtimeSinceStartup + 3;
            while (!app.PageToldOnce && Time.realtimeSinceStartup < until) yield return null;
            Assert.That(app.PageToldOnce, Is.True, "Resuming must keep the narration-completed signal alive.");
        }

        [UnityTest] public IEnumerator SourceAndQuestionDetailsReturnWithoutLosingTheUnderlyingRecording()
        {
            yield return SeekIntoPage();
            app.TurnsPages = false; // Manual reading makes the existing page questions available before its voice ends.
            var clip = voice.clip; var seconds = voice.time; var caption = app.View.Caption;
            app.View.ContextRequested?.Invoke();
            app.View.DecisionSelected?.Invoke("question:0");
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Question));
            app.View.SourceDetailsRequested?.Invoke();
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Sources));
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(voice.isPlaying, Is.False);
            app.View.ContinueRequested?.Invoke();
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Question));
            app.View.ContinueRequested?.Invoke();
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Reading));
            Assert.That(voice.clip, Is.SameAs(clip));
            Assert.That(voice.time, Is.EqualTo(seconds).Within(.12f));
            Assert.That(app.View.Caption, Is.EqualTo(caption));
            yield return null; yield return null;
            Assert.That(voice.isPlaying, Is.True);
        }

        [UnityTest] public IEnumerator ConfirmedLeaveClearsTheCheckpointBeforeANewVisit()
        {
            yield return SeekIntoPage();
            var version = app.Session.RequestVersion;
            app.View.ContextRequested?.Invoke();
            app.View.LeaveRequested?.Invoke();
            app.View.DecisionSelected?.Invoke("leave");
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Intake));
            river.Advance(.1f); yield return null; yield return null;
            yield return Water();
            Assert.That(app.Session.RequestVersion, Is.GreaterThan(version));
            Assert.That(voice.time, Is.LessThan(1), "The new visit cannot inherit the old page's narration checkpoint.");
            Assert.That(app.View.ReadingPart, Is.Zero);
        }

        [UnityTest] public IEnumerator CaptionsOnlyContinuesFromTheVoicedBeatAndStillFinishesThePage()
        {
            yield return SeekIntoPage();
            var progress = app.View.Stage.Progress; var caption = app.View.Caption;
            app.View.ComfortRequested?.Invoke();
            app.View.DecisionSelected?.Invoke("listen");
            Assert.That(app.NarrationEnabled, Is.False);
            Assert.That(voice.isPlaying, Is.False);
            Assert.That(app.View.Stage.Progress, Is.EqualTo(progress).Within(.005f));
            Assert.That(app.View.Caption, Is.EqualTo(caption));
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(app.View.Stage.Progress, Is.GreaterThan(progress), "The stage must not remain owned by a stopped recording.");
            app.View.Stage.Seek(.995f);
            var until = Time.realtimeSinceStartup + 3;
            while (!app.PageToldOnce && Time.realtimeSinceStartup < until) yield return null;
            Assert.That(app.PageToldOnce, Is.True, "Captions-only mode still reaches its normal completion boundary.");
            Assert.That(app.View.ReadingPart, Is.EqualTo(app.View.ReadingPartCount - 1));
        }

        [UnityTest] public IEnumerator PausedSourceReturnCannotCancelTheAcceptedStory()
        {
            yield return SeekIntoPage();
            var story = app.Session.Story; var version = app.Session.RequestVersion;
            var clip = voice.clip; var seconds = voice.time; var caption = app.View.Caption;
            app.View.ContextRequested?.Invoke();
            app.View.DecisionSelected?.Invoke("sources");
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Sources));
            app.Session.Pause(true);
            app.View.ContinueRequested?.Invoke();
            Assert.That(app.Session.Paused, Is.True);
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Sources));
            Assert.That(app.Session.Story, Is.SameAs(story));
            Assert.That(app.Session.RequestVersion, Is.EqualTo(version), "a blocked Return must not fall through to Cancel");
            Assert.That(voice.isPlaying, Is.False);
            app.Session.Pause(false);
            app.View.ContinueRequested?.Invoke();
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Reading));
            Assert.That(voice.clip, Is.SameAs(clip));
            Assert.That(voice.time, Is.EqualTo(seconds).Within(.12f));
            Assert.That(app.View.Caption, Is.EqualTo(caption));
        }

        [UnityTest] public IEnumerator PausedContextAndComfortDecisionsCloseTheirModalWithoutResuming()
        {
            yield return SeekIntoPage();
            var clip = voice.clip; var seconds = voice.time; var progress = app.View.Stage.Progress;
            app.View.ContextRequested?.Invoke();
            app.Session.Pause(true);
            app.View.DecisionSelected?.Invoke("return");
            Assert.That(Panel("Return and recovery actions").Hidden, Is.True, "the handled context modal is retired while the story stays paused");
            Assert.That(app.Session.Paused, Is.True);
            Assert.That(voice.isPlaying, Is.False);

            var height = app.View.TableRoot.position.y;
            app.View.ComfortRequested?.Invoke();
            app.View.DecisionSelected?.Invoke("lower");
            Assert.That(app.View.TableRoot.position.y, Is.LessThan(height));
            Assert.That(Panel("Return and recovery actions").Hidden, Is.True);
            Assert.That(app.Session.Paused, Is.True);
            // Options can be opened and answered again; its old buttons are not left without a handler.
            app.View.ComfortRequested?.Invoke();
            Assert.That(Panel("Return and recovery actions").Hidden, Is.False);
            app.View.DecisionSelected?.Invoke("return");
            Assert.That(Panel("Return and recovery actions").Hidden, Is.True);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(app.View.Stage.Progress, Is.EqualTo(progress).Within(.005f));
            Assert.That(voice.isPlaying, Is.False);
            app.Session.Pause(false);
            Assert.That(voice.clip, Is.SameAs(clip));
            Assert.That(voice.time, Is.EqualTo(seconds).Within(.12f));
            yield return null; yield return null;
            Assert.That(voice.isPlaying, Is.True);
        }
    }
}
