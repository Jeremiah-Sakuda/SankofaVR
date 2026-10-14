using System.Collections.Generic;
using Sankofa.Core.Story;
using Sankofa.Core.Visits;

namespace Sankofa.Flow
{
    public enum VisitPhase { Idle, Welcome, Choosing, Story, Transitioning, Paused, Closing, Ended }

    /// <summary>What the visitor is choosing at the hearth.</summary>
    public enum ChoiceMenu { None, Interests, Returning, ConfirmForget }

    public enum TransitionKind { Enter, Return }

    /// <summary>Hand controls the flow currently permits. Presentation decides how they look and what they say.</summary>
    public enum ControlKind
    {
        Choose, Follow, Replay, NewStory, Back, Forget, ConfirmForget, CancelForget,
        Pause, Resume, ReturnToHearth, Accept, Keep, BeginAgain, Leave,
        /// <summary>Leaving a finished visit that is not kept, after being asked.</summary>
        ConfirmLeave,
        /// <summary>Staying at the hearth instead of leaving an unkept visit.</summary>
        CancelLeave,
    }

    /// <summary>A store operation that failed; the visit itself carries on.</summary>
    public enum StoreOperation { Load, Keep, Forget, Replay }

    public sealed class ControlOption
    {
        public ControlOption(ControlKind kind, string target = null, string label = null)
        {
            Kind = kind;
            Target = target;
            Label = label;
        }

        public ControlKind Kind { get; }
        /// <summary>The route chosen (<see cref="ControlKind.Choose"/>, <see cref="ControlKind.Follow"/>) or the invitation accepted.</summary>
        public string Target { get; }
        /// <summary>Content-supplied label (a route or an invitation's alternative), or null for the presentation's own label.</summary>
        public string Label { get; }

        public bool SameAs(ControlOption other) => other != null && Kind == other.Kind && Target == other.Target && Label == other.Label;
        public override string ToString() => Target == null ? Kind.ToString() : $"{Kind}({Target})";
    }

    /// <summary>Shared element carried across a transition, such as an ember becoming a lantern.</summary>
    public sealed class Continuity
    {
        public Continuity(string from, string to)
        {
            From = from;
            To = to;
        }

        public string From { get; }
        public string To { get; }
        public override string ToString() => From + "->" + To;
    }

    /// <summary>
    /// An accepted instruction from the flow to presentation. Presentation renders cues and reports completion
    /// with their tokens; it never advances the story itself.
    /// </summary>
    public abstract class Cue
    {
        Cue() { }

        public sealed class PhaseChanged : Cue
        {
            internal PhaseChanged(VisitPhase phase) => Phase = phase;
            public VisitPhase Phase { get; }
            public override string ToString() => "Phase " + Phase;
        }

        /// <summary>Reset staged objects: a new visit begins, or the visitor is back to choosing.</summary>
        public sealed class ResetStage : Cue
        {
            internal ResetStage() { }
            public override string ToString() => "ResetStage";
        }

        /// <summary>A visit began with this plan; <see cref="Mode"/> says whether it was chosen, continues or replays a kept visit.</summary>
        public sealed class VisitStarted : Cue
        {
            internal VisitStarted(StoryPlan plan, VisitMode mode)
            {
                Plan = plan;
                Mode = mode;
            }

            public StoryPlan Plan { get; }
            public VisitMode Mode { get; }
            public override string ToString() => $"VisitStarted {Plan.Route} {Mode}";
        }

        public sealed class Speak : Cue
        {
            internal Speak(int token, Line line)
            {
                Token = token;
                Line = line;
            }

            public int Token { get; }
            public Line Line { get; }
            public override string ToString() => $"Speak #{Token} {Line.Id}";
        }

        /// <summary>Stop the current line at once (pause, question, interruption or leaving).</summary>
        public sealed class StopSpeaking : Cue
        {
            internal StopSpeaking() { }
            public override string ToString() => "StopSpeaking";
        }

        public sealed class Fire : Cue
        {
            internal Fire(FireState state) => State = state;
            public FireState State { get; }
            public override string ToString() => "Fire " + State;
        }

        public sealed class Act : Cue
        {
            internal Act(StoryAction action, string lineId)
            {
                Action = action;
                LineId = lineId;
            }

            public StoryAction Action { get; }
            public string LineId { get; }
            public override string ToString() => "Act " + Action;
        }

        /// <summary>
        /// An invitation is open. In a replay it is shown but not offered: the kept outcome follows and the visitor's
        /// hands cannot change it.
        /// </summary>
        public sealed class InvitationOpened : Cue
        {
            internal InvitationOpened(Invitation invitation, bool replay)
            {
                Invitation = invitation;
                Replay = replay;
            }

            public Invitation Invitation { get; }
            public bool Replay { get; }
            public override string ToString() => "InvitationOpened " + Invitation.Id + (Replay ? " (replay)" : "");
        }

        /// <summary>The invitation closed with an outcome, or was withdrawn (null) because the visitor left the story.</summary>
        public sealed class InvitationClosed : Cue
        {
            internal InvitationClosed(Invitation invitation, Outcome? outcome)
            {
                Invitation = invitation;
                Outcome = outcome;
            }

