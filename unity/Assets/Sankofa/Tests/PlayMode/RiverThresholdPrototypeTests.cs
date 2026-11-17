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
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Sankofa.Tests
{
    /// <summary>Real app, scene and hand/pointer adapter; simulated input and desktop rendering are not headset evidence.</summary>
    public sealed class RiverThresholdPrototypeTests
    {
        RiverThresholdPrototype prototype;
        PersonalStoryApp app;
        string shelf;
        readonly List<string> errors = new List<string>();

        [UnitySetUp]
        public IEnumerator Load()
        {
            errors.Clear(); LogAssert.ignoreFailingMessages = true; Application.logMessageReceived += Error;
            shelf = Path.Combine(Path.GetTempPath(), "sankofa-river-test-" + Guid.NewGuid().ToString("N"));
            PersonalStoryApp.StoreFolderOverride = shelf;
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/Sankofa/Scenes/RiverThresholdPrototype.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene("RiverThresholdPrototype");
#endif
            yield return null; yield return null;
            prototype = Object.FindFirstObjectByType<RiverThresholdPrototype>();
            Assert.That(prototype, Is.Not.Null);
            prototype.AutomaticClock = false;
            app = prototype.App;
            app.ServiceEndpoint = "invalid-endpoint";
            foreach (var p in Object.FindObjectsByType<PointerSource>(FindObjectsSortMode.None)) p.enabled = false;
        }

        [UnityTearDown]
        public IEnumerator Clean()
        {
            Time.captureDeltaTime = 0;
            var scene = SceneManager.GetSceneByName("RiverThresholdPrototype");
            if (scene.IsValid() && scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("River cleanup " + Guid.NewGuid().ToString("N")));
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
        void Advance(float seconds) { for (var i=0; i<seconds*100; i++) prototype.Advance(.01f); }
        IEnumerator Press(PokePanel panel, string id)
        {
            Assert.That(panel.gameObject.activeInHierarchy && !panel.Hidden, Is.True, panel.name);
            Assert.That(panel.Ids, Does.Contain(id));
            yield return new WaitForSecondsRealtime(.33f);
            panel.PressFromPointer(id); yield return null;
        }
        IEnumerator ChooseWater()
        {
            yield return Press(prototype.Controls, "primary");
            Assert.That(prototype.Sequence.Phase, Is.EqualTo(RiverThresholdPhase.Choosing));
            yield return Press(app.View.Panels.Single(p => p.name == "Interest choices"), "place-water");
            Assert.That(prototype.Sequence.Phase, Is.EqualTo(RiverThresholdPhase.Crossing));
        }

        [UnityTest]
        public IEnumerator PotChoiceDefersTheStoryAndNeverMovesTheTrackedRig()
        {
            var head = Camera.main.transform;
            var rig = head.root;
            var position = rig.position; var rotation = rig.rotation;
            Assert.That(app.PresentationHeld, Is.True);
            app.StartPrepared(); Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Intake));
            yield return ChooseWater();
            Assert.That(app.Session.Story, Is.Null, "nothing is narrated underneath the crossing");
            Advance(3);
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Intake));
            Advance(3.2f); yield return null;
            Assert.That(prototype.RiverVisible, Is.True);
            Assert.That(app.PresentationHeld, Is.False);
            Assert.That(app.Session.Chapter.Id, Is.EqualTo("place-water.listening"));
            Assert.That(rig.position, Is.EqualTo(position)); Assert.That(rig.rotation, Is.EqualTo(rotation));
        }

        [UnityTest]
        public IEnumerator PauseFocusRecoveryAndCancelCannotLeakADeferredStart()
        {
            yield return ChooseWater(); Advance(2);
            app.SendMessage("OnApplicationPause", true);
            var before = prototype.Sequence.Seconds; Advance(10);
            Assert.That(prototype.Sequence.Seconds, Is.EqualTo(before));
            Assert.That(app.Session.Story, Is.Null);
            app.SendMessage("OnApplicationPause", false);
            yield return Press(prototype.Controls, "primary"); Advance(15);
            Assert.That(prototype.Sequence.Phase, Is.EqualTo(RiverThresholdPhase.Choosing));
            Assert.That(app.PresentationHeld, Is.False);
            Assert.That(app.Session.Story, Is.Null, "the cancelled water selection cannot complete later");
        }

        [UnityTest]
        public IEnumerator ReturningToTableAndRiverKeepsTheSameRealisedStory()
        {
            yield return ChooseWater(); Advance(6.2f); yield return null;
            var story = app.Session.Story; var chapter = app.Session.Chapter;
            yield return Press(prototype.Controls, "primary");
            Assert.That(prototype.RiverVisible, Is.False);
            Assert.That(app.View.TableRoot.gameObject.activeInHierarchy, Is.True);
            Assert.That(app.View.Stage.GetComponentsInChildren<Renderer>().Any(r => r.enabled), Is.True);
            yield return Press(prototype.Controls, "primary"); Advance(6.2f); yield return null;
            Assert.That(prototype.Sequence.Phase, Is.EqualTo(RiverThresholdPhase.Riverside));
            Assert.That(app.Session.Story, Is.SameAs(story)); Assert.That(app.Session.Chapter, Is.SameAs(chapter));
        }

        [UnityTest]
        public IEnumerator OneHandPinchReleaseCanSkipAndPauseWithoutAController()
        {
            var pointer = Object.FindFirstObjectByType<PointerSource>();
            var panel = prototype.Controls;
            var button = panel.transform.Find("Button primary");
            var hit = new Ray(button.position-button.forward*.35f, button.forward);
            pointer.ProcessPointer(hit, 0, true, 1, .02f);
            pointer.ProcessPointer(hit, 1, true, 1, .02f);
            Assert.That(prototype.Sequence.Phase, Is.EqualTo(RiverThresholdPhase.Opening));
            pointer.ProcessPointer(hit, 0, true, 1, .02f); yield return null;
            Assert.That(prototype.Sequence.Phase, Is.EqualTo(RiverThresholdPhase.Choosing));
            yield return Press(panel, "motion");
            Assert.That(prototype.Sequence.ReducedMotion, Is.True);
            yield return Press(panel, "pause");
            Assert.That(app.Session.Paused, Is.True);
            yield return Press(panel, "pause"); Assert.That(app.Session.Paused, Is.False);
            var interests=app.View.Panels.Single(p=>p.name=="Interest choices");
            var pot=interests.transform.Find("Button place-water");
            hit=new Ray(pot.position-pot.forward*.35f,pot.forward);
            pointer.ProcessPointer(hit,0,true,1,.02f); pointer.ProcessPointer(hit,1,true,1,.02f);
            pointer.ProcessPointer(hit,0,true,1,.02f); yield return null;
            Assert.That(prototype.Sequence.Phase,Is.EqualTo(RiverThresholdPhase.Crossing),"one hand's pinch-release selects the actual pot card");
        }

        [UnityTest]
        public IEnumerator FoleyFiresOnceAndPauseAndReducedMotionHoldThePaint()
        {
            yield return ChooseWater();
            Assert.That(prototype.LastSoundCue, Is.EqualTo("pot-touch"));
            var count=prototype.RequestedSoundCues;
            var river=Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(r=>r.name=="River beside the seat");
            var block=new MaterialPropertyBlock(); river.GetPropertyBlock(block); var clock=block.GetFloat("_Clock");
            app.Session.Pause(true); Advance(20); river.GetPropertyBlock(block);
            Assert.That(block.GetFloat("_Clock"),Is.EqualTo(clock));
            Assert.That(prototype.RequestedSoundCues,Is.EqualTo(count));
            app.Session.Pause(false); app.SetReducedMotion(true); Advance(2);
            Assert.That(prototype.LastSoundCue,Is.EqualTo("water-stir"));
            Assert.That(prototype.RequestedSoundCues,Is.EqualTo(count+1));
            Advance(3); Assert.That(prototype.RequestedSoundCues,Is.EqualTo(count+1));
            river.GetPropertyBlock(block); Assert.That(block.GetFloat("_Motion"),Is.Zero);
            Advance(2); yield return null; yield return null;
            var fire=Object.FindFirstObjectByType<RiverStoryFire>();
            var tongue=fire.Tongues[0]; tongue.GetPropertyBlock(block);
            Assert.That(block.GetFloat("_Motion"),Is.Zero);
            clock=block.GetFloat("_Clock"); app.Session.Pause(true); Advance(20); yield return null; yield return null;
            tongue.GetPropertyBlock(block); Assert.That(block.GetFloat("_Clock"),Is.EqualTo(clock));
            Assert.That(app.View.TableRoot.Find("Griot fire/Quiet tabletop fire").GetComponentsInChildren<Renderer>().All(r=>!r.enabled),Is.True,
                "the old additive flame must not reappear when the story starts");
        }

        [UnityTest]
        public IEnumerator AdjustingTheStandNeverLiftsTheRiverOrTheViewpoint()
        {
            yield return ChooseWater(); Advance(6.2f); yield return null;
            var world=GameObject.Find("River threshold world").transform;
            var before=world.position.y; var head=Camera.main.transform; var rig=head.root;
            var rigPosition=rig.position;
            var tableHeight=app.View.TableRoot.position.y;
            app.GetComponent<StoryTablePlacement>().AdjustHeight(.06f); yield return null;
            Assert.That(app.View.TableRoot.position.y,Is.GreaterThan(tableHeight));
            Assert.That(world.position.y,Is.EqualTo(before).Within(.001f));
            Assert.That(rig.position,Is.EqualTo(rigPosition));
        }

        [UnityTest]
        public IEnumerator ReviewSolidObjectsFromThreeAnglesAndPassTheGourd()
        {
            yield return Press(prototype.Controls,"primary");
            yield return null; yield return null;
            Assert.That(app.View.Panels.Single(p=>p.name=="Pass the gourd").Hidden,Is.True,"no invitation exists during intake");
            var capture=Environment.GetEnvironmentVariable("SANKOFA_CAPTURE")=="1";
            var table=app.View.TableRoot;
            var directory=Path.GetFullPath(Path.Combine(Application.dataPath,"../../artifacts/river-threshold"));
            Directory.CreateDirectory(directory);
            var eye=new GameObject("Object depth review camera").AddComponent<Camera>();
            eye.stereoTargetEye=StereoTargetEyeMask.None; eye.fieldOfView=51; eye.nearClipPlane=.03f;
            eye.clearFlags=CameraClearFlags.SolidColor; eye.backgroundColor=new Color(.008f,.012f,.022f);
            var target=new RenderTexture(1600,900,24){antiAliasing=4}; eye.targetTexture=target;
            var pixels=new Texture2D(1600,900,TextureFormat.RGB24,false);
            void Capture(string name,Vector3 at,Vector3 look)
            {
                if(!capture) return;
                eye.transform.position=table.TransformPoint(at); eye.transform.LookAt(table.TransformPoint(look));
                var previous=RenderTexture.active; eye.Render(); RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,1600,900),0,0); pixels.Apply(); RenderTexture.active=previous;
                File.WriteAllBytes(Path.Combine(directory,name+".png"),pixels.EncodeToPNG());
            }
            Capture("11-objects-front",new Vector3(0,.36f,-.36f),new Vector3(-.12f,.09f,.12f));
            Capture("12-objects-left",new Vector3(-.55f,.34f,-.18f),new Vector3(-.12f,.09f,.12f));
            Capture("13-objects-right",new Vector3(.45f,.38f,-.15f),new Vector3(-.12f,.09f,.12f));
            yield return Press(app.View.Panels.Single(p=>p.name=="Interest choices"),"trade-household");
            var stage=app.View.Stage;
            for(var until=Time.realtimeSinceStartup+4;stage.Rise<1&&Time.realtimeSinceStartup<until;) yield return null;
            Assert.That(app.Session.Next(),Is.True); yield return null; yield return null;
            stage.Seek(.4f); yield return new WaitForSecondsRealtime(.6f);
            yield return Press(app.View.Panels.Single(p=>p.name=="Follow naserian"),"choice:1");
            for(var until=Time.realtimeSinceStartup+4;(stage.Turning||stage.Rise<1)&&Time.realtimeSinceStartup<until;) yield return null;
            stage.Seek(.8f); yield return null; yield return null;
            Assert.That(stage.GourdInvited,Is.True);
            yield return Press(app.View.Panels.Single(p=>p.name=="Pass the gourd"),"pass");
            Assert.That(stage.VisitorPassedGourd,Is.True);
            Assert.That(stage.GourdPlace,Is.EqualTo(Sankofa.Core.Personal.TableStaging.GourdBetween));
            target.Release(); Object.Destroy(eye.gameObject); Object.Destroy(target); Object.Destroy(pixels);
        }

        [UnityTest]
        public IEnumerator CaptureActualOpeningAndRiver()
        {
            if (Environment.GetEnvironmentVariable("SANKOFA_CAPTURE") != "1") Assert.Ignore("Opt-in desktop visual evidence");
            var directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../artifacts/river-threshold"));
            Directory.CreateDirectory(directory);
            var movie = Environment.GetEnvironmentVariable("SANKOFA_CAPTURE_MOVIE") == "1";
            System.Diagnostics.Process encoder=null;
            if(movie)
            {
                var ffmpeg=Environment.GetEnvironmentVariable("SANKOFA_FFMPEG");
                Assert.That(File.Exists(ffmpeg),Is.True,"Movie capture requires SANKOFA_FFMPEG; frames are streamed, never left as a large sequence on disk.");
                encoder=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName=ffmpeg, UseShellExecute=false, CreateNoWindow=true, RedirectStandardInput=true,
                    Arguments="-v error -y -f rawvideo -pix_fmt rgb24 -s 1600x900 -r 24 -i - -vf vflip -an -c:v libx264 -preset fast -crf 20 -pix_fmt yuv420p \""+Path.Combine(directory,"river-threshold-silent.mp4")+"\""
                });
            }
            var eye = new GameObject("Stationary seated desktop evidence camera").AddComponent<Camera>();
            eye.stereoTargetEye = StereoTargetEyeMask.None;
            eye.transform.SetPositionAndRotation(new Vector3(0,1.15f,0), Quaternion.Euler(8,0,0));
            eye.fieldOfView = 60; eye.clearFlags = CameraClearFlags.SolidColor; eye.backgroundColor = new Color(.008f,.012f,.022f);
            var target = new RenderTexture(1600,900,24) { antiAliasing = 4 };
            eye.targetTexture = target;
            var pixels = new Texture2D(1600,900,TextureFormat.RGB24,false);
            Time.captureDeltaTime = 1f/24;
            app.TurnsPages = true;
            try
            {
            for (var f=0; f<50*24; f++)
            {
                // Three seconds to inspect the table; then its actual hand/pointer panel chooses the pot.
                if (f==35*24) app.View.Panels.Single(p => p.name=="Interest choices").PressFromPointer("place-water");
                prototype.Advance(1f/24); yield return null;
                var still = f==3*24 ? "01-invitation" : f==8*24 ? "02-griot" : f==16*24 ? "03-hands-and-pot" : f==29*24 ? "04-title"
                    : f==34*24 ? "05-choose-the-pot" : f==36*24 ? "06-threshold" : f==39*24 ? "07-river-reveal" : f==46*24 ? "08-beside-the-river" : null;
                if (still==null && !movie) continue;
                var previous = RenderTexture.active; eye.Render(); RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0); pixels.Apply();
                RenderTexture.active=previous;
                if(still!=null) File.WriteAllBytes(Path.Combine(directory,still+".png"),pixels.EncodeToPNG());
                if(movie) { var raw=pixels.GetRawTextureData<byte>().ToArray(); encoder.StandardInput.BaseStream.Write(raw,0,raw.Length); }
            }
            }
            finally
            {
            if(encoder!=null)
            {
                encoder.StandardInput.Close();
                if(!encoder.WaitForExit(15000)) { encoder.Kill(); Assert.Fail("Preview encoder did not finish"); }
                Assert.That(encoder.ExitCode,Is.Zero); encoder.Dispose();
            }
            }
            Assert.That(prototype.Sequence.Phase, Is.EqualTo(RiverThresholdPhase.Riverside));
            Assert.That(app.Session.Chapter.Id, Is.EqualTo("place-water.listening"));
            target.Release(); Object.Destroy(eye.gameObject); Object.Destroy(target); Object.Destroy(pixels);
        }
    }
}
