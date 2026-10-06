using System.Collections.Generic;
using System.Linq;
using Sankofa.Core.Json;
using Sankofa.Core.Story;
using NUnit.Framework;
using static Sankofa.Tests.Fixtures;

namespace Sankofa.Tests
{
    public class PlanValidatorTests
    {
        [Test]
        public void MinimalFixtureIsValid()
        {
            var report = PlanValidator.Validate(Minimal());
            Assert.That(report.Issues, Is.Empty, report.ToString());
        }

        [Test]
        public void InvalidPlansAreNeverPlayable()
        {
            var p = Minimal();
            p.Beats[2].Scene = "yard";
            Assert.That(PlanValidator.Accept(p, out var report), Is.Null);
            Assert.That(Codes(report), Does.Contain("ending.hearth"));
        }

        static ValidationReport Validate(System.Action<StoryPlan> change, IStageInventory inventory = null)
        {
            var p = Minimal();
            change(p);
            return PlanValidator.Validate(p, inventory);
        }

        [Test]
        public void RejectsActionsOnUnavailableObjects()
        {
            Assert.That(Codes(Validate(p => p.Beats[1].Lines = new List<Line> { Line("x", 2, null, Do(ActionKind.Reveal, "drum")) })),
                Does.Contain("action.object"));
            Assert.That(Codes(Validate(p => p.Beats[1].Lines = new List<Line> { Line("x", 2, null, Do(ActionKind.Place, "ember")) })),
                Does.Contain("action.scene"), "the ember is staged at the hearth, not in the yard");
            Assert.That(Codes(Validate(p => p.Beats[1].Lines = new List<Line> { Line("x", 2, null, Do(ActionKind.Offer, "bowl")) })),
                Does.Contain("action.capability"));
            Assert.That(Codes(Validate(p => p.Beats[1].Lines = new List<Line> { Line("x", 2, null, Do(ActionKind.Touch, "bowl")) })),
                Does.Contain("action.kind"), "touching is the visitor's, never the story's");
        }

        [Test]
        public void AnOfferNeedsAnInvitationAndAnInvitationNeedsAnUnconditionalOffer()
        {
            Assert.That(Codes(Validate(p => p.EarlyReturn = new List<Line> { Line("e", 2, null, Do(ActionKind.Offer, "ember")) })),
                Does.Contain("action.offer"));
            Assert.That(Codes(Validate(p => p.Beats[0].Lines = new List<Line> { Line("o1") })), Does.Contain("invitation.offer"));
        }

        [Test]
        public void EveryInvitationHasAPassivePathAndASelectAlternative()
        {
            Assert.That(Codes(Validate(p => p.Beats[0].Invitation.Default = new List<Line>())), Does.Contain("invitation.default"));
            Assert.That(Codes(Validate(p => p.Beats[0].Invitation.Alternative = " ")), Does.Contain("invitation.alternative"));
            Assert.That(Codes(Validate(p => p.Beats[0].Invitation.WaitSeconds = 0)), Does.Contain("invitation.wait"));
        }

        [Test]
        public void OutcomeLinesFollowTheirInvitation()
        {
            Assert.That(Codes(Validate(p => p.Beats[0].Lines = new List<Line>(p.Beats[0].Lines) { Line("early.line", 2, "take:accepted") })),
                Does.Contain("when.order"), "the invitation is unresolved within its own beat");
            Assert.That(Codes(Validate(p => p.EarlyReturn = new List<Line> { Line("e", 2, "take:default") })), Does.Contain("when.context"));
            Assert.That(Codes(Validate(p => p.Beats[2].Lines = new List<Line> { Line("h", 2, "nothing:default") })), Does.Contain("when.unknown"));
        }

        [Test]
        public void EveryBeatSaysSomethingWhateverTheEarlierOutcome()
        {
            var report = Validate(p => p.Beats[1].Lines = new List<Line> { Line("only.carried", 2, "take:accepted") });
            Assert.That(Codes(report), Does.Contain("beat.coverage"));
        }

        [Test]
        public void ChecksIdsScenesContinuityAndTiming()
        {
            Assert.That(Codes(Validate(p => p.Beats[2].Lines = new List<Line> { Line("o1") })), Does.Contain("id.duplicate"));
            Assert.That(Codes(Validate(p => p.Scenes = new List<SceneDef>(p.Scenes) { new SceneDef { Id = "second", Kind = SceneKind.Hearth } })),
                Does.Contain("hearth.count"));
            Assert.That(Codes(Validate(p => p.Objects[1].Continues = "bowl")), Does.Contain("object.continues"));
            Assert.That(Codes(Validate(p => p.Beats[2].Lines = new List<Line> { Line("h1", 0) })), Does.Contain("line.seconds"));
            Assert.That(Codes(Validate(p => p.Keepsake = "drum")), Does.Contain("keepsake.unknown"));
            Assert.That(Codes(Validate(p => p.Keepsake = "bowl")), Does.Contain("keepsake.scene"), "a keepsake waits at the hearth");
            Assert.That(Codes(Validate(p => p.Route = "")), Does.Contain("plan.field"));
        }

