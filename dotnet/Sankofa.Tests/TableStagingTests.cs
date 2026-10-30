using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Sankofa.Core.Personal;

namespace Sankofa.Tests
{
    public class TableStagingTests
    {
        static TableStaging Shipped() => TableStaging.Parse(File.ReadAllText(Fixtures.RepositoryFile("unity/Assets/Sankofa/Content/personal-table-staging.json")));

        static PersonalStoryDocument Telling(string interest) =>
            BundledPersonalStoryComposer.Compose(new PersonalStoryRequest("enkare-nairobi", "1850s", interest)).Story;

        [Test]
        public void EveryPreparedChapterIsStagedAndEveryChoiceHasAFigureToTouch()
        {
            var staging = Shipped();
            foreach (var interest in PersonalStoryCatalog.Interests)
            {
                var story = Telling(interest);
                foreach (var chapter in story.Chapters)
                    Assert.That(staging.For(chapter.Id), Is.Not.Null, chapter.Id + " has no staging");
                var choices = story.Chapters.SelectMany(c => (IEnumerable<PersonalStoryChoice>)c.Choices ?? Array.Empty<PersonalStoryChoice>()).ToArray();
                Assert.That(choices, Is.Not.Empty, interest);
                // Two perspectives stand for two different people, so touching one figure is never ambiguous.
                Assert.That(choices.Select(c => staging.FigureFor(c.Id)).Distinct().Count(), Is.EqualTo(choices.Length), interest);
                foreach (var choice in choices)
                {
                    var figure = staging.FigureFor(choice.Id);
                    var next = staging.For(choice.Next);
                    Assert.That(next.Entry, Is.EqualTo(TableStaging.Turn), choice.Next + " turns the stage to the chosen side");
                    // The chosen figure stands nearest the visitor when the stage has turned.
                    var front = TableStaging.Figures.OrderBy(f => next.Beats.Where(b => b.Who == f && b.Z.HasValue).Select(b => b.Z.Value).DefaultIfEmpty(1).First()).First();
                    Assert.That(front, Is.EqualTo(figure), choice.Next);
                }
            }
            var known = PersonalStoryCatalog.Interests.SelectMany(i => Telling(i).Chapters.Select(c => c.Id)).ToArray();
            Assert.That(staging.ChapterIds.Except(known), Is.Empty, "staging names only prepared chapters");
        }

        [Test]
        public void TheVisitorIsOfferedTheGourdOnlyWhereTheWordsSetItBetweenTheWomen()
        {
            var staging = Shipped();
            var invited = staging.ChapterIds.Select(staging.For).SelectMany(c => c.Beats.Where(b => b.Invite).Select(b => (c.Id, b))).ToArray();
            Assert.That(invited.Select(x => x.Id), Is.EquivalentTo(new[] { "trade-household.agreement", "trade-household.expected" }), "the gourd's own telling offers it on both sides");
            foreach (var (id, beat) in invited)
            {
                var chapter = Telling("trade-household").Chapters.Single(c => c.Id == id);
                Assert.That(beat.Place, Is.EqualTo(TableStaging.GourdBetween));
                Assert.That(chapter.Narration, Does.Contain("between them").Or.Contain("let us begin with what is here"), id + ": the words describe the moment the visitor may make");
            }
        }

        [Test]
        public void TheChoicePageShowsBothFiguresBeforeItsNarrationEnds()
        {
            var staging = Shipped();
            foreach (var interest in PersonalStoryCatalog.Interests)
            {
                var offer = Telling(interest).Chapters.Single(c => c.Choices != null && c.Choices.Count > 0);
                var beats = staging.For(offer.Id).Beats;
                foreach (var figure in TableStaging.Figures)
                {
                    var last = beats.LastOrDefault(b => b.Who == figure && b.Shown.HasValue);
                    Assert.That(last?.Shown, Is.True, offer.Id + ": " + figure + " must be there to be chosen");
                }
            }
        }

        [TestCase("{\"version\":1,\"choices\":{},\"chapters\":{\"a\":{\"beats\":[{\"at\":0.5,\"who\":\"wambui\"},{\"at\":0.2,\"who\":\"wambui\"}]}}}", "out of order")]
        [TestCase("{\"version\":1,\"choices\":{},\"chapters\":{\"a\":{\"beats\":[{\"at\":0,\"who\":\"stranger\"}]}}}", "unknown figure")]
        [TestCase("{\"version\":1,\"choices\":{},\"chapters\":{\"a\":{\"beats\":[{\"at\":0,\"who\":\"wambui\",\"x\":0.6}]}}}", "leaves the stage")]
        [TestCase("{\"version\":1,\"choices\":{},\"chapters\":{\"a\":{\"beats\":[{\"at\":0,\"who\":\"wambui\",\"pose\":3}]}}}", "not painted")]
        [TestCase("{\"version\":1,\"choices\":{},\"chapters\":{\"a\":{\"beats\":[{\"at\":0,\"who\":\"gourd\",\"place\":\"with\"}]}}}", "needs a figure")]
        [TestCase("{\"version\":1,\"choices\":{},\"chapters\":{\"a\":{\"beats\":[{\"at\":1.5,\"who\":\"wambui\"}]}}}", "outside its narration")]
        [TestCase("{\"version\":1,\"choices\":{\"x\":\"nobody\"},\"chapters\":{}}", "unknown figure")]
        [TestCase("{\"version\":1,\"choices\":{},\"chapters\":{\"a\":{\"entry\":\"spin\",\"beats\":[]}}}", "unknown entry")]
        [TestCase("{\"version\":1,\"choices\":{},\"chapters\":{\"a\":{\"beats\":[{\"at\":0.5,\"who\":\"wambui\",\"invite\":true}]}}}", "Only the gourd")]
        [TestCase("{\"version\":1,\"choices\":{},\"chapters\":{\"a\":{\"beats\":[{\"at\":0.5,\"who\":\"gourd\",\"place\":\"lifted\",\"invite\":true}]}}}", "Only setting the gourd between")]
        public void MalformedStagingIsRefusedRatherThanGuessed(string json, string problem)
        {
            Assert.That(() => TableStaging.Parse(json), Throws.TypeOf<FormatException>().With.Message.Contains(problem));
        }
    }
}
