using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Sankofa.Flow;
using Sankofa.Runtime.Input;
using Sankofa.Runtime.Personal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Sankofa.Tests
{
    public sealed class RiverCastComparisonTests
    {
        readonly List<string> errors=new List<string>();
        string shelf,sceneName;
        [UnitySetUp] public IEnumerator Setup()
        {
            errors.Clear(); LogAssert.ignoreFailingMessages=true; Application.logMessageReceived+=Error;
            shelf=Path.Combine(Path.GetTempPath(),"sankofa-cast-test-"+Guid.NewGuid().ToString("N"));
            PersonalStoryApp.StoreFolderOverride=shelf; yield return null;
        }
        void Error(string message,string trace,LogType type)
        {
            if(type!=LogType.Error && type!=LogType.Assert && type!=LogType.Exception) return;
            if(type==LogType.Error && message=="Unable to process a controller whose SampleRateHz is 0 now.") return;
            errors.Add(message+"\n"+trace);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.captureDeltaTime=0;
            var scene=SceneManager.GetSceneByName(sceneName??"");
            if(scene.IsValid() && scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Cast cleanup "+Guid.NewGuid().ToString("N")));
                yield return SceneManager.UnloadSceneAsync(scene);
            }
            PersonalStoryApp.StoreFolderOverride=null;
            if(Directory.Exists(shelf)) Directory.Delete(shelf,true);
            Application.logMessageReceived-=Error; LogAssert.ignoreFailingMessages=false;
            Assert.That(errors,Is.Empty,string.Join("\n",errors));
        }
        [UnityTest] public IEnumerator PaintedCastObeysStoryPauseAndReturn() { yield return Review(false); }
        [UnityTest] public IEnumerator SolidCastObeysStoryPauseAndReturn() { yield return Review(true); }
        IEnumerator Review(bool solid)
        {
            sceneName=solid ? "RiverThreshold3D" : "RiverThresholdPainted";
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/Sankofa/Scenes/"+sceneName+".unity",new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(sceneName);
#endif
            yield return null; yield return null;
            var prototype=Object.FindFirstObjectByType<RiverThresholdPrototype>(); prototype.AutomaticClock=false;
            var app=prototype.App; app.ServiceEndpoint="invalid-endpoint"; app.SetReducedMotion(false);
            foreach(var pointer in Object.FindObjectsByType<PointerSource>(FindObjectsSortMode.None)) pointer.enabled=false;
            var cast=Object.FindFirstObjectByType<RiverCastPerformance>(); Assert.That(cast.Solid,Is.EqualTo(solid));
            var rig=Camera.main.transform.root; var rigPosition=rig.position; var rigRotation=rig.rotation;
            Assert.That(cast.Actors.All(a=>!a.gameObject.activeInHierarchy),Is.True,"cast stays hidden during intro");
            prototype.Controls.PressFromPointer("primary"); yield return null;
            app.View.Panels.Single(p=>p.name=="Interest choices").PressFromPointer("place-water");
            yield return null;
            for(var i=0;i<62;i++) prototype.Advance(.1f);
            yield return null; yield return new WaitForSecondsRealtime(1.6f);
            Assert.That(prototype.Sequence.Phase,Is.EqualTo(RiverThresholdPhase.Riverside));
            Assert.That(cast.Actors.All(a=>a.gameObject.activeInHierarchy),Is.True);
            var stage=app.View.Stage; stage.Seek(.2f); yield return null;
            var story=app.Session.Story;
            if(Environment.GetEnvironmentVariable("SANKOFA_CAPTURE")=="1") yield return Capture(prototype,cast);
            if(solid) Assert.That(cast.Actors.SelectMany(a=>a.GetComponentsInChildren<SkinnedMeshRenderer>()).All(r=>r.enabled),Is.True,"imported animation must not hide the visible cast");
            var clock=cast.AppliedClock; var positions=cast.Actors.SelectMany(a=>a.GetComponentsInChildren<Transform>()).Select(t=>t.position).ToArray();
            app.Session.Pause(true); for(var i=0;i<20;i++) prototype.Advance(.1f); yield return null;
            Assert.That(cast.AppliedClock,Is.EqualTo(clock));
            if(solid) Assert.That(cast.Actors.SelectMany(a=>a.GetComponentsInChildren<SkinnedMeshRenderer>()).All(r=>r.enabled),Is.True,"pause retains the visible cast");
            var paused=cast.Actors.SelectMany(a=>a.GetComponentsInChildren<Transform>()).Select(t=>t.position).ToArray();
            Assert.That(paused.Select((p,i)=>Vector3.Distance(p,positions[i])).Max(),Is.LessThan(.00001f),"pause holds the composed pose to floating-point precision");
            app.Session.Pause(false); app.SetReducedMotion(true); prototype.Advance(.1f); yield return null;
            Assert.That(cast.MotionAllowed,Is.False); app.SetReducedMotion(false);
            // This exact authored context beat removes both people until 60%; geometry must not leak a future reveal.
            Assert.That(app.Session.Next(),Is.True); stage.Seek(.1f); yield return null;
            Assert.That(app.Session.Chapter.Id,Is.EqualTo("place-water.name"));
            Assert.That(cast.Actors.All(a=>!a.gameObject.activeSelf),Is.True);
            stage.Seek(.8f); yield return null; yield return new WaitForSecondsRealtime(.7f);
            Assert.That(cast.Actors.All(a=>a.gameObject.activeSelf),Is.True);
            if (solid) { prototype.Controls.PressFromPointer("primary"); yield return null; }
            else
            {
                app.View.Panels.Single(p => p.name == "Always available controls").PressFromPointer("options"); yield return null;
                app.View.Panels.Single(p => p.name == "Return and recovery actions").PressFromPointer("table"); yield return null; yield return null;
            }
            Assert.That(prototype.Sequence.Phase,Is.EqualTo(RiverThresholdPhase.Table));
            Assert.That(cast.Actors.All(a=>!a.gameObject.activeInHierarchy),Is.True);
            yield return new WaitForSecondsRealtime(.34f); prototype.Controls.PressFromPointer("primary");
            for(var i=0;i<62;i++) prototype.Advance(.1f); yield return null;
            Assert.That(app.Session.Story,Is.SameAs(story)); Assert.That(app.Session.Chapter.Id,Is.EqualTo("place-water.name"));
            Assert.That(rig.position,Is.EqualTo(rigPosition)); Assert.That(rig.rotation,Is.EqualTo(rigRotation));
        }
        IEnumerator Capture(RiverThresholdPrototype prototype,RiverCastPerformance cast)
        {
            var directory=Path.GetFullPath(Path.Combine(Application.dataPath,"../../artifacts/river-cohesion")); Directory.CreateDirectory(directory);
            var label=cast.Solid ? "3d" : "painted";
            var eye=new GameObject("Comparison review camera").AddComponent<Camera>(); eye.stereoTargetEye=StereoTargetEyeMask.None;
            eye.fieldOfView=60; eye.nearClipPlane=.03f; eye.clearFlags=CameraClearFlags.SolidColor; eye.backgroundColor=new Color(.008f,.012f,.022f);
            var target=new RenderTexture(1600,900,24){antiAliasing=4}; eye.targetTexture=target;
            var pixels=new Texture2D(1600,900,TextureFormat.RGB24,false);
            System.Diagnostics.Process encoder=null;
            if(Environment.GetEnvironmentVariable("SANKOFA_CAPTURE_MOVIE")=="1")
            {
                var ffmpeg=Environment.GetEnvironmentVariable("SANKOFA_FFMPEG"); Assert.That(File.Exists(ffmpeg),Is.True);
                encoder=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName=ffmpeg,UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,
                    Arguments="-v error -y -f rawvideo -pix_fmt rgb24 -s 1600x900 -r 24 -i - -vf vflip -an -c:v libx264 -preset fast -crf 20 -pix_fmt yuv420p \""+Path.Combine(directory,label+"-comparison.mp4")+"\"" });
            }
            Time.captureDeltaTime=1f/24;
            try
            {
                for(var f=0;f<8*24;f++)
                {
                    prototype.App.View.Stage.Seek(.2f); prototype.Advance(1f/24); yield return null;
                    // Diagnostic camera offsets only; the actual tracked rig is never moved.
                    var x=f<48 ? 0 : Mathf.Sin((f-48)/144f*Mathf.PI*2)*.65f;
                    eye.transform.position=new Vector3(x,1.15f,0); eye.transform.LookAt(new Vector3(-.25f,.87f,2.6f));
                    var still=f==24 ? "front" : f==84 ? "right" : f==156 ? "left" : null;
                    if(still==null && encoder==null) continue;
                    var prior=RenderTexture.active; eye.Render(); RenderTexture.active=target;
                    pixels.ReadPixels(new Rect(0,0,1600,900),0,0); pixels.Apply(); RenderTexture.active=prior;
                    if(still!=null) File.WriteAllBytes(Path.Combine(directory,label+"-"+still+".png"),pixels.EncodeToPNG());
                    if(encoder!=null) { var raw=pixels.GetRawTextureData<byte>().ToArray(); encoder.StandardInput.BaseStream.Write(raw,0,raw.Length); }
                }
            }
            finally
            {
                if(encoder!=null) { encoder.StandardInput.Close(); if(!encoder.WaitForExit(15000)) encoder.Kill(); Assert.That(encoder.ExitCode,Is.Zero); encoder.Dispose(); }
                target.Release(); Object.Destroy(eye.gameObject); Object.Destroy(target); Object.Destroy(pixels); Time.captureDeltaTime=0;
            }
        }
    }
}
