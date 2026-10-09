using Sankofa.Runtime.Stage;
using UnityEngine;

namespace Sankofa.Runtime.Audio
{
    /// <summary>
    /// An occasional recorded sound in a story scene: the ewe bleating soon after she arrives and now and then
    /// afterwards, or hammering somewhere along the line. With a subject it sounds only while that object is in view;
    /// without one, whenever its scene is shown. It follows its scene's ambience level and ducks under the narrator.
    /// </summary>
    public sealed class ObjectSound : MonoBehaviour
    {
        [SerializeField] StageObject subject;
        [SerializeField] AudioSource source;
        [Tooltip("Recordings to choose from at random each time.")]
        [SerializeField] AudioClip[] clips = new AudioClip[0];
        [SerializeField] float volume = 0.35f;
        [Tooltip("Seconds between sounds, at random within this range.")]
        [SerializeField] Vector2 interval = new Vector2(9f, 18f);
        [Tooltip("Seconds after it can first be heard before the first sound.")]
        [SerializeField] float firstAfter = 3f;

        float countdown;
        bool wasAudible;

        public float Level { get; set; } = 1f;
        public bool Duck { get; set; }

        void OnEnable() => wasAudible = false;

        void Update()
        {
            var audible = subject == null || subject.Visible;
            if (audible && !wasAudible) countdown = firstAfter;
            wasAudible = audible;
            if (!audible || clips.Length == 0 || source == null) return;
            countdown -= Sankofa.Runtime.Stage.StageClock.DeltaTime;
            if (countdown > 0) return;
            countdown = Random.Range(interval.x, interval.y);
            var clip = clips[Random.Range(0, clips.Length)];
            if (clip != null) source.PlayOneShot(clip, volume * Mathf.Clamp01(Level) * (Duck ? 0.55f : 1f));
        }
    }
}
