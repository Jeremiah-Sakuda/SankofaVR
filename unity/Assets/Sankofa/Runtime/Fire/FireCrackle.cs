using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sankofa.Runtime.Fire
{
    /// <summary>
    /// The fire's sound: a field recording of a campfire, looped. Its loud cracks are found in the recording itself
    /// when it starts, so sparks can burst in time with what the visitor hears. See Art/Recorded/sources.json.
    /// </summary>
    public sealed class FireCrackle : MonoBehaviour
    {
        [SerializeField] AudioSource sound;
        [SerializeField] float volume = 0.5f;
        [Tooltip("How much louder than the fire's recent level a moment must be to count as a crack.")]
        [SerializeField] float crackRatio = 2.6f;

        readonly List<(int Sample, float Strength)> cracks = new List<(int, float)>();
        int lastSample;

        public float Level { get; set; } = 0.6f;
        /// <summary>Raised as each loud crack plays, with its strength, so sparks can burst in time with the sound.</summary>
        public event Action<float> Popped;

        void Start()
        {
            if (sound == null || sound.clip == null) return;
            FindCracks(sound.clip);
            sound.loop = true;
            sound.timeSamples = UnityEngine.Random.Range(0, sound.clip.samples);
            lastSample = sound.timeSamples;
            sound.Play();
        }

        /// <summary>Deactivating the fire for a story scene stops its source; resume it when the hearth returns.</summary>
        void OnEnable()
        {
            if (sound != null && sound.clip != null && !sound.isPlaying) sound.Play();
        }

        void Update()
        {
            if (sound == null) return;
            sound.volume = volume * (0.45f + 0.55f * Level);
            AnnounceCracks();
        }

        void AnnounceCracks()
        {
            if (sound.clip == null || !sound.isPlaying || Popped == null) return;
            var now = sound.timeSamples;
            var wrapped = now < lastSample;
            foreach (var (sample, strength) in cracks)
                if (wrapped ? sample > lastSample || sample <= now : sample > lastSample && sample <= now)
                    Popped(strength);
            lastSample = now;
        }

        /// <summary>
        /// Marks moments in the recording whose 5 ms level jumps well above its level over the surrounding 300 ms:
        /// the cracks and pops of burning wood. Analysis of the recording only; nothing is added to it.
        /// </summary>
        void FindCracks(AudioClip clip)
        {
            cracks.Clear();
            var data = new float[clip.samples * clip.channels];
            if (!clip.GetData(data, 0)) return;
            var window = Mathf.Max(1, clip.frequency / 200);
            var count = clip.samples / window;
            var level = new float[count];
            var loudest = 1e-6f;
            for (var w = 0; w < count; w++)
            {
                var sum = 0f;
                for (var k = 0; k < window; k++)
                {
                    var s = data[(w * window + k) * clip.channels];
                    sum += s * s;
                }
                level[w] = Mathf.Sqrt(sum / window);
                loudest = Mathf.Max(loudest, level[w]);
            }
            const int around = 60;
            var last = -100;
            for (var w = around; w < count - around; w++)
            {
                var background = 0f;
                for (var k = -around; k <= around; k++) background += level[w + k];
                background /= 2 * around + 1;
                if (level[w] > background * crackRatio && level[w] > loudest * 0.2f && w - last > 16)
                {
                    cracks.Add((w * window, Mathf.Clamp01(level[w] / loudest)));
                    last = w;
                }
            }
        }
    }
}