        [Test]
        public void DocumentedClaimsNeedSourcesAndLinesCiteOnlyKnownClaims()
        {
            var report = Validate(p =>
            {
                p.Claims = new List<Claim> { new Claim { Id = "c1", State = EvidenceState.Documented, Text = "A documented statement" } };
                p.Beats[2].Lines = new List<Line> { new Line { Id = "h1", Text = "Home", Seconds = 2, Claims = new[] { "c1", "c2" } } };
            });
            Assert.That(Codes(report), Does.Contain("claim.sources"));
            Assert.That(Codes(report), Does.Contain("claim.unknown"));

            var reconstruction = Validate(p => p.Claims = new List<Claim> { new Claim { Id = "r1", State = EvidenceState.Reconstruction, Text = "Imagined detail" } });
            Assert.That(reconstruction.IsValid, Is.True, "reconstruction is labelled, not sourced");
        }

        static Invitation TouchBowl() => new Invitation
        {
            Id = "feel",
            Kind = InvitationKind.Touch,
            Object = "bowl",
            Alternative = "Touch the bowl",
            WaitSeconds = 8,
            Accepted = new List<Line> { Line("felt") },
            Default = new List<Line> { Line("seen") },
        };

        [Test]
        public void TheVisitorIsInvitedToTouchOnlyWhatIsVisibleAndTouchable()
        {
            Assert.That(Validate(p => p.Beats[1].Invitation = TouchBowl()).IsValid, Is.True, "the bowl is revealed earlier in the beat");
            var hidden = Validate(p =>
            {
                p.Beats[1].Invitation = TouchBowl();
                p.Beats[1].Lines = new List<Line> { Line("s1") };
            });
            Assert.That(Codes(hidden), Does.Contain("staging.touch"));
            var untouchable = Validate(p =>
            {
                p.Beats[1].Invitation = TouchBowl();
                p.Beats[1].Invitation.Object = "lantern";
            });
            Assert.That(Codes(untouchable), Does.Contain("invitation.object"));
        }

        [Test]
        public void PlacingNeedsTheObjectInHandAndAVisibleTargetAndLeavesItThere()
        {
            void PlaceLantern(StoryPlan p)
            {
                p.Objects[1].Capabilities = new[] { ActionKind.Reveal, ActionKind.Illuminate, ActionKind.Place };
                p.Beats[1].Lines = new List<Line>(p.Beats[1].Lines) { Line("set", 2, null, Do(ActionKind.Place, "lantern", "bowl")) };
            }
            var placed = Validate(p =>
            {
                PlaceLantern(p);
                p.Keepsake = null;
            });
            Assert.That(placed.IsValid, Is.True, placed.ToString());
            Assert.That(Codes(Validate(PlaceLantern)), Does.Contain("staging.keepsake"),
                "a lantern set on the bowl stays in the yard, so it cannot come home as the ember");

            var absent = Validate(p =>
            {
                p.Objects = new List<StoryObject>(p.Objects) { new StoryObject { Id = "stone", Scene = "yard", Capabilities = new[] { ActionKind.Place } } };
                p.Beats[1].Lines = new List<Line>(p.Beats[1].Lines) { Line("set", 2, null, Do(ActionKind.Place, "stone", "bowl")) };
            });
            Assert.That(Codes(absent), Does.Contain("staging.place"), "the stone was never given to the visitor");

            Assert.That(Codes(Validate(p => p.Beats[1].Lines = new List<Line>(p.Beats[1].Lines) { Line("x", 2, null, Do(ActionKind.Reveal, "bowl", "lantern")) })),
                Does.Contain("action.at"), "only place takes a target");
            Assert.That(Codes(Validate(p => p.Beats[0].Invitation.Default = new List<Line> { Line("o.no", 2, null, Do(ActionKind.Place, "ember", "bowl")) })),
                Does.Contain("action.at"), "the bowl is in another scene");
        }

