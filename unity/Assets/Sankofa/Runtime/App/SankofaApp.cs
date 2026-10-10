using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sankofa.Core.Story;
using Sankofa.Core.Visits;
using Sankofa.Flow;
using Sankofa.Runtime.Audio;
using Sankofa.Runtime.Fire;
using Sankofa.Runtime.Input;
using Sankofa.Runtime.Narration;
using Sankofa.Runtime.Stage;
using Sankofa.Runtime.UI;
using Sankofa.Storage;
using UnityEngine;

namespace Sankofa.Runtime.App
{
    /// <summary>
    /// Composition root. Loads and validates the bundled story package against the stage, shows the start screen,
    /// runs the visit flow with on-device storage for kept visits once the visitor begins, and translates its cues
    /// into presentation; translates hands, controls, questions and noticing into flow commands. Story rules live in
    /// the flow, not here.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public sealed class SankofaApp : MonoBehaviour
    {
        [SerializeField] TextAsset packageJson;
        [Tooltip("Credits for the recordings, textures and typeface, shown from the settings.")]
        [SerializeField] TextAsset credits;
        [SerializeField] WorldAnchor anchor;
        [SerializeField] HearthFire fire;
        [SerializeField] NarrationPlayer narration;
        [SerializeField] NightAmbience ambience;
        [Tooltip("The sound of each story scene; each plays only while its scene is shown.")]
        [SerializeField] StoryAmbience[] storyAmbience = new StoryAmbience[0];
        [SerializeField] CaptionBoard captions;
        [SerializeField] StartScreen startScreen;
        [Tooltip("Pause and return to the fire, at the lap, always within reach but out of the story's way.")]
        [SerializeField] PokePanel controls;
        [Tooltip("Choices and the nearby alternatives to gestures, raised into view where the visitor will see them.")]
        [SerializeField] PokePanel choicesPanel;
        [SerializeField] PokePanel questionsPanel;
        [SerializeField] PokePanel settingsPanel;
        [SerializeField] StoryStage stage;
        [SerializeField] StoryTransition transition;
        [SerializeField] PalmReceiver receiver;
        [SerializeField] TouchReceiver toucher;
        [SerializeField] AttentionSource attention;
        [SerializeField] HandSource[] hands;
        const float MaxStep = 0.1f;
        const string StartId = "start", LeaveId = "leave";

        [Tooltip("Offer the steady-fire baseline in development builds, for the fire presence comparison.")]
        [SerializeField] bool fireComparison = true;

        /// <summary>Tests keep visits in a temporary folder instead of the application's own storage.</summary>
        public static string StoreFolderOverride;

        VisitFlow flow;
        ExperienceSettings settings;
        // The settings fold behind one plaque, so the lap holds pause and return and little else.
        bool settingsOpen;
        int creditsPage;
        StageObject invited;
        HandSource acceptingHand;
        string baseStatus = "";
        bool placed, started;

        public VisitFlow Flow => flow;
        public string LoadProblem { get; private set; }
        public ExperienceSettings Settings => settings;
        public string StoreFolder { get; private set; }
        /// <summary>The start screen is showing: the hearth is placed and the visitor has not begun.</summary>
        public bool AtStart => flow != null && placed && !started;

        void Awake()
        {
            Debug.Log($"[Sankofa] build {Application.version} on {SystemInfo.deviceModel}");
#if UNITY_ANDROID && !UNITY_EDITOR
            // Render the periphery at lower resolution, more so when the frame is heavy.
            OVRManager.foveatedRenderingLevel = OVRManager.FoveatedRenderingLevel.High;
            OVRManager.useDynamicFoveatedRendering = true;
#endif
            settings = ExperienceSettings.Load();
            flow = LoadFlow();
            foreach (var hand in hands)
            {
                var h = hand;
                controls.AddFingertip(() => h.Fingertip);
                choicesPanel.AddFingertip(() => h.Fingertip);
                questionsPanel.AddFingertip(() => h.Fingertip);
                settingsPanel.AddFingertip(() => h.Fingertip);
            }
            controls.Pressed += OnControl;
            choicesPanel.Pressed += OnControl;
            questionsPanel.Pressed += OnQuestion;
            settingsPanel.Pressed += OnSetting;
            narration.Finished += token =>
            {
                captions.ClearAfter(2.5f);
                flow?.LineFinished(token);
            };
            narration.DurationMeasured += (token, seconds) => flow?.ReportLineDuration(token, seconds);
            transition.Finished += token => flow?.TransitionFinished(token);
            receiver.Received += OnReceived;
            toucher.Touched += OnReceived;
            attention.Noticed += OnNoticed;
            OVRManager.HMDUnmounted += OnHeadsetRemoved;
            OVRManager.InputFocusLost += OnFocusLost;
        }

        void OnDestroy()
        {
            OVRManager.HMDUnmounted -= OnHeadsetRemoved;
            OVRManager.InputFocusLost -= OnFocusLost;
        }

        VisitFlow LoadFlow()
        {
            if (packageJson == null)
            {
                LoadProblem = "No story package is assigned.";
                return null;
            }
            try
            {
                var playable = PackageValidator.Accept(StoryReader.ReadPackage(packageJson.text), out var report, stage);
                foreach (var issue in report.Issues)
                {
                    if (issue.Severity == IssueSeverity.Error) Debug.LogError("[Sankofa] " + issue);
                    else Debug.LogWarning("[Sankofa] " + issue);
                }
                if (playable == null)
                {
                    LoadProblem = report.Errors.First().ToString();
                    return null;
                }
                // Kept visits stay on this headset, in the application's private storage.
                StoreFolder = StoreFolderOverride ?? Path.Combine(Application.persistentDataPath, "visits");
                if (playable.Package.Status == ContentStatus.Placeholder) baseStatus = "Development placeholder story · not reviewed content";
                else if (playable.Package.Status == ContentStatus.Draft) baseStatus = "Draft story · not yet reviewed by family or community";
                return new VisitFlow(playable, null, new FileVisitStore(StoreFolder), stage);
            }
            catch (StoryFormatException e)
            {
                LoadProblem = e.Message;
                Debug.LogError("[Sankofa] Story package could not be read: " + e.Message);
            }
            return null;
        }

        void Start()
        {
            ApplySettings();
            ShowSettings();
            stage.ResetAll();
            if (flow == null)
            {
                // Fail visibly and leave a way out; never improvise a different story.
                captions.SetCaption("This story could not be opened.");
                captions.SetStatus(LoadProblem);
                choicesPanel.Show(new List<(string, string)> { (LeaveId, "Leave") });
                return;
            }
            captions.SetStatus(baseStatus);
            anchor.PlacedEvent += ShowStart;
            if (anchor.Placed) ShowStart();
        }

        /// <summary>Once the hearth is placed before the visitor, the start screen waits for them to begin.</summary>
        void ShowStart()
        {
            if (placed) return;
            placed = true;
            startScreen.Show();
            choicesPanel.Show(new List<(string, string)> { (StartId, "Begin"), (LeaveId, "Leave") });
        }

        /// <summary>Leaves the start screen and opens the hearth: the fire's welcome, then the choice.</summary>
        public void Begin()
        {
            if (!AtStart) return;
            started = true;
            startScreen.Hide();
            flow.Start();
            Dispatch();
        }

        void Update()
        {
            LogFrameTimes();
            if (flow == null || !started) return;
            // A hitch or a resumed process must not leap story time; an invitation never expires in one frame.
            // Scaled time, so tests can run a visit faster; the experience itself always runs at a time scale of 1.
            flow.Tick(Mathf.Min(Time.deltaTime, MaxStep * Mathf.Max(1f, Time.timeScale)));
            Dispatch();
            fire.SpeechLevel = narration.Level;
            ambience.Duck = narration.Speaking;
            foreach (var a in storyAmbience) a.Duck = narration.Speaking;
            attention.Listening = flow.Phase == VisitPhase.Story;
            if (invited != null) invited.Feedback = receiver.Open ? receiver.Feedback : toucher.Open ? toucher.Feedback : 0;
            NudgeWaitingChoice();
        }

        readonly FrameTiming[] timing = new FrameTiming[1];
        float perfAt;
        int perfFrames, perfTimed;
        double perfCpu, perfGpu;
        float perfWorst;
        int perfLate;

        /// <summary>
        /// Every ten seconds, the frame rate and the average CPU and GPU frame times over those ten seconds, as
        /// "[Sankofa] perf", from the start screen on, so a run on a headset measures itself (adb logcat -s Unity).
        /// GPU time is 0 where the platform does not report it.
        /// </summary>
        void LogFrameTimes()
        {
            FrameTimingManager.CaptureFrameTimings();
            if (FrameTimingManager.GetLatestTimings(1, timing) > 0)
            {
                perfCpu += timing[0].cpuFrameTime;
                perfGpu += timing[0].gpuFrameTime;
                perfTimed++;
            }
            perfFrames++;
            // The slowest frame, and the frames that missed a 72 Hz display (about 13.9 ms).
            perfWorst = Mathf.Max(perfWorst, Time.unscaledDeltaTime * 1000f);
            if (Time.unscaledDeltaTime > 1f / 72f * 1.05f) perfLate++;
            if ((perfAt += Time.unscaledDeltaTime) < 10f) return;
            var n = Math.Max(1, perfTimed);
            Debug.Log($"[Sankofa] perf {perfFrames / perfAt:0.0} fps, cpu {perfCpu / n:0.00} ms, gpu {perfGpu / n:0.00} ms over {perfTimed} frames, worst {perfWorst:0.0} ms, {perfLate} late, scene {(stage != null ? stage.ActiveScene : "none")}");
            perfAt = 0;
            perfFrames = perfTimed = 0;
            perfCpu = perfGpu = 0;
            perfWorst = 0;
            perfLate = 0;
        }

        bool askedReturn;
        float choiceWait;
        int choiceNudge;

        /// <summary>
        /// While the fire waits at a choice with nothing pressed, the choices glow in turn now and then, so a visitor
        /// who is unsure what to do sees where to reach without anything being chosen for them.
        /// </summary>
        void NudgeWaitingChoice()
        {
            if ((flow.Phase != VisitPhase.Choosing && flow.Phase != VisitPhase.Closing) || flow.CurrentLine != null)
            {
                choiceWait = 0;
                return;
            }
            choiceWait += Time.deltaTime;
            if (choiceWait < 7f) return;
            choiceWait = 0;
            var ids = choicesPanel.Ids.ToList();
            if (ids.Count > 0) choicesPanel.Pulse(ids[choiceNudge++ % ids.Count]);
        }

        void Dispatch()
        {
            foreach (var cue in flow.DrainCues())
            {
                Debug.Log($"[Sankofa] {cue} at {flow.Clock:0.00}s");
                switch (cue)
                {
                    case Cue.PhaseChanged p:
                        StageClock.Paused = p.Phase == VisitPhase.Paused;
                        if (p.Phase == VisitPhase.Paused) captions.SetCaption("Paused");
                        // The question about returning goes once the visitor has answered it, either way.
                        else if (askedReturn)
                        {
                            askedReturn = false;
                            captions.ClearNotice();
                        }
                        else if (captions.Caption == "Paused") captions.Clear();
                        if (p.Phase == VisitPhase.Ended)
                        {
                            LogJournal();
                            // A word from the fire before the app closes, rather than a cut to black.
                            captions.SetCaption("Until the next fire.");
                            StartCoroutine(QuitAfter(2.5f));
                        }
                        break;
                    case Cue.ResetStage _:
                        CloseReceivers();
                        stage.ResetAll();
                        captions.SetStatus(baseStatus);
                        break;
                    case Cue.VisitStarted v:
                        stage.ClearEchoes();
                        var mode = v.Mode == VisitMode.Continued ? "Continuing a kept visit" : v.Mode == VisitMode.Replayed ? "Replay of a kept visit, as it was" : null;
                        captions.SetStatus(string.Join(" · ", new[] { mode, baseStatus }.Where(s => !string.IsNullOrEmpty(s))));
                        break;
                    case Cue.Speak s:
                        narration.Play(s.Token, s.Line);
                        captions.SetCaption(s.Line.Text);
                        break;
                    case Cue.StopSpeaking _:
                        narration.Stop();
                        captions.Clear();
                        break;
                    case Cue.Fire f:
                        fire.SetState(f.State);
                        break;
                    case Cue.Act a:
                        stage.Perform(a.Action);
                        break;
                    case Cue.InvitationOpened o:
                        invited = stage.Find(o.Invitation.Object);
                        // A replay shows the invitation but its kept outcome stands; hands cannot change it.
                        if (invited == null || o.Replay) break;
                        if (o.Invitation.Kind == InvitationKind.Receive) receiver.Listen(o.Invitation.Id, invited.transform);
                        else toucher.Listen(o.Invitation.Id, invited);
                        // The questions step aside while the visitor reaches for something, so nothing floats over it.
                        questionsPanel.Hidden = true;
                        break;
                    case Cue.InvitationClosed c:
                        receiver.Close();
                        toucher.Close();
                        if (invited != null)
                        {
                            invited.Feedback = 0;
                            if (c.Outcome == Outcome.Accepted && c.Invitation.Kind == InvitationKind.Receive) stage.Carry(invited, acceptingHand);
                        }
                        acceptingHand = null;
                        invited = null;
                        questionsPanel.Hidden = false;
                        // The story notices what the visitor did: the nearest person of it turns to them.
                        if (c.Outcome == Outcome.Accepted) stage.AcknowledgeVisitor();
                        break;
                    case Cue.Transition t:
                        captions.Clear();
                        transition.ReducedMotion = settings.ReducedMotion;
                        transition.Play(t.Token, t.To.Id, t.Continuities);
                        break;
                    case Cue.Controls c:
                        // Pause and return stay at the lap; every choice comes up into view.
                        controls.Show(c.Options.Where(IsStoryControl).Select(o => (ControlId(o), ControlLabel(o))).ToList());
                        choicesPanel.Show(c.Options.Where(o => !IsStoryControl(o)).Select(o => (ControlId(o), ControlLabel(o))).ToList());
                        break;
                    case Cue.Questions q:
                        questionsPanel.Show(q.Available.Select(x => ("q:" + x.Id, x.Label)).ToList());
                        break;
                    case Cue.QuestionAsked _:
                        fire.Acknowledge();
                        break;
                    case Cue.Noticed n:
                        stage.Find(n.ObjectId)?.Pulse();
                        stage.AnswerLook(n.ObjectId);
                        if (n.QuestionId != null) questionsPanel.Pulse("q:" + n.QuestionId);
                        break;
                    case Cue.Closed c:
                        LogJournal();
                        stage.Settle();
                        if (c.Completed) captions.Notice(WhatYouDid(), 14, true);
                        break;
                    case Cue.Echo e:
                        stage.Echo(e.ObjectIds);
                        break;
                    case Cue.Keepsake k:
                        stage.ShowKeepsake(k.ObjectId);
                        break;
                    case Cue.Shelf s:
                        if (s.ObjectId != null) stage.ShowOnShelf(s.ObjectId);
                        // What the kept visit lingered on waits beside its keepsake, so keeping it kept them too.
                        if (s.Visit != null) stage.ShelveEchoes(s.Visit.Lingered);
                        else stage.ClearEchoes();
                        break;
                    case Cue.Kept _:
                        stage.RestKeepsake();
                        captions.Notice("Kept on this headset only: the path, what you did, and the things you looked toward most. You can forget it here at any time.", 7);
                        break;
                    case Cue.Forgotten _:
                        captions.Notice("Forgotten. Nothing of that visit is kept.", 5);
                        break;
                    case Cue.StoreFailed f:
                        Debug.LogWarning($"[Sankofa] {f.Operation} failed: {f.Message}");
                        captions.Notice(StoreMessage(f.Operation), 6);
                        break;
                }
            }
        }

        void CloseReceivers()
        {
            receiver.Close();
            toucher.Close();
            if (invited != null) invited.Feedback = 0;
            invited = null;
            acceptingHand = null;
        }

        void LogJournal()
        {
            if (flow.Journal != null)
                Debug.Log($"[Sankofa] Journal of visit {flow.Journal.VisitNumber} ({flow.Journal.Route}, {flow.Journal.Mode}):\n" + string.Join("\n", flow.Journal.Entries));
        }

        static string StoreMessage(StoreOperation operation)
        {
            switch (operation)
            {
                case StoreOperation.Keep: return "That visit could not be kept. You can try again.";
                case StoreOperation.Forget: return "That visit could not be forgotten. It is still kept.";
                case StoreOperation.Replay: return "That kept visit cannot be replayed in this version.";
                default: return "A kept visit could not be read. It has been left as it was.";
            }
        }

        static bool IsStoryControl(ControlOption o) =>
            o.Kind == ControlKind.Pause || o.Kind == ControlKind.Resume || o.Kind == ControlKind.ReturnToHearth;

        static string ControlId(ControlOption o) => o.Target == null ? o.Kind.ToString() : o.Kind + ":" + o.Target;

        string ControlLabel(ControlOption o)
        {
            switch (o.Kind)
            {
                case ControlKind.Choose: return o.Label;
                case ControlKind.Follow: return o.Label;
                case ControlKind.Replay: return "Hear it again";
                case ControlKind.NewStory: return "Something new";
                case ControlKind.Back: return "Back";
                case ControlKind.Forget: return "Forget this visit";
                case ControlKind.ConfirmForget: return "Forget it";
                case ControlKind.CancelForget: return "Keep it";
                case ControlKind.Pause: return "Pause";
                case ControlKind.Resume when flow.ConfirmingReturn: return "Stay in the story";
                case ControlKind.Resume: return "Resume";
                case ControlKind.ReturnToHearth: return "Return to the fire";
                case ControlKind.Keep: return o.Target == null ? "Keep this visit" : "Keep this instead";
                case ControlKind.BeginAgain: return "Begin again";
                case ControlKind.Leave: return "Leave";
                case ControlKind.ConfirmLeave: return flow.Phase == VisitPhase.Paused ? "Leave Sankofa" : "Leave without keeping";
                case ControlKind.CancelLeave: return "Stay";
                default: return o.Label ?? "Accept";
            }
        }

        void OnControl(string id)
        {
            if (flow == null || !started)
            {
                if (id == LeaveId) Quit();
                else if (id == StartId) Begin();
                return;
            }
            var colon = id.IndexOf(':');
            var target = colon < 0 ? null : id.Substring(colon + 1);
            if (!Enum.TryParse<ControlKind>(colon < 0 ? id : id.Substring(0, colon), out var kind)) return;
            switch (kind)
            {
                case ControlKind.Choose: flow.Choose(target); break;
                case ControlKind.Follow: flow.Follow(); break;
                case ControlKind.Replay: flow.Replay(); break;
                case ControlKind.NewStory: flow.NewStory(); break;
                case ControlKind.Back: flow.Back(); break;
                case ControlKind.Forget: flow.Forget(); break;
                case ControlKind.ConfirmForget: flow.ConfirmForget(); break;
                case ControlKind.CancelForget: flow.CancelForget(); break;
                case ControlKind.Pause: flow.Pause("visitor"); break;
                case ControlKind.Resume: flow.Resume(); break;
                // From a playing story the way home asks first; once asked, or from a pause, it goes.
                case ControlKind.ReturnToHearth:
                    if (flow.Phase == VisitPhase.Story)
                    {
                        if (flow.AskToReturn())
                        {
                            askedReturn = true;
                            captions.Notice("Return to the fire? The story stops here, and a visit left early cannot be kept.", 30, true);
                        }
                    }
                    else flow.ReturnToHearth();
                    break;
                case ControlKind.Accept:
                    acceptingHand = null;
                    flow.Accept(target);
                    break;
                case ControlKind.Keep: flow.Keep(); break;
                case ControlKind.BeginAgain: flow.BeginAgain(); break;
                case ControlKind.Leave:
                    flow.Leave();
                    if (flow.ConfirmingLeave)
                        captions.Notice(flow.Phase == VisitPhase.Paused
                            ? "Leave Sankofa now? This visit stops here and is not kept."
                            : "This visit is not kept. Keep it to come back to it, or leave without keeping.", 8);
                    break;
                case ControlKind.ConfirmLeave: flow.ConfirmLeave(); break;
                case ControlKind.CancelLeave: flow.CancelLeave(); break;
            }
            Dispatch();
        }

        void OnQuestion(string id)
        {
            if (flow == null || !id.StartsWith("q:")) return;
            flow.Ask(id.Substring(2));
            Dispatch();
        }

        void OnReceived(string invitationId, HandSource hand)
        {
            acceptingHand = hand;
            if (!flow.Accept(invitationId)) acceptingHand = null;
            Dispatch();
        }

        /// <summary>
        /// What the visitor did by hand in this visit, said back plainly at the closing ("You carried the flour. You
        /// took the bead."), so their actions are seen to have been taken up. Nothing is claimed for a path the story
        /// took on its own.
        /// </summary>
        string WhatYouDid()
        {
            var done = flow.Plan.Plan.Beats.Select(b => b.Invitation)
                .Where(i => i != null && flow.Journal.Outcomes.TryGetValue(i.Id, out var o) && o == Outcome.Accepted && !string.IsNullOrEmpty(i.Alternative))
                .Select(i => "You " + Past(i.Alternative) + ".").ToList();
            var replayed = flow.Journal.Mode == VisitMode.Replayed ? "As it was kept: " : "";
            var said = done.Count == 0 ? "This time the story went on without your hands." : string.Join(" ", done);
            // And what they looked toward longest, by the name the story gives it (head direction, not eyes).
            var longest = flow.Journal.Lingered(1).Select(id => flow.Plan.Plan.Objects.FirstOrDefault(o => o.Id == id)?.Name).FirstOrDefault(n => n != null);
            if (longest != null) said += " You looked longest at " + longest + ".";
            return replayed + said;
        }

        /// <summary>A button's words as something done: "Carry the flour" becomes "carried the flour".</summary>
        static string Past(string label)
        {
            var space = label.IndexOf(' ');
            var verb = (space < 0 ? label : label.Substring(0, space)).ToLowerInvariant();
            var rest = space < 0 ? "" : label.Substring(space);
            switch (verb)
            {
                case "take": verb = "took"; break;
                case "hang": verb = "hung"; break;
                case "hold": verb = "held"; break;
                case "give": verb = "gave"; break;
                case "set": break;
                case "carry": verb = "carried"; break;
                default: verb = verb.EndsWith("e") ? verb + "d" : verb + "ed"; break;
            }
            return verb + rest;
        }

        void OnNoticed(string objectId)
        {
            if (flow != null && flow.Notice(objectId)) Dispatch();
        }

        void OnSetting(string id)
        {
            switch (id)
            {
                case "settings":
                    settingsOpen = true;
                    ShowSettings();
                    return;
                case "done":
                    settingsOpen = false;
                    ShowSettings();
                    return;
                case "captions":
                    settings.CaptionSize = (CaptionSize)(((int)settings.CaptionSize + 1) % 3);
                    break;
                case "motion":
                    settings.ReducedMotion = !settings.ReducedMotion;
                    break;
                case "fire":
                    settings.FireMode = settings.FireMode == FireMode.Directed ? FireMode.Simple : FireMode.Directed;
                    break;
                case "ambience":
                    settings.Ambience = (settings.Ambience + 1) % 3;
                    break;
                case "height":
                    // Middle, higher, lower, and round again.
                    settings.Height = settings.Height == 0 ? 1 : settings.Height == 1 ? -1 : 0;
                    break;
                case "recenter":
                    anchor.Place();
                    break;
                case "credits":
                    // The credits come a page at a time, one page per press.
                    if (credits == null) return;
                    var pages = credits.text.Replace("\r", "").Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
                    captions.Notice(pages[creditsPage % pages.Length].Trim(), 20);
                    creditsPage++;
                    return;
            }
            settings.Save();
            ApplySettings();
            ShowSettings();
        }

        void ApplySettings()
        {
            captions.Size = settings.CaptionSize;
            fire.ReducedMotion = settings.ReducedMotion;
            transition.ReducedMotion = settings.ReducedMotion;
            fire.Mode = settings.FireMode;
            ambience.Level = settings.AmbienceLevel;
            foreach (var a in storyAmbience) a.Level = settings.AmbienceLevel;
            anchor.HeightOffset = settings.HeightOffset;
        }

        void ShowSettings()
        {
            if (!settingsOpen)
            {
                settingsPanel.Show(new List<(string, string)> { ("settings", "Settings") });
                return;
            }
            var items = new List<(string, string)>
            {
                ("done", "Done"),
                ("captions", "Text: " + settings.CaptionSize),
                ("motion", settings.ReducedMotion ? "Motion: reduced" : "Motion: full"),
                ("ambience", "Ambience: " + settings.AmbienceName),
                ("height", "Height: " + settings.HeightName),
                ("recenter", "Recenter"),
                ("credits", "Credits"),
            };
            if (fireComparison && Debug.isDebugBuild) items.Add(("fire", "Fire: " + settings.FireMode.ToString().ToLowerInvariant()));
            settingsPanel.Show(items);
        }

        void OnHeadsetRemoved() => PauseFor("headset removed");
        void OnFocusLost() => PauseFor("system menu");

        void OnApplicationPause(bool paused)
        {
            if (paused) PauseFor("application paused");
        }

        void PauseFor(string reason)
        {
            if (flow != null && started && flow.Pause(reason)) Dispatch();
        }

        System.Collections.IEnumerator QuitAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            Quit();
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
