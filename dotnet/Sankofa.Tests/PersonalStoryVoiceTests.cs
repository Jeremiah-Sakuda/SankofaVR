using System.Linq;
using NUnit.Framework;
using Sankofa.Core.Personal;

namespace Sankofa.Tests
{
    public class PersonalStoryVoiceTests
    {
        [TestCase("journeys", "journeys.arrival", "journeys.context", "journeys.meeting", "scenery-atlas", "scenery-atlas", "cast-atlas", "C6")]
        [TestCase("trade-household", "trade-household.load", "trade-household.vessel", "trade-household.agreement", "cast-atlas", "cast-atlas", "cast-atlas", "C7")]
        [TestCase("place-water", "place-water.listening", "place-water.name", "place-water.return", "scenery-atlas", "scenery-atlas", "modern-river", "C1")]
        public void VoicePassPreservesContextIdsArtAndEvidence(string interest, string first, string context, string last, string firstArt, string contextArt, string lastArt, string claim)
        {
            var story = BundledPersonalStoryComposer.Compose(new PersonalStoryRequest("enkare-nairobi", "1850s", interest)).Story;
            Assert.That(story.Region, Is.EqualTo("enkare-nairobi"));
            Assert.That(story.Period, Is.EqualTo("1850s"));
            Assert.That(story.Chapters.Select(c => c.Id), Is.EqualTo(new[] { first, context, last }));
            Assert.That(story.Chapters.Select(c => c.Illustration), Is.EqualTo(new[] { firstArt, contextArt, lastArt }));
            Assert.That(story.Chapters.Select(c => c.Truth), Is.EqualTo(new[] { "reconstructed", "historical", "reconstructed" }));
            Assert.That(story.Chapters[0].Narration, Does.StartWith(PersonalStoryCatalog.ReconstructionPrefix));
            Assert.That(story.Chapters[2].Narration, Does.StartWith(PersonalStoryCatalog.ReconstructionPrefix));
            Assert.That(story.Chapters[1].Sources, Is.EqualTo(new[] { claim }));
            Assert.That(PersonalStoryCatalog.IsApprovedHistory(story.Chapters[1].Narration, story.Chapters[1].Sources), Is.True);
            Assert.That(PersonalStoryValidator.Validate(story).IsValid, Is.True);
        }

        [Test]
        public void StoryNarrationLeavesOperationalDisclosuresToTheInterfaceAndQuestions()
        {
            foreach (var interest in PersonalStoryCatalog.Interests)
            {
                var story = BundledPersonalStoryComposer.Compose(new PersonalStoryRequest("enkare-nairobi", "1850s", interest)).Story;
                foreach (var chapter in story.Chapters.Where(c => c.Truth == "reconstructed"))
                    foreach (var oldInstruction in new[] { "bundled telling", "saving it is your choice", "keep this telling", "The scene offers a question", "Let the painted paths" })
                        Assert.That(chapter.Narration, Does.Not.Contain(oldInstruction).IgnoreCase);
                Assert.That(story.Chapters.SelectMany(c => c.Questions).Any(), Is.True, "Evidence and reconstruction questions remain available separately.");
            }
        }
    }
}
