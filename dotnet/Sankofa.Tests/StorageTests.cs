using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sankofa.Core.Story;
using Sankofa.Core.Visits;
using Sankofa.Storage;
using NUnit.Framework;
using static Sankofa.Tests.Fixtures;

namespace Sankofa.Tests
{
    public class SavedVisitTests
    {
        internal static SavedVisit Visit(string id, int minute, string route = "makers")
        {
            var package = Playable(ReadPlaceholder());
            var plan = package.Compose(route);
            var journal = new VisitJournal(1, plan, VisitMode.Chosen);
            journal.Started(0);
            journal.LineHeard(1, "makers.gather");
            journal.InvitationResolved(2, "receive-ember", Outcome.Accepted);
            journal.QuestionAsked(3, "q.bowl.maker");
            return SavedVisitCodec.FromVisit(id, new DateTimeOffset(2026, 10, 4, 21, minute, 0, TimeSpan.Zero), plan, journal);
        }

        [Test]
        public void AKeptVisitIsWrittenAndReadBackExactly()
        {
            var visit = Visit("v1", 5);
            var json = SavedVisitCodec.Write(visit);
            var read = SavedVisitCodec.Read(json);
            Assert.That(SavedVisitCodec.Write(read), Is.EqualTo(json));
            Assert.That(read.SavedAt, Is.EqualTo(visit.SavedAt));
            Assert.That(read.Outcomes["receive-ember"], Is.EqualTo(Outcome.Accepted));
            Assert.That(read.Asked, Is.EqualTo(new[] { "q.bowl.maker" }));
            Assert.That(read.Heard, Is.EqualTo(new[] { "makers.gather" }));
            Assert.That(PlanValidator.Validate(read.Plan).IsValid, Is.True);
            Assert.That(Sankofa.Core.Json.JsonText.ParseObject(json).Keys, Is.EqualTo(new[]
            {
                "schema", "kind", "id", "saved_at", "package", "version", "locale", "route", "label", "next", "keepsake", "outcomes", "asked", "heard", "plan",
            }), "story choices and content only: no name, voice, room or hand data");
        }

        [Test]
        public void IdsAreSafeFileNames()
        {
            Assert.That(SavedVisitCodec.IsValidId("v20261004-ab12"), Is.True);
            foreach (var bad in new[] { "", "../escape", "a/b", "UPPER", ".hidden", "-dash", new string('a', 65) })
                Assert.That(SavedVisitCodec.IsValidId(bad), Is.False, bad);
            Assert.Throws<ArgumentException>(() => Visit("../escape", 1));
        }

        [Test]
        public void AnUnreadablePlanLeavesTheVisitListedWithoutIt()
        {
            var json = SavedVisitCodec.Write(Visit("v1", 1)).Replace("\"kind\": \"plan\"", "\"kind\": \"plan\", \"mystery\": 1");
            var read = SavedVisitCodec.Read(json);
            Assert.That(read.Plan, Is.Null);
            Assert.That(read.PlanProblem, Is.EqualTo("plan.mystery: unknown field"));
            Assert.That(read.Route, Is.EqualTo("makers"), "it can still be continued or forgotten");
        }

        [Test]
        public void AnotherSchemaOrKindIsRefused()
        {
            var json = SavedVisitCodec.Write(Visit("v1", 1));
            Assert.That(Assert.Throws<StoryFormatException>(() => SavedVisitCodec.Read(json.Replace("\"schema\": 1,\n  \"kind\": \"kept-visit\"", "\"schema\": 7,\n  \"kind\": \"kept-visit\""))).Path,
                Is.EqualTo("$.schema"));
            Assert.That(Assert.Throws<StoryFormatException>(() => SavedVisitCodec.Read(json.Replace("\"kind\": \"kept-visit\"", "\"kind\": \"note\""))).Path,
                Is.EqualTo("$.kind"));
        }
    }

    public class FileVisitStoreTests
    {
        string folder;

        [SetUp]
        public void CreateFolder() => folder = Path.Combine(Path.GetTempPath(), "sankofa-store-" + Guid.NewGuid().ToString("N"));

        [TearDown]
        public void DeleteFolder()
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }

        [Test]
        public void KeepsLoadsNewestFirstAndForgets()
        {
            var store = new FileVisitStore(folder);
            Assert.That(store.Load().Visits, Is.Empty, "a missing folder is an empty shelf");
            store.Save(SavedVisitTests.Visit("v-older", 1));
            store.Save(SavedVisitTests.Visit("v-newer", 9, "travellers"));
            var contents = store.Load();
            Assert.That(contents.Problems, Is.Empty);
            Assert.That(contents.Visits.Select(v => v.Id), Is.EqualTo(new[] { "v-newer", "v-older" }));
            Assert.That(contents.Visits[0].Plan.Route, Is.EqualTo("travellers"));

            store.Delete("v-newer");
            Assert.That(store.Load().Visits.Select(v => v.Id), Is.EqualTo(new[] { "v-older" }));
            Assert.DoesNotThrow(() => store.Delete("v-newer"), "forgetting twice is harmless");
            Assert.That(Directory.GetFiles(folder).Select(Path.GetFileName), Is.EqualTo(new[] { "v-older.json" }), "no temporary files remain");
        }

        [Test]
        public void SavingAgainReplacesTheFileInPlace()
        {
            var store = new FileVisitStore(folder);
            store.Save(SavedVisitTests.Visit("v1", 1));
            store.Save(SavedVisitTests.Visit("v1", 2));
            Assert.That(store.Load().Visits.Single().SavedAt.Minute, Is.EqualTo(2));
        }

        [Test]
        public void UnreadableFilesAreReportedAndLeftInPlace()
        {
            var store = new FileVisitStore(folder);
            store.Save(SavedVisitTests.Visit("v1", 1));
            File.WriteAllText(Path.Combine(folder, "broken.json"), "{ not json");
            File.WriteAllText(Path.Combine(folder, "v2.json"), SavedVisitCodec.Write(SavedVisitTests.Visit("v3", 1)));
            File.WriteAllText(Path.Combine(folder, "v4.json.tmp"), "partial");
            var contents = store.Load();
            Assert.That(contents.Visits.Select(v => v.Id), Is.EqualTo(new[] { "v1" }));
            Assert.That(contents.Problems.Count, Is.EqualTo(2));
            Assert.That(contents.Problems.Any(p => p.StartsWith("broken.json")), Is.True);
            Assert.That(contents.Problems.Any(p => p.Contains("does not match the file name")), Is.True);
            Assert.That(File.Exists(Path.Combine(folder, "broken.json")), Is.True, "never deleted without the visitor asking");
        }

        [Test]
        public void AFailedSaveLeavesEarlierVisitsIntactAndNoPartialFile()
        {
            var store = new FileVisitStore(folder);
            store.Save(SavedVisitTests.Visit("v1", 1));
            Directory.CreateDirectory(Path.Combine(folder, "v2.json"));
            var failure = Assert.Throws<VisitStoreException>(() => store.Save(SavedVisitTests.Visit("v2", 2)));
            Assert.That(failure.Message, Does.StartWith("The visit could not be kept"));
            Assert.That(File.Exists(Path.Combine(folder, "v2.json.tmp")), Is.False);
            Assert.That(store.Load().Visits.Select(v => v.Id), Is.EqualTo(new[] { "v1" }));
        }

        [Test]
        public void UnsafeIdsAreRefusedBeforeTouchingTheDisk()
        {
            var store = new FileVisitStore(folder);
            Assert.Throws<VisitStoreException>(() => store.Delete("../outside"));
            Assert.That(Directory.Exists(folder), Is.False);
        }
    }
}
