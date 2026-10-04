using System;
using System.Collections.Generic;
using System.Linq;
using Sankofa.Core.Story;
using Sankofa.Core.Visits;

namespace Sankofa.Flow
{
    public sealed class FlowSettings
    {
        /// <summary>A transition that presentation never reports finished still completes after this long.</summary>
        public double TransitionTimeout = 15;
        /// <summary>A line whose playback never reports finished completes after its duration × factor + grace.</summary>
        public double WatchdogFactor = 2;
        public double WatchdogGrace = 8;
        /// <summary>After a pause or a question, an open invitation waits at least this long before its default path.</summary>
        public double MinWaitAfterResume = 6;
        /// <summary>In a replay, how long an invitation stays visible before its kept outcome plays.</summary>
        public double ReplayHold = 2.5;
    }

    /// <summary>
    /// The application flow at the hearth: welcome → choice → story → hearth, again and again. It owns accepted state
    /// changes, timing and the permitted controls: which stories can be chosen, which questions asked, whether a
    /// visit can be kept, replayed exactly, continued or forgotten. Presentation renders its <see cref="Cue"/>s and
    /// reports completion by token; a completion with a stale token (after a pause, question, interruption or scene
    /// change) changes nothing. Time advances only through <see cref="Tick"/>, excluding pauses, so it is
    /// deterministic under test.
    /// </summary>
    public sealed class VisitFlow
    {
        enum Activity { None, Speaking, Silence, Awaiting, Transitioning }
        enum LineContext { Hearth, Story, Answer, EarlyReturn }
        enum Continuation { None, Story, EarlyReturn }

        readonly PlayablePackage package;
        readonly FlowSettings settings;
        readonly IVisitStore store;
        readonly IStageInventory inventory;
        readonly Func<string> newVisitId;
        readonly Func<DateTimeOffset> now;
        readonly SceneDef hearth;
        readonly List<Cue> cues = new List<Cue>();
        readonly Staging staging = new Staging();
        IReadOnlyList<ControlOption> controls = Array.Empty<ControlOption>();
        IReadOnlyList<Question> questions = Array.Empty<Question>();

        PlayablePlan plan;
        StoryRun run;
        IReadOnlyDictionary<string, Outcome> replayOutcomes;
        bool visitActive;
        bool keptThisVisit;
        bool replayBlocked;
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
        IReadOnlyList<Line> hearthLines = Array.Empty<Line>();
        int hearthIndex;
        int earlyIndex;

        Invitation openInvitation;
        double awaitRemaining;

        Question answering;
        int answerIndex;
        Activity interruptedActivity;
        Line interruptedLine;
        LineContext interruptedContext;
        double interruptedAwait;

        int transitionToken;
        double transitionElapsed;
        SceneDef transitionTarget;
        Continuation afterTransition;
        bool pauseAfterTransition;
        Continuation resumeWith;

        /// <param name="store">Where kept visits live; without one, nothing can be kept.</param>
        /// <param name="inventory">What the stage can show; plans needing anything else are refused.</param>
        public VisitFlow(PlayablePackage package, FlowSettings settings = null, IVisitStore store = null, IStageInventory inventory = null,
            Func<string> newVisitId = null, Func<DateTimeOffset> now = null)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            this.settings = settings ?? new FlowSettings();
            this.store = store;
            this.inventory = inventory;
            this.newVisitId = newVisitId ?? (() => "v" + Guid.NewGuid().ToString("N").Substring(0, 16));
            this.now = now ?? (() => DateTimeOffset.UtcNow);
            hearth = package.Package.Scenes.Single(s => s.Kind == SceneKind.Hearth);
            Scene = hearth;
        }

