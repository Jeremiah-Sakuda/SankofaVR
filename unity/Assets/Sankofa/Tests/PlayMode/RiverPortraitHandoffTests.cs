using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Sankofa.Runtime.Input;
using Sankofa.Runtime.Personal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Sankofa.Tests
{
    /// <summary>Real painted scene, synthetic progression: no headset or listening acceptance.</summary>
    public sealed class RiverPortraitHandoffTests
    {
        PersonalStoryApp app;
        RiverThresholdPrototype river;
        RiverCastPerformance cast;
        string shelf;
        readonly List<string> errors = new List<string>();

        [UnitySetUp] public IEnumerator Load()
        {
            errors.Clear();
            LogAssert.ignoreFailingMessages = true;
            Application.logMessageReceived += Error;
            shelf = Path.Combine(Path.GetTempPath(), "sankofa-river-portraits-" + Guid.NewGuid().ToString("N"));
            PersonalStoryApp.StoreFolderOverride = shelf;
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/Sankofa/Scenes/RiverThresholdPainted.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene("RiverThresholdPainted");
#endif
            yield return null; yield return null;
            river = Object.FindFirstObjectByType<RiverThresholdPrototype>();
            cast = Object.FindFirstObjectByType<RiverCastPerformance>();
            app = river.App;
            river.AutomaticClock = false;
            app.ServiceEndpoint = ""; app.TurnsPages = false;
            Time.captureDeltaTime = 1f / 60;
            foreach (var pointer in Object.FindObjectsByType<PointerSource>(FindObjectsSortMode.None)) pointer.enabled = false;
            river.Controls.PressFromPointer("primary"); yield return null;
            app.View.Panels.Single(p => p.name == "Interest choices").PressFromPointer("place-water");
            yield return null;
            for (var i = 0; i < 62; i++) river.Advance(.1f);
            yield return Settle();
            Assert.That(cast.Solid, Is.False);
        }

        [UnityTearDown] public IEnumerator Clean()
        {
            Time.captureDeltaTime = 0;
            var scene = SceneManager.GetSceneByName("RiverThresholdPainted");
            if (scene.IsValid() && scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Portrait cleanup " + Guid.NewGuid().ToString("N")));
                yield return SceneManager.UnloadSceneAsync(scene);
            }
            PersonalStoryApp.StoreFolderOverride = null;
            if (Directory.Exists(shelf)) Directory.Delete(shelf, true);
            Application.logMessageReceived -= Error;
            LogAssert.ignoreFailingMessages = false;
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }

        void Error(string message, string trace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Assert && type != LogType.Exception) return;
            if (type == LogType.Error && message == "Unable to process a controller whose SampleRateHz is 0 now.") return;
            errors.Add(message + "\n" + trace);
        }

        IEnumerator Settle()
        {
            for (var i = 0; i < 240 && !app.View.Stage.Standing; i++) { river.Advance(1f / 60); yield return null; }
            yield return null; yield return null;
            Assert.That(app.View.Stage.Standing, Is.True, "the accepted miniature staging must settle");
        }

        IEnumerator OfferSides()
        {
            Assert.That(app.Session.Next(), Is.True);
            Assert.That(app.Session.Chapter.Id, Is.EqualTo("place-water.name"));
            app.View.Stage.Seek(.8f);
            yield return null; yield return null;
            Assert.That(app.Session.OffersChoice, Is.True);
        }

        MaterialPropertyBlock Paint(int index)
        {
            var properties = new MaterialPropertyBlock();
            cast.Actors[index].GetComponent<Renderer>().GetPropertyBlock(properties);
            return properties;
        }

        void AssertPresent(Vector3[] positions, Vector3[] scales)
        {
            for (var i = 0; i < cast.Actors.Length; i++)
            {
                var actor = cast.Actors[i];
                Assert.That(actor.gameObject.activeInHierarchy, Is.True, "a paper-stage fold must not erase the river portrait");
                Assert.That(Vector3.Distance(actor.position, positions[i]), Is.LessThan(.00001f), "the river character stays in her own place");
                Assert.That(actor.localScale, Is.EqualTo(scales[i]), "a change of perspective must not mirror a person's body");
                Assert.That(Paint(i).GetFloat("_Visibility"), Is.EqualTo(1).Within(.00001f), "active geometry must also remain painted");
                Assert.That(Paint(i).GetFloat("_Unfold"), Is.EqualTo(1).Within(.00001f), "river figures do not inherit the hidden miniature's fold");
            }
        }

        [UnityTest] public IEnumerator BothPerspectivesKeepPeopleVisibleAndInTheirOwnPlaces()
        {
            var positions = cast.Actors.Select(a => a.position).ToArray();
            var scales = cast.Actors.Select(a => a.localScale).ToArray();
            var rig = Camera.main.transform.root;
            var rigPosition = rig.position; var rigRotation = rig.rotation;
            yield return OfferSides();
            Assert.That(app.Session.Choose(0), Is.True);
            for (var side = 0; side < 2; side++)
            {
                var turningSamples = 0;
                for (var frame = 0; frame < 240; frame++)
                {
                    river.Advance(1f / 60); yield return null;
                    AssertPresent(positions, scales);
                    if (app.View.Stage.Turning) turningSamples++;
                    if (frame > 40 && app.View.Stage.Standing) break;
                }
                Assert.That(turningSamples, Is.GreaterThan(0), "the test must exercise a real folded perspective handoff");
                for (var frame = 0; frame < 30; frame++) { river.Advance(1f / 60); yield return null; }
                Assert.That(Paint(side).GetFloat("_Highlight"), Is.EqualTo(1).Within(.001f), "attention follows the accepted side");
                if (side == 0)
                {
                    Assert.That(app.Session.OffersOtherSide, Is.True);
                    Assert.That(app.Session.HearOtherSide(0, true), Is.True);
                }
            }
            Assert.That(rig.position, Is.EqualTo(rigPosition));
            Assert.That(rig.rotation, Is.EqualTo(rigRotation));
        }

        [UnityTest] public IEnumerator ContextAbsenceIsPreservedWithoutMovingTheReturningPeople()
        {
            var positions = cast.Actors.Select(a => a.position).ToArray();
            var scales = cast.Actors.Select(a => a.localScale).ToArray();
            Assert.That(app.Session.Next(), Is.True);
            app.View.Stage.Seek(.1f); yield return null; yield return null;
            Assert.That(cast.Actors.All(a => !a.gameObject.activeSelf), Is.True, "do not reveal people during the source-only context beat");
            app.View.Stage.Seek(.8f); yield return null; yield return null;
            AssertPresent(positions, scales);
        }

        [UnityTest] public IEnumerator PortraitInkAndAttentionFreezeOnPauseAndSettleWithReducedMotion()
        {
            yield return OfferSides();
            Assert.That(app.Session.Choose(0), Is.True);
            yield return Settle();
            app.View.Stage.Seek(.9f); yield return null; yield return null;
            river.Advance(.1f); yield return null; yield return null;
            var before = Paint(0);
            Assert.That(before.GetFloat("_PoseBlend"), Is.LessThan(1), "sample an actual ink transition rather than an already-held pose");
            var pose = before.GetVector("_FrameB");
            var blend = before.GetFloat("_PoseBlend");
            var focus = before.GetFloat("_Highlight");
            var positions = cast.Actors.Select(a => a.position).ToArray();
            var scales = cast.Actors.Select(a => a.localScale).ToArray();
            app.Session.Pause(true);
            for (var i = 0; i < 8; i++) { river.Advance(.1f); yield return null; }
            AssertPresent(positions, scales);
            Assert.That(Paint(0).GetVector("_FrameB"), Is.EqualTo(pose));
            Assert.That(Paint(0).GetFloat("_PoseBlend"), Is.EqualTo(blend).Within(.00001f));
            Assert.That(Paint(0).GetFloat("_Highlight"), Is.EqualTo(focus).Within(.00001f));
            app.SetReducedMotion(true); app.Session.Pause(false);
            river.Advance(.1f); yield return null; yield return null;
            AssertPresent(positions, scales);
            Assert.That(Paint(0).GetFloat("_PoseBlend"), Is.EqualTo(1));
            Assert.That(Paint(0).GetFloat("_Highlight"), Is.EqualTo(1));
        }
    }
}
