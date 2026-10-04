using System;
using System.Collections;
using System.Collections.Generic;
using Sankofa.Flow;
using UnityEngine;

namespace Sankofa.Runtime.Stage
{
    /// <summary>
    /// Changes the world around the stationary visitor without moving the camera. Full: the fire rises into a curtain
    /// of warm light, the scene changes behind it and the light parts. Reduced motion: a short, dim dissolve with no
    /// rising fire or moving light. Both carry the same continuities (an ember becoming a lantern) and keep orientation.
    /// </summary>
    public sealed class StoryTransition : MonoBehaviour
    {
        [SerializeField] Renderer curtain;
        [SerializeField] StoryStage stage;
        [SerializeField] Color fullColor = new Color(1f, 0.62f, 0.3f);
        [SerializeField] Color reducedColor = new Color(0.22f, 0.16f, 0.11f);
        [SerializeField] float fullIn = 1.5f;
        [SerializeField] float fullHold = 0.35f;
        [SerializeField] float fullOut = 1.7f;
        [SerializeField] float reducedIn = 0.7f;
        [SerializeField] float reducedOut = 0.7f;

        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int AlphaId = Shader.PropertyToID("_Alpha");
        static readonly int MotionId = Shader.PropertyToID("_Motion");

        MaterialPropertyBlock block;
        Coroutine routine;

        public bool ReducedMotion { get; set; }
        public bool Busy => routine != null;
        public event Action<int> Finished;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            if (curtain != null) curtain.enabled = false;
        }

        public void Play(int token, string toScene, IReadOnlyList<Continuity> continuities)
        {
            Cancel();
            routine = StartCoroutine(Run(token, toScene, continuities));
        }

        public void Cancel()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            if (curtain != null) curtain.enabled = false;
        }

        IEnumerator Run(int token, string toScene, IReadOnlyList<Continuity> continuities)
        {
            var reduced = ReducedMotion;
            var peak = reduced ? 0.94f : 1f;
            curtain.enabled = true;
            curtain.GetPropertyBlock(block);
            block.SetColor(ColorId, reduced ? reducedColor : fullColor);
            block.SetFloat(MotionId, reduced ? 0 : 1);

            yield return Fade(0, peak, reduced ? reducedIn : fullIn);
            if (!reduced) yield return new WaitForSeconds(fullHold);

            foreach (var c in continuities) stage.Cross(c.From, c.To, toScene);
            stage.ShowScene(toScene);

            yield return Fade(peak, 0, reduced ? reducedOut : fullOut);
            curtain.enabled = false;
            routine = null;
            Finished?.Invoke(token);
        }

        IEnumerator Fade(float from, float to, float seconds)
        {
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                var x = Mathf.Clamp01(t / seconds);
                SetAlpha(Mathf.Lerp(from, to, x * x * (3f - 2f * x)));
                yield return null;
            }
            SetAlpha(to);
        }

        void SetAlpha(float alpha)
        {
            block.SetFloat(AlphaId, alpha);
            curtain.SetPropertyBlock(block);
        }
    }
}
