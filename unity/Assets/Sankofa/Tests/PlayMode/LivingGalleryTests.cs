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
        [UnityTest] public IEnumerator GallerySurroundIsCompleteClearAndWithdrawsWithTheStory()
        {
            yield return Open();
            var architecture=GameObject.Find("Gallery architecture");
            var surfaces=architecture.GetComponentsInChildren<Renderer>();
            var eye=Camera.main.transform;
            foreach(var direction in new[]{Vector3.forward,Vector3.back,Vector3.left,Vector3.right,Vector3.up,Vector3.down})
                Assert.That(surfaces.Any(r=>r.enabled&&r.bounds.IntersectRay(new Ray(eye.position,direction))),Is.True,"the seated surround is complete in "+direction);
            foreach(var name in new[]{"Title origin","Exhibit placard","Gallery title","Gallery purpose","Exhibit title"})
            {
                var label=GameObject.Find(name).GetComponent<TMPro.TMP_Text>();label.ForceMeshUpdate();
                var destination=label.GetComponent<Renderer>().bounds.center;
                var ray=new Ray(eye.position,(destination-eye.position).normalized);
                foreach(var surface in surfaces.Where(r=>r is MeshRenderer&&r.GetComponent<TMPro.TMP_Text>()==null))
                    if(surface.bounds.IntersectRay(ray,out var distance))
                        Assert.That(distance,Is.GreaterThan((destination-eye.position).magnitude-0.02f),name+" is occluded by "+surface.name);
            }
            Capture("environment-front",1920,1080,70);
            var rotation=eye.rotation;
            try
            {
                foreach(var view in new[]{("left",-65f,0f),("right",65f,0f),("rear",180f,0f),("ceiling",0f,-55f)})
                {
                    eye.rotation=rotation*Quaternion.Euler(view.Item3,view.Item2,0);
                    Capture("environment-"+view.Item1,1440,1080,75);
                }
            }
            finally {eye.rotation=rotation;}
            var home=stage.ActiveScene;
            stage.ShowScene("story.water.1850s");yield return null;
            Assert.That(surfaces.All(r=>!r.gameObject.activeInHierarchy),Is.True,"room architecture cannot intersect the unfolded story");
            stage.ShowScene(home);yield return null;
            Assert.That(architecture.activeInHierarchy,Is.True,"the room returns with its exhibit");
            Assert.That(problems,Is.Empty,string.Join("\n",problems));
        }
        [UnityTest] public IEnumerator WallContextIsReadableBesideEntryAndExpandedReturnMenus()
        {
            yield return Open();
            var labels=new[]{"Title origin","Exhibit placard","Gallery title","Gallery purpose","Exhibit title"}.Select(n=>GameObject.Find(n).GetComponent<TMPro.TMP_Text>()).ToArray();
            foreach(var text in labels)text.ForceMeshUpdate();
            var cameraObject=new GameObject("Context projection review");
            var review=cameraObject.AddComponent<Camera>();review.enabled=false;
            var eye=Camera.main.transform;var anchor=Object.FindFirstObjectByType<WorldAnchor>();
            try
            {
                foreach(var expanded in new[]{false,true})
                {
                    if(expanded)
                    {
                        // Exercise a larger return menu as well as the actual primary-first entry layout.
                        GameObject.Find("Choices panel").GetComponent<Sankofa.Runtime.UI.PokePanel>().Show(new List<(string,string)>
                        {
                            ("gallery-other","Their earlier meeting · shorter telling"),("Replay","Replay saved telling"),
                            ("Follow","The same water today"),("Forget","Forget saved telling"),
                            ("BeginAgain","Back to the artwork"),("Leave","Leave")
                        });
                        yield return null;
                    }
                    foreach(var height in new[]{0.85f,1.15f,1.45f})
                    foreach(var reach in new[]{0f,-0.06f,-0.12f})
                    {
                        GameObject.Find("OVRCameraRig").transform.position=new Vector3(0,height,0);
                        anchor.Place();anchor.ReachOffset=reach;
                        foreach(var lens in new[]{new Vector2(60,16f/9),new Vector2(60,2),new Vector2(90,1)})
                        foreach(var side in new[]{-0.032f,0.032f})
                        {
                            review.transform.SetPositionAndRotation(eye.position+eye.right*side,eye.rotation);
                            review.fieldOfView=lens.x;review.aspect=lens.y;
                            var context=labels.Select(t=>Projected(review,t.GetComponent<Renderer>().bounds)).ToArray();
                            for(var i=0;i<context.Length;i++)
                            for(var j=i+1;j<context.Length;j++)
                                Assert.That(context[i].Overlaps(context[j]),Is.False,labels[i].name+" and "+labels[j].name+" retain separate text blocks");
                            foreach(var rectangle in context)
                            {
                                Assert.That(rectangle.xMin,Is.GreaterThan(0),"context remains inside the forward field of view");
                                Assert.That(rectangle.xMax,Is.LessThan(1));
                                Assert.That(rectangle.yMin,Is.GreaterThan(0));Assert.That(rectangle.yMax,Is.LessThan(1));
                                foreach(var name in new[]{"Choices panel","Controls panel","Settings panel"})
                                foreach(var face in GameObject.Find(name).GetComponentsInChildren<Renderer>().Where(r=>r.gameObject.name=="Face"))
                                    Assert.That(rectangle.Overlaps(Projected(review,face.bounds)),Is.False,"wall context must not sit behind "+name);
                                Assert.That(rectangle.Overlaps(Projected(review,GameObject.Find("Canonical canvas").GetComponent<Renderer>().bounds)),Is.False,"context stays outside the artwork");
                            }
                        }
                    }
                }
                GameObject.Find("OVRCameraRig").transform.position=new Vector3(0,1.15f,0);
                anchor.Place();anchor.ReachOffset=0;
                // The review still shows the real entry layout, rather than the stress-test menu above.
                GameObject.Find("Choices panel").GetComponent<Sankofa.Runtime.UI.PokePanel>().Show(new List<(string,string)>
                {
                    ("gallery-enter","Enter painting"),("Choose:era.1850s","Wambui · before her journey"),
                    ("Choose:era.1850s.remembered","Naserian · their earlier meeting")
                },primaryFirst:true);
                yield return null;
                Capture("wall-context-clear",1920,1080,60);
            }
            finally {Object.Destroy(cameraObject);}
        }
        static Rect Projected(Camera camera,Bounds bounds)
        {
            var min=Vector2.one*float.PositiveInfinity;var max=Vector2.one*float.NegativeInfinity;
            for(var i=0;i<8;i++)
            {
                var corner=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                var p=camera.WorldToViewportPoint(corner);
                Assert.That(p.z,Is.GreaterThan(0));min=Vector2.Min(min,p);max=Vector2.Max(max,p);
            }
            return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
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
            panel.PressFromPointer("motion");yield return new WaitForSecondsRealtime(0.35f);
            panel.PressFromPointer("done");yield return new WaitForSecondsRealtime(0.35f);
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
            var active=Object.FindObjectsByType<Sankofa.Runtime.UI.PokePanel>(FindObjectsSortMode.None).Where(p=>!p.Hidden).SelectMany(p=>p.Ids).Where(id=>id.StartsWith("q:")).ToArray();
            Assert.That(active.Distinct().Count(),Is.EqualTo(active.Length),"a contextual question is not duplicated on the central panel");
            var household=chips.Panels.First(p=>!p.Hidden&&p.Ids.Contains("q:q.gallery.household"));
            Assert.That(Vector3.Distance(chip.transform.position,household.transform.position),Is.GreaterThan(0.09f),"questions about one object occupy separate rows");
            var milk=stage.Find("gourd.milk");milk.Hide();yield return null;
            Assert.That(GameObject.Find("Questions panel").GetComponent<Sankofa.Runtime.UI.PokePanel>().Ids,Does.Contain("q:q.e1.milk"),"unavailable object chips retain the accessible central question");
            milk.Reveal();yield return null;
            chip=chips.Panels.First(p=>!p.Hidden&&p.Ids.Contains("q:q.e1.milk"));
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

        [UnityTest] public IEnumerator CaptionsAvoidTheCarriedGourdAndHoldTheirPlacementForTheLine()
        {
            yield return Open();app.enabled=false;
            foreach(var panel in Object.FindObjectsByType<Sankofa.Runtime.UI.PokePanel>(FindObjectsSortMode.None))panel.Hidden=true;
            yield return gallery.Transition("story.water.1850s",new Continuity[0],true);
            var board=Object.FindFirstObjectByType<Sankofa.Runtime.UI.CaptionBoard>();
            var placement=board.GetComponent<Sankofa.Runtime.UI.GalleryCaptionPlacement>();
            Assert.That(placement,Is.Not.Null);
            var gourd=stage.Find("gourd.flour");
            stage.Perform(new Core.Story.StoryAction(Core.Story.ActionKind.Reveal,gourd.Id));
            var anchor=new GameObject("Synthetic caption obstruction");
            anchor.transform.position=board.transform.position+Camera.main.transform.forward*0.08f;
            gourd.Carry(null,()=>anchor.transform.position);
            board.Size=Sankofa.Runtime.UI.CaptionSize.Large;
            board.SetCaption("You have it. Keep it steady.");
            yield return new WaitForSeconds(1.5f);placement.LayoutNow();
            Assert.That(board.ContentOffset,Is.Not.EqualTo(Vector2.zero));
            foreach(var surface in gourd.Surfaces.Where(s=>s.enabled))
                Assert.That(Sankofa.Runtime.UI.GalleryCaptionPlacement.Overlap(placement.CaptionRect,
                    Sankofa.Runtime.UI.GalleryCaptionPlacement.Project(Camera.main,surface.bounds)),Is.LessThan(0.00005f));
            var held=board.ContentOffset;placement.LayoutNow();Assert.That(board.ContentOffset,Is.EqualTo(held));
            Assert.That(placement.CaptionRect.yMin,Is.GreaterThanOrEqualTo(0.04f));
            Assert.That(placement.CaptionRect.yMax,Is.LessThanOrEqualTo(0.93f));
            Capture("08-caption-clearance");
            foreach(var size in new[]{Sankofa.Runtime.UI.CaptionSize.Medium,Sankofa.Runtime.UI.CaptionSize.Large})
            {
                board.Size=size;board.SetCaption("The gourd rests in your hand. 'Thank you. I nearly turned back,' Wambui says.");
                yield return null;placement.LayoutNow();
                foreach(var surface in gourd.Surfaces.Where(s=>s.enabled))
                    Assert.That(Sankofa.Runtime.UI.GalleryCaptionPlacement.Overlap(placement.CaptionRect,
                        Sankofa.Runtime.UI.GalleryCaptionPlacement.Project(Camera.main,surface.bounds)),Is.LessThan(0.00005f),"long recorded caption avoids the carried object at both text sizes");
            }
            var backing=board.transform.Find("Caption backing");
            Assert.That(Mathf.Abs(backing.localPosition.y-board.CaptionText.transform.localPosition.y),Is.LessThan(0.12f));
            board.Clear();placement.LayoutNow();Assert.That(board.ContentOffset,Is.EqualTo(Vector2.zero));
            gourd.ResetToStart();Object.Destroy(anchor);Assert.That(problems,Is.Empty,string.Join("\n",problems));
        }

        [UnityTest] public IEnumerator TheSheepIsVisibleWhenNamedAndFocalPropsHaveReviewCaptures()
        {
            yield return Open();app.enabled=false;
            foreach(var panel in Object.FindObjectsByType<Sankofa.Runtime.UI.PokePanel>(FindObjectsSortMode.None))panel.Hidden=true;
            Object.FindFirstObjectByType<Sankofa.Runtime.UI.CaptionBoard>().Clear();
            yield return gallery.Transition("story.water.1850s",new Continuity[0],true);
            foreach(var id in new[]{"figure.wambui","figure.naserian","figure.ewe","gourd.flour","gourd.milk","hide"})
                stage.Perform(new Core.Story.StoryAction(Core.Story.ActionKind.Reveal,id));
            yield return new WaitForSeconds(1.8f);
            var ewe=stage.Find("figure.ewe");
            var rect=Sankofa.Runtime.UI.GalleryCaptionPlacement.Project(Camera.main,ewe.Surfaces.First().bounds);
            Assert.That(rect.xMin,Is.GreaterThan(0));Assert.That(rect.xMax,Is.LessThan(1));
            Assert.That(ewe.GetComponent<FigureMotion>(),Is.Null,"the illustrated animal does not inherit rigged arrival or gait");
            Assert.That(ewe.GetComponent<IllustratedFigure>().RegisteredPoseCount,Is.EqualTo(1));
            Capture("09-named-sheep");
            var eye=Camera.main.transform;var position=eye.position;var rotation=eye.rotation;
            eye.position=ewe.transform.position+new Vector3(-0.75f,0.65f,0.85f);eye.LookAt(ewe.transform.position+Vector3.up*0.45f);
            Capture("10-sheep-detail");
            var bead=stage.Find("bead");stage.Perform(new Core.Story.StoryAction(Core.Story.ActionKind.Reveal,bead.Id));yield return new WaitForSeconds(1.2f);
            eye.position=bead.transform.position+new Vector3(0,0.018f,-0.065f);eye.LookAt(bead.transform.position);
            var near=Camera.main.nearClipPlane;Camera.main.nearClipPlane=0.01f;Capture("11-bead-detail");Camera.main.nearClipPlane=near;
            eye.position=position;eye.rotation=rotation;
            Assert.That(problems,Is.Empty,string.Join("\n",problems));
        }

        [UnityTest] public IEnumerator RecordMutedSoftwarePreviewWhenExplicitlyRequested()
        {
            if(Environment.GetEnvironmentVariable("SANKOFA_DEMO")!="1")yield break;
            yield return Open();yield return MovieSegment("01-frame",40);
            var hand=Object.FindObjectsByType<Sankofa.Runtime.Input.HandSource>(FindObjectsSortMode.None).First(h=>!h.IsLeft);
            Assert.That(app.EnterExhibit("era.1850s"),Is.True);Time.timeScale=8;
            var recorded=new HashSet<string>();var deadline=Time.realtimeSinceStartup+220;
            while(app.Flow.Phase!=VisitPhase.Closing||app.Flow.CurrentLine!=null)
            {
                Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline));
                var line=app.Flow.CurrentLine?.Id;
                if(app.Flow.Phase==VisitPhase.Transitioning&&recorded.Add("entry-unfold"))yield return MovieSegment("02-entry-unfold",40);
                if(line=="gallery.portions.divide"&&recorded.Add(line))yield return MovieSegment("03-memory",40);
                if(line=="e1.rest"&&recorded.Add(line))yield return MovieSegment("05-agreement",40);
                if(line=="gallery.milk.held"&&recorded.Add(line))yield return MovieSegment("05-hospitality",30);
                var inv=app.Flow.OpenInvitation;
                if(inv!=null&&inv.Id=="receive-flour"&&recorded.Add("receive-flour"))
                {
                    Time.timeScale=1;var item=stage.Find("gourd.flour");
                    var at=item.transform.position;hand.Simulated=()=>Palm(item.CarriedBy==hand?at:item.transform.position,true);
                    yield return Until(()=>item.CarriedBy==hand,8);
                    at=new Vector3(0.13f,0.92f,0.38f);yield return new WaitForSeconds(0.5f);
                    yield return MovieSegment("02-gourd-received",40);hand.Simulated=null;Time.timeScale=8;
                }
                else if(inv!=null)
                {
                    if(inv.Id=="offer-flour"&&recorded.Add("offer-flour"))
                    {app.Flow.Accept(inv.Id);yield return MovieSegment("04-flour-placement",30);}
                    else app.Flow.Accept(inv.Id);
                }
                yield return null;
            }
            Assert.That(app.Flow.Journal.Outcomes["receive-flour"],Is.EqualTo(Core.Story.Outcome.Accepted));
            yield return MovieSegment("06-same-frame",40);
            Assert.That(app.Flow.Keep(),Is.True);app.Flow.BeginAgain();
            yield return MovieSegment("07-returning-memory",40);
            GameObject.Find("Choices panel").GetComponent<Sankofa.Runtime.UI.PokePanel>().PressFromPointer("gallery-other");
            yield return Until(()=>app.Flow.CurrentLine?.Id=="gallery.waiting.stay"&&stage.Find("memory.waiting").GetComponent<MemoryPerformance>().Progress>0.9f,60);
            yield return MovieSegment("08-other-memory",40);
            yield return Until(()=>app.Flow.Phase==VisitPhase.Closing&&app.Flow.CurrentLine==null,90);
            app.Flow.BeginAgain();app.Flow.NewStory();Assert.That(app.EnterExhibit("era.1850s"),Is.True);Time.timeScale=12;
            yield return Until(()=>app.Flow.CurrentLine?.Id=="e1.witness.quiet",90);
            yield return MovieSegment("09-passive-hospitality",30);
            Assert.That(app.Flow.Journal.Outcomes["offer-flour"],Is.EqualTo(Core.Story.Outcome.Default));
            Assert.That(problems,Is.Empty,string.Join("\n",problems));
        }

        [UnityTest] public IEnumerator ActualSelectionAndReplayCarriesRemainProtectedAtEveryCaptionSize()
        {
            yield return Open();Time.timeScale=12;app.Begin();
            yield return Until(()=>app.Flow.OpenInvitation?.Id=="receive-flour");
            GameObject.Find("Choices panel").GetComponent<Sankofa.Runtime.UI.PokePanel>().PressFromPointer("Accept:receive-flour");
            yield return Until(()=>app.Flow.CurrentLine?.Id=="gallery.portions.divide");
            yield return CheckFallbackCarry("12-selection-caption");
            Time.timeScale=15;yield return Until(()=>app.Flow.Phase==VisitPhase.Closing&&app.Flow.CurrentLine==null,150);
            Assert.That(app.KeepVisit(),Is.True);app.Flow.BeginAgain();Assert.That(app.Flow.Replay(),Is.True);
            yield return Until(()=>app.Flow.CurrentLine?.Id=="gallery.portions.divide");
            yield return CheckFallbackCarry("13-replay-caption");
            Assert.That(problems,Is.Empty,string.Join("\n",problems));
        }
        IEnumerator CheckFallbackCarry(string capture)
        {
            Time.timeScale=1;var gourd=stage.Find("gourd.flour");
            var board=Object.FindFirstObjectByType<Sankofa.Runtime.UI.CaptionBoard>();
            var placement=board.GetComponent<Sankofa.Runtime.UI.GalleryCaptionPlacement>();
            yield return new WaitForSeconds(0.8f);
            Assert.That(gourd.IsCarried,Is.True);Assert.That(gourd.CarriedBy,Is.Null);
            Camera.main.fieldOfView=60;Camera.main.aspect=4f/3f;
            var visible=Sankofa.Runtime.UI.GalleryCaptionPlacement.Project(Camera.main,gourd.Surfaces.First().bounds);
            Assert.That(visible.xMin,Is.GreaterThan(0));Assert.That(visible.xMax,Is.LessThan(1));
            Assert.That(visible.yMin,Is.GreaterThan(0));Assert.That(visible.yMax,Is.LessThan(1),"selection carry stays visible, not below the forward view");
            foreach(var size in new[]{Sankofa.Runtime.UI.CaptionSize.Small,Sankofa.Runtime.UI.CaptionSize.Medium,Sankofa.Runtime.UI.CaptionSize.Large})
            {
                board.Size=size;yield return null;placement.LayoutNow();
                foreach(var surface in gourd.Surfaces.Where(r=>r.enabled))
                    Assert.That(Sankofa.Runtime.UI.GalleryCaptionPlacement.Overlap(placement.CaptionRect,
                        Sankofa.Runtime.UI.GalleryCaptionPlacement.Project(Camera.main,surface.bounds)),Is.LessThan(0.00005f),size.ToString());
            }
            Capture(capture);
            var carry=GameObject.Find("Carry point").transform;var before=carry.position;
            var world=Object.FindFirstObjectByType<WorldAnchor>();world.HeightOffset+=0.08f;
            yield return new WaitForSeconds(0.8f);
            Assert.That(Vector3.Distance(gourd.transform.position,carry.position),Is.LessThan(0.02f),"fallback follows world adjustment");
            Assert.That(Vector3.Distance(before,carry.position),Is.GreaterThan(0.05f));
            world.HeightOffset-=0.08f;
        }
        [UnityTest] public IEnumerator ModalSettingsProtectTheInvitationAndRespectVisitorAndSystemPauses()
        {
            yield return Open();Time.timeScale=12;app.Begin();
            yield return Until(()=>app.Flow.OpenInvitation?.Id=="receive-milk"&&app.Flow.CurrentLine==null);
            var panel=GameObject.Find("Settings panel").GetComponent<Sankofa.Runtime.UI.PokePanel>();
            panel.PressFromPointer("settings");yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(app.Flow.Phase,Is.EqualTo(VisitPhase.Paused));var clock=app.Flow.Clock;
            yield return new WaitForSeconds(12);
            Assert.That(app.Flow.Clock,Is.EqualTo(clock));Assert.That(app.Flow.Journal.Outcomes.ContainsKey("receive-milk"),Is.False);
            Assert.That(GameObject.Find("Choices panel").GetComponent<Sankofa.Runtime.UI.PokePanel>().Hidden,Is.True);
            panel.PressFromPointer("done");yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(app.Flow.Phase,Is.EqualTo(VisitPhase.Story));
            Assert.That(app.Flow.OpenInvitation?.Id,Is.EqualTo("receive-milk"));
            app.Flow.Pause("visitor");yield return null;
            panel.PressFromPointer("settings");yield return new WaitForSecondsRealtime(0.35f);panel.PressFromPointer("done");yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(app.Flow.Phase,Is.EqualTo(VisitPhase.Paused),"Done does not undo a pre-existing pause");
            app.Flow.Resume();yield return null;
            panel.PressFromPointer("settings");yield return new WaitForSecondsRealtime(0.35f);
            app.SendMessage("OnFocusLost");panel.PressFromPointer("done");yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(app.Flow.Phase,Is.EqualTo(VisitPhase.Paused),"system interruption owns the pause");
            Assert.That(problems,Is.Empty,string.Join("\n",problems));
        }
        [UnityTest] public IEnumerator ClosingSettingsBeforeATransitionLandsDoesNotLeaveADeferredPause()
        {
            yield return Open();Time.timeScale=12;app.Begin();
            yield return Until(()=>app.Flow.Phase==VisitPhase.Transitioning);Time.timeScale=1;
            var panel=GameObject.Find("Settings panel").GetComponent<Sankofa.Runtime.UI.PokePanel>();
            panel.PressFromPointer("settings");yield return new WaitForSecondsRealtime(0.35f);panel.PressFromPointer("done");yield return new WaitForSecondsRealtime(0.35f);
            yield return Until(()=>app.Flow.Phase==VisitPhase.Story,15);
            Assert.That(app.SettingsOpen,Is.False);Assert.That(StageClock.Paused,Is.False);
            Assert.That(problems,Is.Empty,string.Join("\n",problems));
        }
        [UnityTest] public IEnumerator UnkeptAndForgottenVisitsDoNotWriteAnInteractionTrailToLogs()
        {
            var messages=new List<string>();Application.LogCallback watch=(s,stack,type)=>messages.Add(s);
            Application.logMessageReceived+=watch;
            try
            {
                yield return Open();Time.timeScale=15;app.Begin();
                yield return Until(()=>app.Flow.Phase==VisitPhase.Closing&&app.Flow.CurrentLine==null,150);
                Assert.That(app.Flow.KeptVisit,Is.Null);Assert.That(Directory.Exists(folder)&&Directory.GetFiles(folder).Length>0,Is.False);
                Assert.That(app.KeepVisit(),Is.True);app.Flow.BeginAgain();yield return null;
                Assert.That(app.Flow.Forget(),Is.True);Assert.That(app.Flow.ConfirmForget(),Is.True);yield return null;
                Assert.That(app.Flow.KeptVisit,Is.Null);
                Assert.That(messages.Any(m=>m.Contains("Journal of visit")||m.Contains("head direction")||m.Contains("receive-flour")),Is.False,string.Join("\n",messages));
                Assert.That(problems,Is.Empty,string.Join("\n",problems));
            }
            finally {Application.logMessageReceived-=watch;}
        }

        [UnityTest] public IEnumerator SilentNarrationAndReachPreferencesPersistWithoutShorteningALine()
        {
            yield return Open();
            var panel=GameObject.Find("Settings panel").GetComponent<Sankofa.Runtime.UI.PokePanel>();
            panel.PressFromPointer("settings");yield return new WaitForSecondsRealtime(0.35f);
            for(var i=0;i<2;i++){panel.PressFromPointer("narration");yield return new WaitForSecondsRealtime(0.35f);}
            panel.PressFromPointer("reach");yield return new WaitForSecondsRealtime(0.35f);
            var world=Object.FindFirstObjectByType<WorldAnchor>();
            Assert.That(ExperienceSettings.Load().Narration,Is.EqualTo(2));Assert.That(ExperienceSettings.Load().Reach,Is.EqualTo(1));
            Assert.That(world.ReachOffset,Is.EqualTo(-0.06f));
            panel.PressFromPointer("recenter");yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(world.transform.position.z,Is.EqualTo(-0.06f).Within(0.001f));
            panel.PressFromPointer("done");yield return new WaitForSecondsRealtime(0.35f);
            app.Begin();var player=Object.FindFirstObjectByType<Sankofa.Runtime.Narration.NarrationPlayer>();
            var line=app.Flow.CurrentLine;Assert.That(line.Id,Is.EqualTo("gallery.sit"));
            Assert.That(player.CaptionOnly,Is.True);Assert.That(player.Volume,Is.Zero);
            yield return new WaitForSeconds(0.4f);
            Assert.That(app.Flow.CurrentLine.Id,Is.EqualTo(line.Id),"caption mode does not finish a recorded line in 100 ms");
            Assert.That(player.Speaking,Is.True);
            Assert.That(player.GetComponent<AudioSource>().isPlaying,Is.False);
            Capture("14-caption-only");Assert.That(problems,Is.Empty,string.Join("\n",problems));
        }
        [UnityTest] public IEnumerator SettingsAndCreditsRemainInTheForwardViewWithoutStoryControlsCompeting()
        {
            yield return Open();var panel=GameObject.Find("Settings panel").GetComponent<Sankofa.Runtime.UI.PokePanel>();
            var eye=Camera.main;eye.fieldOfView=70;eye.aspect=4f/3f;
            foreach(var p in new[]{panel,GameObject.Find("Controls panel").GetComponent<Sankofa.Runtime.UI.PokePanel>()})
                foreach(var id in p.Ids)
                {
                    var viewport=eye.WorldToViewportPoint(p.transform.Find("Button "+id).position);
                    Assert.That(viewport.x,Is.InRange(0.03f,0.97f));Assert.That(viewport.y,Is.InRange(0.03f,0.97f));
                }
            panel.PressFromPointer("settings");yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(GameObject.Find("Choices panel").GetComponent<Sankofa.Runtime.UI.PokePanel>().Hidden,Is.True);
            Assert.That(GameObject.Find("Settings heading").GetComponent<TMPro.TMP_Text>().enabled,Is.True);
            foreach(var id in panel.Ids)
            {
                var viewport=eye.WorldToViewportPoint(panel.transform.Find("Button "+id).position);
                Assert.That(viewport.x,Is.InRange(0.03f,0.97f));Assert.That(viewport.y,Is.InRange(0.03f,0.97f));
            }
            Capture("15-settings");panel.PressFromPointer("credits");yield return new WaitForSecondsRealtime(0.35f);
            var info=GameObject.Find("Credits page").GetComponent<TMPro.TMP_Text>();info.ForceMeshUpdate();
            Assert.That(info.enabled,Is.True);Assert.That(info.text,Is.Not.Empty);
            Assert.That(info.textBounds.size.y,Is.LessThan(info.rectTransform.rect.height+0.005f),"a credits page fits its reading area");
            Assert.That(panel.Ids,Is.EqualTo(new[]{"done","credits-back","credits-next"}));
            Capture("16-credits");
            var firstPage=info.text;var pages=new HashSet<string>();
            do
            {
                Assert.That(pages.Add(info.text),Is.True);info.ForceMeshUpdate();
                Assert.That(info.textBounds.size.y,Is.LessThan(info.rectTransform.rect.height+0.005f),"every credits page fits");
                panel.PressFromPointer("credits-next");yield return new WaitForSecondsRealtime(0.35f);
            } while(info.text!=firstPage);
            Assert.That(pages.Count,Is.GreaterThan(3));
            panel.PressFromPointer("credits-back");yield return new WaitForSecondsRealtime(0.35f);
            panel.PressFromPointer("context");yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(info.text,Does.Contain("Akan").And.Contain("Ghana").And.Contain("Kenyan"));
            panel.PressFromPointer("done");yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(info.enabled,Is.False);Assert.That(panel.transform.localPosition.y,Is.EqualTo(0.88f).Within(0.001f));
            Assert.That(problems,Is.Empty,string.Join("\n",problems));
        }

        [UnityTest] public IEnumerator RegisteredPosesKeepFeetAndPalmsStableAndAttentionNeverAcceptsAnInvitation()
        {
            yield return Open();app.enabled=false;
            foreach(var panel in Object.FindObjectsByType<Sankofa.Runtime.UI.PokePanel>(FindObjectsSortMode.None))panel.Hidden=true;
            yield return gallery.Transition("story.water.1850s",new Continuity[0],true);
            foreach(var id in new[]{"figure.wambui","figure.naserian","figure.ewe","gourd.flour","gourd.milk"})
                stage.Perform(new Core.Story.StoryAction(Core.Story.ActionKind.Reveal,id));
            foreach(var id in new[]{"figure.wambui","figure.naserian"})
            {
                var figure=stage.Find(id).GetComponent<IllustratedFigure>();Assert.That(figure.RegisteredPoseCount,Is.EqualTo(5));
                var ground=figure.transform.position;
                for(var pose=0;pose<5;pose++)
                {
                    figure.SetPose(pose);yield return new WaitForSeconds(0.8f);
                    Assert.That(Vector3.Distance(figure.PaintedFoot,ground),Is.LessThan(0.005f),id+" pose "+pose);
                    Assert.That(figure.Surface.sharedMaterial.GetFloat("_Cutoff"),Is.EqualTo(0.5f));
                }
                figure.SetPose(0);yield return new WaitForSeconds(0.8f);figure.Glance(Camera.main.transform.position,1.2f);
                yield return new WaitForSeconds(0.8f);
                Assert.That(figure.Pose,Is.Zero,"reaction keeps the authored pose separate");Assert.That(figure.DisplayPose,Is.EqualTo(2));
                yield return new WaitForSeconds(1.3f);Assert.That(figure.DisplayPose,Is.Zero,"reaction returns to the narrative pose");
                figure.SetPose(3);yield return new WaitForSeconds(0.8f);figure.Glance(Camera.main.transform.position,1);
                yield return new WaitForSeconds(0.8f);Assert.That(figure.DisplayPose,Is.EqualTo(3),"concern is not replaced by a smile");
            }
            Assert.That(app.Flow.Journal,Is.Null,"presentation has not begun or chosen a visit");
            var giver=stage.Find("figure.wambui").GetComponent<IllustratedFigure>();var gourd=stage.Find("gourd.flour");
            Assert.That(gourd.transform.parent,Is.SameAs(giver.transform.parent),"resting vessel belongs on the bank, not a downturned hand");
            stage.Perform(new Core.Story.StoryAction(Core.Story.ActionKind.Offer,gourd.Id));
            yield return Until(()=>giver.DisplayPose==1,3);yield return new WaitForSeconds(0.1f);
            Assert.That(Vector3.Distance(gourd.transform.position,giver.GiftOrigin),Is.LessThan(0.01f));
            Capture("17-registered-offer");Assert.That(problems,Is.Empty,string.Join("\n",problems));
        }

        [UnityTest] public IEnumerator CaptionsPreserveTheMemoryWindowNamedAnimalAndEssentialControls()
        {
            yield return Open();Time.timeScale=12;app.Begin();
            yield return Until(()=>app.Flow.OpenInvitation?.Id=="receive-flour");
            GameObject.Find("Choices panel").GetComponent<Sankofa.Runtime.UI.PokePanel>().PressFromPointer("Accept:receive-flour");
            yield return Until(()=>app.Flow.CurrentLine?.Id=="gallery.portions.divide");Time.timeScale=1;
            yield return new WaitForSeconds(0.8f);
            var board=Object.FindFirstObjectByType<Sankofa.Runtime.UI.CaptionBoard>();
            var layout=board.GetComponent<Sankofa.Runtime.UI.GalleryCaptionPlacement>();
            foreach(var size in new[]{Sankofa.Runtime.UI.CaptionSize.Small,Sankofa.Runtime.UI.CaptionSize.Medium,Sankofa.Runtime.UI.CaptionSize.Large})
            {
                board.Size=size;yield return null;layout.LayoutNow();
                foreach(var surface in stage.Find("memory.portions").Surfaces.Concat(stage.Find("gourd.flour").Surfaces).Where(r=>r.enabled))
                    Assert.That(Sankofa.Runtime.UI.GalleryCaptionPlacement.Overlap(layout.CaptionRect,Sankofa.Runtime.UI.GalleryCaptionPlacement.Project(Camera.main,surface.bounds)),Is.LessThan(0.00005f),"memory and carry "+size);
            }
            Capture("19-memory-caption");Time.timeScale=12;
            yield return Until(()=>app.Flow.CurrentLine?.Id=="e1.naserian.arrives");Time.timeScale=1;
            yield return new WaitForSeconds(0.8f);layout.LayoutNow();
            foreach(var surface in stage.Find("figure.ewe").Surfaces.Concat(stage.Find("gourd.flour").Surfaces).Where(r=>r.enabled))
                Assert.That(Sankofa.Runtime.UI.GalleryCaptionPlacement.Overlap(layout.CaptionRect,Sankofa.Runtime.UI.GalleryCaptionPlacement.Project(Camera.main,surface.bounds)),Is.LessThan(0.00005f),"the animal is visible when named");
            foreach(var name in new[]{"Controls panel","Settings panel"})
                foreach(var surface in GameObject.Find(name).GetComponentsInChildren<Renderer>().Where(r=>r.enabled))
                    Assert.That(Sankofa.Runtime.UI.GalleryCaptionPlacement.Overlap(layout.CaptionRect,Sankofa.Runtime.UI.GalleryCaptionPlacement.Project(Camera.main,surface.bounds)),Is.LessThan(0.00005f),"caption does not cover essential controls");
            Capture("18-named-sheep-caption");Assert.That(problems,Is.Empty,string.Join("\n",problems));
        }
        [UnityTest] public IEnumerator SeatedEyeHeightsKeepControlsInTheSameRelativePlaceWithoutMovingTheCamera()
        {
            yield return Open();app.enabled=false;
            var rig=GameObject.Find("OVRCameraRig").transform;var world=Object.FindFirstObjectByType<WorldAnchor>();
            var eye=Camera.main;eye.fieldOfView=70;eye.aspect=4f/3f;
            var panel=GameObject.Find("Settings panel").transform;Vector3? reference=null;
            foreach(var height in new[]{0.85f,1.15f,1.45f})
            {
                rig.position=new Vector3(0,height,0);var before=eye.transform.position;world.Place();yield return null;
                Assert.That(Vector3.Distance(before,eye.transform.position),Is.LessThan(0.001f));
                var relative=eye.transform.InverseTransformPoint(panel.position);
                if(reference.HasValue)Assert.That(Vector3.Distance(reference.Value,relative),Is.LessThan(0.001f));else reference=relative;
                var viewport=eye.WorldToViewportPoint(panel.position);
                Assert.That(viewport.x,Is.InRange(0.03f,0.97f));Assert.That(viewport.y,Is.InRange(0.03f,0.97f));
                world.HeightOffset=0.08f;world.Place();
                Assert.That(eye.transform.InverseTransformPoint(panel.position).y-relative.y,Is.EqualTo(0.08f).Within(0.001f));
                world.HeightOffset=0;
            }
            Assert.That(problems,Is.Empty,string.Join("\n",problems));
        }

        IEnumerator MovieSegment(string name,int frames)
        {
            var speed=Time.timeScale;Time.timeScale=1;
            var directory=Path.GetFullPath(Path.Combine(Application.dataPath,"..","..","artifacts","gallery-demo",name));Directory.CreateDirectory(directory);
            var eye=GameObject.Find("CenterEyeAnchor").GetComponent<Camera>();eye.fieldOfView=70;
            var target=new RenderTexture(960,720,24){antiAliasing=4};var previous=eye.targetTexture;var active=RenderTexture.active;
            var frame=new Texture2D(960,720,TextureFormat.RGB24,false);
            var timestamps=new List<double>();
            try
            {
                for(var i=0;i<frames;i++)
                {
                    yield return new WaitForSecondsRealtime(0.1f);
                    timestamps.Add(Time.realtimeSinceStartupAsDouble);
                    eye.targetTexture=target;eye.Render();RenderTexture.active=target;
                    frame.ReadPixels(new Rect(0,0,960,720),0,0);frame.Apply();
                    File.WriteAllBytes(Path.Combine(directory,i.ToString("D4")+".jpg"),frame.EncodeToJPG(88));
                    eye.targetTexture=previous;RenderTexture.active=active;
                }
                File.WriteAllText(Path.Combine(directory,"timing.json"),JsonUtility.ToJson(new PreviewTiming{timestamps=timestamps.ToArray()},true));
            }
            finally
            {eye.targetTexture=previous;RenderTexture.active=active;Object.Destroy(frame);target.Release();Object.Destroy(target);Time.timeScale=speed;}
        }

        [Serializable] sealed class PreviewTiming { public double[] timestamps; }

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

        void Capture(string name,int width=1440,int height=1080,float fieldOfView=70)
        {
            if(Environment.GetEnvironmentVariable("SANKOFA_CAPTURE")!="1")return;
            var eye=GameObject.Find("CenterEyeAnchor").GetComponent<Camera>();eye.fieldOfView=fieldOfView;
            var directory=Path.GetFullPath(Path.Combine(Application.dataPath,"..","..","artifacts","living-gallery"));Directory.CreateDirectory(directory);
            if(!gallery.Framed&&GameObject.Find("Control hint")!=null)GameObject.Find("Control hint").GetComponent<TMPro.TMP_Text>().enabled=false;
            var target=new RenderTexture(width,height,24){antiAliasing=4};var previous=eye.targetTexture;var active=RenderTexture.active;
            eye.targetTexture=target;eye.Render();RenderTexture.active=target;
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
            File.WriteAllBytes(Path.Combine(directory,name+".png"),image.EncodeToPNG());eye.targetTexture=previous;RenderTexture.active=active;
            Object.Destroy(image);target.Release();Object.Destroy(target);
        }
    }
}
