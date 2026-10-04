using System;
using System.Collections.Generic;
using System.Linq;
using Sankofa.Core.Story;

namespace Sankofa.Flow
{
    public sealed class FlowSettings
    {
        /// <summary>A transition that presentation never reports finished still completes after this long.</summary>
        public double TransitionTimeout = 15;
        /// <summary>A line whose playback never reports finished completes after its duration × factor + grace.</summary>
        public double WatchdogFactor = 2;
        public double WatchdogGrace = 8;
        /// <summary>After a pause, an open invitation waits at least this long before its default path.</summary>
        public double MinWaitAfterResume = 6;
    }

    /// <summary>
    /// The application flow for one visitor: welcome → story → hearth. It owns accepted state changes, timing and
    /// the permitted controls; presentation renders its <see cref="Cue"/>s and reports completion by token. A
    /// completion with a stale token (after a pause, interruption or scene change) changes nothing.
    /// Time advances only through <see cref="Tick"/>, excluding pauses, so it is deterministic under test.
    /// </summary>
    public sealed class VisitFlow
    {
        enum Activity { None, Speaking, Silence, Awaiting, Transitioning }
        enum LineContext { Welcome, Story, EarlyReturn }
        enum Continuation { None, Story, EarlyReturn }

        readonly PlayableEpisode episode;
        readonly FlowSettings settings;
        readonly List<Cue> cues = new List<Cue>();
        IReadOnlyList<ControlOption> controls = Array.Empty<ControlOption>();

        StoryRun run;
        int visitNumber;
        int tokens;

        Activity activity;
        VisitPhase pausedFrom;

        Line line;
        LineContext context;
        int lineToken;
        double lineElapsed;
        double lineWatchdog;
        double silenceRemaining;
        int welcomeIndex;
        int earlyIndex;

        Invitation openInvitation;
        double awaitRemaining;

        int transitionToken;
        double transitionElapsed;
        SceneDef transitionTarget;
        Continuation afterTransition;
        bool pauseAfterTransition;
        Continuation resumeWith;

        public VisitFlow(PlayableEpisode episode, FlowSettings settings = null)
        {
            this.episode = episode ?? throw new ArgumentNullException(nameof(episode));
            this.settings = settings ?? new FlowSettings();
            Scene = episode.Hearth;
        }

        public VisitPhase Phase { get; private set; } = VisitPhase.Idle;
        public VisitJournal Journal { get; private set; }
        /// <summary>Visit time in seconds, excluding pauses.</summary>
        public double Clock { get; private set; }
        /// <summary>The scene the visitor is in once any transition completes.</summary>
        public SceneDef Scene { get; private set; }
        public IReadOnlyList<ControlOption> Controls => controls;
        public Invitation OpenInvitation => openInvitation;
        public Line CurrentLine => activity == Activity.Speaking || activity == Activity.Silence ? line : null;
        public PlayableEpisode Episode => episode;

        public IReadOnlyList<Cue> DrainCues()
        {
            var drained = cues.ToArray();
            cues.Clear();
            return drained;
        }

        // ---- visitor commands ----

        /// <summary>Opens the hearth. A returning visitor may skip the welcome.</summary>
        public void Start(bool skipWelcome = false)
        {
            if (Phase != VisitPhase.Idle) throw new InvalidOperationException("The visit has already started");
            NewJournal();
            Emit(new Cue.ResetStage());
            if (skipWelcome || episode.Episode.Welcome.Count == 0)
            {
                AwaitBegin();
                return;
            }
            SetPhase(VisitPhase.Welcome);
            welcomeIndex = 0;
            SpeakLine(episode.Episode.Welcome[0], LineContext.Welcome);
            UpdateControls();
        }

        /// <summary>Starts the story, also during the welcome: the visitor can act while the fire speaks.</summary>
        public bool Begin()
        {
            if (Phase != VisitPhase.Welcome && Phase != VisitPhase.AwaitingBegin) return false;
            StopLine();
            BeginStory();
            return true;
        }

        /// <summary>The visitor's deliberate action for the open invitation (gesture or nearby select). Acknowledged once.</summary>
        public bool Accept(string invitationId)
        {
            if (Phase != VisitPhase.Story || openInvitation == null || openInvitation.Id != invitationId) return false;
            if (!run.Resolve(invitationId, Outcome.Accepted)) return false;
            CloseInvitation(Outcome.Accepted);
            // While the offer is still being spoken the line finishes first; the run then skips the wait.
            if (activity == Activity.Awaiting)
            {
                activity = Activity.None;
                AdvanceStory();
            }
            return true;
        }

