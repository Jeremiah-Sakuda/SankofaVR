using System;
using Sankofa.Runtime.Input;
using UnityEngine;

namespace Sankofa.Runtime.Stage
{
    /// <summary>
    /// A staged story object, matched to the plan by <see cref="Id"/>. Presentation only: the flow decides when it
    /// is revealed, illuminated, offered, placed, withdrawn or carried, and when it may be touched.
    /// </summary>
    public sealed class StageObject : MonoBehaviour
    {
        [SerializeField] string objectId;
        [SerializeField] string sceneId;
        [SerializeField] bool visibleAtStart;
        [Tooltip("Grow from nothing when revealed. Off for shapes of light, which fade in place instead.")]
        [SerializeField] bool scaleWithVisibility = true;
        [SerializeField] Renderer[] surfaces;
        [SerializeField] Renderer glow;
        [SerializeField] float glowIntensity = 1f;
        [SerializeField] bool emitsLight;
        [SerializeField] Color lightColor = new Color(1f, 0.68f, 0.38f);
        [SerializeField] float lightIntensity = 1.2f;
        [Tooltip("Where a hand touches it when the visitor is invited to; the object itself if empty.")]
        [SerializeField] Transform touchPoint;
        [SerializeField] float touchRadius = 0.09f;
        [Tooltip("Approximate radius for noticing by head direction; 0 never counts as looked at.")]
        [SerializeField] float attentionRadius;
        [Tooltip("Where something placed on it comes to rest; just above the object if empty.")]
        [SerializeField] Transform placePoint;

        static readonly int HighlightId = Shader.PropertyToID("_Highlight");
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        bool initialised;
        Vector3 startLocalPosition;
        Quaternion startLocalRotation;
        Vector3 startScale;
        MaterialPropertyBlock block;
        float shown, shownTarget;
        float highlight, highlightTarget;
        float pulse;
        Vector3? moveTarget;
        float smoothTime = 0.3f;
        Vector3 velocity;
        Func<Vector3?> follow;
        float fadeAfter = -1;

        public string Id => objectId;
        public string Scene => sceneId;
        public bool Visible => shownTarget > 0;
        public bool EmitsLight => emitsLight;
        public Color LightColor => lightColor;
        public float LightIntensity => lightIntensity * Mathf.Clamp01(shown) * (1f + highlight * 0.3f);
        /// <summary>0..1 local feedback while a hand approaches to receive or touch it.</summary>
        public float Feedback { get; set; }
        /// <summary>The hand carrying it, if any; carried objects cross into their counterparts in that hand.</summary>
        public HandSource CarriedBy { get; private set; }
        public Vector3 TouchPosition => touchPoint != null ? touchPoint.position : transform.position;
        public float TouchRadius => touchRadius;
        public float AttentionRadius => attentionRadius;
        public Vector3 PlacePosition => placePoint != null ? placePoint.position : transform.position + Vector3.up * 0.05f;
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
            Release();
            moveTarget = null;
            velocity = Vector3.zero;
            transform.localPosition = startLocalPosition;
            transform.localRotation = startLocalRotation;
            shown = shownTarget = visibleAtStart ? 1 : 0;
            highlight = highlightTarget = 0;
            pulse = 0;
            Feedback = 0;
            fadeAfter = -1;
            Apply();
        }

        public void Reveal()
        {
            Initialise();
            shownTarget = 1;
            fadeAfter = -1;
        }

        public void Hide()
        {
            Initialise();
            Release();
            shown = shownTarget = 0;
            fadeAfter = -1;
            Apply();
        }

        /// <summary>Appears at once at a world position, for an element carried across a transition.</summary>
        public void ShowAt(Vector3 world)
        {
            Initialise();
            Release();
            moveTarget = null;
            velocity = Vector3.zero;
            transform.position = world;
            shown = shownTarget = 1;
            fadeAfter = -1;
            Apply();
        }

        /// <summary>Fades in where it stands, at a world position: a keepsake waiting on the hearth stone.</summary>
        public void AppearAt(Vector3 world)
        {
            Initialise();
            Release();
            moveTarget = null;
            velocity = Vector3.zero;
            transform.position = world;
            shown = 0;
            shownTarget = 1;
            fadeAfter = -1;
            Apply();
        }

        public void Illuminate()
        {
            Initialise();
            shownTarget = 1;
            highlightTarget = 1;
        }

        /// <summary>A brief brightening: the fire noticed the visitor looking at it.</summary>
        public void Pulse() => pulse = 1f;

        public void MoveTo(Vector3 world, float seconds)
        {
            Initialise();
            Release();
            moveTarget = world;
            smoothTime = Mathf.Max(0.05f, seconds / 3f);
        }

        /// <summary>Carried in a hand: follows its anchor, holding position while the anchor is unavailable.</summary>
        public void Carry(HandSource hand, Func<Vector3?> anchor)
        {
            Initialise();
            follow = anchor;
            CarriedBy = hand;
            moveTarget = null;
            smoothTime = 0.06f;
        }

        /// <summary>Goes back into the fire (or wherever it came from) and fades out on the way.</summary>
        public void Withdraw(Vector3 into, float seconds = 1.2f)
        {
            MoveTo(into, seconds);
            fadeAfter = seconds * 0.6f;
        }

        void Release()
        {
            follow = null;
            CarriedBy = null;
        }

        void Update()
        {
            var dt = Time.deltaTime;
            if (fadeAfter >= 0 && (fadeAfter -= dt) < 0) shownTarget = 0;
            shown = Mathf.MoveTowards(shown, shownTarget, dt / 0.6f);
            highlight = Mathf.MoveTowards(highlight, highlightTarget, dt / 1.2f);
            pulse = Mathf.MoveTowards(pulse, 0, dt / 1.1f);
            var goal = follow?.Invoke() ?? moveTarget;
            if (goal.HasValue) transform.position = Vector3.SmoothDamp(transform.position, goal.Value, ref velocity, smoothTime, Mathf.Infinity, dt);
            Apply();
        }

        void Apply()
        {
            var eased = 1f - (1f - shown) * (1f - shown);
            transform.localScale = scaleWithVisibility ? startScale * Mathf.Max(0.0001f, eased) : startScale;
            var visible = shown > 0.001f;
            var lift = Mathf.Max(highlight, pulse * 0.45f + Feedback * 0.35f);
            foreach (var s in surfaces)
            {
                if (s == null) continue;
                s.enabled = visible;
                s.GetPropertyBlock(block);
                block.SetFloat(HighlightId, lift * shown);
                s.SetPropertyBlock(block);
            }
            if (glow != null)
            {
                glow.enabled = visible;
                var flicker = 1f + (Mathf.PerlinNoise(Time.time * 3.1f, objectId.Length) - 0.5f) * 0.25f;
                glow.GetPropertyBlock(block);
                block.SetFloat(IntensityId, glowIntensity * shown * flicker * (1f + Feedback * 1.6f + highlight * 0.5f + pulse * 0.6f));
                glow.SetPropertyBlock(block);
            }
        }
    }
}
