using System;
using Sankofa.Core.Personal;
using Sankofa.Flow;
using UnityEngine;

namespace Sankofa.Runtime.Personal
{
    /// <summary>
    /// Makes the existing kept strand visible beside the river after an actual save. This is a view of the
    /// application's strand, never another store, an automatic save, or a claim that an unsaved visit was kept.
    /// </summary>
    [DefaultExecutionOrder(750)]
    public sealed class RiverKeepsakePresentation : MonoBehaviour
    {
        [SerializeField] PersonalStoryApp app;
        [SerializeField] RiverThresholdPrototype river;
        [SerializeField] Transform sourceStrand, displayAnchor;
        [SerializeField] Material cordMaterial;
        Transform display;
        Renderer[] sources = Array.Empty<Renderer>(), copies = Array.Empty<Renderer>();
        PersonalStoryDocument savedDocument;
        int savedVersion = -1;
        float reveal = 1;
        Vector3 centredPosition;
        bool subscribed;

        public bool Visible => display != null && display.gameObject.activeInHierarchy;
        public float RevealProgress => reveal;
        public int ShownBeadCount { get; private set; }
        /// <summary>The small neutral beads are unfilled positions, not additional kept perspectives.</summary>
        public int ShownSlotCount => Visible ? copies.Length : 0;
        public Transform DisplayRoot => display;
        public const float RevealSeconds = 1.2f;

        /// <summary>The scene supplies placement; the original strand supplies its approved meshes and materials.</summary>
        public void Configure(PersonalStoryApp storyApp, RiverThresholdPrototype presentation, Transform strand, Transform anchor, Material threadMaterial = null)
        {
            app = storyApp; river = presentation; sourceStrand = strand; displayAnchor = anchor;
            cordMaterial = threadMaterial;
            if (Application.isPlaying) Initialise();
        }

        void Start() => Initialise();

        void Initialise()
        {
            if (subscribed || app == null || river == null || sourceStrand == null || displayAnchor == null) return;
            sources = sourceStrand.GetComponentsInChildren<Renderer>(true);
            display = Instantiate(sourceStrand, displayAnchor, false);
            display.name = "Kept telling beside the river";
            display.localRotation = Quaternion.identity;
            display.localScale = Vector3.one;
            copies = display.GetComponentsInChildren<Renderer>(true);
            // The same beads form a shallow hanging arc on the page's XY plane, rather than inheriting the
            // horizontal theatre rim's positions. The six slots make it read as one object, even with one bead kept.
            var positions = new Vector3[copies.Length];
            for (var i = 0; i < copies.Length; i++)
            {
                var along = copies.Length < 2 ? 0 : 2f * i / (copies.Length - 1) - 1;
                positions[i] = new Vector3((i - (copies.Length - 1) * .5f) * .038f, -.014f * (1 - along * along), 0);
                copies[i].transform.localPosition = positions[i];
            }
            centredPosition = Vector3.zero;
            display.localPosition = centredPosition;
            foreach (var collider in display.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            if (positions.Length > 0)
            {
                var material = cordMaterial != null ? cordMaterial : sources[0].sharedMaterial;
                var previous = positions[0] + new Vector3(-.018f, .004f, 0);
                for (var i = 0; i <= positions.Length; i++)
                {
                    var next = i < positions.Length ? positions[i] : positions[positions.Length - 1] + new Vector3(.018f, .004f, 0);
                    AddCord(previous, next, i, material);
                    previous = next;
                }
            }
            display.gameObject.SetActive(false);
            app.StoryKept += Kept;
            subscribed = true;
        }

        void AddCord(Vector3 from, Vector3 to, int index, Material material)
        {
            var cord = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cord.name = "Strand cord " + (index + 1);
            cord.transform.SetParent(display, false);
            var difference = to - from;
            cord.transform.localPosition = (from + to) * .5f;
            cord.transform.localRotation = Quaternion.FromToRotation(Vector3.up, difference.normalized);
            cord.transform.localScale = new Vector3(.0036f, difference.magnitude * .5f, .0036f);
            cord.GetComponent<Collider>().enabled = false;
            var renderer = cord.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        void Kept()
        {
            savedDocument = app.Session.Story;
            savedVersion = app.Session.RequestVersion;
            reveal = app.ReducedMotionEnabled ? 1 : 0;
        }

        void LateUpdate()
        {
            if (display == null || app == null || river == null) return;
            var savedHere = savedVersion == app.Session.RequestVersion && ReferenceEquals(savedDocument, app.Session.Story);
            var show = river.RiverVisible && app.Session.State == PersonalStoryState.Closing
                && (savedHere || app.Session.IsReplay) && app.View.StrandKept > 0;
            display.gameObject.SetActive(show);
            ShownBeadCount = show ? Mathf.Min(app.View.StrandKept, copies.Length) : 0;
            if (!show) return;
            // Reopening a kept telling never performs the save acknowledgement again.
            if (!savedHere || app.ReducedMotionEnabled) reveal = 1;
            else if (!app.Session.Paused) reveal = Mathf.Min(1, reveal + Time.deltaTime / RevealSeconds);
            var eased = reveal * reveal * (3 - 2 * reveal);
            display.localPosition = centredPosition + Vector3.down * (.012f * (1 - eased));
            for (var i = 0; i < copies.Length; i++)
            {
                copies[i].enabled = true;
                if (i >= sources.Length) continue;
                copies[i].sharedMaterial = sources[i].sharedMaterial;
                copies[i].transform.localScale = sources[i].transform.localScale;
            }
        }

        void OnDisable() { if (display != null) display.gameObject.SetActive(false); }
        void OnDestroy()
        {
            if (subscribed && app != null) app.StoryKept -= Kept;
            if (display != null) Destroy(display.gameObject);
        }
    }
}
