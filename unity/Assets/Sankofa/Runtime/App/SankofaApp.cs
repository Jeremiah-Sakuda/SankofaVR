using System.Collections.Generic;
using System.Linq;
using Sankofa.Core.Story;
using Sankofa.Flow;
using Sankofa.Runtime.Audio;
using Sankofa.Runtime.Fire;
using Sankofa.Runtime.Input;
using Sankofa.Runtime.Narration;
using Sankofa.Runtime.Stage;
using Sankofa.Runtime.UI;
using UnityEngine;

namespace Sankofa.Runtime.App
{
    /// <summary>
    /// Composition root. Loads and validates the bundled episode, runs the visit flow and translates its cues into
    /// presentation; translates hands and controls into flow commands. Story rules live in the flow, not here.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public sealed class SankofaApp : MonoBehaviour
    {
        [SerializeField] TextAsset episodeJson;
        [SerializeField] WorldAnchor anchor;
        [SerializeField] HearthFire fire;
        [SerializeField] NarrationPlayer narration;
        [SerializeField] NightAmbience ambience;
        [SerializeField] CaptionBoard captions;
        [SerializeField] PokePanel controls;
        [SerializeField] PokePanel settingsPanel;
        [SerializeField] StoryStage stage;
        [SerializeField] StoryTransition transition;
        [SerializeField] PalmReceiver receiver;
        [SerializeField] HandSource[] hands;
        const float MaxStep = 0.1f;

        [Tooltip("Offer the steady-fire baseline in development builds, for the fire presence comparison.")]
        [SerializeField] bool fireComparison = true;

        VisitFlow flow;
        ExperienceSettings settings;
        StageObject offered;
        HandSource acceptingHand;
        bool started;

        public VisitFlow Flow => flow;
        public string LoadProblem { get; private set; }
        public ExperienceSettings Settings => settings;

        void Awake()
        {
            settings = ExperienceSettings.Load();
            flow = LoadFlow();
            foreach (var hand in hands)
            {
                var h = hand;
                controls.AddFingertip(() => h.Fingertip);
                settingsPanel.AddFingertip(() => h.Fingertip);
            }
            controls.Pressed += OnControl;
            settingsPanel.Pressed += OnSetting;
            narration.Finished += token =>
            {
                captions.ClearAfter(2.5f);
                flow?.LineFinished(token);
            };
            narration.DurationMeasured += (token, seconds) => flow?.ReportLineDuration(token, seconds);
            transition.Finished += token => flow?.TransitionFinished(token);
            receiver.Received += OnReceived;
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
            if (episodeJson == null)
            {
                LoadProblem = "No episode is assigned.";
                return null;
            }
            try
            {
                var playable = EpisodeValidator.Accept(EpisodeReader.Read(episodeJson.text), out var report);
                foreach (var issue in report.Issues)
                {
                    if (issue.Severity == IssueSeverity.Error) Debug.LogError("[Sankofa] " + issue);
                    else Debug.LogWarning("[Sankofa] " + issue);
                }
                if (playable != null) return new VisitFlow(playable);
                LoadProblem = report.Errors.First().ToString();
            }
            catch (EpisodeFormatException e)
            {
                LoadProblem = e.Message;
                Debug.LogError("[Sankofa] Episode could not be read: " + e.Message);
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
                controls.Show(new List<(string, string)> { ("leave", "Leave") });
                return;
            }
            if (flow.Episode.Episode.Status != ContentStatus.Reviewed)
                captions.SetStatus("Development placeholder story · not reviewed content");
            anchor.PlacedEvent += BeginVisit;
            if (anchor.Placed) BeginVisit();
        }

        void BeginVisit()
        {
            if (started) return;
            started = true;
            flow.Start();
            Dispatch();
        }

        void Update()
        {
            if (flow == null || !started) return;
            // A hitch or a resumed process must not leap story time; an invitation never expires in one frame.
            flow.Tick(Mathf.Min(Time.unscaledDeltaTime, MaxStep));
            Dispatch();
            fire.SpeechLevel = narration.Level;
            ambience.Duck = narration.Speaking;
            if (offered != null) offered.Feedback = receiver.Open ? receiver.Feedback : 0;
        }

        void Dispatch()
        {
            foreach (var cue in flow.DrainCues())
            {
                Debug.Log($"[Sankofa] {cue} at {flow.Clock:0.00}s");
                switch (cue)
                {
                    case Cue.PhaseChanged p:
                        if (p.Phase == VisitPhase.Paused) captions.SetCaption("Paused");
                        else if (captions.Caption == "Paused") captions.Clear();
                        if (p.Phase == VisitPhase.Ended)
                        {
                            LogJournal();
                            Quit();
                        }
                        break;
                    case Cue.ResetStage _:
                        receiver.Close();
                        offered = null;
                        stage.ResetAll();
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
                        offered = stage.Find(o.Invitation.Object);
                        if (offered != null) receiver.Listen(o.Invitation.Id, offered.transform);
                        break;
                    case Cue.InvitationClosed c:
                        receiver.Close();
                        if (offered != null)
                        {
                            offered.Feedback = 0;
                            if (c.Outcome == Outcome.Accepted) stage.Carry(offered, acceptingHand);
                        }
                        acceptingHand = null;
                        offered = null;
                        break;
                    case Cue.Transition t:
                        captions.Clear();
                        transition.ReducedMotion = settings.ReducedMotion;
                        transition.Play(t.Token, t.To.Id, t.Continuities);
                        break;
                    case Cue.Controls c:
                        controls.Show(c.Options.Select(o => (ControlId(o), ControlLabel(o))).ToList());
                        break;
                    case Cue.Closed _:
                        LogJournal();
                        break;
                    case Cue.Keepsake k:
                        stage.ShowKeepsake(k.ObjectId);
                        break;
                }
            }
        }

        void LogJournal() => Debug.Log($"[Sankofa] Journal of visit {flow.Journal.VisitNumber}:\n" + string.Join("\n", flow.Journal.Entries));

        static string ControlId(ControlOption o) => o.Kind == ControlKind.Accept ? "accept:" + o.InvitationId : o.Kind.ToString();

        static string ControlLabel(ControlOption o)
        {
            switch (o.Kind)
            {
                case ControlKind.Begin: return "Begin";
                case ControlKind.Pause: return "Pause";
                case ControlKind.Resume: return "Resume";
                case ControlKind.ReturnToHearth: return "Return to the fire";
                case ControlKind.BeginAgain: return "Begin again";
                case ControlKind.Leave: return "Leave";
                default: return o.Label ?? "Accept";
            }
        }

        void OnControl(string id)
        {
            if (flow == null)
            {
                if (id == "leave") Quit();
                return;
            }
            if (id.StartsWith("accept:"))
            {
                acceptingHand = null;
                flow.Accept(id.Substring("accept:".Length));
            }
            else if (id == ControlKind.Begin.ToString()) flow.Begin();
            else if (id == ControlKind.Pause.ToString()) flow.Pause("visitor");
            else if (id == ControlKind.Resume.ToString()) flow.Resume();
            else if (id == ControlKind.ReturnToHearth.ToString()) flow.ReturnToHearth();
            else if (id == ControlKind.BeginAgain.ToString()) flow.BeginAgain();
            else if (id == ControlKind.Leave.ToString()) flow.Leave();
            Dispatch();
        }

        void OnReceived(string invitationId, HandSource hand)
        {
            acceptingHand = hand;
            if (!flow.Accept(invitationId)) acceptingHand = null;
            Dispatch();
        }

        void OnSetting(string id)
        {
            switch (id)
            {
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
                case "recenter":
                    anchor.Place();
                    break;
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
        }

        void ShowSettings()
        {
            var items = new List<(string, string)>
            {
                ("captions", "Text: " + settings.CaptionSize),
                ("motion", settings.ReducedMotion ? "Motion: reduced" : "Motion: full"),
                ("ambience", "Ambience: " + settings.AmbienceName),
                ("recenter", "Recenter"),
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
