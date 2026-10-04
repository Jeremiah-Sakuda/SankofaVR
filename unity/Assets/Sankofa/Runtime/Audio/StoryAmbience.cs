using System;
using UnityEngine;

namespace Sankofa.Runtime.Audio
{
    /// <summary>
    /// The sound of one story scene, synthesised at start-up like the hearth's night, so there is no recording to
    /// license: the stream, insects, wind, hammers on iron in the 1899 camp, or the city around the river today.
    /// Each layer is a seamless loop on its own placed source. It ducks under the narrator and follows the visitor's
    /// ambience level; it plays only while its scene is shown.
    /// </summary>
    public sealed class StoryAmbience : MonoBehaviour
    {
        public enum Kind { Stream, Crickets, Chorus, Wind, Hammers, City }

        [Serializable]
        public sealed class Layer
        {
            public Kind Kind;
            public AudioSource Source;
            public float Volume = 0.15f;
        }

        [SerializeField] Layer[] layers = Array.Empty<Layer>();
        [SerializeField] float duckTo = 0.55f;
        [SerializeField] int seed = 61;

        const int Rate = NightAmbience.SampleRate;
        float duck = 1f;
        bool started;

        public float Level { get; set; } = 1f;
        public bool Duck { get; set; }

        void Start()
        {
            for (var i = 0; i < layers.Length; i++)
            {
                var layer = layers[i];
                if (layer.Source == null) continue;
                var s = seed + i * 7;
                var clip = layer.Kind switch
                {
                    Kind.Stream => NightAmbience.Loop("Stream", 17.3f, Stream, s),
                    Kind.Crickets => NightAmbience.Loop("Crickets", 13.1f, (b, r) => NightAmbience.Cricket(b, r, 4300f + 97f * i, 1f), s),
                    Kind.Chorus => NightAmbience.Loop("Chorus", 19.7f, NightAmbience.Chorus, s),
                    Kind.Wind => NightAmbience.Loop("Wind", 29.3f, NightAmbience.Wind, s),
                    Kind.Hammers => NightAmbience.Loop("Hammers", 23.9f, Hammers, s),
                    _ => NightAmbience.Loop("City", 31.1f, City, s),
                };
                layer.Source.clip = clip;
                layer.Source.loop = true;
                layer.Source.timeSamples = UnityEngine.Random.Range(0, clip.samples);
                layer.Source.Play();
            }
            started = true;
            Update();
        }

        void OnEnable()
        {
            if (!started) return;
            foreach (var layer in layers)
                if (layer.Source != null && layer.Source.clip != null && !layer.Source.isPlaying) layer.Source.Play();
        }

        void Update()
        {
            duck = Mathf.MoveTowards(duck, Duck ? duckTo : 1f, Time.unscaledDeltaTime / 0.5f);
            var level = Mathf.Clamp01(Level) * duck;
            foreach (var layer in layers)
                if (layer.Source != null) layer.Source.volume = layer.Volume * level;
        }

        /// <summary>
        /// A small stream over stones: a soft band of rushing noise and, on top, the short rising "bloops" of
        /// bubbles bursting, scattered irregularly so it babbles rather than hisses.
        /// </summary>
        static void Stream(float[] buffer, System.Random random)
        {
            float low = 0, band = 0, prev = 0;
            for (var i = 0; i < buffer.Length; i++)
            {
                var white = (float)(random.NextDouble() * 2 - 1);
                low += (white - low) * 0.18f;
                var high = low - prev;
                prev = low;
                band += (high - band) * 0.5f;
                buffer[i] = band * 0.9f;
            }
            var t = 0;
            while (t < buffer.Length)
            {
                var length = (int)((0.012 + 0.03 * random.NextDouble()) * Rate);
                var start = 380f + 700f * (float)random.NextDouble();
                var rise = 1.4f + 1.2f * (float)random.NextDouble();
                var amplitude = 0.05f + 0.12f * (float)random.NextDouble();
                var phase = 0f;
                for (var k = 0; k < length && t + k < buffer.Length; k++)
                {
                    var f = start * Mathf.Pow(rise, (float)k / length);
                    phase += 2f * Mathf.PI * f / Rate;
                    var envelope = Mathf.Sin(Mathf.PI * k / length);
                    buffer[t + k] += amplitude * envelope * Mathf.Sin(phase);
                }
                t += (int)((0.004 + 0.06 * random.NextDouble() * random.NextDouble()) * Rate);
            }
        }

