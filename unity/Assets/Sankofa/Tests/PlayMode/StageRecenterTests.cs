using System.Collections;
using NUnit.Framework;
using Sankofa.Runtime.Stage;
using UnityEngine;
using UnityEngine.TestTools;

namespace Sankofa.Tests
{
    public class StageRecenterTests
    {
        [UnityTest]
        public IEnumerator APausedFigureAndItsRouteFollowAStageTranslationRotationAndHeightChange()
        {
            var stage = new GameObject("Stage");
            var person = new GameObject("Person");
            person.transform.SetParent(stage.transform);
            person.transform.localPosition = new Vector3(2, 0, 2);
            person.AddComponent<StageObject>();
            var motion = person.AddComponent<FigureMotion>();
            motion.GoTo(new Vector3(3, 0, 2), Vector3.zero, false);
            StageClock.Paused = true;
            stage.transform.SetPositionAndRotation(new Vector3(5, 0.08f, 4), Quaternion.Euler(0, 90, 0));
            yield return null;
            Assert.That(Vector3.Distance(person.transform.position, stage.transform.TransformPoint(new Vector3(2, 0, 2))), Is.LessThan(0.001f));
            var route = (System.Collections.Generic.List<Vector3>)typeof(FigureMotion).GetField("route", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(motion);
            Assert.That(Vector3.Distance(route[route.Count - 1], stage.transform.TransformPoint(new Vector3(3, 0, 2))), Is.LessThan(0.001f));
            StageClock.Paused = false;
            for (var i = 0; i < 180 && motion.Walking; i++) yield return null;
            // Rebase must affect the pending route as well as the idle pose.
            Assert.That(person.transform.position.y, Is.EqualTo(0.08f).Within(0.03f));
            Assert.That(person.transform.position.x, Is.GreaterThan(6.9f));
            Object.Destroy(stage);
        }

        [UnityTest]
        public IEnumerator AnOfferedObjectKeepsItsDestinationInTheRecenteredStage()
        {
            var stage = new GameObject("Stage");
            var prop = new GameObject("Prop");
            prop.transform.SetParent(stage.transform);
            var item = prop.AddComponent<StageObject>();
            item.MoveTo(new Vector3(0.3f, 0.6f, 0.5f), 0.3f);
            StageClock.Paused = true;
            stage.transform.SetPositionAndRotation(new Vector3(5, 0.08f, 4), Quaternion.Euler(0, 90, 0));
            yield return null;
            StageClock.Paused = false;
            for (var i = 0; i < 100; i++) yield return null;
            var goal = stage.transform.TransformPoint(new Vector3(0.3f, 0.6f, 0.5f));
            // Variable-speed batch frames: destination is inspected directly after rebasing, then motion heads to it.
            var field = typeof(StageObject).GetField("moveTarget", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.That(Vector3.Distance((Vector3)field.GetValue(item), goal), Is.LessThan(0.001f));
            Object.Destroy(stage);
        }

        [TearDown] public void ResetClock() => StageClock.Paused = false;
    }
}
