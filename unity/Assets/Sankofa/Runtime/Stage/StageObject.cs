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
        [Tooltip("Where a figure stands or kneels to use it, one per side it can be approached from.")]
        [SerializeField] Transform[] approachPoints = new Transform[0];
        [Tooltip("Whether someone using it kneels there (a hide on a low stone) or stands.")]
        [SerializeField] bool approachKneels;
        [Tooltip("The person of the story who carries or gives this, and gestures when it is offered, shown or set down.")]
        [SerializeField] FigureMotion holder;

        static readonly int HighlightId = Shader.PropertyToID("_Highlight");
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        bool initialised;
        Vector3 startLocalPosition;
        Transform startParent;
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
        public FigureMotion Holder => holder;
        public Vector3 TouchPosition => touchPoint != null ? touchPoint.position : transform.position;
        public float TouchRadius => touchRadius;
        public float AttentionRadius => attentionRadius;
        public Vector3 PlacePosition => placePoint != null ? placePoint.position : transform.position + Vector3.up * 0.05f;
        /// <summary>
        /// Where a figure coming from <paramref name="from"/> stands or kneels to use this: the nearest of its approach
        /// points, so two people coming from either side take a side each; without one, half a metre short of it.
        /// </summary>
        public (Vector3 Place, bool Kneel) ApproachFrom(Vector3 from)
        {
            Transform best = null;
            var bestDistance = float.MaxValue;
            foreach (var p in approachPoints)
            {
                if (p == null) continue;
                var d = (p.position - from).sqrMagnitude;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = p;
                }
            }
            if (best != null) return (best.position, approachKneels);
            var toward = transform.position - from;
            toward.y = 0;
            var place = transform.position - toward.normalized * Mathf.Min(0.5f, toward.magnitude);
            place.y = from.y;
            return (place, approachKneels);
        }

        public Vector3 HomePosition
        {
            get
            {
                Initialise();
                return startParent != null ? startParent.TransformPoint(startLocalPosition) : startLocalPosition;
            }
        }

        void Awake() => Initialise();

        void Initialise()
        {
            if (initialised) return;
            initialised = true;
            block = new MaterialPropertyBlock();
            startLocalPosition = transform.localPosition;
            startParent = transform.parent;
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
            if (transform.parent != startParent) transform.SetParent(startParent, false);
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
        /// <summary>
        /// Leaves the hand of the person who carried it (its holder's anchor) for the scene itself, keeping where it
        /// is, so it no longer moves with her once it is offered, set down or taken.
        /// </summary>
        void LeaveHolder()
        {
            if (holder == null || transform.parent == null || transform.parent == holder.transform.parent) return;
            transform.SetParent(holder.transform.parent, true);
        }

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
            LeaveHolder();
            Initialise();
            Release();
            moveTarget = world;
            smoothTime = Mathf.Max(0.05f, seconds / 3f);
        }

        /// <summary>Carried in a hand: follows its anchor, holding position while the anchor is unavailable.</summary>
        public void Carry(HandSource hand, Func<Vector3?> anchor)
        {
            LeaveHolder();
            Initialise();
            follow = anchor;
            CarriedBy = hand;
            moveTarget = null;
            smoothTime = 0.06f;
        }

        /// <summary>Fades out after a while, wherever it is going.</summary>
        public void FadeAway(float after)
        {
            Initialise();
            fadeAfter = Mathf.Max(0f, after);
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
