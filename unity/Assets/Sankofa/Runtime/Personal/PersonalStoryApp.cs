using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Sankofa.Core.Json;
using Sankofa.Core.Personal;
using Sankofa.Flow;
using Sankofa.Interaction;
using Sankofa.Runtime.App;
using Sankofa.Runtime.Input;
using Sankofa.Runtime.UI;
using Sankofa.Storage;
using UnityEngine;
using UnityEngine.Networking;

namespace Sankofa.Runtime.Personal
{
    /// <summary>Composition for personal intake, checked content, a reusable room-aware table and explicit Keep.</summary>
    [DefaultExecutionOrder(500)]
    public sealed partial class PersonalStoryApp : MonoBehaviour
    {
        [Tooltip("Public HTTPS service root. Never place provider credentials in this scene.")]
        [SerializeField] string serviceEndpoint = "";
        [Tooltip("Limited service access credential for a controlled build, never a Google key. APK contents can be extracted; rotate and limit server access.")]
        [SerializeField] string serviceAccessToken = "";
        [Tooltip("A prepared field recording of a small fire (Art/Recorded/sources.json); played on the headset only.")]
        [SerializeField] AudioClip fireRecording;
        [Tooltip("Prepared field recordings of a brook and of night insects, heard from the stage while a telling stands; headset only.")]
        [SerializeField] AudioClip riverRecording, nightRecording;
        StoryTableView view;
        StoryTablePlacement placement;
        StoryTableKeyboard keyboard;
        PersonalStoryStore store;
        PersonalStrandStore strands;
        KeptStrand strand = new KeptStrand();
        readonly PersonalStorySession session = new PersonalStorySession();
        string region = "enkare-nairobi", period = "1850s", interest = "trade-household", seed = "";
        UnityWebRequest pending;
        Action<string> decision;
        bool systemOwnsPause, reducedMotion;
        string narrationToken;
        AudioSource voice;
        Coroutine voiceRequest;
        bool narrationEnabled;
        UnityWebRequest pendingAudio;
        bool largeText;
        string personalReason;
        AudioSource fireSound, riverSound, nightSound;
        readonly float[] speech = new float[256];
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        public static string StoreFolderOverride;
        public PersonalStorySession Session => session;
        public StoryTableView View => view;
        public StoryTableKeyboard Keyboard => keyboard;
        public string Seed => seed;
        public bool HasKept { get { try { return store != null && store.HasKept; } catch { return false; } } }
        public string ServiceEndpoint
        {
            get => serviceEndpoint;
            set { serviceEndpoint = value ?? ""; if (view != null && session.State == PersonalStoryState.Intake && decision == null) Render(); }
        }
        /// <summary>The sides of tellings this visitor has kept: one bead each, at most six.</summary>
        public KeptStrand Strand => strand;
        [Tooltip("Offers Create my story at the intake when a story service is configured. Turn off for a build whose service only answers questions.")]
        [SerializeField] bool onlineCreation = true;
        public bool OnlineCreation { get => onlineCreation; set { onlineCreation = value; if (view != null && session.State == PersonalStoryState.Intake && decision == null) Render(); } }
        public bool OnlineAvailable => onlineCreation && Endpoint() != null;
        public string PersonalReason => personalReason;
        public string ServiceAccessToken { set => serviceAccessToken = value ?? ""; }
        public bool NarrationEnabled => narrationEnabled;

        /// <summary>
        /// The headset presentation: the griot speaks, welcomes the visitor and turns the pages. Editor, simulator and
        /// batch sessions start silent and hand-turned unless SANKOFA_PRESENT=1 asks for the presentation, as when
        /// recording the simulator.
        /// </summary>
        static bool Presenting => !Application.isEditor || Environment.GetEnvironmentVariable("SANKOFA_PRESENT") == "1";

        /// <summary>What a first-time visitor hears and reads before choosing, ending on the invitation that stays.</summary>
        public static readonly string[] FirstWelcome = PersonalStoryVoice.Welcome.ToArray();
        /// <summary>What a visitor with a kept telling hears instead.</summary>
        public static readonly string[] ReturnWelcome = PersonalStoryVoice.ReturnWelcome.ToArray();
        const float TurnAfterSeconds = 1.6f, ChoiceWaitSeconds = 14f;
        /// <summary>How long the fire waits, once one side is told, before it turns the stage to the other itself.</summary>
        public const float OtherSideWaitSeconds = 10f;
        /// <summary>How long a told page that has questions waits for one before it turns.</summary>
        public const float QuestionWaitSeconds = 5f;
        /// <summary>What the page adds once one side of a meeting is told. Presentation: never part of the story or its narration.</summary>
        public const string OtherSidePrompt = "This meeting has another side. Sweep a hand across the stage to turn it.";
        /// <summary>What ending a telling after one side is called.</summary>
        public const string EndHereLabel = "End here";
        string[] welcome;
        int welcomeLine = -1;
        float welcomeClock, welcomeLength, pageRest;
        bool pageSpoken, pageTold, askingForHand, voiceHeld;
        string turnKey, toldPage, pageKey;
        int pageKeyVersion, pageKeyNumber;
        PersonalStoryChapter pageKeyChapter;
        TurnGesture[] turns = Array.Empty<TurnGesture>();
        TouchGesture[,] reaches = new TouchGesture[0, 0];
        /// <summary>
        /// One side of the meeting has been told and the other may be heard now: at its figure, by a hand sweeping the
        /// stage round, or left to the fire. Hand-turned pages offer it as soon as the side's page opens.
        /// </summary>
        public bool OtherSideOffered => session.OffersOtherSide && (!TurnsPages || PageToldOnce);
        /// <summary>
        /// The page being read has been told to its end at least once. Where pages turn themselves, what a page offers
        /// beyond its telling waits for this: its questions, which would otherwise say what the page has not yet said,
        /// and the other side of its meeting. Coming back from an answer does not tell the page again.
        /// </summary>
        public bool PageToldOnce => toldPage != null && toldPage == PageKey();
        /// <summary>The page's own questions may be asked now.</summary>
        public bool QuestionsOffered => session.State == PersonalStoryState.Reading && (!TurnsPages || PageToldOnce);

