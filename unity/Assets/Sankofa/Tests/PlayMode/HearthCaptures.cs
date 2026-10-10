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

        static void Capture(Camera eye, string name, int width = 1600, int height = 1200)
        {
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
        /// A run of frames in real time, looking at one place, so motion (a walk, a kneel, a hand-over) can be
        /// reviewed as a strip rather than judged from a single still. The story runs at normal speed meanwhile.
        /// </summary>
        static IEnumerator Strip(Camera eye, string name, Vector3 look, int frames, float spacing)
        {
            var ahead = eye.transform.rotation;
            var pace = Time.timeScale;
            Time.timeScale = 1;
            eye.transform.rotation = Quaternion.LookRotation(look - eye.transform.position, Vector3.up);
            for (var i = 0; i < frames; i++)
            {
                Capture(eye, $"{name}-{i + 1:00}", 800, 600);
                yield return new WaitForSeconds(spacing);
            }
            eye.transform.rotation = ahead;
            Time.timeScale = pace;
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
        [Timeout(900000)] // Three visits rendered at review resolution take longer than the default three minutes.
        public IEnumerator CaptureVisits()
        {
            if (Environment.GetEnvironmentVariable("SANKOFA_CAPTURE") != "1") Assert.Ignore("Set SANKOFA_CAPTURE=1 to write review images.");
            LogAssert.ignoreFailingMessages = true;
            var store = Path.Combine(Path.GetTempPath(), "sankofa-capture-" + Guid.NewGuid().ToString("N"));
            SankofaApp.StoreFolderOverride = store;
            // The mixes are rendered from the clips themselves; nothing needs to reach the speakers.
            AudioListener.volume = 0;
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
                var right = new Vector3(0.25f, 0.5f, 0.55f);
                var people = new Vector3(0.05f, 1.0f, 1.4f);
                IEnumerator Line(string id, float seconds = 90) => WaitFor(() => flow.CurrentLine?.Id == id, seconds);
                IEnumerator Invited(string id, float seconds = 90) => WaitFor(() => flow.OpenInvitation?.Id == id, seconds);

                yield return WaitFor(() => app.AtStart, 10);
                yield return new WaitForSeconds(1.5f);
                CaptureBoth(eye, "00-start");
                app.Begin();
                yield return WaitFor(() => flow.Phase == VisitPhase.Welcome, 10);
                yield return new WaitForSeconds(1.5f);
                CaptureBoth(eye, "01-welcome");
                WriteMix(Path.Combine(Folder, "hearth-sound.wav"), eye.transform.position, 30f);
                Time.timeScale = 4;
                yield return WaitFor(() => flow.Phase == VisitPhase.Choosing, 60);
                yield return new WaitForSeconds(1f);
                CaptureBoth(eye, "02-choice");

                flow.Choose("era.1850s");
                yield return Invited("receive-ember");
                yield return new WaitForSeconds(2f);
                CaptureBoth(eye, "03-offer");
                flow.Accept("receive-ember");
                yield return WaitFor(() => flow.Phase == VisitPhase.Transitioning, 30);
                // The entry through the flame, frame by frame: the burn from the ember, then the stream opening.
                yield return Strip(eye, "m0-entering", eye.transform.position + eye.transform.forward * 2f - Vector3.up * 0.4f, 9, 0.42f);
                yield return Line("e1.sides");
                yield return new WaitForSeconds(1f);
                CaptureBoth(eye, "05-water", right);
                WriteMix(Path.Combine(Folder, "water-1850s-sound.wav"), eye.transform.position, 20f);
                yield return Invited("touch-water");
                flow.Accept("touch-water");
                yield return Line("e1.wambui.arrives", 60);
                yield return Strip(eye, "m1-wambui-arrives", new Vector3(-2.2f, 1.0f, 1.7f), 8, 0.8f);
                yield return Invited("receive-flour");
                // The moment of giving: the gift has left her hand and drifted to the visitor.
                yield return WaitFor(() => Vector3.Distance(stage.Find("gourd.flour").transform.position, stage.GiftPoint(stage.Find("gourd.flour").HandOverPlace.Value)) < 0.08f, 20);
                CaptureBoth(eye, "06-wambui", people);
                flow.Accept("receive-flour");
                yield return Line("e1.problem");
                yield return new WaitForSeconds(1f);
                CaptureBoth(eye, "07-naserian", people);
                // The visitor lingers on the ewe and on Naserian's gourd of milk: the story looks with them, and at
                // home the fire shows them back.
                flow.Notice("figure.ewe");
                flow.Notice("figure.ewe");
                flow.Notice("gourd.milk");
                yield return Line("e1.hide", 60);
                yield return Strip(eye, "m2-kneel-at-hide", new Vector3(0.35f, 0.75f, 0.95f), 6, 0.5f);
                yield return Invited("offer-flour");
                flow.Accept("offer-flour");
                yield return Invited("receive-bead");
                yield return Strip(eye, "m3-bead-offered", new Vector3(0.35f, 0.95f, 0.9f), 8, 0.5f);
                yield return WaitFor(() => Vector3.Distance(stage.Find("bead").transform.position, stage.GiftPoint(stage.Find("bead").HandOverPlace.Value)) < 0.08f, 20);
                CaptureBoth(eye, "08-bead", right);
                flow.Accept("receive-bead");
                yield return Line("e1.part", 60);
                yield return Strip(eye, "m4-parting", new Vector3(0.1f, 1.0f, 1.6f), 8, 0.8f);
                // Home: what the visitor lingered on rises out of the fire in its light.
                yield return WaitFor(() => flow.Phase == VisitPhase.Closing, 120);
                var pace = Time.timeScale;
                Time.timeScale = 1;
                yield return Strip(eye, "m6-echoes", stage.FireTop + Vector3.up * 0.15f, 6, 0.7f);
                Time.timeScale = pace;
                yield return WaitFor(() => flow.Phase == VisitPhase.Closing && flow.CurrentLine == null, 120);
                yield return new WaitForSeconds(2f);
                CaptureBoth(eye, "09-keepsake");

                flow.Keep();
                flow.BeginAgain();
                yield return new WaitForSeconds(1f);
                flow.Follow();
                yield return Line("today.rubbish", 120);
                yield return new WaitForSeconds(1f);
                CaptureBoth(eye, "10-today", right);
                WriteMix(Path.Combine(Folder, "water-today-sound.wav"), eye.transform.position, 20f);
                yield return Invited("hang-bead");
                flow.Accept("hang-bead");
                yield return new WaitForSeconds(2f);
                CaptureBoth(eye, "11-gate", right);
                yield return WaitFor(() => flow.Phase == VisitPhase.Closing && flow.CurrentLine == null, 120);

                flow.BeginAgain();
                yield return null;
                flow.NewStory();
                flow.Choose("era.1899");
                yield return Line("e2.depot", 120);
                yield return new WaitForSeconds(1f);
                CaptureBoth(eye, "12-depot", right);
                WriteMix(Path.Combine(Folder, "water-1899-sound.wav"), eye.transform.position, 20f);
                yield return Invited("touch-rail");
                flow.Accept("touch-rail");
                yield return Line("e2.nothing", 120);
                yield return new WaitForSeconds(1f);
                CaptureBoth(eye, "13-granddaughters", people);
                yield return Line("e2.share", 60);
                yield return Strip(eye, "m5-pouring", new Vector3(0.0f, 1.0f, 1.4f), 6, 0.6f);
                yield return Invited("receive-seed");
                yield return WaitFor(() => Vector3.Distance(stage.Find("seed").transform.position, stage.GiftPoint(stage.Find("seed").HandOverPlace.Value)) < 0.08f, 20);
                Capture(eye, "14-seed");
                Assert.That(stage.ActiveScene, Is.EqualTo("story.water.1899"));
            }
            finally
            {
                Time.timeScale = 1;
                AudioListener.volume = 1;
                SankofaApp.StoreFolderOverride = null;
                if (Directory.Exists(store)) Directory.Delete(store, true);
            }
        }
    }
}
