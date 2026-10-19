using System;
using System.Collections.Generic;
using System.Linq;
using Sankofa.Runtime.Fire;
using Sankofa.Runtime.UI;
using TMPro;
using UnityEngine;

namespace Sankofa.Runtime.Personal
{
    /// <summary>Only presentation data crosses this boundary; story state and permitted actions stay in the app.</summary>
    [Serializable]
    public sealed class StoryTableChapterViewData
    {
        public int Index, Count;
        public string Title, Caption, ArtKey, ImageLabel;
        public string[] PreviousPageTitles = Array.Empty<string>();
        public string[] Questions = Array.Empty<string>();
    }

    /// <summary>A seated illustrated journal. Every essential action has a labelled hand/pointer button.</summary>
    public sealed class StoryTableView : MonoBehaviour
    {
        [Serializable] sealed class Illustration
        {
            public string Key;
            public GameObject Root;
        }

        [SerializeField] Transform tableRoot;
        [SerializeField] Transform[] turningPages = Array.Empty<Transform>();
        [SerializeField] TMP_Text heading, subheading, capability, pageNumber, chapterTitle, caption, illustrationLabel;
        [SerializeField] TMP_Text intakeHeading, intakeSummary, seedText, detailHeading, detailBody, progressText;
        [SerializeField] TMP_Text[] previousTitles = Array.Empty<TMP_Text>();
        [SerializeField] GameObject intakeRoot, readingRoot, detailsRoot, progressRoot, cloudConsentRoot;
        [SerializeField] PokePanel routes, regions, periods, intakeActions, transport, questions, controls, detailActions, consentActions;
        [SerializeField] HearthFire fire;
        [SerializeField] Illustration[] illustrations = Array.Empty<Illustration>();
        [SerializeField] Renderer customIllustration;

        public Action<string> RouteSelected, RegionSelected, PeriodSelected, QuestionSelected;
        public Action StartRequested, StartLocalRequested, SeedEditRequested, SourceDetailsRequested;
        public Action NextRequested, BackRequested, AskRequested, SaveRequested, PauseRequested, RecenterRequested, RoomRequested, LeaveRequested, ContinueRequested, RetryRequested;
        public Action<string> DecisionSelected;

        string selectedRoute = "trade-household", selectedRegion = "enkare-nairobi", selectedPeriod = "1850s";
        string seed = "", currentTitle = "A question worth carrying";
        bool paused, reducedMotion, canBack, canNext, canAsk, canSave, detailsOpen, intake = true;
        bool error, closing, decisionOpen;
        float pageTurn;
        Quaternion[] restingPageRotations;

        public Transform TableRoot => tableRoot != null ? tableRoot : transform;
        public PokePanel[] Panels => new[] { routes, regions, periods, intakeActions, transport, questions, controls, detailActions, consentActions }.Where(p => p != null).ToArray();
        public string Caption => caption != null ? caption.text : "";
        public string CapabilityLabel => capability != null ? capability.text : "";
        public bool IntakeVisible => intake;
        public bool IsPaused => paused;

        void Awake()
        {
            if (routes != null) routes.Pressed += SelectRoute;
            if (regions != null) regions.Pressed += SelectRegion;
            if (periods != null) periods.Pressed += SelectPeriod;
            if (intakeActions != null) intakeActions.Pressed += IntakeAction;
            if (transport != null) transport.Pressed += TransportAction;
            if (questions != null) questions.Pressed += id => QuestionSelected?.Invoke(id);
            if (controls != null) controls.Pressed += ControlAction;
            if (detailActions != null) detailActions.Pressed += DetailAction;
            if (consentActions != null) consentActions.Pressed += ConsentAction;
            restingPageRotations = turningPages.Select(p => p.localRotation).ToArray();
        }

        void Start() => RefreshControls();

        /// <summary>Fits only the physical deck; it never moves the visitor or follows their head.</summary>
        public void FitToSurface(Vector3 center, Quaternion rotation, float width, float depth)
        {
            TableRoot.SetPositionAndRotation(center, rotation);
            var scale = Mathf.Clamp(Mathf.Min(width / 1.25f, depth / 0.72f), 0.65f, 1.15f);
            TableRoot.localScale = Vector3.one * scale;
        }

        public void RenderStory(string title, string subtitle, string capabilityLabel)
        {
            currentTitle = string.IsNullOrWhiteSpace(title) ? "A question worth carrying" : title;
            SetText(heading, currentTitle);
            SetText(subheading, subtitle);
            SetText(capability, capabilityLabel);
            SetText(intakeHeading, "What would you like to understand?");
        }

