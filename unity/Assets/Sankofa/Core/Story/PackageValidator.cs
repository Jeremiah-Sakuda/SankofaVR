using System;
using System.Collections.Generic;
using System.Linq;

namespace Sankofa.Core.Story
{
    /// <summary>
    /// A package whose every route composes into a valid plan, for every way a kept parent visit could have gone.
    /// Only <see cref="PackageValidator.Accept"/> creates one.
    /// </summary>
    public sealed class PlayablePackage
    {
        readonly Dictionary<string, Route> routes;

        internal PlayablePackage(StoryPackage package, ValidationReport report)
        {
            Package = package;
            Report = report;
            routes = package.Routes.ToDictionary(r => r.Id);
        }

        public StoryPackage Package { get; }
        public ValidationReport Report { get; }

        /// <summary>Routes a visitor can choose without a kept visit, in package order.</summary>
        public IEnumerable<Route> FirstRoutes => Package.Routes.Where(r => !r.IsContinuation);
        public Route Route(string id) => id != null && routes.TryGetValue(id, out var r) ? r : null;

        public StoryPlan Compose(string routeId, ParentVisit parent = null, bool returning = false) => PlanComposer.Compose(Package, routeId, parent, returning);
    }

    /// <summary>
    /// Checks a content package: its hearth lines and routes, then composes every route (each continuation once
    /// per combination of its parent's outcomes) and validates each plan, including against the stage inventory.
    /// </summary>
    public static class PackageValidator
    {
        public static PlayablePackage Accept(StoryPackage package, out ValidationReport report, IStageInventory inventory = null)
        {
            report = Validate(package, inventory);
            return report.IsValid ? new PlayablePackage(package, report) : null;
        }

        public static ValidationReport Validate(StoryPackage package, IStageInventory inventory = null)
        {
            if (package == null) throw new ArgumentNullException(nameof(package));
            var issues = new List<ValidationIssue>();
            var seen = new HashSet<string>();
            void Add(ValidationIssue issue)
            {
                if (seen.Add(issue.Severity + issue.Code + issue.Path + issue.Message)) issues.Add(issue);
            }
            void Error(string code, string path, string message) => Add(new ValidationIssue(IssueSeverity.Error, code, path, message));
            void Warning(string code, string path, string message) => Add(new ValidationIssue(IssueSeverity.Warning, code, path, message));

            foreach (var (name, value) in new[] { ("id", package.Id), ("version", package.Version), ("title", package.Title), ("locale", package.Locale) })
                if (string.IsNullOrWhiteSpace(value)) Error("package.field", "$." + name, $"package {name} is empty");
            if (package.Status != ContentStatus.Reviewed)
                Warning("content.unreviewed", "$.status", $"content status is {package.Status.ToString().ToLowerInvariant()}; it must not be presented as reviewed release content");

            CheckHearthLines(package, Error);

            var routes = new Dictionary<string, Route>();
            var pool = new HashSet<string>(package.Beats.Where(b => b.Id != null).Select(b => b.Id));
            for (var i = 0; i < package.Beats.Count; i++)
                if (package.Beats.Take(i).Any(b => b.Id == package.Beats[i].Id))
                    Error("id.duplicate", $"$.beats[{i}]", $"duplicate beat id '{package.Beats[i].Id}'");
            for (var i = 0; i < package.Routes.Count; i++)
            {
                var r = package.Routes[i];
                var path = $"$.routes[{i}]";
                if (string.IsNullOrWhiteSpace(r.Id)) Error("id.empty", path, "route id is empty");
                else if (!routes.ContainsKey(r.Id)) routes.Add(r.Id, r);
                else Error("id.duplicate", path, $"duplicate route id '{r.Id}'");
                if (string.IsNullOrWhiteSpace(r.Label)) Error("route.label", path + ".label", $"route '{r.Id}' needs a label for its choice");
                if (r.IsContinuation && r.ReturningBeats.Count > 0) Error("route.returning", path, "a continuation cannot define a returning telling");
                if (r.Beats.Count == 0) Error("route.beats", path + ".beats", $"route '{r.Id}' has no beats");
                foreach (var beat in r.Beats.Concat(r.ReturningBeats))
                    if (!pool.Contains(beat)) Error("route.beat", path + ".beats", $"route '{r.Id}' uses unknown beat '{beat}'");
            }
            if (!package.Routes.Any(r => !r.IsContinuation)) Error("routes.first", "$.routes", "at least one route must be a first story that continues nothing");
            for (var i = 0; i < package.Routes.Count; i++)
            {
                var r = package.Routes[i];
                var path = $"$.routes[{i}]";
                if (r.Follows != null && !routes.ContainsKey(r.Follows))
                    Error("route.follows", path + ".follows", $"route '{r.Id}' follows unknown route '{r.Follows}'");
                if (r.Next != null && (!routes.TryGetValue(r.Next, out var next) || next.Follows != r.Id))
                    Error("route.next", path + ".next", $"route '{r.Id}' continues with '{r.Next}', which must be a route that follows it");
            }
            var used = new HashSet<string>(package.Routes.SelectMany(r => r.Beats.Concat(r.ReturningBeats)));
            for (var i = 0; i < package.Beats.Count; i++)
                if (!used.Contains(package.Beats[i].Id)) Warning("beat.unused", $"$.beats[{i}]", $"beat '{package.Beats[i].Id}' is not used by any route");

            if (issues.Any(i => i.Severity == IssueSeverity.Error)) return new ValidationReport(issues);

            for (var i = 0; i < package.Routes.Count; i++)
            {
                var r = package.Routes[i];
                var path = $"$.routes[{i}]";
                if (!CheckRecalls(package, r, routes, path, Error)) continue;
                if (r.ReturningBeats.Count > 0)
                    foreach (var issue in PlanValidator.Validate(PlanComposer.Compose(package, r.Id, returning: true), inventory).Issues)
                        if (issue.Code != "content.unreviewed")
                            Add(new ValidationIssue(issue.Severity, issue.Code, path + "(returning)" + issue.Path.Substring(1), issue.Message));
                foreach (var parent in Parents(package, r, routes))
                {
                    var plan = PlanComposer.Compose(package, r.Id, parent);
                    var variant = parent == null || parent.Outcomes.Count == 0 ? "" : " after " + string.Join(", ", parent.Outcomes.Select(o => o.Key + ":" + (o.Value == Outcome.Accepted ? "accepted" : "default")));
                    foreach (var issue in PlanValidator.Validate(plan, inventory).Issues)
                    {
                        if (issue.Code == "content.unreviewed") continue;
                        Add(new ValidationIssue(issue.Severity, issue.Code, $"{path}(plan){issue.Path.Substring(1)}", $"route '{r.Id}'{variant}: {issue.Message}"));
                    }
                }
            }
            return new ValidationReport(issues);
        }

