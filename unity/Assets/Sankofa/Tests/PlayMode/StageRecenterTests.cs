using System.Collections;
using NUnit.Framework;
using Sankofa.Runtime.Stage;
using Sankofa.Runtime.App;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace Sankofa.Tests
{
    public class StageRecenterTests
    {
        [UnityTest]
        public IEnumerator CharacterActingFreezesOnPauseAndReleasesTheHandWhenItsObjectLeaves()
        {
            LogAssert.ignoreFailingMessages = true; // No headset: the Meta SDK logs unsupported native calls.
            SceneManager.LoadScene("Hearth");
            yield return null;
            Object.FindFirstObjectByType<SankofaApp>().enabled = false;
            var stage = Object.FindFirstObjectByType<StoryStage>();
            stage.ShowScene("story.water.1899");
            var figure = stage.Find("figure.njeri");
            var motion = figure.GetComponent<FigureMotion>();
            motion.ResetState(); figure.Reveal();
            var vessel = stage.Find("gourd.empty");
            vessel.Reveal();
            var finger = System.Array.Find(figure.GetComponentsInChildren<Transform>(true), t => t.name == "index_02_r");
            var eyes = System.Array.Find(figure.GetComponentsInChildren<SkinnedMeshRenderer>(true), t => t.name == "Eyes");
            StageClock.Paused = false;
            var deadline = Time.realtimeSinceStartup + 8;
            while (eyes.GetBlendShapeWeight(0) < 50 && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(eyes.GetBlendShapeWeight(0), Is.GreaterThanOrEqualTo(50), "observe a real blink before pausing it");
            var curled = finger.localRotation;
            StageClock.Paused = true;
            var blink = eyes.GetBlendShapeWeight(0);
            yield return new WaitForSeconds(0.25f);
            Assert.That(Quaternion.Angle(curled, finger.localRotation), Is.LessThan(0.1f), "paused fingers do not keep curling");
            Assert.That(eyes.GetBlendShapeWeight(0), Is.EqualTo(blink).Within(0.001f), "paused eyes hold their expression");
            vessel.MoveTo(vessel.transform.position + Vector3.forward, 0.5f);
            StageClock.Paused = false;
            yield return new WaitForSeconds(0.7f);
            Assert.That(Quaternion.Angle(curled, finger.localRotation), Is.GreaterThan(5), "the hand opens after release");
        }

        [UnityTest]
        public IEnumerator StanceFeetStayPlantedWhileTheBodyAdvances()
        {
            LogAssert.ignoreFailingMessages = true;
            SceneManager.LoadScene("Hearth"); yield return null;
            Object.FindFirstObjectByType<SankofaApp>().enabled = false;
            var stage = Object.FindFirstObjectByType<StoryStage>(); stage.ShowScene("story.water.1899");
            var figure = stage.Find("figure.njeri"); figure.Reveal();
            var motion = figure.GetComponent<FigureMotion>(); motion.ResetState();
            var foot = System.Array.Find(figure.GetComponentsInChildren<Transform>(true), t => t.name == "foot_r");
            var grounding = figure.GetComponent<FigureGrounding>();
            var start = figure.transform.position;
            motion.GoTo(start + Vector3.left * 1.3f, start + Vector3.left * 2, false);
            Vector3 previousFoot = foot.position, previousBody = start;
            var wasPlanted = false; var measured = 0; var worstSlip = 0f;
            var version = grounding.RightContactVersion;
            Time.captureFramerate = 60; // Deterministic stance sampling, independent of unbounded batch frame rate.
            var end = Time.realtimeSinceStartup + 7;
            while (Time.realtimeSinceStartup < end && motion.Walking)
            {
                yield return null;
                if (grounding.RightPlanted && wasPlanted && version == grounding.RightContactVersion && motion.WalkVelocity > 0.2f && Vector3.Distance(figure.transform.position, previousBody) > 0.001f)
                {
                    var shift = foot.position - previousFoot; shift.y = 0;
                    measured++; worstSlip = Mathf.Max(worstSlip, shift.magnitude);
                }
                wasPlanted = grounding.RightPlanted;
                version = grounding.RightContactVersion;
                previousFoot = foot.position; previousBody = figure.transform.position;
            }
            Assert.That(Vector3.Distance(start, figure.transform.position), Is.GreaterThan(1f), "observe an actual walk");
            Assert.That(measured, Is.GreaterThan(4), "observe stance while the body moves");
            Assert.That(worstSlip, Is.LessThan(0.02f), "stance foot remains in place rather than skating with the root");
        }

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

        [TearDown] public void ResetClock() { StageClock.Paused = false; Time.captureFramerate = 0; }
    }
}
