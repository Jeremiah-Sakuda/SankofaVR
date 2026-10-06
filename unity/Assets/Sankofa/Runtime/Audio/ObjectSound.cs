using Sankofa.Runtime.Stage;
using UnityEngine;

namespace Sankofa.Runtime.Audio
{
    /// <summary>
    /// The occasional sound of one staged object while it is in view: the ewe bleating soon after she arrives and
    /// now and then afterwards. Synthesised at start-up like the ambience, so there is no recording to license; it
    /// follows its scene's ambience level and ducks under the narrator with it.
    /// </summary>
    public sealed class ObjectSound : MonoBehaviour
    {
        public enum Voice { Bleat }

        [SerializeField] Voice voice;
        [SerializeField] StageObject subject;
        [SerializeField] AudioSource source;
        [SerializeField] float volume = 0.35f;
        [Tooltip("Seconds between sounds, at random within this range.")]
        [SerializeField] Vector2 interval = new Vector2(9f, 18f);
        [Tooltip("Seconds after the object comes into view before the first sound.")]
        [SerializeField] float firstAfter = 3f;
        [SerializeField] int seed = 5;

        const int Rate = NightAmbience.SampleRate;
        AudioClip[] clips;
        float countdown;
        bool wasVisible;

        public float Level { get; set; } = 1f;
        public bool Duck { get; set; }

        void Start()
        {
            clips = new AudioClip[3];
            for (var i = 0; i < clips.Length; i++)
            {
                var random = new System.Random(seed + i * 13);
                var seconds = 0.75f + 0.35f * (float)random.NextDouble();
                var data = new float[(int)(seconds * Rate)];
                Bleat(data, random);
                clips[i] = AudioClip.Create(voice + " " + i, data.Length, 1, Rate, false);
                clips[i].SetData(data, 0);
            }
        }

        void Update()
        {
            var visible = subject != null && subject.Visible;
            if (visible && !wasVisible) countdown = firstAfter;
            wasVisible = visible;
            if (!visible || clips == null || source == null) return;
            countdown -= Time.deltaTime;
            if (countdown > 0) return;
            countdown = Random.Range(interval.x, interval.y);
            source.PlayOneShot(clips[Random.Range(0, clips.Length)], volume * Mathf.Clamp01(Level) * (Duck ? 0.55f : 1f));
        }

        /// <summary>
        /// A ewe's bleat: a buzzy voiced tone that rises a little then falls, with the fast flutter of a sheep's
        /// call, shaped by three mouth resonances and a breath of noise.
        /// </summary>
        static void Bleat(float[] buffer, System.Random random)
        {
            var f0 = 300f + 80f * (float)random.NextDouble();
            var flutterRate = 18f + 7f * (float)random.NextDouble();
            var r1 = new StoryAmbience.Resonator(650f + 120f * (float)random.NextDouble(), 5f);
            var r2 = new StoryAmbience.Resonator(1350f + 200f * (float)random.NextDouble(), 7f);
            var r3 = new StoryAmbience.Resonator(2700f, 8f);
            var phase = 0f;
            var peak = 1e-4f;
            for (var i = 0; i < buffer.Length; i++)
            {
                var t = (float)i / Rate;
                var left = (float)(buffer.Length - i) / Rate;
                var envelope = Mathf.SmoothStep(0f, 1f, t / 0.06f) * Mathf.SmoothStep(0f, 1f, left / 0.25f);
                var contour = 1f + 0.08f * Mathf.Clamp01(t / 0.15f) - 0.12f * t;
                var flutter = 1f + 0.35f * Mathf.Sin(2f * Mathf.PI * flutterRate * t);
                phase += f0 * contour * (1f + 0.015f * Mathf.Sin(2f * Mathf.PI * 6f * t)) / Rate;
                var saw = 2f * (phase - Mathf.Floor(phase)) - 1f;
                var breath = (float)(random.NextDouble() * 2 - 1) * 0.15f;
                var x = (saw * 0.8f + breath) * envelope * flutter;
                buffer[i] = r1.Process(x) + 0.7f * r2.Process(x) + 0.3f * r3.Process(x);
                peak = Mathf.Max(peak, Mathf.Abs(buffer[i]));
            }
            for (var i = 0; i < buffer.Length; i++) buffer[i] *= 0.6f / peak;
        }
    }
}
