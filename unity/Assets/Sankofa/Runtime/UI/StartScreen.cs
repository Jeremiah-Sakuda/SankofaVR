using TMPro;
using UnityEngine;

namespace Sankofa.Runtime.UI
{
    /// <summary>
    /// The title card at the hearth before a visit: the name, the place, and how to choose. The fire burns and the
    /// night sounds play, but nothing is spoken and no story begins until the visitor presses Begin.
    /// </summary>
    public sealed class StartScreen : MonoBehaviour
    {
        [SerializeField] TextMeshPro title;
        [SerializeField] TextMeshPro subtitle;
        [SerializeField] TextMeshPro hint;
        [SerializeField] float fadeSeconds = 0.8f;

        float shown, target;

        public bool Visible => target > 0;

        public void Show() => target = 1;

        public void Hide() => target = 0;

        void Awake() => Apply();

        void Update()
        {
            if (Mathf.Approximately(shown, target)) return;
            shown = Mathf.MoveTowards(shown, target, Time.unscaledDeltaTime / fadeSeconds);
            Apply();
        }

        void Apply()
        {
            foreach (var text in new[] { title, subtitle, hint })
            {
                if (text == null) continue;
                text.alpha = shown;
                text.enabled = shown > 0.001f;
            }
        }
    }
}
