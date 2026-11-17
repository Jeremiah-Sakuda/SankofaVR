#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

namespace Sankofa.EditorTools
{
    /// <summary>
    /// Opt-in capture of the actual rendered Game View, without a replacement camera or fixed simulation clock.
    /// Keep Game View visible and its dimensions unchanged. Set SANKOFA_SIMULATOR_CAPTURE=1 to start when XR runs.
    /// Audio is deliberately omitted: this helper never records desktop audio, microphone input, or reroutes the mix.
    /// </summary>
    [InitializeOnLoad]
    public static class SankofaSimulatorCapture
    {
        const string Menu = "Sankofa/Simulator Capture/";
        const double Interval = 1d / 24, MaximumSeconds = 600;
        static Session current;
        static CancellationTokenSource pendingStart;

        sealed class Session
        {
            public string folder;
            public double started, firstFrame = -1, nextFrame;
            public int frames, width, height;
            public MediaEncoder encoder;
            public StreamWriter timestamps;
            public readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        }

        static SankofaSimulatorCapture()
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
            AssemblyReloadEvents.beforeAssemblyReload += () => Stop("assembly reload");
            EditorApplication.quitting += () => Stop("editor quit");
        }

        [MenuItem(Menu + "Start actual Game View recording")]
        public static void Start()
        {
            if (current != null) return;
            if (!EditorApplication.isPlaying || Application.isBatchMode || !Ready())
            {
                Debug.LogError("[Sankofa capture] Start requires interactive Play mode, a running XR display, and simulator runtime evidence.");
                return;
            }
            if (Time.captureDeltaTime != 0)
            {
                Debug.LogError("[Sankofa capture] Refusing a fixed simulation clock. Restart normal simulator Play mode first.");
                return;
            }
            try
            {
                var folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../artifacts/simulator-visit",
                    DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture)));
                Directory.CreateDirectory(folder);
                current = new Session { folder = folder, started = Time.realtimeSinceStartupAsDouble };
                current.timestamps = new StreamWriter(Path.Combine(folder, "frames.csv"));
                current.timestamps.WriteLine("frame,utc,elapsed_seconds,movie_seconds,unity_frame,unity_time,time_scale");
                File.WriteAllText(Path.Combine(folder, "capture.txt"),
                    "Actual Unity Game View / XR rendered output, captured after Awaitable.EndOfFrameAsync.\n" +
                    "Unity MediaEncoder receives measured real-time frame timestamps; maximum sampling rate 24 Hz.\n" +
                    "No replacement camera, altered simulation clock, synthetic input, or desktop-window recording is introduced by this helper.\n" +
                    "Audio omitted. No microphone or desktop audio capture; normal app output is unchanged.\n" +
                    "Capture overhead and sampled video are NOT device performance, tracking, comfort, or participant evidence.\n" +
                    "Started UTC: " + DateTime.UtcNow.ToString("O") + "\n" + Evidence());
                Debug.Log("[Sankofa capture] Recording actual Game View to " + folder + ". Keep Game View visible and its size fixed.");
                Capture(current);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                Stop("start failed: " + error.Message);
            }
        }

        [MenuItem(Menu + "Finish recording")]
        public static void Finish() => Stop("manual finish");

        static async void Capture(Session session)
        {
            try
            {
                while (current == session && EditorApplication.isPlaying)
                {
                    await Awaitable.NextFrameAsync(session.cancellation.Token);
                    // Official end-of-frame awaitable works in interactive Play mode, without a runtime component.
                    await Awaitable.EndOfFrameAsync(session.cancellation.Token);
                    if (current != session) return;
                    var now = Time.realtimeSinceStartupAsDouble;
                    if (now - session.started >= MaximumSeconds) { Stop("ten-minute recording limit"); return; }
                    if (Time.captureDeltaTime != 0) throw new InvalidOperationException("Simulation clock changed to fixed capture timing.");
                    if (!Ready()) throw new InvalidOperationException("Running XR display or simulator runtime evidence was lost.");
                    if (now < session.nextFrame) continue;
                    session.nextFrame = now + Interval;
                    var texture = ScreenCapture.CaptureScreenshotAsTexture();
                    if (texture == null) throw new InvalidOperationException("Game View capture returned no image.");
                    try
                    {
                        if (session.encoder == null)
                        {
                            session.width = texture.width; session.height = texture.height;
                            if ((texture.width & 1) != 0 || (texture.height & 1) != 0)
                                throw new InvalidOperationException("Choose an even-sized Game View resolution before recording.");
                            session.encoder = new MediaEncoder(Path.Combine(session.folder, "actual-simulator-visit.mp4"),
                                new VideoTrackAttributes { width = (uint)texture.width, height = (uint)texture.height,
                                    frameRate = new MediaRational(24), includeAlpha = false });
                            session.firstFrame = now;
                            File.WriteAllBytes(Path.Combine(session.folder, "first-frame.png"), texture.EncodeToPNG());
                        }
                        if (texture.width != session.width || texture.height != session.height)
                            throw new InvalidOperationException("Game View dimensions changed during recording.");
                        var movieSeconds = now - session.firstFrame;
                        if (!session.encoder.AddFrame(texture, new MediaTime((long)Math.Round(movieSeconds * 1000000), 1000000)))
                            throw new InvalidOperationException("Unity MediaEncoder rejected the captured frame.");
                        session.timestamps.WriteLine(string.Join(",", session.frames, DateTime.UtcNow.ToString("O"),
                            Number(now - session.started), Number(movieSeconds), Time.frameCount, Number(Time.timeAsDouble), Number(Time.timeScale)));
                        if (++session.frames % 24 == 0) session.timestamps.Flush();
                    }
                    finally { UnityEngine.Object.Destroy(texture); }
                }
                if (current == session) Stop("Play mode ended");
            }
            catch (OperationCanceledException) { }
            catch (Exception error)
            {
                Debug.LogException(error);
                if (current == session) Stop("capture failed: " + error.Message);
            }
        }

        static void Stop(string reason)
        {
            pendingStart?.Cancel(); pendingStart = null;
            var session = current; current = null;
            if (session == null) return;
            session.cancellation.Cancel();
            var finalize = "closed";
            try { session.encoder?.Dispose(); }
            catch (Exception error) { finalize = "encoder finalization failed: " + error.Message; Debug.LogException(error); }
            finally { session.timestamps?.Dispose(); session.cancellation.Dispose(); }
            File.AppendAllText(Path.Combine(session.folder, "capture.txt"),
                "\nFinished UTC: " + DateTime.UtcNow.ToString("O") + "\nReason: " + reason + "\nEncoder: " + finalize +
                "\nCaptured frames: " + session.frames + "\nImage: " + session.width + "x" + session.height +
                "\nElapsed real seconds: " + Number(Time.realtimeSinceStartupAsDouble - session.started) +
                "\nMovie timestamps begin at the first captured frame; CSV preserves startup delay and real sample times.\n" + Evidence());
            Debug.Log("[Sankofa capture] " + reason + ": " + session.folder);
        }

        static void OnPlayMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode) Stop("Play mode exit");
            if (state == PlayModeStateChange.EnteredPlayMode && Environment.GetEnvironmentVariable("SANKOFA_SIMULATOR_CAPTURE") == "1")
                AutoStart();
        }

        static async void AutoStart()
        {
            pendingStart?.Cancel();
            var pending = pendingStart = new CancellationTokenSource();
            var deadline = Time.realtimeSinceStartupAsDouble + 30;
            try
            {
                while (EditorApplication.isPlaying && Time.realtimeSinceStartupAsDouble < deadline)
                {
                    if (Ready()) { Start(); return; }
                    await Awaitable.NextFrameAsync(pending.Token);
                }
                Debug.LogWarning("[Sankofa capture] Auto-start timed out before XR simulator became ready; no footage recorded.");
            }
            catch (OperationCanceledException) { }
            finally { if (pendingStart == pending) pendingStart = null; pending.Dispose(); }
        }

        static bool Ready()
        {
            var manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
            if (manager == null || manager.activeLoader == null) return false;
            var displays = new List<XRDisplaySubsystem>(); SubsystemManager.GetSubsystems(displays);
            if (!displays.Exists(display => display.running)) return false;
            return (OpenXRRuntime.name + " " + Environment.GetEnvironmentVariable("XR_SELECTED_RUNTIME_JSON") + " " +
                Environment.GetEnvironmentVariable("XR_RUNTIME_JSON")).IndexOf("simulator", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static string Evidence()
        {
            var manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
            var displays = new List<XRDisplaySubsystem>(); SubsystemManager.GetSubsystems(displays);
            var result = "Unity: " + Application.unityVersion + "\nScene: " + SceneManager.GetActiveScene().path +
                "\nXR loader: " + (manager != null && manager.activeLoader != null ? manager.activeLoader.GetType().FullName : "none") +
                "\nOpenXR runtime: " + OpenXRRuntime.name + "\nRuntime version: " + OpenXRRuntime.version +
                "\nOpenXR API: " + OpenXRRuntime.apiVersion +
                "\nConfigured XR_SELECTED_RUNTIME_JSON: " + Environment.GetEnvironmentVariable("XR_SELECTED_RUNTIME_JSON") +
                "\nConfigured XR_RUNTIME_JSON: " + Environment.GetEnvironmentVariable("XR_RUNTIME_JSON") +
                "\nTime.captureDeltaTime: " + Number(Time.captureDeltaTime) + "\nTime.timeScale: " + Number(Time.timeScale) + "\n";
            foreach (var display in displays) result += "XR display: " + display.subsystemDescriptor.id + "; running=" + display.running + "\n";
            return result;
        }

        static string Number(double value) => value.ToString("F6", CultureInfo.InvariantCulture);
    }
}
#endif
