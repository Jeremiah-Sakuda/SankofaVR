using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NUnit.Framework;
using Sankofa.Core.Story;
using Sankofa.Flow;
using static Sankofa.Tests.Fixtures;

namespace Sankofa.Tests
{
    public class IllustratedStoryTests
    {
        static PlayablePackage Package() => Playable(StoryReader.ReadPackage(File.ReadAllText(
            RepositoryFile("unity/Assets/Sankofa/Content/illustrated-theatre-mvp.json"))));

        static readonly string[] Invitations =
            { "receive-ember", "touch-water", "receive-flour", "offer-flour", "receive-bead" };

        static (VisitFlow, FakePresentation) Open(MemoryStore store = null)
        {
            var flow = new VisitFlow(Package(), store: store ?? new MemoryStore());
            var view = new FakePresentation(flow);
            flow.Start(skipWelcome: true);
            Assert.That(flow.Choose("era.1850s"), Is.True);
            return (flow, view);
        }

        static void Finish(VisitFlow flow, FakePresentation view, params string[] accepted)
        {
            var remaining = new HashSet<string>(accepted);
            view.RunUntil(() =>
            {
                if (flow.OpenInvitation != null && remaining.Remove(flow.OpenInvitation.Id))
                    Assert.That(flow.Accept(flow.OpenInvitation.Id), Is.True);
                return flow.Phase == VisitPhase.Closing && flow.CurrentLine == null;
            });
            Assert.That(flow.Journal.Completed, Is.True);
        }

        // Test mixed paths as well as the two extremes: helping with the trade does not imply taking the gift.
        static IEnumerable<int> Paths => Enumerable.Range(0, 32);

        [TestCaseSource(nameof(Paths))]
        public void EveryPathKeepsTheWomenInChargeAndRemembersOnlyActualParticipation(int mask)
        {
            var accepted = Invitations.Where((id, bit) => (mask & (1 << bit)) != 0).ToArray();
            var (flow, view) = Open();
            Finish(flow, view, accepted);
            var heard = flow.Journal.Heard;
            foreach (var id in new[] { "e1.promise", "e1.remember", "e1.rest", "e1.kin" })
                Assert.That(heard, Does.Contain(id), "every visitor hears the full agreement");
            AssertPair(heard, "e1.offer.carried", "e1.offer.beside", accepted.Contains("receive-flour"));
            AssertPair(heard, "e1.witness.helped", "e1.witness.quiet", accepted.Contains("offer-flour"));
            AssertPair(heard, "home.trade.helped", "home.trade.quiet", accepted.Contains("offer-flour"));
            AssertPair(heard, "home.bead.held", "home.bead.beside", accepted.Contains("receive-bead"));
            Assert.That(flow.Journal.Outcomes.Keys, Is.EquivalentTo(Invitations));

            Assert.That(flow.Keep(), Is.True);
            Assert.That(flow.BeginAgain(), Is.True);
            Assert.That(flow.Follow(), Is.True);
            var continuation = flow.Plan.Plan.Beats.SelectMany(b => b.Lines).Select(l => l.Id).ToList();
            AssertPair(continuation, "again.trade.helped", "again.trade.quiet", accepted.Contains("offer-flour"));
            AssertPair(continuation, "again.bead.held", "again.bead.left", accepted.Contains("receive-bead"));
        }

        [Test]
        public void ASilentVisitorIsTakenIntoTheOnlyStoryButAReturningOneIsAsked()
        {
            var store = new MemoryStore();
            var flow = new VisitFlow(Package(), store: store);
            var view = new FakePresentation(flow);
            flow.Start();
            view.RunUntil(() => flow.Phase == VisitPhase.Choosing && flow.CurrentLine == null);
            view.Advance(14);
            Assert.That(flow.Phase, Is.EqualTo(VisitPhase.Choosing), "the fire leaves time to choose");
            view.Advance(1.2);
            Assert.That(flow.Phase, Is.Not.EqualTo(VisitPhase.Choosing), "silence is a way to begin");
            Assert.That(view.Of<Cue.VisitStarted>().Single().Mode, Is.EqualTo(VisitMode.Chosen));
            Assert.That(flow.Plan.Plan.Route, Is.EqualTo("era.1850s"));

            Finish(flow, view);
            Assert.That(flow.Keep(), Is.True);
            var again = new VisitFlow(Package(), store: store);
            var returning = new FakePresentation(again);
            again.Start();
            returning.Advance(120);
            Assert.That(again.Phase, Is.EqualTo(VisitPhase.Choosing), "with a kept visit, the visitor chooses what comes next");
            Assert.That(returning.Of<Cue.VisitStarted>(), Is.Empty);
        }

        static void AssertPair(IEnumerable<string> heard, string accepted, string passive, bool acted)
        {
            Assert.That(heard, Does.Contain(acted ? accepted : passive));
            Assert.That(heard, Does.Not.Contain(acted ? passive : accepted));
        }

