using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Sankofa.Core.Personal;
using Sankofa.Flow;
using Sankofa.Interaction;
using Sankofa.Runtime.Input;
using Sankofa.Runtime.Personal;
using Sankofa.Runtime.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Sankofa.Tests
{
    /// <summary>Actual painted scene and persistence with synthetic hands; no hardware or listening acceptance.</summary>
    public sealed class RiverKeepsakeTests
    {
        PersonalStoryApp app;
        RiverThresholdPrototype river;
        RiverKeepsakePresentation keepsake;
        string shelf;
        readonly List<string> errors = new List<string>();

        [UnitySetUp] public IEnumerator Load()
        {
            errors.Clear(); LogAssert.ignoreFailingMessages = true; Application.logMessageReceived += Error;
            shelf = Path.Combine(Path.GetTempPath(), "sankofa-keepsake-" + Guid.NewGuid().ToString("N"));
            PersonalStoryApp.StoreFolderOverride = shelf;
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/Sankofa/Scenes/RiverThresholdPainted.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene("RiverThresholdPainted");
#endif
            yield return null; yield return null;
            river = Object.FindFirstObjectByType<RiverThresholdPrototype>(); app = river.App;
            river.AutomaticClock = false; app.ServiceEndpoint = ""; app.TurnsPages = false;
            keepsake = Object.FindFirstObjectByType<RiverKeepsakePresentation>();
            Assert.That(keepsake, Is.Not.Null, "the selected painted scene includes its kept-story presentation");
            foreach (var pointer in Object.FindObjectsByType<PointerSource>(FindObjectsSortMode.None)) pointer.enabled = false;
        }

        [UnityTearDown] public IEnumerator Clean()
        {
            Time.timeScale = 1;
            foreach (var hand in Object.FindObjectsByType<HandSource>(FindObjectsSortMode.None)) hand.Simulated = null;
            var scene = SceneManager.GetSceneByName("RiverThresholdPainted");
            if (scene.IsValid() && scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Keepsake cleanup " + Guid.NewGuid().ToString("N")));
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
            Assert.That(panel.gameObject.activeInHierarchy && !panel.Hidden, Is.True, panel.name + " is visible");
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
        IEnumerator Water(bool opening = true)
        {
            if (opening) yield return Pinch(river.Controls, "primary");
            yield return Pinch(Panel("Interest choices"), "place-water");
            for (var i = 0; i < 62; i++) river.Advance(.1f);
            yield return null; yield return null;
            Assert.That(river.RiverVisible, Is.True);
        }
        IEnumerator FirstSide()
        {
            for (var i = 0; i < 12 && !app.Session.OffersChoice; i++) { Assert.That(app.Session.Next(), Is.True); yield return null; }
            Assert.That(app.Session.OffersChoice, Is.True);
            Assert.That(app.Session.Choose(0), Is.True); yield return null;
            for (var i = 0; i < 12 && !app.Session.OffersOtherSide; i++) { Assert.That(app.Session.Next(), Is.True); yield return null; }
            Assert.That(app.OtherSideOffered, Is.True);
        }
        IEnumerator BothSidesEnding()
        {
            yield return FirstSide(); Assert.That(app.Session.HearOtherSide(0, true), Is.True); yield return null;
            for (var i = 0; i < 12 && app.Session.State != PersonalStoryState.Closing; i++) { Assert.That(app.Session.Next(), Is.True); yield return null; }
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Closing)); yield return null;
        }

        [UnityTest] public IEnumerator OnlyExplicitSuccessfulKeepRevealsTheStrandAndReplayKeepsItsSetting()
        {
            var saved = 0; var reopened = 0; string reopenedInterest = null;
            app.StoryKept += () => saved++;
            app.KeptStoryReopened += interest => { reopened++; reopenedInterest = interest; };
            yield return Water(); var exact = PersonalStoryJson.Write(app.Session.Story);
            yield return BothSidesEnding();
            Assert.That(app.HasKept, Is.False); Assert.That(saved, Is.Zero); Assert.That(keepsake.Visible, Is.False);
            yield return Pinch(Panel("Turn a page"), "save");
            Assert.That(saved, Is.EqualTo(1)); Assert.That(File.ReadAllText(Path.Combine(shelf, "kept.story")), Is.EqualTo(exact));
            Assert.That(keepsake.Visible, Is.True); Assert.That(keepsake.ShownBeadCount, Is.EqualTo(2));
            Assert.That(keepsake.ShownSlotCount, Is.EqualTo(6), "the remaining small beads are empty strand positions, not kept sides");
            var shownBeads = keepsake.DisplayRoot.GetComponentsInChildren<Renderer>().Where(r => r.name.StartsWith("Bead ", StringComparison.Ordinal)).ToArray();
            Assert.That(shownBeads.Length, Is.EqualTo(6));
            Assert.That(shownBeads.All(r => r.enabled), Is.True);
            Assert.That(shownBeads[0].sharedMaterial, Is.Not.SameAs(shownBeads[2].sharedMaterial), "kept beads remain distinguishable from neutral unfilled positions");
            Assert.That(keepsake.DisplayRoot.GetComponentsInChildren<Renderer>().Count(r => r.enabled && r.name.StartsWith("Strand cord ", StringComparison.Ordinal)), Is.EqualTo(7));
            Assert.That(keepsake.DisplayRoot.GetComponentsInChildren<Collider>().All(c => !c.enabled), Is.True, "the kept strand must not intercept page controls");
            app.Session.Pause(true); yield return null;
            var held = keepsake.RevealProgress; var position = keepsake.DisplayRoot.localPosition;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(keepsake.RevealProgress, Is.EqualTo(held)); Assert.That(keepsake.DisplayRoot.localPosition, Is.EqualTo(position));
            app.Session.Pause(false);
            Assert.That(app.View.Caption, Does.StartWith("Kept on this device."), "resume retains the successful-save receipt");
            Assert.That(Panel("Turn a page").Ids, Does.Not.Contain("save"), "the completed save is not offered again");
            yield return Pinch(Panel("Turn a page"), "again"); river.Advance(.1f); yield return null;
            Assert.That(keepsake.Visible, Is.False);
            yield return Pinch(Panel("Kept story choices"), "reopen");
            for (var i = 0; i < 62; i++) river.Advance(.1f);
            yield return null; yield return null;
            Assert.That(reopened, Is.EqualTo(1)); Assert.That(reopenedInterest, Is.EqualTo("place-water"));
            Assert.That(app.Session.IsReplay, Is.True); Assert.That(PersonalStoryJson.Write(app.Session.Story), Is.EqualTo(exact));
            Assert.That(river.RiverVisible, Is.True, "exact replay must retain its riverside presentation");
            yield return BothSidesEnding();
            Assert.That(saved, Is.EqualTo(1), "reopening never saves again");
            Assert.That(keepsake.Visible, Is.True); Assert.That(keepsake.RevealProgress, Is.EqualTo(1), "replay does not pretend to thread new beads");
        }

        [UnityTest] public IEnumerator AnOlderKeptStoryDoesNotMarkTheNewVisitKeptAndReducedMotionDoesNotRise()
        {
            app.SetReducedMotion(true);
            yield return Water(); yield return BothSidesEnding();
            yield return Pinch(Panel("Turn a page"), "save");
            Assert.That(keepsake.Visible, Is.True); Assert.That(keepsake.RevealProgress, Is.EqualTo(1));
            yield return Pinch(Panel("Turn a page"), "again"); river.Advance(.1f); yield return null;
            yield return Water(false); yield return BothSidesEnding();
            Assert.That(app.HasKept, Is.True); Assert.That(keepsake.Visible, Is.False, "the older file does not prove this sitting was saved");
            yield return Pinch(Panel("Turn a page"), "save");
            Assert.That(keepsake.Visible, Is.False, "a replacement request is not a successful replacement");
            yield return Pinch(Panel("Return and recovery actions"), "cancel");
            Assert.That(keepsake.Visible, Is.False);
        }

        [UnityTest] public IEnumerator AFailedSaveCannotShowAStoredKeepsake()
        {
            var saved = 0; app.StoryKept += () => saved++;
            yield return Water(); yield return BothSidesEnding();
            Directory.CreateDirectory(Path.Combine(shelf, "kept.story"));
            yield return Pinch(Panel("Turn a page"), "save");
            Assert.That(saved, Is.Zero); Assert.That(app.HasKept, Is.False); Assert.That(keepsake.Visible, Is.False);
            Assert.That(app.View.Caption, Does.Contain("could not be kept"));
        }

        [UnityTest] public IEnumerator OnlyTheVisibleTableStageAcceptsASweep()
        {
            yield return Water(); yield return FirstSide();
            var stage = app.View.Stage; var hand = Object.FindObjectsByType<HandSource>(FindObjectsSortMode.None).First();
            for (var until = Time.realtimeSinceStartup + 4; !stage.Standing && Time.realtimeSinceStartup < until;) yield return null;
            Assert.That(stage.Standing, Is.True);
            var chapter = app.Session.Chapter.Id;
            yield return Sweep(hand);
            Assert.That(app.Session.Chapter.Id, Is.EqualTo(chapter), "an invisible stage cannot read an unannounced sweep");
            Assert.That(app.View.SpatialStageVisible, Is.False);
            yield return Pinch(Panel("Always available controls"), "options");
            yield return Pinch(Panel("Return and recovery actions"), "table");
            Assert.That(app.View.SpatialStageVisible, Is.True);
            // A real Options modal folds the stage. A sweep is offered again only after it has risen.
            for (var until = Time.realtimeSinceStartup + 4; !stage.Standing && Time.realtimeSinceStartup < until;) yield return null;
            Assert.That(stage.Standing, Is.True, "the visible stage has settled before it may read a sweep");
            Assert.That(app.OtherSideOffered, Is.True);
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Reading));
            Assert.That(app.Session.Paused, Is.False);
            yield return Sweep(hand);
            Assert.That(app.Session.Chapter.Id, Is.Not.EqualTo(chapter), "the same real gesture path still turns the visible stage");
            Assert.That(app.Session.FollowedByChoice, Is.True);
        }
        IEnumerator Sweep(HandSource hand)
        {
            var began = Time.time;
            var samples = 0;
            hand.Simulated = () =>
            {
                samples++;
                var stage = app.View.Stage.transform;
                var palm = stage.parent.TransformPoint(stage.localPosition + new Vector3(Mathf.Lerp(-.18f, .18f, Mathf.Clamp01((Time.time - began) / .4f)), .20f, -.36f));
                System.Numerics.Vector3 N(Vector3 v) => new System.Numerics.Vector3(v.x, v.y, v.z);
                return new HandSample
                {
                    Wrist = N(palm + new Vector3(0, 0, -.045f)), MiddleProximal = N(palm + new Vector3(0, 0, .045f)),
                    IndexProximal = N(palm + new Vector3(.025f, 0, .04f)), LittleProximal = N(palm + new Vector3(-.03f, 0, .035f)),
                    IndexTip = N(palm + new Vector3(.03f, 0, .12f)), MiddleTip = N(palm + new Vector3(0, 0, .13f)), RingTip = N(palm + new Vector3(-.015f, 0, .125f))
                };
            };
            Assert.That(hand.Tracked, Is.True);
            var target = app.View.Stage.transform;
            var start = target.parent.InverseTransformPoint(hand.PalmCenter) - target.localPosition;
            var reach = new TurnSettings();
            Assert.That(start.x, Is.EqualTo(-.18f).Within(.001f));
            Assert.That(start.y, Is.InRange(reach.Low, reach.High));
            Assert.That(start.z, Is.InRange(reach.Near, reach.Far));
            yield return new WaitForSeconds(.6f);
            Assert.That(samples, Is.GreaterThan(2), "the real hand adapter consumed the synthetic path");
            hand.Simulated = null; yield return null;
        }
    }
}
