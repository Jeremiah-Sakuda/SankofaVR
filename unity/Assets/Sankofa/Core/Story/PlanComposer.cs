using System;
using System.Collections.Generic;
using System.Linq;

namespace Sankofa.Core.Story
{
    /// <summary>What a continuation may recall: the kept visit it follows, its route and how its invitations resolved.</summary>
    public sealed class ParentVisit
    {
        public ParentVisit(string visitId, string route, IReadOnlyDictionary<string, Outcome> outcomes)
        {
            VisitId = visitId ?? throw new ArgumentNullException(nameof(visitId));
            Route = route ?? throw new ArgumentNullException(nameof(route));
            Outcomes = outcomes ?? new Dictionary<string, Outcome>();
        }

        public string VisitId { get; }
        public string Route { get; }
        public IReadOnlyDictionary<string, Outcome> Outcomes { get; }
    }

    /// <summary>
    /// Composes the plan for one route: its beats in order, with lines that recall the kept parent visit kept only
    /// when that visit actually had the recalled outcome. The plan carries only the scenes and objects the route can
    /// stage, so a kept visit never depends on unrelated content. The result still has to pass
    /// <see cref="PlanValidator"/>; composition never makes content playable by itself.
    /// </summary>
    public static class PlanComposer
    {
        public static StoryPlan Compose(StoryPackage package, string routeId, ParentVisit parent = null)
        {
            if (package == null) throw new ArgumentNullException(nameof(package));
            var route = package.Routes.FirstOrDefault(r => r.Id == routeId) ?? throw new ArgumentException($"Unknown route '{routeId}'", nameof(routeId));
            if (route.IsContinuation)
            {
                if (parent == null) throw new ArgumentException($"Route '{route.Id}' continues a kept visit of '{route.Follows}' and needs it", nameof(parent));
                if (parent.Route != route.Follows) throw new ArgumentException($"Route '{route.Id}' follows '{route.Follows}', not '{parent.Route}'", nameof(parent));
            }
            else if (parent != null) throw new ArgumentException($"Route '{route.Id}' is a first story and continues nothing", nameof(parent));

            var pool = new Dictionary<string, Beat>();
            foreach (var b in package.Beats)
                if (b.Id != null && !pool.ContainsKey(b.Id)) pool.Add(b.Id, b);
            var beats = route.Beats.Select(id => pool.TryGetValue(id, out var beat) ? Resolve(beat, parent) : throw new ArgumentException($"Route '{route.Id}' uses unknown beat '{id}'")).ToList();

            var sceneIds = new HashSet<string>(beats.Select(b => b.Scene));
            foreach (var s in package.Scenes)
                if (s.Kind == SceneKind.Hearth) sceneIds.Add(s.Id);
            var scenes = package.Scenes.Where(s => sceneIds.Contains(s.Id)).ToList();
            var sceneOf = package.Objects.Where(o => o.Id != null).GroupBy(o => o.Id).ToDictionary(g => g.Key, g => g.First().Scene);
            // A counterpart of something in a scene this route never visits can never appear in it.
            var objects = package.Objects.Where(o => sceneIds.Contains(o.Scene) &&
                (o.Continues == null || (sceneOf.TryGetValue(o.Continues, out var other) && sceneIds.Contains(other)))).ToList();

            return new StoryPlan
            {
                Schema = StoryReader.SupportedSchema,
                Package = package.Id,
                Version = package.Version,
                Route = route.Id,
                Label = route.Label,
                Parent = parent?.VisitId,
                Next = route.Next,
                Title = package.Title,
                Locale = package.Locale,
                Status = package.Status,
                Note = package.Note,
                Scenes = scenes,
                Objects = objects,
                Claims = package.Claims,
                Beats = beats,
                EarlyReturn = package.EarlyReturn,
                Keepsake = route.Keepsake,
            };
        }

        /// <summary>
        /// Keeps a recall line only when the kept visit had that outcome. Without a parent nothing is resolved, so
        /// the validator reports the recall instead of a story silently losing a line.
        /// </summary>
        static Beat Resolve(Beat beat, ParentVisit parent)
        {
            if (parent == null || beat.Lines.All(l => l.Recall == null)) return beat;
            return new Beat
            {
                Id = beat.Id,
                Scene = beat.Scene,
                Lines = beat.Lines.Where(l => l.Recall == null || (parent.Outcomes.TryGetValue(l.Recall.Invitation, out var o) && o == l.Recall.Outcome))
                    .Select(l => l.Recall == null ? l : Resolved(l)).ToList(),
                Invitation = beat.Invitation,
                Questions = beat.Questions,
            };
        }

        static Line Resolved(Line line) => new Line
        {
            Id = line.Id,
            Text = line.Text,
            Audio = line.Audio,
            Seconds = line.Seconds,
            PauseAfter = line.PauseAfter,
            Fire = line.Fire,
            When = line.When,
            Actions = line.Actions,
            Claims = line.Claims,
        };
    }
}
