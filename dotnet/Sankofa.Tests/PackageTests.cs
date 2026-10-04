using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sankofa.Core.Json;
using Sankofa.Core.Story;
using NUnit.Framework;
using static Sankofa.Tests.Fixtures;

namespace Sankofa.Tests
{
    public class PackageReaderTests
    {
        [Test]
        public void ReadsTheBundledPlaceholderPackage()
        {
            var p = ReadPlaceholder();
            Assert.That(p.Id, Is.EqualTo("placeholder.hearth-stories"));
            Assert.That(p.Status, Is.EqualTo(ContentStatus.Placeholder));
            Assert.That(p.Welcome[0].Text, Is.EqualTo("Come. Sit with me."), "the user-selected opening line");
            Assert.That(p.Routes.Where(r => !r.IsContinuation).Select(r => r.Label), Is.EqualTo(new[] { "Hands that make", "Roads that carry" }));
            Assert.That(p.Routes.Single(r => r.Id == "makers").Next, Is.EqualTo("makers.again"));
            Assert.That(p.Routes.Single(r => r.Id == "makers.again").Follows, Is.EqualTo("makers"));
            var recall = p.Beats.Single(b => b.Id == "again.makers").Lines[0].Recall;
            Assert.That(recall.ToString(), Is.EqualTo("receive-shard:accepted"));
            Assert.That(p.Objects.Single(o => o.Id == "lantern").Continues, Is.EqualTo("ember"));
        }

        static JsonObject PlaceholderJson() => JsonText.ParseObject(File.ReadAllText(RepositoryFile(PlaceholderPackage)));

        static StoryFormatException ReadFails(JsonObject json) =>
            Assert.Throws<StoryFormatException>(() => StoryReader.ReadPackage(JsonText.Serialize(json)));

        [Test]
        public void RejectsUnknownFieldsSoTyposCannotDropContent()
        {
            var json = PlaceholderJson();
            ((JsonObject)json.GetArray("beats")[2]).Add("invitaton", new JsonObject());
            Assert.That(ReadFails(json).Path, Is.EqualTo("$.beats[2].invitaton"));
        }

        [Test]
        public void NamesTheMissingFieldAndUnknownValues()
        {
            var json = PlaceholderJson();
            var line = (JsonObject)((JsonObject)json.GetArray("beats")[3]).GetArray("lines")[0];
            line["fire"] = "roaring";
            Assert.That(ReadFails(json).Path, Is.EqualTo("$.beats[3].lines[0].fire"));

            json = PlaceholderJson();
            json["welcome"] = new List<object> { new JsonObject { { "id", "w" }, { "text", "No duration" } } };
            Assert.That(ReadFails(json).Path, Is.EqualTo("$.welcome[0].seconds"));
        }

        [Test]
        public void RejectsMalformedConditionsOtherSchemasAndRecallOutsideStories()
        {
            var json = PlaceholderJson();
            var line = (JsonObject)((JsonObject)json.GetArray("beats")[4]).GetArray("lines")[1];
            line["when"] = "receive-ember";
            Assert.That(ReadFails(json).Path, Is.EqualTo("$.beats[4].lines[1].when"));

            json = PlaceholderJson();
            json["schema"] = 1.0;
            Assert.That(ReadFails(json).Message, Does.Contain("unsupported schema 1"));

            json = PlaceholderJson();
            ((JsonObject)json.GetArray("welcome")[0]).Add("recall", "receive-ember:accepted");
            Assert.That(ReadFails(json).Path, Is.EqualTo("$.welcome[0].recall"), "only story beats may recall a kept visit");

            Assert.That(Assert.Throws<StoryFormatException>(() => StoryReader.ReadPackage("{ not json")).Path, Is.EqualTo("$"));
        }
    }

    public class PackageValidatorTests
    {
        [Test]
        public void BundledPlaceholderIsPlayableButNeverPassesAsReviewedContent()
        {
            var report = PackageValidator.Validate(ReadPlaceholder());
            Assert.That(report.IsValid, Is.True, report.ToString());
            Assert.That(report.Warnings.Select(w => w.Code), Is.EqualTo(new[] { "content.unreviewed" }), "every beat is used and nothing else is doubtful");
        }

