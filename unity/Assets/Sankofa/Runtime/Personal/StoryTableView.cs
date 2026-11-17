using System;
using System.Collections.Generic;
using System.Linq;
using Sankofa.Core.Personal;
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
        /// <summary>The chapter's own id, so the pop-up stage can perform it; never shown.</summary>
        public string Id;
        public string Title, Caption, ArtKey, ImageLabel;
        public string[] PreviousPageTitles = Array.Empty<string>();
        public string[] Questions = Array.Empty<string>();
        /// <summary>Perspectives to follow from this page; when present they take the place of the side questions.</summary>
        public string[] Choices = Array.Empty<string>();
        /// <summary>Ids of <see cref="Choices"/>, in order, so each perspective can stand at its painted figure.</summary>
        public string[] ChoiceIds = Array.Empty<string>();
        public string ChoicePrompt;
        /// <summary>
        /// What continuing without choosing is called. When set, that action is offered even where pages turn
        /// themselves, as when a visitor may end a telling rather than hear its other side.
        /// </summary>
        public string ContinueLabel;
    }

    /// <summary>A seated illustrated journal. Every essential action has a labelled hand/pointer button.</summary>
    public sealed class StoryTableView : MonoBehaviour
    {
        [Serializable] sealed class Illustration
        {
            public string Key;
            public GameObject Root;
        }

        /// <summary>One painted layer of a print, with where it rests on the page and how far it stands forward.</summary>
        sealed class PrintLayer
        {
            public Transform Layer;
            public Vector3 Position, Scale;
            public float Depth;
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
        [Tooltip("The pop-up paper theatre; when present it performs each chapter in place of a flat print.")]
        [SerializeField] PopUpStage stage;
        [Tooltip("Heirlooms beside the interest cards, in the same order as the interests.")]
        [SerializeField] Transform[] heirlooms = Array.Empty<Transform>();
        [Tooltip("A plaque at each painted figure's feet, in the staging's figure order, for choosing whose side to follow.")]
        [SerializeField] PokePanel[] figureChoices = Array.Empty<PokePanel>();
        [Tooltip("Appears over the stage's gourd when the visitor may set it down themselves.")]
        [SerializeField] PokePanel gourdInvite;
        [Tooltip("The kept strand on the stage's rim: one bead for each side of a telling the visitor has kept.")]
        [SerializeField] Renderer[] beads = Array.Empty<Renderer>();
        [SerializeField] Material beadEmpty;
        [Tooltip("A bead's colour is its heirloom's, in the same order as the interests.")]
        [SerializeField] Material[] beadTones = Array.Empty<Material>();

        public Action<string> RouteSelected, RegionSelected, PeriodSelected, QuestionSelected, ChoiceSelected;
        public Action StartRequested, StartLocalRequested, SeedEditRequested, SourceDetailsRequested;
        public Action NextRequested, BackRequested, AskRequested, SaveRequested, PauseRequested, RecenterRequested, RoomRequested, LeaveRequested, ContinueRequested, RetryRequested;
        public Action ReopenRequested, ForgetRequested, ComfortRequested, AboutRequested;
        public Action ContextRequested;
        public Action<string> DecisionSelected;

        /// <summary>How long a newly threaded bead swells before it settles on the strand.</summary>
        public const float BeadSwellSeconds = 0.9f;
        /// <summary>How far an heirloom rises to meet a reaching hand.</summary>
        public const float HeirloomLift = 0.014f;
        string selectedRoute = "trade-household", selectedRegion = "enkare-nairobi", selectedPeriod = "1850s";
        string seed = "", currentTitle = "A question worth carrying";
        bool paused, reducedMotion, canBack, canNext, canAsk, canSave, detailsOpen, intake = true;
        bool error, closing, decisionOpen, hasKeptStory, offeringChoice, onlineAvailable = true;
        bool closingReplay;
        string closingNext;
        readonly List<string> captionPages = new List<string>();
        int captionPage, chapterIndex, chapterCount;
        string previousCaption, previousChapterTitle, closingMessage;
        int partBeforeClosing;
        float authoredCaptionSize, authoredChapterSize, authoredDetailSize;
        Vector2 authoredCaptionBounds;
        Vector3 authoredCaptionPosition;
        readonly List<string> modalPages = new List<string>();
        (string Id, string Label)[] modalActions = Array.Empty<(string Id, string Label)>();
        string modalTitle, modalContent;
        int modalPage;
        bool modalUsesConsent;
        float pageTurn;
        Quaternion[] restingPageRotations;

        // Pop-up print: each illustration's existing painted layers stand forward of the page in stereo depth, the
        // backdrop on the paper and the figures nearest the reader, rising from flat as a page turns.
        const float LayerStep = 0.012f, PopUpSeconds = 0.5f, LayerInset = 0.975f, NominalViewingDistance = 0.95f;
        const int DeepestLayer = 3;
        public const float MaximumLayerDepth = LayerStep * DeepestLayer;
        readonly Dictionary<GameObject, PrintLayer[]> prints = new Dictionary<GameObject, PrintLayer[]>();
        GameObject raisedPrint;
        float popUp = 1, viewingDistance = NominalViewingDistance;

        /// <summary>What the page says while the visitor chooses, once the griot's welcome has been said.</summary>
        public const string StandingInvitation = PersonalStoryVoice.Invitation;
        string invitation;
        bool welcoming, selfTurning, spokenQuestions, ownTable, strandShown;
        bool spatialStageVisible = true;
        bool cinematicReading;
        float[] beadSwell = Array.Empty<float>();
        bool[] beadKept = Array.Empty<bool>();
        Vector3[] heirloomRest = Array.Empty<Vector3>();
        float[] heirloomReach = Array.Empty<float>();

        const string ChoicePrefix = "choice:";
        public const string DefaultChoicePrompt = "Whose side will you follow?";
        public Transform TableRoot => tableRoot != null ? tableRoot : transform;
        public PokePanel[] Panels => new[] { routes, regions, periods, intakeActions, keptActions, transport, questions, controls, detailActions, consentActions }.Concat(figureChoices).Concat(new[] { gourdInvite }).Where(p => p != null).ToArray();
        public string Caption => caption != null ? caption.text : "";
        public PopUpStage Stage => stage;
        /// <summary>Whether its physical stage is visible and can truthfully offer a stage-based hand gesture.</summary>
        public bool SpatialStageVisible => spatialStageVisible;
        /// <summary>The page last rendered, so presentation can be redrawn when only how it is shown has changed.</summary>
        public StoryTableChapterViewData LastChapter { get; private set; }
        /// <summary>Where the pop-up stage stands, for the sounds of its scene.</summary>
        public Vector3 StagePosition => stage != null ? stage.transform.position + Vector3.up * .1f : TableRoot.position;
        /// <summary>Where the griot's fire burns, for its voice and sound.</summary>
        public Vector3 FirePosition => fire != null ? fire.transform.position : TableRoot.TransformPoint(new Vector3(-.47f, .20f, .20f));
        /// <summary>The ordinary ink buttons, as the style for panels made at runtime such as the keyboard.</summary>
        public PokePanel StylePanel => intakeActions != null ? intakeActions : Panels.First();
        public string CapabilityLabel => capability != null ? capability.text : "";
        public bool IntakeVisible => intake;
        public bool IsPaused => paused;
        public bool CinematicReading => cinematicReading;

        /// <summary>The surrounding story uses one context entry; all source and question actions remain available inside it.</summary>
        public void SetCinematicReading(bool value)
        {
            if (cinematicReading == value) return;
            cinematicReading = value;
            if (caption != null)
            {
                var body = closing ? closingMessage : previousCaption;
                var at = string.IsNullOrEmpty(body) || string.IsNullOrEmpty(caption.text) ? 0
                    : Mathf.Max(0, body.IndexOf(caption.text, StringComparison.Ordinal));
                var height = authoredCaptionBounds.y * (value ? .68f : 1);
                caption.rectTransform.sizeDelta = new Vector2(authoredCaptionBounds.x, height);
                caption.transform.localPosition = authoredCaptionPosition + Vector3.up * ((authoredCaptionBounds.y - height) * .5f);
                if (!intake && !detailsOpen && body != null)
                {
                    PaginateCaption(body, 0);
                    for (var i = captionPages.Count - 1; i >= 0; i--)
                        if (body.IndexOf(captionPages[i], StringComparison.Ordinal) <= at) { SetReadingPart(i); break; }
                }
            }
            RefreshControls();
        }
        public int ReadingPart => captionPage;
        public int ReadingPartCount => captionPages.Count;
        public int ModalPart => modalPage;
        public int ModalPartCount => modalPages.Count;
        /// <summary>0 while a print lies flat on a newly turned page, 1 once its layers stand at full depth.</summary>
        public float PopUpProgress => popUp;
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
            if (gourdInvite != null)
            {
                gourdInvite.Show(new[] { ("pass", "Set the gourd down") });
                gourdInvite.Hidden = true;
                gourdInvite.Pressed += id => { if (id == "pass" && stage != null) stage.AcceptGourd(); };
            }
            foreach (var plaque in figureChoices.Where(p => p != null))
                plaque.Pressed += id => { if (id.StartsWith(ChoicePrefix, StringComparison.Ordinal)) ChoiceSelected?.Invoke(id.Substring(ChoicePrefix.Length)); };
            if (controls != null) controls.Pressed += ControlAction;
            if (detailActions != null) detailActions.Pressed += DetailAction;
            if (consentActions != null) consentActions.Pressed += ConsentAction;
            restingPageRotations = turningPages.Select(p => p.localRotation).ToArray();
            heirloomRest = heirlooms.Select(h => h != null ? h.localPosition : Vector3.zero).ToArray();
            heirloomReach = new float[heirlooms.Length];
            beadSwell = new float[beads.Length]; beadKept = new bool[beads.Length];
            CollectPrints();
            authoredCaptionSize = caption != null ? caption.fontSize : 0.23f;
            if (caption != null) { authoredCaptionBounds = caption.rectTransform.sizeDelta; authoredCaptionPosition = caption.transform.localPosition; }
            authoredChapterSize = chapterTitle != null ? chapterTitle.fontSize : 0.22f;
            authoredDetailSize = detailBody != null ? detailBody.fontSize : 0.135f;
        }

        void Start() => RefreshControls();

        /// <summary>The deck's own size in metres before it is fitted to a surface.</summary>
        public const float DeckWidth = 1.25f, DeckDepth = 0.72f;
        /// <summary>How large the deck is drawn on a surface with this much room, never too small to read.</summary>
        public static float SurfaceScale(float width, float depth) => Mathf.Clamp(Mathf.Min(width / DeckWidth, depth / DeckDepth), 0.65f, 1.15f);

        /// <summary>Fits only the physical deck; it never moves the visitor or follows their head.</summary>
        public void FitToSurface(Vector3 center, Quaternion rotation, float width, float depth)
        {
            TableRoot.SetPositionAndRotation(center, rotation);
            TableRoot.localScale = Vector3.one * SurfaceScale(width, depth);
            if (raisedPrint != null) { viewingDistance = MeasureViewingDistance(raisedPrint.transform); ApplyDepth(); }
        }

        public void RenderStory(string title, string subtitle, string capabilityLabel)
        {
            currentTitle = string.IsNullOrWhiteSpace(title) ? "A question worth carrying" : title;
            SetText(heading, currentTitle);
            SetText(subheading, subtitle);
            SetText(capability, capabilityLabel);
            RefreshIntakeHeading();
        }

        /// <summary>
        /// The one line above the first page's actions. Where the griot speaks the invitation, it says how hands choose,
        /// so nobody needs a tutorial; elsewhere it introduces the typed question.
        /// </summary>
        public const string HandsHint = "Reach for an heirloom, or point and pinch its card.";
        void RefreshIntakeHeading() => SetText(intakeHeading, selfTurning && stage != null ? HandsHint : "What would you like to understand?");

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
            ShowIllustration("exchange", false);
            if (stage != null)
            {
                stage.Rest(); ReturnHeirlooms();
                SetText(caption, invitation ?? StandingInvitation);
                SetText(illustrationLabel, "Imagined scenes • sourced context");
            }
            SetPreviousPages(Array.Empty<string>());
            RefreshIntake();
            RefreshControls();
            SetFire(welcoming ? Sankofa.Core.Story.FireState.Welcoming : Sankofa.Core.Story.FireState.Listening);
        }

        /// <summary>
        /// The griot's welcome, one line at a time, where the standing invitation otherwise is. The heirlooms stay in
        /// reach throughout: choosing one never waits for the welcome to finish. Null returns the standing invitation.
        /// </summary>
        public void SetInvitation(string line)
        {
            invitation = string.IsNullOrWhiteSpace(line) ? null : line;
            welcoming = invitation != null;
            if (!intake || detailsOpen || stage == null) return;
            SetText(caption, invitation ?? StandingInvitation);
            SetFire(welcoming ? Sankofa.Core.Story.FireState.Welcoming : Sankofa.Core.Story.FireState.Listening);
        }

        /// <summary>
        /// When pages turn themselves the page carries less: no page-turning buttons, no second copy of a choice that
        /// already stands at each figure, and no start button beside heirlooms that begin a telling when touched.
        /// </summary>
        public void SetSelfTurning(bool value)
        {
            if (selfTurning == value) return;
            selfTurning = value;
            RefreshIntakeHeading();
            RefreshIntake();
            RefreshControls();
            if (offeringChoice && LastChapter != null && !intake && !detailsOpen && !closing) SetChoices(LastChapter.Choices ?? Array.Empty<string>());
        }

        /// <summary>A surrounding scene can replace the miniature stage while retaining reachable choices on the page.</summary>
        public void SetSpatialStageVisible(bool value)
        {
            if (spatialStageVisible == value) return;
            spatialStageVisible = value;
            if (offeringChoice && LastChapter != null && !intake && !detailsOpen && !closing)
            {
                var part = captionPage; var wasLast = part == captionPages.Count - 1;
                RenderChapter(LastChapter);
                SetReadingPart(wasLast ? captionPages.Count - 1 : part);
            }
            RefreshFigureChoices(); RefreshGourdInvite();
        }

        /// <summary>How many beads the strand shows as kept.</summary>
        public int StrandKept { get; private set; }

        /// <summary>
        /// Shows the kept strand: a coloured bead for each kept side, in the order kept, and a small plain bead for
        /// each side not yet heard, so the strand itself says how much of the stories remains.
        /// </summary>
        public void SetStrand(IReadOnlyList<string> keptTellings)
        {
            StrandKept = Mathf.Min(keptTellings?.Count ?? 0, beads.Length);
            if (beadKept.Length != beads.Length) { beadKept = new bool[beads.Length]; beadSwell = new float[beads.Length]; }
            for (var i = 0; i < beads.Length; i++)
            {
                if (beads[i] == null) continue;
                var tone = i < StrandKept ? Array.IndexOf(RouteIds, keptTellings[i]) : -1;
                var kept = tone >= 0 && tone < beadTones.Length && beadTones[tone] != null;
                beads[i].sharedMaterial = kept ? beadTones[tone] : beadEmpty;
                // A bead threaded during this visit swells once as it joins the strand; beads from earlier visits are simply there.
                if (kept && !beadKept[i] && strandShown && !reducedMotion) beadSwell[i] = 1;
                else if (!kept) beadSwell[i] = 0;
                beadKept[i] = kept;
                if (beadSwell[i] <= 0) beads[i].transform.localScale = Vector3.one * (kept ? BeadSize : BeadSize * 0.55f);
            }
            strandShown = true;
        }
        public const float BeadSize = 0.024f;
        /// <summary>A bead is still swelling onto the strand.</summary>
        public bool BeadSwelling => beadSwell.Any(b => b > 0);

        /// <summary>Whether the story stands on the visitor's own table, so the intake can offer the way back to the stand.</summary>
        public void SetOwnTable(bool value)
        {
            if (ownTable == value) return;
            ownTable = value;
            RefreshIntake();
        }

        public int HeirloomCount => heirlooms.Length;
        /// <summary>The heirlooms are out to be chosen: a hand that reaches one begins its telling.</summary>
        public bool HeirloomsOffered => intake && !paused && !detailsOpen && carried < 0 && routes != null && !routes.Hidden;
        /// <summary>The middle of an heirloom in the world, where a hand closes on it.</summary>
        public Vector3 HeirloomPosition(int index) => index >= 0 && index < heirlooms.Length && heirlooms[index] != null
            ? heirlooms[index].position + TableRoot.up * (0.04f * TableRoot.lossyScale.y) : TableRoot.position;
        /// <summary>0 to 1: how near a hand is to an heirloom. It rises a little to meet the hand.</summary>
        public void SetHeirloomReach(int index, float nearness)
        {
            if (index < 0 || index >= heirloomReach.Length) return;
            heirloomReach[index] = Mathf.Clamp01(nearness);
        }
        public float HeirloomReach(int index) => index >= 0 && index < heirloomReach.Length ? heirloomReach[index] : 0;
        /// <summary>A hand has closed on an heirloom: the same choice as touching its card.</summary>
        public bool ChooseHeirloom(int index)
        {
            if (!HeirloomsOffered || index < 0 || index >= heirlooms.Length || index >= RouteIds.Length) return false;
            SelectRoute(RouteIds[index]);
            return true;
        }

        /// <summary>When the fire can be asked aloud, the page's question button says so.</summary>
        public void SetSpokenQuestions(bool value)
        {
            if (spokenQuestions == value) return;
            spokenQuestions = value;
            RefreshControls();
        }

        /// <summary>The app directs the fire while it listens to a question and prepares an answer.</summary>
        public void SetFireState(Sankofa.Core.Story.FireState state) => SetFire(state);

        /// <summary>The griot's fire answers a hand held near it, before any word is said.</summary>
        public void AcknowledgeHand() { if (fire != null && !paused) fire.Acknowledge(); }

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

        /// <summary>
        /// While the griot reads a chapter aloud, the stage performs to the voice and the captions keep pace with it,
        /// turning to the part being spoken. Without narration the visitor turns caption parts themselves.
        /// </summary>
        public void SetNarrationProgress(float seconds, float length)
        {
            if (length <= 0 || intake || detailsOpen || closing) return;
            if (stage != null) stage.SetNarration(seconds, length);
            KeepCaptionsWith(seconds / length);
        }

        /// <summary>
        /// When pages turn themselves and no voice is reading, the captions keep pace with the stage's own clock, so
        /// every part of a page is shown before it turns.
        /// </summary>
        public void FollowStageClock()
        {
            if (stage == null || intake || detailsOpen || closing) return;
            KeepCaptionsWith(stage.Progress);
        }

        void KeepCaptionsWith(float fraction)
        {
            if (captionPages.Count < 2) return;
            var spoken = Mathf.Clamp01(fraction) * captionPages.Sum(p => p.Length);
            var part = 0;
            for (var reached = captionPages[0].Length; part < captionPages.Count - 1 && spoken > reached; reached += captionPages[++part].Length) { }
            if (part > captionPage) SetReadingPart(part);
        }

        /// <summary>Shows the end of the page, as when something has been added after the words already read.</summary>
        public void ShowLastReadingPart()
        {
            if (intake || detailsOpen || closing || captionPages.Count == 0) return;
            if (captionPage != captionPages.Count - 1) SetReadingPart(captionPages.Count - 1);
        }

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
            LastChapter = chapter;
            var fromClosing = closing;
            intake = false; error = closing = detailsOpen = decisionOpen = false;
            ShowOnly(readingRoot);
            var choices = chapter.Choices ?? Array.Empty<string>();
            offeringChoice = choices.Length > 0;
            // The prompt is presentation: it follows the page's own words and is never part of the story or its narration.
            var prompt = string.IsNullOrWhiteSpace(chapter.ChoicePrompt) ? DefaultChoicePrompt
                : spatialStageVisible ? chapter.ChoicePrompt : "This meeting has another side. Choose whose side to hear next.";
            if (cinematicReading && string.IsNullOrWhiteSpace(chapter.ChoicePrompt))
                prompt = selfTurning
                    ? "Choose a side: point, pinch and release. Or wait and let the fire guide you."
                    : "Choose a side: point, pinch and release. Take your time.";
            else if (cinematicReading && selfTurning)
                prompt = "Choose the other side, or wait and let the fire guide you.";
            var text = offeringChoice ? chapter.Caption + "\n\n" + prompt : chapter.Caption;
            var samePage = previousChapterTitle == chapter.Title && chapterIndex == chapter.Index;
            // Stepping back from the ending returns to the part of the page that was being read before it.
            var retainedPart = previousCaption == text && samePage ? (fromClosing ? partBeforeClosing : captionPage) : 0;
            chapterIndex = chapter.Index; chapterCount = Mathf.Max(1, chapter.Count);
            SetText(chapterTitle, chapter.Title);
            PaginateCaption(text, retainedPart);
            previousCaption = text; previousChapterTitle = chapter.Title;
            SetText(illustrationLabel, chapter.ImageLabel);
            // Returning from a question or source detail keeps the print standing; a new page raises it again.
            ShowIllustration(chapter.ArtKey, !samePage);
            if (stage != null)
            {
                stage.Perform(chapter.Id ?? chapter.Title, chapter.Caption, !samePage);
                var ids = chapter.ChoiceIds ?? Array.Empty<string>();
                stage.OfferFigures(offeringChoice ? ids.Select(id => stage.Staging?.FigureFor(id)).ToArray() : Array.Empty<string>());
            }
            SetPreviousPages(chapter.PreviousPageTitles);
            choiceFigures = offeringChoice && stage != null ? (chapter.ChoiceIds ?? Array.Empty<string>()).Select(id => stage.Staging?.FigureFor(id)).ToArray() : Array.Empty<string>();
            if (offeringChoice) SetChoices(choices); else SetQuestions(chapter.Questions);
            if (!samePage) pageTurn = reducedMotion ? 0 : 1;
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
            if (!closing) partBeforeClosing = captionPage;
            intake = false; error = decisionOpen = detailsOpen = offeringChoice = false; closing = true;
            ShowOnly(readingRoot);
            SetText(pageNumber, "WHAT YOU CARRY FORWARD");
            SetText(chapterTitle, "The next page is yours");
            // The ending is measured like any page: at a larger text size it continues on a second part, never clipped.
            closingMessage = message;
            PaginateCaption(message, 0);
            SetQuestions(Array.Empty<string>());
            RefreshControls();
            SetFire(Sankofa.Core.Story.FireState.Resting);
        }

        public void SetStatus(string message) => SetText(capability, message);

        public void SetClosingReplay(bool available) { closingReplay = available; RefreshControls(); }
        public void SetClosingNext(string label) { closingNext = label; RefreshControls(); }

        public void SetPaused(bool value)
        {
            paused = value;
            if (fire != null) fire.enabled = !value;
            if (stage != null) stage.SetPaused(value);
            RefreshControls();
        }

        public void SetReducedMotion(bool value)
        {
            reducedMotion = value;
            if (fire != null) fire.ReducedMotion = value;
            if (stage != null) stage.SetReducedMotion(value);
            if (value) { pageTurn = 0; popUp = 1; ApplyDepth(); }
            RestorePages();
        }

        public void SetLargeText(bool value)
        {
            if (caption != null) caption.fontSize = authoredCaptionSize * (value ? 1.22f : 1);
            if (chapterTitle != null) chapterTitle.fontSize = authoredChapterSize * (value ? 1.12f : 1);
            if (detailBody != null) detailBody.fontSize = authoredDetailSize * (value ? 1.15f : 1);
            if (consentBody != null) consentBody.fontSize = 0.14f * (value ? 1.15f : 1);
            if (previousCaption != null && !intake && !detailsOpen && !closing) PaginateCaption(previousCaption, captionPage);
            else if (closing && !detailsOpen && closingMessage != null) PaginateCaption(closingMessage, captionPage);
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

        /// <summary>
        /// Side-by-side perspectives in the same reachable panel as the questions; continuing without choosing lets the
        /// fire choose. On the pop-up stage each perspective also stands at the feet of the figure it follows.
        /// </summary>
        public void SetChoices(IReadOnlyList<string> labels)
        {
            for (var f = 0; f < figureChoices.Length; f++)
            {
                if (figureChoices[f] == null) continue;
                var figure = f < TableStaging.Figures.Count ? TableStaging.Figures[f] : null;
                var index = Array.IndexOf(choiceFigures, figure);
                figureChoices[f].Show(index >= 0 && index < labels.Count ? new[] { (ChoicePrefix + index, labels[index]) } : Array.Empty<(string, string)>());
            }
            RefreshFigureChoices();
            if (questions == null) return;
            var items = new List<(string Id, string Label)>();
            // With self-turning pages the choice stands at the figures alone; waiting leaves it to the fire.
            var atFigures = selfTurning && spatialStageVisible && stage != null && figureChoices.Any(p => p != null && p.Ids.Any());
            for (var i = 0; i < Mathf.Min(2, labels.Count) && !atFigures; i++)
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
            if (stage != null) stage.SetWaiting(active != null && active != readingRoot && active != intakeRoot);
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
            var routeLabels = stage != null
                ? new[] { ("journeys", "A bundle\nJourneys"), ("trade-household", "A gourd\nTrade & home"), ("place-water", "A water pot\nPlace & water") }
                : new[] { ("journeys", "Journeys"), ("trade-household", "Trade & home"), ("place-water", "Place & water") };
            if (routes != null) routes.Show(routeLabels.Select(x => (x.Item1, (stage == null && selectedRoute == x.Item1 ? "• " : "") + x.Item2)).ToArray());
            if (regions != null) regions.Show(new[] { ("enkare-nairobi", "• Enkare Nairobi") });
            if (periods != null) periods.Show(new[] { ("1850s", "• Around the 1850s") });
            var edit = ("edit", seed.Length == 0 ? "Add my question" : "Edit my question");
            if (intakeActions != null) intakeActions.Show(onlineAvailable
                ? new[] { ("local", "Read prepared story"), edit, ("create", "Create my story") }
                : selfTurning && stage != null && seed.Length == 0 ? new[] { edit, ("about", "How it works"), ownTable ? ("stand", "Use the story stand") : ("room", "Use my table") }
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
            // Heirlooms belong to choosing; once a telling begins only the carried one is still on its way to the fire.
            for (var i = 0; i < heirlooms.Length; i++)
                if (heirlooms[i] != null && i != carried) heirlooms[i].gameObject.SetActive(intake && !detailsOpen);
            // Only what a visitor may need at any moment stays in reach; placement and comfort live in Options.
            if (controls != null)
            {
                controls.SetColumns(cinematicReading ? 4 : 3);
                var utility = new List<(string, string)>();
                if (cinematicReading) utility.Add(("context", "Context"));
                utility.Add(("pause", paused ? "Resume" : "Pause"));
                utility.Add(("options", "Options")); utility.Add(("leave", "Leave"));
                controls.Show(utility);
            }
            if (transport == null) return;
            var items = new List<(string Id, string Label)>();
            if (closing)
            {
                if (captionPage > 0) items.Add(("back", "Earlier text"));
                else if (canBack && !selfTurning) items.Add(("back", "Previous page"));
                if (captionPage + 1 < captionPages.Count) items.Add(("next", "More text"));
                if (canSave) items.Add(("save", "Keep this story"));
                if (closingReplay) items.Add(("reopen-closing", "Reopen kept telling"));
                items.Add(("again", closingNext ?? (selfTurning ? "Another story" : "Another question")));
            }
            else
            {
                if (!selfTurning && (canBack || captionPage > 0)) items.Add(("back", "Previous page"));
                var morePage = captionPage + 1 < captionPages.Count;
                var ending = offeringChoice && !string.IsNullOrWhiteSpace(LastChapter?.ContinueLabel) ? LastChapter.ContinueLabel : null;
                if (!selfTurning && (canNext || morePage)) items.Add(("next", morePage ? "Continue reading" : ending ?? (offeringChoice ? "Let the fire choose" : "Next page")));
                // Where pages turn themselves, the one thing a visitor may still need to say is that they are done.
                else if (selfTurning && ending != null && canNext && !morePage) items.Add(("next", ending));
                if (!cinematicReading)
                {
                    if (canAsk) items.Add(("ask", spokenQuestions ? "Ask the fire" : "Explore context"));
                    items.Add(("sources", "About this story"));
                }
            }
            transport.Show(items);
            transport.Hidden = intake || paused || detailsOpen || error || (progressRoot != null && progressRoot.activeSelf);
            if (questions != null) questions.Hidden = intake || paused || detailsOpen || closing || error
                || (cinematicReading && !offeringChoice) || (progressRoot != null && progressRoot.activeSelf);
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

        void ShowIllustration(string key, bool newPage)
        {
            if (customIllustration != null) customIllustration.gameObject.SetActive(false);
            var chosen = illustrations.FirstOrDefault(x => x.Key == key) ?? illustrations.FirstOrDefault(x => x.Key == "exchange");
            foreach (var root in illustrations.Where(x => x.Root != null).Select(x => x.Root).Distinct()) root.SetActive(chosen != null && root == chosen.Root);
            if (chosen?.Root == null) return;
            if (newPage || chosen.Root != raisedPrint)
            {
                raisedPrint = chosen.Root;
                viewingDistance = MeasureViewingDistance(raisedPrint.transform);
                popUp = reducedMotion ? 1 : 0;
            }
            ApplyDepth();
        }

        /// <summary>Orders each print's painted layers from the page outward by their authored drawing order.</summary>
        void CollectPrints()
        {
            foreach (var root in illustrations.Where(x => x.Root != null).Select(x => x.Root).Distinct())
            {
                var layers = root.GetComponentsInChildren<Renderer>(true).Where(r => r.transform.parent == root.transform)
                    .Select(r => r.transform).OrderByDescending(t => t.localPosition.z).ToArray();
                prints[root] = layers.Select((t, rank) => new PrintLayer
                    { Layer = t, Position = t.localPosition, Scale = t.localScale, Depth = Mathf.Min(rank, DeepestLayer) * LayerStep }).ToArray();
            }
        }

        static float MeasureViewingDistance(Transform print)
        {
            var eye = Camera.main;
            if (eye == null) return NominalViewingDistance;
            return Mathf.Clamp(Mathf.Abs(Vector3.Dot(eye.transform.position - print.position, print.forward)), 0.45f, 1.6f);
        }

        /// <summary>
        /// Raises each layer toward the reader and draws it in toward the print's centre by the same proportion, so from
        /// the seat the composition keeps its painted size and stays inside the print: only depth and parallax change.
        /// A small inset keeps raised edges within the backdrop when the visitor leans.
        /// </summary>
        void ApplyDepth()
        {
            if (raisedPrint == null || !prints.TryGetValue(raisedPrint, out var layers)) return;
            var eased = popUp * popUp * (3 - 2 * popUp);
            var worldScale = Mathf.Abs(raisedPrint.transform.lossyScale.z);
            foreach (var item in layers)
            {
                if (item.Layer == null) continue;
                var depth = item.Depth * eased;
                var keep = depth <= 0 ? 1 : Mathf.Max(0.5f, (viewingDistance - depth * worldScale) / viewingDistance) * Mathf.Lerp(1, LayerInset, eased);
                item.Layer.localPosition = new Vector3(item.Position.x * keep, item.Position.y * keep, item.Position.z - depth);
                item.Layer.localScale = new Vector3(item.Scale.x * keep, item.Scale.y * keep, item.Scale.z);
            }
        }

        void SelectRoute(string id)
        {
            selectedRoute = id; RefreshIntake(); RouteSelected?.Invoke(id);
            if (stage == null) return;
            // Choosing an heirloom carries it to the fire. Without an online path, that choice is also the beginning.
            var index = Array.IndexOf(RouteIds, id);
            if (index >= 0 && index < heirlooms.Length && heirlooms[index] != null) { carried = index; carry = 0; carryFrom = heirlooms[index].localPosition; }
            for (var i = 0; i < heirloomReach.Length; i++) heirloomReach[i] = 0;
            if (!onlineAvailable) StartLocalRequested?.Invoke();
        }
        static readonly string[] RouteIds = { "journeys", "trade-household", "place-water" };
        int carried = -1;
        float carry = 1;
        Vector3 carryFrom;
        void SelectRegion(string id) { selectedRegion = id; RefreshIntake(); RegionSelected?.Invoke(id); }
        void SelectPeriod(string id) { selectedPeriod = id; RefreshIntake(); PeriodSelected?.Invoke(id); }
        void IntakeAction(string id)
        {
            if (id == "local") StartLocalRequested?.Invoke();
            else if (id == "edit") SeedEditRequested?.Invoke();
            else if (id == "about") AboutRequested?.Invoke();
            else if (id == "room") RoomRequested?.Invoke();
            else if (id == "stand") RecenterRequested?.Invoke();
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
            if (id == "back") { if (captionPage > 0) SetReadingPart(captionPage - 1); else BackRequested?.Invoke(); }
            else if (id == "next") { if (captionPage + 1 < captionPages.Count) SetReadingPart(captionPage + 1); else NextRequested?.Invoke(); }
            else if (id == "ask") AskRequested?.Invoke();
            else if (id == "context") ContextRequested?.Invoke();
            else if (id == "save") SaveRequested?.Invoke();
            else if (id == "sources") SourceDetailsRequested?.Invoke();
            else if (id == "again") ContinueRequested?.Invoke();
            else if (id == "reopen-closing") ReopenRequested?.Invoke();
        }
        void ControlAction(string id)
        {
            if (id == "pause") PauseRequested?.Invoke();
            else if (id == "context") ContextRequested?.Invoke();
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
            // A short preceding sentence is preferable to cutting the next sentence's emotional payoff in two.
            for (var i = fitted - 1; i >= 0; i--)
            {
                if (text[i] != '.' && text[i] != '?' && text[i] != '!') continue;
                var end = i + 1;
                while (end < fitted && (text[end] == '\'' || text[end] == '"' || text[end] == '’' || text[end] == '”')) end++;
                if (end == text.Length || char.IsWhiteSpace(text[end])) return end;
            }
            // A sentence longer than the viewport may still have a natural phrase break. Avoid leaving a few
            // final words on their own, especially with large text, without shrinking or dropping any words.
            if (text.Substring(fitted).Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length < 8)
                for (var i = fitted - 1; i >= fitted * .4f; i--)
                    if (text[i] == ',' || text[i] == ';' || text[i] == ':') return i + 1;
            return fitted;
        }

        void SetReadingPart(int index)
        {
            captionPage = Mathf.Clamp(index, 0, captionPages.Count - 1);
            // The page stirs when its words change, not when only the actions beneath them do.
            var sameWords = caption != null && caption.text == captionPages[captionPage];
            SetText(caption, captionPages[captionPage]);
            if (!closing) SetText(pageNumber, $"PAGE {chapterIndex + 1:00} / {chapterCount:00}" + (captionPages.Count > 1 ? $"   •   {captionPage + 1} / {captionPages.Count}" : ""));
            if (!sameWords) pageTurn = reducedMotion ? 0 : 1;
            RefreshControls();
        }

        void Update()
        {
            RefreshFigureChoices();
            RefreshGourdInvite();
            if (paused) return;
            CarryHeirloom();
            LiftHeirlooms();
            SwellBeads();
            if (popUp < 1)
            {
                popUp = reducedMotion ? 1 : Mathf.Min(1, popUp + Time.deltaTime / PopUpSeconds);
                ApplyDepth();
            }
            if (reducedMotion || pageTurn <= 0) return;
            pageTurn = Mathf.Max(0, pageTurn - Time.deltaTime / 0.38f);
            for (var i = 0; i < turningPages.Length; i++)
                if (turningPages[i] != null) turningPages[i].localRotation = restingPageRotations[i] * Quaternion.Euler(0, Mathf.Sin(pageTurn * Mathf.PI) * (i == 0 ? 3 : -2), 0);
        }
        /// <summary>The chosen heirloom rises in an arc to the fire and is taken in by it; the others wait for another visit.</summary>
        string[] choiceFigures = Array.Empty<string>();

        /// <summary>A figure's plaque is there only while she stands on the stage and a choice is open.</summary>
        void RefreshFigureChoices()
        {
            for (var f = 0; f < figureChoices.Length; f++)
            {
                if (figureChoices[f] == null) continue;
                var figure = f < TableStaging.Figures.Count ? TableStaging.Figures[f] : null;
                var open = spatialStageVisible && offeringChoice && !intake && !paused && !detailsOpen && !closing && !error && stage != null
                    && !stage.Turning && stage.FigureShown(figure) && Array.IndexOf(choiceFigures, figure) >= 0;
                if (figureChoices[f].Hidden == open) figureChoices[f].Hidden = !open;
            }
        }

        /// <summary>The invitation hangs just above the gourd while the visitor may set it down, and nowhere else.</summary>
        void RefreshGourdInvite()
        {
            if (gourdInvite == null || stage == null) return;
            var open = spatialStageVisible && stage.GourdInvited && !intake && !paused && !detailsOpen && !closing && !error;
            if (open) gourdInvite.transform.position = stage.GourdWorldPosition + TableRoot.up * 0.075f * TableRoot.lossyScale.y;
            if (gourdInvite.Hidden == open) gourdInvite.Hidden = !open;
        }

        void CarryHeirloom()
        {
            if (carried < 0 || carry >= 1 || heirlooms[carried] == null) return;
            carry = Mathf.Min(1, carry + Time.deltaTime / (reducedMotion ? 0.01f : 0.9f));
            var item = heirlooms[carried];
            var target = item.parent.InverseTransformPoint(FirePosition + Vector3.up * 0.04f);
            var eased = carry * carry * (3 - 2 * carry);
            var at = Vector3.Lerp(carryFrom, target, eased);
            at.y += Mathf.Sin(eased * Mathf.PI) * 0.12f;
            item.localPosition = at;
            item.localScale = Vector3.one * Mathf.Lerp(1, 0.25f, eased * eased);
            if (carry >= 1) item.gameObject.SetActive(false);
        }

        void ReturnHeirlooms()
        {
            carried = -1; carry = 1;
            for (var i = 0; i < heirlooms.Length; i++)
            {
                if (heirlooms[i] == null) continue;
                if (i < heirloomRest.Length) heirlooms[i].localPosition = heirloomRest[i];
                if (i < heirloomReach.Length) heirloomReach[i] = 0;
                heirlooms[i].localScale = Vector3.one; heirlooms[i].gameObject.SetActive(true);
            }
        }

        /// <summary>An heirloom that is waiting rises a little toward a hand that reaches for it, and settles when the hand goes.</summary>
        void LiftHeirlooms()
        {
            for (var i = 0; i < heirlooms.Length && i < heirloomRest.Length; i++)
            {
                if (heirlooms[i] == null || i == carried) continue;
                var lift = intake && !reducedMotion ? heirloomReach[i] * HeirloomLift : 0;
                var at = heirloomRest[i] + Vector3.up * lift;
                heirlooms[i].localPosition = Vector3.MoveTowards(heirlooms[i].localPosition, at, Time.deltaTime * 0.12f);
            }
        }

        void SwellBeads()
        {
            for (var i = 0; i < beads.Length && i < beadSwell.Length; i++)
            {
                if (beadSwell[i] <= 0 || beads[i] == null) continue;
                beadSwell[i] = Mathf.Max(0, beadSwell[i] - Time.deltaTime / BeadSwellSeconds);
                beads[i].transform.localScale = Vector3.one * BeadSize * (1 + 0.8f * Mathf.Sin(beadSwell[i] * Mathf.PI));
            }
        }

        void RestorePages()
        {
            if (restingPageRotations == null) return;
            for (var i = 0; i < turningPages.Length; i++) if (turningPages[i] != null) turningPages[i].localRotation = restingPageRotations[i];
        }
    }
}
