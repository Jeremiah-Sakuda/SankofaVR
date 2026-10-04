using System;
using UnityEngine;

namespace Sankofa.Runtime.Stage
{
    /// <summary>
    /// A staged story object, matched to the episode by <see cref="Id"/>. Presentation only: the flow decides
    /// when it is revealed, illuminated, offered, placed or carried.
    /// </summary>
    public sealed class StageObject : MonoBehaviour
    {
        [SerializeField] string objectId;
        [SerializeField] string sceneId;
        [SerializeField] bool visibleAtStart;
        [SerializeField] Renderer[] surfaces;
        [SerializeField] Renderer glow;
        [SerializeField] float glowIntensity = 1f;
        [SerializeField] bool emitsLight;
        [SerializeField] Color lightColor = new Color(1f, 0.68f, 0.38f);
        [SerializeField] float lightIntensity = 1.2f;

        static readonly int HighlightId = Shader.PropertyToID("_Highlight");
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        bool initialised;
        Vector3 startLocalPosition;
        Quaternion startLocalRotation;
        Vector3 startScale;
        MaterialPropertyBlock block;
        float shown, shownTarget;
        float highlight, highlightTarget;
        Vector3? moveTarget;
        float smoothTime = 0.3f;
        Vector3 velocity;
        Func<Vector3?> follow;

        public string Id => objectId;
        public string Scene => sceneId;
        public bool Visible => shownTarget > 0;
        public bool EmitsLight => emitsLight;
        public Color LightColor => lightColor;
        public float LightIntensity => lightIntensity * Mathf.Clamp01(shown) * (1f + highlight * 0.3f);
        public float Feedback { get; set; }
        public Vector3 HomePosition
        {
            get
            {
                Initialise();
                return transform.parent != null ? transform.parent.TransformPoint(startLocalPosition) : startLocalPosition;
            }
        }

        void Awake() => Initialise();

        void Initialise()
        {
            if (initialised) return;
            initialised = true;
            block = new MaterialPropertyBlock();
            startLocalPosition = transform.localPosition;
            startLocalRotation = transform.localRotation;
            startScale = transform.localScale;
            shown = shownTarget = visibleAtStart ? 1 : 0;
            Apply();
        }

        public void ResetToStart()
        {
            Initialise();
            follow = null;
            moveTarget = null;
            velocity = Vector3.zero;
            transform.localPosition = startLocalPosition;
            transform.localRotation = startLocalRotation;
            shown = shownTarget = visibleAtStart ? 1 : 0;
            highlight = highlightTarget = 0;
            Feedback = 0;
            Apply();
        }

        public void Reveal()
        {
            Initialise();
            shownTarget = 1;
        }

        public void Hide()
        {
            Initialise();
            follow = null;
            shown = shownTarget = 0;
            Apply();
        }

        /// <summary>Appears at once at a world position, for an element carried across a transition.</summary>
        public void ShowAt(Vector3 world)
        {
            Initialise();
            follow = null;
            moveTarget = null;
            velocity = Vector3.zero;
            transform.position = world;
            shown = shownTarget = 1;
            Apply();
        }

        public void Illuminate()
        {
            Initialise();
            shownTarget = 1;
            highlightTarget = 1;
        }

        public void MoveTo(Vector3 world, float seconds)
        {
            Initialise();
            follow = null;
            moveTarget = world;
            smoothTime = Mathf.Max(0.05f, seconds / 3f);
        }

        /// <summary>Follows a moving anchor such as an open palm; holds position while the anchor is unavailable.</summary>
        public void Follow(Func<Vector3?> anchor)
        {
            Initialise();
            follow = anchor;
            moveTarget = null;
            smoothTime = 0.06f;
        }

        void Update()
        {
            var dt = Time.deltaTime;
            shown = Mathf.MoveTowards(shown, shownTarget, dt / 0.6f);
            highlight = Mathf.MoveTowards(highlight, highlightTarget, dt / 1.2f);
            var goal = follow?.Invoke() ?? moveTarget;
            if (goal.HasValue) transform.position = Vector3.SmoothDamp(transform.position, goal.Value, ref velocity, smoothTime, Mathf.Infinity, dt);
            Apply();
        }

        void Apply()
        {
            var eased = 1f - (1f - shown) * (1f - shown);
            transform.localScale = startScale * Mathf.Max(0.0001f, eased);
            var visible = shown > 0.001f;
            foreach (var s in surfaces)
            {
                if (s == null) continue;
                s.enabled = visible;
                s.GetPropertyBlock(block);
                block.SetFloat(HighlightId, highlight * shown);
                s.SetPropertyBlock(block);
            }
            if (glow != null)
            {
                glow.enabled = visible;
                var flicker = 1f + (Mathf.PerlinNoise(Time.time * 3.1f, objectId.Length) - 0.5f) * 0.25f;
                glow.GetPropertyBlock(block);
                block.SetFloat(IntensityId, glowIntensity * shown * flicker * (1f + Feedback * 1.6f + highlight * 0.5f));
                glow.SetPropertyBlock(block);
            }
        }
    }
}
