using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sankofa.Runtime.Fire
{
    /// <summary>
    /// Synthesised fire sound: a low roar and sparse pops, each a seamless loop of a different length so the
    /// combination does not repeat audibly. Generated at start-up, so it has no recording or licence to track.
    /// </summary>
    public sealed class FireCrackle : MonoBehaviour
    {
        [SerializeField] AudioSource roar;
        [SerializeField] AudioSource pops;
        [SerializeField] float roarVolume = 0.32f;
        [SerializeField] float popVolume = 0.55f;
        [SerializeField] int seed = 7;

        const int SampleRate = 48000;

        readonly List<(int Sample, float Strength)> popTimes = new List<(int, float)>();
        int lastPopSample;

        public float Level { get; set; } = 0.6f;
        /// <summary>Raised as each audible pop plays, with its strength, so sparks can burst in time with the sound.</summary>
        public event Action<float> Popped;

        void Start()
        {
            if (roar != null)
            {
                roar.clip = Loop("Fire roar", 9.7f, Roar);
                roar.loop = true;
                roar.Play();
            }
            if (pops != null)
            {
                pops.clip = Loop("Fire pops", 7.3f, (buffer, random) => Pops(buffer, random, popTimes));
                pops.loop = true;
                pops.Play();
            }
        }

        /// <summary>Deactivating the fire for a story scene stops its sources; resume them when the hearth returns.</summary>
        void OnEnable()
        {
            if (roar != null && roar.clip != null && !roar.isPlaying) roar.Play();
            if (pops != null && pops.clip != null && !pops.isPlaying) pops.Play();
        }

        void Update()
        {
            if (roar != null) roar.volume = roarVolume * (0.45f + 0.55f * Level);
            if (pops != null) pops.volume = popVolume * (0.25f + 0.75f * Level);
            AnnouncePops();
        }

        void AnnouncePops()
        {
            if (pops == null || pops.clip == null || !pops.isPlaying || Popped == null) return;
            var now = pops.timeSamples;
            var wrapped = now < lastPopSample;
            foreach (var (sample, strength) in popTimes)
                if (wrapped ? sample > lastPopSample || sample <= now : sample > lastPopSample && sample <= now)
                    Popped(strength);
            lastPopSample = now;
        }

        delegate void Synth(float[] buffer, System.Random random);

        /// <summary>Renders a second longer than needed and crossfades the tail into the head for a seamless loop.</summary>
        AudioClip Loop(string name, float seconds, Synth synth)
        {
            var length = (int)(seconds * SampleRate);
            var fade = SampleRate;
            var raw = new float[length + fade];
            synth(raw, new System.Random(seed + name.Length));
            var data = new float[length];
            for (var i = 0; i < length; i++)
            {
                data[i] = raw[i];
                if (i < fade)
                {
                    var t = (float)i / fade;
                    data[i] = raw[i] * t + raw[length + i] * (1f - t);
                }
            }
            var clip = AudioClip.Create(name, length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static void Roar(float[] buffer, System.Random random)
        {
            float low = 0, lower = 0;
            for (var i = 0; i < buffer.Length; i++)
            {
                var white = (float)(random.NextDouble() * 2 - 1);
                low += (white - low) * 0.06f;
                lower += (low - lower) * 0.02f;
                var t = (float)i / SampleRate;
                var swell = 0.75f + 0.25f * Mathf.Sin(t * 1.3f) * Mathf.Sin(t * 0.37f + 1.1f);
                buffer[i] = (lower * 2.2f + low * 0.35f) * swell;
            }
        }

        static void Pops(float[] buffer, System.Random random, List<(int, float)> times)
        {
            times.Clear();
            var loopLength = buffer.Length - SampleRate;
            var i = 0;
            while (i < buffer.Length)
            {
                // Poisson-spaced pops, about six per second, some in quick doubles.
                i += (int)(-Mathf.Log(1f - (float)random.NextDouble()) * SampleRate / 6f) + 40;
                var bursts = random.NextDouble() < 0.2 ? 2 : 1;
                for (var b = 0; b < bursts && i < buffer.Length; b++)
                {
                    var amplitude = 0.15f + 0.75f * (float)(random.NextDouble() * random.NextDouble());
                    if (i < loopLength && amplitude > 0.35f) times.Add((i, amplitude));
                    var decay = 0.0015f + 0.006f * (float)random.NextDouble();
                    var brightness = 0.35f + 0.5f * (float)random.NextDouble();
                    var count = (int)(decay * 6 * SampleRate);
                    float smooth = 0, previous = 0;
                    for (var k = 0; k < count && i + k < buffer.Length; k++)
                    {
                        var white = (float)(random.NextDouble() * 2 - 1);
                        smooth += (white - smooth) * brightness;
                        var high = smooth - previous;
                        previous = smooth;
                        buffer[i + k] += high * amplitude * Mathf.Exp(-k / (decay * SampleRate));
                    }
                    i += count + random.Next(200, 2400);
                }
            }
        }
    }
}