        public bool Pause(string reason = "visitor")
        {
            if (Phase == VisitPhase.Transitioning)
            {
                // A transition is short and visual; pause as soon as it lands rather than freezing it midway.
                pauseAfterTransition = true;
                return true;
            }
            if (Phase != VisitPhase.Welcome && Phase != VisitPhase.AwaitingBegin && Phase != VisitPhase.Story && Phase != VisitPhase.Closing) return false;
            pausedFrom = Phase;
            if (activity == Activity.Speaking && lineToken != 0)
            {
                lineToken = 0;
                Emit(new Cue.StopSpeaking());
            }
            SetPhase(VisitPhase.Paused);
            Journal.Paused(Clock, reason);
            Emit(new Cue.Fire(FireState.Resting));
            UpdateControls();
            return true;
        }

        public bool Resume()
        {
            if (Phase != VisitPhase.Paused) return false;
            SetPhase(pausedFrom);
            Journal.Resumed(Clock);
            if (resumeWith != Continuation.None)
            {
                var next = resumeWith;
                resumeWith = Continuation.None;
                Continue(next);
            }
            else
            {
                switch (activity)
                {
                    case Activity.Speaking:
                        // Resume from the line's start, a safe boundary; its completed actions are not repeated.
                        SpeakLine(line, context);
                        break;
                    case Activity.Awaiting:
                        awaitRemaining = Math.Max(awaitRemaining, settings.MinWaitAfterResume);
                        Emit(new Cue.Fire(FireState.Listening));
                        break;
                    case Activity.Silence:
                        Emit(new Cue.Fire(line.Fire));
                        break;
                    default:
                        Emit(new Cue.Fire(Phase == VisitPhase.Closing ? FireState.Welcoming : FireState.Listening));
                        break;
                }
            }
            UpdateControls();
            return true;
        }

        /// <summary>Leaves the story for the hearth at any point, without pretending the story finished.</summary>
        public bool ReturnToHearth()
        {
            var inStory = Phase == VisitPhase.Story || (Phase == VisitPhase.Paused && pausedFrom == VisitPhase.Story);
            if (!inStory) return false;
            if (Phase == VisitPhase.Paused)
            {
                SetPhase(VisitPhase.Story);
                Journal.Resumed(Clock);
                resumeWith = Continuation.None;
            }
            StopLine();
            if (openInvitation != null)
            {
                Journal.InvitationWithdrawn(Clock, openInvitation.Id);
                Emit(new Cue.InvitationClosed(openInvitation.Id, null));
                openInvitation = null;
            }
            activity = Activity.None;
            Journal.ReturnedEarlyAt(Clock, run.CurrentBeat?.Id);
            if (Scene != episode.Hearth) StartTransition(Scene, episode.Hearth, Continuation.EarlyReturn);
            else
            {
                SetPhase(VisitPhase.Closing);
                Continue(Continuation.EarlyReturn);
                UpdateControls();
            }
            return true;
        }

        public bool BeginAgain()
        {
            if (Phase != VisitPhase.Closing) return false;
            StopLine();
            NewJournal();
            Emit(new Cue.ResetStage());
            BeginStory();
            return true;
        }

        public bool Leave()
        {
            if (Phase == VisitPhase.Ended || Phase == VisitPhase.Idle) return false;
            StopLine();
            if (openInvitation != null)
            {
                Journal.InvitationWithdrawn(Clock, openInvitation.Id);
                Emit(new Cue.InvitationClosed(openInvitation.Id, null));
                openInvitation = null;
            }
            activity = Activity.None;
            transitionToken = 0;
            Journal.LeftAt(Clock);
            SetPhase(VisitPhase.Ended);
            Emit(new Cue.Fire(FireState.Resting));
            UpdateControls();
            return true;
        }

        // ---- presentation reports ----

        /// <summary>Playback of a line ended. Returns false for a stale or unknown token, which changes nothing.</summary>
        public bool LineFinished(int token)
        {
            if (token == 0 || token != lineToken || activity != Activity.Speaking || Phase == VisitPhase.Paused || Phase == VisitPhase.Ended) return false;
            lineToken = 0;
            Journal.LineHeard(Clock, line.Id);
            if (line.PauseAfter > 0)
            {
                activity = Activity.Silence;
                silenceRemaining = line.PauseAfter;
                return true;
            }
            AfterLine();
            return true;
        }

        /// <summary>Presentation measured the actual clip length; the watchdog allows for it.</summary>
        public bool ReportLineDuration(int token, double seconds)
        {
            if (token == 0 || token != lineToken || double.IsNaN(seconds) || seconds < 0) return false;
            lineWatchdog = Math.Max(lineWatchdog, seconds + settings.WatchdogGrace);
            return true;
        }