        [Test]
        public void AFigureMovesOnlyWhileStandingInTheSceneAndOnlyToSomethingVisible()
        {
            void AddFigure(StoryPlan p, params Line[] lines)
            {
                p.Objects = new List<StoryObject>(p.Objects)
                {
                    new StoryObject { Id = "potter", Scene = "yard", Capabilities = new[] { ActionKind.Reveal, ActionKind.Move, ActionKind.Withdraw } },
                };
                p.Beats[1].Lines = new List<Line>(p.Beats[1].Lines).Concat(lines).ToList();
            }
            var moved = Validate(p => AddFigure(p,
                Line("arrive", 2, null, Do(ActionKind.Reveal, "potter")),
                Line("kneel", 2, null, Do(ActionKind.Move, "potter", "bowl")),
                Line("rise", 2, null, Do(ActionKind.Move, "potter"))));
            Assert.That(moved.IsValid, Is.True, moved.ToString());

            Assert.That(Codes(Validate(p => AddFigure(p, Line("kneel", 2, null, Do(ActionKind.Move, "potter", "bowl"))))),
                Does.Contain("staging.move"), "the potter was never revealed");
            Assert.That(Codes(Validate(p =>
                {
                    AddFigure(p,
                        Line("arrive", 2, null, Do(ActionKind.Reveal, "potter")),
                        Line("kneel", 2, null, Do(ActionKind.Move, "potter", "wheel")));
                    p.Objects = new List<StoryObject>(p.Objects) { new StoryObject { Id = "wheel", Scene = "yard", Capabilities = new[] { ActionKind.Reveal } } };
                })),
                Does.Contain("staging.move"), "the wheel is never revealed");
            Assert.That(Codes(Validate(p => AddFigure(p,
                    Line("arrive", 2, null, Do(ActionKind.Reveal, "potter")),
                    Line("kneel", 2, null, Do(ActionKind.Move, "potter", "potter"))))),
                Does.Contain("action.at"), "a figure cannot move to itself");
            Assert.That(Codes(Validate(p => p.Beats[1].Lines = new List<Line>(p.Beats[1].Lines) { Line("x", 2, null, Do(ActionKind.Move, "bowl")) })),
                Does.Contain("action.capability"), "only objects that can move are moved");
            Assert.That(Codes(Validate(p => AddFigure(p,
                    Line("arrive", 2, null, Do(ActionKind.Reveal, "potter")),
                    Line("kneel", 2, null, Do(ActionKind.Move, "potter", "ember"))))),
                Does.Contain("action.at"), "the ember is staged at the hearth, not in the yard");
        }

        [Test]
        public void AMoveSurvivesWritingAndReading()
        {
            var plan = Minimal();
            plan.Objects = new List<StoryObject>(plan.Objects) { new StoryObject { Id = "potter", Scene = "yard", Capabilities = new[] { ActionKind.Reveal, ActionKind.Move } } };
            plan.Beats[1].Lines = new List<Line>(plan.Beats[1].Lines) { Line("arrive", 2, null, Do(ActionKind.Reveal, "potter")), Line("kneel", 2, null, Do(ActionKind.Move, "potter", "bowl")) };
            var json = StoryWriter.PlanJson(plan);
            Assert.That(json, Does.Contain("\"move\""));
            var again = StoryReader.ReadPlan(json);
            var kneel = again.Beats[1].Lines.Single(l => l.Id == "kneel").Actions.Single();
            Assert.That((kneel.Kind, kneel.Object, kneel.At), Is.EqualTo((ActionKind.Move, "potter", "bowl")));
        }

        [Test]
        public void TheKeepsakeMustComeHomeOnEveryPath()
        {
            var withdrawn = Validate(p => p.Beats[2].Lines = new List<Line> { Line("h1", 2, null, Do(ActionKind.Withdraw, "ember")) });
            Assert.That(Codes(withdrawn), Does.Contain("staging.keepsake"));
            Assert.That(withdrawn.Errors.First(e => e.Code == "staging.keepsake").Message, Does.Contain("take:"), "names the path that fails");

            var withdrawnTwice = Validate(p =>
            {
                p.Keepsake = null;
                p.Beats[2].Lines = new List<Line> { Line("h1", 2, null, Do(ActionKind.Withdraw, "ember")), Line("h2", 2, null, Do(ActionKind.Withdraw, "ember")) };
            });
            Assert.That(Codes(withdrawnTwice), Does.Contain("staging.withdraw"));
        }

