using System;
using System.Collections.Generic;
using System.Linq;

namespace Sankofa.Core.Story
{
    public enum JournalKind
    {
        Started, LineHeard, ActionPerformed, InvitationOpened, InvitationResolved, InvitationWithdrawn,
        SceneEntered, Paused, Resumed, ReturnedEarly, Completed, Left,
    }

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
        /// <summary>The line, invitation, scene or object concerned, if any.</summary>
        public string Subject { get; }
        public string Detail { get; }

        public override string ToString() => $"#{Sequence} {At:0.00}s {Kind} {Subject} {Detail}".TrimEnd();
    }

    /// <summary>
    /// Record of one visit: heard lines, completed actions and how each invitation was resolved. Heard and
    /// performed are separate so a resumed line never repeats its completed action. Explicit and default outcomes
    /// stay distinguishable so the story never claims the visitor chose a passive path.
    /// </summary>
    public sealed class VisitJournal
    {
        readonly List<JournalEntry> entries = new List<JournalEntry>();
        readonly HashSet<string> heard = new HashSet<string>();
        readonly HashSet<string> performed = new HashSet<string>();
        readonly Dictionary<string, Outcome> outcomes = new Dictionary<string, Outcome>();

        public VisitJournal(int visitNumber, string episodeId, string episodeVersion)
        {
            VisitNumber = visitNumber;
            EpisodeId = episodeId;
            EpisodeVersion = episodeVersion;
        }

        public int VisitNumber { get; }
        public string EpisodeId { get; }
        public string EpisodeVersion { get; }
        public IReadOnlyList<JournalEntry> Entries => entries;
        public IReadOnlyDictionary<string, Outcome> Outcomes => outcomes;
        public bool Completed => entries.Any(e => e.Kind == JournalKind.Completed);
        public bool ReturnedEarly => entries.Any(e => e.Kind == JournalKind.ReturnedEarly);

        public bool HasHeard(string lineId) => heard.Contains(lineId);
        public bool HasPerformed(string lineId, StoryAction action) => performed.Contains(Key(lineId, action));

        internal void Add(double at, JournalKind kind, string subject = null, string detail = null) =>
            entries.Add(new JournalEntry(entries.Count + 1, at, kind, subject, detail));

        public void Started(double at) => Add(at, JournalKind.Started, EpisodeId, EpisodeVersion);

        public void LineHeard(double at, string lineId)
        {
            if (heard.Add(lineId)) Add(at, JournalKind.LineHeard, lineId);
        }

        /// <summary>Records an action of a line. Returns false if it was already performed, so it is not repeated.</summary>
        public bool TryPerform(double at, string lineId, StoryAction action)
        {
            if (!performed.Add(Key(lineId, action))) return false;
            Add(at, JournalKind.ActionPerformed, action.Object, action.Kind.ToString().ToLowerInvariant() + " by " + lineId);
            return true;
        }

        public void InvitationOpened(double at, string invitationId) => Add(at, JournalKind.InvitationOpened, invitationId);

        public void InvitationResolved(double at, string invitationId, Outcome outcome)
        {
            if (outcomes.ContainsKey(invitationId)) throw new InvalidOperationException($"Invitation '{invitationId}' was already resolved");
            outcomes.Add(invitationId, outcome);
            Add(at, JournalKind.InvitationResolved, invitationId, outcome == Outcome.Accepted ? "accepted by the visitor" : "default path; no visitor choice");
        }

        public void InvitationWithdrawn(double at, string invitationId) => Add(at, JournalKind.InvitationWithdrawn, invitationId, "story left before resolution");
        public void SceneEntered(double at, string sceneId) => Add(at, JournalKind.SceneEntered, sceneId);
        public void Paused(double at, string reason) => Add(at, JournalKind.Paused, null, reason);
        public void Resumed(double at) => Add(at, JournalKind.Resumed);
        public void ReturnedEarlyAt(double at, string beatId) => Add(at, JournalKind.ReturnedEarly, beatId);
        public void CompletedAt(double at) => Add(at, JournalKind.Completed, EpisodeId);
        public void LeftAt(double at) => Add(at, JournalKind.Left);

        static string Key(string lineId, StoryAction action) => lineId + "|" + action.Kind + "|" + action.Object;
    }
}