        static ValidationReport Validate(System.Action<StoryPackage> change)
        {
            var p = ReadPlaceholder();
            change(p);
            return PackageValidator.Validate(p);
        }

        static Route RouteOf(StoryPackage p, string id) => p.Routes.Single(r => r.Id == id);

        [Test]
        public void RoutesMustResolveAndContinuationsMustFollowTheirRoute()
        {
            Assert.That(Codes(Validate(p => RouteOf(p, "makers").Beats = new[] { "gather.makers", "nowhere" })), Does.Contain("route.beat"));
            Assert.That(Codes(Validate(p => RouteOf(p, "makers").Next = "travellers.again")), Does.Contain("route.next"));
            Assert.That(Codes(Validate(p => RouteOf(p, "makers.again").Follows = "nobody")), Does.Contain("route.follows"));
            Assert.That(Codes(Validate(p => p.Routes = p.Routes.Where(r => r.IsContinuation).ToList())), Does.Contain("routes.first"));
            Assert.That(Codes(Validate(p => RouteOf(p, "travellers").Label = "")), Does.Contain("route.label"));
        }

        [Test]
        public void OnlyContinuationsRecallAndOnlyWhatTheirParentCouldHaveDone()
        {
            Assert.That(Codes(Validate(p => RouteOf(p, "travellers").Beats = new[] { "again.makers" }.Concat(RouteOf(p, "travellers").Beats).ToList())),
                Does.Contain("recall.context"));
            Assert.That(Codes(Validate(p => RouteOf(p, "travellers.again").Beats = new[] { "again.makers" }.Concat(RouteOf(p, "travellers.again").Beats.Skip(1)).ToList())),
                Does.Contain("recall.unknown"), "the road's visitor never had a shard to recall");
        }

        [Test]
        public void HearthLinesAreSpokenBeforeAnyStoryAndCannotActOrDepend()
        {
            Assert.That(Codes(Validate(p => p.Welcome = new List<Line>())), Does.Contain("welcome.empty"));
            Assert.That(Codes(Validate(p => p.Choose = new List<Line> { Line("choose.x", 2, null, Do(ActionKind.Offer, "ember")) })), Does.Contain("action.context"));
            Assert.That(Codes(Validate(p => p.WelcomeBack = new List<Line> { Line("back.x", 2, "receive-ember:accepted") })), Does.Contain("when.context"));
            Assert.That(Codes(Validate(p => p.Choose = new List<Line> { Line("offer.light") })), Does.Contain("id.duplicate"), "the journal names lines by id");
        }

        [Test]
        public void AProblemInsideARouteIsReportedForThatRoute()
        {
            var report = Validate(p =>
            {
                var bowl = p.Beats.Single(b => b.Id == "yard.bowl");
                bowl.Lines = bowl.Lines.Where(l => l.Actions.Count == 0).ToList();
            });
            var touch = report.Errors.First(e => e.Code == "staging.touch");
            Assert.That(touch.Path, Does.StartWith("$.routes[0](plan)"));
            Assert.That(touch.Message, Does.StartWith("route 'makers'"));
        }

        [Test]
        public void UnusedBeatsAreReported()
        {
            var report = Validate(p => p.Beats = p.Beats.Concat(new[] { new Beat { Id = "spare", Scene = "hearth", Lines = new[] { Line("spare.line") } } }).ToList());
            Assert.That(report.IsValid, Is.True);
            Assert.That(report.Warnings.Select(w => w.Code), Does.Contain("beat.unused"));
        }
    }

    /// <summary>The PRD's personalisation bar: contrasting interests change what happens, not just the wording.</summary>
    public class CompositionTests
    {
        static PlayablePackage Package() => Playable(ReadPlaceholder());

