using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using Sankofa.Core.Json;
using Sankofa.Core.Personal;

namespace Sankofa.Tests
{
    public class PersonalStoryVoiceTests
    {
        [TestCase("journeys", "journeys.arrival", "journeys.context", "journeys.meeting", "scenery-atlas", "scenery-atlas", "cast-atlas", "C6")]
        [TestCase("trade-household", "trade-household.load", "trade-household.vessel", "trade-household.agreement", "cast-atlas", "cast-atlas", "cast-atlas", "C6")]
        [TestCase("place-water", "place-water.listening", "place-water.name", "place-water.return", "scenery-atlas", "scenery-atlas", "modern-river", "C1")]
        public void VoicePassPreservesContextIdsArtAndEvidence(string interest, string first, string context, string last, string firstArt, string contextArt, string lastArt, string claim)
        {
            var story = BundledPersonalStoryComposer.Compose(new PersonalStoryRequest("enkare-nairobi", "1850s", interest)).Story;
            Assert.That(story.Region, Is.EqualTo("enkare-nairobi"));
            Assert.That(story.Period, Is.EqualTo("1850s"));
            // The recorded default route is unchanged; the other perspective is an added fourth chapter.
            var route = new[] { 0, PersonalStoryRoutes.Continue(story, 0), PersonalStoryRoutes.Continue(story, 1) }.Select(i => story.Chapters[i]).ToArray();
            Assert.That(PersonalStoryRoutes.Continue(story, 2), Is.EqualTo(-1));
            Assert.That(route.Select(c => c.Id), Is.EqualTo(new[] { first, context, last }));
            Assert.That(route.Select(c => c.Illustration), Is.EqualTo(new[] { firstArt, contextArt, lastArt }));
            Assert.That(route.Select(c => c.Truth), Is.EqualTo(new[] { "reconstructed", "historical", "reconstructed" }));
            Assert.That(route[0].Narration, Does.StartWith(PersonalStoryCatalog.ReconstructionPrefix));
            Assert.That(route[2].Narration, Does.StartWith(PersonalStoryCatalog.ReconstructionPrefix));
            var other = story.Chapters[3];
            Assert.That(story.Chapters[1].Choices.Select(c => c.Next), Is.EqualTo(new[] { last, other.Id }));
            Assert.That(other.Truth, Is.EqualTo("reconstructed"));
            Assert.That(other.Illustration, Is.Not.EqualTo("modern-river"), "the other side is an 1850s scene");
            Assert.That(other.Narration, Does.StartWith(PersonalStoryCatalog.ReconstructionPrefix));
            Assert.That(story.Chapters[1].Sources, Is.EqualTo(new[] { claim }));
            Assert.That(PersonalStoryCatalog.IsApprovedHistory(story.Chapters[1].Narration, story.Chapters[1].Sources), Is.True);
            Assert.That(PersonalStoryValidator.Validate(story).IsValid, Is.True);
        }

