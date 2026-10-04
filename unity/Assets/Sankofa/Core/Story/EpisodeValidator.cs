using System;
using System.Collections.Generic;
using System.Linq;

namespace Sankofa.Core.Story
{
    public enum IssueSeverity { Error, Warning }

    public sealed class ValidationIssue
    {
        public ValidationIssue(IssueSeverity severity, string code, string path, string message)
        {
            Severity = severity;
            Code = code;
            Path = path;
            Message = message;
        }

        public IssueSeverity Severity { get; }
        /// <summary>Stable identifier for tests and tooling, such as "action.capability".</summary>
        public string Code { get; }
        public string Path { get; }
        public string Message { get; }

        public override string ToString() => $"{Severity} {Code} at {Path}: {Message}";
    }

    public sealed class ValidationReport
    {
        internal ValidationReport(IReadOnlyList<ValidationIssue> issues) => Issues = issues;

        public IReadOnlyList<ValidationIssue> Issues { get; }
        public IEnumerable<ValidationIssue> Errors => Issues.Where(i => i.Severity == IssueSeverity.Error);
        public IEnumerable<ValidationIssue> Warnings => Issues.Where(i => i.Severity == IssueSeverity.Warning);
        public bool IsValid => !Errors.Any();

        public override string ToString() => Issues.Count == 0 ? "No issues" : string.Join("\n", Issues);
    }

    /// <summary>
    /// An episode that passed validation, with lookups the story run and application flow rely on.
    /// Only <see cref="EpisodeValidator.Accept"/> creates one: unvalidated content is never playable.
    /// </summary>
    public sealed class PlayableEpisode
    {
        readonly Dictionary<string, SceneDef> scenes;
        readonly Dictionary<string, StoryObject> objects;
        readonly Dictionary<string, Invitation> invitations;

        internal PlayableEpisode(Episode episode, ValidationReport report)
        {
            Episode = episode;
            Report = report;
            scenes = episode.Scenes.ToDictionary(s => s.Id);
            objects = episode.Objects.ToDictionary(o => o.Id);
            invitations = episode.Beats.Where(b => b.Invitation != null).ToDictionary(b => b.Invitation.Id, b => b.Invitation);
            Hearth = episode.Scenes.Single(s => s.Kind == SceneKind.Hearth);
        }

        public Episode Episode { get; }
        /// <summary>Warnings remain visible after acceptance, for example placeholder content.</summary>
        public ValidationReport Report { get; }
        public SceneDef Hearth { get; }

        public SceneDef Scene(string id) => scenes[id];
        public StoryObject Object(string id) => objects[id];
        public Invitation Invitation(string id) => invitations[id];
        public IEnumerable<StoryObject> ObjectsIn(string sceneId) => Episode.Objects.Where(o => o.Scene == sceneId);
    }

    /// <summary>
    /// Checks an episode against the staged capabilities and story rules before it can play: references resolve,
    /// actions are permitted for their objects and scenes, every invitation has a passive path, outcome-dependent
    /// lines follow their invitation, documented claims have sources and the visit ends at the hearth.
    /// </summary>
    public static class EpisodeValidator
    {
        public static PlayableEpisode Accept(Episode episode, out ValidationReport report)
        {
            report = Validate(episode);
            return report.IsValid ? new PlayableEpisode(episode, report) : null;
        }

        public static ValidationReport Validate(Episode episode)
        {
            var v = new Run(episode ?? throw new ArgumentNullException(nameof(episode)));
            v.Check();
            return new ValidationReport(v.Issues);
        }

        sealed class Run
        {
            readonly Episode e;
            readonly List<ValidationIssue> issues = new List<ValidationIssue>();
            readonly Dictionary<string, SceneDef> scenes = new Dictionary<string, SceneDef>();
            readonly Dictionary<string, StoryObject> objects = new Dictionary<string, StoryObject>();
            readonly Dictionary<string, Claim> claims = new Dictionary<string, Claim>();
            readonly HashSet<string> lineIds = new HashSet<string>();
            /// <summary>Invitation id to the index of the beat that offers it.</summary>
            readonly Dictionary<string, int> invitationBeats = new Dictionary<string, int>();

