using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Sankofa.Core.Json;
using Sankofa.Core.Story;

namespace Sankofa.Core.Visits
{
    /// <summary>
    /// A visit the visitor chose to keep. It holds the realised plan exactly as it played, so a replay never
    /// regenerates anything, and how its invitations resolved, so a continuation recalls only what happened.
    /// No name, voice, room or hand data is stored.
    /// </summary>
    public sealed class SavedVisit
    {
        public const int CurrentSchema = 1;

        public string Id { get; internal set; }
        public DateTimeOffset SavedAt { get; internal set; }
        public string Package { get; internal set; }
        public string Version { get; internal set; }
        public string Locale { get; internal set; }
        public string Route { get; internal set; }
        public PerspectiveSelection Selection { get; internal set; }
        public string Label { get; internal set; }
        /// <summary>The kept visit this one continued, or null.</summary>
        public string Parent { get; internal set; }
        /// <summary>The continuation route this visit leads to, or null.</summary>
        public string Next { get; internal set; }
        public string Keepsake { get; internal set; }
        public IReadOnlyDictionary<string, Outcome> Outcomes { get; internal set; } = new Dictionary<string, Outcome>();
        public IReadOnlyList<string> Asked { get; internal set; } = Array.Empty<string>();
        public IReadOnlyList<string> Heard { get; internal set; } = Array.Empty<string>();
        /// <summary>
        /// Up to three story objects the visitor's head rested on most, so the fire can show them back. Head
        /// direction only, not eye tracking; no other head or hand data is kept.
        /// </summary>
        public IReadOnlyList<string> Lingered { get; internal set; } = Array.Empty<string>();
        /// <summary>The realised plan, or null when it can no longer be read; see <see cref="PlanProblem"/>.</summary>
        public StoryPlan Plan { get; internal set; }
        public string PlanProblem { get; internal set; }

        public ParentVisit AsParent() => new ParentVisit(Id, Route, Outcomes);
    }

    /// <summary>Reads and writes kept visits (schema 1), embedding the plan in the format <see cref="StoryWriter"/> writes.</summary>
    public static class SavedVisitCodec
    {
        /// <summary>Lowercase letters, digits, dots and hyphens: safe as a file name on every platform.</summary>
        public static bool IsValidId(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > 64 || id[0] == '.' || id[0] == '-') return false;
            return id.All(c => (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '.' || c == '-');
        }

        /// <summary>The record of a completed visit, ready to keep.</summary>
        public static SavedVisit FromVisit(string id, DateTimeOffset savedAt, StoryPlan plan, VisitJournal journal)
        {
            if (!IsValidId(id)) throw new ArgumentException($"'{id}' is not a valid visit id", nameof(id));
            return new SavedVisit
            {
                Id = id,
                SavedAt = savedAt,
                Package = plan.Package,
                Version = plan.Version,
                Locale = plan.Locale,
                Route = plan.Route,
                Selection = journal.Selection,
                Label = plan.Label,
                Parent = plan.Parent,
                Next = plan.Next,
                Keepsake = plan.Keepsake,
                Outcomes = new Dictionary<string, Outcome>(journal.Outcomes.ToDictionary(p => p.Key, p => p.Value)),
                Asked = journal.Asked.ToList(),
                Heard = journal.Heard.ToList(),
                Lingered = journal.Lingered().ToList(),
                Plan = plan,
            };
        }

        public static string Write(SavedVisit visit)
        {
            if (visit.Plan == null) throw new ArgumentException("A visit without its plan cannot be written", nameof(visit));
            var outcomes = new JsonObject();
            foreach (var pair in visit.Outcomes.OrderBy(p => p.Key, StringComparer.Ordinal))
                outcomes.Add(pair.Key, pair.Value == Outcome.Accepted ? "accepted" : "default");
            var o = new JsonObject()
                .Add("schema", SavedVisit.CurrentSchema)
                .Add("kind", "kept-visit")
                .Add("id", visit.Id)
                .Add("saved_at", visit.SavedAt.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture))
                .Add("package", visit.Package)
                .Add("version", visit.Version)
                .Add("locale", visit.Locale)
                .Add("route", visit.Route)
                .Add("label", visit.Label);
            if (visit.Selection != PerspectiveSelection.Unknown)
                o.Add("perspective_selection", visit.Selection == PerspectiveSelection.Default ? "default" : "explicit");
            if (visit.Parent != null) o.Add("parent", visit.Parent);
            if (visit.Next != null) o.Add("next", visit.Next);
            if (visit.Keepsake != null) o.Add("keepsake", visit.Keepsake);
            o.Add("outcomes", outcomes)
                .Add("asked", visit.Asked.Cast<object>().ToList())
                .Add("heard", visit.Heard.Cast<object>().ToList());
            if (visit.Lingered.Count > 0) o.Add("lingered", visit.Lingered.Cast<object>().ToList());
            o.Add("plan", StoryWriter.Plan(visit.Plan));
            return JsonText.Serialize(o);
        }

