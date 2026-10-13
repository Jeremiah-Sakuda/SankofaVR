using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Sankofa.Flow;
using Sankofa.Runtime.App;
using Sankofa.Runtime.Stage;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace Sankofa.Tests
{
    public class LivingGalleryTests
    {
        SankofaApp app; StoryStage stage; LivingGallery gallery; string folder;
        readonly List<string> problems=new List<string>();
        [SetUp] public void Setup()
        {
            folder=Path.Combine(Path.GetTempPath(),"sankofa-gallery-"+Guid.NewGuid().ToString("N"));
            SankofaApp.StoreFolderOverride=folder;StageClock.Paused=false;AudioListener.volume=0;
            Application.logMessageReceived+=Watch;LogAssert.ignoreFailingMessages=true;
        }
        void Watch(string s,string stack,LogType type)
        { if((type==LogType.Error||type==LogType.Exception||type==LogType.Assert)&&(s.Contains("[Sankofa]")||(stack??"").Contains("Sankofa.")))problems.Add(s); }
        [TearDown] public void Cleanup()
        {
            AudioListener.volume=0;Time.timeScale=1;StageClock.Paused=false;SankofaApp.StoreFolderOverride=null;
            Application.logMessageReceived-=Watch;if(Directory.Exists(folder))Directory.Delete(folder,true);
        }
        static IEnumerator Until(Func<bool> ready,float seconds=130)
        {
            var deadline=Time.realtimeSinceStartup+seconds;
            while(!ready()) {Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline),"gallery flow timed out");yield return null;}
        }
        IEnumerator Open()
        {
            yield return SceneManager.LoadSceneAsync("LivingGallery");
            GameObject.Find("OVRCameraRig").transform.position=new Vector3(0,1.15f,0);
            Object.FindFirstObjectByType<WorldAnchor>().Place();
            app=Object.FindFirstObjectByType<SankofaApp>();stage=Object.FindFirstObjectByType<StoryStage>();gallery=Object.FindFirstObjectByType<LivingGallery>();
            yield return Until(()=>app.Flow?.Phase==VisitPhase.Choosing);
            Assert.That(app.LoadProblem,Is.Null);yield return new WaitForSeconds(0.3f);
        }
        [UnityTest] public IEnumerator PreviewWaitsAndHasNoAutomaticChoiceOrSave()
        {
            yield return Open();Time.timeScale=15;yield return new WaitForSeconds(45);
            Assert.That(app.Flow.Phase,Is.EqualTo(VisitPhase.Choosing));Assert.That(app.Flow.Journal,Is.Null);
            Assert.That(gallery.Framed,Is.True);Assert.That(Directory.Exists(folder)&&Directory.GetFiles(folder).Length>0,Is.False);
            Capture("01-gallery");Assert.That(problems,Is.Empty,string.Join("\n",problems));
        }
        [UnityTest] public IEnumerator BothThreadsReturnToTheSamePaintingAndContinueFromKeptOutcomes()
        {
            yield return Open();Time.timeScale=10;
            var original=GameObject.Find("Canonical canvas").GetComponent<Renderer>().sharedMaterial.mainTexture;
            foreach(var route in new[]{"era.1850s","era.1850s.remembered"})
            {
                Assert.That(app.EnterExhibit(route),Is.True);
                var deadline=Time.realtimeSinceStartup+150;
                var captured=false;
                while(app.Flow.Phase!=VisitPhase.Closing||app.Flow.CurrentLine!=null)
                {
                    Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline));
                    if(app.Flow.OpenInvitation!=null)app.Flow.Accept(app.Flow.OpenInvitation.Id);
                    if(!captured&&app.Flow.CurrentLine?.Id==(route=="era.1850s"?"gallery.portions.divide":"gallery.waiting.stay"))
                    { Capture(route=="era.1850s"?"02-what-she-brings":"03-what-she-remembers");captured=true; }
                    yield return null;
                }
                Assert.That(captured,Is.True);Assert.That(gallery.Framed,Is.True);
                Assert.That(GameObject.Find("Canonical canvas").GetComponent<Renderer>().sharedMaterial.mainTexture,Is.SameAs(original));
                Assert.That(app.Flow.Journal.Outcomes["receive-milk"],Is.EqualTo(Core.Story.Outcome.Accepted));
                Capture("04-return");Assert.That(app.Flow.Keep(),Is.True);app.Flow.BeginAgain();
                yield return null;
            }
            Assert.That(app.Flow.Follow(),Is.True);
            while(app.Flow.Phase!=VisitPhase.Closing||app.Flow.CurrentLine!=null)
            { if(app.Flow.OpenInvitation!=null)app.Flow.Accept(app.Flow.OpenInvitation.Id);yield return null; }
            Assert.That(app.Flow.Journal.Completed,Is.True);Assert.That(gallery.Framed,Is.True);
            Assert.That(problems,Is.Empty,string.Join("\n",problems));
        }
        [UnityTest] public IEnumerator PassiveThreadAndReducedMotionKeepTheVisitorStationary()
        {
            yield return Open();Time.timeScale=10;
            stage.GetComponent<StoryTransition>().ReducedMotion=true;
            var eye=Camera.main.transform;var position=eye.position;var rotation=eye.rotation;
            Assert.That(app.EnterExhibit("era.1850s.remembered"),Is.True);
            var deadline=Time.realtimeSinceStartup+150;
            while(app.Flow.Phase!=VisitPhase.Closing||app.Flow.CurrentLine!=null)
            { Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline));yield return null; }
            Assert.That(app.Flow.Journal.Outcomes.Values,Is.All.EqualTo(Core.Story.Outcome.Default));
            Assert.That(Vector3.Distance(eye.position,position),Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(eye.rotation,rotation),Is.LessThan(0.001f));
            Assert.That(gallery.Framed,Is.True);Assert.That(problems,Is.Empty,string.Join("\n",problems));
        }
        [UnityTest] public IEnumerator ATrackedHandCanReceiveAndExplicitlyDepositTheMemento()
        {
            yield return Open();Time.timeScale=12;app.Begin();
            yield return Until(()=>app.Flow.Phase==VisitPhase.Closing&&app.Flow.CurrentLine==null,150);
            Time.timeScale=1;
            var item=stage.Find(app.Flow.Plan.Plan.Keepsake);
            var hand=Object.FindObjectsByType<Sankofa.Runtime.Input.HandSource>(FindObjectsSortMode.None).First(h=>!h.IsLeft);
            var at=item.transform.position;var open=true;
            hand.Simulated=()=>Palm(at,open);
            yield return Until(()=>item.CarriedBy==hand,6);
            at=Object.FindFirstObjectByType<GalleryKeep>().Recess.position+Vector3.up*0.1f;
            yield return new WaitForSeconds(0.6f);
            Assert.That(app.Flow.KeptVisit,Is.Null,"carrying over the recess alone is not saving");
            open=false;yield return Until(()=>app.Flow.KeptVisit!=null,6);
            hand.Simulated=null;
            Assert.That(Directory.GetFiles(folder).Length,Is.GreaterThan(0));
            Assert.That(problems,Is.Empty,string.Join("\n",problems));
        }
        [UnityTest] public IEnumerator AnObjectQuestionAsksOnceWithoutAcceptingAnInvitationOrSpoilingWaiting()
        {
            yield return Open();Time.timeScale=10;app.Begin();
            yield return Until(()=>app.Flow.CurrentLine?.Id=="e1.price",150);
            Assert.That(app.Flow.Questions.Any(q=>q.Id=="q.e1.friends"),Is.False);
            var chips=Object.FindFirstObjectByType<Sankofa.Runtime.UI.ObjectQuestions>();
            yield return null;
            var chip=chips.Panels.First(p=>!p.Hidden&&p.Ids.Contains("q:q.e1.milk"));
            var outcomes=app.Flow.Journal.Outcomes.Count;
            chip.PressFromPointer("q:q.e1.milk");
            yield return Until(()=>app.Flow.Answering?.Id=="q.e1.milk",5);
            Assert.That(app.Flow.Journal.Outcomes.Count,Is.EqualTo(outcomes));
            Assert.That(app.Flow.Journal.Asked.Count(q=>q=="q.e1.milk"),Is.EqualTo(1));
            Assert.That(problems,Is.Empty,string.Join("\n",problems));
        }

        [UnityTest] public IEnumerator ModernRiverPresentationKeepsOnlyOneWaterAndTangibleForeground()
        {
            yield return Open();app.enabled=false;
            foreach(var panel in Object.FindObjectsByType<Sankofa.Runtime.UI.PokePanel>(FindObjectsSortMode.None))panel.Hidden=true;
            yield return gallery.Transition("story.water.today",new Continuity[0],true);
            foreach(var id in new[]{"gate","soil","jiko"})stage.Perform(new Sankofa.Core.Story.StoryAction(Sankofa.Core.Story.ActionKind.Reveal,id));
            yield return new WaitForSeconds(1.5f);
            var modern=stage.Scenes.Single(s=>s.Id=="story.water.today").Root;
            Assert.That(modern.transform.Find("Illustrated modern river").GetComponent<Renderer>().enabled,Is.True);
            Assert.That(modern.transform.Find("River").GetComponent<Renderer>().enabled,Is.False);
            Capture("05-modern-river");Assert.That(problems,Is.Empty,string.Join("\n",problems));
        }

        [UnityTest] public IEnumerator ALastKnownHandPoseCannotCompleteANewReceive()
        {
            yield return Open();app.enabled=false;
            var hand=Object.FindObjectsByType<Sankofa.Runtime.Input.HandSource>(FindObjectsSortMode.None).First(h=>!h.IsLeft);
            hand.enabled=false;var point=new Vector3(0.1f,1.0f,0.45f);var sample=Palm(point,true);sample.Tracked=true;
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            typeof(Sankofa.Runtime.Input.HandSource).GetField("tracked",flags).SetValue(hand,true);
            typeof(Sankofa.Runtime.Input.HandSource).GetField("live",flags).SetValue(hand,false);
            typeof(Sankofa.Runtime.Input.HandSource).GetField("last",flags).SetValue(hand,sample);
            var target=new GameObject("Test offer");target.transform.position=point;
            var receiver=Object.FindFirstObjectByType<Sankofa.Runtime.Input.PalmReceiver>();var received=false;
            receiver.Received+=(id,h)=>received=true;receiver.Listen("confidence-test",target.transform);
            yield return new WaitForSeconds(0.6f);
            Assert.That(hand.Holding,Is.True);Assert.That(received,Is.False,"last-known poses supply visuals, not a new act");
            hand.Simulated=()=>Palm(point,true);yield return Until(()=>received,3);
            receiver.Close();hand.Simulated=null;Object.Destroy(target);
            Assert.That(problems,Is.Empty,string.Join("\n",problems));
        }

        static Sankofa.Interaction.HandSample Palm(Vector3 point,bool open)
        {
            System.Numerics.Vector3 N(Vector3 v)=>new System.Numerics.Vector3(v.x,v.y,v.z);
            var c=point+Vector3.down*0.1f;
            return new Sankofa.Interaction.HandSample
            {
                Wrist=N(c+new Vector3(0,0,-0.045f)), MiddleProximal=N(c+new Vector3(0,0,0.045f)),
                IndexProximal=N(c+new Vector3(0.025f,0,0.04f)),LittleProximal=N(c+new Vector3(-0.03f,0,0.035f)),
                IndexTip=N(c+new Vector3(0.03f,0,open?0.12f:0.015f)),MiddleTip=N(c+new Vector3(0,0,open?0.13f:0.015f)),RingTip=N(c+new Vector3(-0.015f,0,open?0.125f:0.015f)),
            };
        }

        void Capture(string name)
        {
            if(Environment.GetEnvironmentVariable("SANKOFA_CAPTURE")!="1")return;
            var eye=GameObject.Find("CenterEyeAnchor").GetComponent<Camera>();eye.fieldOfView=70;
            var directory=Path.GetFullPath(Path.Combine(Application.dataPath,"..","..","artifacts","living-gallery"));Directory.CreateDirectory(directory);
            var target=new RenderTexture(1440,1080,24){antiAliasing=4};var previous=eye.targetTexture;var active=RenderTexture.active;
            eye.targetTexture=target;eye.Render();RenderTexture.active=target;
            var image=new Texture2D(1440,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,1080),0,0);image.Apply();
            File.WriteAllBytes(Path.Combine(directory,name+".png"),image.EncodeToPNG());eye.targetTexture=previous;RenderTexture.active=active;
            Object.Destroy(image);target.Release();Object.Destroy(target);
        }
    }
}