        [Test]
        public void RecordedWordsMatchEveryCaptionAndOlderAudioRemainsAvailable()
        {
            var p = Package().Package;
            var baseline = ReadEnkareNairobi();
            IEnumerable<Line> Lines(StoryPackage story) => story.Welcome.Concat(story.WelcomeBack)
                .Concat(story.Choose).Concat(story.EarlyReturn).Concat(story.Beats.SelectMany(b => b.Lines
                    .Concat(b.Invitation?.Accepted ?? new Line[0]).Concat(b.Invitation?.Default ?? new Line[0])
                    .Concat(b.Questions.SelectMany(q => q.Lines))));
            var originals = Lines(baseline).ToDictionary(l => l.Id);
            foreach (var line in Lines(p))
            {
                Assert.That(line.Audio, Is.Not.Null, line.Id);
                Assert.That(File.Exists(RepositoryFile("unity/Assets/Sankofa/Resources/Narration/" + line.Audio + ".mp3")), Is.True, line.Id);
                if (line.Audio.StartsWith("enkare-nairobi/"))
                {
                    Assert.That(line.Text, Is.EqualTo(originals[line.Id].Text), "never reuse old speech for changed words");
                    continue;
                }
                var audioNamespace = line.Audio.Substring(0, line.Audio.IndexOf('/'));
                using var manifest = JsonDocument.Parse(File.ReadAllText(RepositoryFile(
                    "unity/Assets/Sankofa/Content/" + audioNamespace + ".narration.json")));
                var record = manifest.RootElement.GetProperty(line.Id);
                Assert.That(record.GetProperty("text").GetString(), Is.EqualTo(line.Text), line.Id);
                var fingerprint = string.Join("\n", new[] { line.Text, record.GetProperty("model").GetString(),
                    record.GetProperty("voice").GetString(), record.GetProperty("direction").GetString() });
                var hash = System.Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint))).ToLowerInvariant();
                Assert.That(record.GetProperty("fingerprint").GetString(), Is.EqualTo(hash), line.Id);
                Assert.That(line.Seconds, Is.EqualTo(record.GetProperty("seconds").GetDouble()), line.Id);
            }
            foreach (var line in Lines(baseline).Where(l => l.Audio != null))
                Assert.That(File.Exists(RepositoryFile("unity/Assets/Sankofa/Resources/Narration/" + line.Audio + ".mp3")), Is.True,
                    "older plans must still find " + line.Audio);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void LeavingTheBeadAtTheGateRequiresAnExplicitAction(bool hang)
        {
            var (flow, view) = Open(); Finish(flow, view);
            flow.Keep(); flow.BeginAgain(); flow.Follow();
            var start = view.Cues.Count;
            Finish(flow, view, hang ? new[] { "hang-bead" } : new string[0]);
            var placements = view.Cues.Skip(start).OfType<Cue.Act>()
                .Where(c => c.Action.Kind == ActionKind.Place && c.Action.Object == "bead.today").ToArray();
            Assert.That(placements.Length, Is.EqualTo(1));
            Assert.That(placements[0].Action.At, Is.EqualTo(hang ? "gate" : null));
            AssertPair(flow.Journal.Heard, "today.brought.shared", "today.brought.carried", hang);
        }

        [Test]
        public void LookingSuggestsARelevantQuestionButCannotResolveTheInvitation()
        {
            var (flow, view) = Open();
            view.RunUntil(() => flow.OpenInvitation?.Id == "receive-flour");
            var before = flow.Journal.Outcomes.Count;
            Assert.That(flow.Notice("gourd.flour"), Is.True); view.Pump();
            Assert.That(view.Of<Cue.Noticed>().Last().QuestionId, Is.EqualTo("q.e1.flour"));
            Assert.That(flow.Journal.Outcomes.Count, Is.EqualTo(before));
            Assert.That(flow.OpenInvitation.Id, Is.EqualTo("receive-flour"));
            Assert.That(flow.Ask("q.e1.flour"), Is.True);
            view.RunUntil(() => flow.Answering == null);
            Assert.That(flow.OpenInvitation.Id, Is.EqualTo("receive-flour"));
            Finish(flow, view);
            Assert.That(flow.Journal.Outcomes["receive-flour"], Is.EqualTo(Outcome.Default));
            Assert.That(flow.Journal.Heard, Does.Contain("home.trade.quiet"));
        }

        [Test]
        public void QuestionsAboutTheAgreementWaitForTheRevelationAndSurvivePause()
        {
            var (flow, view) = Open();
            view.RunUntil(() => flow.CurrentLine?.Id == "e1.remember");
            Assert.That(flow.Questions.Select(q => q.Id), Does.Not.Contain("q.e1.trust"));
            view.RunUntil(() => flow.Questions.Any(q => q.Id == "q.e1.trust"));
            Assert.That(flow.Journal.Heard, Does.Contain("e1.rest"));
            Assert.That(flow.Notice("figure.naserian"), Is.True); view.Pump();
            Assert.That(view.Of<Cue.Noticed>().Last().QuestionId, Is.EqualTo("q.e1.trust"));
            Assert.That(flow.Ask("q.e1.trust"), Is.True); view.Pump();
            var staleToken = view.SpeakingToken;
            flow.Pause(); view.Pump(); view.Advance(30);
            Assert.That(flow.LineFinished(staleToken), Is.False);
            flow.Resume(); view.Pump();
            view.RunUntil(() => flow.Answering == null);
            Assert.That(flow.Journal.Asked.Count(id => id == "q.e1.trust"), Is.EqualTo(1));
            Finish(flow, view);
            Assert.That(flow.Journal.Heard, Does.Contain("q.e1.trust.answer"));
        }

        [Test]
        public void KeptMixedPathReplaysItsActualClosingWordsAndAudioReferences()
        {
            var (flow, view) = Open();
            Finish(flow, view, "receive-flour", "receive-bead");
            var heard = flow.Journal.Heard.ToArray();
            var spoken = view.Of<Cue.Speak>().Select(c => (c.Line.Id, c.Line.Text, c.Line.Audio)).ToArray();
            Assert.That(flow.Keep(), Is.True); flow.BeginAgain(); view.Pump();
            var start = view.Cues.Count;
            Assert.That(flow.Replay(), Is.True);
            Finish(flow, view);
            Assert.That(flow.Journal.Heard, Is.EqualTo(heard));
            Assert.That(view.Cues.Skip(start).OfType<Cue.Speak>().Select(c => (c.Line.Id, c.Line.Text, c.Line.Audio)), Is.EqualTo(spoken));
        }
    }
}
