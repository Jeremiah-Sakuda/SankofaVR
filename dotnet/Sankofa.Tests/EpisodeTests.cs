using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sankofa.Core.Json;
using Sankofa.Core.Story;
using NUnit.Framework;
using static Sankofa.Tests.Fixtures;

namespace Sankofa.Tests
{
    public class EpisodeReaderTests
    {
        [Test]
        public void ReadsTheBundledPlaceholderEpisode()
        {
            var e = ReadPlaceholder();
            Assert.That(e.Id, Is.EqualTo("placeholder.carried-ember"));
            Assert.That(e.Status, Is.EqualTo(ContentStatus.Placeholder));
            Assert.That(e.Welcome[0].Text, Is.EqualTo("Come. Sit with me."), "the user-selected opening line");
            Assert.That(e.Beats.Select(b => b.Id), Is.EqualTo(new[] { "offer", "enter", "reveal", "return" }));
            var invitation = e.Beats[0].Invitation;
            Assert.That(invitation.Alternative, Is.EqualTo("Take the ember"));
            Assert.That(e.Beats[2].Lines[0].When.ToString(), Is.EqualTo("receive-ember:accepted"));
            Assert.That(e.Objects.Single(o => o.Id == "lantern").Continues, Is.EqualTo("ember"));
        }

        static JsonObject PlaceholderJson() => JsonText.ParseObject(File.ReadAllText(RepositoryFile(PlaceholderEpisode)));

        static EpisodeFormatException ReadFails(JsonObject json) =>
            Assert.Throws<EpisodeFormatException>(() => EpisodeReader.Read(JsonText.Serialize(json)));

        [Test]
        public void RejectsUnknownFieldsSoTyposCannotDropContent()
        {
            var json = PlaceholderJson();
            ((JsonObject)json.GetArray("beats")[0]).Add("invitaton", new JsonObject());
            Assert.That(ReadFails(json).Path, Is.EqualTo("$.beats[0].invitaton"));
        }

        [Test]
        public void NamesTheMissingFieldAndUnknownValues()
        {
            var json = PlaceholderJson();
            var line = (JsonObject)((JsonObject)json.GetArray("beats")[1]).GetArray("lines")[0];
            line["fire"] = "roaring";
            Assert.That(ReadFails(json).Path, Is.EqualTo("$.beats[1].lines[0].fire"));

            json = PlaceholderJson();
            var welcome = new JsonObject { { "id", "w" }, { "text", "No duration" } };
            json["welcome"] = new List<object> { welcome };
            Assert.That(ReadFails(json).Path, Is.EqualTo("$.welcome[0].seconds"));
        }

        [Test]
        public void RejectsMalformedConditionsAndOtherSchemas()
        {
            var json = PlaceholderJson();
            var line = (JsonObject)((JsonObject)json.GetArray("beats")[2]).GetArray("lines")[0];
            line["when"] = "receive-ember";
            Assert.That(ReadFails(json).Path, Is.EqualTo("$.beats[2].lines[0].when"));

            json = PlaceholderJson();
            json["schema"] = 2.0;
            Assert.That(ReadFails(json).Message, Does.Contain("unsupported schema 2"));

            Assert.That(Assert.Throws<EpisodeFormatException>(() => EpisodeReader.Read("{ not json")).Path, Is.EqualTo("$"));
        }
    }

    public class EpisodeValidatorTests
    {
        [Test]
        public void BundledPlaceholderIsPlayableButNeverPassesAsReviewedContent()
        {
            var report = EpisodeValidator.Validate(ReadPlaceholder());
            Assert.That(report.IsValid, Is.True, report.ToString());
            Assert.That(report.Warnings.Select(w => w.Code), Is.EqualTo(new[] { "content.unreviewed" }));
        }

        [Test]
        public void MinimalFixtureIsValid()
        {
            var report = EpisodeValidator.Validate(Minimal());
            Assert.That(report.Issues, Is.Empty, report.ToString());
        }

