using System;
using System.IO;
using NUnit.Framework;
using Sankofa.Core.Personal;
using Sankofa.Storage;

namespace Sankofa.Tests
{
    public sealed class PersonalStoreTests
    {
        string folder;
        [SetUp] public void SetUp() => folder = Path.Combine(Path.GetTempPath(), "sankofa-personal-store-" + Guid.NewGuid().ToString("N"));
        [TearDown] public void Clean() { if (Directory.Exists(folder)) Directory.Delete(folder, true); }

        [Test] public void AStoryIsNotStoredUntilKeepAndReplayDoesNotRecompose()
        {
            var store = new PersonalStoryStore(folder);
            var story = BundledPersonalStoryComposer.Compose(new PersonalStoryRequest("enkare-nairobi", "1850s", "journeys", question: "How did journeys connect this place?")).Story;
            Assert.That(store.Load(), Is.Null); Assert.That(Directory.Exists(folder), Is.False);
            store.Keep(story); Assert.That(store.HasKept, Is.True);
            Assert.That(PersonalStoryJson.Write(store.Load()), Is.EqualTo(PersonalStoryJson.Write(story)));
            Assert.That(File.ReadAllText(Path.Combine(folder, "kept.story")), Does.Not.Contain("How did journeys connect this place?"));
        }

        [Test] public void ForgetAndReplaceHaveOneAuthoritativeShelf()
        {
            var store = new PersonalStoryStore(folder);
            var first = BundledPersonalStoryComposer.Compose(new PersonalStoryRequest("enkare-nairobi", "1850s", "journeys")).Story;
            var second = BundledPersonalStoryComposer.Compose(new PersonalStoryRequest("enkare-nairobi", "1850s", "place-water")).Story;
            store.Keep(first); store.Keep(second); Assert.That(store.Load().Interest, Is.EqualTo("place-water"));
            store.Forget(); Assert.That(store.Load(), Is.Null); Assert.That(store.HasKept, Is.False);
        }

        [Test] public void InvalidReplacementLeavesEarlierStoryIntact()
        {
            var store = new PersonalStoryStore(folder);
            var first = BundledPersonalStoryComposer.Compose(new PersonalStoryRequest("enkare-nairobi", "1850s", "journeys")).Story;
            store.Keep(first);
            var invalid = BundledPersonalStoryComposer.Compose(new PersonalStoryRequest("enkare-nairobi", "1850s", "place-water")).Story;
            invalid.Schema = "not-supported";
            Assert.Throws<InvalidDataException>(() => store.Keep(invalid));
            Assert.That(store.Load().Interest, Is.EqualTo("journeys"));
        }
    }
}