            public Run(Episode episode) => e = episode;
            public List<ValidationIssue> Issues => issues;

            void Error(string code, string path, string message) => issues.Add(new ValidationIssue(IssueSeverity.Error, code, path, message));
            void Warning(string code, string path, string message) => issues.Add(new ValidationIssue(IssueSeverity.Warning, code, path, message));

            public void Check()
            {
                foreach (var (name, value) in new[] { ("id", e.Id), ("version", e.Version), ("title", e.Title), ("locale", e.Locale) })
                    if (string.IsNullOrWhiteSpace(value)) Error("episode.field", "$." + name, $"episode {name} is empty");
                if (e.Status != ContentStatus.Reviewed)
                    Warning("content.unreviewed", "$.status", $"content status is {e.Status.ToString().ToLowerInvariant()}; it must not be presented as reviewed release content");

                IndexScenes();
                IndexObjects();
                IndexClaims();
                IndexInvitations();

                CheckLines(e.Welcome, "$.welcome", Hearth?.Id, -1, null, allowConditions: false);
                CheckBeats();
                CheckLines(e.EarlyReturn, "$.early_return", Hearth?.Id, -1, null, allowConditions: false);

                if (e.Keepsake != null && !objects.ContainsKey(e.Keepsake))
                    Error("keepsake.unknown", "$.keepsake", $"keepsake '{e.Keepsake}' is not a staged object");
            }

            SceneDef Hearth { get; set; }

            void IndexScenes()
            {
                for (var i = 0; i < e.Scenes.Count; i++)
                {
                    var s = e.Scenes[i];
                    var path = $"$.scenes[{i}]";
                    if (string.IsNullOrWhiteSpace(s.Id)) Error("id.empty", path, "scene id is empty");
                    else if (!scenes.ContainsKey(s.Id)) scenes.Add(s.Id, s);
                    else Error("id.duplicate", path, $"duplicate scene id '{s.Id}'");
                }
                var hearths = e.Scenes.Where(s => s.Kind == SceneKind.Hearth).ToList();
                if (hearths.Count != 1) Error("hearth.count", "$.scenes", $"exactly one hearth scene is required; found {hearths.Count}");
                else Hearth = hearths[0];
            }

            void IndexObjects()
            {
                for (var i = 0; i < e.Objects.Count; i++)
                {
                    var o = e.Objects[i];
                    var path = $"$.objects[{i}]";
                    if (string.IsNullOrWhiteSpace(o.Id)) Error("id.empty", path, "object id is empty");
                    else if (!objects.ContainsKey(o.Id)) objects.Add(o.Id, o);
                    else Error("id.duplicate", path, $"duplicate object id '{o.Id}'");
                    if (!scenes.ContainsKey(o.Scene ?? "")) Error("object.scene", path + ".scene", $"object '{o.Id}' is in unknown scene '{o.Scene}'");
                    if (o.Capabilities.Count == 0) Error("object.capabilities", path + ".capabilities", $"object '{o.Id}' supports no actions");
                }
                for (var i = 0; i < e.Objects.Count; i++)
                {
                    var o = e.Objects[i];
                    if (o.Continues == null) continue;
                    var path = $"$.objects[{i}].continues";
                    if (!objects.TryGetValue(o.Continues, out var previous))
                        Error("object.continues", path, $"object '{o.Id}' continues unknown object '{o.Continues}'");
                    else if (previous.Scene == o.Scene)
                        Error("object.continues", path, $"object '{o.Id}' continues '{o.Continues}' in the same scene; continuity crosses a transition");
                    else if (previous.Continues != null)
                        Error("object.continues", path, $"object '{o.Id}' continues '{o.Continues}', which itself continues another object; chain continuity explicitly per transition");
                }
            }