        public bool TransitionFinished(int token)
        {
            if (token == 0 || token != transitionToken || Phase != VisitPhase.Transitioning) return false;
            transitionToken = 0;
            activity = Activity.None;
            Scene = transitionTarget;
            Journal.SceneEntered(Clock, Scene.Id);
            var next = afterTransition;
            afterTransition = Continuation.None;
            SetPhase(next == Continuation.EarlyReturn ? VisitPhase.Closing : VisitPhase.Story);
            if (pauseAfterTransition)
            {
                pauseAfterTransition = false;
                resumeWith = next;
                Pause("deferred until the transition finished");
                return true;
            }
            Continue(next);
            UpdateControls();
            return true;
        }

        public void Tick(double deltaSeconds)
        {
            if (Phase == VisitPhase.Idle || Phase == VisitPhase.Paused || Phase == VisitPhase.Ended) return;
            if (double.IsNaN(deltaSeconds) || deltaSeconds <= 0) return;
            Clock += deltaSeconds;
            switch (activity)
            {
                case Activity.Speaking:
                    lineElapsed += deltaSeconds;
                    if (lineToken != 0 && lineElapsed > lineWatchdog) LineFinished(lineToken);
                    break;
                case Activity.Silence:
                    silenceRemaining -= deltaSeconds;
                    if (silenceRemaining <= 0)
                    {
                        activity = Activity.None;
                        AfterLine();
                    }
                    break;
                case Activity.Awaiting:
                    awaitRemaining -= deltaSeconds;
                    if (awaitRemaining <= 0 && openInvitation != null && run.Resolve(openInvitation.Id, Outcome.Default))
                    {
                        CloseInvitation(Outcome.Default);
                        activity = Activity.None;
                        AdvanceStory();
                    }
                    break;
                case Activity.Transitioning:
                    transitionElapsed += deltaSeconds;
                    if (transitionElapsed > settings.TransitionTimeout) TransitionFinished(transitionToken);
                    break;
            }
        }

        // ---- internals ----

        void NewJournal()
        {
            visitNumber++;
            Journal = new VisitJournal(visitNumber, episode.Episode.Id, episode.Episode.Version);
            Journal.Started(Clock);
        }

        void BeginStory()
        {
            run = new StoryRun(episode);
            Scene = episode.Hearth;
            openInvitation = null;
            pauseAfterTransition = false;
            resumeWith = Continuation.None;
            SetPhase(VisitPhase.Story);
            AdvanceStory();
            UpdateControls();
        }

        void AwaitBegin()
        {
            activity = Activity.None;
            SetPhase(VisitPhase.AwaitingBegin);
            Emit(new Cue.Fire(FireState.Listening));
            UpdateControls();
        }

        void Continue(Continuation next)
        {
            if (next == Continuation.Story) AdvanceStory();
            else if (next == Continuation.EarlyReturn) SpeakEarlyReturn(0);
        }

        void AdvanceStory()
        {
            switch (run.Next())
            {
                case StoryStep.Speak speak:
                    SpeakLine(speak.Line, LineContext.Story);
                    break;
                case StoryStep.ChangeScene change:
                    StartTransition(change.From, change.To, Continuation.Story);
                    break;
                case StoryStep.Await wait:
                    activity = Activity.Awaiting;
                    awaitRemaining = wait.Invitation.WaitSeconds;
                    if (openInvitation == null) OpenInvitationFor(wait.Invitation.Object);
                    Emit(new Cue.Fire(FireState.Listening));
                    break;
                default:
                    Complete();
                    break;
            }
            UpdateControls();
        }

        void SpeakLine(Line next, LineContext lineContext)
        {
            line = next;
            context = lineContext;
            activity = Activity.Speaking;
            lineToken = ++tokens;
            lineElapsed = 0;
            lineWatchdog = next.Seconds * settings.WatchdogFactor + settings.WatchdogGrace;
            foreach (var action in next.Actions)
            {
                if (!Journal.TryPerform(Clock, next.Id, action)) continue;
                Emit(new Cue.Act(action, next.Id));
                if (action.Kind == ActionKind.Offer) OpenInvitationFor(action.Object);
            }
            Emit(new Cue.Fire(next.Fire));
            Emit(new Cue.Speak(lineToken, next));
        }

        void AfterLine()
        {
            switch (context)
            {
                case LineContext.Welcome:
                    welcomeIndex++;
                    if (welcomeIndex < episode.Episode.Welcome.Count) SpeakLine(episode.Episode.Welcome[welcomeIndex], LineContext.Welcome);
                    else AwaitBegin();
                    break;
                case LineContext.Story:
                    AdvanceStory();
                    break;
                case LineContext.EarlyReturn:
                    SpeakEarlyReturn(earlyIndex + 1);
                    break;
            }
        }