        /// <summary>Names the page being read, or null between pages. Built once per page: it is asked for every frame.</summary>
        string PageKey()
        {
            if (session.State != PersonalStoryState.Reading || session.Chapter == null) return null;
            var chapter = session.Chapter;
            if (pageKey == null || pageKeyVersion != session.RequestVersion || pageKeyNumber != session.PageNumber || !ReferenceEquals(pageKeyChapter, chapter))
            {
                pageKeyVersion = session.RequestVersion; pageKeyNumber = session.PageNumber; pageKeyChapter = chapter;
                pageKey = pageKeyVersion + ":" + pageKeyNumber + ":" + chapter.Id;
            }
            return pageKey;
        }
        /// <summary>The griot is still saying the welcome; the heirlooms can be chosen throughout.</summary>
        public bool Welcoming => welcome != null;
        /// <summary>The welcome line now on the page, or null once the standing invitation has returned.</summary>
        public string WelcomeLine => welcome != null && welcomeLine >= 0 && welcomeLine < welcome.Length ? welcome[welcomeLine] : null;
        /// <summary>
        /// A page turns itself once the griot has finished it, and an unanswered choice is left to the fire. The
        /// visitor can still turn sooner, go back or pause. On by default wherever the headset presentation is.
        /// </summary>
        public bool TurnsPages
        {
            get => turnsPages;
            set { turnsPages = value; if (view != null) view.SetSelfTurning(value); }
        }
        bool turnsPages;
        HandSource[] hands;
        float handWarmth;
        const float HandWarmthMetres = 0.26f;
        /// <summary>A hand closes on an heirloom within this distance of its middle, held for a moment.</summary>
        static readonly TouchSettings HeirloomTouch = new TouchSettings { Radius = 0.07f, Dwell = 0.2, FeedbackRange = 0.30f };

        void Awake()
        {
            view = FindFirstObjectByType<StoryTableView>(FindObjectsInactive.Include);
            if (view == null) { enabled = false; return; }
            var oldAnchor = FindFirstObjectByType<WorldAnchor>();
            if (oldAnchor != null) oldAnchor.enabled = false; // This scene has one placement owner.
            placement = gameObject.AddComponent<StoryTablePlacement>();
            keyboard = gameObject.AddComponent<StoryTableKeyboard>();
            keyboard.Configure(view, view.StylePanel);
            var panels = view.Panels.Concat(new[] { keyboard.Panel }).ToArray();
            hands = FindObjectsByType<HandSource>(FindObjectsSortMode.None);
            turns = hands.Select(_ => new TurnGesture()).ToArray();
            reaches = new TouchGesture[hands.Length, view.HeirloomCount];
            for (var h = 0; h < hands.Length; h++) for (var i = 0; i < view.HeirloomCount; i++) reaches[h, i] = new TouchGesture(HeirloomTouch);
            foreach (var panel in panels) foreach (var hand in hands) { var source = hand; panel.AddFingertip(() => source.Fingertip); }
            foreach (var pointer in FindObjectsByType<PointerSource>(FindObjectsSortMode.None)) pointer.ConfigurePanels(panels);
            var folder = StoreFolderOverride ?? Path.Combine(Application.persistentDataPath, "personal-story");
            store = new PersonalStoryStore(folder);
            strands = new PersonalStrandStore(folder);
            strand = strands.Load();
            // On the headset the griot reads aloud by default, beside full captions. Editor, simulator and batch sessions
            // start silently; Options switches either way.
            narrationEnabled = Presenting;
            TurnsPages = Presenting;
            voice = gameObject.AddComponent<AudioSource>(); voice.playOnAwake = false; voice.volume = narrationEnabled ? .85f : 0; voice.spatialBlend = .75f;
            view.StartLocalRequested += StartPrepared;
            view.StartRequested += StartGenerated;
            view.RouteSelected += id => interest = id;
            view.RegionSelected += id => region = id;
            view.PeriodSelected += id => period = id;
            view.SeedEditRequested += EditSeed;
            view.NextRequested += () => { StopVoice(); session.Next(); };
            view.BackRequested += () => { StopVoice(); session.Back(); };
            view.QuestionSelected += id => { if (int.TryParse(id, out var index)) { StopVoice(); session.Ask(index); } };
            view.ChoiceSelected += id =>
            {
                if (!int.TryParse(id, out var index)) return;
                if (session.OffersChoice) { StopVoice(); session.Choose(index); }
                else if (OtherSideOffered) HearOtherSide(index, true, 0);
            };
            view.AboutRequested += ShowAbout;
            view.AskRequested += AskTheFire;
            view.SourceDetailsRequested += ShowSources;
            view.SaveRequested += RequestKeep;
            view.PauseRequested += () => session.Pause(!session.Paused);
            view.RecenterRequested += () => placement.Recenter();
            view.RoomRequested += () => placement.FitRoomTable();
            view.ReopenRequested += () => Reopen();
            view.ForgetRequested += Forget;
            view.ComfortRequested += ShowOptions;
            view.ContinueRequested += Continue;
            view.RetryRequested += StartGenerated;
            view.LeaveRequested += RequestLeave;
            view.DecisionSelected += id => { var action = decision; decision = null; askingForHand = false; action?.Invoke(id); };
            keyboard.Edited += value => view.ShowDecision(value.Length >= StoryTableKeyboard.MaximumLength ? "Limit reached • 500 / 500" : $"Your question • {value.Length} / 500", (value.Length == 0 ? "What would you like to understand?" : value) + "\n\nNothing is sent while you type. Delete text to make room if you reach the limit.", Array.Empty<(string, string)>());
            keyboard.Committed += value => { seed = value; decision = null; session.Cancel(); };
            keyboard.Cancelled += () => { decision = null; Render(); };
            session.Changed += Render;
            placement.StatusChanged += message =>
            {
                view.SetOwnTable(placement.UsesSurface);
                if (session.State == PersonalStoryState.Intake) view.SetStatus(message);
                else if (session.State == PersonalStoryState.Reading) view.SetStatus(Capability(session.Story) + " • " + message);
            };
            placement.HandAsked += AskForARestingHand;
            placement.HandSettled += () => { view.SetOwnTable(placement.UsesSurface); if (askingForHand) { askingForHand = false; decision = null; Render(); } };
            OVRManager.HMDUnmounted += OnFocusLost;
            OVRManager.InputFocusLost += OnFocusLost;
            OVRManager.HMDMounted += OnFocusRestored;
            OVRManager.InputFocusAcquired += OnFocusRestored;
        }