        static void CheckHearthLines(StoryPackage package, Action<string, string, string> error)
        {
            if (package.Welcome.Count == 0) error("welcome.empty", "$.welcome", "the fire needs a welcome for a first visit");
            var ids = new HashSet<string>();
            foreach (var b in package.Beats)
            {
                foreach (var l in b.Lines) ids.Add(l.Id);
                if (b.Invitation != null) foreach (var l in b.Invitation.Accepted.Concat(b.Invitation.Default)) ids.Add(l.Id);
                foreach (var q in b.Questions) foreach (var l in q.Lines) ids.Add(l.Id);
            }
            foreach (var l in package.EarlyReturn) ids.Add(l.Id);
            foreach (var (name, lines) in new[] { ("welcome", package.Welcome), ("welcome_back", package.WelcomeBack), ("choose", package.Choose) })
            {
                for (var i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
                    var path = $"$.{name}[{i}]";
                    if (string.IsNullOrWhiteSpace(line.Id)) error("id.empty", path, "line id is empty");
                    else if (!ids.Add(line.Id)) error("id.duplicate", path, $"duplicate line id '{line.Id}'");
                    if (string.IsNullOrWhiteSpace(line.Text)) error("line.text", path + ".text", $"line '{line.Id}' has no text for its caption");
                    if (!(line.Seconds > 0) || double.IsInfinity(line.Seconds)) error("line.seconds", path + ".seconds", $"line '{line.Id}' needs a finite positive duration");
                    if (!(line.PauseAfter >= 0) || double.IsInfinity(line.PauseAfter)) error("line.pause", path + ".pause_after", $"line '{line.Id}' has an invalid pause");
                    if (line.When != null) error("when.context", path + ".when", $"line '{line.Id}' is spoken before any story; it cannot depend on an outcome");
                    if (line.Actions.Count > 0) error("action.context", path + ".actions", $"line '{line.Id}' is spoken before any story; it cannot act on staged objects");
                    if (line.Claims.Count > 0 && !package.Claims.Any(c => line.Claims.Contains(c.Id))) error("claim.unknown", path + ".claims", $"line '{line.Id}' cites an unknown claim");
                }
            }
        }

        /// <summary>Recall lines belong to continuations and must name an invitation of the route they follow.</summary>
        static bool CheckRecalls(StoryPackage package, Route route, IReadOnlyDictionary<string, Route> routes, string path, Action<string, string, string> error)
        {
            var ok = true;
            var parentInvitations = route.IsContinuation ? Invitations(package, routes[route.Follows]) : new HashSet<string>();
            foreach (var beatId in route.Beats.Concat(route.ReturningBeats).Distinct())
            {
                var beat = package.Beats.First(b => b.Id == beatId);
                foreach (var line in beat.Lines.Where(l => l.Recall != null))
                {
                    if (!route.IsContinuation)
                    {
                        error("recall.context", path, $"route '{route.Id}' is a first story, but beat '{beat.Id}' line '{line.Id}' recalls a kept visit");
                        ok = false;
                    }
                    else if (!parentInvitations.Contains(line.Recall.Invitation))
                    {
                        error("recall.unknown", path, $"line '{line.Id}' recalls '{line.Recall.Invitation}', which is not an invitation of '{route.Follows}'");
                        ok = false;
                    }
                }
            }
            return ok;
        }

        static HashSet<string> Invitations(StoryPackage package, Route route) =>
            new HashSet<string>(route.Beats.Select(id => package.Beats.First(b => b.Id == id).Invitation).Where(i => i != null).Select(i => i.Id));

        /// <summary>No parent for a first story; for a continuation, one parent per combination of the followed route's outcomes.</summary>
        static IEnumerable<ParentVisit> Parents(StoryPackage package, Route route, IReadOnlyDictionary<string, Route> routes)
        {
            if (!route.IsContinuation)
            {
                yield return null;
                yield break;
            }
            var invitations = Invitations(package, routes[route.Follows]).ToList();
            var count = Math.Min(invitations.Count, PlanValidator.MaxEnumeratedInvitations);
            for (var mask = 0; mask < 1 << count; mask++)
            {
                var outcomes = new Dictionary<string, Outcome>();
                for (var i = 0; i < invitations.Count; i++)
                    outcomes[invitations[i]] = i < count && (mask & (1 << i)) != 0 ? Outcome.Default : Outcome.Accepted;
                yield return new ParentVisit("validation", route.Follows, outcomes);
            }
        }
    }
}
