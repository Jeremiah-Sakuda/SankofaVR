using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sankofa.Flow;

namespace Sankofa.Runtime.Stage
{
    /// <summary>One canonical framed work becomes a world around the stationary seat, then returns to its frame.</summary>
    public sealed class LivingGallery : MonoBehaviour
    {
        [SerializeField] Transform painting;
        [SerializeField] Renderer canvas;
        [SerializeField] Renderer[] frame;
        [SerializeField] StoryStage stage;
        [SerializeField] GalleryAtmosphere atmosphere;
        [SerializeField] string defaultRoute = "era.1850s";
        [SerializeField] string alternateRoute = "era.1850s.remembered";
        MaterialPropertyBlock block;
        Vector3 scale;
        public string DefaultRoute => defaultRoute;
        public string OtherRoute(string previous) => previous == defaultRoute ? alternateRoute : defaultRoute;
        public bool Framed { get; private set; } = true;
        public float Expansion { get; private set; }
        void Awake() { scale = painting.localScale; block = new MaterialPropertyBlock(); }
        public void ResetView()
        {
            Framed = true;
            Apply(0, 1);
        }
        public void SettleView(string scene)
        {
            Framed = stage.IsHearth(scene);
            Apply(0, Framed ? 1 : 0);
        }
        void Apply(float expansion, float opacity)
        {
            if (block == null) block = new MaterialPropertyBlock();
            if (scale == Vector3.zero) scale = painting.localScale;
            Expansion = expansion;
            if(atmosphere!=null)atmosphere.SetPresence((1-expansion)*opacity);
            painting.localScale = scale * Mathf.Lerp(1, 5.5f, expansion);
            canvas.enabled = opacity > 0;
            canvas.GetPropertyBlock(block); block.SetFloat("_Opacity", opacity); canvas.SetPropertyBlock(block);
            foreach (var r in frame) r.enabled = opacity > 0 && expansion < 0.85f;
        }
        public IEnumerator Transition(string to, IReadOnlyList<Continuity> continuity, bool reduced)
        {
            var home = stage.IsHearth(to);
            if (home)
            {
                yield return Animate(reduced ? 0 : 1, reduced ? 0 : 1, 0, 1, reduced ? 0.65f : 1.2f);
                foreach (var c in continuity) stage.Cross(c.From, c.To, to);
                stage.ShowScene(to);
                yield return Animate(reduced ? 0 : 1, 0, 1, 1, reduced ? 0.1f : 1.8f);
            }
            else
            {
                yield return Animate(0, reduced ? 0 : 1, 1, 1, reduced ? 0.65f : 1.8f);
                foreach (var c in continuity) stage.Cross(c.From, c.To, to);
                stage.ShowScene(to);
                yield return Animate(reduced ? 0 : 1, reduced ? 0 : 1, 1, 0, reduced ? 0.65f : 1.5f);
            }
            Framed = home;
            if (home) Apply(0, 1); else Apply(0, 0);
        }
        IEnumerator Animate(float from, float to, float alphaFrom, float alphaTo, float seconds)
        {
            for (var t = 0f; t < seconds; t += StageClock.DeltaTime)
            {
                var x = Mathf.SmoothStep(0, 1, t / seconds);
                Apply(Mathf.Lerp(from, to, x), Mathf.Lerp(alphaFrom, alphaTo, x));
                yield return null;
            }
            Apply(to, alphaTo);
        }
    }
}