        public VisitPhase Phase { get; private set; } = VisitPhase.Idle;
        /// <summary>What the visitor is choosing while at the hearth before a story.</summary>
        public ChoiceMenu Menu { get; private set; }
        /// <summary>The current visit's journal, or the last one's after it closed.</summary>
        public VisitJournal Journal { get; private set; }
        /// <summary>Session time in seconds, excluding pauses.</summary>
        public double Clock { get; private set; }
        /// <summary>The scene the visitor is in once any transition completes.</summary>
        public SceneDef Scene { get; private set; }
        public IReadOnlyList<ControlOption> Controls => controls;
        public IReadOnlyList<Question> Questions => questions;
        public Invitation OpenInvitation => openInvitation;
        public Line CurrentLine => activity == Activity.Speaking || activity == Activity.Silence ? line : null;
        public Question Answering => answering;
        public PlayablePackage Package => package;
        /// <summary>The plan of the current visit, or of the last one.</summary>
        public PlayablePlan Plan => plan;
        /// <summary>The visit waiting at the hearth, if the visitor kept one.</summary>
        public SavedVisit KeptVisit { get; private set; }
        public bool Replaying => replayOutcomes != null;
        public bool CanFollow => KeptVisit != null && KeptVisit.Package == package.Package.Id && package.Route(KeptVisit.Next)?.Follows == KeptVisit.Route;
        public bool CanReplay => KeptVisit?.Plan != null && !replayBlocked;
        public bool CanKeep => store != null && visitActive && Journal != null && Journal.Completed && !Replaying && !keptThisVisit;

        public IReadOnlyList<Cue> DrainCues()
        {
            var drained = cues.ToArray();
            cues.Clear();
            return drained;
        }

        // ---- at the hearth ----

        /// <summary>Opens the hearth: the welcome (or welcome back, with a kept visit waiting), then the choice.</summary>
        public void Start(bool skipWelcome = false)
        {
            if (Phase != VisitPhase.Idle) throw new InvalidOperationException("The hearth has already opened");
            LoadKept();
            Emit(new Cue.ResetStage());
            Emit(new Cue.Shelf(KeptVisit));
            var menu = KeptVisit != null ? ChoiceMenu.Returning : ChoiceMenu.Interests;
            var lines = KeptVisit != null && package.Package.WelcomeBack.Count > 0 ? package.Package.WelcomeBack : package.Package.Welcome;
            if (skipWelcome || lines.Count == 0)
            {
                EnterChoosing(menu, speak: false);
                return;
            }
            Menu = menu;
            SetPhase(VisitPhase.Welcome);
            SpeakHearth(lines);
            UpdateControls();
        }

        /// <summary>Begins a first story by interest. The visitor may choose while the fire is still speaking.</summary>
        public bool Choose(string routeId)
        {
            if (!AtChoice(ChoiceMenu.Interests)) return false;
            var route = package.Route(routeId);
            if (route == null || route.IsContinuation) return false;
            return BeginVisit(package.Compose(route.Id), VisitMode.Chosen, null, null);
        }

        /// <summary>Continues the kept visit's thread: a new, labelled child visit that recalls only what was kept.</summary>
        public bool Follow()
        {
            if (!AtChoice(ChoiceMenu.Returning) || !CanFollow) return false;
            return BeginVisit(package.Compose(KeptVisit.Next, KeptVisit.AsParent()), VisitMode.Continued, KeptVisit.Id, null);
        }

        /// <summary>Plays the kept visit exactly as it was kept: its own plan and outcomes, nothing regenerated.</summary>
        public bool Replay()
        {
            if (!AtChoice(ChoiceMenu.Returning) || !CanReplay) return false;
            return BeginVisit(KeptVisit.Plan, VisitMode.Replayed, KeptVisit.Id, KeptVisit.Outcomes);
        }

        public bool NewStory()
        {
            if (!AtChoice(ChoiceMenu.Returning)) return false;
            StopLine();
            EnterChoosing(ChoiceMenu.Interests, speak: true);
            return true;
        }

