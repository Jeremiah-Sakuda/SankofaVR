using System;
using System.Collections.Generic;
using System.Linq;
using Sankofa.Interaction;
using TMPro;
using UnityEngine;

namespace Sankofa.Runtime.UI
{
    /// <summary>
    /// A hand-operated button panel. A press is a fingertip reaching the button face from in
    /// front, inside its rectangle; it must withdraw before it can press again. While a finger
    /// hovers near the panel, <see cref="FingerNear"/> is true so other hand input can yield to it.
    /// A pointer (a hand's pinch or a controller's trigger, from a distance) can also hover and press
    /// a button through <see cref="TryHit"/>, <see cref="PointAt"/> and <see cref="PressFromPointer"/>.
    /// </summary>
    // OVRSkeleton updates first, then HandSource (100) validates the current sample.
    // Menu gestures must not consume last frame's validity with this frame's position.
    [DefaultExecutionOrder(200)]
    public sealed class PokePanel : MonoBehaviour
    {
        sealed class Button
        {
            public string Id;
            public GameObject Root;
            public Renderer Face;
            public TextMeshPro Label;
            public Vector2 Size;
            public readonly Dictionary<int, PokeGesture> Gestures = new Dictionary<int, PokeGesture>();
            public float Flash;
            public float Glow;
            public bool Hover;
        }

        [SerializeField] Material buttonMaterial;
        [SerializeField] Material plaqueBodyMaterial;
        [SerializeField] float plaqueDepth;
        [SerializeField] TMP_FontAsset labelFont;
        [SerializeField] Material labelMaterial;
        [SerializeField] Color labelColor = Color.white;
        [Tooltip("TextMeshPro world size for labels; 0.13 is a line about 1.5 cm tall.")]
        [SerializeField] float labelSize = 0.13f;
        [Tooltip("Emission per unit of button colour, for materials lit by the scene (0 for unlit ones).")]
        [SerializeField] float glow;
        [SerializeField] Vector2 buttonSize = new Vector2(0.13f, 0.05f);
        [SerializeField] float gap = 0.012f;
        [SerializeField] int columns = 2;
        [SerializeField] float pressDepth = 0.0f;
        [SerializeField] float releaseDepth = 0.025f;
        [SerializeField] float minPressInterval = 0.3f;
        [SerializeField] float hoverDepth = 0.08f;
        [SerializeField] Color idle = new Color(0.12f, 0.14f, 0.22f, 0.9f);
        [SerializeField] Color hover = new Color(0.25f, 0.3f, 0.45f, 0.95f);
        [SerializeField] Color pressed = new Color(0.95f, 0.7f, 0.3f, 1f);

        readonly List<Button> buttons = new List<Button>();
        readonly List<Func<Vector3?>> fingertips = new List<Func<Vector3?>>();
        MaterialPropertyBlock block;
        string pointed;
        string pointerPress;
        float lastPointerPress = float.NegativeInfinity;
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int EmissionId = Shader.PropertyToID("_Emission");

        public event Action<string> Pressed;
        public bool FingerNear { get; private set; }
        public int Revision { get; private set; }

        void Awake()
        {
            block = new MaterialPropertyBlock();
        }

        public void AddFingertip(Func<Vector3?> fingertip) => fingertips.Add(fingertip);

        bool hidden;
        bool primaryLayout;

        /// <summary>
        /// Set aside for a while, keeping its buttons: nothing shows and nothing can be pressed. The questions step
        /// aside while the visitor is asked to touch or receive, so no button floats over what they reach for.
        /// </summary>
        public bool Hidden
        {
            get => hidden;
            set
            {
                if (hidden == value) return;
                hidden = value;
                Revision++;
                pointerPress = pointed = null;
                foreach (var b in buttons) b.Root.SetActive(!value);
                foreach (var g in buttons.SelectMany(b => b.Gestures.Values)) g.Lost();
            }
        }