        static IEnumerable<string> ActedOn(StoryPlan plan) =>
            plan.Beats.SelectMany(b => b.Lines.Concat(b.Invitation?.Accepted ?? new Line[0]).Concat(b.Invitation?.Default ?? new Line[0]))
                .SelectMany(l => l.Actions).Select(a => a.Object).Distinct();

        [Test]
        public void TwoInterestsChangeScenesObjectsInvitationsAndWhatComesHome()
        {
            var package = Package();
            var makers = package.Compose("makers");
            var travellers = package.Compose("travellers");

            var makersScenes = makers.Beats.Select(b => b.Scene).Distinct().ToList();
            var travellersScenes = travellers.Beats.Select(b => b.Scene).Distinct().ToList();
            Assert.That(makersScenes, Does.Contain("story.yard").And.Not.Contain("story.road"));
            Assert.That(travellersScenes, Does.Contain("story.road").And.Not.Contain("story.yard"));

            Assert.That(ActedOn(makers), Does.Contain("bowl").And.Contain("shard"));
            Assert.That(ActedOn(travellers), Does.Contain("waymarker").And.Contain("waystone"));
            Assert.That(ActedOn(makers).Intersect(ActedOn(travellers)), Is.EquivalentTo(new[] { "ember" }), "only the fire's own ember is shared");

            var makersInvitations = makers.Beats.Where(b => b.Invitation != null).Select(b => b.Invitation.Object).ToList();
            var travellersInvitations = travellers.Beats.Where(b => b.Invitation != null).Select(b => b.Invitation.Object).ToList();
            Assert.That(makersInvitations.Skip(1), Is.Not.EqualTo(travellersInvitations.Skip(1)), "the hand moments in the story differ");

            Assert.That(makers.Keepsake, Is.Not.EqualTo(travellers.Keepsake));
            Assert.That(makers.Next, Is.Not.EqualTo(travellers.Next), "and so does the thread they leave");
            Assert.That(makers.Beats.SelectMany(b => b.Questions).Select(q => q.Id).Intersect(travellers.Beats.SelectMany(b => b.Questions).Select(q => q.Id)), Is.Empty);
        }

        [Test]
        public void AContinuationRecallsOnlyWhatTheKeptVisitContained()
        {
            var package = Package();
            var parent = new ParentVisit("v1", "makers", new Dictionary<string, Outcome>
            {
                ["receive-ember"] = Outcome.Default, ["touch-bowl"] = Outcome.Default, ["receive-shard"] = Outcome.Accepted,
            });
            var plan = package.Compose("makers.again", parent);
            var lines = plan.Beats.SelectMany(b => b.Lines).Select(l => l.Id).ToList();
            Assert.That(lines, Does.Contain("again.makers.carried"));
            Assert.That(lines, Does.Not.Contain("again.makers.held").And.Not.Contain("again.makers.felt"), "no memory of a touch that never happened");
            Assert.That(plan.Beats.SelectMany(b => b.Lines).All(l => l.Recall == null), Is.True, "the plan holds only resolved lines");
            Assert.That(plan.Parent, Is.EqualTo("v1"));
            Assert.That(plan.Next, Is.Null, "this thread ends here");
            Assert.That(Playable(plan).Plan.Keepsake, Is.EqualTo("ember"));
        }

        [Test]
        public void APlanCarriesOnlyTheScenesAndObjectsItsRouteCanStage()
        {
            var plan = Package().Compose("makers");
            Assert.That(plan.Scenes.Select(s => s.Id), Is.EqualTo(new[] { "hearth", "story.yard" }));
            Assert.That(plan.Objects.Select(o => o.Id), Is.EquivalentTo(new[] { "ember", "lantern", "bowl", "shard", "shard.kept" }));
        }

        [Test]
        public void ContinuationsNeedTheVisitTheyFollow()
        {
            var package = Package();
            Assert.Throws<System.ArgumentException>(() => package.Compose("makers.again"));
            Assert.Throws<System.ArgumentException>(() => package.Compose("makers.again", new ParentVisit("v1", "travellers", null)));
            Assert.Throws<System.ArgumentException>(() => package.Compose("makers", new ParentVisit("v1", "makers", null)));
        }
    }
}
