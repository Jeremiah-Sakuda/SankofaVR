using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Sankofa.Flow;
using Sankofa.Runtime.Input;
using Sankofa.Runtime.Personal;
using Sankofa.Runtime.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Sankofa.Tests
{
    /// <summary>Actual release-on-pinch adapter with synthetic single-hand samples; not real-hand evidence.</summary>
    public sealed class RiverPerspectiveChoiceTests
    {
        PersonalStoryApp app;
        RiverThresholdPrototype river;
        RiverPerspectiveChoices choices;
        PointerSource pointer;
        string shelf;
        readonly List<string> errors = new List<string>();

        [UnitySetUp] public IEnumerator Load()
        {
            errors.Clear(); LogAssert.ignoreFailingMessages = true; Application.logMessageReceived += Error;
            shelf = Path.Combine(Path.GetTempPath(), "sankofa-spatial-choices-" + Guid.NewGuid().ToString("N"));
            PersonalStoryApp.StoreFolderOverride = shelf;
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/Sankofa/Scenes/RiverThresholdPainted.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene("RiverThresholdPainted");
#endif
            yield return null; yield return null;
            river = Object.FindFirstObjectByType<RiverThresholdPrototype>();
            river.AutomaticClock = false; app = river.App; app.ServiceEndpoint = ""; app.TurnsPages = false;
            choices = Object.FindFirstObjectByType<RiverPerspectiveChoices>();
            Assert.That(choices, Is.Not.Null, "The selected painted scene must wire spatial choices.");
            pointer = Object.FindFirstObjectByType<PointerSource>();
            foreach (var source in Object.FindObjectsByType<PointerSource>(FindObjectsSortMode.None)) source.enabled = false;
            // The real rig has no tracked floor-level head in batch mode. Placement deliberately supplies a
            // 1.15m fallback, but Camera.main remains at the tracking origin; a ray from there approaches the
            // angled intake plaques from below. Supply one explicit seated test pose before recentering.
            var head = Camera.main.transform;
            Debug.Log("[Sankofa spatial test] Eye before synthetic seated setup: " + head.position);
            var rig = head.GetComponentsInParent<MonoBehaviour>().FirstOrDefault(component => component.GetType().Name == "OVRCameraRig");
            if (rig != null) rig.enabled = false;
            head.SetPositionAndRotation(new Vector3(0, 1.15f, 0), Quaternion.identity);
            app.GetComponent<StoryTablePlacement>().Recenter();
            yield return null; yield return null;
            Assert.That(Vector3.Distance(head.position, new Vector3(0, 1.15f, 0)), Is.LessThan(.001f));
        }

        [UnityTearDown] public IEnumerator Clean()
        {
            var scene = SceneManager.GetSceneByName("RiverThresholdPainted");
            if (scene.IsValid() && scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Spatial choice cleanup " + Guid.NewGuid().ToString("N")));
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
        Ray FromSeat(PokePanel panel, string id)
        {
            var button = panel.transform.Find("Button " + id);
            var origin = Camera.main.transform.position;
            return new Ray(origin, (button.position - origin).normalized);
        }
        IEnumerator Pinch(PokePanel panel, string id)
        {
            Assert.That(panel.isActiveAndEnabled && !panel.Hidden, Is.True, panel.name);
            Assert.That(panel.Ids, Does.Contain(id));
            yield return new WaitForSecondsRealtime(.34f);
            var ray = FromSeat(panel, id);
            Assert.That(panel.TryHit(ray, 4, out _, out var hit), Is.True,
                "Seated ray misses " + id + ": origin=" + ray.origin + ", direction=" + ray.direction + ", panel=" + panel.transform.position);
            Assert.That(hit, Is.EqualTo(id));
            pointer.ProcessPointer(ray, 0, true, 1, .02f);
            Assert.That(pointer.Hovered, Is.EqualTo(id), "The seated pointer must reach the intended panel without moving the camera.");
            pointer.ProcessPointer(ray, 1, true, 1, .02f);
            pointer.ProcessPointer(ray, 0, true, 1, .02f);
            yield return null; yield return null;
        }
        IEnumerator Water()
        {
            // These original panels must remain registered when the new spatial panels are appended.
            yield return Pinch(river.Controls, "primary");
            yield return Pinch(Panel("Interest choices"), "place-water");
            for (var i = 0; i < 62; i++) river.Advance(.1f);
            yield return null; yield return null;
        }
        IEnumerator WaitFor(Func<bool> condition)
        {
            var until = Time.realtimeSinceStartup + 6;
            while (!condition() && Time.realtimeSinceStartup < until) yield return null;
            Assert.That(condition(), Is.True, "The expected visible choices did not arrive.");
        }
        IEnumerator FirstChoice()
        {
            yield return Water();
            Assert.That(choices.Panels.All(p => p.Hidden), Is.True, "No perspective is offered before its story boundary.");
            app.Session.Next(); app.View.Stage.Seek(1);
            yield return WaitFor(() => choices.Panels.Count(p => !p.Hidden) == 2);
        }

        [UnityTest] public IEnumerator OneSeatedPointerChoosesBothWomenAndRetainsCaptionAlternatives()
        {
            yield return FirstChoice();
            var captions = Panel("Questions beside the story");
            Assert.That(captions.Hidden, Is.False);
            CollectionAssert.AreEquivalent(new[] { "choice:0", "choice:1" }, captions.Ids);
            foreach (var panel in choices.Panels)
            {
                var id = panel.Ids.Single().Substring("river:".Length);
                var index = Array.IndexOf(app.View.LastChapter.ChoiceIds, id);
                Assert.That(panel.GetComponentInChildren<TextMeshPro>().text, Is.EqualTo(app.View.LastChapter.Choices[index]));
            }
            var first = choices.Panels.Single(p => p.Ids.Contains("river:place-water.follow.remembers"));
            var face = first.GetComponentInChildren<Renderer>();
            var before = new MaterialPropertyBlock(); face.GetPropertyBlock(before);
            var ray = FromSeat(first, first.Ids.Single());
            pointer.ProcessPointer(ray, 0, true, 1, .02f); yield return null;
            var after = new MaterialPropertyBlock(); face.GetPropertyBlock(after);
            Assert.That(after.GetColor("_Color"), Is.Not.EqualTo(before.GetColor("_Color")), "Pointing gives visible local feedback.");
            yield return Pinch(first, first.Ids.Single());
            Assert.That(app.Session.Chapter.Id, Is.EqualTo("place-water.return"));
            Assert.That(app.Session.FollowedByChoice, Is.True);
            app.View.Stage.Seek(1);
            yield return WaitFor(() => choices.Panels.Count(p => !p.Hidden) == 1);
            var other = choices.Panels.Single(p => !p.Hidden);
            Assert.That(other.Ids.Single(), Is.EqualTo("river:place-water.follow.traveller"));
            yield return Pinch(other, other.Ids.Single());
            Assert.That(app.Session.Chapter.Id, Is.EqualTo("place-water.traveller"));
            Assert.That(app.Session.Heard.Count, Is.EqualTo(2));
            yield return WaitFor(() => choices.Panels.All(p => p.Hidden));
        }

        [UnityTest] public IEnumerator PauseCancelsHeldSelectionAndDetailsCannotRevealLiveTargets()
        {
            yield return FirstChoice();
            var chosen = choices.Panels[0]; var ray = FromSeat(chosen, chosen.Ids.Single());
            var chapter = app.Session.Chapter.Id;
            var armedRevision = chosen.Revision;
            pointer.ProcessPointer(ray, 0, true, 1, .02f);
            pointer.ProcessPointer(ray, 1, true, 1, .02f);
            app.Session.Pause(true); yield return null; yield return null;
            Assert.That(choices.Panels.All(p => p.Hidden), Is.True);
            Assert.That(chosen.Revision, Is.GreaterThan(armedRevision), "Hiding invalidates the armed panel/button key even when the pointer supplies no samples during pause.");
            var pausedRevision = chosen.Revision;
            app.Session.Pause(false); yield return null; yield return null;
            Assert.That(chosen.Revision, Is.GreaterThan(pausedRevision));
            pointer.ProcessPointer(ray, 0, true, 1, .02f); yield return null; yield return null;
            Assert.That(app.Session.Chapter.Id, Is.EqualTo(chapter), "A pinch held through pause is not a fresh choice.");
            app.View.AboutRequested?.Invoke(); yield return null; yield return null;
            Assert.That(choices.Panels.All(p => p.Hidden), Is.True, "A help modal owns the interaction while open.");
            app.View.DecisionSelected?.Invoke("return"); yield return null; yield return null;
            yield return WaitFor(() => choices.Panels.Count(p => !p.Hidden) == 2);
            yield return Pinch(chosen, chosen.Ids.Single());
            Assert.That(app.Session.Chapter.Id, Is.Not.EqualTo(chapter));
        }

        [UnityTest] public IEnumerator HistoricalContextFinishesBeforeSpatialInvitationAppears()
        {
            yield return Water();
            app.Session.Next();
            Assert.That(app.Session.Chapter.Id, Is.EqualTo("place-water.name"));
            app.View.Stage.Seek(.3f); yield return null; yield return null;
            Assert.That(app.View.Stage.FigureShown("wambui"), Is.False);
            Assert.That(app.View.Stage.FigureShown("naserian"), Is.False);
            Assert.That(choices.Panels.All(p => p.Hidden), Is.True);
            // This invokes the real authored .6 reveal; the adapter never activates a character itself.
            app.View.Stage.Seek(.7f); yield return null; yield return null;
            Assert.That(app.View.Stage.FigureShown("wambui"), Is.True);
            Assert.That(app.View.Stage.FigureShown("naserian"), Is.True);
            Assert.That(choices.Panels.All(p => p.Hidden), Is.True, "Visible characters alone cannot offer a premature spatial choice.");
            app.View.Stage.Seek(1);
            yield return WaitFor(() => choices.Panels.Count(p => !p.Hidden) == 2);
        }

        [UnityTest] public IEnumerator ReturningToTableOrCancellingRemovesEveryRiverTarget()
        {
            yield return FirstChoice();
            var camera = Camera.main.transform; var position = camera.position; var rotation = camera.rotation;
            yield return Pinch(Panel("Always available controls"), "options");
            yield return Pinch(Panel("Return and recovery actions"), "table");
            Assert.That(river.RiverVisible, Is.False);
            Assert.That(choices.Panels.All(p => p.Hidden || !p.gameObject.activeInHierarchy), Is.True);
            Assert.That(Vector3.Distance(position, camera.position), Is.LessThan(.001f));
            Assert.That(Quaternion.Angle(rotation, camera.rotation), Is.LessThan(.01f));
            app.Session.Cancel(); yield return null; yield return null;
            Assert.That(choices.Panels.All(p => p.Hidden || !p.gameObject.activeInHierarchy), Is.True);
        }
    }
}