        /// <summary>Whether a fingertip at this point is at or just in front of one of the buttons, about to press.</summary>
        public bool Near(Vector3 point)
        {
            if (!isActiveAndEnabled || hidden) return false;
            foreach (var b in buttons)
            {
                var local = b.Root.transform.InverseTransformPoint(point);
                var depth = -local.z;
                if (Mathf.Abs(local.x) <= b.Size.x / 2f && Mathf.Abs(local.y) <= b.Size.y / 2f && depth < hoverDepth && depth > -0.05f)
                    return true;
            }
            return false;
        }

        /// <summary>The nearest button a ray meets from in front, within reach, and how far along the ray.</summary>
        public bool TryHit(Ray ray, float reach, out float distance, out string id)
        {
            distance = float.MaxValue;
            id = null;
            if (!isActiveAndEnabled || hidden) return false;
            foreach (var b in buttons)
            {
                var face = b.Root.transform;
                // Buttons are seen from their −Z side, so a ray from the visitor travels along +Z into them.
                if (Vector3.Dot(ray.direction, face.forward) <= 0.05f) continue;
                if (!new Plane(face.forward, face.position).Raycast(ray, out var d) || d > reach || d >= distance) continue;
                var local = face.InverseTransformPoint(ray.GetPoint(d));
                if (Mathf.Abs(local.x) > b.Size.x / 2f || Mathf.Abs(local.y) > b.Size.y / 2f) continue;
                distance = d;
                id = b.Id;
            }
            return id != null;
        }

        /// <summary>A pointer rests on this button for the current frame.</summary>
        public void PointAt(string id) => pointed = id;

        /// <summary>A pointer completed a select on this button; dispatched with this frame's presses.</summary>
        public void PressFromPointer(string id)
        {
            if (Time.unscaledTime - lastPointerPress < minPressInterval) return;
            lastPointerPress = Time.unscaledTime;
            pointerPress = id;
        }

        /// <summary>The ids of the buttons shown, in order.</summary>
        public IEnumerable<string> Ids
        {
            get
            {
                foreach (var b in buttons) yield return b.Id;
            }
        }

        /// <summary>Draws the eye to one button for a moment, as when the fire notices what the visitor looks at.</summary>
        public void Pulse(string id)
        {
            foreach (var b in buttons)
                if (b.Id == id) b.Glow = 1f;
        }

        /// <summary>Replaces the buttons. Order is kept; ids are returned in <see cref="Pressed"/>.
        /// An identical list leaves the existing buttons alone so a press in progress is not lost.</summary>
        public void Show(IReadOnlyList<(string Id, string Label)> items, bool primaryFirst = false)
        {
            if (items.Count == buttons.Count && primaryLayout == primaryFirst)
            {
                var same = true;
                for (var i = 0; i < items.Count && same; i++)
                    same = buttons[i].Id == items[i].Id && buttons[i].Label.text == items[i].Label;
                if (same) return;
            }
            Revision++;
            pointerPress = pointed = null;
            foreach (var b in buttons) Destroy(b.Root);
            buttons.Clear();
            primaryLayout = primaryFirst;
            for (var i = 0; i < items.Count; i++)
            {
                var first = primaryFirst && i == 0;
                var index = primaryFirst ? i - 1 : i;
                var row = first ? 0 : index / columns + (primaryFirst ? 1 : 0);
                var col = first ? 0 : index % columns;
                var x = (col - (columns - 1) / 2f) * (buttonSize.x + gap);
                if (first) x = 0;
                var y = -row * (buttonSize.y + gap);
                var size = first ? new Vector2(buttonSize.x * columns + gap * (columns - 1), buttonSize.y) : buttonSize;
                buttons.Add(CreateButton(items[i].Id, items[i].Label, new Vector3(x, y, 0), size));
                buttons[buttons.Count - 1].Root.SetActive(!hidden);
            }
        }

