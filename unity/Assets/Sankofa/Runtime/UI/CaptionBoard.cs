using TMPro;
using UnityEngine;

namespace Sankofa.Runtime.UI
{
    public enum CaptionSize { Small, Medium, Large }

    /// <summary>
    /// World-locked captions near the fire, set in signed-distance-field type so they stay crisp in a headset: the
    /// spoken line on a soft dark backing that fits it, a status line, and short notices. Usable with sound off.
    /// Size is the visitor's choice; each line fades in briefly rather than popping.
    /// </summary>
    public sealed class CaptionBoard : MonoBehaviour
    {
        /// <summary>Font sizes for TextMeshPro in world space, where a size of 1 is a line about 11.5 cm tall.</summary>
        /// Medium is a line about 1.4 degrees tall at the caption's distance, comfortably above the 1-degree minimum.
        public static float FontSize(CaptionSize size) => size == CaptionSize.Small ? 0.25f : size == CaptionSize.Large ? 0.4f : 0.32f;

        [SerializeField] TMP_FontAsset font;
        [SerializeField] Material textMaterial;
        [SerializeField] Material backingMaterial;
        [SerializeField] float width = 0.95f;
        [SerializeField] float noticeSeconds = 3.5f;
        [SerializeField] float fadeSeconds = 0.18f;

        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int ZTestId = Shader.PropertyToID("unity_GUIZTestMode");

        TextMeshPro caption;
        TextMeshPro status;
        TextMeshPro notice;
        Renderer backing;
        MaterialPropertyBlock block;
        float noticeUntil;
        float clearAt = float.PositiveInfinity;
        float shownAt;
        CaptionSize size = CaptionSize.Large;
        string captionText = "";

        public string Caption => captionText;
        public string Status => status != null ? status.text : "";
        public TextMeshPro CaptionText => caption;

        public CaptionSize Size
        {
            get => size;
            set
            {
                size = value;
                ApplySize();
            }
        }

        /// <summary>
        /// The board's own copy of a material, drawn over the scene. The text and backing shaders take their depth test
        /// from unity_GUIZTestMode, which they do not declare, so a saved material cannot keep it: it is set here.
        /// </summary>
        public static Material OverScene(Material shared)
        {
            if (shared == null) return null;
            var m = new Material(shared) { name = shared.name + " (over scene)" };
            m.SetInt(ZTestId, (int)UnityEngine.Rendering.CompareFunction.Always);
            return m;
        }

        void Awake()
        {
            block = new MaterialPropertyBlock();
            textMaterial = OverScene(textMaterial);
            backingMaterial = OverScene(backingMaterial);
            caption = Make("Caption", 0f, Color.white, textMaterial);
            status = Make("Status", -0.15f, new Color(0.82f, 0.78f, 0.7f), textMaterial);
            notice = Make("Notice", 0.15f, new Color(1f, 0.84f, 0.56f), textMaterial);

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(quad.GetComponent<Collider>());
            quad.name = "Caption backing";
            quad.transform.SetParent(transform, false);
            backing = quad.GetComponent<Renderer>();
            if (backingMaterial != null) backing.sharedMaterial = backingMaterial;
            backing.enabled = false;
            ApplySize();
        }

        TextMeshPro Make(string name, float y, Color color, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0, y, 0);
            var text = go.AddComponent<TextMeshPro>();
            if (font != null) text.font = font;
            if (material != null) text.fontSharedMaterial = material;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.rectTransform.sizeDelta = new Vector2(width, 0.2f);
            text.color = color;
            text.text = "";
            return text;
        }

        void ApplySize()
        {
            if (caption == null) return;
            var s = FontSize(size);
            caption.fontSize = s;
            // The status line is the smallest text; at Medium it still subtends a degree from the seat.
            status.fontSize = s * 0.74f;
            notice.fontSize = s * 0.75f;
            // Larger type wraps sooner; the line stays within a comfortable reading width.
            caption.rectTransform.sizeDelta = new Vector2(width * (size == CaptionSize.Large ? 1.1f : 1f), 0.2f);
            FitBacking();
        }

        public void SetCaption(string text)
        {
            clearAt = float.PositiveInfinity;
            captionText = text ?? "";
            caption.text = captionText;
            shownAt = Time.unscaledTime;
            FitBacking();
        }

        public void Clear() => SetCaption("");

        /// <summary>Keeps a finished line readable for a moment, unless another caption replaces it first.</summary>
        public void ClearAfter(float seconds) => clearAt = Time.unscaledTime + seconds;

        public void SetStatus(string text) => status.text = text ?? "";

        public void ClearNotice() => notice.text = "";

        /// <summary>
        /// A short notice above the caption. A prominent one, such as what the visitor did, is set nearly at caption
        /// size and a little higher, so it reads as part of the moment rather than a footnote.
        /// </summary>
        public void Notice(string text, float seconds = -1, bool prominent = false)
        {
            notice.text = text ?? "";
            notice.fontSize = FontSize(size) * (prominent ? 0.85f : 0.75f);
            notice.transform.localPosition = new Vector3(0, prominent ? 0.21f : 0.15f, 0);
            noticeUntil = Time.unscaledTime + (seconds > 0 ? seconds : noticeSeconds);
        }

        void FitBacking()
        {
            if (backing == null) return;
            backing.enabled = captionText.Length > 0;
            if (!backing.enabled) return;
            caption.ForceMeshUpdate();
            var bounds = caption.textBounds;
            backing.transform.localPosition = new Vector3(bounds.center.x, bounds.center.y, 0.004f);
            backing.transform.localScale = new Vector3(bounds.size.x + 0.07f, bounds.size.y + 0.045f, 1f);
        }

        void Update()
        {
            if (notice.text.Length > 0 && Time.unscaledTime > noticeUntil) notice.text = "";
            if (Time.unscaledTime > clearAt) Clear();
            var fade = Mathf.Clamp01((Time.unscaledTime - shownAt) / Mathf.Max(0.01f, fadeSeconds));
            caption.alpha = fade;
            if (backing.enabled)
            {
                backing.GetPropertyBlock(block);
                // Dark enough that bright windows or flame behind a line do not read through its letters.
                block.SetColor(ColorId, new Color(0.025f, 0.02f, 0.018f, 0.72f * fade));
                backing.SetPropertyBlock(block);
            }
        }
    }
}
