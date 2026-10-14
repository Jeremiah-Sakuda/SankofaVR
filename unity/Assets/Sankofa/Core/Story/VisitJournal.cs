using System;
using System.Collections.Generic;
using System.Linq;

namespace Sankofa.Core.Story
{
    public enum JournalKind
    {
        Started, LineHeard, ActionPerformed, InvitationOpened, InvitationResolved, InvitationWithdrawn,
        SceneEntered, QuestionAsked, Noticed, Paused, Resumed, ReturnedEarly, Completed, Left,
    }

    /// <summary>How a visit began: chosen by interest, continuing a kept visit, or replaying one exactly.</summary>
    public enum VisitMode { Chosen, Continued, Replayed }

    /// <summary>Whether the visitor selected a perspective or entered an authored default. Older records are unknown.</summary>
    public enum PerspectiveSelection { Unknown, Explicit, Default }

    /// <summary>One thing that actually happened, at visit time (seconds, excluding pauses).</summary>
    public sealed class JournalEntry
    {
        internal JournalEntry(int sequence, double at, JournalKind kind, string subject, string detail)
        {
            Sequence = sequence;
            At = at;
            Kind = kind;
            Subject = subject;
            Detail = detail;
        }

        public int Sequence { get; }
        public double At { get; }
        public JournalKind Kind { get; }
        /// <summary>The line, invitation, scene, question or object concerned, if any.</summary>
        public string Subject { get; }
        public string Detail { get; }

        public override string ToString() => $"#{Sequence} {At:0.00}s {Kind} {Subject} {Detail}".TrimEnd();
    }

    /// <summary>
    /// Record of one visit: heard lines, completed actions, questions and how each invitation was resolved. Heard
    /// and performed are separate so a resumed line never repeats its completed action. Explicit and default
    /// outcomes stay distinguishable so the story never claims the visitor chose a passive path.
    /// </summary>
    public sealed class VisitJournal
    {
        readonly List<JournalEntry> entries = new List<JournalEntry>();
        readonly List<string> heardInOrder = new List<string>();
        readonly HashSet<string> heard = new HashSet<string>();
        readonly HashSet<string> performed = new HashSet<string>();
        readonly Dictionary<string, Outcome> outcomes = new Dictionary<string, Outcome>();
        readonly List<string> asked = new List<string>();
        readonly HashSet<string> noticed = new HashSet<string>();
        readonly List<string> noticedInOrder = new List<string>();
        readonly Dictionary<string, int> looks = new Dictionary<string, int>();

        public VisitJournal(int visitNumber, StoryPlan plan, VisitMode mode, string relatedVisit = null, PerspectiveSelection? selection = null)
        {
            VisitNumber = visitNumber;
            Package = plan.Package;
            Version = plan.Version;
            Route = plan.Route;
            Mode = mode;
            Selection = selection ?? (mode == VisitMode.Chosen ? PerspectiveSelection.Explicit : PerspectiveSelection.Unknown);
            RelatedVisit = relatedVisit;
        }

        public int VisitNumber { get; }
        public string Package { get; }
        public string Version { get; }
        public string Route { get; }
        public VisitMode Mode { get; }
        public PerspectiveSelection Selection { get; }
        /// <summary>The kept visit this one continues or replays, or null.</summary>
        public string RelatedVisit { get; }
        public IReadOnlyList<JournalEntry> Entries => entries;
        public IReadOnlyDictionary<string, Outcome> Outcomes => outcomes;
        public IReadOnlyList<string> Heard => heardInOrder;
        public IReadOnlyList<string> Asked => asked;
        public bool Completed => entries.Any(e => e.Kind == JournalKind.Completed);
        public bool ReturnedEarly => entries.Any(e => e.Kind == JournalKind.ReturnedEarly);

        public bool HasHeard(string lineId) => heard.Contains(lineId);
        public bool HasAsked(string questionId) => asked.Contains(questionId);
        public bool HasPerformed(string lineId, StoryAction action) => performed.Contains(Key(lineId, action));

