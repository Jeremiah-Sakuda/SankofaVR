using System;
using UnityEngine;

namespace Sankofa.Runtime.Audio
{
    /// <summary>
    /// The sound of one story scene, from field recordings (Art/Recorded/sources.json): the stream, crickets and the
    /// Maasai Mara's birds and insects, wind in grass, a distant animal bell, tent canvas, and a matatu stage and the
    /// far city today. Each layer loops on its own placed source from a different point, and the scene's occasional
    /// sounds (a ewe's bleat, hammering along the line) follow it. It ducks under the narrator and follows the
    /// visitor's ambience level; it plays only while its scene is shown. No recorded voices are placed in the past:
    /// the people whose languages they would be have not been asked.
    /// </summary>
    public sealed class StoryAmbience : MonoBehaviour
    {
        [Serializable]
        public sealed class Layer
        {
            public AudioSource Source;
            public float Volume = 0.15f;
        }

        [SerializeField] Layer[] layers = Array.Empty<Layer>();
        [Tooltip("Occasional sounds of this scene, which follow its level and ducking.")]
        [SerializeField] ObjectSound[] voices = Array.Empty<ObjectSound>();
        [SerializeField] float duckTo = 0.55f;

        float duck = 1f;
        bool started;

        public float Level { get; set; } = 1f;
        public bool Duck { get; set; }

        void Start()
        {
            foreach (var layer in layers)
                NightAmbience.Play(layer.Source);
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
    }
}