        public bool Back()
        {
            if (!AtChoice(ChoiceMenu.Interests) || KeptVisit == null) return false;
            StopLine();
            EnterChoosing(ChoiceMenu.Returning, speak: false);
            return true;
        }

        /// <summary>Asks for confirmation before forgetting the kept visit.</summary>
        public bool Forget()
        {
            if (!AtChoice(ChoiceMenu.Returning)) return false;
            Menu = ChoiceMenu.ConfirmForget;
            UpdateControls();
            return true;
        }

        /// <summary>Deletes the kept visit, its keepsake and its thread. A failed delete keeps everything as it was.</summary>
        public bool ConfirmForget()
        {
            if (!AtChoice(ChoiceMenu.ConfirmForget)) return false;
            var visit = KeptVisit;
            try
            {
                store?.Delete(visit.Id);
            }
            catch (VisitStoreException e)
            {
                Emit(new Cue.StoreFailed(StoreOperation.Forget, e.Message));
                Menu = ChoiceMenu.Returning;
                UpdateControls();
                return false;
            }
            KeptVisit = null;
            replayBlocked = false;
            Emit(new Cue.Forgotten(visit.Id));
            Emit(new Cue.Shelf(null));
            Menu = ChoiceMenu.Interests;
            UpdateControls();
            return true;
        }

        public bool CancelForget()
        {
            if (!AtChoice(ChoiceMenu.ConfirmForget)) return false;
            Menu = ChoiceMenu.Returning;
            UpdateControls();
            return true;
        }

        // ---- during a visit ----

        /// <summary>The visitor's deliberate action for the open invitation (gesture or nearby select). Acknowledged once.</summary>
        public bool Accept(string invitationId)
        {
            if (Phase != VisitPhase.Story || Replaying || openInvitation == null || openInvitation.Id != invitationId) return false;
            if (!run.Resolve(invitationId, Outcome.Accepted)) return false;
            CloseInvitation(Outcome.Accepted);
            // While the offer is still being spoken the line finishes first; the run then skips the wait.
            if (activity == Activity.Awaiting)
            {
                activity = Activity.None;
                AdvanceStory();
            }
            UpdateControls();
            return true;
        }

        /// <summary>
        /// Interrupts the fire with a question by hand. The answer plays, then the story resumes from a safe
        /// boundary: an interrupted line from its start (its completed actions are not repeated), an open invitation
        /// with time to act. Each question is answered once per visit.
        /// </summary>
        public bool Ask(string questionId)
        {
            if (Phase != VisitPhase.Story || answering != null) return false;
            var question = questions.FirstOrDefault(q => q.Id == questionId);
            if (question == null) return false;
            var first = NextPlayable(question.Lines, 0);
            if (first < 0) return false;
            interruptedActivity = activity;
            interruptedLine = line;
            interruptedContext = context;
            interruptedAwait = awaitRemaining;
            if (activity == Activity.Speaking && lineToken != 0) Emit(new Cue.StopSpeaking());
            lineToken = 0;
            activity = Activity.None;
            answering = question;
            answerIndex = first;
            Journal.QuestionAsked(Clock, question.Id);
            Emit(new Cue.QuestionAsked(question));
            SpeakLine(question.Lines[first], LineContext.Answer);
            UpdateControls();
            return true;
        }

        /// <summary>
        /// Head direction rested on an object. Recorded once as a head-direction signal (not eye tracking, never
        /// consent) and surfaced so presentation can offer a question about it; the story does not change.
        /// </summary>
        public bool Notice(string objectId)
        {
            if (Phase != VisitPhase.Story || plan == null || !plan.HasObject(objectId)) return false;
            if (!staging.Visible(objectId) || plan.Object(objectId).Scene != Scene.Id) return false;
            Journal.Noticed(Clock, objectId);
            Emit(new Cue.Noticed(objectId, questions.FirstOrDefault(q => q.About == objectId)?.Id));
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
            if (Phase != VisitPhase.Welcome && Phase != VisitPhase.Choosing && Phase != VisitPhase.Story && Phase != VisitPhase.Closing) return false;
            pausedFrom = Phase;
            if (activity == Activity.Speaking && lineToken != 0)
            {
                lineToken = 0;
                Emit(new Cue.StopSpeaking());
            }
            SetPhase(VisitPhase.Paused);
            if (visitActive) Journal.Paused(Clock, reason);
            Emit(new Cue.Fire(FireState.Resting));
            UpdateControls();
            return true;
        }

