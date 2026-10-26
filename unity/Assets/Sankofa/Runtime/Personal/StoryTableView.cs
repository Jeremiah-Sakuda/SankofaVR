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
        /// <summary>Perspectives to follow from this page; when present they take the place of the side questions.</summary>
        public string[] Choices = Array.Empty<string>();
        public string ChoicePrompt;
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
        [SerializeField] TMP_Text consentHeading, consentBody;
        [SerializeField] TMP_Text[] previousTitles = Array.Empty<TMP_Text>();
        [SerializeField] GameObject intakeRoot, readingRoot, detailsRoot, progressRoot, cloudConsentRoot;
        [SerializeField] PokePanel routes, regions, periods, intakeActions, keptActions, transport, questions, controls, detailActions, consentActions;
        [SerializeField] HearthFire fire;
        [SerializeField] Illustration[] illustrations = Array.Empty<Illustration>();
        [SerializeField] Renderer customIllustration;

        public Action<string> RouteSelected, RegionSelected, PeriodSelected, QuestionSelected, ChoiceSelected;
        public Action StartRequested, StartLocalRequested, SeedEditRequested, SourceDetailsRequested;
        public Action NextRequested, BackRequested, AskRequested, SaveRequested, PauseRequested, RecenterRequested, RoomRequested, LeaveRequested, ContinueRequested, RetryRequested;
        public Action ReopenRequested, ForgetRequested, ComfortRequested, AboutRequested;
        public Action<string> DecisionSelected;

        string selectedRoute = "trade-household", selectedRegion = "enkare-nairobi", selectedPeriod = "1850s";
        string seed = "", currentTitle = "A question worth carrying";
        bool paused, reducedMotion, canBack, canNext, canAsk, canSave, detailsOpen, intake = true;
        bool error, closing, decisionOpen, hasKeptStory, offeringChoice, onlineAvailable = true;
        readonly List<string> captionPages = new List<string>();
        int captionPage, chapterIndex, chapterCount;
        string previousCaption, previousChapterTitle;
        float authoredCaptionSize, authoredChapterSize, authoredDetailSize;
        readonly List<string> modalPages = new List<string>();
        (string Id, string Label)[] modalActions = Array.Empty<(string Id, string Label)>();
        string modalTitle, modalContent;
        int modalPage;
        bool modalUsesConsent;
        float pageTurn;
        Quaternion[] restingPageRotations;

        const string ChoicePrefix = "choice:";
        public const string DefaultChoicePrompt = "Whose side will you follow?";
        public Transform TableRoot => tableRoot != null ? tableRoot : transform;
        public PokePanel[] Panels => new[] { routes, regions, periods, intakeActions, keptActions, transport, questions, controls, detailActions, consentActions }.Where(p => p != null).ToArray();
        public string Caption => caption != null ? caption.text : "";
        public string CapabilityLabel => capability != null ? capability.text : "";
        public bool IntakeVisible => intake;
        public bool IsPaused => paused;
        public int ReadingPart => captionPage;
        public int ReadingPartCount => captionPages.Count;
        public int ModalPart => modalPage;
        public int ModalPartCount => modalPages.Count;
        public string ModalText => modalUsesConsent ? consentBody != null ? consentBody.text : "" : detailBody != null ? detailBody.text : "";

        void Awake()
        {
            if (routes != null) routes.Pressed += SelectRoute;
            if (regions != null) regions.Pressed += SelectRegion;
            if (periods != null) periods.Pressed += SelectPeriod;
            if (intakeActions != null) intakeActions.Pressed += IntakeAction;
            if (keptActions != null) keptActions.Pressed += id => { if (id == "reopen") ReopenRequested?.Invoke(); else if (id == "forget") ForgetRequested?.Invoke(); };
            if (transport != null) transport.Pressed += TransportAction;
            if (questions != null) questions.Pressed += id =>
            {
                if (id.StartsWith(ChoicePrefix, StringComparison.Ordinal)) ChoiceSelected?.Invoke(id.Substring(ChoicePrefix.Length));
                else QuestionSelected?.Invoke(id);
            };
            if (controls != null) controls.Pressed += ControlAction;
            if (detailActions != null) detailActions.Pressed += DetailAction;
            if (consentActions != null) consentActions.Pressed += ConsentAction;
            restingPageRotations = turningPages.Select(p => p.localRotation).ToArray();
            authoredCaptionSize = caption != null ? caption.fontSize : 0.23f;
            authoredChapterSize = chapterTitle != null ? chapterTitle.fontSize : 0.22f;
            authoredDetailSize = detailBody != null ? detailBody.fontSize : 0.135f;
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
            intake = true; closing = error = decisionOpen = detailsOpen = offeringChoice = false;
            modalUsesConsent = false;
            previousCaption = previousChapterTitle = null; captionPages.Clear(); captionPage = 0;
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
            RefreshIntake();
        }

        public void SetHasKeptStory(bool value) { hasKeptStory = value; RefreshIntake(); }

        /// <summary>Without a configured story service, the intake offers no online action that can only fail.</summary>
        public void SetOnlineAvailable(bool value) { onlineAvailable = value; RefreshIntake(); }

        /// <summary>0..1 loudness of the narrator's voice; the fire breathes with it.</summary>
        public void SetSpeechLevel(float level) { if (fire != null) fire.SpeechLevel = level; }

        public void ResetReadingPosition()
        {
            previousCaption = previousChapterTitle = null;
            if (captionPages.Count > 0) SetReadingPart(0); else captionPage = 0;
        }

        public void ShowPreparing(string message = "Gathering the next pages…")
        {
            intake = false; error = closing = decisionOpen = detailsOpen = offeringChoice = false;
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
            var choices = chapter.Choices ?? Array.Empty<string>();
            offeringChoice = choices.Length > 0;
            // The prompt is presentation: it follows the page's own words and is never part of the story or its narration.
            var text = offeringChoice ? chapter.Caption + "\n\n" + (string.IsNullOrWhiteSpace(chapter.ChoicePrompt) ? DefaultChoicePrompt : chapter.ChoicePrompt) : chapter.Caption;
            var retainedPart = previousCaption == text && previousChapterTitle == chapter.Title && chapterIndex == chapter.Index ? captionPage : 0;
            chapterIndex = chapter.Index; chapterCount = Mathf.Max(1, chapter.Count);
            SetText(chapterTitle, chapter.Title);
            PaginateCaption(text, retainedPart);
            previousCaption = text; previousChapterTitle = chapter.Title;
            SetText(illustrationLabel, chapter.ImageLabel);
            ShowIllustration(chapter.ArtKey);
            SetPreviousPages(chapter.PreviousPageTitles);
            if (offeringChoice) SetChoices(choices); else SetQuestions(chapter.Questions);
            pageTurn = reducedMotion ? 0 : 1;
            RefreshControls();
            SetFire(Sankofa.Core.Story.FireState.Telling);
        }

        public void RenderChapter(int index, int count, string title, string text, string artKey = "exchange", string imageLabel = "Illustrated reconstruction") =>
            RenderChapter(new StoryTableChapterViewData { Index = index, Count = count, Title = title, Caption = text, ArtKey = artKey, ImageLabel = imageLabel });

        public void RenderQuestion(string question, string answer, string provenanceLabel)
        {
            ShowDetails(question, answer, provenanceLabel);
            SetModalActions(new[] { ("continue", "Return to the story"), ("sources", "About this answer") });
        }

        public void ShowSourceDetails(string title, string details) => ShowDetails(title, details, "Context, your account and reconstruction are kept distinct.");

        public void ShowDecision(string title, string message, IReadOnlyList<(string Id, string Label)> actions)
        {
            ShowDetails(title, message, "");
            decisionOpen = true;
            SetModalActions(actions ?? Array.Empty<(string Id, string Label)>());
        }

        public void ShowError(string message)
        {
            intake = false; error = true; closing = decisionOpen = detailsOpen = offeringChoice = false;
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
            intake = false; error = decisionOpen = detailsOpen = offeringChoice = false; closing = true;
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

        public void SetLargeText(bool value)
        {
            if (caption != null) caption.fontSize = authoredCaptionSize * (value ? 1.22f : 1);
            if (chapterTitle != null) chapterTitle.fontSize = authoredChapterSize * (value ? 1.12f : 1);
            if (detailBody != null) detailBody.fontSize = authoredDetailSize * (value ? 1.15f : 1);
            if (consentBody != null) consentBody.fontSize = 0.14f * (value ? 1.15f : 1);
            if (previousCaption != null && !intake && !detailsOpen && !closing) PaginateCaption(previousCaption, captionPage);
            if (detailsOpen) RefreshModal();
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

        /// <summary>Side-by-side perspectives in the same reachable panel as the questions; continuing without choosing lets the fire choose.</summary>
        public void SetChoices(IReadOnlyList<string> labels)
        {
            if (questions == null) return;
            var items = new List<(string Id, string Label)>();
            for (var i = 0; i < Mathf.Min(2, labels.Count); i++)
                if (!string.IsNullOrWhiteSpace(labels[i])) items.Add((ChoicePrefix + i, labels[i]));
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
            detailsOpen = true; decisionOpen = false; modalUsesConsent = false;
            ShowOnly(detailsRoot);
            modalTitle = title; modalContent = body + (string.IsNullOrWhiteSpace(note) ? "" : "\n\n" + note); modalPage = 0;
            SetModalActions(new[] { ("continue", "Return to the story") });
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
            if (active == progressRoot && detailActions != null && progressRoot != null)
                detailActions.transform.SetPositionAndRotation(progressRoot.transform.TransformPoint(new Vector3(0, -0.10f, -0.008f)), progressRoot.transform.rotation);
            var preview = active == intakeRoot && seed.Length > 0;
            if (seedText != null) seedText.gameObject.SetActive(preview);
            if (caption != null) caption.gameObject.SetActive(!preview);
        }

        void RefreshIntake()
        {
            var routeLabels = new[] { ("journeys", "Journeys"), ("trade-household", "Trade & home"), ("place-water", "Place & water") };
            if (routes != null) routes.Show(routeLabels.Select(x => (x.Item1, (selectedRoute == x.Item1 ? "• " : "") + x.Item2)).ToArray());
            if (regions != null) regions.Show(new[] { ("enkare-nairobi", "• Enkare Nairobi") });
            if (periods != null) periods.Show(new[] { ("1850s", "• Around the 1850s") });
            var edit = ("edit", seed.Length == 0 ? "Add my question" : "Edit my question");
            if (intakeActions != null) intakeActions.Show(onlineAvailable
                ? new[] { ("local", "Read prepared story"), edit, ("create", "Create my story") }
                : new[] { ("local", seed.Length == 0 ? "Begin the story" : "Begin with my question"), edit, ("about", "How it works") });
            if (keptActions != null) { keptActions.Show(new[] { ("reopen", "Reopen kept story"), ("forget", "Forget kept story") }); keptActions.Hidden = !hasKeptStory || paused || detailsOpen || intakeRoot == null || !intakeRoot.activeSelf; }
            if (intakeHeading != null) intakeHeading.gameObject.SetActive(!hasKeptStory);
            var thread = selectedRoute == "journeys" ? "Journeys" : selectedRoute == "place-water" ? "Place & water" : "Trade & home";
            SetText(intakeSummary, thread + "  /  Enkare Nairobi  /  around the 1850s");
            var previewLimit = Mathf.Min(170, seed.Length);
            string Preview()
            {
                var excerpt = seed.Length > previewLimit ? seed.Substring(0, previewLimit).TrimEnd() + "…\nFull question is available with Edit my question." : seed;
                return "Your question" + (seed.Length > previewLimit ? " (preview)" : "") + ":\n" + excerpt;
            }
            if (seedText != null)
            {
                while (previewLimit > 40 && seedText.GetPreferredValues(Preview(), seedText.rectTransform.rect.width, Mathf.Infinity).y > seedText.rectTransform.rect.height) previewLimit -= 10;
                SetText(seedText, Preview());
            }
            var preview = intakeRoot != null && intakeRoot.activeSelf && seed.Length > 0;
            if (seedText != null) seedText.gameObject.SetActive(preview);
            if (caption != null && intakeRoot != null && intakeRoot.activeSelf) caption.gameObject.SetActive(!preview);
        }

        void RefreshControls()
        {
            var choosing = intakeRoot != null && intakeRoot.activeSelf && !paused && !detailsOpen;
            if (routes != null) routes.Hidden = !choosing;
            if (regions != null) regions.Hidden = !choosing;
            if (periods != null) periods.Hidden = !choosing;
            if (intakeActions != null) intakeActions.Hidden = !choosing;
            if (keptActions != null) keptActions.Hidden = !choosing || !hasKeptStory;
            if (controls != null) controls.Show(new[] { ("pause", paused ? "Resume" : "Pause"), ("recenter", "Recenter"), ("room", "Fit my table"), ("options", "Options"), ("leave", "Leave") });
            if (transport == null) return;
            var items = new List<(string Id, string Label)>();
            if (closing)
            {
                if (canBack) items.Add(("back", "Previous page"));
                if (canSave) items.Add(("save", "Keep this story"));
                items.Add(("again", "Another question"));
            }
            else
            {
                if (canBack || captionPage > 0) items.Add(("back", "Previous page"));
                var morePage = captionPage + 1 < captionPages.Count;
                if (canNext || morePage) items.Add(("next", morePage ? "Continue reading" : offeringChoice ? "Let the fire choose" : "Next page"));
                if (canAsk) items.Add(("ask", "Explore context"));
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
            else if (id == "about") AboutRequested?.Invoke();
            else if (id == "create")
            {
                detailsOpen = true; modalUsesConsent = true; ShowOnly(cloudConsentRoot);
                modalTitle = "Create a story from this question";
                var thread = selectedRoute == "journeys" ? "Journeys & encounters" : selectedRoute == "place-water" ? "Place & water" : "Trade & household";
                modalContent = "These selections and your full question go to the Sankofa service, which uses Google Gemini when connected. Read aloud can send accepted story text to Google Cloud Text-to-Speech. Share only what you are comfortable sending. Prepared stories work offline.\n\nPlace: Enkare Nairobi (Kenya)\nPeriod: before the railway (1850s)\nInterest: " + thread + "\n\nYour question or fragment:\n" + (seed.Length == 0 ? "None supplied." : seed);
                modalPage = 0; SetModalActions(new[] { ("create", "Agree & create"), ("cancel", "Back to my choices") });
                RefreshControls();
            }
        }
        void ConsentAction(string id)
        {
            if (id == "modal-prev") { modalPage = Mathf.Max(0, modalPage - 1); RefreshModal(); }
            else if (id == "modal-next") { modalPage = Mathf.Min(modalPages.Count - 1, modalPage + 1); RefreshModal(); }
            else if (id == "create") StartRequested?.Invoke(); else ShowIntake();
        }
        void TransportAction(string id)
        {
            if (id == "back") { if (!closing && captionPage > 0) SetReadingPart(captionPage - 1); else BackRequested?.Invoke(); }
            else if (id == "next") { if (captionPage + 1 < captionPages.Count) SetReadingPart(captionPage + 1); else NextRequested?.Invoke(); }
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
            else if (id == "options") ComfortRequested?.Invoke();
            else if (id == "leave") LeaveRequested?.Invoke();
        }
        void DetailAction(string id)
        {
            if (id == "modal-prev") { modalPage = Mathf.Max(0, modalPage - 1); RefreshModal(); return; }
            if (id == "modal-next") { modalPage = Mathf.Min(modalPages.Count - 1, modalPage + 1); RefreshModal(); return; }
            if (decisionOpen) { DecisionSelected?.Invoke(id); return; }
            if (id == "sources") SourceDetailsRequested?.Invoke();
            else if (id == "retry") RetryRequested?.Invoke();
            else if (id == "local") StartLocalRequested?.Invoke();
            else if (id == "cancel") ContinueRequested?.Invoke();
            else { detailsOpen = false; ContinueRequested?.Invoke(); }
        }
        void SetFire(Sankofa.Core.Story.FireState state) { if (fire != null) fire.SetState(state); }
        static void SetText(TMP_Text text, string value) { if (text != null) text.text = value ?? ""; }

        void SetModalActions(IReadOnlyList<(string Id, string Label)> actions)
        {
            modalActions = actions.ToArray(); RefreshModal();
        }

        void RefreshModal()
        {
            var body = modalUsesConsent ? consentBody : detailBody;
            var actions = modalUsesConsent ? consentActions : detailActions;
            var root = modalUsesConsent ? cloudConsentRoot : detailsRoot;
            var headingText = modalUsesConsent ? consentHeading : detailHeading;
            if (body == null || actions == null || root == null) return;
            var compact = modalActions.Length > 3;
            void LayoutBody()
            {
                body.rectTransform.sizeDelta = new Vector2(0.76f, compact ? 0.09f : 0.22f);
                body.transform.localPosition = new Vector3(0, compact ? 0.065f : 0.015f, -0.006f);
                modalPages.Clear(); modalPages.AddRange(SplitText(body, modalContent));
            }
            LayoutBody();
            if (!compact && modalPages.Count > 1 && modalActions.Length + 2 > 3) { compact = true; LayoutBody(); }
            modalPage = Mathf.Clamp(modalPage, 0, modalPages.Count - 1);
            SetText(body, modalPages[modalPage]);
            SetText(headingText, modalTitle + (modalPages.Count > 1 ? $"   •   {modalPage + 1} / {modalPages.Count}" : ""));
            var items = modalActions.ToList();
            if (modalPage > 0) items.Add(("modal-prev", "Earlier text"));
            if (modalPage + 1 < modalPages.Count) items.Add(("modal-next", "More text"));
            actions.transform.SetPositionAndRotation(root.transform.TransformPoint(new Vector3(0, compact ? -0.043f : -0.145f, -0.008f)), root.transform.rotation);
            actions.Show(items);
        }

        static List<string> SplitText(TMP_Text field, string value)
        {
            var result = new List<string>(); var remaining = (value ?? "").Trim();
            var bounds = field.rectTransform.rect.size;
            while (remaining.Length > 0)
            {
                if (field.GetPreferredValues(remaining, bounds.x, Mathf.Infinity).y <= bounds.y) { result.Add(remaining); break; }
                var low = 1; var high = remaining.Length;
                while (low < high)
                {
                    var middle = (low + high + 1) / 2;
                    if (field.GetPreferredValues(remaining.Substring(0, middle), bounds.x, Mathf.Infinity).y <= bounds.y) low = middle; else high = middle - 1;
                }
                var cut = low; while (cut > 0 && !char.IsWhiteSpace(remaining[cut - 1])) cut--;
                if (cut == 0) cut = low;
                cut = SentenceBoundary(remaining, cut);
                result.Add(remaining.Substring(0, cut).TrimEnd()); remaining = remaining.Substring(cut).TrimStart();
            }
            if (result.Count == 0) result.Add(""); return result;
        }

        void PaginateCaption(string value, int retainedPart)
        {
            captionPages.Clear(); captionPage = 0;
            var remaining = (value ?? "").Trim();
            if (caption == null) { captionPages.Add(remaining); return; }
            // TMP measures the actual selected font and rectangle; text is never silently clipped or shrunk.
            while (remaining.Length > 0)
            {
                var bounds = caption.rectTransform.rect.size;
                if (caption.GetPreferredValues(remaining, bounds.x, Mathf.Infinity).y <= bounds.y)
                { captionPages.Add(remaining); break; }
                var low = 1; var high = remaining.Length;
                while (low < high)
                {
                    var middle = (low + high + 1) / 2;
                    if (caption.GetPreferredValues(remaining.Substring(0, middle), bounds.x, Mathf.Infinity).y <= bounds.y) low = middle;
                    else high = middle - 1;
                }
                var cut = low;
                while (cut > 0 && !char.IsWhiteSpace(remaining[cut - 1])) cut--;
                if (cut == 0) cut = low;
                cut = SentenceBoundary(remaining, cut);
                captionPages.Add(remaining.Substring(0, cut).TrimEnd());
                remaining = remaining.Substring(cut).TrimStart();
            }
            if (captionPages.Count == 0) captionPages.Add("");
            SetReadingPart(Mathf.Clamp(retainedPart, 0, captionPages.Count - 1));
        }

        static int SentenceBoundary(string text, int fitted)
        {
            for (var i = fitted - 1; i >= fitted * .55f; i--)
            {
                if (text[i] != '.' && text[i] != '?' && text[i] != '!') continue;
                var end = i + 1;
                while (end < fitted && (text[end] == '\'' || text[end] == '"' || text[end] == '’' || text[end] == '”')) end++;
                if (end == text.Length || char.IsWhiteSpace(text[end])) return end;
            }
            return fitted;
        }

        void SetReadingPart(int index)
        {
            captionPage = Mathf.Clamp(index, 0, captionPages.Count - 1);
            SetText(caption, captionPages[captionPage]);
            SetText(pageNumber, $"PAGE {chapterIndex + 1:00} / {chapterCount:00}" + (captionPages.Count > 1 ? $"   •   {captionPage + 1} / {captionPages.Count}" : ""));
            pageTurn = reducedMotion ? 0 : 1;
            RefreshControls();
        }

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
