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
        string hintText;

        public bool Visible => target > 0;

        public void Show() => Show(null);

        /// <summary>Shows the card, with a notice (such as the story's review status) set under the hint.</summary>
        public void Show(string notice)
        {
            if (hint != null)
            {
                hintText ??= hint.text;
                hint.text = string.IsNullOrEmpty(notice) ? hintText : hintText + "\n<size=72%><alpha=#B0>" + notice;
            }
            target = 1;
        }

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