        void Start()
        {
            if (view == null) return;
            var eye = Camera.main;
            placement.Configure(view, eye != null ? eye.transform : null, hands);
            reducedMotion = ExperienceSettings.Load().ReducedMotion;
            view.SetReducedMotion(reducedMotion);
            StartFireSound();
            Render();
            if (Presenting) BeginWelcome();
        }

        /// <summary>Begins the spoken welcome on the intake. A first visit and a return are greeted differently.</summary>
        public void BeginWelcome()
        {
            if (view == null || session.State != PersonalStoryState.Intake) return;
            welcome = strand.Count > 0 ? PersonalStoryVoice.ReturnWelcomeFor(strand).ToArray() : HasKept ? ReturnWelcome : FirstWelcome;
            welcomeLine = -1;
            NextWelcomeLine();
        }

        /// <summary>
        /// Back at the fire after an ending, the griot names a side the strand has not yet threaded and the heirloom
        /// that leads to it, in the words and recordings of the return welcome. Nothing is said to a visitor who has
        /// kept nothing, or everything.
        /// </summary>
        public void SuggestNext()
        {
            if (view == null || session.State != PersonalStoryState.Intake) return;
            var next = strand.Count > 0 ? strand.NextUnheard() : null;
            var side = next == null ? null : KeptStrand.Sides(next.Telling).FirstOrDefault(s => s.Id == next.Side);
            if (side == null) return;
            welcome = new[] { PersonalStoryVoice.ReturnInvitation(next.Telling, side.Label) };
            welcomeLine = -1;
            NextWelcomeLine();
        }

        void NextWelcomeLine()
        {
            welcomeLine++;
            if (welcome == null || welcomeLine >= welcome.Length) { EndWelcome(); return; }
            var line = welcome[welcomeLine];
            var clip = narrationEnabled ? Resources.Load<AudioClip>("Narration/personal-table/" + PersonalStoryVoice.ClipName(line)) : null;
            var words = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Length;
            // A recorded line holds for its own length; an unrecorded one for long enough to be read.
            welcomeLength = clip != null ? clip.length + .6f : Mathf.Max(2.6f, words / 2.6f + 1.2f);
            welcomeClock = 0;
            view.SetInvitation(line);
            if (clip != null) PlayClip(clip);
        }

        void EndWelcome()
        {
            if (welcome == null) return;
            welcome = null; welcomeLine = -1;
            view.SetInvitation(null);
        }

        void TellWelcome()
        {
            if (welcome == null) return;
            if (session.State != PersonalStoryState.Intake) { EndWelcome(); return; }
            if (session.Paused || decision != null || keyboard.IsOpen) return;
            welcomeClock += Time.deltaTime;
            if (welcomeClock >= welcomeLength) NextWelcomeLine();
        }

        /// <summary>
        /// Turns the page once it has been told: when its recording ends, or, without a voice, when the stage has
        /// finished performing it. A page that offers a choice waits longer, then the fire chooses.
        /// </summary>
        void TurnPageWhenTold()
        {
            var key = PageKey();
            var reading = key != null;
            if (key != turnKey) { turnKey = key; pageRest = 0; pageTold = pageSpoken = false; askWhenTold = false; }
            if (!reading || session.Paused || decision != null || keyboard.IsOpen) { pageRest = 0; return; }
            var speaking = voice != null && voice.isPlaying;
            if (pageSpoken && !speaking && voice.clip != null && voice.time <= 0.01f)
            {
                // The recording has ended: the stage and captions reach the end of the page with it.
                view.SetNarrationProgress(voice.clip.length, voice.clip.length);
                pageSpoken = false; pageTold = true;
            }
            var stage = view.Stage;
            var voiced = voice != null && voice.clip != null;
            if (TurnsPages && !voiced && voiceRequest == null) view.FollowStageClock();
            var told = !speaking && voiceRequest == null
                && (voiced ? pageTold : stage != null && stage.Progress >= 1f)
                && (stage == null || (!stage.Turning && !stage.Waiting));
            if (!told) { pageRest = 0; return; }
            // A question held until the page was told is heard now, before any page turns.
            if (askWhenTold) { askWhenTold = false; pageRest = 0; BeginSpokenQuestion(); return; }
            if (toldPage != key)
            {
                // The page is told. What it may now offer appears: its questions, and the other side of its meeting.
                // Its last words stay in view and the fire turns to the visitor.
                toldPage = key; pageRest = 0;
                if (TurnsPages)
                {
                    Render(); view.ShowLastReadingPart();
                    if (session.OffersOtherSide || HasQuestions) view.SetFireState(Sankofa.Core.Story.FireState.Listening);
                }
                return;
            }
            if (!TurnsPages) return;
            pageRest += Time.deltaTime;
            var wait = session.OffersChoice ? ChoiceWaitSeconds : session.OffersOtherSide ? OtherSideWaitSeconds : HasQuestions ? QuestionWaitSeconds : TurnAfterSeconds;
            if (pageRest < wait) return;
            pageRest = 0;
            // Left alone, the fire turns the stage itself, so a visitor who only watches still hears both sides.
            if (session.OffersOtherSide) { HearOtherSide(0, false, 0); return; }
            StopVoice(); session.Next();
        }