            void IndexClaims()
            {
                for (var i = 0; i < e.Claims.Count; i++)
                {
                    var c = e.Claims[i];
                    var path = $"$.claims[{i}]";
                    if (string.IsNullOrWhiteSpace(c.Id)) Error("id.empty", path, "claim id is empty");
                    else if (!claims.ContainsKey(c.Id)) claims.Add(c.Id, c);
                    else Error("id.duplicate", path, $"duplicate claim id '{c.Id}'");
                    if (string.IsNullOrWhiteSpace(c.Text)) Error("claim.text", path + ".text", $"claim '{c.Id}' has no text");
                    if (c.State == EvidenceState.Documented && c.Sources.Count(s => !string.IsNullOrWhiteSpace(s)) == 0)
                        Error("claim.sources", path + ".sources", $"documented claim '{c.Id}' has no source reference");
                }
            }

            void IndexInvitations()
            {
                for (var b = 0; b < e.Beats.Count; b++)
                {
                    var inv = e.Beats[b].Invitation;
                    if (inv == null) continue;
                    var path = $"$.beats[{b}].invitation";
                    if (string.IsNullOrWhiteSpace(inv.Id)) Error("id.empty", path, "invitation id is empty");
                    else if (!invitationBeats.ContainsKey(inv.Id)) invitationBeats.Add(inv.Id, b);
                    else Error("id.duplicate", path, $"duplicate invitation id '{inv.Id}'");
                }
            }

            void CheckBeats()
            {
                if (e.Beats.Count == 0)
                {
                    Error("beats.empty", "$.beats", "an episode needs at least one beat");
                    return;
                }
                var beatIds = new HashSet<string>();
                for (var b = 0; b < e.Beats.Count; b++)
                {
                    var beat = e.Beats[b];
                    var path = $"$.beats[{b}]";
                    if (string.IsNullOrWhiteSpace(beat.Id)) Error("id.empty", path, "beat id is empty");
                    else if (!beatIds.Add(beat.Id)) Error("id.duplicate", path, $"duplicate beat id '{beat.Id}'");
                    var known = scenes.ContainsKey(beat.Scene ?? "");
                    if (!known) Error("beat.scene", path + ".scene", $"beat '{beat.Id}' is in unknown scene '{beat.Scene}'");
                    if (beat.Lines.Count == 0) Error("beat.lines", path + ".lines", $"beat '{beat.Id}' has no lines");

                    var scene = known ? beat.Scene : null;
                    CheckLines(beat.Lines, path + ".lines", scene, b, beat.Invitation, allowConditions: true);
                    CheckCoverage(beat, path);
                    if (beat.Invitation != null) CheckInvitation(beat, b, path + ".invitation", scene);
                }
                var last = e.Beats[e.Beats.Count - 1];
                if (Hearth != null && last.Scene != Hearth.Id)
                    Error("ending.hearth", $"$.beats[{e.Beats.Count - 1}].scene", "the final beat must return the visitor to the hearth");
            }

            void CheckInvitation(Beat beat, int beatIndex, string path, string scene)
            {
                var inv = beat.Invitation;
                if (!objects.TryGetValue(inv.Object ?? "", out var target))
                    Error("invitation.object", path + ".object", $"invitation '{inv.Id}' offers unknown object '{inv.Object}'");
                else
                {
                    if (scene != null && target.Scene != scene)
                        Error("invitation.object", path + ".object", $"invitation '{inv.Id}' offers '{inv.Object}', which is not staged in scene '{scene}'");
                    if (!target.Capabilities.Contains(ActionKind.Offer))
                        Error("invitation.object", path + ".object", $"object '{inv.Object}' cannot be offered");
                }
                if (!(inv.WaitSeconds > 0) || double.IsInfinity(inv.WaitSeconds))
                    Error("invitation.wait", path + ".wait_seconds", $"invitation '{inv.Id}' needs a finite positive wait before its default path");
                if (string.IsNullOrWhiteSpace(inv.Alternative))
                    Error("invitation.alternative", path + ".alternative", $"invitation '{inv.Id}' needs a label for its nearby select alternative");
                if (!beat.Lines.Any(l => l.When == null && l.Actions.Any(a => a.Kind == ActionKind.Offer && a.Object == inv.Object)))
                    Error("invitation.offer", path, $"no unconditional line in beat '{beat.Id}' offers '{inv.Object}' before inviting the visitor to take it");
                if (inv.Accepted.Count == 0)
                    Error("invitation.accepted", path + ".accepted", $"invitation '{inv.Id}' must acknowledge an accepted action");
                if (inv.Default.Count == 0)
                    Error("invitation.default", path + ".default", $"invitation '{inv.Id}' needs an authored passive path");
                CheckLines(inv.Accepted, path + ".accepted", scene, beatIndex, null, allowConditions: false);
                CheckLines(inv.Default, path + ".default", scene, beatIndex, null, allowConditions: false);
            }