        /// <summary>
        /// The app finds a recording by the hash of the exact words spoken: a chapter's stored text without its leading
        /// disclosure. Any wording change must update the manifest, so an edited chapter can never silently lose its
        /// voice or play an older recording.
        /// </summary>
        [Test]
        public void NarrationManifestMatchesEveryBundledChapterAndItsRecording()
        {
            static string Sha(byte[] bytes) { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
            var manifest = JsonText.ParseObject(File.ReadAllText(Fixtures.RepositoryFile("unity/Assets/Sankofa/Content/personal-table.narration.json")));
            var entries = ((List<object>)manifest["chapters"]).Cast<JsonObject>().ToDictionary(e => (string)e["chapter_id"]);
            var chapters = PersonalStoryCatalog.Interests.SelectMany(i => BundledPersonalStoryComposer.Compose(new PersonalStoryRequest("enkare-nairobi", "1850s", i)).Story.Chapters).ToArray();
            Assert.That(entries.Keys, Is.EquivalentTo(chapters.Select(c => c.Id)), "one manifest entry per current bundled chapter");
            var recorded = 0;
            foreach (var chapter in chapters)
            {
                var entry = entries[chapter.Id];
                var hash = Sha(Encoding.UTF8.GetBytes(chapter.Narration));
                var spoken = PersonalStoryVoice.Spoken(chapter.Narration);
                Assert.That((string)entry["text"], Is.EqualTo(chapter.Narration), chapter.Id);
                Assert.That((string)entry["text_sha256"], Is.EqualTo(hash), chapter.Id);
                Assert.That(spoken, Does.Not.StartWith(PersonalStoryCatalog.ReconstructionPrefix), chapter.Id + " is recorded without the spoken label");
                Assert.That((string)entry["spoken"], Is.EqualTo(spoken), chapter.Id);
                Assert.That((string)entry["spoken_sha256"], Is.EqualTo(Sha(Encoding.UTF8.GetBytes(spoken))), chapter.Id);
                Assert.That((string)entry["filename"], Is.EqualTo(PersonalStoryVoice.ClipName(spoken) + ".mp3"), chapter.Id);
                var clip = Fixtures.RepositoryFile("unity/Assets/Sankofa/Resources/Narration/personal-table/" + (string)entry["filename"]);
                if (entry.GetStringOrNull("status") == "pending-recording")
                {
                    Assert.That(entry.TryGet("audio_sha256", out _), Is.False, chapter.Id + " cannot claim audio while pending");
                    continue;
                }
                Assert.That(File.Exists(clip), Is.True, chapter.Id + " recording");
                Assert.That(Sha(File.ReadAllBytes(clip)), Is.EqualTo((string)entry["audio_sha256"]), chapter.Id);
                recorded++;
            }
            Assert.That(recorded, Is.GreaterThanOrEqualTo(9), "the three default routes stay fully voiced");
        }

        /// <summary>The welcome and the invitation are recorded like chapters: by the hash of their exact words.</summary>
        [Test]
        public void EveryLineTheGriotSaysOutsideATellingHasItsOwnRecordingEntry()
        {
            static string Sha(byte[] bytes) { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
            var manifest = JsonText.ParseObject(File.ReadAllText(Fixtures.RepositoryFile("unity/Assets/Sankofa/Content/personal-table.narration.json")));
            var entries = ((List<object>)manifest["lines"]).Cast<JsonObject>().ToArray();
            Assert.That(entries.Select(e => (string)e["line_id"]), Is.EqualTo(PersonalStoryVoice.Lines.Select(l => l.Key)));
            Assert.That(entries.Select(e => (string)e["text"]), Is.EqualTo(PersonalStoryVoice.Lines.Select(l => l.Value)));
            foreach (var entry in entries)
            {
                var text = (string)entry["text"];
                Assert.That((string)entry["text_sha256"], Is.EqualTo(Sha(Encoding.UTF8.GetBytes(text))), text);
                Assert.That((string)entry["filename"], Is.EqualTo(PersonalStoryVoice.ClipName(text) + ".mp3"), text);
                if (entry.GetStringOrNull("status") == "pending-recording") { Assert.That(entry.TryGet("audio_sha256", out _), Is.False, text); continue; }
                var clip = Fixtures.RepositoryFile("unity/Assets/Sankofa/Resources/Narration/personal-table/" + (string)entry["filename"]);
                Assert.That(File.Exists(clip), Is.True, text);
                Assert.That(Sha(File.ReadAllBytes(clip)), Is.EqualTo((string)entry["audio_sha256"]), text);
            }
            // The disclosure every imagined page used to speak is said once, before a telling is chosen.
            var welcome = string.Join(" ", PersonalStoryVoice.Welcome);
            Assert.That(welcome, Does.Contain("imagined").And.Contain("documented"));
            Assert.That(PersonalStoryVoice.Welcome.Last(), Is.EqualTo(PersonalStoryVoice.Invitation));
            Assert.That(PersonalStoryVoice.ReturnWelcome.Last(), Is.EqualTo(PersonalStoryVoice.Invitation));
            Assert.That(PersonalStoryVoice.Spoken("Imagined scene: She waits."), Is.EqualTo("She waits."));
            Assert.That(PersonalStoryVoice.Spoken("Before 1900, trade linked them."), Is.EqualTo("Before 1900, trade linked them."));
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

        /// <summary>
        /// What the griot tells on a documented page is the documented sentence itself. Notes about review and about
        /// what a museum example cannot show are for a visitor who asks, in that page's question and its sources.
        /// </summary>
        [Test]
        public void DocumentedPagesAreToldWithoutRegisterNotesAndTheNotesStayWithinReach()
        {
            Assert.That(PersonalStoryCatalog.NamingFact, Does.StartWith(PersonalStoryCatalog.NamingClaim), "the told sentence is the approved passage's own first sentence");
            Assert.That(PersonalStoryCatalog.IsApprovedHistory(PersonalStoryCatalog.NamingClaim, new[] { "C1" }), Is.True);
            Assert.That(PersonalStoryCatalog.IsApprovedHistory(PersonalStoryCatalog.NamingClaim, new[] { "C6" }), Is.False);
            Assert.That(PersonalStoryCatalog.IsApprovedHistory(PersonalStoryCatalog.NamingClaim + " And more.", new[] { "C1" }), Is.False);
            foreach (var interest in PersonalStoryCatalog.Interests)
            {
                var story = BundledPersonalStoryComposer.Compose(new PersonalStoryRequest("enkare-nairobi", "1850s", interest)).Story;
                foreach (var chapter in story.Chapters)
                    foreach (var note in new[] { "need review", "museum", "does not establish" })
                        Assert.That(chapter.Narration, Does.Not.Contain(note).IgnoreCase, chapter.Id);
                var documented = story.Chapters.Single(c => c.Truth == "historical");
                Assert.That(PersonalStoryCatalog.IsApprovedHistory(documented.Narration, documented.Sources), Is.True, documented.Id);
                Assert.That(documented.Questions.All(q => q.Truth == "historical" && PersonalStoryCatalog.IsApprovedHistory(q.Answer, q.Sources)), Is.True, documented.Id);
                // The opening's first question is about the person in it, not about the register.
                Assert.That(story.Chapters[0].Questions[0].Label, Does.StartWith("Why"), interest);
                Assert.That(story.Chapters[0].Questions[0].Truth, Is.EqualTo("reconstructed"));
            }
            var water = BundledPersonalStoryComposer.Compose(new PersonalStoryRequest("enkare-nairobi", "1850s", "place-water")).Story;
            Assert.That(water.Chapters[1].Narration, Is.EqualTo(PersonalStoryCatalog.NamingClaim));
            Assert.That(water.Chapters[1].Questions.Single().Answer, Is.EqualTo(PersonalStoryCatalog.NamingFact), "the review note is still there for a visitor who asks");
            var trade = BundledPersonalStoryComposer.Compose(new PersonalStoryRequest("enkare-nairobi", "1850s", "trade-household")).Story;
            Assert.That(trade.Chapters[1].Narration, Is.EqualTo(PersonalStoryCatalog.TradeFact), "the exchange is set in the documented trade, not in a museum vessel of another kind");
            Assert.That(trade.Chapters[1].Questions.Single().Answer, Is.EqualTo(PersonalStoryCatalog.VesselFact));
            Assert.That(trade.Sources.Select(s => s.Id), Is.EquivalentTo(new[] { "C6", "C7" }));
        }
    }
}