        /// <summary>
        /// Hammers on iron somewhere along the line: groups of two to four strikes, each a few inharmonic metal
        /// partials ringing down, softened by distance, with long uneven rests between gangs.
        /// </summary>
        static void Hammers(float[] buffer, System.Random random)
        {
            var partials = new[] { 1.0f, 2.76f, 5.4f, 8.93f };
            var i = (int)(random.NextDouble() * Rate);
            while (i < buffer.Length)
            {
                var strikes = 2 + random.Next(3);
                var pitch = 620f + 260f * (float)random.NextDouble();
                for (var s = 0; s < strikes && i < buffer.Length; s++)
                {
                    var amplitude = 0.35f + 0.2f * (float)random.NextDouble();
                    var length = (int)(0.9f * Rate);
                    for (var k = 0; k < length && i + k < buffer.Length; k++)
                    {
                        var time = (float)k / Rate;
                        var v = 0f;
                        for (var p = 0; p < partials.Length; p++)
                            v += Mathf.Sin(2f * Mathf.PI * pitch * partials[p] * time) * Mathf.Exp(-time * (6f + p * 5f)) / (1 + p);
                        buffer[i + k] += amplitude * v;
                    }
                    i += (int)((0.55 + 0.25 * random.NextDouble()) * Rate);
                }
                i += (int)((1.5 + 4.0 * random.NextDouble()) * Rate);
            }
            // Distance: a gentle low-pass takes the edge off the ring.
            float smooth = 0;
            for (var k = 0; k < buffer.Length; k++)
            {
                smooth += (buffer[k] - smooth) * 0.35f;
                buffer[k] = smooth;
            }
        }

        /// <summary>
        /// The city around the river at night: a deep traffic rumble that swells as vehicles pass, and now and then
        /// a far-off two-note horn.
        /// </summary>
        static void City(float[] buffer, System.Random random)
        {
            float low = 0, lower = 0, swell = 0.5f, target = 0.5f;
            var change = 0;
            for (var i = 0; i < buffer.Length; i++)
            {
                if (--change <= 0)
                {
                    target = 0.3f + 0.7f * (float)random.NextDouble();
                    change = (int)((1.5 + 3.5 * random.NextDouble()) * Rate);
                }
                swell += (target - swell) * 0.00008f;
                var white = (float)(random.NextDouble() * 2 - 1);
                low += (white - low) * 0.02f;
                lower += (low - lower) * 0.02f;
                buffer[i] = lower * 5f * swell;
            }
            var t = (int)(random.NextDouble() * 4 * Rate);
            while (t < buffer.Length)
            {
                var f1 = 380f + 80f * (float)random.NextDouble();
                var f2 = f1 * (1.18f + 0.1f * (float)random.NextDouble());
                var length = (int)(0.35f * Rate);
                for (var note = 0; note < 2; note++)
                {
                    var f = note == 0 ? f1 : f2;
                    for (var k = 0; k < length && t + k < buffer.Length; k++)
                    {
                        var envelope = Mathf.Min(1f, k / (0.03f * Rate)) * Mathf.Min(1f, (length - k) / (0.05f * Rate));
                        var phase = 2f * Mathf.PI * f * k / Rate;
                        // A buzzy horn, softened: odd harmonics only.
                        var v = Mathf.Sin(phase) + Mathf.Sin(phase * 3) / 3 + Mathf.Sin(phase * 5) / 5;
                        buffer[t + k] += 0.03f * envelope * v;
                    }
                    t += length + (int)(0.05f * Rate);
                }
                t += (int)((5 + 9 * random.NextDouble()) * Rate);
            }
        }
    }
}
