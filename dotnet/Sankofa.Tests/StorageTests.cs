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
                "schema", "kind", "id", "saved_at", "package", "version", "locale", "route", "label", "perspective_selection", "next", "keepsake", "outcomes", "asked", "heard", "plan",
            }), "story choices and content only: no name, voice, room or hand data");
        }

        [Test] public void LegacyPerspectivePreferenceIsUnknownRatherThanInferred()
        {
            var json = SavedVisitCodec.Write(Visit("legacy", 1)).Replace("  \"perspective_selection\": \"explicit\",\n", "");
            var read = SavedVisitCodec.Read(json);
            Assert.That(read.Selection, Is.EqualTo(PerspectiveSelection.Unknown));
            Assert.That(SavedVisitCodec.Write(read), Is.EqualTo(json));
            Assert.Throws<StoryFormatException>(() => SavedVisitCodec.Read(json.Replace("\"label\":", "\"perspective_selection\": \"inferred\", \"label\":")));
        }

        [Test]
        public void WhatWasLingeredOnIsKeptMostFirstAndAtMostThree()
        {
            var package = Playable(ReadPlaceholder());
            var plan = package.Compose("makers");
            var journal = new VisitJournal(1, plan, VisitMode.Chosen);
            journal.Noticed(1, "bowl");
            journal.Noticed(2, "shard");
            journal.Noticed(3, "shard");
            journal.Noticed(4, "lantern");
            journal.Noticed(5, "ember");
            Assert.That(journal.Lingered(), Is.EqualTo(new[] { "shard", "bowl", "lantern" }), "most looked at first, then in the order first noticed");
            Assert.That(journal.Entries.Count(e => e.Kind == JournalKind.Noticed), Is.EqualTo(4), "one journal entry per object");
            var visit = SavedVisitCodec.FromVisit("v2", new DateTimeOffset(2026, 10, 4, 21, 0, 0, TimeSpan.Zero), plan, journal);
            var json = SavedVisitCodec.Write(visit);
            Assert.That(SavedVisitCodec.Read(json).Lingered, Is.EqualTo(new[] { "shard", "bowl", "lantern" }));
            Assert.That(Sankofa.Core.Json.JsonText.ParseObject(json).Keys, Does.Contain("lingered"));
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
            Assert.That(store.Load().Visits, Is.Empty, "forgetting the active legacy shelf cannot expose an older visit");
            Assert.DoesNotThrow(() => store.Delete("v-newer"), "forgetting twice is harmless");
            Assert.That(Directory.GetFiles(folder).Select(Path.GetFileName), Is.EqualTo(new[] { "current.visit" }), "the durable empty shelf remains, without temporary or legacy files");
        }

        [Test]
        public void ReplacementAndForgetCannotResurrectAnObsoleteLegacyVisit()
        {
            var store = new FileVisitStore(folder);
            store.Save(SavedVisitTests.Visit("v-old", 1));
            store.Replace(SavedVisitTests.Visit("v-new", 2));
            // Simulate a process ending before the previous JSON was cleaned up.
            Assert.That(File.Exists(Path.Combine(folder, "v-old.json")), Is.True);
            Assert.That(new FileVisitStore(folder).Load().Visits.Select(v => v.Id), Is.EqualTo(new[] { "v-new" }));
            store.Delete("v-new");
            // Even an obsolete record left by interrupted cleanup is hidden by the durable empty shelf.
            File.WriteAllText(Path.Combine(folder, "v-old.json"), SavedVisitCodec.Write(SavedVisitTests.Visit("v-old", 1)));
            Assert.That(new FileVisitStore(folder).Load().Visits, Is.Empty);
        }

        [Test]
        public void AFailedReplacementLeavesTheActiveVisitIntact()
        {
            var store = new FileVisitStore(folder);
            store.Replace(SavedVisitTests.Visit("v-old", 1));
            Directory.CreateDirectory(Path.Combine(folder, "current.visit.tmp"));
            Assert.Throws<VisitStoreException>(() => store.Replace(SavedVisitTests.Visit("v-new", 2)));
            Assert.That(store.Load().Visits.Single().Id, Is.EqualTo("v-old"));
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
