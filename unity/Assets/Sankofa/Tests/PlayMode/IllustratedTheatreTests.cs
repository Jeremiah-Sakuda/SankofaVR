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
    public class IllustratedTheatreTests
    {
        string folder;
        readonly List<string> problems = new List<string>();
        SankofaApp app;
        StoryStage stage;
        IllustratedTheatre theatre;

        [SetUp] public void Setup()
        {
            folder = Path.Combine(Path.GetTempPath(), "sankofa-illustration-" + Guid.NewGuid().ToString("N"));
            SankofaApp.StoreFolderOverride = folder;
            AudioListener.volume = 0; StageClock.Paused = false;
            LogAssert.ignoreFailingMessages = true;
            problems.Clear(); Application.logMessageReceived += Watch;
        }

        void Watch(string message, string stack, LogType type)
        {
            if ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert) &&
                (message.Contains("[Sankofa]") || (stack ?? "").Contains("Sankofa."))) problems.Add(message);
        }

        [TearDown] public void Cleanup()
        {
            Application.logMessageReceived -= Watch;
            StageClock.Paused = false; Time.timeScale = 1; Time.captureFramerate = 0; AudioListener.volume = 1;
            SankofaApp.StoreFolderOverride = null;
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }

        static IEnumerator Until(Func<bool> condition, float seconds = 60)
        {
            var end = Time.realtimeSinceStartup + seconds;
            while (!condition()) { Assert.That(Time.realtimeSinceStartup, Is.LessThan(end), "timed out waiting for illustrated visit"); yield return null; }
        }

        IEnumerator Open()
        {
            SceneManager.LoadScene("IllustratedTheatre"); yield return null;
            app = Object.FindFirstObjectByType<SankofaApp>(); stage = Object.FindFirstObjectByType<StoryStage>();
            theatre = Object.FindFirstObjectByType<IllustratedTheatre>(FindObjectsInactive.Include);
            Assert.That(app.Flow, Is.Not.Null, app.LoadProblem);
            yield return Until(() => app.AtStart, 10);
        }

        [UnityTest]
        public IEnumerator GourdCrossesTheIllustrationAndPauseRecenterAndPassiveOutcomeStayCoherent()
        {
            yield return Open(); app.enabled = false;
            stage.ShowScene("story.water.1850s");
            stage.Perform(new StoryAction(ActionKind.Reveal, "figure.wambui"));
            stage.Perform(new StoryAction(ActionKind.Reveal, "gourd.flour"));
            var giver = stage.Find("figure.wambui").GetComponent<IllustratedFigure>();
            var gourd = stage.Find("gourd.flour");
            stage.Perform(new StoryAction(ActionKind.Offer, "gourd.flour"));
            Assert.That(giver.Offering, Is.EqualTo(gourd), "the gift waits for the offering pose to show");
            yield return Until(() => giver.PoseBlend >= 0.5f, 5);
            yield return null;
            Assert.That(Vector3.Distance(gourd.transform.position, giver.GiftOrigin), Is.LessThan(0.001f), "the physical object starts in the painted palm, not at the hip of the pose being left");
            Assert.That(giver.Offering, Is.EqualTo(gourd), "and rests there a moment, so it is seen as hers");
            yield return new WaitForSeconds(0.8f);
            Assert.That(giver.Offering, Is.Null, "then it crosses");
            StageClock.Paused = true;
            var poseBlend = giver.PoseBlend; var before = gourd.transform.position; var unfold = theatre.UnfoldTime;
            yield return new WaitForSeconds(0.25f);
            Assert.That(giver.PoseBlend, Is.EqualTo(poseBlend).Within(0.0001f));
            Assert.That(theatre.UnfoldTime, Is.EqualTo(unfold).Within(0.0001f));
            Assert.That(Vector3.Distance(before, gourd.transform.position), Is.LessThan(0.001f));
            var frame = stage.Find("figure.wambui").transform.parent;
            var offerInFrame = frame.InverseTransformPoint(stage.OfferPosition());
            frame.SetPositionAndRotation(frame.position + new Vector3(1, 0.1f, 0.4f), Quaternion.Euler(0, 30, 0) * frame.rotation);
            yield return null;
            StageClock.Paused = false;
            yield return new WaitForSeconds(2.5f);
            Assert.That(Vector3.Distance(gourd.transform.position, frame.TransformPoint(offerInFrame)), Is.LessThan(0.05f), "the destination rebases with the visitor's stage");
            Assert.That(Vector3.Distance(gourd.transform.position, giver.GiftOrigin), Is.GreaterThan(0.5f), "the object has crossed into reachable space");
            stage.ResolveIllustration(gourd, true); yield return new WaitForSeconds(0.8f);
            Assert.That(giver.Pose, Is.EqualTo(2)); Assert.That(theatre.AcceptedResponses, Is.EqualTo(1));
            stage.Echo(new[] { "figure.wambui" }); yield return new WaitForSeconds(0.8f);
            var echo = GameObject.Find("Echo of figure.wambui");
            Assert.That(echo, Is.Not.Null);
            Assert.That(echo.GetComponent<MeshRenderer>().sharedMaterial, Is.EqualTo(giver.Surface.sharedMaterial), "memory retains the painted art rather than a legacy humanoid");
            Assert.That(echo.GetComponent<MeshFilter>().sharedMesh, Is.EqualTo(giver.GetComponent<StageObject>().EchoMesh));
            stage.ClearEchoes();
            stage.RecallPeople(new[] { "story.water.1850s" }); yield return new WaitForSeconds(0.8f);
            Assert.That(GameObject.Find("Recalled figure.wambui").GetComponent<MeshRenderer>().sharedMaterial, Is.EqualTo(giver.Surface.sharedMaterial));
            stage.ResetAll(); stage.ShowScene("story.water.1850s");
            stage.ResolveIllustration(gourd, false);
            Assert.That(giver.Pose, Is.EqualTo(0)); Assert.That(theatre.AcceptedResponses, Is.Zero, "a passive visitor never receives an accepted response");
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        static float Yaw(IllustratedFigure f, Vector3 toward)
        {
            var card = f.Surface.transform;
            var facing = Vector3.ProjectOnPlane(card.rotation * Vector3.back, Vector3.up);
            return Vector3.Angle(facing, Vector3.ProjectOnPlane(toward - card.position, Vector3.up));
        }

        /// <summary>The pose the shader draws, and how far its light has dipped, mirroring Illustration.shader.</summary>
        static (Vector4 frame, float dip) Shown(IllustratedFigure f)
        {
            var block = new MaterialPropertyBlock();
            f.Surface.GetPropertyBlock(block);
            var t = block.GetFloat("_PoseBlend");
            return (t < 0.5f ? block.GetVector("_FrameA") : block.GetVector("_FrameB"), 1 - Mathf.Abs(t * 2 - 1));
        }

        [UnityTest]
        public IEnumerator PoseChangesDipAndSwapWithoutJumping()
        {
            yield return Open(); app.enabled = false;
            stage.ShowScene("story.water.1850s");
            stage.Perform(new StoryAction(ActionKind.Reveal, "figure.wambui"));
            var wambui = stage.Find("figure.wambui").GetComponent<IllustratedFigure>();
            yield return null;
            var rest = Shown(wambui);
            Assert.That(rest.dip, Is.EqualTo(0).Within(0.001f));
            // One pose is drawn at a time: the old one dims, then the new one brightens.
            wambui.SetPose(1);
            Assert.That(Shown(wambui).frame, Is.EqualTo(rest.frame), "the change starts from the pose that was showing");
            Assert.That(Shown(wambui).dip, Is.EqualTo(0).Within(0.001f), "at full light");
            yield return Until(() => wambui.PoseBlend > 0.6f, 5);
            var rising = Shown(wambui);
            Assert.That(rising.frame, Is.Not.EqualTo(rest.frame), "the swap happened at the bottom of the dip");
            // Another change while the light returns continues from what is shown, at the same brightness.
            wambui.SetPose(2);
            Assert.That(Shown(wambui).frame, Is.EqualTo(rising.frame));
            Assert.That(Shown(wambui).dip, Is.EqualTo(rising.dip).Within(0.001f));
            yield return Until(() => wambui.PoseBlend >= 1, 5);
            var settled = Shown(wambui);
            Assert.That(settled.dip, Is.EqualTo(0).Within(0.001f));
            Assert.That(settled.frame, Is.Not.EqualTo(rest.frame).And.Not.EqualTo(rising.frame), "and it ends in the latest pose");
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [UnityTest]
        public IEnumerator SheepIsVisibleWhenNamedAndWithdrawsWithTheStory()
        {
            yield return Open(); app.enabled = false;
            stage.ShowScene("story.water.1850s");
            stage.Perform(new StoryAction(ActionKind.Reveal, "figure.ewe"));
            yield return new WaitForSeconds(9f);
            var ewe = stage.Find("figure.ewe");
            Assert.That(ewe.Visible, Is.True, "the sheep is in the story, so its sounds play");
            Assert.That(ewe.Surfaces.Count(r => r.enabled), Is.EqualTo(7), "the sheep named in the story has a readable body, head, face and legs");
            stage.Perform(new StoryAction(ActionKind.Withdraw, "figure.ewe"));
            yield return new WaitForSeconds(12f);
            Assert.That(ewe.Surfaces.Any(r => r.enabled), Is.False, "the solid animal leaves with the women");
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [UnityTest]
        public IEnumerator AGiftTakenWhileStillInThePaintedPalmsStaysWithTheVisitor()
        {
            yield return Open(); app.enabled = false;
            stage.ShowScene("story.water.1850s");
            stage.Perform(new StoryAction(ActionKind.Reveal, "figure.naserian"));
            var giver = stage.Find("figure.naserian").GetComponent<IllustratedFigure>();
            var bead = stage.Find("bead");
            stage.Perform(new StoryAction(ActionKind.Offer, "bead"));
            yield return Until(() => giver.PoseBlend >= 0.5f, 5);
            yield return null;
            Assert.That(giver.Offering, Is.EqualTo(bead));
            stage.Carry(bead, null);
            Assert.That(giver.Offering, Is.Null, "received by the button before it crossed");
            yield return new WaitForSeconds(2.5f);
            Assert.That(Vector3.Distance(bead.transform.position, giver.GiftOrigin), Is.GreaterThan(0.5f), "it is not pulled back to her palm");
            Assert.That(Vector3.Distance(bead.transform.position, stage.OfferPosition()), Is.GreaterThan(0.05f), "nor sent on to the offer point afterwards");
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [UnityTest]
        public IEnumerator FlourSetDownWithinReachCrossesToRestBetweenTheWomen()
        {
            yield return Open(); app.enabled = false;
            stage.ShowScene("story.water.1850s");
            foreach (var id in new[] { "figure.wambui", "figure.naserian", "gourd.flour", "hide" }) stage.Perform(new StoryAction(ActionKind.Reveal, id));
            var hide = stage.Find("hide");
            var gourd = stage.Find("gourd.flour");
            var seat = Camera.main.transform.position;
            var reach = Vector3.Distance(seat, hide.transform.position);
            stage.Perform(new StoryAction(ActionKind.Place, "gourd.flour", "hide"));
            yield return new WaitForSeconds(1.6f);
            Assert.That(Vector3.Distance(gourd.transform.position, hide.PlacePosition), Is.LessThan(0.08f), "first it is set down within reach");
            Assert.That(theatre.CrossedIntoPainting, Is.False);
            yield return new WaitForSeconds(4.5f);
            Assert.That(theatre.CrossedIntoPainting, Is.True);
            var wambui = stage.Find("figure.wambui").transform.position;
            var naserian = stage.Find("figure.naserian").transform.position;
            Assert.That(Vector3.Distance(seat, hide.transform.position), Is.GreaterThan(reach + 0.8f), "then it crosses into the painting");
            Assert.That(Vector3.Distance(gourd.transform.position, hide.PlacePosition), Is.LessThan(0.08f), "the flour rests on the hide");
            var between = Vector3.Distance(wambui, hide.transform.position) + Vector3.Distance(hide.transform.position, naserian);
            Assert.That(between, Is.LessThan(Vector3.Distance(wambui, naserian) * 1.35f), "between the two women");
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [UnityTest]
        public IEnumerator PaintedPeopleTurnToTheVisitorAndFollowTheirLook()
        {
            yield return Open(); app.enabled = false;
            stage.ShowScene("story.water.1850s");
            foreach (var id in new[] { "figure.wambui", "figure.naserian", "gourd.milk" }) stage.Perform(new StoryAction(ActionKind.Reveal, id));
            var wambui = stage.Find("figure.wambui").GetComponent<IllustratedFigure>();
            var eye = Camera.main.transform.position;
            var before = Yaw(wambui, eye);
            // Looked at, she turns toward the visitor, and settles back once the moment has passed.
            stage.AnswerLook("figure.wambui");
            yield return new WaitForSeconds(0.8f);
            Assert.That(wambui.GlanceWeight, Is.GreaterThan(0.9f));
            Assert.That(Yaw(wambui, eye), Is.LessThan(before - 5f), "she has turned toward the visitor");
            yield return new WaitForSeconds(3.2f);
            Assert.That(wambui.GlanceWeight, Is.LessThan(0.01f), "and turned back");
            Assert.That(Yaw(wambui, eye), Is.EqualTo(before).Within(0.5f));
            // Looking at a thing, the nearest painted person follows the look to it.
            var naserian = stage.Find("figure.naserian").GetComponent<IllustratedFigure>();
            stage.AnswerLook("gourd.milk");
            yield return new WaitForSeconds(0.8f);
            Assert.That(naserian.GlanceWeight + wambui.GlanceWeight, Is.GreaterThan(0.9f), "someone follows the visitor's look");
            // Acting, the visitor is acknowledged; paused, the turn holds still.
            yield return new WaitForSeconds(3.2f);
            stage.AcknowledgeVisitor();
            yield return new WaitForSeconds(0.3f);
            StageClock.Paused = true;
            var held = naserian.GlanceWeight + wambui.GlanceWeight;
            yield return new WaitForSeconds(0.5f);
            Assert.That(naserian.GlanceWeight + wambui.GlanceWeight, Is.EqualTo(held).Within(0.0001f), "pause holds the turn");
            StageClock.Paused = false;
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [UnityTest]
        public IEnumerator PaintedPalmAndHeldGiftStayTogetherThroughAcknowledgementAndRecenter()
        {
            yield return Open(); app.enabled = false;
            stage.ShowScene("story.water.1850s");
            stage.Perform(new StoryAction(ActionKind.Reveal, "figure.wambui"));
            stage.Perform(new StoryAction(ActionKind.Reveal, "gourd.flour"));
            var woman = stage.Find("figure.wambui").GetComponent<IllustratedFigure>();
            var palm = woman.Surface.transform.InverseTransformPoint(woman.GiftOrigin);
            stage.AnswerLook("figure.wambui");
            yield return new WaitForSeconds(0.8f);
            Assert.That(Vector3.Distance(palm, woman.Surface.transform.InverseTransformPoint(woman.GiftOrigin)), Is.LessThan(0.001f), "the vessel stays on the same painted palm as she turns");
            var turning = woman.Surface.transform.localRotation;
            StageClock.Paused = true;
            var frame = woman.transform.parent;
            frame.SetPositionAndRotation(frame.position + new Vector3(1, 0.1f, 0.4f), Quaternion.Euler(0, 35, 0) * frame.rotation);
            yield return null;
            Assert.That(Quaternion.Angle(turning, woman.Surface.transform.localRotation), Is.LessThan(0.01f), "recenter preserves the stage-relative glance, including during pause");
            Assert.That(Vector3.Distance(palm, woman.Surface.transform.InverseTransformPoint(woman.GiftOrigin)), Is.LessThan(0.001f));
            StageClock.Paused = false;
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [UnityTest]
        public IEnumerator ReducedMotionAcknowledgesWithoutTurningOrDippingThePaintedFigure()
        {
            yield return Open(); app.enabled = false;
            stage.ShowScene("story.water.1850s");
            stage.Perform(new StoryAction(ActionKind.Reveal, "figure.wambui"));
            var transition = stage.GetComponent<StoryTransition>();
            transition.ReducedMotion = true;
            var woman = stage.Find("figure.wambui").GetComponent<IllustratedFigure>();
            stage.AnswerLook("figure.wambui");
            woman.SetPose(1);
            yield return new WaitForSeconds(0.3f);
            Assert.That(woman.GlanceWeight, Is.GreaterThan(0), "the story still acknowledges attention");
            Assert.That(Quaternion.Angle(woman.Surface.transform.localRotation, Quaternion.identity), Is.LessThan(0.01f));
            Assert.That(woman.PoseBlend, Is.EqualTo(1), "reduced motion uses a clean pose swap with no dip");
            var properties = new MaterialPropertyBlock(); woman.Surface.GetPropertyBlock(properties);
            Assert.That(properties.GetFloat("_Highlight"), Is.GreaterThan(0), "acknowledgement remains visible in the light");
            transition.ReducedMotion = false;
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [UnityTest]
        public IEnumerator IllustratedVisitCanFinishKeepAndReplayItsAcceptedPoses()
        {
            yield return Open(); Time.timeScale = 8; app.Begin();
            yield return Until(() => app.Flow.Phase == VisitPhase.Choosing);
            Assert.That(app.Flow.Choose("era.1850s"), Is.True);
            for (var replay = 0; replay < 2; replay++)
            {
                var deadline = Time.realtimeSinceStartup + 140;
                var flourObserved = false;
                while (app.Flow.Phase != VisitPhase.Closing || app.Flow.CurrentLine != null)
                {
                    Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "the full illustrated visit completes");
                    if (replay == 0 && app.Flow.OpenInvitation != null) app.Flow.Accept(app.Flow.OpenInvitation.Id);
                    if (!flourObserved && stage.ActiveScene == "story.water.1850s" && theatre.AcceptedResponses >= 2)
                    {
                        Assert.That(stage.Find("figure.wambui").GetComponent<IllustratedFigure>().Pose, Is.EqualTo(2));
                        flourObserved = true;
                    }
                    yield return null;
                }
                Assert.That(flourObserved, Is.True, "the receive outcome selects the same illustration during replay");
                Assert.That(stage.ActiveScene, Is.EqualTo("hearth"));
                Assert.That(app.Flow.Journal.Completed, Is.True);
                if (replay == 0)
                {
                    Assert.That(app.Flow.Keep(), Is.True);
                    Assert.That(app.Flow.BeginAgain(), Is.True);
                    yield return null;
                    Assert.That(app.Flow.Replay(), Is.True);
                }
            }
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [UnityTest]
        public IEnumerator QuietGuestHearsRecordedStoryAndCanKeepTheBeadThroughTheContinuation()
        {
            yield return Open(); Time.timeScale = 10; app.Begin();
            yield return Until(() => app.Flow.Phase == VisitPhase.Choosing);
            app.Flow.Choose("era.1850s");
            var narrator = Object.FindFirstObjectByType<Runtime.Narration.NarrationPlayer>();
            for (var visit = 0; visit < 2; visit++)
            {
                var deadline = Time.realtimeSinceStartup + 160;
                var gateObserved = false;
                while (app.Flow.Phase != VisitPhase.Closing || app.Flow.CurrentLine != null)
                {
                    Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline));
                    if (narrator.Speaking)
                        Assert.That(narrator.CaptionOnly, Is.False, "revised words have recorded narration");
                    if (visit == 1 && app.Flow.CurrentLine?.Id == "today.bead.no")
                    {
                        gateObserved = true;
                        Assert.That(Vector3.Distance(stage.Find("bead.today").transform.position,
                            stage.Find("gate").PlacePosition), Is.GreaterThan(0.15f), "silence does not hang the bead");
                    }
                    yield return null;
                }
                Assert.That(app.Flow.Journal.Outcomes.Values, Is.All.EqualTo(Outcome.Default));
                if (visit == 0)
                {
                    Assert.That(app.Flow.Journal.Heard, Does.Contain("home.trade.quiet").And.Contain("home.bead.beside"));
                    Assert.That(app.Flow.Keep(), Is.True);
                    Assert.That(app.Flow.BeginAgain(), Is.True);
                    yield return null;
                    Assert.That(app.Flow.Follow(), Is.True);
                }
                else
                {
                    Assert.That(gateObserved, Is.True);
                    Assert.That(app.Flow.Journal.Heard, Does.Contain("again.trade.quiet").And.Contain("today.brought.carried"));
                }
            }
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        static void Capture(Camera eye, string name)
        {
            var directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "artifacts", "illustrated-theatre"));
            Directory.CreateDirectory(directory);
            var target = new RenderTexture(1440, 1080, 24) { antiAliasing = 4 };
            var previous = eye.targetTexture; var previousActive = RenderTexture.active;
            eye.targetTexture = target; eye.Render(); RenderTexture.active = target;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
            File.WriteAllBytes(Path.Combine(directory, name + ".png"), image.EncodeToPNG());
            eye.targetTexture = previous; RenderTexture.active = previousActive;
            Object.Destroy(image); target.Release(); Object.Destroy(target);
        }

        [UnityTest]
        public IEnumerator CaptureIllustratedVisit()
        {
            if (Environment.GetEnvironmentVariable("SANKOFA_CAPTURE") != "1") Assert.Ignore("Set SANKOFA_CAPTURE=1 to capture the MVP.");
            yield return Open();
            GameObject.Find("OVRCameraRig").transform.position = new Vector3(0, 1.15f, 0);
            var eye = GameObject.Find("CenterEyeAnchor").GetComponent<Camera>(); eye.fieldOfView = 78;
            Object.FindFirstObjectByType<WorldAnchor>().Place();
            yield return new WaitForSeconds(1);
            Capture(eye, "01-hearth");
            Time.timeScale = 6; app.Begin();
            yield return Until(() => app.Flow.Phase == VisitPhase.Choosing);
            app.Flow.Choose("era.1850s");
            yield return Until(() => app.Flow.OpenInvitation?.Id == "receive-ember"); app.Flow.Accept("receive-ember");
            yield return Until(() => app.Flow.OpenInvitation?.Id == "touch-water");
            Capture(eye, "02-unfolded"); app.Flow.Accept("touch-water");
            yield return Until(() => app.Flow.CurrentLine?.Id == "e1.flour");
            Capture(eye, "03-painted-person");
            yield return Until(() => app.Flow.CurrentLine?.Id == "e1.carry");
            Time.timeScale = 1;
            // A real-time, fixed 12 fps review strip. This is desktop staging evidence, not device performance.
            Time.captureFramerate = 12;
            var received = false; var afterReceive = 0; var frames = 0;
            for (var i = 0; i < 240; i++)
            {
                if (!received && i >= 24 && app.Flow.OpenInvitation?.Id == "receive-flour") received = app.Flow.Accept("receive-flour");
                Capture(eye, "handover-" + i.ToString("D4")); frames++; yield return null;
                if (received && ++afterReceive >= 36) break;
            }
            Time.captureFramerate = 0;
            File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "artifacts", "illustrated-theatre", "handover-frame-count.txt")), frames.ToString());
            Assert.That(received, Is.True, "the captured visitor received the gourd");
            Capture(eye, "04-received"); Time.timeScale = 6;
            yield return Until(() => app.Flow.OpenInvitation?.Id == "offer-flour");
            Capture(eye, "05-second-beat"); app.Flow.Accept("offer-flour");
            yield return new WaitForSeconds(1.5f); Capture(eye, "06-trade-response");
            yield return Until(() => app.Flow.CurrentLine?.Id == "e1.rest");
            Capture(eye, "06b-sheep-promise");
            yield return Until(() => app.Flow.OpenInvitation?.Id == "receive-bead");
            // Once her offering pose shows, the bead rests in her palms before it crosses.
            yield return new WaitForSeconds(0.6f);
            Capture(eye, "07-keepsake-offer"); app.Flow.Accept("receive-bead");
            yield return Until(() => app.Flow.Phase == VisitPhase.Closing && app.Flow.CurrentLine == null, 120);
            Capture(eye, "08-return");
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }
    }
}