        /// <summary>The page being read has something a visitor could ask.</summary>
        bool HasQuestions => session.Chapter != null && (session.Chapter.Questions.Count > 0 || SpokenQuestionsAvailable);

        /// <summary>
        /// Turns the stage to the side of the meeting not yet heard. <paramref name="chosen"/> records whether the
        /// visitor asked for it; <paramref name="direction"/> is the way their hand swept, or 0.
        /// </summary>
        void HearOtherSide(int index, bool chosen, int direction)
        {
            if (!session.OffersOtherSide || session.Paused) return;
            if (direction != 0 && view.Stage != null) view.Stage.TurnToward(direction);
            StopVoice();
            session.HearOtherSide(index, chosen);
        }

        /// <summary>
        /// While the other side is offered, a hand swept sideways in front of the stage turns it. The stage leans with
        /// the hand first, so the visitor feels it give before it turns.
        /// </summary>
        void TurnByHand()
        {
            var stage = view.Stage;
            if (stage == null) return;
            var armed = OtherSideOffered && session.State == PersonalStoryState.Reading && !session.Paused && decision == null && !keyboard.IsOpen && stage.Standing;
            if (!armed)
            {
                foreach (var turn in turns) turn.Reset();
                stage.SetLean(0);
                return;
            }
            var lean = 0f;
            var table = stage.transform.parent;
            for (var i = 0; i < hands.Length; i++)
            {
                var tracked = hands[i] != null && hands[i].Tracked;
                // The hand in the stage's own space, taken from its parent so the stage's lean never feeds back into it.
                var palm = tracked ? (table != null ? table.InverseTransformPoint(hands[i].PalmCenter) : hands[i].PalmCenter) - stage.transform.localPosition : Vector3.zero;
                var swept = turns[i].Update(tracked, new System.Numerics.Vector3(palm.x, palm.y, palm.z), Time.deltaTime);
                if (swept != 0) { stage.SetLean(0); HearOtherSide(0, true, swept); return; }
                if (Mathf.Abs(turns[i].Lean) > Mathf.Abs(lean)) lean = turns[i].Lean;
            }
            stage.SetLean(lean);
        }

        /// <summary>
        /// A hand that reaches an heirloom itself, rather than its card, begins that telling. The heirloom rises a little
        /// to meet the hand first.
        /// </summary>
        void ReachForHeirlooms()
        {
            var offered = view.HeirloomsOffered && session.State == PersonalStoryState.Intake && !session.Paused && decision == null && !keyboard.IsOpen;
            for (var i = 0; i < view.HeirloomCount; i++)
            {
                var nearness = 0f; var taken = false;
                var middle = view.HeirloomPosition(i);
                for (var h = 0; h < hands.Length; h++)
                {
                    var touch = reaches[h, i];
                    if (!offered || hands[h] == null || !hands[h].Tracked) { touch.Reset(); continue; }
                    taken |= touch.Update(hands[h].Sample, new System.Numerics.Vector3(middle.x, middle.y, middle.z), Time.deltaTime);
                    nearness = Mathf.Max(nearness, touch.Proximity);
                }
                view.SetHeirloomReach(i, nearness);
                if (!taken) continue;
                foreach (var touch in reaches) touch.Reset();
                view.ChooseHeirloom(i);
                return;
            }
        }

        /// <summary>The visitor is asked to rest a hand on their own table; the room is showing so they can see it.</summary>
        void AskForARestingHand()
        {
            StopVoice();
            Decide("Use your own table",
                "Reach forward and rest an open hand on your table. Hold it still for a moment and the story will stand there.\n\nOnly your hand's position is read, on this headset.",
                new[] { ("stand", "Keep the story stand") }, _ => { placement.Recenter(); Render(); });
            askingForHand = true;
        }

        /// <summary>The fire is heard where it burns. Headset only, like the voice; the tabletop view itself creates no sound.</summary>
        void StartFireSound()
        {
            if (!Presenting) return;
            fireSound = Loop("Fire sound", fireRecording, .28f, view.FirePosition);
            // The stage's water and night are silent until a telling stands, and step back while the griot speaks.
            riverSound = Loop("Stage river", riverRecording, 0, view.StagePosition);
            nightSound = Loop("Stage night", nightRecording, 0, view.StagePosition);
        }

        AudioSource Loop(string name, AudioClip clip, float volume, Vector3 at)
        {
            if (clip == null) return null;
            var source = new GameObject(name);
            source.transform.SetParent(transform, false);
            var sound = source.AddComponent<AudioSource>();
            sound.clip = clip; sound.loop = true; sound.playOnAwake = false;
            sound.spatialBlend = .9f; sound.volume = volume; sound.dopplerLevel = 0;
            sound.rolloffMode = AudioRolloffMode.Logarithmic; sound.minDistance = .6f; sound.maxDistance = 12f;
            sound.transform.position = at;
            sound.timeSamples = UnityEngine.Random.Range(0, clip.samples);
            sound.Play();
            return sound;
        }

        void StageSound(AudioSource sound, float full, bool speaking)
        {
            if (sound == null) return;
            var standing = view.Stage != null ? view.Stage.Rise : 0;
            var target = full * standing * (speaking ? .5f : 1);
            sound.volume = Mathf.MoveTowards(sound.volume, target, Time.deltaTime * .25f);
            sound.transform.position = view.StagePosition;
        }

        void Update()
        {
            if (view == null) return;
            var level = 0f;
            if (voice != null && voice.isPlaying)
            {
                voice.GetOutputData(speech, 0);
                var sum = 0f;
                for (var i = 0; i < speech.Length; i++) sum += speech[i] * speech[i];
                level = Mathf.Clamp01(Mathf.Sqrt(sum / speech.Length) * 6f);
            }
            view.SetSpeechLevel(level);
            if (voice != null && voice.isPlaying && voice.clip != null) view.SetNarrationProgress(voice.time, voice.clip.length);
            if (fireSound != null) fireSound.transform.position = view.FirePosition;
            var speaking = voice != null && voice.isPlaying;
            StageSound(riverSound, .16f, speaking);
            StageSound(nightSound, .09f, speaking);
            TellWelcome();
            TurnPageWhenTold();
            TurnByHand();
            ReachForHeirlooms();
            WarmToHands();
            TickVoice();
        }