        public void ShowIntake()
        {
            intake = true; closing = error = decisionOpen = detailsOpen = false;
            ShowOnly(intakeRoot);
            if (cloudConsentRoot != null) cloudConsentRoot.SetActive(false);
            SetText(chapterTitle, "Every story begins with a question");
            SetText(pageNumber, "YOUR STORY TABLE");
            SetText(caption, "Choose a thread below, or bring a small fragment of your own. Read at your own pace; the table stays with you.");
            SetText(illustrationLabel, "Enkare Nairobi • a prepared illustrated setting");
            ShowIllustration("exchange");
            SetPreviousPages(Array.Empty<string>());
            RefreshIntake();
            RefreshControls();
            SetFire(Sankofa.Core.Story.FireState.Listening);
        }

        public void SetIntakeSelection(string route, string region, string period)
        {
            if (!string.IsNullOrEmpty(route)) selectedRoute = route;
            if (!string.IsNullOrEmpty(region)) selectedRegion = region;
            if (!string.IsNullOrEmpty(period)) selectedPeriod = period;
            RefreshIntake();
        }

        public void SetSeedText(string value)
        {
            seed = value ?? "";
            SetText(seedText, seed.Length == 0 ? "Your question or family fragment (optional)" : seed);
            RefreshIntake();
        }

        public void ShowPreparing(string message = "Gathering the next pages…")
        {
            intake = false; error = closing = decisionOpen = detailsOpen = false;
            ShowOnly(progressRoot);
            SetText(progressText, message);
            SetText(pageNumber, "A MOMENT BY THE FIRE");
            SetText(chapterTitle, "Your question is taking shape");
            SetText(caption, "You can return to your choices at any time. A prepared story is always available.");
            if (detailActions != null) detailActions.Show(new[] { ("local", "Read prepared story"), ("cancel", "Back to my choices") });
            RefreshControls();
            SetFire(Sankofa.Core.Story.FireState.Gathering);
        }

        public void RenderChapter(StoryTableChapterViewData chapter)
        {
            if (chapter == null) throw new ArgumentNullException(nameof(chapter));
            intake = false; error = closing = detailsOpen = decisionOpen = false;
            ShowOnly(readingRoot);
            SetText(pageNumber, $"PAGE {chapter.Index + 1:00} / {Mathf.Max(1, chapter.Count):00}");
            SetText(chapterTitle, chapter.Title);
            SetText(caption, chapter.Caption);
            SetText(illustrationLabel, chapter.ImageLabel);
            ShowIllustration(chapter.ArtKey);
            SetPreviousPages(chapter.PreviousPageTitles);
            SetQuestions(chapter.Questions);
            pageTurn = reducedMotion ? 0 : 1;
            RefreshControls();
            SetFire(Sankofa.Core.Story.FireState.Telling);
        }

        public void RenderChapter(int index, int count, string title, string text, string artKey = "exchange", string imageLabel = "Illustrated reconstruction") =>
            RenderChapter(new StoryTableChapterViewData { Index = index, Count = count, Title = title, Caption = text, ArtKey = artKey, ImageLabel = imageLabel });

        public void RenderQuestion(string question, string answer, string provenanceLabel)
        {
            ShowDetails(question, answer, provenanceLabel);
            if (detailActions != null) detailActions.Show(new[] { ("continue", "Return to the story"), ("sources", "About this answer") });
        }

        public void ShowSourceDetails(string title, string details) => ShowDetails(title, details, "Context, your account and reconstruction are kept distinct.");

        public void ShowDecision(string title, string message, IReadOnlyList<(string Id, string Label)> actions)
        {
            ShowDetails(title, message, "");
            decisionOpen = true;
            if (detailActions != null) detailActions.Show(actions ?? Array.Empty<(string Id, string Label)>());
        }

        public void ShowError(string message)
        {
            intake = false; error = true; closing = decisionOpen = detailsOpen = false;
            ShowOnly(progressRoot);
            SetText(progressText, message);
            SetText(chapterTitle, "Let’s find another way in");
            SetText(caption, "Your choices are still here. Try again, or continue with a prepared story.");
            if (detailActions != null) detailActions.Show(new[] { ("retry", "Try again"), ("local", "Read prepared story"), ("cancel", "Back to my choices") });
            RefreshControls();
            SetFire(Sankofa.Core.Story.FireState.Holding);
        }