            /// <summary>Every beat must say something whatever the earlier outcomes were.</summary>
            void CheckCoverage(Beat beat, string path)
            {
                if (beat.Lines.Count == 0 || beat.Lines.Any(l => l.When == null)) return;
                var covered = beat.Lines.GroupBy(l => l.When.Invitation)
                    .Any(g => g.Any(l => l.When.Outcome == Outcome.Accepted) && g.Any(l => l.When.Outcome == Outcome.Default));
                if (!covered)
                    Error("beat.coverage", path + ".lines", $"beat '{beat.Id}' plays nothing for some earlier outcome; add an unconditional line or cover both outcomes");
            }

            void CheckLines(IReadOnlyList<Line> lines, string path, string scene, int beatIndex, Invitation offeredHere, bool allowConditions)
            {
                for (var i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
                    var p = $"{path}[{i}]";
                    if (string.IsNullOrWhiteSpace(line.Id)) Error("id.empty", p, "line id is empty");
                    else if (!lineIds.Add(line.Id)) Error("id.duplicate", p, $"duplicate line id '{line.Id}'");
                    if (string.IsNullOrWhiteSpace(line.Text)) Error("line.text", p + ".text", $"line '{line.Id}' has no text for its caption");
                    if (!(line.Seconds > 0) || double.IsInfinity(line.Seconds))
                        Error("line.seconds", p + ".seconds", $"line '{line.Id}' needs a finite positive duration");
                    if (!(line.PauseAfter >= 0) || double.IsInfinity(line.PauseAfter))
                        Error("line.pause", p + ".pause_after", $"line '{line.Id}' has an invalid pause");

                    foreach (var claim in line.Claims)
                        if (!claims.ContainsKey(claim)) Error("claim.unknown", p + ".claims", $"line '{line.Id}' cites unknown claim '{claim}'");

                    if (line.When != null)
                    {
                        if (!allowConditions)
                            Error("when.context", p + ".when", $"line '{line.Id}' cannot depend on an outcome here");
                        else if (!invitationBeats.TryGetValue(line.When.Invitation, out var offeredAt))
                            Error("when.unknown", p + ".when", $"line '{line.Id}' depends on unknown invitation '{line.When.Invitation}'");
                        else if (offeredAt >= beatIndex)
                            Error("when.order", p + ".when", $"line '{line.Id}' depends on invitation '{line.When.Invitation}', which is not resolved before this beat");
                    }

                    for (var a = 0; a < line.Actions.Count; a++)
                        CheckAction(line, line.Actions[a], $"{p}.actions[{a}]", scene, offeredHere);
                }
            }

            void CheckAction(Line line, StoryAction action, string path, string scene, Invitation offeredHere)
            {
                if (!objects.TryGetValue(action.Object ?? "", out var target))
                {
                    Error("action.object", path, $"line '{line.Id}' acts on unknown object '{action.Object}'");
                    return;
                }
                if (scene != null && target.Scene != scene)
                    Error("action.scene", path, $"line '{line.Id}' acts on '{target.Id}', which is not staged in scene '{scene}'");
                if (!target.Capabilities.Contains(action.Kind))
                    Error("action.capability", path, $"object '{target.Id}' does not support {action.Kind.ToString().ToLowerInvariant()}");
                if (action.Kind == ActionKind.Offer && (offeredHere == null || offeredHere.Object != target.Id))
                    Error("action.offer", path, $"line '{line.Id}' offers '{target.Id}' without an invitation for it in the same beat");
            }
        }
    }
}
