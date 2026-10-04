using System.Collections.Generic;
using Sankofa.Core.Story;

namespace Sankofa.Flow
{
    public enum VisitPhase { Idle, Welcome, AwaitingBegin, Story, Transitioning, Paused, Closing, Ended }

    public enum TransitionKind { Enter, Return }

    /// <summary>Hand controls the flow currently permits. Presentation decides how they look and what they say.</summary>
    public enum ControlKind { Begin, Pause, Resume, ReturnToHearth, BeginAgain, Leave, Accept }

    public sealed class ControlOption
    {
        public ControlOption(ControlKind kind, string invitationId = null, string label = null)
        {
            Kind = kind;
            InvitationId = invitationId;
            Label = label;
        }

        public ControlKind Kind { get; }
        /// <summary>For <see cref="ControlKind.Accept"/>: the invitation the control resolves.</summary>
        public string InvitationId { get; }
        /// <summary>Content-supplied label (the invitation's alternative), or null for the presentation's own label.</summary>
        public string Label { get; }

        public bool SameAs(ControlOption other) => other != null && Kind == other.Kind && InvitationId == other.InvitationId && Label == other.Label;
        public override string ToString() => InvitationId == null ? Kind.ToString() : $"{Kind}({InvitationId})";
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

        /// <summary>Reset staged objects for a new visit.</summary>
        public sealed class ResetStage : Cue
        {
            internal ResetStage() { }
            public override string ToString() => "ResetStage";
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

        /// <summary>Stop the current line at once (pause, interruption or leaving).</summary>
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

        public sealed class InvitationOpened : Cue
        {
            internal InvitationOpened(Invitation invitation) => Invitation = invitation;
            public Invitation Invitation { get; }
            public override string ToString() => "InvitationOpened " + Invitation.Id;
        }

        /// <summary>The invitation closed with an outcome, or was withdrawn (null) because the visitor left the story.</summary>
        public sealed class InvitationClosed : Cue
        {
            internal InvitationClosed(string invitationId, Outcome? outcome)
            {
                InvitationId = invitationId;
                Outcome = outcome;
            }

            public string InvitationId { get; }
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

        public sealed class Controls : Cue
        {
            internal Controls(IReadOnlyList<ControlOption> options) => Options = options;
            public IReadOnlyList<ControlOption> Options { get; }
            public override string ToString() => "Controls [" + string.Join(", ", Options) + "]";
        }

        /// <summary>The visit has reached its closing at the hearth and its journal is final.</summary>
        public sealed class Closed : Cue
        {
            internal Closed(bool completed) => Completed = completed;
            /// <summary>False when the visitor returned before the story's ending.</summary>
            public bool Completed { get; }
            public override string ToString() => "Closed " + (Completed ? "completed" : "returned early");
        }

        public sealed class Keepsake : Cue
        {
            internal Keepsake(string objectId) => ObjectId = objectId;
            public string ObjectId { get; }
            public override string ToString() => "Keepsake " + ObjectId;
        }
    }
}