        [Test]
        public void InvalidEpisodesAreNeverPlayable()
        {
            var e = Minimal();
            e.Beats[2].Scene = "yard";
            Assert.That(EpisodeValidator.Accept(e, out var report), Is.Null);
            Assert.That(Codes(report), Does.Contain("ending.hearth"));
        }

        static ValidationReport Validate(System.Action<Episode> change)
        {
            var e = Minimal();
            change(e);
            return EpisodeValidator.Validate(e);
        }

        [Test]
        public void RejectsActionsOnUnavailableObjects()
        {
            Assert.That(Codes(Validate(e => e.Beats[1].Lines = new List<Line> { Line("x", 2, null, Do(ActionKind.Reveal, "drum")) })),
                Does.Contain("action.object"));
            Assert.That(Codes(Validate(e => e.Beats[1].Lines = new List<Line> { Line("x", 2, null, Do(ActionKind.Place, "ember")) })),
                Does.Contain("action.scene"), "the ember is staged at the hearth, not in the yard");
            Assert.That(Codes(Validate(e => e.Beats[1].Lines = new List<Line> { Line("x", 2, null, Do(ActionKind.Offer, "bowl")) })),
                Does.Contain("action.capability"));
        }

        [Test]
        public void AnOfferNeedsAnInvitationAndAnInvitationNeedsAnUnconditionalOffer()
        {
            Assert.That(Codes(Validate(e => e.Welcome = new List<Line> { Line("w", 2, null, Do(ActionKind.Offer, "ember")) })),
                Does.Contain("action.offer"));
            Assert.That(Codes(Validate(e => e.Beats[0].Lines = new List<Line> { Line("o1") })), Does.Contain("invitation.offer"));
        }

        [Test]
        public void EveryInvitationHasAPassivePathAndASelectAlternative()
        {
            Assert.That(Codes(Validate(e => e.Beats[0].Invitation.Default = new List<Line>())), Does.Contain("invitation.default"));
            Assert.That(Codes(Validate(e => e.Beats[0].Invitation.Alternative = " ")), Does.Contain("invitation.alternative"));
            Assert.That(Codes(Validate(e => e.Beats[0].Invitation.WaitSeconds = 0)), Does.Contain("invitation.wait"));
        }

        [Test]
        public void OutcomeLinesFollowTheirInvitation()
        {
            Assert.That(Codes(Validate(e => e.Beats[0].Lines = new List<Line>(e.Beats[0].Lines) { Line("early", 2, "take:accepted") })),
                Does.Contain("when.order"), "the invitation is unresolved within its own beat");
            Assert.That(Codes(Validate(e => e.Welcome = new List<Line> { Line("w", 2, "take:default") })), Does.Contain("when.context"));
            Assert.That(Codes(Validate(e => e.Beats[2].Lines = new List<Line> { Line("h", 2, "nothing:default") })), Does.Contain("when.unknown"));
        }

        [Test]
        public void EveryBeatSaysSomethingWhateverTheEarlierOutcome()
        {
            var report = Validate(e => e.Beats[1].Lines = new List<Line> { Line("only.carried", 2, "take:accepted") });
            Assert.That(Codes(report), Does.Contain("beat.coverage"));
        }

        [Test]
        public void ChecksIdsScenesContinuityAndTiming()
        {
            Assert.That(Codes(Validate(e => e.Beats[2].Lines = new List<Line> { Line("o1") })), Does.Contain("id.duplicate"));
            Assert.That(Codes(Validate(e => e.Scenes = new List<SceneDef>(e.Scenes) { new SceneDef { Id = "second", Kind = SceneKind.Hearth } })),
                Does.Contain("hearth.count"));
            Assert.That(Codes(Validate(e => e.Objects[1].Continues = "bowl")), Does.Contain("object.continues"));
            Assert.That(Codes(Validate(e => e.Beats[2].Lines = new List<Line> { Line("h1", 0) })), Does.Contain("line.seconds"));
            Assert.That(Codes(Validate(e => e.Keepsake = "drum")), Does.Contain("keepsake.unknown"));
        }