        public bool Resume()
        {
            if (Phase != VisitPhase.Paused) return false;
            SetPhase(pausedFrom);
            if (visitActive) Journal.Resumed(Clock);
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
                        if (!Replaying) awaitRemaining = Math.Max(awaitRemaining, settings.MinWaitAfterResume);
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
            answering = null;
            WithdrawInvitation();
            activity = Activity.None;
            Journal.ReturnedEarlyAt(Clock, run.CurrentBeat?.Id);
            if (Scene.Id != plan.Hearth.Id) StartTransition(Scene, plan.Hearth, Continuation.EarlyReturn);
            else
            {
                SetPhase(VisitPhase.Closing);
                Continue(Continuation.EarlyReturn);
            }
            UpdateControls();
            return true;
        }

        /// <summary>Keeps the completed visit on this device, replacing any visit kept before. Failure leaves it unkept and retryable.</summary>
        public bool Keep()
        {
            if (Phase != VisitPhase.Closing || !CanKeep) return false;
            var visit = SavedVisitCodec.FromVisit(newVisitId(), now(), plan.Plan, Journal);
            try
            {
                store.Save(visit);
            }
            catch (VisitStoreException e)
            {
                Emit(new Cue.StoreFailed(StoreOperation.Keep, e.Message));
                return false;
            }
            var previous = KeptVisit;
            KeptVisit = visit;
            keptThisVisit = true;
            replayBlocked = false;
            Emit(new Cue.Kept(visit));
            if (previous != null && previous.Id != visit.Id)
            {
                try
                {
                    store.Delete(previous.Id);
                }
                catch (VisitStoreException e)
                {
                    Emit(new Cue.StoreFailed(StoreOperation.Forget, e.Message));
                }
            }
            UpdateControls();
            return true;
        }

        /// <summary>Back to the choice at the hearth, without the welcome.</summary>
        public bool BeginAgain()
        {
            if (Phase != VisitPhase.Closing) return false;
            StopLine();
            EndVisit();
            Emit(new Cue.ResetStage());
            Emit(new Cue.Shelf(KeptVisit));
            EnterChoosing(KeptVisit != null ? ChoiceMenu.Returning : ChoiceMenu.Interests, speak: true);
            return true;
        }

