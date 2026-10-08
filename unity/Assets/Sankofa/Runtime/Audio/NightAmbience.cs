using UnityEngine;

namespace Sankofa.Runtime.Audio
{
    /// <summary>
    /// The night around the hearth, from field recordings (Art/Recorded/sources.json): crickets recorded in the
    /// Maasai Mara placed around the clearing, the Mara's night further off, and wind in dry grass. Each source loops
    /// from a different point so the layers never line up. It ducks under the narrator's voice and follows the
    /// visitor's ambience level.
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

        float duck = 1f;
        bool started;

        /// <summary>The visitor's ambience level: 1 full, 0.5 low, 0 off.</summary>
        public float Level { get; set; } = 1f;
        /// <summary>True while the narrator speaks.</summary>
        public bool Duck { get; set; }

        void Start()
        {
            foreach (var source in crickets) Play(source);
            Play(chorus);
            Play(wind);
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

        /// <summary>Loops a source's recording from a random point.</summary>
        internal static void Play(AudioSource source)
        {
            if (source == null || source.clip == null) return;
            source.loop = true;
            source.timeSamples = Random.Range(0, source.clip.samples);
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
    }
}
