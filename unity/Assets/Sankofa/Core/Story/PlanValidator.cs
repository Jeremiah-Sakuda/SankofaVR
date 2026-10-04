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

    /// <summary>What the presentation can actually stage. Plans that need anything else are rejected before they play.</summary>
    public interface IStageInventory
    {
        bool HasScene(string sceneId);
        bool HasObject(string objectId, string sceneId);
    }

    /// <summary>
    /// A plan that passed validation, with lookups the story run and application flow rely on.
    /// Only <see cref="PlanValidator.Accept"/> creates one: unvalidated content is never playable.
    /// </summary>
    public sealed class PlayablePlan
    {
        readonly Dictionary<string, SceneDef> scenes;
        readonly Dictionary<string, StoryObject> objects;
        readonly Dictionary<string, Invitation> invitations;
        readonly Dictionary<string, Question> questions;

        internal PlayablePlan(StoryPlan plan, ValidationReport report)
        {
            Plan = plan;
            Report = report;
            scenes = plan.Scenes.ToDictionary(s => s.Id);
            objects = plan.Objects.ToDictionary(o => o.Id);
            invitations = plan.Beats.Where(b => b.Invitation != null).ToDictionary(b => b.Invitation.Id, b => b.Invitation);
            questions = plan.Beats.SelectMany(b => b.Questions).ToDictionary(q => q.Id);
            Hearth = plan.Scenes.Single(s => s.Kind == SceneKind.Hearth);
        }

        public StoryPlan Plan { get; }
        /// <summary>Warnings remain visible after acceptance, for example placeholder content.</summary>
        public ValidationReport Report { get; }
        public SceneDef Hearth { get; }

        public SceneDef Scene(string id) => scenes[id];
        public StoryObject Object(string id) => objects[id];
        public bool HasObject(string id) => id != null && objects.ContainsKey(id);
        public Invitation Invitation(string id) => invitations[id];
        public Question Question(string id) => questions.TryGetValue(id ?? "", out var q) ? q : null;
        public IEnumerable<StoryObject> ObjectsIn(string sceneId) => Plan.Objects.Where(o => o.Scene == sceneId);

        /// <summary>Objects that correspond across a change from one scene to the other, as (source, target) pairs.</summary>
        public IEnumerable<(string From, string To)> CorrespondencesBetween(string from, string to) => Correspondences(Plan.Objects, from, to);

        internal static IEnumerable<(string From, string To)> Correspondences(IReadOnlyList<StoryObject> objects, string from, string to)
        {
            var byId = new Dictionary<string, StoryObject>();
            foreach (var o in objects)
                if (o.Id != null && !byId.ContainsKey(o.Id)) byId.Add(o.Id, o);
            foreach (var o in objects)
            {
                if (o.Continues == null || !byId.TryGetValue(o.Continues, out var previous)) continue;
                if (o.Scene == to && previous.Scene == from) yield return (previous.Id, o.Id);
                else if (o.Scene == from && previous.Scene == to) yield return (o.Id, previous.Id);
            }
        }
    }

    /// <summary>
    /// Checks a plan against the staged capabilities and story rules before it can play: references resolve,
    /// actions are permitted for their objects and scenes, every invitation has a passive path, outcome-dependent
    /// lines follow their invitation, documented claims have sources and the visit ends at the hearth. It then
    /// walks every combination of invitation outcomes, so nothing is touched, placed or kept that the visitor
    /// could not see or have at that moment, and checks the stage can show everything the plan uses.
    /// </summary>
    public static class PlanValidator
    {
        /// <summary>Beyond this many invitations only the all-accepted and all-default paths are walked.</summary>
        public const int MaxEnumeratedInvitations = 10;

        public static PlayablePlan Accept(StoryPlan plan, out ValidationReport report, IStageInventory inventory = null)
        {
            report = Validate(plan, inventory);
            return report.IsValid ? new PlayablePlan(plan, report) : null;
        }

        public static ValidationReport Validate(StoryPlan plan, IStageInventory inventory = null)
        {
            var v = new Run(plan ?? throw new ArgumentNullException(nameof(plan)), inventory);
            v.Check();
            return new ValidationReport(v.Issues);
        }

        sealed class Run
        {
            readonly StoryPlan p;
            readonly IStageInventory inventory;
            readonly List<ValidationIssue> issues = new List<ValidationIssue>();
            readonly HashSet<string> reported = new HashSet<string>();
            readonly Dictionary<string, SceneDef> scenes = new Dictionary<string, SceneDef>();
            readonly Dictionary<string, StoryObject> objects = new Dictionary<string, StoryObject>();
            readonly Dictionary<string, Claim> claims = new Dictionary<string, Claim>();
            readonly HashSet<string> lineIds = new HashSet<string>();
            readonly HashSet<string> questionIds = new HashSet<string>();
            /// <summary>Invitation id to the index of the beat that offers it.</summary>
            readonly Dictionary<string, int> invitationBeats = new Dictionary<string, int>();

            public Run(StoryPlan plan, IStageInventory inventory)
            {
                p = plan;
                this.inventory = inventory;
            }

            public List<ValidationIssue> Issues => issues;
            SceneDef Hearth { get; set; }

            void Error(string code, string path, string message)
            {
                // The staging walk visits the same line once per outcome combination; report each problem once.
                if (reported.Add(code + "@" + path)) issues.Add(new ValidationIssue(IssueSeverity.Error, code, path, message));
            }

            void Warning(string code, string path, string message) => issues.Add(new ValidationIssue(IssueSeverity.Warning, code, path, message));

            public void Check()
            {
                foreach (var (name, value) in new[] { ("package", p.Package), ("version", p.Version), ("route", p.Route), ("label", p.Label), ("title", p.Title), ("locale", p.Locale) })
                    if (string.IsNullOrWhiteSpace(value)) Error("plan.field", "$." + name, $"plan {name} is empty");
                if (p.Status != ContentStatus.Reviewed)
                    Warning("content.unreviewed", "$.status", $"content status is {p.Status.ToString().ToLowerInvariant()}; it must not be presented as reviewed release content");

                IndexScenes();
                IndexObjects();
                IndexClaims();
                IndexInvitations();

                CheckBeats();
                CheckLines(p.EarlyReturn, "$.early_return", Hearth?.Id, -1, null, allowConditions: false, answers: false);

                if (p.Keepsake != null)
                {
                    if (!objects.TryGetValue(p.Keepsake, out var keepsake)) Error("keepsake.unknown", "$.keepsake", $"keepsake '{p.Keepsake}' is not a staged object");
                    else if (Hearth != null && keepsake.Scene != Hearth.Id) Error("keepsake.scene", "$.keepsake", $"keepsake '{p.Keepsake}' must be a hearth object; it waits there after the visit");
                }

                if (issues.All(i => i.Severity != IssueSeverity.Error)) Stage();
                if (inventory != null) CheckInventory();
            }

            void IndexScenes()
            {
                for (var i = 0; i < p.Scenes.Count; i++)
                {
                    var s = p.Scenes[i];
                    var path = $"$.scenes[{i}]";
                    if (string.IsNullOrWhiteSpace(s.Id)) Error("id.empty", path, "scene id is empty");
                    else if (!scenes.ContainsKey(s.Id)) scenes.Add(s.Id, s);
                    else Error("id.duplicate", path, $"duplicate scene id '{s.Id}'");
                }
                var hearths = p.Scenes.Where(s => s.Kind == SceneKind.Hearth).ToList();
                if (hearths.Count != 1) Error("hearth.count", "$.scenes", $"exactly one hearth scene is required; found {hearths.Count}");
                else Hearth = hearths[0];
            }

            void IndexObjects()
            {
                for (var i = 0; i < p.Objects.Count; i++)
                {
                    var o = p.Objects[i];
                    var path = $"$.objects[{i}]";
                    if (string.IsNullOrWhiteSpace(o.Id)) Error("id.empty", path, "object id is empty");
                    else if (!objects.ContainsKey(o.Id)) objects.Add(o.Id, o);
                    else Error("id.duplicate", path, $"duplicate object id '{o.Id}'");
                    if (!scenes.ContainsKey(o.Scene ?? "")) Error("object.scene", path + ".scene", $"object '{o.Id}' is in unknown scene '{o.Scene}'");
                    if (o.Capabilities.Count == 0) Error("object.capabilities", path + ".capabilities", $"object '{o.Id}' supports no actions");
                }
                for (var i = 0; i < p.Objects.Count; i++)
                {
                    var o = p.Objects[i];
                    if (o.Continues == null) continue;
                    var path = $"$.objects[{i}].continues";
                    if (!objects.TryGetValue(o.Continues, out var previous))
                        Error("object.continues", path, $"object '{o.Id}' continues unknown object '{o.Continues}'");
                    else if (previous.Scene == o.Scene)
                        Error("object.continues", path, $"object '{o.Id}' continues '{o.Continues}' in the same scene; continuity crosses a transition");
                    else if (previous.Continues != null)
                        Error("object.continues", path, $"object '{o.Id}' continues '{o.Continues}', which itself continues another object; chain continuity explicitly per transition");
                }
                // One object may not become two things in the same scene: the crossing would be ambiguous.
                foreach (var group in p.Objects.Where(o => o.Continues != null).GroupBy(o => (o.Continues, o.Scene)))
                    if (group.Count() > 1)
                        Error("object.continues", "$.objects", $"'{group.Key.Continues}' continues as more than one object in scene '{group.Key.Scene}'");
            }

            void IndexClaims()
            {
                for (var i = 0; i < p.Claims.Count; i++)
                {
                    var c = p.Claims[i];
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
                for (var b = 0; b < p.Beats.Count; b++)
                {
                    var inv = p.Beats[b].Invitation;
                    if (inv == null) continue;
                    var path = $"$.beats[{b}].invitation";
                    if (string.IsNullOrWhiteSpace(inv.Id)) Error("id.empty", path, "invitation id is empty");
                    else if (!invitationBeats.ContainsKey(inv.Id)) invitationBeats.Add(inv.Id, b);
                    else Error("id.duplicate", path, $"duplicate invitation id '{inv.Id}'");
                }
            }

            void CheckBeats()
            {
                if (p.Beats.Count == 0)
                {
                    Error("beats.empty", "$.beats", "a plan needs at least one beat");
                    return;
                }
                var beatIds = new HashSet<string>();
                for (var b = 0; b < p.Beats.Count; b++)
                {
                    var beat = p.Beats[b];
                    var path = $"$.beats[{b}]";
                    if (string.IsNullOrWhiteSpace(beat.Id)) Error("id.empty", path, "beat id is empty");
                    else if (!beatIds.Add(beat.Id)) Error("id.duplicate", path, $"duplicate beat id '{beat.Id}'");
                    var known = scenes.ContainsKey(beat.Scene ?? "");
                    if (!known) Error("beat.scene", path + ".scene", $"beat '{beat.Id}' is in unknown scene '{beat.Scene}'");
                    if (beat.Lines.Count == 0) Error("beat.lines", path + ".lines", $"beat '{beat.Id}' has no lines");

                    var scene = known ? beat.Scene : null;
                    CheckLines(beat.Lines, path + ".lines", scene, b, beat.Invitation, allowConditions: true, answers: false);
                    CheckCoverage(beat, path);
                    if (beat.Invitation != null) CheckInvitation(beat, b, path + ".invitation", scene);
                    for (var q = 0; q < beat.Questions.Count; q++) CheckQuestion(beat.Questions[q], b, $"{path}.questions[{q}]", scene);
                }
                var last = p.Beats[p.Beats.Count - 1];
                if (Hearth != null && last.Scene != Hearth.Id)
                    Error("ending.hearth", $"$.beats[{p.Beats.Count - 1}].scene", "the final beat must return the visitor to the hearth");
            }

            void CheckInvitation(Beat beat, int beatIndex, string path, string scene)
            {
                var inv = beat.Invitation;
                var needed = inv.Kind == InvitationKind.Receive ? ActionKind.Offer : ActionKind.Touch;
                if (!objects.TryGetValue(inv.Object ?? "", out var target))
                    Error("invitation.object", path + ".object", $"invitation '{inv.Id}' concerns unknown object '{inv.Object}'");
                else
                {
                    if (scene != null && target.Scene != scene)
                        Error("invitation.object", path + ".object", $"invitation '{inv.Id}' concerns '{inv.Object}', which is not staged in scene '{scene}'");
                    if (!target.Capabilities.Contains(needed))
                        Error("invitation.object", path + ".object", $"object '{inv.Object}' cannot be {(needed == ActionKind.Offer ? "offered" : "touched")}");
                }
                if (!(inv.WaitSeconds > 0) || double.IsInfinity(inv.WaitSeconds))
                    Error("invitation.wait", path + ".wait_seconds", $"invitation '{inv.Id}' needs a finite positive wait before its default path");
                if (string.IsNullOrWhiteSpace(inv.Alternative))
                    Error("invitation.alternative", path + ".alternative", $"invitation '{inv.Id}' needs a label for its nearby select alternative");
                if (inv.Kind == InvitationKind.Receive && !beat.Lines.Any(l => l.When == null && l.Actions.Any(a => a.Kind == ActionKind.Offer && a.Object == inv.Object)))
                    Error("invitation.offer", path, $"no unconditional line in beat '{beat.Id}' offers '{inv.Object}' before inviting the visitor to take it");
                if (inv.Accepted.Count == 0)
                    Error("invitation.accepted", path + ".accepted", $"invitation '{inv.Id}' must acknowledge an accepted action");
                if (inv.Default.Count == 0)
                    Error("invitation.default", path + ".default", $"invitation '{inv.Id}' needs an authored passive path");
                CheckLines(inv.Accepted, path + ".accepted", scene, beatIndex, null, allowConditions: false, answers: false);
                CheckLines(inv.Default, path + ".default", scene, beatIndex, null, allowConditions: false, answers: false);
            }

            void CheckQuestion(Question q, int beatIndex, string path, string scene)
            {
                if (string.IsNullOrWhiteSpace(q.Id)) Error("id.empty", path, "question id is empty");
                else if (!questionIds.Add(q.Id)) Error("id.duplicate", path, $"duplicate question id '{q.Id}'");
                if (string.IsNullOrWhiteSpace(q.Label)) Error("question.label", path + ".label", $"question '{q.Id}' needs a label for its control");
                if (q.Lines.Count == 0) Error("question.lines", path + ".lines", $"question '{q.Id}' has no answer");
                if (q.About != null)
                {
                    if (!objects.TryGetValue(q.About, out var about)) Error("question.about", path + ".about", $"question '{q.Id}' is about unknown object '{q.About}'");
                    else if (scene != null && about.Scene != scene) Error("question.about", path + ".about", $"question '{q.Id}' is about '{q.About}', which is not staged in scene '{scene}'");
                }
                CheckLines(q.Lines, path + ".lines", scene, beatIndex, null, allowConditions: true, answers: true);
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

            void CheckLines(IReadOnlyList<Line> lines, string path, string scene, int beatIndex, Invitation offeredHere, bool allowConditions, bool answers)
            {
                for (var i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
                    var lp = $"{path}[{i}]";
                    if (string.IsNullOrWhiteSpace(line.Id)) Error("id.empty", lp, "line id is empty");
                    else if (!lineIds.Add(line.Id)) Error("id.duplicate", lp, $"duplicate line id '{line.Id}'");
                    if (string.IsNullOrWhiteSpace(line.Text)) Error("line.text", lp + ".text", $"line '{line.Id}' has no text for its caption");
                    if (!(line.Seconds > 0) || double.IsInfinity(line.Seconds))
                        Error("line.seconds", lp + ".seconds", $"line '{line.Id}' needs a finite positive duration");
                    if (!(line.PauseAfter >= 0) || double.IsInfinity(line.PauseAfter))
                        Error("line.pause", lp + ".pause_after", $"line '{line.Id}' has an invalid pause");
                    if (line.Recall != null)
                        Error("recall.unresolved", lp + ".recall", $"line '{line.Id}' still depends on a kept visit; plans contain only resolved lines");

                    foreach (var claim in line.Claims)
                        if (!claims.ContainsKey(claim)) Error("claim.unknown", lp + ".claims", $"line '{line.Id}' cites unknown claim '{claim}'");

                    if (line.When != null)
                    {
                        if (!allowConditions)
                            Error("when.context", lp + ".when", $"line '{line.Id}' cannot depend on an outcome here");
                        else if (!invitationBeats.TryGetValue(line.When.Invitation, out var offeredAt))
                            Error("when.unknown", lp + ".when", $"line '{line.Id}' depends on unknown invitation '{line.When.Invitation}'");
                        else if (offeredAt >= beatIndex)
                            Error("when.order", lp + ".when", $"line '{line.Id}' depends on invitation '{line.When.Invitation}', which is not resolved before this beat");
                    }

                    for (var a = 0; a < line.Actions.Count; a++)
                        CheckAction(line, line.Actions[a], $"{lp}.actions[{a}]", scene, offeredHere, answers);
                }
            }

            void CheckAction(Line line, StoryAction action, string path, string scene, Invitation offeredHere, bool answer)
            {
                if (!objects.TryGetValue(action.Object ?? "", out var target))
                {
                    Error("action.object", path, $"line '{line.Id}' acts on unknown object '{action.Object}'");
                    return;
                }
                if (action.Kind == ActionKind.Touch)
                {
                    Error("action.kind", path, $"line '{line.Id}' cannot touch '{target.Id}'; touching is the visitor's, through a touch invitation");
                    return;
                }
                if (answer && action.Kind != ActionKind.Reveal && action.Kind != ActionKind.Illuminate)
                    Error("question.action", path, $"answer line '{line.Id}' may only reveal or illuminate; a question never changes what the visitor has");
                if (scene != null && target.Scene != scene)
                    Error("action.scene", path, $"line '{line.Id}' acts on '{target.Id}', which is not staged in scene '{scene}'");
                if (!target.Capabilities.Contains(action.Kind))
                    Error("action.capability", path, $"object '{target.Id}' does not support {action.Kind.ToString().ToLowerInvariant()}");
                if (action.Kind == ActionKind.Offer && (offeredHere == null || offeredHere.Kind != InvitationKind.Receive || offeredHere.Object != target.Id))
                    Error("action.offer", path, $"line '{line.Id}' offers '{target.Id}' without a receive invitation for it in the same beat");
                if (action.At != null)
                {
                    if (action.Kind != ActionKind.Place) Error("action.at", path, $"line '{line.Id}' gives a target to {action.Kind.ToString().ToLowerInvariant()}; only place takes one");
                    else if (!objects.TryGetValue(action.At, out var at)) Error("action.at", path, $"line '{line.Id}' places '{target.Id}' at unknown object '{action.At}'");
                    else if (at.Id == target.Id) Error("action.at", path, $"line '{line.Id}' places '{target.Id}' on itself");
                    else if (scene != null && at.Scene != scene) Error("action.at", path, $"line '{line.Id}' places '{target.Id}' at '{at.Id}', which is not staged in scene '{scene}'");
                }
            }

            // ---- staging: what the visitor can see and has with them, along every outcome path ----

            void Stage()
            {
                var ordered = invitationBeats.OrderBy(pair => pair.Value).Select(pair => pair.Key).ToList();
                var combinations = new List<Dictionary<string, Outcome>>();
                if (ordered.Count > MaxEnumeratedInvitations)
                {
                    Warning("staging.combinations", "$.beats", $"{ordered.Count} invitations; only the all-accepted and all-default paths were walked");
                    combinations.Add(ordered.ToDictionary(id => id, id => Outcome.Accepted));
                    combinations.Add(ordered.ToDictionary(id => id, id => Outcome.Default));
                }
                else
                {
                    for (var mask = 0; mask < 1 << ordered.Count; mask++)
                    {
                        var outcomes = new Dictionary<string, Outcome>();
                        for (var i = 0; i < ordered.Count; i++) outcomes[ordered[i]] = (mask & (1 << i)) != 0 ? Outcome.Default : Outcome.Accepted;
                        combinations.Add(outcomes);
                    }
                }
                foreach (var outcomes in combinations) Walk(outcomes);
            }

            void Walk(IReadOnlyDictionary<string, Outcome> outcomes)
            {
                var staging = new Staging();
                var scene = Hearth.Id;
                for (var b = 0; b < p.Beats.Count; b++)
                {
                    var beat = p.Beats[b];
                    var path = $"$.beats[{b}]";
                    if (beat.Scene != scene)
                    {
                        staging.Cross(PlayablePlan.Correspondences(p.Objects, scene, beat.Scene));
                        scene = beat.Scene;
                    }
                    for (var i = 0; i < beat.Lines.Count; i++)
                    {
                        var line = beat.Lines[i];
                        if (line.When == null || outcomes[line.When.Invitation] == line.When.Outcome)
                            Apply(staging, line, $"{path}.lines[{i}]");
                    }
                    var inv = beat.Invitation;
                    if (inv == null) continue;
                    if (inv.Kind == InvitationKind.Touch && !staging.Visible(inv.Object))
                        Error("staging.touch", path + ".invitation", $"invitation '{inv.Id}' asks the visitor to touch '{inv.Object}' before it is visible on some path; reveal it first");
                    var accepted = outcomes[inv.Id] == Outcome.Accepted;
                    var lines = accepted ? inv.Accepted : inv.Default;
                    for (var i = 0; i < lines.Count; i++)
                        Apply(staging, lines[i], $"{path}.invitation.{(accepted ? "accepted" : "default")}[{i}]");
                }
                if (p.Keepsake != null && !staging.WithVisitor(p.Keepsake))
                {
                    var path = "$.keepsake";
                    Error("staging.keepsake", path, $"keepsake '{p.Keepsake}' is not with the visitor at the hearth when the plan ends with outcomes {Describe(outcomes)}");
                }
            }

            void Apply(Staging staging, Line line, string path)
            {
                for (var a = 0; a < line.Actions.Count; a++)
                {
                    var action = line.Actions[a];
                    var ap = $"{path}.actions[{a}]";
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
                            if (action.At == null)
                            {
                                staging.Free(action.Object);
                                break;
                            }
                            if (!staging.Visible(action.Object))
                                Error("staging.place", ap, $"line '{line.Id}' places '{action.Object}', which the visitor does not have on some path");
                            if (!staging.Visible(action.At))
                                Error("staging.place", ap, $"line '{line.Id}' places '{action.Object}' at '{action.At}', which is not visible on some path");
                            staging.Attach(action.Object);
                            break;
                        case ActionKind.Withdraw:
                            if (!staging.Visible(action.Object))
                                Error("staging.withdraw", ap, $"line '{line.Id}' withdraws '{action.Object}', which is not there on some path");
                            staging.Remove(action.Object);
                            break;
                    }
                }
            }

            static string Describe(IReadOnlyDictionary<string, Outcome> outcomes) =>
                outcomes.Count == 0 ? "(none)" : string.Join(", ", outcomes.Select(o => o.Key + ":" + (o.Value == Outcome.Accepted ? "accepted" : "default")));

            void CheckInventory()
            {
                for (var i = 0; i < p.Scenes.Count; i++)
                    if (!string.IsNullOrWhiteSpace(p.Scenes[i].Id) && !inventory.HasScene(p.Scenes[i].Id))
                        Error("asset.scene", $"$.scenes[{i}]", $"the stage cannot show scene '{p.Scenes[i].Id}'");
                for (var i = 0; i < p.Objects.Count; i++)
                    if (!string.IsNullOrWhiteSpace(p.Objects[i].Id) && !inventory.HasObject(p.Objects[i].Id, p.Objects[i].Scene))
                        Error("asset.object", $"$.objects[{i}]", $"the stage has no object '{p.Objects[i].Id}' in scene '{p.Objects[i].Scene}'");
            }
        }
    }

    /// <summary>
    /// Which objects have been shown, and which of those travel with the visitor. Something shown in a scene stays
    /// there; an offered, received or set-beside object travels, and crosses to its counterpart when the visitor
    /// changes scene. Placing an object on another attaches it where it was placed. The application flow keeps the
    /// same record while a visit plays, so both agree on what crosses a transition and what comes home.
    /// </summary>
    public sealed class Staging
    {
        readonly HashSet<string> visible = new HashSet<string>();
        readonly HashSet<string> travelling = new HashSet<string>();

        public bool Visible(string id) => id != null && visible.Contains(id);
        public bool WithVisitor(string id) => id != null && travelling.Contains(id);
        public IEnumerable<string> VisibleObjects => visible;

        public void Clear()
        {
            visible.Clear();
            travelling.Clear();
        }

        /// <summary>Revealed or illuminated: visible in its scene; it travels only if it already did.</summary>
        public void Show(string id) => visible.Add(id);

        /// <summary>Offered to the visitor or set beside them: visible and travelling with them.</summary>
        public void Free(string id)
        {
            visible.Add(id);
            travelling.Add(id);
        }

        public void Attach(string id) => travelling.Remove(id);

        public void Remove(string id)
        {
            visible.Remove(id);
            travelling.Remove(id);
        }

        /// <summary>Applies a scene change and returns the correspondences that actually crossed.</summary>
        public List<(string From, string To)> Cross(IEnumerable<(string From, string To)> correspondences)
        {
            var crossed = new List<(string From, string To)>();
            foreach (var (from, to) in correspondences)
            {
                if (!travelling.Contains(from)) continue;
                visible.Remove(from);
                travelling.Remove(from);
                visible.Add(to);
                travelling.Add(to);
                crossed.Add((from, to));
            }
            return crossed;
        }

    }
}