        public void ShowClosing(string message = "A new question can begin another visit.")
        {
            intake = false; error = decisionOpen = detailsOpen = false; closing = true;
            ShowOnly(readingRoot);
            SetText(pageNumber, "WHAT YOU CARRY FORWARD");
            SetText(chapterTitle, "The next page is yours");
            SetText(caption, message);
            SetQuestions(Array.Empty<string>());
            RefreshControls();
            SetFire(Sankofa.Core.Story.FireState.Resting);
        }

        public void SetStatus(string message) => SetText(capability, message);

        public void SetPaused(bool value)
        {
            paused = value;
            if (fire != null) fire.enabled = !value;
            RefreshControls();
        }

        public void SetReducedMotion(bool value)
        {
            reducedMotion = value;
            if (fire != null) fire.ReducedMotion = value;
            if (value) pageTurn = 0;
            RestorePages();
        }

        public void SetControlState(bool back, bool next, bool ask, bool save)
        {
            canBack = back; canNext = next; canAsk = ask; canSave = save;
            RefreshControls();
        }

        public void SetQuestions(IReadOnlyList<string> labels)
        {
            if (questions == null) return;
            var items = new List<(string Id, string Label)>();
            if (labels != null)
                for (var i = 0; i < Mathf.Min(2, labels.Count); i++)
                    if (!string.IsNullOrWhiteSpace(labels[i])) items.Add((i.ToString(), labels[i]));
            questions.Show(items);
            questions.Hidden = intake || detailsOpen || paused || items.Count == 0;
        }

        /// <summary>A generated image is still one flat illustration on a page, never unvalidated scene geometry.</summary>
        public void SetIllustration(Texture texture, string label)
        {
            if (customIllustration == null || texture == null) return;
            foreach (var item in illustrations) if (item.Root != null) item.Root.SetActive(false);
            customIllustration.gameObject.SetActive(true);
            var block = new MaterialPropertyBlock();
            customIllustration.GetPropertyBlock(block); block.SetTexture("_MainTex", texture); customIllustration.SetPropertyBlock(block);
            SetText(illustrationLabel, label);
        }

        void ShowDetails(string title, string body, string note)
        {
            detailsOpen = true; decisionOpen = false;
            ShowOnly(detailsRoot);
            SetText(detailHeading, title);
            SetText(detailBody, body + (string.IsNullOrWhiteSpace(note) ? "" : "\n\n" + note));
            if (detailActions != null) detailActions.Show(new[] { ("continue", "Return to the story") });
            RefreshControls();
            SetFire(Sankofa.Core.Story.FireState.Listening);
        }

        void ShowOnly(GameObject active)
        {
            foreach (var root in new[] { intakeRoot, readingRoot, detailsRoot, progressRoot, cloudConsentRoot }) if (root != null) root.SetActive(root == active);
            if (questions != null) questions.Hidden = active != readingRoot || paused;
            if (transport != null) transport.Hidden = active != readingRoot || paused;
            // The shared recovery panel is separate from both modal roots.
            if (detailActions != null) detailActions.Hidden = active != detailsRoot && active != progressRoot;
        }

        void RefreshIntake()
        {
            var routeLabels = new[] { ("journeys", "Journeys"), ("trade-household", "Trade & home"), ("place-water", "Place & water") };
            if (routes != null) routes.Show(routeLabels.Select(x => (x.Item1, (selectedRoute == x.Item1 ? "• " : "") + x.Item2)).ToArray());
            if (regions != null) regions.Show(new[] { ("enkare-nairobi", "• Enkare Nairobi") });
            if (periods != null) periods.Show(new[] { ("1850s", "• Around the 1850s") });
            if (intakeActions != null) intakeActions.Show(new[] { ("local", "Read prepared story"), ("edit", seed.Length == 0 ? "Add my question" : "Edit my question"), ("create", "Create my story") });
            var thread = selectedRoute == "journeys" ? "Journeys" : selectedRoute == "place-water" ? "Place & water" : "Trade & home";
            SetText(intakeSummary, thread + "  /  Enkare Nairobi  /  around the 1850s");
            SetText(seedText, seed.Length == 0 ? "Your question or family fragment (optional)" : seed);
        }