        /// <summary>A hand held to the fire makes it swell and throw sparks: the first thing a visitor's hands do here.</summary>
        void WarmToHands()
        {
            handWarmth -= Time.deltaTime;
            if (handWarmth > 0 || hands == null || session.Paused) return;
            var flame = view.FirePosition + Vector3.up * 0.14f;
            foreach (var hand in hands)
            {
                if (hand == null || !hand.Tracked || (hand.PalmCenter - flame).sqrMagnitude > HandWarmthMetres * HandWarmthMetres) continue;
                view.AcknowledgeHand(); handWarmth = 0.9f;
                return;
            }
        }

        /// <summary>A telling is beginning or being reopened: no page of it has been told yet.</summary>
        void ResetReadingPosition() { toldPage = null; view.ResetReadingPosition(); }

        public void SetSeed(string question) { seed = question ?? ""; view.SetSeedText(seed); }
        PersonalStoryRequest Request() => new PersonalStoryRequest(region, period, interest, question: seed);

        public void StartPrepared()
        {
            if (session.Paused) return;
            AbortPending(); StopVoice(); ClearClips();
            keyboard.Close(); decision = null; narrationToken = null; storyToken = null; ResetReadingPosition();
            var request = Request(); var version = session.Begin(request);
            if (session.State != PersonalStoryState.Preparing) return;
            var result = BundledPersonalStoryComposer.Compose(request);
            personalReason = string.IsNullOrWhiteSpace(seed) ? null : result.Reason;
            if (result.Success) session.Accept(version, result.Story); else session.Fail(version, result.Message);
        }

        public void StartGenerated()
        {
            if (session.Paused) return;
            AbortPending(); StopVoice(); ClearClips(); keyboard.Close(); decision = null; narrationToken = null; ResetReadingPosition();
            personalReason = null; storyToken = null;
            var input = Request(); var version = session.Begin(input);
            if (session.State != PersonalStoryState.Preparing) return;
            var endpoint = Endpoint();
            if (endpoint == null) { session.Fail(version, "The online storyteller is not connected. Your prepared story is ready below."); return; }
            StartCoroutine(Generate(version, input, endpoint));
        }

