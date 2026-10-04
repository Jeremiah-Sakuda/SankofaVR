using System;
using Sankofa.Core.Story;
using UnityEngine;

namespace Sankofa.Runtime.Narration
{
    /// <summary>
    /// Speaks a line from the fire's position and reports completion with the flow's token. With a recorded clip
    /// the clip's end decides; a caption-only line lasts its authored duration. Stopping drops the token, so a late
    /// completion can never arrive for an interrupted line.
    /// </summary>
    public sealed class NarrationPlayer : MonoBehaviour
    {
        public const string ResourceFolder = "Narration/";

        [SerializeField] AudioSource voice;

        readonly float[] samples = new float[256];
        int token;
        bool usingClip;
        double elapsed;
        double duration;

        public event Action<int> Finished;
        public event Action<int, double> DurationMeasured;

        /// <summary>0..1 loudness of the current line, for the fire's breathing.</summary>
        public float Level { get; private set; }
        public bool Speaking => token != 0;
        /// <summary>True when the current line has no recording and is timed from its caption duration.</summary>
        public bool CaptionOnly => token != 0 && !usingClip;

        public void Play(int lineToken, Line line)
        {
            Stop();
            token = lineToken;
            elapsed = 0;
            var clip = line.Audio != null ? Resources.Load<AudioClip>(ResourceFolder + line.Audio) : null;
            if (line.Audio != null && clip == null)
                Debug.LogWarning($"[Sankofa] Narration clip '{line.Audio}' for {line.Id} is missing; using caption timing.");
            usingClip = clip != null && voice != null;
            if (usingClip)
            {
                voice.clip = clip;
                voice.Play();
                duration = clip.length;
                DurationMeasured?.Invoke(lineToken, clip.length);
            }
            else duration = line.Seconds;
        }

        public void Stop()
        {
            if (voice != null && voice.isPlaying) voice.Stop();
            token = 0;
            Level = 0;
        }

        void Update()
        {
            if (token == 0) return;
            elapsed += Time.deltaTime;
            bool done;
            if (usingClip)
            {
                voice.GetOutputData(samples, 0);
                var sum = 0f;
                for (var i = 0; i < samples.Length; i++) sum += samples[i] * samples[i];
                Level = Mathf.Clamp01(Mathf.Sqrt(sum / samples.Length) * 6f);
                done = !voice.isPlaying && elapsed > 0.1;
            }
            else
            {
                Level = CaptionCadence(elapsed, duration);
                done = elapsed >= duration;
            }
            if (!done) return;
            var finished = token;
            token = 0;
            Level = 0;
            Finished?.Invoke(finished);
        }

        /// <summary>
        /// A speech-like rhythm for caption-only lines so a directed fire can still be judged in development.
        /// Not a substitute for real narration in any presence comparison.
        /// </summary>
        static float CaptionCadence(double t, double total)
        {
            var syllables = 0.5 + 0.5 * Math.Sin(t * 2 * Math.PI * 4.1);
            var phrase = 0.55 + 0.45 * Math.Sin(t * 2 * Math.PI * 0.45 + 0.8);
            var edges = Math.Min(1, Math.Min(t / 0.25, (total - t) / 0.4));
            return (float)Math.Max(0, syllables * phrase * edges * 0.8);
        }
    }
}