        [Test]
        public void QuestionsAreAnsweredInTheirSceneAndNeverChangeWhatTheVisitorHas()
        {
            Question Q(string id, string about, params Line[] lines) => new Question { Id = id, Label = "Label " + id, About = about, Lines = lines };
            Assert.That(Validate(p => p.Beats[1].Questions = new[] { Q("q1", "bowl", Line("a1")), Q("q2", null, Line("a2", 2, "take:default")) }).IsValid, Is.True);
            Assert.That(Codes(Validate(p => p.Beats[1].Questions = new[] { Q("q1", "ember", Line("a1")) })), Does.Contain("question.about"));
            Assert.That(Codes(Validate(p => p.Beats[1].Questions = new[] { Q("q1", null) })), Does.Contain("question.lines"));
            Assert.That(Codes(Validate(p => p.Beats[1].Questions = new[] { new Question { Id = "q1", Label = " ", Lines = new[] { Line("a1") } } })),
                Does.Contain("question.label"));
            Assert.That(Codes(Validate(p => p.Beats[1].Questions = new[] { Q("q1", null, Line("a1", 2, null, Do(ActionKind.Place, "lantern"))) })),
                Does.Contain("question.action"));
            Assert.That(Codes(Validate(p => p.Beats[1].Questions = new[] { Q("q1", null, Line("a1")), Q("q1", null, Line("a2")) })),
                Does.Contain("id.duplicate"));
        }

        [Test]
        public void PlansNeedEverythingTheyUseOnTheStage()
        {
            var complete = new Inventory(new[] { "hearth", "yard" }, new[] { "ember", "lantern", "bowl" });
            Assert.That(Validate(p => { }, complete).IsValid, Is.True);
            var noYard = Validate(p => { }, new Inventory(new[] { "hearth" }, new[] { "ember", "lantern", "bowl" }));
            Assert.That(Codes(noYard), Does.Contain("asset.scene"));
            var noBowl = Validate(p => { }, new Inventory(new[] { "hearth", "yard" }, new[] { "ember", "lantern" }));
            Assert.That(Codes(noBowl), Does.Contain("asset.object"));
        }

        [Test]
        public void APlanNeverDependsOnAKeptVisitItCannotSee()
        {
            var report = Validate(p => p.Beats[2].Lines = new List<Line>
            {
                new Line { Id = "h1", Text = "Home", Seconds = 2, Recall = new Condition("take", Outcome.Accepted) },
            });
            Assert.That(Codes(report), Does.Contain("recall.unresolved"));
        }
    }

    public class StoryRunTests
    {
        static List<string> Drain(StoryRun run, Outcome outcome)
        {
            var steps = new List<string>();
            for (var i = 0; i < 80; i++)
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
        public void EveryPlaceholderRouteEndsAtTheHearthOnBothPaths()
        {
            var package = Playable(ReadPlaceholder());
            foreach (var route in package.FirstRoutes)
            foreach (var outcome in new[] { Outcome.Accepted, Outcome.Default })
            {
                var steps = Drain(new StoryRun(Playable(package.Compose(route.Id))), outcome);
                Assert.That(steps.Last(), Is.EqualTo("End"));
                Assert.That(steps.Count(s => s.StartsWith("ChangeScene")), Is.EqualTo(2), route.Id + " enters one story scene and returns");
            }
        }
    }

    public class PlanFormatTests
    {
        [Test]
        public void APlanIsWrittenAndReadBackExactly()
        {
            var package = Playable(ReadPlaceholder());
            foreach (var route in package.FirstRoutes)
            {
                var plan = package.Compose(route.Id);
                var json = StoryWriter.PlanJson(plan);
                var again = StoryReader.ReadPlan(json);
                Assert.That(StoryWriter.PlanJson(again), Is.EqualTo(json), route.Id);
                Assert.That(PlanValidator.Validate(again).IsValid, Is.True);
                Assert.That(StoryReader.ReadPlan(StoryWriter.Plan(plan)).Beats.Count, Is.EqualTo(plan.Beats.Count), "a written plan reads without reparsing");
            }
        }

        [Test]
        public void APackageIsNotAPlan()
        {
            var json = System.IO.File.ReadAllText(RepositoryFile(PlaceholderPackage));
            Assert.That(Assert.Throws<StoryFormatException>(() => StoryReader.ReadPlan(json)).Path, Is.EqualTo("$.kind"));
            var plan = StoryWriter.PlanJson(Minimal());
            Assert.That(Assert.Throws<StoryFormatException>(() => StoryReader.ReadPackage(plan)).Path, Is.EqualTo("$.kind"));
        }

        [Test]
        public void PlansCannotCarryUnresolvedRecall()
        {
            var json = JsonText.ParseObject(StoryWriter.PlanJson(Minimal()));
            var line = (JsonObject)((JsonObject)json.GetArray("beats")[2]).GetArray("lines")[0];
            line.Add("recall", "take:accepted");
            Assert.That(Assert.Throws<StoryFormatException>(() => StoryReader.ReadPlan(JsonText.Serialize(json))).Path, Is.EqualTo("$.beats[2].lines[0].recall"));
        }
    }
}
