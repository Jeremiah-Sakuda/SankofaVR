using System;
using UnityEngine;

namespace Sankofa.Runtime.Audio
{
    /// <summary>
    /// The sound of one story scene, synthesised at start-up like the hearth's night, so there is no recording to
    /// license: the stream, insects, birds, wind, a distant sheep bell, hammers on iron and canvas in the 1899 camp,
    /// voices, or the city around the river today. Voices are a wordless murmur: no language is spoken until the
    /// people whose language it is have been asked. Each layer is a seamless loop on its own placed source, and the
    /// scene's own occasional sounds (a ewe's bleat) follow it. It ducks under the narrator and follows the visitor's
    /// ambience level; it plays only while its scene is shown.
    /// </summary>
    public sealed class StoryAmbience : MonoBehaviour
    {
        public enum Kind { Stream, Crickets, Chorus, Wind, Hammers, City, Bell, Birds, Voices, MenAtWork, Canvas }

        [Serializable]
        public sealed class Layer
        {
            public Kind Kind;
            public AudioSource Source;
            public float Volume = 0.15f;
        }

        [SerializeField] Layer[] layers = Array.Empty<Layer>();
        [Tooltip("Occasional sounds of objects in this scene, which follow its level and ducking.")]
        [SerializeField] ObjectSound[] voices = Array.Empty<ObjectSound>();
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
                    Kind.Bell => NightAmbience.Loop("Sheep bell", 27.7f, Bell, s),
                    Kind.Birds => NightAmbience.Loop("Birds", 21.3f, Birds, s),
                    Kind.Voices => NightAmbience.Loop("Voices", 33.7f, (b, r) => Murmur(b, r, 4, 105f, 230f), s),
                    Kind.MenAtWork => NightAmbience.Loop("Work gang", 37.1f, (b, r) => Murmur(b, r, 6, 90f, 140f), s),
                    Kind.Canvas => NightAmbience.Loop("Canvas", 19.9f, Canvas, s),
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
            foreach (var voice in voices)
            {
                if (voice == null) continue;
                voice.Level = Level;
                voice.Duck = Duck;
            }
        }

        /// <summary>
        /// A small clapper bell on an animal somewhere out on the plain: clusters of two to five uneven clanks as it
        /// walks, a few bright inharmonic partials ringing down, softened by distance, with long rests.
        /// </summary>
        static void Bell(float[] buffer, System.Random random)
        {
            var partials = new[] { 1.0f, 2.32f, 4.25f, 6.8f };
            var i = (int)(random.NextDouble() * 2 * Rate);
            while (i < buffer.Length)
            {
                var clanks = 2 + random.Next(4);
                var pitch = 1050f + 120f * (float)random.NextDouble();
                for (var c = 0; c < clanks && i < buffer.Length; c++)
                {
                    var amplitude = 0.18f + 0.14f * (float)random.NextDouble();
                    var length = (int)(0.8f * Rate);
                    for (var k = 0; k < length && i + k < buffer.Length; k++)
                    {
                        var time = (float)k / Rate;
                        var v = 0f;
                        for (var p = 0; p < partials.Length; p++)
                            v += Mathf.Sin(2f * Mathf.PI * pitch * partials[p] * time) * Mathf.Exp(-time * (5f + p * 4f)) / (1 + p * 0.7f);
                        buffer[i + k] += amplitude * v;
                    }
                    i += (int)((0.28 + 0.4 * random.NextDouble()) * Rate);
                }
                i += (int)((3.0 + 7.0 * random.NextDouble()) * Rate);
            }
            LowPass(buffer, 0.3f);
        }

        /// <summary>Birds settling at dusk: now and then a run of quick falling chirps, or a two-note whistle.</summary>
        static void Birds(float[] buffer, System.Random random)
        {
            var t = (int)(random.NextDouble() * Rate);
            while (t < buffer.Length)
            {
                var amplitude = 0.06f + 0.08f * (float)random.NextDouble();
                if (random.NextDouble() < 0.6)
                {
                    var chirps = 3 + random.Next(4);
                    var top = 3000f + 900f * (float)random.NextDouble();
                    for (var c = 0; c < chirps; c++)
                    {
                        var length = (int)((0.04 + 0.03 * random.NextDouble()) * Rate);
                        var phase = 0f;
                        for (var k = 0; k < length && t + k < buffer.Length; k++)
                        {
                            var f = Mathf.Lerp(top, top * 0.72f, (float)k / length);
                            phase += 2f * Mathf.PI * f / Rate;
                            buffer[t + k] += amplitude * Mathf.Sin(Mathf.PI * k / length) * Mathf.Sin(phase);
                        }
                        t += length + (int)(0.035f * Rate);
                    }
                }
                else
                {
                    var low = 1700f + 300f * (float)random.NextDouble();
                    foreach (var f in new[] { low, low * 1.22f })
                    {
                        var length = (int)(0.17f * Rate);
                        for (var k = 0; k < length && t + k < buffer.Length; k++)
                        {
                            var vibrato = 1f + 0.01f * Mathf.Sin(2f * Mathf.PI * 30f * k / Rate);
                            buffer[t + k] += amplitude * Mathf.Sin(Mathf.PI * k / length) * Mathf.Sin(2f * Mathf.PI * f * vibrato * k / Rate);
                        }
                        t += length + (int)(0.06f * Rate);
                    }
                }
                t += (int)((1.8 + 5.0 * random.NextDouble()) * Rate);
            }
        }

