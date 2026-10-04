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
        /// Looking ahead, then down toward the lap panel. Without a headset nothing drives the eye's pose,
        /// so it is restored explicitly.
        /// </summary>
        static void CaptureBoth(Camera eye, string name)
        {
            Capture(eye, name);
            var ahead = eye.transform.rotation;
            eye.transform.rotation = Quaternion.LookRotation(new Vector3(-0.1f, 0.6f, 0.45f) - eye.transform.position, Vector3.up);
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
        public IEnumerator CaptureAVisit()
        {
            if (Environment.GetEnvironmentVariable("SANKOFA_CAPTURE") != "1") Assert.Ignore("Set SANKOFA_CAPTURE=1 to write review images.");
            LogAssert.ignoreFailingMessages = true;
            SceneManager.LoadScene("Hearth");
            yield return null;
            // No headset: lift the rig to a seated eye height and give the camera a headset-like field of view.
            var rig = GameObject.Find("OVRCameraRig").transform;
            rig.position = new Vector3(0, 1.15f, 0);
            var eye = GameObject.Find("CenterEyeAnchor").GetComponent<Camera>();
            eye.fieldOfView = 90;
            var app = Object.FindFirstObjectByType<SankofaApp>();
            var stage = Object.FindFirstObjectByType<StoryStage>();

            yield return WaitFor(() => app.Flow.Phase == VisitPhase.Welcome, 10);
            yield return new WaitForSeconds(1.5f);
            CaptureBoth(eye, "1-welcome");
            WriteMix(Path.Combine(Folder, "hearth-sound.wav"), eye.transform.position, 30f);

            app.Flow.Begin();
            yield return WaitFor(() => app.Flow.OpenInvitation != null, 20);
            yield return new WaitForSeconds(2f);
            CaptureBoth(eye, "2-offer");
            app.Flow.Accept("receive-ember");

            yield return WaitFor(() => app.Flow.Phase == VisitPhase.Transitioning, 20);
            yield return new WaitForSeconds(0.8f);
            Capture(eye, "3-entering");

            yield return WaitFor(() => app.Flow.Journal.Entries.Count > 0 && app.Flow.CurrentLine?.Id == "reveal.carried", 40);
            yield return new WaitForSeconds(2f);
            CaptureBoth(eye, "4-reveal");

            yield return WaitFor(() => app.Flow.Phase == VisitPhase.Closing && app.Flow.CurrentLine == null, 60);
            yield return new WaitForSeconds(2f);
            CaptureBoth(eye, "5-keepsake");
            Assert.That(stage.ActiveScene, Is.EqualTo("hearth"));
        }
    }
}
