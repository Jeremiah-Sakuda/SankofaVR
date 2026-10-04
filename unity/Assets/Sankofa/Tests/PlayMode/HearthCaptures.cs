using System;
using System.Collections;
using System.IO;
using Sankofa.Flow;
using Sankofa.Runtime.App;
using Sankofa.Runtime.Stage;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Sankofa.Tests
{
    /// <summary>
    /// Renders still frames of a visit from the seated eye for development review, into the ignored
    /// artifacts/captures folder. A desktop render: not evidence of how the scene looks or performs on Quest.
    /// Skipped unless SANKOFA_CAPTURE=1, so ordinary test runs write nothing.
    /// </summary>
    public class HearthCaptures
    {
        static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "artifacts", "captures"));

        static IEnumerator WaitFor(Func<bool> condition, float seconds)
        {
            var end = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < end) yield return null;
        }

        static void Capture(Camera eye, string name)
        {
            const int width = 1600, height = 1200;
            var target = new RenderTexture(width, height, 24) { antiAliasing = 4 };
            var previous = eye.targetTexture;
            eye.targetTexture = target;
            eye.Render();
            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            eye.targetTexture = previous;
            RenderTexture.active = null;
            Directory.CreateDirectory(Folder);
            File.WriteAllBytes(Path.Combine(Folder, name + ".png"), image.EncodeToPNG());
            Object.Destroy(image);
            target.Release();
        }

        /// <summary>
        /// Looking ahead, then down toward what the visitor would look at there. Without a headset nothing drives
        /// the eye's pose, so it is restored explicitly.
        /// </summary>
        static void CaptureBoth(Camera eye, string name, Vector3? down = null)
        {
            Capture(eye, name);
            var ahead = eye.transform.rotation;
            eye.transform.rotation = Quaternion.LookRotation((down ?? new Vector3(-0.1f, 0.6f, 0.45f)) - eye.transform.position, Vector3.up);
            Capture(eye, name + "-down");
            eye.transform.rotation = ahead;
        }

        /// <summary>
        /// A mono mixdown of every looping hearth sound as heard from the seat, with logarithmic distance falloff
        /// but no spatialisation, for listening on a desktop. Narration is not included.
        /// </summary>
        static void WriteMix(string path, Vector3 ear, float seconds)
        {
            const int rate = 48000;
            var mix = new float[(int)(seconds * rate)];
            foreach (var source in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            {
                if (source.clip == null || !source.loop || !source.isActiveAndEnabled) continue;
                var clip = source.clip;
                var data = new float[clip.samples * clip.channels];
                clip.GetData(data, 0);
                var distance = Vector3.Distance(source.transform.position, ear);
                var gain = source.volume * Mathf.Lerp(1f, Mathf.Min(1f, source.minDistance / Mathf.Max(distance, 0.01f)), source.spatialBlend);
                var ratio = (double)clip.frequency / rate;
                for (var i = 0; i < mix.Length; i++)
                    mix[i] += data[(int)((source.timeSamples + (long)(i * ratio)) % clip.samples) * clip.channels] * gain;
            }
            var peak = 0f;
            foreach (var s in mix) peak = Mathf.Max(peak, Mathf.Abs(s));
            var scale = peak > 0.9f ? 0.9f / peak : 1f;
            using var file = new BinaryWriter(File.Create(path));
            file.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            file.Write(36 + mix.Length * 2);
            file.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            file.Write(16);
            file.Write((short)1);
            file.Write((short)1);
            file.Write(rate);
            file.Write(rate * 2);
            file.Write((short)2);
            file.Write((short)16);
            file.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            file.Write(mix.Length * 2);
            foreach (var s in mix) file.Write((short)Mathf.Clamp(Mathf.RoundToInt(s * scale * 32767f), -32768, 32767));
        }

        [UnityTest]
        public IEnumerator CaptureVisits()
        {
            if (Environment.GetEnvironmentVariable("SANKOFA_CAPTURE") != "1") Assert.Ignore("Set SANKOFA_CAPTURE=1 to write review images.");
            LogAssert.ignoreFailingMessages = true;
            var store = Path.Combine(Path.GetTempPath(), "sankofa-capture-" + Guid.NewGuid().ToString("N"));
            SankofaApp.StoreFolderOverride = store;
            try
            {
                SceneManager.LoadScene("Hearth");
                yield return null;
                // No headset: lift the rig to a seated eye height and give the camera a headset-like field of view.
                var rig = GameObject.Find("OVRCameraRig").transform;
                rig.position = new Vector3(0, 1.15f, 0);
                var eye = GameObject.Find("CenterEyeAnchor").GetComponent<Camera>();
                eye.fieldOfView = 90;
                var app = Object.FindFirstObjectByType<SankofaApp>();
                var stage = Object.FindFirstObjectByType<StoryStage>();
                var flow = app.Flow;
                var bench = new Vector3(0.12f, 0.55f, 0.62f);
                IEnumerator Line(string id, float seconds = 60) => WaitFor(() => flow.CurrentLine?.Id == id, seconds);

                yield return WaitFor(() => flow.Phase == VisitPhase.Welcome, 10);
                yield return new WaitForSeconds(1.5f);
                CaptureBoth(eye, "01-welcome");
                WriteMix(Path.Combine(Folder, "hearth-sound.wav"), eye.transform.position, 30f);
                Time.timeScale = 3;
                yield return WaitFor(() => flow.Phase == VisitPhase.Choosing, 30);
                yield return new WaitForSeconds(1f);
                CaptureBoth(eye, "02-choice");

                flow.Choose("makers");
                yield return WaitFor(() => flow.OpenInvitation != null, 30);
                yield return new WaitForSeconds(2f);
                CaptureBoth(eye, "03-offer");
                flow.Accept("receive-ember");
                yield return WaitFor(() => flow.Phase == VisitPhase.Transitioning, 30);
                yield return new WaitForSeconds(0.8f);
                Capture(eye, "04-entering");
                yield return Line("yard.dusk");
                yield return new WaitForSeconds(1f);
                CaptureBoth(eye, "05-yard", bench);
                yield return WaitFor(() => flow.OpenInvitation?.Id == "touch-bowl", 60);
                yield return new WaitForSeconds(1f);
                CaptureBoth(eye, "06-bowl", bench);
                flow.Accept("touch-bowl");
                yield return WaitFor(() => flow.OpenInvitation?.Id == "receive-shard", 60);
                yield return new WaitForSeconds(2f);
                CaptureBoth(eye, "07-piece", bench);
                flow.Accept("receive-shard");
                yield return WaitFor(() => flow.Phase == VisitPhase.Closing && flow.CurrentLine == null, 90);
                yield return new WaitForSeconds(2f);
                CaptureBoth(eye, "08-keepsake");

                flow.Keep();
                flow.BeginAgain();
                yield return new WaitForSeconds(1f);
                CaptureBoth(eye, "09-returning");
                flow.Follow();
                yield return WaitFor(() => flow.OpenInvitation?.Id == "touch-mend", 90);
                flow.Accept("touch-mend");
                yield return new WaitForSeconds(2.5f);
                CaptureBoth(eye, "10-mended", bench);
                yield return WaitFor(() => flow.Phase == VisitPhase.Closing && flow.CurrentLine == null, 90);

                flow.BeginAgain();
                yield return null;
                flow.NewStory();
                flow.Choose("travellers");
                yield return WaitFor(() => stage.ActiveScene == "story.road" && flow.Phase == VisitPhase.Story, 90);
                yield return Line("road.rest");
                CaptureBoth(eye, "11-road", new Vector3(0.25f, 0.45f, 0.5f));
                yield return WaitFor(() => flow.OpenInvitation?.Id == "touch-marker", 90);
                yield return new WaitForSeconds(1f);
                CaptureBoth(eye, "12-waymarker", new Vector3(0.25f, 0.45f, 0.5f));
                flow.Accept("touch-marker");
                yield return WaitFor(() => flow.OpenInvitation?.Id == "receive-waystone", 60);
                yield return new WaitForSeconds(2f);
                Capture(eye, "13-waystone");
                Assert.That(stage.ActiveScene, Is.EqualTo("story.road"));
            }
            finally
            {
                Time.timeScale = 1;
                SankofaApp.StoreFolderOverride = null;
                if (Directory.Exists(store)) Directory.Delete(store, true);
            }
        }
    }
}