        [Test]
        public void DocumentedClaimsNeedSourcesAndLinesCiteOnlyKnownClaims()
        {
            var report = Validate(e =>
            {
                e.Claims = new List<Claim> { new Claim { Id = "c1", State = EvidenceState.Documented, Text = "A documented statement" } };
                e.Beats[2].Lines = new List<Line> { new Line { Id = "h1", Text = "Home", Seconds = 2, Claims = new[] { "c1", "c2" } } };
            });
            Assert.That(Codes(report), Does.Contain("claim.sources"));
            Assert.That(Codes(report), Does.Contain("claim.unknown"));

            var reconstruction = Validate(e => e.Claims = new List<Claim> { new Claim { Id = "r1", State = EvidenceState.Reconstruction, Text = "Imagined detail" } });
            Assert.That(reconstruction.IsValid, Is.True, "reconstruction is labelled, not sourced");
        }
    }

    public class StoryRunTests
    {
        static List<string> Drain(StoryRun run, Outcome outcome)
        {
            var steps = new List<string>();
            for (var i = 0; i < 50; i++)
            {
                var step = run.Next();
                steps.Add(step.ToString());
                if (step is StoryStep.Await wait) Assert.That(run.Resolve(wait.Invitation.Id, outcome), Is.True);
                if (step is StoryStep.End) return steps;
            }
            Assert.Fail("Story did not end");
            return steps;
        }

        [Test]
        public void AcceptedAndDefaultPathsDifferOnlyWhereAuthored()
        {
            var accepted = Drain(new StoryRun(Playable(Minimal())), Outcome.Accepted);
            Assert.That(accepted, Is.EqualTo(new[]
            {
                "Speak o1", "Speak o2", "Await take", "Speak o.yes", "ChangeScene hearth -> yard", "Speak s1", "Speak s.carried",
                "ChangeScene yard -> hearth", "Speak h1", "End",
            }));
            var passive = Drain(new StoryRun(Playable(Minimal())), Outcome.Default);
            Assert.That(passive, Does.Contain("Speak o.no").And.Contain("Speak s.placed"));
            Assert.That(passive, Does.Not.Contain("Speak o.yes").And.Not.Contain("Speak s.carried"));
        }

        [Test]
        public void AnInvitationResolvesOnceAndOnlyWhileItsBeatIsCurrent()
        {
            var run = new StoryRun(Playable(Minimal()));
            Assert.That(run.Resolve("take", Outcome.Accepted), Is.False, "not started");
            run.Next();
            Assert.That(run.PendingInvitation?.Id, Is.EqualTo("take"));
            Assert.That(run.Resolve("other", Outcome.Accepted), Is.False);
            Assert.That(run.Resolve("take", Outcome.Accepted), Is.True, "accepted while the offer is spoken");
            Assert.That(run.Resolve("take", Outcome.Default), Is.False, "acknowledged once");
            Assert.That(run.Next().ToString(), Is.EqualTo("Speak o2"));
            Assert.That(run.Next().ToString(), Is.EqualTo("Speak o.yes"), "no wait once accepted");
        }

        [Test]
        public void PlaceholderEpisodeHasCompleteEndingsOnBothPaths()
        {
            foreach (var outcome in new[] { Outcome.Accepted, Outcome.Default })
            {
                var steps = Drain(new StoryRun(Playable(ReadPlaceholder())), outcome);
                Assert.That(steps[steps.Count - 2], Is.EqualTo("Speak return.home"));
                Assert.That(steps, Does.Contain(outcome == Outcome.Accepted ? "Speak reveal.carried" : "Speak reveal.placed"));
            }
        }
    }
}