        void RefreshControls()
        {
            if (controls != null) controls.Show(new[] { ("pause", paused ? "Resume" : "Pause"), ("recenter", "Recenter"), ("room", "Fit my table"), ("leave", "Leave") });
            if (transport == null) return;
            var items = new List<(string Id, string Label)>();
            if (closing)
            {
                if (canSave) items.Add(("save", "Keep this story"));
                items.Add(("again", "Another question"));
            }
            else
            {
                if (canBack) items.Add(("back", "Previous page"));
                if (canNext) items.Add(("next", "Next page"));
                if (canAsk) items.Add(("ask", "Ask a question"));
                items.Add(("sources", "About this story"));
            }
            transport.Show(items);
            transport.Hidden = intake || paused || detailsOpen || error || (progressRoot != null && progressRoot.activeSelf);
            if (questions != null) questions.Hidden = intake || paused || detailsOpen || closing || error || (progressRoot != null && progressRoot.activeSelf);
        }

        void SetPreviousPages(IReadOnlyList<string> titles)
        {
            for (var i = 0; i < previousTitles.Length; i++)
            {
                var available = titles != null && i < titles.Count;
                if (previousTitles[i] == null) continue;
                previousTitles[i].transform.parent.gameObject.SetActive(available);
                previousTitles[i].text = available ? $"{i + 1:00}\n{titles[i]}" : "";
            }
        }

        void ShowIllustration(string key)
        {
            if (customIllustration != null) customIllustration.gameObject.SetActive(false);
            var chosen = illustrations.FirstOrDefault(x => x.Key == key) ?? illustrations.FirstOrDefault(x => x.Key == "exchange");
            foreach (var root in illustrations.Where(x => x.Root != null).Select(x => x.Root).Distinct()) root.SetActive(chosen != null && root == chosen.Root);
        }

        void SelectRoute(string id) { selectedRoute = id; RefreshIntake(); RouteSelected?.Invoke(id); }
        void SelectRegion(string id) { selectedRegion = id; RefreshIntake(); RegionSelected?.Invoke(id); }
        void SelectPeriod(string id) { selectedPeriod = id; RefreshIntake(); PeriodSelected?.Invoke(id); }
        void IntakeAction(string id)
        {
            if (id == "local") StartLocalRequested?.Invoke();
            else if (id == "edit") SeedEditRequested?.Invoke();
            else if (id == "create")
            {
                ShowOnly(cloudConsentRoot);
                if (consentActions != null) consentActions.Show(new[] { ("create", "Agree & create"), ("cancel", "Back to my choices") });
            }
        }
        void ConsentAction(string id) { if (id == "create") StartRequested?.Invoke(); else ShowIntake(); }
        void TransportAction(string id)
        {
            if (id == "back") BackRequested?.Invoke();
            else if (id == "next") NextRequested?.Invoke();
            else if (id == "ask") AskRequested?.Invoke();
            else if (id == "save") SaveRequested?.Invoke();
            else if (id == "sources") SourceDetailsRequested?.Invoke();
            else if (id == "again") ContinueRequested?.Invoke();
        }
        void ControlAction(string id)
        {
            if (id == "pause") PauseRequested?.Invoke();
            else if (id == "recenter") RecenterRequested?.Invoke();
            else if (id == "room") RoomRequested?.Invoke();
            else if (id == "leave") LeaveRequested?.Invoke();
        }
        void DetailAction(string id)
        {
            if (decisionOpen) { DecisionSelected?.Invoke(id); return; }
            if (id == "sources") SourceDetailsRequested?.Invoke();
            else if (id == "retry") RetryRequested?.Invoke();
            else if (id == "local") StartLocalRequested?.Invoke();
            else if (id == "cancel") ContinueRequested?.Invoke();
            else { detailsOpen = false; ContinueRequested?.Invoke(); }
        }
        void SetFire(Sankofa.Core.Story.FireState state) { if (fire != null) fire.SetState(state); }
        static void SetText(TMP_Text text, string value) { if (text != null) text.text = value ?? ""; }

        void Update()
        {
            if (paused || reducedMotion || pageTurn <= 0) return;
            pageTurn = Mathf.Max(0, pageTurn - Time.deltaTime / 0.38f);
            for (var i = 0; i < turningPages.Length; i++)
                if (turningPages[i] != null) turningPages[i].localRotation = restingPageRotations[i] * Quaternion.Euler(0, Mathf.Sin(pageTurn * Mathf.PI) * (i == 0 ? 3 : -2), 0);
        }
        void RestorePages()
        {
            if (restingPageRotations == null) return;
            for (var i = 0; i < turningPages.Length; i++) if (turningPages[i] != null) turningPages[i].localRotation = restingPageRotations[i];
        }
    }
}