            public Invitation Invitation { get; }
            public string InvitationId => Invitation.Id;
            public Outcome? Outcome { get; }
            public override string ToString() => $"InvitationClosed {InvitationId} {(Outcome.HasValue ? Outcome.ToString() : "withdrawn")}";
        }

        public sealed class Transition : Cue
        {
            internal Transition(int token, SceneDef from, SceneDef to, TransitionKind kind, IReadOnlyList<Continuity> continuities)
            {
                Token = token;
                From = from;
                To = to;
                Kind = kind;
                Continuities = continuities;
            }

            public int Token { get; }
            public SceneDef From { get; }
            public SceneDef To { get; }
            public TransitionKind Kind { get; }
            public IReadOnlyList<Continuity> Continuities { get; }
            public override string ToString() => $"Transition #{Token} {Kind} {From.Id}->{To.Id} [{string.Join(", ", Continuities)}]";
        }

        /// <summary>Stop a late transition and settle its presentation before story cues continue.</summary>
        public sealed class RecoverTransition : Cue
        {
            internal RecoverTransition(Transition transition) => Change = transition;
            public Transition Change { get; }
        }

        public sealed class Controls : Cue
        {
            internal Controls(IReadOnlyList<ControlOption> options) => Options = options;
            public IReadOnlyList<ControlOption> Options { get; }
            public override string ToString() => "Controls [" + string.Join(", ", Options) + "]";
        }

        /// <summary>The questions the visitor may ask by hand now. Empty while none are available.</summary>
        public sealed class Questions : Cue
        {
            internal Questions(IReadOnlyList<Question> available) => Available = available;
            public IReadOnlyList<Question> Available { get; }
            public override string ToString() => "Questions [" + string.Join(", ", System.Linq.Enumerable.Select(Available, q => q.Id)) + "]";
        }

        /// <summary>A question was asked: acknowledge it at once, before the answer is heard.</summary>
        public sealed class QuestionAsked : Cue
        {
            internal QuestionAsked(Question question) => Question = question;
            public Question Question { get; }
            public override string ToString() => "QuestionAsked " + Question.Id;
        }

        /// <summary>
        /// Head direction rested on a visible object. Presentation may acknowledge it and highlight
        /// <see cref="QuestionId"/>, a question about it; the story itself does not change.
        /// </summary>
        public sealed class Noticed : Cue
        {
            internal Noticed(string objectId, string questionId)
            {
                ObjectId = objectId;
                QuestionId = questionId;
            }

            public string ObjectId { get; }
            public string QuestionId { get; }
            public override string ToString() => "Noticed " + ObjectId + (QuestionId != null ? " (" + QuestionId + ")" : "");
        }

        /// <summary>The visit has reached its closing at the hearth and its journal is final.</summary>
        public sealed class Closed : Cue
        {
            internal Closed(bool completed) => Completed = completed;
            /// <summary>False when the visitor returned before the story's ending.</summary>
            public bool Completed { get; }
            public override string ToString() => "Closed " + (Completed ? "completed" : "returned early");
        }

        /// <summary>
        /// What the visitor lingered on (by head direction), shown back in the light as small echoes: at home after a
        /// visit, or as a continuation opens. Presentation only; nothing in the story depends on it.
        /// </summary>
        public sealed class Echo : Cue
        {
            internal Echo(IReadOnlyList<string> objectIds) => ObjectIds = objectIds;
            public IReadOnlyList<string> ObjectIds { get; }
            public override string ToString() => "Echo " + string.Join(", ", ObjectIds);
        }

        /// <summary>The keepsake the visitor brought home, set on the hearth stone.</summary>
        public sealed class Keepsake : Cue
        {
            internal Keepsake(string objectId) => ObjectId = objectId;
            public string ObjectId { get; }
            public override string ToString() => "Keepsake " + ObjectId;
        }

        /// <summary>The kept visit's keepsake waits on the hearth stone while the visitor chooses; null when nothing is kept.</summary>
        public sealed class Shelf : Cue
        {
            internal Shelf(SavedVisit visit) => Visit = visit;
            public SavedVisit Visit { get; }
            public string ObjectId => Visit?.Keepsake;
            public override string ToString() => "Shelf " + (Visit == null ? "empty" : Visit.Id + " " + Visit.Keepsake);
        }

        public sealed class Kept : Cue
        {
            internal Kept(SavedVisit visit) => Visit = visit;
            public SavedVisit Visit { get; }
            public override string ToString() => "Kept " + Visit.Id;
        }

        public sealed class Forgotten : Cue
        {
            internal Forgotten(string visitId) => VisitId = visitId;
            public string VisitId { get; }
            public override string ToString() => "Forgotten " + VisitId;
        }

        /// <summary>Saving, forgetting, loading or replaying failed. The experience stays usable; the message is for the status line and logs.</summary>
        public sealed class StoreFailed : Cue
        {
            internal StoreFailed(StoreOperation operation, string message)
            {
                Operation = operation;
                Message = message;
            }

            public StoreOperation Operation { get; }
            public string Message { get; }
            public override string ToString() => $"StoreFailed {Operation}: {Message}";
        }
    }
}
