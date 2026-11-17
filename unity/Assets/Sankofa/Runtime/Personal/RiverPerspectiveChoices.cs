using System;
using System.Linq;
using Sankofa.Core.Personal;
using Sankofa.Flow;
using Sankofa.Runtime.Input;
using Sankofa.Runtime.UI;
using UnityEngine;

namespace Sankofa.Runtime.Personal
{
    /// <summary>
    /// Places the already permitted perspective choices beside the river's illustrated women.
    /// The reading panel remains an alternative; this adapter owns no story or gesture state.
    /// </summary>
    [DefaultExecutionOrder(800)]
    public sealed class RiverPerspectiveChoices : MonoBehaviour
    {
        [SerializeField] PersonalStoryApp app;
        [SerializeField] RiverThresholdPrototype river;
        [SerializeField] RiverCastPerformance cast;
        [SerializeField] float labelHeight = 1.70f;
        [SerializeField] float forwardOffset = .28f;
        [SerializeField] float panelScale = 2.6f;

        PokePanel[] panels = Array.Empty<PokePanel>();
        string[] boundChoices = Array.Empty<string>(), boundChapters = Array.Empty<string>();
        Action<string>[] handlers = Array.Empty<Action<string>>();

        public PokePanel[] Panels => panels;

        /// <summary>Scene-builder wiring for the selected painted river only.</summary>
        public void Configure(PersonalStoryApp application, RiverThresholdPrototype threshold, RiverCastPerformance performance)
        {
            app = application; river = threshold; cast = performance;
        }

        void Start()
        {
            if (app == null || app.View == null || river == null || cast == null || cast.Solid) return;
            var count = Mathf.Min(cast.Actors.Length, TableStaging.Figures.Count);
            panels = new PokePanel[count]; boundChoices = new string[count]; boundChapters = new string[count];
            handlers = new Action<string>[count];
            var hands = FindObjectsByType<HandSource>(FindObjectsSortMode.None);
            for (var i = 0; i < count; i++)
            {
                var index = i;
                var panel = app.View.StylePanel.CreateSibling(cast.Actors[i].parent,
                    "River perspective " + TableStaging.Figures[i], new Vector2(.24f, .06f), 1);
                panel.transform.localScale = Vector3.one * panelScale;
                panel.Hidden = true;
                handlers[i] = id => Select(index, id);
                panel.Pressed += handlers[i];
                foreach (var hand in hands) { var source = hand; panel.AddFingertip(() => source != null ? source.Fingertip : null); }
                panels[i] = panel;
            }
            // Start follows the application's/threshold's initial wiring. Appending preserves keyboards,
            // recovery controls and other panels already registered with either hand's pointer.
            foreach (var pointer in FindObjectsByType<PointerSource>(FindObjectsSortMode.None)) pointer.AddPanels(panels);
        }

        bool Available(int index)
        {
            var view = app?.View;
            if (!isActiveAndEnabled || view == null || app.Session == null || app.Session.Paused
                || app.Session.State != PersonalStoryState.Reading || river == null || !river.RiverVisible
                || cast == null || cast.Solid || view.Stage == null || view.Stage.Turning
                || !(app.Session.OffersChoice || app.OtherSideOffered)) return false;
            // The contextual page deliberately brings the women back before it finishes. Their presence is
            // not itself an invitation: these spatial labels wait for its complete telling/read pacing.
            // The existing caption alternatives retain their application's manual-reading behaviour.
            if (app.Session.OffersChoice && (app.TurnsPages ? !app.PageToldOnce : view.Stage.Progress < .999f)) return false;
            var chapter = view.LastChapter;
            if (chapter == null || chapter.Id != app.Session.Chapter?.Id || index < 0
                || index >= chapter.ChoiceIds.Length || index >= chapter.Choices.Length) return false;
            // Visibility of the existing accessible choice is the reveal/modal authority. A stale LastChapter
            // cannot expose choices while a question, source detail, options or another decision covers it.
            var id = "choice:" + index;
            return view.Panels.Any(p => p != null && p.isActiveAndEnabled && !p.Hidden && p.Ids.Contains(id));
        }

        int ChoiceFor(int actor)
        {
            var chapter = app?.View?.LastChapter;
            var staging = app?.View?.Stage?.Staging;
            if (chapter == null || staging == null || actor >= TableStaging.Figures.Count) return -1;
            return Array.FindIndex(chapter.ChoiceIds, id => staging.FigureFor(id) == TableStaging.Figures[actor]);
        }

        void LateUpdate()
        {
            for (var i = 0; i < panels.Length; i++)
            {
                var panel = panels[i]; var actor = cast.Actors[i]; var choice = ChoiceFor(i);
                var shown = actor != null && actor.gameObject.activeInHierarchy && Available(choice);
                if (!shown) { panel.Hidden = true; continue; }
                var chapter = app.View.LastChapter;
                boundChoices[i] = chapter.ChoiceIds[choice]; boundChapters[i] = chapter.Id;
                panel.Show(new[] { ("river:" + boundChoices[i], chapter.Choices[choice]) });
                // The labels follow the grounded actor's river placement, not the portrait's negative scale.
                // Facing remains fixed in the seated world; head movement never moves a choice under a pinch.
                panel.transform.SetPositionAndRotation(actor.parent.TransformPoint(new Vector3(actor.localPosition.x,
                    labelHeight, actor.localPosition.z - forwardOffset)), actor.parent.rotation);
                panel.Hidden = false;
            }
        }

        void Select(int actor, string id)
        {
            if (actor < 0 || actor >= panels.Length || panels[actor].Hidden || id != "river:" + boundChoices[actor]) return;
            var choice = ChoiceFor(actor); var chapter = app.View.LastChapter;
            if (!Available(choice) || chapter.Id != boundChapters[actor] || chapter.ChoiceIds[choice] != boundChoices[actor]) return;
            // The same application handler accepts both spatial and caption alternatives.
            app.View.ChoiceSelected?.Invoke(choice.ToString());
        }

        void OnDisable()
        {
            foreach (var panel in panels) if (panel != null) panel.Hidden = true;
        }

        void OnDestroy()
        {
            for (var i = 0; i < panels.Length; i++)
                if (panels[i] != null) { panels[i].Hidden = true; panels[i].Pressed -= handlers[i]; }
        }
    }
}
