using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Sankofa.Core.Personal;
using Sankofa.Storage;

namespace Sankofa.Tests
{
    public sealed class PersonalStrandStoreTests
    {
        string folder;
        [SetUp] public void SetUp() => folder = Path.Combine(Path.GetTempPath(), "sankofa-strand-store-" + Guid.NewGuid().ToString("N"));
        [TearDown] public void Clean()
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            else if (File.Exists(folder)) File.Delete(folder);
        }
        string Shelf => Path.Combine(folder, "kept.strand");

        [Test]
        public void NothingIsStoredUntilTheStrandIsSaved()
        {
            var store = new PersonalStrandStore(folder);
            Assert.That(store.Load().Count, Is.EqualTo(0));
            Assert.That(Directory.Exists(folder), Is.False, "loading creates nothing");
            var strand = new KeptStrand();
            strand.Add("trade-household", "trade-household.follow.more");
            strand.Add("journeys", "journeys.follow.late");
            store.Save(strand);
            Assert.That(File.ReadAllText(Shelf), Is.EqualTo(KeptStrandJson.Write(strand)), "identifiers only, in the documented form");
            Assert.That(Directory.GetFiles(folder), Has.Length.EqualTo(1), "no temporary file is left behind");
            var read = new PersonalStrandStore(folder).Load();
            Assert.That(read.Beads, Is.EqualTo(strand.Beads));
            read.Add("journeys", "journeys.follow.waiting");
            store.Save(read);
            Assert.That(store.Load().Beads.Select(b => b.Side), Is.EqualTo(new[] { "trade-household.follow.more", "journeys.follow.late", "journeys.follow.waiting" }));
        }

        [TestCase("")]
        [TestCase("forgotten")]
        [TestCase("{\"schema\":\"sankofa.kept-strand\",\"version\":1,\"beads\":[")]
        [TestCase("{\"schema\":\"sankofa.kept-strand\",\"version\":1,\"beads\":[{\"telling\":\"journeys\",\"side\":\"journeys.follow.unknown\"}]}")]
        [TestCase("{\"schema\":\"sankofa.kept-strand\",\"version\":9,\"beads\":[]}")]
        public void ADamagedFileIsAnEmptyStrandAndCanBeReplaced(string damaged)
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(Shelf, damaged);
            var store = new PersonalStrandStore(folder);
            var strand = store.Load();
            Assert.That(strand.Count, Is.EqualTo(0));
            Assert.That(File.ReadAllText(Shelf), Is.EqualTo(damaged), "loading never rewrites or deletes the file");
            strand.Add("place-water", "place-water.follow.traveller");
            store.Save(strand);
            Assert.That(store.Load().Count, Is.EqualTo(1));
        }

        [Test]
        public void AFailedSaveReachesTheCallerAndLeavesTheEarlierStrand()
        {
            var store = new PersonalStrandStore(folder);
            var kept = new KeptStrand();
            kept.Add("journeys", "journeys.follow.late");
            store.Save(kept);
            // A directory where the temporary file belongs makes the write fail on every platform, whoever runs the tests.
            Directory.CreateDirectory(Shelf + ".tmp");
            var more = store.Load();
            more.Add("journeys", "journeys.follow.waiting");
            Assert.That(() => store.Save(more), Throws.InstanceOf<IOException>().Or.InstanceOf<UnauthorizedAccessException>());
            Assert.That(store.Load().Beads, Is.EqualTo(kept.Beads), "the strand on disk is still the one that was saved");
        }

        [Test]
        public void AFolderThatCannotBeCreatedFailsTheSaveNotTheLoad()
        {
            File.WriteAllText(folder, "a file where the folder should be");
            var store = new PersonalStrandStore(folder);
            Assert.That(store.Load().Count, Is.EqualTo(0));
            var strand = new KeptStrand();
            strand.Add("journeys", "journeys.follow.late");
            Assert.That(() => store.Save(strand), Throws.InstanceOf<IOException>().Or.InstanceOf<UnauthorizedAccessException>());
            Assert.Throws<ArgumentNullException>(() => store.Save(null));
            Assert.Throws<ArgumentNullException>(() => new PersonalStrandStore(null));
        }

        [Test]
        public void ForgettingRemovesTheStrandAndCanBeRepeated()
        {
            var store = new PersonalStrandStore(folder);
            store.Forget();
            Assert.That(Directory.Exists(folder), Is.False, "forgetting nothing creates nothing");
            var strand = new KeptStrand();
            strand.Add("journeys", "journeys.follow.late");
            store.Save(strand);
            store.Forget();
            Assert.That(File.Exists(Shelf), Is.False);
            Assert.That(store.Load().Count, Is.EqualTo(0));
            store.Forget();
            store.Save(strand);
            Assert.That(store.Load().Count, Is.EqualTo(1), "a new strand can be kept after forgetting");
        }

        [Test]
        public void TheStrandAndTheKeptTellingShareAFolderWithoutTouchingEachOther()
        {
            var story = BundledPersonalStoryComposer.Compose(new PersonalStoryRequest("enkare-nairobi", "1850s", "journeys")).Story;
            var stories = new PersonalStoryStore(folder);
            var strands = new PersonalStrandStore(folder);
            stories.Keep(story);
            var strand = new KeptStrand();
            strand.Add("journeys", "journeys.follow.late");
            strands.Save(strand);
            stories.Forget();
            Assert.That(strands.Load().Count, Is.EqualTo(1), "forgetting the telling is a separate decision from forgetting the strand");
            strands.Forget();
            Assert.That(stories.HasKept, Is.False);
            stories.Keep(story);
            Assert.That(stories.Load().Interest, Is.EqualTo("journeys"));
        }
    }
}