        public bool Leave()
        {
            if (Phase == VisitPhase.Ended || Phase == VisitPhase.Idle) return false;
            StopLine();
            if (visitActive)
            {
                WithdrawInvitation();
                Journal.LeftAt(Clock);
            }
            answering = null;
            activity = Activity.None;
            transitionToken = 0;
            visitActive = false;
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
            if (context != LineContext.Hearth) Journal.LineHeard(Clock, line.Id);
            if (line.PauseAfter > 0)
            {
                activity = Activity.Silence;
                silenceRemaining = line.PauseAfter;
                return true;
            }
            activity = Activity.None;
            AfterLine();
            UpdateControls();
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
                        UpdateControls();
                    }
                    break;
                case Activity.Awaiting:
                    awaitRemaining -= deltaSeconds;
                    if (awaitRemaining <= 0 && openInvitation != null)
                    {
                        var outcome = Outcome.Default;
                        if (Replaying && replayOutcomes.TryGetValue(openInvitation.Id, out var kept)) outcome = kept;
                        if (run.Resolve(openInvitation.Id, outcome))
                        {
                            CloseInvitation(outcome);
                            activity = Activity.None;
                            AdvanceStory();
                            UpdateControls();
                        }
                    }
                    break;
                case Activity.Transitioning:
                    transitionElapsed += deltaSeconds;
                    if (transitionElapsed > settings.TransitionTimeout) TransitionFinished(transitionToken);
                    break;
            }
        }

        // ---- internals ----

        bool AtChoice(ChoiceMenu menu) => (Phase == VisitPhase.Welcome || Phase == VisitPhase.Choosing) && Menu == menu;

        void LoadKept()
        {
            if (store == null) return;
            VisitStoreContents contents;
            try
            {
                contents = store.Load();
            }
            catch (VisitStoreException e)
            {
                Emit(new Cue.StoreFailed(StoreOperation.Load, e.Message));
                return;
            }
            foreach (var problem in contents.Problems) Emit(new Cue.StoreFailed(StoreOperation.Load, problem));
            KeptVisit = contents.Visits.FirstOrDefault();
        }

        void EnterChoosing(ChoiceMenu menu, bool speak)
        {
            activity = Activity.None;
            Menu = menu;
            Scene = hearth;
            SetPhase(VisitPhase.Choosing);
            Emit(new Cue.Fire(FireState.Listening));
            if (speak && package.Package.Choose.Count > 0) SpeakHearth(package.Package.Choose);
            UpdateControls();
        }

        bool BeginVisit(StoryPlan candidate, VisitMode mode, string related, IReadOnlyDictionary<string, Outcome> replay)
        {
            var playable = PlanValidator.Accept(candidate, out var report, inventory);
            if (playable == null)
            {
                // Never improvise a different story: a kept plan the stage can no longer show is reported, not replaced.
                if (mode == VisitMode.Replayed) replayBlocked = true;
                Emit(new Cue.StoreFailed(StoreOperation.Replay, $"{candidate.Route} cannot play: {report.Errors.First()}"));
                UpdateControls();
                return false;
            }
            StopLine();
            plan = playable;
            replayOutcomes = replay;
            visitActive = true;
            keptThisVisit = false;
            visitNumber++;
            Journal = new VisitJournal(visitNumber, plan.Plan, mode, related);
            Journal.Started(Clock);
            run = new StoryRun(plan);
            Scene = plan.Hearth;
            staging.Clear();
            openInvitation = null;
            answering = null;
            pauseAfterTransition = false;
            resumeWith = Continuation.None;
            Menu = ChoiceMenu.None;
            Emit(new Cue.ResetStage());
            Emit(new Cue.VisitStarted(plan.Plan, mode));
            SetPhase(VisitPhase.Story);
            AdvanceStory();
            UpdateControls();
            return true;
        }

        void EndVisit()
        {
            visitActive = false;
            replayOutcomes = null;
            openInvitation = null;
            answering = null;
            staging.Clear();
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
                    awaitRemaining = Replaying ? settings.ReplayHold : wait.Invitation.WaitSeconds;
                    if (openInvitation == null) OpenInvitationFor(wait.Invitation.Object);
                    Emit(new Cue.Fire(FireState.Listening));
                    break;
                default:
                    Complete();
                    break;
            }
        }

        void SpeakHearth(IReadOnlyList<Line> lines)
        {
            hearthLines = lines;
            hearthIndex = 0;
            if (lines.Count > 0) SpeakLine(lines[0], LineContext.Hearth);
        }

        void SpeakLine(Line next, LineContext lineContext)
        {
            line = next;
            context = lineContext;
            activity = Activity.Speaking;
            lineToken = ++tokens;
            lineElapsed = 0;
            lineWatchdog = next.Seconds * settings.WatchdogFactor + settings.WatchdogGrace;
            if (lineContext != LineContext.Hearth)
            {
                foreach (var action in next.Actions)
                {
                    if (!Journal.TryPerform(Clock, next.Id, action)) continue;
                    Stage(action);
                    Emit(new Cue.Act(action, next.Id));
                    if (action.Kind == ActionKind.Offer) OpenInvitationFor(action.Object);
                }
            }
            Emit(new Cue.Fire(next.Fire));
            Emit(new Cue.Speak(lineToken, next));
        }

        /// <summary>The same staging rules the validator walked, so what crosses and what comes home agree.</summary>
        void Stage(StoryAction action)
        {
            switch (action.Kind)
            {
                case ActionKind.Reveal:
                case ActionKind.Illuminate:
                    staging.Show(action.Object);
                    break;
                case ActionKind.Offer:
                    staging.Free(action.Object);
                    break;
                case ActionKind.Place:
                    if (action.At == null) staging.Free(action.Object);
                    else staging.Attach(action.Object);
                    break;
                case ActionKind.Withdraw:
                    staging.Remove(action.Object);
                    break;
            }
        }

        void AfterLine()
        {
            switch (context)
            {
                case LineContext.Hearth:
                    hearthIndex++;
                    if (hearthIndex < hearthLines.Count) SpeakLine(hearthLines[hearthIndex], LineContext.Hearth);
                    else if (Phase == VisitPhase.Welcome) EnterChoosing(Menu, speak: false);
                    break;
                case LineContext.Story:
                    AdvanceStory();
                    break;
                case LineContext.Answer:
                    var next = NextPlayable(answering.Lines, answerIndex + 1);
                    if (next >= 0)
                    {
                        answerIndex = next;
                        SpeakLine(answering.Lines[next], LineContext.Answer);
                    }
                    else FinishAnswer();
                    break;
                case LineContext.EarlyReturn:
                    SpeakEarlyReturn(earlyIndex + 1);
                    break;
            }
        }

        int NextPlayable(IReadOnlyList<Line> lines, int from)
        {
            for (var i = from; i < lines.Count; i++)
            {
                var when = lines[i].When;
                if (when == null || (run.Outcomes.TryGetValue(when.Invitation, out var o) && o == when.Outcome)) return i;
            }
            return -1;
        }

        void FinishAnswer()
        {
            answering = null;
            activity = Activity.None;
            switch (interruptedActivity)
            {
                case Activity.Speaking when !OffersResolvedInvitation(interruptedLine):
                    SpeakLine(interruptedLine, interruptedContext);
                    break;
                case Activity.Speaking:
                case Activity.Silence:
                    // The interrupted offer was already taken, or only silence remained: carry on from there.
                    line = interruptedLine;
                    context = interruptedContext;
                    AfterLine();
                    break;
                case Activity.Awaiting when openInvitation != null:
                    activity = Activity.Awaiting;
                    awaitRemaining = Replaying ? interruptedAwait : Math.Max(interruptedAwait, settings.MinWaitAfterResume);
                    Emit(new Cue.Fire(FireState.Listening));
                    break;
                default:
                    AdvanceStory();
                    break;
            }
        }

        bool OffersResolvedInvitation(Line spoken)
        {
            var inv = run.CurrentBeat?.Invitation;
            return inv != null && run.Outcomes.ContainsKey(inv.Id) && spoken.Actions.Any(a => a.Kind == ActionKind.Offer && a.Object == inv.Object);
        }

        void SpeakEarlyReturn(int index)
        {
            earlyIndex = index;
            if (index < plan.Plan.EarlyReturn.Count) SpeakLine(plan.Plan.EarlyReturn[index], LineContext.EarlyReturn);
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
            Emit(new Cue.InvitationOpened(pending, Replaying));
            UpdateControls();
        }

        void CloseInvitation(Outcome outcome)
        {
            Journal.InvitationResolved(Clock, openInvitation.Id, outcome, Replaying);
            Emit(new Cue.InvitationClosed(openInvitation, outcome));
            openInvitation = null;
        }

        void WithdrawInvitation()
        {
            if (openInvitation == null) return;
            Journal.InvitationWithdrawn(Clock, openInvitation.Id);
            Emit(new Cue.InvitationClosed(openInvitation, null));
            openInvitation = null;
        }

        void StartTransition(SceneDef from, SceneDef to, Continuation then)
        {
            activity = Activity.Transitioning;
            transitionToken = ++tokens;
            transitionElapsed = 0;
            transitionTarget = to;
            afterTransition = then;
            var crossed = staging.Cross(plan.CorrespondencesBetween(from.Id, to.Id)).Select(c => new Continuity(c.From, c.To)).ToList();
            SetPhase(VisitPhase.Transitioning);
            Emit(new Cue.Fire(FireState.Transitioning));
            Emit(new Cue.Transition(transitionToken, from, to, to.Kind == SceneKind.Hearth ? TransitionKind.Return : TransitionKind.Enter, crossed));
            UpdateControls();
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
            var keepsake = plan.Plan.Keepsake;
            if (keepsake != null && staging.WithVisitor(keepsake)) Emit(new Cue.Keepsake(keepsake));
            Emit(new Cue.Closed(Journal.Completed));
            UpdateControls();
        }

        void SetPhase(VisitPhase phase)
        {
            if (Phase == phase) return;
            Phase = phase;
            Emit(new Cue.PhaseChanged(phase));
        }

        void UpdateQuestions()
        {
            var next = new List<Question>();
            if (Phase == VisitPhase.Story && answering == null && activity != Activity.Transitioning && run?.CurrentBeat != null)
            {
                foreach (var q in run.CurrentBeat.Questions)
                {
                    if (Journal.HasAsked(q.Id)) continue;
                    if (q.About != null && (!staging.Visible(q.About) || plan.Object(q.About).Scene != Scene.Id)) continue;
                    if (NextPlayable(q.Lines, 0) < 0) continue;
                    next.Add(q);
                }
            }
            if (next.Count == questions.Count && next.Zip(questions, (a, b) => a == b).All(same => same)) return;
            questions = next;
            Emit(new Cue.Questions(next));
        }

        void UpdateControls()
        {
            UpdateQuestions();
            var next = new List<ControlOption>();
            switch (Phase)
            {
                case VisitPhase.Welcome:
                case VisitPhase.Choosing:
                    switch (Menu)
                    {
                        case ChoiceMenu.Interests:
                            foreach (var route in package.FirstRoutes) next.Add(new ControlOption(ControlKind.Choose, route.Id, route.Label));
                            if (KeptVisit != null) next.Add(new ControlOption(ControlKind.Back));
                            next.Add(new ControlOption(ControlKind.Pause));
                            break;
                        case ChoiceMenu.Returning:
                            if (CanFollow) next.Add(new ControlOption(ControlKind.Follow, KeptVisit.Next, package.Route(KeptVisit.Next).Label));
                            if (CanReplay) next.Add(new ControlOption(ControlKind.Replay, KeptVisit.Id, KeptVisit.Label));
                            next.Add(new ControlOption(ControlKind.NewStory));
                            next.Add(new ControlOption(ControlKind.Forget, KeptVisit?.Id));
                            next.Add(new ControlOption(ControlKind.Pause));
                            break;
                        case ChoiceMenu.ConfirmForget:
                            next.Add(new ControlOption(ControlKind.ConfirmForget, KeptVisit?.Id));
                            next.Add(new ControlOption(ControlKind.CancelForget));
                            break;
                    }
                    break;
                case VisitPhase.Story:
                    if (openInvitation != null && !Replaying) next.Add(new ControlOption(ControlKind.Accept, openInvitation.Id, openInvitation.Alternative));
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
                    // The target names the kept visit this one would replace, so presentation can say so.
                    if (CanKeep) next.Add(new ControlOption(ControlKind.Keep, KeptVisit?.Id));
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