        Button CreateButton(string id, string label, Vector3 localPosition, Vector2 size)
        {
            var root = new GameObject("Button " + id);
            root.transform.SetParent(transform, false);
            root.transform.localPosition = localPosition;

            var face = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(face.GetComponent<Collider>());
            face.name = "Face";
            face.transform.SetParent(root.transform, false);
            face.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = face.GetComponent<Renderer>();
            if (buttonMaterial != null) renderer.sharedMaterial = buttonMaterial;
            if(plaqueDepth>0&&plaqueBodyMaterial!=null)
            {
                // Depth lives behind the existing face. Its hit plane and fingertip release distance stay stable.
                var body=GameObject.CreatePrimitive(PrimitiveType.Cube);Destroy(body.GetComponent<Collider>());
                body.name="Plaque body";body.transform.SetParent(root.transform,false);
                body.transform.localPosition=new Vector3(0,0,plaqueDepth/2+0.0005f);
                body.transform.localScale=new Vector3(size.x+0.003f,size.y+0.003f,plaqueDepth);
                var surface=body.GetComponent<Renderer>();surface.sharedMaterial=plaqueBodyMaterial;
                surface.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;surface.receiveShadows=false;
            }
            // Its resting colour from the first frame it is drawn, not the material's own until the next update.
            if (block == null) block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor(ColorId, idle);
            if (glow > 0) block.SetColor(EmissionId, idle * glow * 0.25f);
            renderer.SetPropertyBlock(block);

            // Signed-distance-field type stays crisp in a headset. One size for every label; a long label wraps
            // to a second line, and only shrinks if even that would not fit.
            var go = new GameObject("Label");
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = new Vector3(0, 0, -0.002f);
            var text = go.AddComponent<TextMeshPro>();
            if (labelFont != null) text.font = labelFont;
            if (labelMaterial != null) text.fontSharedMaterial = labelMaterial;
            text.rectTransform.sizeDelta = new Vector2(size.x * 0.9f, size.y * 0.86f);
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.lineSpacing = -12f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 0.1f;
            text.fontSizeMax = labelSize;
            text.color = labelColor;
            text.text = label;

            return new Button { Id = id, Root = root, Face = renderer, Label = text, Size = size };
        }

        void Update()
        {
            if (hidden)
            {
                FingerNear = false;
                pointed = null;
                pointerPress = null;
                return;
            }
            var near = false;
            var now = Time.unscaledTime;
            string pressedId = null;
            foreach (var b in buttons)
            {
                b.Hover = b.Id == pointed;
                if (b.Id == pointerPress)
                {
                    b.Flash = 1f;
                    pressedId = b.Id;
                }
                for (var f = 0; f < fingertips.Count; f++)
                {
                    if (!b.Gestures.TryGetValue(f, out var gesture))
                        b.Gestures[f] = gesture = new PokeGesture(pressDepth, releaseDepth, minPressInterval);
                    var tip = fingertips[f]();
                    if (!tip.HasValue)
                    {
                        gesture.Lost();
                        continue;
                    }
                    var local = b.Root.transform.InverseTransformPoint(tip.Value);
                    var depth = -local.z;
                    var inside = Mathf.Abs(local.x) <= b.Size.x / 2f && Mathf.Abs(local.y) <= b.Size.y / 2f;
                    if (inside && depth < hoverDepth && depth > -0.05f)
                    {
                        b.Hover = true;
                        near = true;
                    }
                    if (gesture.Update(depth, inside, now))
                    {
                        b.Flash = 1f;
                        // One action per frame, dispatched after hit testing. A handler may
                        // replace this entire panel, including the button being pressed.
                        if (pressedId == null) pressedId = b.Id;
                    }
                }
                b.Flash = Mathf.Max(0, b.Flash - Time.unscaledDeltaTime / 0.25f);
                b.Glow = Mathf.Max(0, b.Glow - Time.unscaledDeltaTime / 2.5f);
                var rest = Color.Lerp(idle, hover, Mathf.Max(b.Hover ? 1f : 0f, Mathf.Sin(b.Glow * Mathf.PI) * 0.8f));
                var color = Color.Lerp(rest, pressed, b.Flash);
                b.Face.GetPropertyBlock(block);
                block.SetColor(ColorId, color);
                if (glow > 0) block.SetColor(EmissionId, color * glow * (0.25f + (b.Hover ? 0.5f : 0f) + b.Flash * 1.5f));
                b.Face.SetPropertyBlock(block);
            }
            FingerNear = near;
            pointed = null;
            pointerPress = null;
            if (pressedId != null) Pressed?.Invoke(pressedId);
        }
    }
}
