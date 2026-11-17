using System.Collections.Generic;
using System.Linq;
using Sankofa.Flow;
using Sankofa.Runtime.UI;
using TMPro;
using UnityEngine;

namespace Sankofa.Runtime.Personal
{
    /// <summary>Fits the river's caption backing to its words, retaining the authored text and stationary hand targets.</summary>
    [DefaultExecutionOrder(850)]
    public sealed class RiverReadingLayout : MonoBehaviour
    {
        [SerializeField] RiverThresholdPrototype prototype;
        Transform page, paper, shadow, rule;
        TMP_Text caption, setting, truth, availability;
        Vector3 paperPosition, paperScale, shadowPosition, shadowScale;
        Vector3 truthPosition, availabilityPosition;
        PokePanel transport, questions, utility;
        Vector3 transportPosition, questionsPosition, utilityPosition;
        int transportRevision = -1, questionsRevision = -1;
        bool transportActive, questionsActive;
        string measuredText;
        float measuredFont;
        Vector2 measuredBounds;
        bool applied;

        public float CaptionBackingHeight { get; private set; }
        public float CaptionFontSize => caption != null ? caption.fontSize : 0;

        void Start()
        {
            page = prototype.App.View.TableRoot.Find("Story page");
            paper = page.Find("Page paper"); shadow = page.Find("Page shadow"); rule = page.Find("Fine rule");
            caption = page.Find("Narrative captions").GetComponent<TMP_Text>();
            setting = page.Find("Story setting").GetComponent<TMP_Text>();
            truth = page.Find("Illustration context").GetComponent<TMP_Text>();
            availability = page.Find("Story availability").GetComponent<TMP_Text>();
            paperPosition = paper.localPosition; paperScale = paper.localScale;
            shadowPosition = shadow.localPosition; shadowScale = shadow.localScale;
            truthPosition = truth.transform.localPosition; availabilityPosition = availability.transform.localPosition;
            transport = page.Find("Read and wonder/Turn a page").GetComponent<PokePanel>();
            questions = page.Find("Read and wonder/Questions beside the story").GetComponent<PokePanel>();
            utility = page.Find("Always available controls").GetComponent<PokePanel>();
            transportPosition = transport.transform.localPosition; questionsPosition = questions.transform.localPosition;
            utilityPosition = utility.transform.localPosition;
        }

        void LateUpdate()
        {
            if (page == null) return;
            var view = prototype.App.View;
            var active = prototype.Sequence.Phase == RiverThresholdPhase.Riverside && !view.IntakeVisible;
            view.SetCinematicReading(active);
            prototype.Controls.Hidden = active;
            if (!active)
            {
                if (applied) Restore();
                return;
            }
            applied = true;
            ArrangeActions();
            // Pagination and font size remain owned by the view; only opaque empty space is removed.
            var bounds = caption.rectTransform.rect.size;
            if (measuredText != caption.text || measuredFont != caption.fontSize || measuredBounds != bounds)
            {
                measuredText = caption.text; measuredFont = caption.fontSize; measuredBounds = bounds;
                var captionTop = caption.transform.localPosition.y + bounds.y * .5f;
                var textHeight = Mathf.Min(bounds.y, caption.GetPreferredValues(caption.text, bounds.x, Mathf.Infinity).y);
                var bottom = captionTop - Mathf.Max(.018f, textHeight) - .020f;
                var top = .108f;
                CaptionBackingHeight = top - bottom;
                paper.localPosition = new Vector3(0, (top + bottom) * .5f, paperPosition.z);
                paper.localScale = new Vector3(paperScale.x, CaptionBackingHeight, paperScale.z);
                shadow.localPosition = paper.localPosition + new Vector3(.004f, -.004f, .006f);
                shadow.localScale = new Vector3(shadowScale.x, CaptionBackingHeight + .004f, shadowScale.z);
                truth.transform.localPosition = new Vector3(truthPosition.x, bottom + .009f, truthPosition.z);
                availability.transform.localPosition = new Vector3(availabilityPosition.x, bottom + .009f, availabilityPosition.z);
            }
            // Keep the source boundary visible while the optional context lives one deliberate action away.
            var reading = page.Find("Read and wonder").gameObject.activeSelf;
            paper.gameObject.SetActive(reading); shadow.gameObject.SetActive(reading);
            if (rule != null) rule.gameObject.SetActive(reading);
            setting.gameObject.SetActive(reading); truth.gameObject.SetActive(reading); availability.gameObject.SetActive(reading);
        }

        void ArrangeActions()
        {
            // Two centred story-action rows at fixed heights, followed by one stable utility dock.
            // Real faces keep their authored size; optional caption pages cannot shift this dock.
            transport.transform.localPosition = questions.transform.localPosition = new Vector3(0, 0, -.007f);
            utility.transform.localPosition = new Vector3(0, -.112f, -.007f);
            var showingTransport = transport.gameObject.activeInHierarchy && !transport.Hidden;
            var showingQuestions = questions.gameObject.activeInHierarchy && !questions.Hidden;
            if (transportRevision == transport.Revision && questionsRevision == questions.Revision
                && transportActive == showingTransport && questionsActive == showingQuestions) return;
            var actions = new List<(PokePanel Panel, string Id, float Width)>();
            foreach (var panel in new[] { questions, transport })
                if (panel.gameObject.activeInHierarchy && !panel.Hidden)
                    foreach (var id in panel.Ids)
                    {
                        var face = panel.transform.Find("Button " + id + "/Face");
                        if (face != null) actions.Add((panel, id, face.localScale.x));
                    }
            var positions = new Dictionary<PokePanel, Dictionary<string, Vector3>>
                { [questions] = new Dictionary<string, Vector3>(), [transport] = new Dictionary<string, Vector3>() };
            var first = 0; var row = 0;
            while (first < actions.Count)
            {
                var end = first; var width = 0f;
                while (end < actions.Count && (end == first || width + .012f + actions[end].Width <= .624f))
                { width += (end == first ? 0 : .012f) + actions[end].Width; end++; }
                var x = -width * .5f;
                for (var i = first; i < end; i++)
                {
                    var action = actions[i];
                    positions[action.Panel][action.Id] = new Vector3(x + action.Width * .5f, -.018f - row * .047f, 0);
                    x += action.Width + .012f;
                }
                first = end; row++;
            }
            foreach (var panel in new[] { questions, transport }) panel.ArrangeButtons(positions[panel]);
            transportRevision = transport.Revision; questionsRevision = questions.Revision;
            transportActive = showingTransport; questionsActive = showingQuestions;
        }

        void Restore()
        {
            applied = false;
            measuredText = null; transportRevision = questionsRevision = -1;
            paper.localPosition = paperPosition; paper.localScale = paperScale;
            shadow.localPosition = shadowPosition; shadow.localScale = shadowScale;
            truth.transform.localPosition = truthPosition; availability.transform.localPosition = availabilityPosition;
            transport.transform.localPosition = transportPosition; questions.transform.localPosition = questionsPosition;
            utility.transform.localPosition = utilityPosition;
            transport.ArrangeButtons(null); questions.ArrangeButtons(null);
            foreach (var target in new[] { paper, shadow, rule, setting.transform, truth.transform, availability.transform }.Where(t => t != null))
                target.gameObject.SetActive(true);
        }
    }
}
