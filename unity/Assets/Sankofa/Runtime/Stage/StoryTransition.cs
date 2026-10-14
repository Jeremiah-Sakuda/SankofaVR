using System;
using System.Collections;
using System.Collections.Generic;
using Sankofa.Flow;
using UnityEngine;

namespace Sankofa.Runtime.Stage
{
    /// <summary>
    /// Changes the world around the stationary visitor without moving the camera. Full: flame spreads across the view
    /// outward from the light the visitor carries (the ember, or the fire), the scene changes behind it, and the new
    /// place opens outward from its own light (the traders' fire, the lantern, the jiko). Reduced motion: a short, dim
    /// dissolve with no moving light. Both carry the same continuities and keep orientation.
    /// </summary>
    public sealed class StoryTransition : MonoBehaviour
    {
        [SerializeField] Renderer curtain;
        [SerializeField] StoryStage stage;
        [SerializeField] LivingGallery gallery;
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
        static readonly int SourceId = Shader.PropertyToID("_Source");
        static readonly int TargetId = Shader.PropertyToID("_Target");
        static readonly int CoverId = Shader.PropertyToID("_Cover");
        static readonly int HoleId = Shader.PropertyToID("_Hole");
        static readonly int SeeThroughId = Shader.PropertyToID("_SeeThrough");
        // Past 1 the front has crossed the whole sphere, ragged edge included.
        const float Across = 1.2f;

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

        /// <summary>Watchdog recovery stops the coroutine before any late scene swap can occur.</summary>
        public void Recover(Cue.Transition change)
        {
            Cancel();
            foreach (var c in change.Continuities) stage.Cross(c.From, c.To, change.To.Id);
            stage.ShowScene(change.To.Id);
            gallery?.SettleView(change.To.Id);
        }

        IEnumerator Run(int token, string toScene, IReadOnlyList<Continuity> continuities)
        {
            if (gallery != null)
            {
                yield return gallery.Transition(toScene, continuities, ReducedMotion);
                routine = null;
                Finished?.Invoke(token);
                yield break;
            }
            var reduced = ReducedMotion;
            var peak = reduced ? 0.94f : 1f;
            curtain.enabled = true;
            curtain.GetPropertyBlock(block);
            block.SetColor(ColorId, reduced ? reducedColor : fullColor);
            block.SetFloat(MotionId, reduced ? 0 : 1);
            block.SetFloat(CoverId, 0);
            block.SetFloat(HoleId, 0);
            block.SetFloat(SeeThroughId, 0);
            block.SetVector(SourceId, Toward(continuities.Count > 0 ? continuities[0].From : null));
            curtain.SetPropertyBlock(block);

            if (reduced)
            {
                yield return Fade(AlphaId, 0, peak, reducedIn);
                foreach (var c in continuities) stage.Cross(c.From, c.To, toScene);
                stage.ShowScene(toScene);
            }
            else
            {
                yield return Fade(CoverId, 0, Across, fullIn);
                // Once the flame has covered the view, the new place is already there behind it, glimmering through
                // the dark ember while the light holds, before it opens.
                foreach (var c in continuities) stage.Cross(c.From, c.To, toScene);
                stage.ShowScene(toScene);
                Set(SeeThroughId, 1);
                yield return new WaitForSeconds(fullHold);
            }

            if (reduced) yield return Fade(AlphaId, peak, 0, reducedOut);
            else
            {
                block.SetVector(TargetId, Toward(continuities.Count > 0 ? continuities[0].To : null));
                yield return Fade(HoleId, 0, Across, fullOut);
            }
            curtain.enabled = false;
            routine = null;
            Finished?.Invoke(token);
        }

        /// <summary>From the visitor's head toward a staged light, or toward the fire when there is none.</summary>
        Vector4 Toward(string objectId)
        {
            var o = objectId != null ? stage.Find(objectId) : null;
            var at = o != null && o.isActiveAndEnabled ? o.transform.position : o != null ? o.HomePosition : stage.FireTop;
            var d = at - curtain.transform.position;
            return d.sqrMagnitude > 1e-6f ? (Vector4)d.normalized : (Vector4)Vector3.forward;
        }

        IEnumerator Fade(int property, float from, float to, float seconds)
        {
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                var x = Mathf.Clamp01(t / seconds);
                Set(property, Mathf.Lerp(from, to, x * x * (3f - 2f * x)));
                yield return null;
            }
            Set(property, to);
        }

        void Set(int property, float value)
        {
            block.SetFloat(property, value);
            curtain.SetPropertyBlock(block);
        }
    }
}
