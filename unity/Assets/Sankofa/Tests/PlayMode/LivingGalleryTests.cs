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
        ExperienceSettings originalSettings;
        SankofaApp app; StoryStage stage; LivingGallery gallery; string folder;
        readonly List<string> problems=new List<string>();
        [SetUp] public void Setup()
        {
            problems.Clear();
            originalSettings=ExperienceSettings.Load();
            new ExperienceSettings().Save();
            folder=Path.Combine(Path.GetTempPath(),"sankofa-gallery-"+Guid.NewGuid().ToString("N"));
            SankofaApp.StoreFolderOverride=folder;StageClock.Paused=false;AudioListener.volume=0;
            Application.logMessageReceived+=Watch;LogAssert.ignoreFailingMessages=true;
        }
        void Watch(string s,string stack,LogType type)
        { if((type==LogType.Error||type==LogType.Exception||type==LogType.Assert)&&(s.Contains("[Sankofa]")||(stack??"").Contains("Sankofa.")))problems.Add(s); }
        [TearDown] public void Cleanup()
        {
            originalSettings.Save();
            AudioListener.volume=0;Time.timeScale=1;StageClock.Paused=false;SankofaApp.StoreFolderOverride=null;
            Application.logMessageReceived-=Watch;if(Directory.Exists(folder))Directory.Delete(folder,true);
            if(File.Exists(folder))File.Delete(folder);
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
            var choices=GameObject.Find("Choices panel").GetComponent<Sankofa.Runtime.UI.PokePanel>();
            Assert.That(choices.Ids,Is.EqualTo(new[]{"gallery-enter","Choose:era.1850s","Choose:era.1850s.remembered"}));
            Assert.That(GameObject.Find("Controls panel").GetComponent<Sankofa.Runtime.UI.PokePanel>().Ids,Does.Contain("Leave"));
            Assert.That(GameObject.Find("Keep recess label"),Is.Null,"keep guidance waits for a completed visit");
            var edge=choices.transform.Find("Button gallery-enter").TransformPoint(new Vector3(-0.21f,0,0));
            var eye=Camera.main.transform.position;
            Assert.That(choices.TryHit(new Ray(eye,(edge-eye).normalized),3,out _,out var id),Is.True);
            Assert.That(id,Is.EqualTo("gallery-enter"),"the primary action keeps its full-width hit target");
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
                    if(!captured&&app.Flow.CurrentLine?.Id==(route=="era.1850s"?"gallery.portions.divide":"gallery.waiting.stay")
                        &&stage.Find(route=="era.1850s"?"memory.portions":"memory.waiting").GetComponent<MemoryPerformance>().Progress>0.95f)
                    { Capture(route=="era.1850s"?"02-what-she-brings":"03-what-she-remembers");captured=true; }
                    yield return null;
                }
                Assert.That(captured,Is.True);Assert.That(gallery.Framed,Is.True);
                Assert.That(GameObject.Find("Canonical canvas").GetComponent<Renderer>().sharedMaterial.mainTexture,Is.SameAs(original));
                Assert.That(app.Flow.Journal.Outcomes["receive-milk"],Is.EqualTo(Core.Story.Outcome.Accepted));
                Capture("04-return");Assert.That(app.Flow.Keep(),Is.True);app.Flow.BeginAgain();
                var parent=app.Flow.KeptVisit.Id;
                Assert.That(app.Flow.Follow(),Is.True);
                Assert.That(app.Flow.Plan.Plan.Parent,Is.EqualTo(parent));
                Assert.That(app.Flow.Plan.Plan.Route,Is.EqualTo(route=="era.1850s"?"today.from-1850s":"today.from-remembered"));
                yield return Until(()=>app.Flow.Phase==VisitPhase.Closing&&app.Flow.CurrentLine==null,150);
                Assert.That(app.Flow.Journal.Completed,Is.True);Assert.That(gallery.Framed,Is.True);
                app.Flow.BeginAgain();
                yield return null;
            }
            Assert.That(problems,Is.Empty,string.Join("\n",problems));
        }
        [UnityTest] public IEnumerator PassiveThreadAndReducedMotionKeepTheVisitorStationary()
        {
            yield return Open();Time.timeScale=10;
            var panel=GameObject.Find("Settings panel").GetComponent<Sankofa.Runtime.UI.PokePanel>();
            panel.PressFromPointer("settings");yield return new WaitForSecondsRealtime(0.4f);
            panel.PressFromPointer("motion");yield return null;
            var transition=stage.GetComponent<StoryTransition>();
            Assert.That(ExperienceSettings.Load().ReducedMotion,Is.True);Assert.That(transition.ReducedMotion,Is.True);
            var eye=Camera.main.transform;var position=eye.position;var rotation=eye.rotation;
            Assert.That(app.EnterExhibit("era.1850s.remembered"),Is.True);
            var deadline=Time.realtimeSinceStartup+150;
            while(app.Flow.Phase!=VisitPhase.Closing||app.Flow.CurrentLine!=null)
            { Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline));Assert.That(transition.ReducedMotion,Is.True);Assert.That(gallery.Expansion,Is.Zero);yield return null; }
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
            Assert.That(app.Flow.Journal.Selection,Is.EqualTo(Core.Story.PerspectiveSelection.Default));
            yield return Until(()=>GameObject.Find("Keep recess label")!=null,3);
            var item=stage.Find(app.Flow.Plan.Plan.Keepsake);
            var hand=Object.FindObjectsByType<Sankofa.Runtime.Input.HandSource>(FindObjectsSortMode.None).First(h=>!h.IsLeft);
            var at=item.transform.position;var open=true;
            hand.Simulated=()=>Palm(at,open);
            yield return Until(()=>item.CarriedBy==hand,6);
            at=Object.FindFirstObjectByType<GalleryKeep>().Recess.position+Vector3.up*0.1f;
            yield return new WaitForSeconds(0.6f);
            Assert.That(app.Flow.KeptVisit,Is.Null,"carrying over the recess alone is not saving");
            Capture("06-keep-guide");
            File.WriteAllText(folder,"Deliberate local test obstruction");
            open=false;yield return new WaitForSeconds(0.5f);
            Assert.That(app.Flow.KeptVisit,Is.Null,"a failed write must not look like a saved deposit");
            Assert.That(item.CarriedBy,Is.SameAs(hand),"failure leaves the keepsake in the visitor's hand for retry");
            File.Delete(folder);
            open=true;yield return new WaitForSeconds(0.6f);
            open=false;yield return Until(()=>app.Flow.KeptVisit!=null,6);
            yield return Until(()=>GameObject.Find("Keep recess label")==null,3);
            Assert.That(GameObject.Find("Keep recess label"),Is.Null,"saved visits no longer invite another deposit");
            hand.Simulated=null;
            Assert.That(Directory.GetFiles(folder).Length,Is.GreaterThan(0));
            Assert.That(problems,Is.Empty,string.Join("\n",problems));
        }
        [UnityTest] public IEnumerator PhysicalReplacementNeedsDisclosureAndASecondDeliberateDeposit()
        {
            yield return Open();Time.timeScale=15;app.Begin();
            yield return Until(()=>app.Flow.Phase==VisitPhase.Closing&&app.Flow.CurrentLine==null,150);
            Assert.That(app.KeepVisit(),Is.True);var previous=app.Flow.KeptVisit.Id;
            app.Flow.BeginAgain();Assert.That(app.Flow.ChooseReturning("era.1850s.remembered"),Is.True);
            yield return Until(()=>app.Flow.Phase==VisitPhase.Closing&&app.Flow.CurrentLine==null,150);
            Time.timeScale=1;yield return Until(()=>GameObject.Find("Keep recess label")!=null,3);
            var label=GameObject.Find("Keep recess label").GetComponent<TMPro.TMP_Text>();
            Assert.That(label.text,Does.Contain("replace").And.Contain("previous"));
            var item=stage.Find(app.Flow.Plan.Plan.Keepsake);
            var hand=Object.FindObjectsByType<Sankofa.Runtime.Input.HandSource>(FindObjectsSortMode.None).First(h=>!h.IsLeft);
            var at=item.transform.position;var open=true;hand.Simulated=()=>Palm(at,open);
            yield return Until(()=>item.CarriedBy==hand,6);
            at=Object.FindFirstObjectByType<GalleryKeep>().Recess.position+Vector3.up*0.1f;
            yield return new WaitForSeconds(0.6f);
            Assert.That(label.text,Does.Contain("removed"));
            open=false;yield return new WaitForSeconds(0.4f);
            Assert.That(app.Flow.KeptVisit.Id,Is.EqualTo(previous));Assert.That(item.CarriedBy,Is.SameAs(hand));
            open=true;yield return new WaitForSeconds(0.6f);open=false;
            yield return Until(()=>app.Flow.KeptVisit.Id!=previous,6);
            Assert.That(app.Flow.KeptVisit.Route,Is.EqualTo("era.1850s.remembered"));hand.Simulated=null;
            Assert.That(problems,Is.Empty,string.Join("\n",problems));
        }

        [UnityTest] public IEnumerator PointerSlipGraceSelectsItsOriginalVisibleButtonAndCancelsHiddenOrChangedTargets()
        {
            yield return Open();app.enabled=false;
            var panel=new GameObject("Pointer adapter test panel").AddComponent<Sankofa.Runtime.UI.PokePanel>();
            panel.transform.position=new Vector3(0,1.15f,1);panel.Show(new[]{("gallery-enter","Enter")});
            foreach(var p in Object.FindObjectsByType<Sankofa.Runtime.Input.PointerSource>(FindObjectsSortMode.None))p.enabled=false;
            var pointer=Object.FindFirstObjectByType<Sankofa.Runtime.Input.PointerSource>(FindObjectsInactive.Include);
            typeof(Sankofa.Runtime.Input.PointerSource).GetField("panels",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(pointer,new[]{panel});
            var button=panel.transform.Find("Button gallery-enter");var origin=Camera.main.transform.position;
            var hit=new Ray(origin,(button.position-origin).normalized);var off=new Ray(origin,Vector3.up);
            var pressed=new List<string>();panel.Pressed+=pressed.Add;
            pointer.ProcessPointer(hit,0,true,3,0.01f);pointer.ProcessPointer(hit,1,true,3,0.01f);
            pointer.ProcessPointer(off,0,true,3,0.05f);yield return null;
            Assert.That(pressed,Is.EqualTo(new[]{"gallery-enter"}));pressed.Clear();yield return new WaitForSecondsRealtime(0.4f);
            pointer.ProcessPointer(hit,1,true,3,0.01f);panel.Hidden=true;
            pointer.ProcessPointer(off,0,true,3,0.05f);yield return null;Assert.That(pressed,Is.Empty);
            panel.Hidden=false;pointer.ProcessPointer(hit,1,true,3,0.01f);
            panel.Show(new[]{("gallery-enter","Changed label" )});pointer.ProcessPointer(off,0,true,3,0.05f);
            yield return null;Assert.That(pressed,Is.Empty);Object.Destroy(panel.gameObject);
            Assert.That(problems,Is.Empty,string.Join("\n",problems));
        }

        [UnityTest] public IEnumerator AuthoredActingAndMemoryActionsRespectPauseAndReducedMotionWithoutInventingChoices()
        {
            yield return Open();app.enabled=false;
            foreach(var panel in Object.FindObjectsByType<Sankofa.Runtime.UI.PokePanel>(FindObjectsSortMode.None))panel.Hidden=true;
            yield return gallery.Transition("story.water.1850s",new Continuity[0],true);
            var performance=Object.FindFirstObjectByType<GalleryPerformance>();
            var lines=app.Flow.Package.Package.Beats.SelectMany(b=>b.Lines).ToDictionary(l=>l.Id);
            foreach(var id in new[]{"figure.wambui","figure.naserian","figure.ewe","gourd.flour"})
                stage.Perform(new Core.Story.StoryAction(Core.Story.ActionKind.Reveal,id));
            performance.Speak(lines["e1.price"]);yield return new WaitForSeconds(1.8f);
            Assert.That(stage.Find("figure.wambui").GetComponent<IllustratedFigure>().Pose,Is.EqualTo(3));
            Assert.That(stage.Find("figure.naserian").GetComponent<IllustratedFigure>().Pose,Is.EqualTo(3));
            Capture("07-shortfall");
            var portions=stage.Find("memory.portions");stage.Perform(new Core.Story.StoryAction(Core.Story.ActionKind.Reveal,portions.Id));
            var left=portions.transform.Find("Journey portion");var right=portions.transform.Find("Home portion");
            var before=Vector3.Distance(left.localPosition,right.localPosition);
            performance.Speak(lines["gallery.portions.divide"]);yield return new WaitForSeconds(1);
            Assert.That(Vector3.Distance(left.localPosition,right.localPosition),Is.GreaterThan(before));
            StageClock.Paused=true;var held=left.localPosition;yield return new WaitForSeconds(0.5f);
            Assert.That(left.localPosition,Is.EqualTo(held));StageClock.Paused=false;
            yield return new WaitForSeconds(2.8f);
            Assert.That(left.localPosition.x,Is.EqualTo(-0.2f).Within(0.001f));Assert.That(right.localPosition.x,Is.EqualTo(0.2f).Within(0.001f));
            Capture("02-what-she-brings");stage.Perform(new Core.Story.StoryAction(Core.Story.ActionKind.Withdraw,portions.Id));
            var waiting=stage.Find("memory.waiting");stage.Perform(new Core.Story.StoryAction(Core.Story.ActionKind.Reveal,waiting.Id));
            var patient=waiting.transform.Find("Remembered woman 0");var arrives=waiting.transform.Find("Remembered woman 1");
            var patientPosition=patient.localPosition;
            stage.GetComponent<StoryTransition>().ReducedMotion=true;
            performance.Speak(lines["gallery.waiting.short"]);
            Assert.That(arrives.localPosition.x,Is.EqualTo(0.24f).Within(0.001f));
            performance.Speak(lines["gallery.waiting.stay"]);
            Assert.That(patient.localPosition,Is.EqualTo(patientPosition));Assert.That(arrives.localPosition.x,Is.EqualTo(0.14f).Within(0.001f));
            var paint=new MaterialPropertyBlock();arrives.GetComponent<Renderer>().GetPropertyBlock(paint);
            Assert.That(paint.GetVector("_FrameA").y,Is.EqualTo(0.5f),"Naserian lifts her face to listen as they find words");
            yield return new WaitForSeconds(1.5f);Capture("03-what-she-remembers");
            Assert.That(app.Flow.Journal,Is.Null,"performance alone never creates a visitor act or visit");
            performance.ResetState();Assert.That(arrives.localPosition.x,Is.EqualTo(0.36f).Within(0.001f));
            Assert.That(problems,Is.Empty,string.Join("\n",problems));
        }

        [UnityTest] public IEnumerator RecoveryCancelsTheLateTransitionAndKeepsItsSettledScene()
        {
            yield return Open();app.enabled=false;
            app.Flow.Choose("era.1850s");var cues=app.Flow.DrainCues();
            // Move the core through the hearth, allowing a transition request without normal presentation.
            for(var i=0;i<1000&&!cues.OfType<Cue.Transition>().Any();i++)
            {
                foreach(var speak in cues.OfType<Cue.Speak>())app.Flow.LineFinished(speak.Token);
                if(app.Flow.OpenInvitation!=null)app.Flow.Accept(app.Flow.OpenInvitation.Id);
                app.Flow.Tick(1);cues=app.Flow.DrainCues();
            }
            var change=cues.OfType<Cue.Transition>().Single();var transition=stage.GetComponent<StoryTransition>();
            transition.Play(change.Token,change.To.Id,change.Continuities);yield return null;
            transition.Recover(change);Assert.That(transition.Busy,Is.False);
            gallery.ResetView();stage.ShowScene("hearth");yield return new WaitForSeconds(4);
            Assert.That(stage.ActiveScene,Is.EqualTo("hearth"));Assert.That(gallery.Framed,Is.True);
            Assert.That(problems,Is.Empty,string.Join("\\n",problems));
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