        void SpeakEarlyReturn(int index)
        {
            earlyIndex = index;
            if (index < episode.Episode.EarlyReturn.Count) SpeakLine(episode.Episode.EarlyReturn[index], LineContext.EarlyReturn);
            else Close();
        }

        void StopLine()
        {
            if (activity == Activity.Speaking && lineToken != 0) Emit(new Cue.StopSpeaking());
            lineToken = 0;
            if (activity == Activity.Speaking || activity == Activity.Silence) activity = Activity.None;
        }

        void OpenInvitationFor(string objectId)
        {
            var pending = run?.PendingInvitation;
            if (pending == null || pending.Object != objectId || openInvitation == pending) return;
            openInvitation = pending;
            Journal.InvitationOpened(Clock, pending.Id);
            Emit(new Cue.InvitationOpened(pending));
            UpdateControls();
        }

        void CloseInvitation(Outcome outcome)
        {
            Journal.InvitationResolved(Clock, openInvitation.Id, outcome);
            Emit(new Cue.InvitationClosed(openInvitation.Id, outcome));
            openInvitation = null;
            UpdateControls();
        }

        void StartTransition(SceneDef from, SceneDef to, Continuation then)
        {
            activity = Activity.Transitioning;
            transitionToken = ++tokens;
            transitionElapsed = 0;
            transitionTarget = to;
            afterTransition = then;
            SetPhase(VisitPhase.Transitioning);
            Emit(new Cue.Fire(FireState.Transitioning));
            Emit(new Cue.Transition(transitionToken, from, to, to.Kind == SceneKind.Hearth ? TransitionKind.Return : TransitionKind.Enter, Continuities(from, to)));
            UpdateControls();
        }

        IReadOnlyList<Continuity> Continuities(SceneDef from, SceneDef to)
        {
            var list = new List<Continuity>();
            foreach (var o in episode.Episode.Objects)
            {
                if (o.Continues == null) continue;
                var previous = episode.Object(o.Continues);
                if (o.Scene == to.Id && previous.Scene == from.Id) list.Add(new Continuity(previous.Id, o.Id));
                else if (o.Scene == from.Id && previous.Scene == to.Id) list.Add(new Continuity(o.Id, previous.Id));
            }
            return list;
        }

        void Complete()
        {
            Journal.CompletedAt(Clock);
            Close();
        }

        void Close()
        {
            activity = Activity.None;
            SetPhase(VisitPhase.Closing);
            Emit(new Cue.Fire(FireState.Welcoming));
            var keepsake = episode.Episode.Keepsake;
            if (keepsake != null && (Journal.Completed || Encountered(keepsake))) Emit(new Cue.Keepsake(keepsake));
            Emit(new Cue.Closed(Journal.Completed));
            UpdateControls();
        }

        /// <summary>Whether the visitor met the keepsake (or what it became) before leaving the story early.</summary>
        bool Encountered(string objectId)
        {
            var related = new HashSet<string> { objectId };
            foreach (var o in episode.Episode.Objects)
                if (o.Continues == objectId) related.Add(o.Id);
            return Journal.Entries.Any(e => e.Kind == JournalKind.ActionPerformed && related.Contains(e.Subject));
        }

        void SetPhase(VisitPhase phase)
        {
            if (Phase == phase) return;
            Phase = phase;
            Emit(new Cue.PhaseChanged(phase));
        }

        void UpdateControls()
        {
            var next = new List<ControlOption>();
            switch (Phase)
            {
                case VisitPhase.Welcome:
                case VisitPhase.AwaitingBegin:
                    next.Add(new ControlOption(ControlKind.Begin));
                    next.Add(new ControlOption(ControlKind.Pause));
                    break;
                case VisitPhase.Story:
                    if (openInvitation != null) next.Add(new ControlOption(ControlKind.Accept, openInvitation.Id, openInvitation.Alternative));
                    next.Add(new ControlOption(ControlKind.Pause));
                    next.Add(new ControlOption(ControlKind.ReturnToHearth));
                    break;
                case VisitPhase.Transitioning:
                    next.Add(new ControlOption(ControlKind.Pause));
                    break;
                case VisitPhase.Paused:
                    next.Add(new ControlOption(ControlKind.Resume));
                    if (pausedFrom == VisitPhase.Story) next.Add(new ControlOption(ControlKind.ReturnToHearth));
                    next.Add(new ControlOption(ControlKind.Leave));
                    break;
                case VisitPhase.Closing:
                    next.Add(new ControlOption(ControlKind.BeginAgain));
                    next.Add(new ControlOption(ControlKind.Leave));
                    break;
            }
            if (next.Count == controls.Count && next.Zip(controls, (a, b) => a.SameAs(b)).All(same => same)) return;
            controls = next;
            Emit(new Cue.Controls(next));
        }

        void Emit(Cue cue) => cues.Add(cue);
    }
}