        /// <summary>
        /// Voices at a distance: several speakers taking turns and overlapping, each a voiced pulse with its own pitch
        /// and rising and falling intonation, shaped syllable by syllable through vowel-like resonances. It is muffled
        /// heavily, as by distance and canvas, and is built from no words, so it is speech-like but says nothing.
        /// </summary>
        static void Murmur(float[] buffer, System.Random random, int speakers, float lowPitch, float highPitch)
        {
            var vowels = new[] { (700f, 1150f), (500f, 1800f), (320f, 2200f), (450f, 900f), (360f, 800f), (600f, 1500f) };
            for (var s = 0; s < speakers; s++)
            {
                var pitch = Mathf.Lerp(lowPitch, highPitch, (float)random.NextDouble());
                var gain = 0.05f + 0.05f * (float)random.NextDouble();
                var t = (int)(random.NextDouble() * 3 * Rate);
                var phase = 0f;
                while (t < buffer.Length)
                {
                    // One utterance: a run of syllables under a falling intonation.
                    var syllables = 3 + random.Next(10);
                    var start = pitch * (1.05f + 0.15f * (float)random.NextDouble());
                    for (var y = 0; y < syllables && t < buffer.Length; y++)
                    {
                        var length = (int)((0.12 + 0.16 * random.NextDouble()) * Rate);
                        var (f1, f2) = vowels[random.Next(vowels.Length)];
                        var r1 = new Resonator(f1, 6f);
                        var r2 = new Resonator(f2, 9f);
                        var f0 = start * (1f - 0.25f * y / Mathf.Max(1, syllables - 1));
                        for (var k = 0; k < length && t + k < buffer.Length; k++)
                        {
                            var envelope = Mathf.Sin(Mathf.PI * k / length);
                            phase += f0 * (1f + 0.04f * Mathf.Sin(k * 0.002f)) / Rate;
                            var source = 2f * (phase - Mathf.Floor(phase)) - 1f;
                            if (k < 0.02f * Rate) source += (float)(random.NextDouble() * 2 - 1) * 0.5f;
                            buffer[t + k] += gain * envelope * (r1.Process(source) + 0.6f * r2.Process(source));
                        }
                        t += length + (int)(0.015 * Rate * random.NextDouble());
                    }
                    t += (int)((0.6 + 3.5 * random.NextDouble()) * Rate);
                }
            }
            LowPass(buffer, 0.16f);
            LowPass(buffer, 0.2f);
        }

        /// <summary>Tent canvas in the wind: gusts that set it fluttering, with a quiet rustle between them.</summary>
        static void Canvas(float[] buffer, System.Random random)
        {
            float low = 0, band = 0, prev = 0, gust = 0, target = 0, flutterPhase = 0, flutter = 11f;
            var change = 0;
            for (var i = 0; i < buffer.Length; i++)
            {
                if (--change <= 0)
                {
                    var gusting = random.NextDouble() < 0.45;
                    target = gusting ? 0.5f + 0.5f * (float)random.NextDouble() : 0.06f;
                    flutter = 8f + 7f * (float)random.NextDouble();
                    change = (int)((gusting ? 0.5 + 1.5 * random.NextDouble() : 1.5 + 4.0 * random.NextDouble()) * Rate);
                }
                gust += (target - gust) * 0.0006f;
                flutterPhase += flutter / Rate;
                var flap = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * flutterPhase);
                var white = (float)(random.NextDouble() * 2 - 1);
                low += (white - low) * 0.25f;
                var high = low - prev;
                prev = low;
                band += (high - band) * 0.35f;
                buffer[i] = band * gust * (0.25f + 0.75f * flap * flap) * 1.6f;
            }
        }

        static void LowPass(float[] buffer, float amount)
        {
            float smooth = 0;
            for (var k = 0; k < buffer.Length; k++)
            {
                smooth += (buffer[k] - smooth) * amount;
                buffer[k] = smooth;
            }
        }

        /// <summary>A two-pole resonance (a band-pass biquad), for vowel-like formants.</summary>
        internal struct Resonator
        {
            readonly float b0, b2, a1, a2;
            float x1, x2, y1, y2;

            public Resonator(float frequency, float q)
            {
                var w = 2f * Mathf.PI * frequency / Rate;
                var alpha = Mathf.Sin(w) / (2f * q);
                var a0 = 1f + alpha;
                b0 = alpha / a0;
                b2 = -alpha / a0;
                a1 = -2f * Mathf.Cos(w) / a0;
                a2 = (1f - alpha) / a0;
                x1 = x2 = y1 = y2 = 0;
            }

            public float Process(float x)
            {
                var y = b0 * x + b2 * x2 - a1 * y1 - a2 * y2;
                x2 = x1;
                x1 = x;
                y2 = y1;
                y1 = y;
                return y;
            }
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