        /// <summary>
        /// Reads a kept visit. A plan that no longer reads (for example after a schema change) leaves the visit
        /// listed without it, so it can still be forgotten or continued, but never replayed as something else.
        /// </summary>
        public static SavedVisit Read(string json)
        {
            JsonObject root;
            try
            {
                root = JsonText.ParseObject(json);
            }
            catch (FormatException e)
            {
                throw new StoryFormatException("$", e.Message);
            }
            var r = new StoryReader.Fields(root, "$");
            r.Only("schema", "kind", "id", "saved_at", "package", "version", "locale", "route", "label", "perspective_selection", "parent", "next", "keepsake", "outcomes", "asked", "heard", "lingered", "plan");
            var schema = r.Int("schema");
            if (schema != SavedVisit.CurrentSchema) throw new StoryFormatException("$.schema", $"unsupported kept-visit schema {schema}; expected {SavedVisit.CurrentSchema}");
            if (r.String("kind") != "kept-visit") throw new StoryFormatException("$.kind", "expected a kept visit");
            var id = r.String("id");
            if (!IsValidId(id)) throw new StoryFormatException("$.id", $"'{id}' is not a valid visit id");
            if (!DateTimeOffset.TryParseExact(r.String("saved_at"), "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var savedAt))
                throw new StoryFormatException("$.saved_at", "expected a UTC time such as 2026-10-04T21:30:12Z");

            var outcomes = new Dictionary<string, Outcome>();
            var rawOutcomes = r.Object("outcomes");
            foreach (var pair in rawOutcomes)
                outcomes[pair.Key] = StoryReader.Parse(pair.Value as string ?? "", "$.outcomes." + pair.Key, StoryReader.Outcomes);

            var visit = new SavedVisit
            {
                Id = id,
                SavedAt = savedAt,
                Package = r.String("package"),
                Version = r.String("version"),
                Locale = r.String("locale"),
                Route = r.String("route"),
                Selection = ReadSelection(r.OptionalString("perspective_selection")),
                Label = r.String("label"),
                Parent = r.OptionalString("parent"),
                Next = r.OptionalString("next"),
                Keepsake = r.OptionalString("keepsake"),
                Outcomes = outcomes,
                Asked = r.OptionalStrings("asked"),
                Heard = r.OptionalStrings("heard"),
                Lingered = r.OptionalStrings("lingered"),
            };
            try
            {
                visit.Plan = StoryReader.ReadPlan(r.Object("plan"));
            }
            catch (StoryFormatException e)
            {
                visit.PlanProblem = "plan" + e.Message.Substring(1);
            }
            return visit;
        }

        static PerspectiveSelection ReadSelection(string value)
        {
            if (value == null) return PerspectiveSelection.Unknown;
            if (value == "explicit") return PerspectiveSelection.Explicit;
            if (value == "default") return PerspectiveSelection.Default;
            throw new StoryFormatException("$.perspective_selection", "expected explicit or default");
        }
    }

    public sealed class VisitStoreException : Exception
    {
        public VisitStoreException(string message, Exception inner = null) : base(message, inner) { }
    }

    /// <summary>Kept visits found by a store, newest first, and anything it could not read (which it never deletes).</summary>
    public sealed class VisitStoreContents
    {
        public VisitStoreContents(IReadOnlyList<SavedVisit> visits, IReadOnlyList<string> problems)
        {
            Visits = visits.OrderByDescending(v => v.SavedAt).ThenByDescending(v => v.Id, StringComparer.Ordinal).ToList();
            Problems = problems;
        }

        public IReadOnlyList<SavedVisit> Visits { get; }
        public IReadOnlyList<string> Problems { get; }
    }

    /// <summary>
    /// Where kept visits live. Saving happens only when the visitor chooses to keep a visit; forgetting removes it.
    /// Implementations report failure with <see cref="VisitStoreException"/> and leave earlier visits intact.
    /// </summary>
    public interface IVisitStore
    {
        VisitStoreContents Load();
        void Save(SavedVisit visit);
        /// <summary>Atomically replaces the one active kept visit. Obsolete records must never reappear after forgetting.</summary>
        void Replace(SavedVisit visit);
        void Delete(string id);
    }
}
