using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Sankofa.Core.Json;
using Sankofa.Core.Personal;

namespace Sankofa.Tests
{
    /// <summary>
    /// Keeps the app and the optional question service in step without running either inside the other. Files this
    /// suite owns are compared with what the app writes today; replies the service's own suite emitted are read with the
    /// app's reader. scripts/test_personal_service.py makes the matching checks from the other side.
    ///
    /// After a deliberate change to a prepared telling or to the request form, rewrite the app-owned files with
    ///     SANKOFA_UPDATE_FIXTURES=1 dotnet test dotnet/Sankofa.Tests --filter PersonalServiceContractTests
    /// and the service-owned replies with
    ///     python3 scripts/test_personal_service.py --emit-ask-fixtures dotnet/Sankofa.Tests/Fixtures/ask
    /// </summary>
    public sealed class PersonalServiceContractTests
    {
        const string AuthoredTellings = "backend/authored_tellings.json";
        const string Folder = "dotnet/Sankofa.Tests/Fixtures/ask/";
        const string Page = "journeys.context";
        public const string Question = "What supports the regional context?";

        static PersonalStoryDocument Story(string interest = "journeys") =>
            BundledPersonalStoryComposer.Compose(new PersonalStoryRequest(PersonalStoryCatalog.Region, PersonalStoryCatalog.Period, interest)).Story;

        /// <summary>A short fixed waveform built from exact binary fractions, so every platform writes the same bytes.</summary>
        static byte[] Recording()
        {
            var samples = Enumerable.Range(0, 400).Select(i => ((i * 37) % 200 - 100) / 128f).ToArray();
            return PersonalAnswerAudio.EncodeWav(samples, samples.Length);
        }

        static void MatchesAppOutput(string relative, string expected)
        {
            var path = Fixtures.RepositoryFile(relative);
            if (Environment.GetEnvironmentVariable("SANKOFA_UPDATE_FIXTURES") == "1")
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, expected);
            }
            Assert.That(File.Exists(path), Is.True, relative + " is missing; see this class's summary for how to write it");
            Assert.That(File.ReadAllText(path), Is.EqualTo(expected), relative + " no longer matches the app; see this class's summary for how to rewrite it");
        }

        static string Reply(string name) => File.ReadAllText(Fixtures.RepositoryFile(Folder + name));

        [Test]
        public void TheServiceHoldsThePreparedTellingsExactlyAsTheAppComposesThem()
        {
            var tellings = PersonalStoryCatalog.Interests.Select(i => JsonText.ParseObject(PersonalStoryJson.Write(Story(i)))).ToArray();
            MatchesAppOutput(AuthoredTellings, JsonText.Serialize(new JsonObject()
                .Add("schema", "sankofa.authored-tellings.v1")
                .Add("source", "BundledPersonalStoryComposer, written by PersonalServiceContractTests. Do not edit by hand.")
                .Add("tellings", tellings)));
        }

        [Test]
        public void TheRequestsTheAppWritesAreTheOnesTheServiceIsTestedWith()
        {
            MatchesAppOutput(Folder + "request-text.json", PersonalAnswerJson.WriteRequest(Story(), null, Page, Question) + "\n");
            MatchesAppOutput(Folder + "request-audio.json", PersonalAnswerJson.WriteAudioRequest(Story(), null, Page, Recording()) + "\n");
        }

        [TestCase("reply-generated.json", "generated", "answer", "reconstructed")]
        [TestCase("reply-historical.json", "generated", "answer", "historical")]
        [TestCase("reply-unknown.json", "generated", "answer", "unknown")]
        [TestCase("reply-bundled-answer.json", "bundled", "answer", "historical")]
        [TestCase("reply-bundled-questions.json", "bundled", "questions", null)]
        public void TheAppAcceptsEveryKindOfReplyTheServiceEmits(string name, string mode, string kind, string truth)
        {
            var text = Reply(name);
            var answer = PersonalAnswerJson.ReadValidated(text, Story(), Page);
            Assert.That((answer.Mode, answer.Kind, answer.Truth), Is.EqualTo((mode, kind, truth)));
            Assert.That(Canonical.Of(PersonalAnswerJson.Write(answer)), Is.EqualTo(Canonical.Of(text)), "written back unchanged, so a narration proof still matches");
            if (!answer.IsQuestionList)
                Assert.That(Canonical.Of(JsonText.ParseObject(PersonalAnswerJson.WriteNarrationRequest(answer, "1700000000.abcdef"))["answer"]), Is.EqualTo(Canonical.Of(text)));
        }

        [Test]
        public void TheEmittedRepliesCarryTheTextTheAppExpects()
        {
            Assert.That(PersonalAnswerJson.Parse(Reply("reply-unknown.json")).Answer, Is.EqualTo(PersonalAnswer.UnknownReply), "the fixed reply is the same sentence on both sides");
            var historical = PersonalAnswerJson.Parse(Reply("reply-historical.json"));
            Assert.That((historical.Answer, historical.Sources.Single()), Is.EqualTo((PersonalStoryCatalog.TradeFact, "C6")));
            var authored = Story().Chapters[1].Questions[0];
            var bundled = PersonalAnswerJson.Parse(Reply("reply-bundled-answer.json"));
            Assert.That((bundled.QuestionId, bundled.Answer), Is.EqualTo((authored.Id, authored.Answer)));
            Assert.That(PersonalAnswerJson.Parse(Reply("reply-bundled-questions.json")).Questions.Select(q => (q.Id, q.Label)), Is.EqualTo(new[] { (authored.Id, authored.Label) }));
        }

        [Test]
        public void AReplyForOneTellingIsNotAcceptedBesideAnother()
        {
            // The trade telling does not declare C6's page question here, and place-water does not declare C6 at all.
            Assert.Throws<PersonalStoryFormatException>(() => PersonalAnswerJson.ReadValidated(Reply("reply-bundled-answer.json"), Story("place-water"), "place-water.name"));
            Assert.Throws<PersonalStoryFormatException>(() => PersonalAnswerJson.ReadValidated(Reply("reply-historical.json"), Story("place-water"), "place-water.name"));
            Assert.Throws<PersonalStoryFormatException>(() => PersonalAnswerJson.ReadValidated(Reply("reply-bundled-questions.json"), Story(), "journeys.arrival"));
        }
    }
}
