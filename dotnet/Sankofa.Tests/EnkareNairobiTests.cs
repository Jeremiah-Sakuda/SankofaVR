using System.Collections.Generic;
using System.Linq;
using Sankofa.Core.Story;
using NUnit.Framework;
using static Sankofa.Tests.Fixtures;

namespace Sankofa.Tests
{
    /// <summary>The bundled Enkare Nairobi package: valid on every path, sourced, and personal in what it stages.</summary>
    public class EnkareNairobiTests
    {
        [Test]
        public void ThePackageValidatesOnEveryRouteAndOutcome()
        {
            var report = PackageValidator.Validate(ReadEnkareNairobi());
            Assert.That(report.IsValid, Is.True, report.ToString());
            Assert.That(report.Warnings.Select(w => w.Code), Is.EqualTo(new[] { "content.unreviewed" }), "draft until family and community review");
        }

        [Test]
        public void EveryFactualLineCitesASourcedClaimAndPeopleAreReconstruction()
        {
            var p = ReadEnkareNairobi();
            foreach (var claim in p.Claims.Where(c => c.State == EvidenceState.Documented))
                Assert.That(claim.Sources, Is.Not.Empty, claim.Id);
            Assert.That(p.Claims.Single(c => c.Id == "R1").State, Is.EqualTo(EvidenceState.Reconstruction));
            var cited = new HashSet<string>(p.Beats.SelectMany(AllLines).Concat(p.Welcome).SelectMany(l => l.Claims));
            Assert.That(p.Claims.Select(c => c.Id).Where(id => !cited.Contains(id)), Is.Empty, "every claim in the package is used by a line");
        }

        [Test]
        public void EverythingTheVisitorCanNoticeHasANameAndItSurvivesAKeptPlan()
        {
            var p = ReadEnkareNairobi();
            var named = p.Objects.Where(o => o.Name != null).ToList();
            Assert.That(named.Select(o => o.Id), Does.Contain("figure.ewe").And.Contain("water.spill").And.Contain("figure.naserian"));
            Assert.That(p.Objects.Single(o => o.Id == "figure.ewe").Name, Is.EqualTo("the ewe"));
            // A kept plan embeds its objects; their names come back as written.
            var plan = Playable(p).Compose("era.1850s");
            var read = StoryReader.ReadPlan(Sankofa.Core.Json.JsonText.ParseObject(StoryWriter.PlanJson(plan)));
            Assert.That(read.Objects.Single(o => o.Id == "figure.ewe").Name, Is.EqualTo("the ewe"));
        }

        static IEnumerable<Line> AllLines(Beat b) =>
            b.Lines.Concat(b.Invitation?.Accepted ?? new Line[0]).Concat(b.Invitation?.Default ?? new Line[0]).Concat(b.Questions.SelectMany(q => q.Lines));

        [Test]
        public void TheTwoErasStageDifferentScenesObjectsAndKeepsakes()
        {
            var package = Playable(ReadEnkareNairobi());
            var early = package.Compose("era.1850s");
            var railway = package.Compose("era.1899");
            Assert.That(early.Beats.Select(b => b.Scene), Does.Contain("story.water.1850s").And.Not.Contain("story.water.1899"));
            Assert.That(railway.Beats.Select(b => b.Scene), Does.Contain("story.water.1899").And.Not.Contain("story.water.1850s"));
            Assert.That(early.Keepsake, Is.EqualTo("bead.kept"));
            Assert.That(railway.Keepsake, Is.EqualTo("seed.kept"));
            var earlyInvitations = early.Beats.Where(b => b.Invitation != null).Select(b => b.Invitation.Id).ToList();
            var railwayInvitations = railway.Beats.Where(b => b.Invitation != null).Select(b => b.Invitation.Id).ToList();
            Assert.That(earlyInvitations.Intersect(railwayInvitations), Is.EqualTo(new[] { "receive-ember" }), "only the fire's ember is shared");
        }

        [Test]
        public void TodayRecallsOnlyWhatTheKeptVisitHeld()
        {
            var package = Playable(ReadEnkareNairobi());
            var parent = new ParentVisit("v1", "era.1850s", new Dictionary<string, Outcome>
            {
                ["receive-ember"] = Outcome.Default, ["touch-water"] = Outcome.Accepted, ["receive-flour"] = Outcome.Accepted,
                ["offer-flour"] = Outcome.Default, ["receive-bead"] = Outcome.Default,
            });
            var lines = Playable(package.Compose("today.from-1850s", parent)).Plan.Beats.SelectMany(b => b.Lines).Select(l => l.Id).ToList();
            Assert.That(lines, Does.Contain("again.bead.left").And.Not.Contain("again.bead.held"));
            Assert.That(package.Route("era.1850s").Next, Is.EqualTo("today.from-1850s"));
            Assert.That(package.Route("era.1899").Next, Is.EqualTo("today.from-1899"));
        }
    }
}
