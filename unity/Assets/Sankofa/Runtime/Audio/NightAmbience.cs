using UnityEngine;

namespace Sankofa.Runtime.Audio
{
    /// <summary>
    /// The night around the hearth, synthesised at start-up so there is no recording or licence to track: a few
    /// crickets placed around the clearing, a faint distant chorus, and wind that gusts and rustles. Each is a
    /// seamless loop of its own length so the whole does not audibly repeat. It ducks under the narrator's voice
    /// and follows the visitor's ambience level.
    /// </summary>
    public sealed class NightAmbience : MonoBehaviour
    {
        [SerializeField] AudioSource[] crickets;
        [SerializeField] AudioSource chorus;
        [SerializeField] AudioSource wind;
        [SerializeField] float cricketVolume = 0.14f;
        [SerializeField] float chorusVolume = 0.1f;
        [SerializeField] float windVolume = 0.16f;
        [Tooltip("Fraction of the ambience kept while the narrator speaks.")]
        [SerializeField] float duckTo = 0.55f;
        [SerializeField] int seed = 41;

        // Insects and wind need no more: half the output rate halves memory and start-up synthesis.
        internal const int SampleRate = 24000;

        float duck = 1f;
        bool started;

        /// <summary>The visitor's ambience level: 1 full, 0.5 low, 0 off.</summary>
        public float Level { get; set; } = 1f;
        /// <summary>True while the narrator speaks.</summary>
        public bool Duck { get; set; }

        void Start()
        {
            for (var i = 0; i < crickets.Length; i++)
            {
                var carrier = 4100f + 260f * i + (i % 2) * 130f;
                Play(crickets[i], Loop("Cricket " + i, 12.7f + 1.9f * i, (b, r) => Cricket(b, r, carrier, 1f), seed + i));
            }
            Play(chorus, Loop("Cricket chorus", 18.9f, Chorus, seed + 20));
            Play(wind, Loop("Wind", 28.7f, Wind, seed + 30));
            started = true;
            Update();
        }

        /// <summary>Leaving the hearth for a story stops these sources; resume them when the hearth returns.</summary>
        void OnEnable()
        {
            if (!started) return;
            foreach (var source in crickets) Resume(source);
            Resume(chorus);
            Resume(wind);
        }

        static void Resume(AudioSource source)
        {
            if (source != null && source.clip != null && !source.isPlaying) source.Play();
        }

        static void Play(AudioSource source, AudioClip clip)
        {
            if (source == null) return;
            source.clip = clip;
            source.loop = true;
            // Start each loop somewhere different so the layers never line up.
            source.timeSamples = Random.Range(0, clip.samples);
            source.Play();
        }

        void Update()
        {
            duck = Mathf.MoveTowards(duck, Duck ? duckTo : 1f, Time.unscaledDeltaTime / 0.5f);
            var level = Mathf.Clamp01(Level) * duck;
            foreach (var source in crickets)
                if (source != null) source.volume = cricketVolume * level;
            if (chorus != null) chorus.volume = chorusVolume * level;
            if (wind != null) wind.volume = windVolume * level;
        }

        internal delegate void Synth(float[] buffer, System.Random random);

        /// <summary>Renders a second longer than needed and crossfades the tail into the head for a seamless loop.</summary>
        internal static AudioClip Loop(string name, float seconds, Synth synth, int seed)
        {
            var length = (int)(seconds * SampleRate);
            var fade = SampleRate;
            var raw = new float[length + fade];
            synth(raw, new System.Random(seed));
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

        /// <summary>
        /// A field cricket: chirps of three to five short pulses of a high tone, about a second apart, with the
        /// occasional rest so it sounds alive rather than mechanical.
        /// </summary>
        internal static void Cricket(float[] buffer, System.Random random, float carrier, float gain)
        {
            var i = (int)(random.NextDouble() * SampleRate);
            var pulse = (int)(0.011f * SampleRate);
            var gap = (int)(0.019f * SampleRate);
            var step = 2f * Mathf.PI * carrier / SampleRate;
            while (i < buffer.Length)
            {
                var pulses = 3 + random.Next(3);
                var amplitude = gain * (0.55f + 0.45f * (float)random.NextDouble());
                var phase = (float)random.NextDouble() * 6.283f;
                for (var p = 0; p < pulses; p++)
                {
                    for (var k = 0; k < pulse && i + k < buffer.Length; k++)
                    {
                        var envelope = Mathf.Sin(Mathf.PI * k / pulse);
                        buffer[i + k] += amplitude * envelope * envelope * Mathf.Sin(phase + step * k);
                    }
                    i += pulse + gap;
                }
                var rest = random.NextDouble() < 0.07 ? 2.0 + 3.0 * random.NextDouble() : 0.45 + 0.5 * random.NextDouble();
                i += (int)(rest * SampleRate);
            }
        }

        /// <summary>Many distant crickets, quieter and at other pitches, over a faint high hiss of insects.</summary>
        internal static void Chorus(float[] buffer, System.Random random)
        {
            for (var c = 0; c < 8; c++)
                Cricket(buffer, random, 3700f + 220f * c + (float)random.NextDouble() * 90f, 0.12f + 0.2f * (float)random.NextDouble());
            float previous = 0;
            for (var i = 0; i < buffer.Length; i++)
            {
                var white = (float)(random.NextDouble() * 2 - 1);
                var high = white - previous;
                previous = white;
                buffer[i] += high * 0.015f;
            }
        }

        /// <summary>Low, slowly gusting wind, with a leafy rustle that rises with each gust.</summary>
        internal static void Wind(float[] buffer, System.Random random)
        {
            float low = 0, lower = 0, rustle = 0, gust = 0.5f, target = 0.5f;
            var change = 0;
            for (var i = 0; i < buffer.Length; i++)
            {
                if (--change <= 0)
                {
                    target = 0.25f + 0.75f * (float)random.NextDouble();
                    change = (int)((2.5 + 4.0 * random.NextDouble()) * SampleRate);
                }
                gust += (target - gust) * 0.00004f;
                var white = (float)(random.NextDouble() * 2 - 1);
                low += (white - low) * 0.035f;
                lower += (low - lower) * 0.03f;
                rustle += (white - rustle) * 0.6f;
                var leaves = (white - rustle) * gust * gust * 0.12f;
                buffer[i] = lower * 2.6f * gust + leaves;
            }
        }
    }
}
