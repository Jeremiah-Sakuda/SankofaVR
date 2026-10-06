using System.Collections.Generic;
using UnityEngine;

namespace Sankofa.Runtime.Stage
{
    /// <summary>
    /// How a person or animal of the story moves: walking in along a path when revealed, going to stand or kneel
    /// where something is used, going back to its own place, and walking away when withdrawn. Presentation only: the
    /// story decides when, through reveal, move and withdraw. The figure's light shader folds the body into a kneel
    /// from one value, so a single silhouette serves both postures.
    /// </summary>
    [RequireComponent(typeof(StageObject))]
    public sealed class FigureMotion : MonoBehaviour
    {
        [Tooltip("Where the figure walks in from, through to just before its own place, in its parent's space; empty to appear in place.")]
        [SerializeField] Vector3[] entryPath = new Vector3[0];
        [SerializeField] Renderer body;
        [SerializeField] float height = 1.6f;
        [SerializeField] float speed = 0.7f;
        [Tooltip("Height of the rise and fall of each step.")]
        [SerializeField] float bob = 0.016f;
        [SerializeField] float stepLength = 0.55f;
        [Tooltip("Seconds to wait after being revealed before setting off, so a companion can follow behind.")]
        [SerializeField] float delay;

        static readonly int KneelId = Shader.PropertyToID("_Kneel");
        static readonly int HeightId = Shader.PropertyToID("_Height");

        readonly List<Vector3> route = new List<Vector3>();
        StageObject staged;
        MaterialPropertyBlock block;
        bool initialised;
        Vector3 homeLocal;
        Quaternion homeRotation;
        Vector3 basePosition;
        Quaternion? finalRotation;
        int next;
        float wait;
        float travelled;
        float kneel, kneelTarget;
        bool kneelOnArrival;

        public bool Walking => next < route.Count;
        public float Kneel => kneel;

        void Awake() => Initialise();

        void Initialise()
        {
            if (initialised) return;
            initialised = true;
            staged = GetComponent<StageObject>();
            block = new MaterialPropertyBlock();
            homeLocal = transform.localPosition;
            homeRotation = transform.localRotation;
            basePosition = transform.position;
        }

        Vector3 InParent(Vector3 local) => transform.parent != null ? transform.parent.TransformPoint(local) : local;
        Quaternion HomeRotation => transform.parent != null ? transform.parent.rotation * homeRotation : homeRotation;

        /// <summary>Back to its own place, standing, as the scene was built.</summary>
        public void ResetState()
        {
            Initialise();
            route.Clear();
            next = 0;
            wait = 0;
            kneel = kneelTarget = 0;
            kneelOnArrival = false;
            finalRotation = null;
            transform.localPosition = homeLocal;
            transform.localRotation = homeRotation;
            basePosition = transform.position;
            Apply();
        }

        /// <summary>Revealed: appears at the start of its path and walks to its own place.</summary>
        public void Arrive()
        {
            Initialise();
            ResetState();
            if (entryPath.Length == 0) return;
            for (var i = 1; i < entryPath.Length; i++) route.Add(InParent(entryPath[i]));
            route.Add(InParent(homeLocal));
            basePosition = InParent(entryPath[0]);
            transform.position = basePosition;
            var first = route[0] - basePosition;
            first.y = 0;
            if (first.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(first);
            finalRotation = HomeRotation;
            wait = delay;
        }

        /// <summary>Goes to a place, rising first if kneeling, then stands or kneels there facing what it came to.</summary>
        public void GoTo(Vector3 place, Vector3 lookAt, bool kneelThere)
        {
            Initialise();
            route.Clear();
            next = 0;
            wait = 0;
            route.Add(place);
            kneelTarget = 0;
            kneelOnArrival = kneelThere;
            var facing = lookAt - place;
            facing.y = 0;
            finalRotation = facing.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(facing) : HomeRotation;
        }

        /// <summary>Back to its own place, standing and facing the visitor as it did.</summary>
        public void GoHome()
        {
            Initialise();
            GoTo(InParent(homeLocal), Vector3.zero, false);
            finalRotation = HomeRotation;
        }

        /// <summary>Rises and walks back the way it came, fading as it goes; without a path, fades where it stands.</summary>
        public void Leave()
        {
            Initialise();
            route.Clear();
            next = 0;
            wait = 0;
            kneelTarget = 0;
            kneelOnArrival = false;
            finalRotation = null;
            var length = 0f;
            var from = basePosition;
            for (var i = entryPath.Length - 1; i >= 0; i--)
            {
                var p = InParent(entryPath[i]);
                length += Vector3.Distance(from, p);
                from = p;
                route.Add(p);
            }
            // Mostly gone by the time it reaches the edge of the scene.
            staged.FadeAway(route.Count == 0 ? 0.2f : 0.9f + length / speed * 0.45f);
        }

        void Update()
        {
            var dt = Time.deltaTime;
            var walking = false;
            if (wait > 0) wait -= dt;
            else if (next < route.Count)
            {
                if (kneel > 0.02f) kneelTarget = 0;
                else
                {
                    walking = true;
                    var to = route[next] - basePosition;
                    var distance = to.magnitude;
                    var step = speed * dt;
                    if (distance <= step)
                    {
                        basePosition = route[next];
                        next++;
                        if (next >= route.Count) kneelTarget = kneelOnArrival ? 1 : 0;
                    }
                    else basePosition += to / distance * step;
                    travelled += Mathf.Min(step, distance);
                    to.y = 0;
                    if (to.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), dt * 5f);
                }
            }
            else if (finalRotation.HasValue)
                transform.rotation = Quaternion.Slerp(transform.rotation, finalRotation.Value, dt * 3f);

            kneel = Mathf.MoveTowards(kneel, kneelTarget, dt / 0.9f);
            var lift = walking ? bob * Mathf.Abs(Mathf.Sin(travelled / stepLength * Mathf.PI)) : 0f;
            transform.position = basePosition + Vector3.up * lift;
            Apply();
        }

        void Apply()
        {
            if (body == null) return;
            body.GetPropertyBlock(block);
            block.SetFloat(KneelId, Mathf.SmoothStep(0f, 1f, kneel));
            block.SetFloat(HeightId, height);
            body.SetPropertyBlock(block);
        }
    }
}
