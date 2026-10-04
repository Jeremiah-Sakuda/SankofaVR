using System;
using System.Collections.Generic;

namespace Sankofa.Core.Story
{
    /// <summary>What the story needs next. The application flow performs it and reports completion.</summary>
    public abstract class StoryStep
    {
        StoryStep() { }

        /// <summary>Speak one line. <see cref="Beat"/> identifies where it belongs.</summary>
        public sealed class Speak : StoryStep
        {
            internal Speak(Line line, Beat beat)
            {
                Line = line;
                Beat = beat;
            }

            public Line Line { get; }
            public Beat Beat { get; }
            public override string ToString() => "Speak " + Line.Id;
        }

        /// <summary>Move the visitor between scenes around their stationary viewpoint.</summary>
        public sealed class ChangeScene : StoryStep
        {
            internal ChangeScene(SceneDef from, SceneDef to)
            {
                From = from;
                To = to;
            }

            public SceneDef From { get; }
            public SceneDef To { get; }
            public override string ToString() => $"ChangeScene {From.Id} -> {To.Id}";
        }

        /// <summary>The beat's lines are done and its invitation is unresolved: wait for the visitor or the default.</summary>
        public sealed class Await : StoryStep
        {
            internal Await(Invitation invitation) => Invitation = invitation;
            public Invitation Invitation { get; }
            public override string ToString() => "Await " + Invitation.Id;
        }

        public sealed class End : StoryStep
        {
            internal End() { }
            public override string ToString() => "End";
        }

        internal static readonly End Finished = new End();
    }

    /// <summary>
    /// Deterministic progression through a validated plan's beats. It selects outcome-dependent lines, plays the
    /// accepted or default lines of each invitation exactly once and reports scene changes. It has no clock: the
    /// application flow decides when a step is complete.
    /// </summary>
    public sealed class StoryRun
    {
        enum Stage { NotStarted, SceneCheck, BeatLines, Awaiting, OutcomeLines, Done }

        readonly PlayablePlan plan;
        readonly Dictionary<string, Outcome> outcomes = new Dictionary<string, Outcome>();
        Stage stage = Stage.NotStarted;
        int beatIndex = -1;
        int lineIndex;

        public StoryRun(PlayablePlan plan)
        {
            this.plan = plan ?? throw new ArgumentNullException(nameof(plan));
            CurrentScene = plan.Hearth;
        }

        /// <summary>The visitor begins at the hearth, where the welcome is spoken.</summary>
        public SceneDef CurrentScene { get; private set; }
        public Beat CurrentBeat => beatIndex >= 0 && beatIndex < plan.Plan.Beats.Count ? plan.Plan.Beats[beatIndex] : null;
        public bool IsComplete => stage == Stage.Done;
        public IReadOnlyDictionary<string, Outcome> Outcomes => outcomes;

        /// <summary>The invitation of the current beat while it can still be resolved.</summary>
        public Invitation PendingInvitation
        {
            get
            {
                var inv = CurrentBeat?.Invitation;
                return inv != null && (stage == Stage.BeatLines || stage == Stage.Awaiting) && !outcomes.ContainsKey(inv.Id) ? inv : null;
            }
        }

        /// <summary>Advances past the step just completed and returns the next one.</summary>
        public StoryStep Next()
        {
            var beats = plan.Plan.Beats;
            while (true)
            {
                switch (stage)
                {
                    case Stage.NotStarted:
                        beatIndex = 0;
                        stage = Stage.SceneCheck;
                        continue;

                    case Stage.SceneCheck:
                        stage = Stage.BeatLines;
                        lineIndex = 0;
                        var target = plan.Scene(beats[beatIndex].Scene);
                        if (target != CurrentScene)
                        {
                            var from = CurrentScene;
                            CurrentScene = target;
                            return new StoryStep.ChangeScene(from, target);
                        }
                        continue;

                    case Stage.BeatLines:
                    {
                        var beat = beats[beatIndex];
                        if (lineIndex < beat.Lines.Count)
                        {
                            var line = beat.Lines[lineIndex++];
                            if (Plays(line)) return new StoryStep.Speak(line, beat);
                            continue;
                        }
                        if (beat.Invitation != null && !outcomes.ContainsKey(beat.Invitation.Id))
                        {
                            stage = Stage.Awaiting;
                            return new StoryStep.Await(beat.Invitation);
                        }
                        stage = Stage.OutcomeLines;
                        lineIndex = 0;
                        continue;
                    }

                    case Stage.Awaiting:
                    {
                        var inv = beats[beatIndex].Invitation;
                        if (!outcomes.ContainsKey(inv.Id)) return new StoryStep.Await(inv);
                        stage = Stage.OutcomeLines;
                        lineIndex = 0;
                        continue;
                    }

                    case Stage.OutcomeLines:
                    {
                        var beat = beats[beatIndex];
                        var lines = beat.Invitation == null ? Array.Empty<Line>()
                            : outcomes[beat.Invitation.Id] == Outcome.Accepted ? beat.Invitation.Accepted : beat.Invitation.Default;
                        if (lineIndex < lines.Count) return new StoryStep.Speak(lines[lineIndex++], beat);
                        beatIndex++;
                        if (beatIndex >= beats.Count)
                        {
                            stage = Stage.Done;
                            return StoryStep.Finished;
                        }
                        stage = Stage.SceneCheck;
                        continue;
                    }

                    default:
                        return StoryStep.Finished;
                }
            }
        }

        /// <summary>
        /// Records the outcome of the current beat's invitation. Returns false, changing nothing, for any other
        /// invitation or a second resolution: the same action is acknowledged once.
        /// </summary>
        public bool Resolve(string invitationId, Outcome outcome)
        {
            var pending = PendingInvitation;
            if (pending == null || pending.Id != invitationId) return false;
            outcomes.Add(invitationId, outcome);
            return true;
        }

        bool Plays(Line line) =>
            line.When == null || (outcomes.TryGetValue(line.When.Invitation, out var outcome) && outcome == line.When.Outcome);
    }
}
