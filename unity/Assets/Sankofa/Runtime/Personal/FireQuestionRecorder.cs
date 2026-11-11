using Sankofa.Core.Personal;
using UnityEngine;

namespace Sankofa.Runtime.Personal
{
    /// <summary>Records one spoken question. The app owns consent, permission and what happens to the recording.</summary>
    public interface IQuestionRecorder
    {
        /// <summary>A microphone exists and can be used.</summary>
        bool Available { get; }
        bool Recording { get; }
        /// <summary>Seconds recorded so far.</summary>
        float Seconds { get; }
        bool Begin();
        /// <summary>Stops and returns mono samples at the service's rate, or null when nothing was captured.</summary>
        float[] End(out int count);
        void Cancel();
    }

    /// <summary>
    /// The headset's microphone through Unity. Nothing is recorded before <see cref="Begin"/>, and the recording is
    /// released as soon as <see cref="End"/> or <see cref="Cancel"/> returns.
    /// </summary>
    public sealed class MicrophoneRecorder : IQuestionRecorder
    {
        AudioClip clip;
        float started;

        public bool Available => Microphone.devices.Length > 0;
        public bool Recording => clip != null;
        public float Seconds => clip == null ? 0 : Mathf.Min(PersonalAnswerAudio.MaxSeconds, Time.realtimeSinceStartup - started);

        public bool Begin()
        {
            if (clip != null || !Available) return false;
            clip = Microphone.Start(null, false, PersonalAnswerAudio.MaxSeconds, PersonalAnswerAudio.SampleRate);
            started = Time.realtimeSinceStartup;
            return clip != null;
        }

        public float[] End(out int count)
        {
            count = 0;
            if (clip == null) return null;
            // A recording that ran its full length has already stopped, and its position has returned to zero.
            var frames = Microphone.IsRecording(null) ? Microphone.GetPosition(null) : clip.samples;
            Microphone.End(null);
            var recorded = clip; clip = null;
            try
            {
                if (frames <= 0) return null;
                var raw = new float[frames * recorded.channels];
                recorded.GetData(raw, 0);
                return Mono16k(raw, recorded.channels, recorded.frequency, out count);
            }
            finally { Object.Destroy(recorded); }
        }

        public void Cancel()
        {
            if (clip == null) return;
            Microphone.End(null);
            Object.Destroy(clip); clip = null;
        }

        /// <summary>The first channel at 16 kHz, resampled linearly when the device recorded at another rate.</summary>
        public static float[] Mono16k(float[] raw, int channels, int frequency, out int count)
        {
            count = 0;
            if (raw == null || frequency <= 0) return null;
            channels = Mathf.Max(1, channels);
            var frames = raw.Length / channels;
            if (frames == 0) return null;
            if (channels == 1 && frequency == PersonalAnswerAudio.SampleRate)
            {
                count = Mathf.Min(frames, PersonalAnswerAudio.MaxSamples);
                return raw;
            }
            count = Mathf.Min(PersonalAnswerAudio.MaxSamples, Mathf.FloorToInt(frames * (float)PersonalAnswerAudio.SampleRate / frequency));
            var result = new float[count];
            for (var i = 0; i < count; i++)
            {
                var at = i * (float)frequency / PersonalAnswerAudio.SampleRate;
                var a = Mathf.Min(frames - 1, (int)at); var b = Mathf.Min(frames - 1, a + 1);
                result[i] = Mathf.Lerp(raw[a * channels], raw[b * channels], at - a);
            }
            return result;
        }
    }
}