        string Endpoint()
        {
            var value = serviceEndpoint;
#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(value)) value = Environment.GetEnvironmentVariable("SANKOFA_STORY_SERVICE") ?? "http://127.0.0.1:8787";
#endif
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)) return null;
            if (uri.Scheme != "https" && !(Application.isEditor && uri.IsLoopback && uri.Scheme == "http")) return null;
            if (!string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)) return null;
            return uri.AbsoluteUri.TrimEnd('/');
        }

        void Authorize(UnityWebRequest request)
        {
            var token = serviceAccessToken;
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(token)) token = Environment.GetEnvironmentVariable("SANKOFA_STORY_ACCESS_TOKEN");
#endif
            // A client credential is distinct from server-side Google credentials and is never logged.
            if (!string.IsNullOrEmpty(token) && token.Length <= 512 && token.All(c => c >= '!' && c <= '~'))
                request.SetRequestHeader("Authorization", "Bearer " + token);
        }

        IEnumerator Generate(int version, PersonalStoryRequest input, string endpoint)
        {
            using (var call = new UnityWebRequest(endpoint + "/create-story", "POST"))
            {
                pending = call;
                call.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(PersonalStoryJson.WriteRequest(input)));
                call.downloadHandler = new DownloadHandlerBuffer(); call.timeout = 35; call.redirectLimit = 0;
                call.SetRequestHeader("Content-Type", "application/json");
                Authorize(call);
                yield return call.SendWebRequest();
                if (pending == call) pending = null;
                if (version != session.RequestVersion || session.State != PersonalStoryState.Preparing) yield break;
                if (call.result != UnityWebRequest.Result.Success || call.downloadedBytes > PersonalStoryValidator.MaxJsonLength * 4)
                { session.Fail(version, "The storyteller could not finish this request. Try again or read the prepared story."); yield break; }
                PersonalStoryDocument story = null;
                try { story = PersonalStoryJson.ReadValidated(call.downloadHandler.text, input); }
                catch (Exception) { session.Fail(version, "This telling could not be checked. Try again or read the prepared story."); }
                if (story != null && session.Accept(version, story))
                {
                    narrationToken = call.GetResponseHeader("X-Sankofa-Narration-Token");
                    storyToken = call.GetResponseHeader("X-Sankofa-Story-Token");
                    if (story.Mode == "bundled")
                    {
                        if (!string.IsNullOrWhiteSpace(seed)) personalReason = BundledPersonalStoryComposer.Compose(input).Reason;
                        view.SetStatus("Prepared story • online creation was unavailable");
                    }
                    if (narrationEnabled) SpeakCurrent();
                }
            }
        }

        void Render()
        {
            if (view == null) return;
            view.SetStrand(strand.Beads.Select(b => b.Telling).ToArray());
            view.SetPaused(session.Paused); keyboard.Suspend(session.Paused);
            if (session.Paused)
            {
                // Only a voice that is speaking is held, so a page that has finished is never started again by a resume.
                if (voice.isPlaying) { voice.Pause(); voiceHeld = true; }
                foreach (var s in new[] { fireSound, riverSound, nightSound }) if (s != null) s.Pause();
                return;
            }
            if (voiceHeld) { voiceHeld = false; if (voice.clip != null && narrationEnabled && !voice.isPlaying) voice.UnPause(); }
            foreach (var s in new[] { fireSound, riverSound, nightSound }) if (s != null && !s.isPlaying) s.UnPause();
            if (decision != null || keyboard.IsOpen) return;
            var story = session.Story;
            switch (session.State)
            {
                case PersonalStoryState.Intake:
                    view.RenderStory("Sankofa", "For the stories you never got to hear", placement.Status);
                    view.SetIntakeSelection(interest, region, period); view.SetSeedText(seed); view.SetOnlineAvailable(OnlineAvailable); view.ShowIntake();
                    view.SetHasKeptStory(HasKept);
                    view.SetControlState(false, false, false, false);
                    break;
                case PersonalStoryState.Preparing: view.ShowPreparing("Listening to your question, gathering the next pages…"); break;
                case PersonalStoryState.Error: view.ShowError(session.Problem); break;
                case PersonalStoryState.Reading:
                    // The first page says what the visitor's own words changed; later pages return to the setting.
                    view.RenderStory(story.Title, session.PageNumber == 1 && personalReason != null ? personalReason : "Enkare Nairobi • around the 1850s", Capability(story));
                    // A page offers the sides of its meeting to follow, or, once one side is told, the side still unheard.
                    var otherSide = OtherSideOffered;
                    var sides = session.OffersChoice ? session.Chapter.Choices : otherSide ? session.NotFollowed : Array.Empty<PersonalStoryChoice>();
                    view.RenderChapter(new StoryTableChapterViewData
                    {
                        Index = session.PageNumber - 1, Count = session.PageCount, Id = session.Chapter.Id, Title = session.Chapter.Title,
                        Caption = PersonalStoryVoice.Spoken(session.Chapter.Narration), ArtKey = session.Chapter.Illustration,
                        ImageLabel = TruthLabel(session.Chapter.Truth),
                        PreviousPageTitles = session.PagesBefore.Select(c => c.Title).TakeLast(2).ToArray(),
                        Questions = QuestionsOffered ? session.Chapter.Questions.Select(q => q.Label).ToArray() : Array.Empty<string>(),
                        Choices = sides.Select(c => c.Label).ToArray(),
                        ChoiceIds = sides.Select(c => c.Id).ToArray(),
                        ChoicePrompt = otherSide ? OtherSidePrompt : null,
                        ContinueLabel = otherSide ? EndHereLabel : null,
                    });
                    view.SetSpokenQuestions(SpokenQuestionsAvailable);
                    view.SetControlState(session.PageNumber > 1, true, QuestionsOffered && HasQuestions, false);
                    // A page already told is not told again when the visitor comes back to it from an answer or a notice.
                    if (narrationEnabled && voice.clip == null && voiceRequest == null && !PageToldOnce) SpeakCurrent();
                    break;
                case PersonalStoryState.Question:
                    view.RenderQuestion(session.Question.Label, PersonalStoryVoice.Spoken(session.Question.Answer), TruthLabel(session.Question.Truth)); break;
                case PersonalStoryState.Sources:
                    view.ShowSourceDetails("What this telling knows", SourcesText()); break;
                case PersonalStoryState.Closing:
                    view.ShowClosing(Followed(story) + "\n\n" + KeepInvitation(story));
                    view.SetControlState(true, false, false, true); break;
            }
        }

        static string TruthLabel(string truth) => truth == "historical" ? "Documented context • sources available" : truth == "personal-account" ? "Your account • not independently verified" : "Imagined scene";
        string Capability(PersonalStoryDocument story) => session.IsReplay
            ? "Kept telling • originally " + (story.Mode == "generated" ? "generated" : "prepared") + " • no new generation"
            : story.Mode == "generated" ? "Generated telling • prepared illustrations" : "Prepared story • works offline";
        static string ReturnInvitation(string thread) => thread == "trade-household"
            ? "What stayed at home while the gourd travelled? A question about household life can begin a different telling."
            : thread == "place-water"
                ? "Who followed the path beside the water? A question about journeys can follow the people next."
                : "What did the travellers bring to their meeting? A question about trade can follow what they carried.";
        /// <summary>
        /// Names what was heard: both sides of the meeting, or the one side followed, honestly distinguishing the
        /// visitor's choice from the fire's default, with the side still unheard.
        /// </summary>
        string Followed(PersonalStoryDocument story)
        {
            var followed = session.Followed;
            if (followed == null) return ReturnInvitation(story.Interest);
            if (session.Heard.Count > 1 && session.NotFollowed.Count == 0) return "You heard both sides of this meeting.";
            var text = (session.FollowedByChoice ? "You chose to " : "The fire chose to ") + Lower(followed.Label) + ".";
            if (session.NotFollowed.Count > 0) text += " Another time, you could " + Lower(session.NotFollowed[0].Label) + ".";
            return text;
        }
        /// <summary>What keeping would do, said before the visitor decides. Saving stays an explicit choice.</summary>
        string KeepInvitation(PersonalStoryDocument story)
        {
            var beads = BeadsToThread(story).Count;
            return beads == 0 ? "Keep these exact pages on this device, or leave them unsaved."
                : "Keep this telling to thread " + (beads == 1 ? "its bead" : "both beads") + " on your strand, or leave it unsaved.";
        }
        /// <summary>The sides heard in this sitting that a prepared telling can thread as beads, whether or not they are on the strand already.</summary>
        List<PersonalStoryChoice> BeadsToThread(PersonalStoryDocument story) =>
            story == null || story.Mode != "bundled" ? new List<PersonalStoryChoice>()
                : session.Heard.Where(side => side != null && KeptStrand.IsKnown(story.Interest, side.Id)).ToList();
        static string Lower(string label) => string.IsNullOrEmpty(label) ? "" : char.ToLowerInvariant(label[0]) + label.Substring(1);

        void ShowAbout()
        {
            if (session.Paused) return;
            StopVoice();
            Decide("How it works",
                "Touch an heirloom to begin its telling. If you add a question first, recognised words in it choose which telling the fire begins.\n\n" +
                "Each telling reaches a meeting with two sides. You choose whose side to follow, then sweep a hand across the stage to turn it and hear the other. If you only watch, the fire chooses and turns the stage for you.\n\n" +
                "Documented pages are marked and sourced. The people and their meetings are imagined: they are not an account of anyone's ancestors, and nothing here recovers a real relative.\n\n" +
                "At the end you can keep a telling on this device and reopen it exactly, without a connection. Each side you keep threads a bead on your strand.\n\n" +
                "Use my table sets the story on your own table: one found in your room scan, or wherever you rest a hand. Nothing about your room leaves the headset.\n\n" +
                "Sounds from Freesound: \u201ccampfire sound\u201d by SterckxS (CC BY 3.0); \u201cBabbling Brook HQ Seamless Wav Loop\u201d by celldroid (CC BY 4.0); \u201ccrickets\u201d by selcukartut, recorded in the Maasai Mara (CC0). The narrator is a synthetic voice.",
                new[] { ("return", "Return") }, _ => Render());
        }

        string SourcesText()
        {
            var references = session.State == PersonalStoryState.Sources && session.Question != null ? session.Question.Sources : session.Chapter.Sources;
            var names = session.Story.Sources.Where(s => references.Contains(s.Id)).Select(s => s.Id + " • "
                + (s.Id == "C6" ? "The Origins of the Kikuyu Land Problem (1988)" : s.Title) + "\n" + s.Url);
            var why = personalReason != null && !session.IsReplay ? "\n\nWhy this telling: " + personalReason : "";
            return TruthLabel(session.Question != null ? session.Question.Truth : session.Chapter.Truth) + "\n\n" + string.Join("\n\n", names) + why + "\n\nThe illustrations are contemporary reconstructions. This is one researched setting; cultural and pronunciation review remain open.";
        }

        void EditSeed()
        {
            if (session.Paused) return;
            AbortPending(); keyboard.Open(seed);
        }
        void Continue()
        {
            if (keyboard.IsOpen) { keyboard.Close(); Render(); return; }
            if (session.ReturnToStory()) return;
            var fromEnding = session.State == PersonalStoryState.Closing;
            AbortPending(); StopVoice(); session.Cancel();
            if (fromEnding && Presenting) SuggestNext();
        }
        void ShowSources() { StopVoice(); session.Sources(); }
        void ShowOptions()
        {
            StopVoice();
            Decide("Make yourself comfortable", "These choices affect presentation. They do not change or save your story.",
                new[] { ("motion", reducedMotion ? "Gentle motion on" : "Still pages"), ("text", largeText ? "Standard text" : "Larger text"),
                    ("listen", narrationEnabled ? "Captions only" : "Read aloud"), ("lower", "Lower the stand"), ("raise", "Raise the stand"),
                    ("recenter", "Recenter"), ("room", "Use my table"), ("return", "Return") }, id =>
                {
                    if (id == "motion") { reducedMotion = !reducedMotion; view.SetReducedMotion(reducedMotion); }
                    else if (id == "text") { largeText = !largeText; view.SetLargeText(largeText); }
                    else if (id == "lower") placement.AdjustHeight(-.06f);
                    else if (id == "raise") placement.AdjustHeight(.06f);
                    else if (id == "listen") { narrationEnabled = !narrationEnabled; voice.volume = narrationEnabled ? .85f : 0; }
                    else if (id == "recenter") placement.Recenter();
                    else if (id == "room") placement.FitRoomTable();
                    Render();
                });
        }
        void Decide(string title, string text, IReadOnlyList<(string Id, string Label)> actions, Action<string> onAnswer)
        {
            // Any other notice takes the place of the request to rest a hand; a hand that then settles closes nothing.
            askingForHand = false;
            decision = onAnswer; view.ShowDecision(title, text, actions);
        }

        void RequestKeep()
        {
            if (session.State != PersonalStoryState.Closing || session.Paused) return;
            if (HasKept)
                Decide("Replace your kept story?", "Only one story is kept on this device. The new telling will replace the earlier one.", new[] { ("replace", "Replace kept story"), ("cancel", "Keep the earlier story") }, id => { if (id == "replace") Keep(); else Render(); });
            else Keep();
        }
        void Keep()
        {
            try
            {
                store.Keep(session.Story);
                var bead = ThreadBead();
                view.SetStrand(strand.Beads.Select(b => b.Telling).ToArray());
                view.ShowClosing("Kept on this device. " + bead + "You can reopen the same telling or forget it from your story table.");
            }
            catch (Exception) { view.ShowClosing("This story could not be kept. It is still open; try Keep again or leave without saving."); }
            view.SetControlState(true, false, false, true);
        }
        /// <summary>
        /// Keeping a prepared telling threads a bead for each side heard in this sitting, whoever chose it. A side
        /// already on the strand adds nothing. A generated telling has no sides and no bead.
        /// </summary>
        string ThreadBead()
        {
            var story = session.Story; var sides = BeadsToThread(story);
            if (sides.Count == 0) return "";
            try
            {
                var added = false;
                foreach (var side in sides) added |= strand.Add(story.Interest, side.Id);
                if (added) strands.Save(strand);
            }
            catch (Exception) { strand = strands.Load(); return "Its bead could not be threaded this time. "; }
            if (strand.IsComplete) return "Your strand now holds all six sides. ";
            return (sides.Count > 1 ? "Both sides are threaded on your strand: " : "A bead is threaded for this side: ") + strand.Count + " of " + KeptStrand.Capacity + ". ";
        }
        public bool Reopen()
        {
            if (session.Paused) return false;
            try { var kept = store.Load(); if (kept == null) return false; AbortPending(); StopVoice(); ClearClips(); narrationToken = null; storyToken = null; personalReason = null; ResetReadingPosition(); return session.OpenSaved(kept); }
            catch (Exception) { ShelfProblem("Your kept story could not be opened. It has not been replaced.", () => Reopen()); return false; }
        }
        public void Forget()
        {
            if (session.Paused) return;
            Decide("Forget your kept story?", "This removes the story kept on this device and the beads on your strand.", new[] { ("forget", "Forget kept story"), ("cancel", "Keep it") }, id =>
            {
                // Forgetting removes everything kept about earlier visits: the telling and the strand of sides heard.
                if (id == "forget") { try { store.Forget(); strands.Forget(); strand = new KeptStrand(); } catch (Exception) { ShelfProblem("The kept story could not be removed.", Forget); return; } }
                Render();
            });
        }
        void ShelfProblem(string message, Action retry)
        {
            Decide("Your kept story", message + " No story request will be sent.", new[] { ("shelf-retry", "Try the kept story again"), ("shelf-return", "Return") },
                id => { if (id == "shelf-retry") retry(); else Render(); });
        }
        void RequestLeave()
        {
            if (session.State == PersonalStoryState.Intake)
            {
                Decide("Your story table", "Your choices are private until you choose online creation. You can reopen or forget the one story explicitly kept here.",
                    HasKept ? new[] { ("reopen", "Reopen kept story"), ("forget", "Forget kept story"), ("exit", "Close Sankofa"), ("cancel", "Return") } : new[] { ("exit", "Close Sankofa"), ("cancel", "Return") }, id =>
                    { if (id == "reopen") Reopen(); else if (id == "forget") Forget(); else if (id == "exit") Quit(); else Render(); });
                return;
            }
            Decide("Return to your story table?", "This telling stays unsaved unless you choose Keep at its ending.", new[] { ("leave", "Return to my choices"), ("cancel", "Stay with the story") }, id =>
            { if (id == "leave") { AbortPending(); StopVoice(); keyboard.Close(); session.Pause(false); session.Cancel(); } else Render(); });
        }
        void Quit() { AbortPending(); StopVoice(); if (Application.isEditor) { session.Pause(false); session.Cancel(); } else Application.Quit(); }
        void OnFocusLost() { if (!session.Paused) { systemOwnsPause = true; session.Pause(true); } }
        void OnFocusRestored() { if (systemOwnsPause) { systemOwnsPause = false; session.Pause(false); } }
        void OnApplicationPause(bool value) { if (value) OnFocusLost(); else OnFocusRestored(); }
        void AbortPending() { if (pending != null) { pending.Abort(); pending = null; } }
        void StopVoice()
        {
            if (voice != null) { voice.Stop(); voice.clip = null; voiceHeld = false; }
            if (pendingAudio != null) { pendingAudio.Abort(); pendingAudio = null; }
            if (voiceRequest != null) { StopCoroutine(voiceRequest); voiceRequest = null; }
        }
        void SpeakCurrent()
        {
            if (!narrationEnabled || session.State != PersonalStoryState.Reading || session.Paused || session.Chapter == null) return;
            // The recording is of the spoken words. A telling kept before the label left the recordings still finds its
            // earlier clip by its full stored text.
            var key = PersonalStoryVoice.ClipName(PersonalStoryVoice.Spoken(session.Chapter.Narration));
            var clip = Resources.Load<AudioClip>("Narration/personal-table/" + key);
            if (clip == null) clip = Resources.Load<AudioClip>("Narration/personal-table/" + PersonalStoryVoice.ClipName(session.Chapter.Narration));
            if (clip == null) clips.TryGetValue(key, out clip);
            if (clip != null) { PlayClip(clip); return; }
            var endpoint = Endpoint();
            if (endpoint == null || string.IsNullOrEmpty(narrationToken)) { view.SetStatus("This telling is available as captions • no saved voice recording"); return; }
            voiceRequest = StartCoroutine(FetchVoice(key, endpoint, session.RequestVersion, session.Chapter.Id));
        }
        IEnumerator FetchVoice(string key, string endpoint, int version, string chapterId)
        {
            var body = new JsonObject().Add("story", JsonText.ParseObject(PersonalStoryJson.Write(session.Story))).Add("chapter_id", chapterId).Add("token", narrationToken);
            using (var call = UnityWebRequestMultimedia.GetAudioClip(endpoint + "/narration", AudioType.MPEG))
            {
                pendingAudio = call; call.method = "POST";
                call.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonText.Serialize(body)));
                call.SetRequestHeader("Content-Type", "application/json"); call.timeout = 30; call.redirectLimit = 0;
                Authorize(call);
                view.SetStatus("Preparing the voice • the captions remain available");
                yield return call.SendWebRequest();
                if (pendingAudio == call) pendingAudio = null;
                voiceRequest = null;
                if (version != session.RequestVersion || session.Chapter == null || session.Chapter.Id != chapterId) yield break;
                if (call.result != UnityWebRequest.Result.Success || call.downloadedBytes > 8 * 1024 * 1024)
                { view.SetStatus("Voice is unavailable • continue with captions"); yield break; }
                var clip = DownloadHandlerAudioClip.GetContent(call);
                if (clip == null) { view.SetStatus("Voice is unavailable • continue with captions"); yield break; }
                clips[key] = clip;
                if (narrationEnabled && !session.Paused && session.State == PersonalStoryState.Reading) PlayClip(clip);
            }
        }
        void PlayClip(AudioClip clip)
        {
            if (!narrationEnabled || clip == null || session.Paused) return;
            voice.transform.position = view.FirePosition;
            voice.clip = clip; voice.volume = .85f; voice.Play();
            pageSpoken = session.State == PersonalStoryState.Reading;
        }
        void ClearClips() { foreach (var clip in clips.Values) if (clip != null) Destroy(clip); clips.Clear(); }
        void OnDestroy()
        {
            AbortPending(); AbortAsk(); StopListening(false); StopVoice(); ClearClips();
            OVRManager.HMDUnmounted -= OnFocusLost; OVRManager.InputFocusLost -= OnFocusLost;
            OVRManager.HMDMounted -= OnFocusRestored; OVRManager.InputFocusAcquired -= OnFocusRestored;
            session.Changed -= Render;
        }
    }
}