        internal void Add(double at, JournalKind kind, string subject = null, string detail = null) =>
            entries.Add(new JournalEntry(entries.Count + 1, at, kind, subject, detail));

        public void Started(double at)
        {
            var how = Mode == VisitMode.Chosen ? (Selection == PerspectiveSelection.Default ? "authored default; no perspective preference inferred" : "chosen") : Mode == VisitMode.Continued ? "continues kept visit " + RelatedVisit : "exact replay of kept visit " + RelatedVisit;
            Add(at, JournalKind.Started, Route, $"{Package} {Version}; {how}");
        }

        public void LineHeard(double at, string lineId)
        {
            if (!heard.Add(lineId)) return;
            heardInOrder.Add(lineId);
            Add(at, JournalKind.LineHeard, lineId);
        }

        /// <summary>Records an action of a line. Returns false if it was already performed, so it is not repeated.</summary>
        public bool TryPerform(double at, string lineId, StoryAction action)
        {
            if (!performed.Add(Key(lineId, action))) return false;
            Add(at, JournalKind.ActionPerformed, action.Object, action.Kind.ToString().ToLowerInvariant() + (action.At != null ? " at " + action.At : "") + " by " + lineId);
            return true;
        }

        public void InvitationOpened(double at, string invitationId) => Add(at, JournalKind.InvitationOpened, invitationId);

        public void InvitationResolved(double at, string invitationId, Outcome outcome, bool replayed = false)
        {
            if (outcomes.ContainsKey(invitationId)) throw new InvalidOperationException($"Invitation '{invitationId}' was already resolved");
            outcomes.Add(invitationId, outcome);
            var detail = replayed ? "as kept: " + (outcome == Outcome.Accepted ? "accepted" : "default path") + "; no new choice"
                : outcome == Outcome.Accepted ? "accepted by the visitor" : "default path; no visitor choice";
            Add(at, JournalKind.InvitationResolved, invitationId, detail);
        }

        public void InvitationWithdrawn(double at, string invitationId) => Add(at, JournalKind.InvitationWithdrawn, invitationId, "story left before resolution");
        public void SceneEntered(double at, string sceneId) => Add(at, JournalKind.SceneEntered, sceneId);

        public void QuestionAsked(double at, string questionId)
        {
            asked.Add(questionId);
            Add(at, JournalKind.QuestionAsked, questionId, "asked by hand");
        }

        /// <summary>
        /// The objects the visitor's head rested on most often, most first, then in the order first noticed: what
        /// they lingered on. Head direction only; the story may show these back, never treat them as a choice.
        /// </summary>
        public IReadOnlyList<string> Lingered(int most = 3) =>
            noticedInOrder.OrderByDescending(id => looks[id]).Take(Math.Max(0, most)).ToList();

        /// <summary>Head direction rested on an object. Not eye tracking, and never consent to anything.</summary>
        public bool Noticed(double at, string objectId)
        {
            looks[objectId] = looks.TryGetValue(objectId, out var n) ? n + 1 : 1;
            if (!noticed.Add(objectId)) return false;
            noticedInOrder.Add(objectId);
            Add(at, JournalKind.Noticed, objectId, "head direction; not eye tracking");
            return true;
        }

        public void Paused(double at, string reason) => Add(at, JournalKind.Paused, null, reason);
        public void Resumed(double at) => Add(at, JournalKind.Resumed);
        public void ReturnedEarlyAt(double at, string beatId) => Add(at, JournalKind.ReturnedEarly, beatId);
        public void CompletedAt(double at) => Add(at, JournalKind.Completed, Route);
        public void LeftAt(double at) => Add(at, JournalKind.Left);

        static string Key(string lineId, StoryAction action) => lineId + "|" + action.Kind + "|" + action.Object + "|" + action.At;
    }
}
