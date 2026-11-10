using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Sankofa.Core.Json;
using Sankofa.Core.Personal;

namespace Sankofa.Tests
{
    public sealed class PersonalAnswerTests
    {
        const string Page = "journeys.context";
        static PersonalStoryDocument Story(string interest = "journeys") =>
            BundledPersonalStoryComposer.Compose(new PersonalStoryRequest("enkare-nairobi", "1850s", interest)).Story;
        static PersonalAnswer Generated(string text = "She waited because a message cannot sit beside someone at dusk.", string truth = "reconstructed", params string[] sources) =>
            new PersonalAnswer { Mode = "generated", Kind = "answer", Answer = text, Truth = truth, Sources = sources };
        static string[] Codes(PersonalAnswer answer, PersonalStoryDocument story = null, string page = Page) =>
            PersonalAnswerValidator.Validate(answer, story ?? Story(), page).Issues.Select(i => i.Code).ToArray();
        static string Words(int count) => string.Join(" ", Enumerable.Repeat("fire", count));

        [Test]
        public void AnImaginedAnswerIsAcceptedOnlyAsPlainBoundedTextWithoutSources()
        {
            Assert.That(Codes(Generated()), Is.Empty);
            Assert.That(Codes(Generated(Words(60))), Is.Empty, "sixty words is the limit, not past it");
            Assert.That(Codes(Generated("I cannot speak of your ancestors; the fire holds only this telling.")), Is.Empty, "an honest limit is not an assertion");
            var cases = new (string Code, PersonalAnswer Answer)[]
            {
                ("text.length", Generated(Words(61))),
                ("text.length", Generated(new string('a', 601))),
                ("text.required", Generated("  ")),
                ("text.required", Generated(null)),
                ("text.unsafe", Generated("<b>She waited.</b>")),
                ("text.unsafe", Generated("Read https://example.org for more.")),
                ("text.unsafe", Generated("She waited.\u0000")),
                ("text.unsafe", Generated("She waited ‮.")),
                ("reconstruction.sources", Generated(sources: "C6")),
                ("identity.inferred", Generated("Your ancestors lived beside this water.")),
                ("identity.inferred", Generated("You descend from the woman who waited.")),
                ("truth.invalid", Generated(truth: "verified")),
                ("truth.invalid", Generated(truth: "personal-account")),
                ("truth.invalid", Generated(truth: null)),
            };
            foreach (var (code, answer) in cases) Assert.That(Codes(answer), Does.Contain(code), code + ": " + answer.Answer);
        }

        [Test]
        public void AHistoricalLabelNeedsTheExactPassageAndItsOwnSourceFromThisTelling()
        {
            Assert.That(Codes(Generated(PersonalStoryCatalog.TradeFact, "historical", "C6")), Is.Empty);
            Assert.That(Codes(Generated(PersonalStoryCatalog.TradeFact + " It was a busy region.", "historical", "C6")), Does.Contain("history.unapproved"));
            Assert.That(Codes(Generated("Trade linked the communities around Kiambu.", "historical", "C6")), Does.Contain("history.unapproved"), "a paraphrase is not the passage");
            Assert.That(Codes(Generated(PersonalStoryCatalog.TradeFact, "historical")), Does.Contain("history.unapproved"), "a passage without its source");
            Assert.That(Codes(Generated(PersonalStoryCatalog.TradeFact, "historical", "C6", "C6")), Does.Contain("source.reference"));
            // The journeys telling declares only C6, so another passage cannot be cited beside it.
            Assert.That(Codes(Generated(PersonalStoryCatalog.NamingFact, "historical", "C1")), Does.Contain("source.reference"));
            Assert.That(Codes(Generated(PersonalStoryCatalog.NamingFact, "historical", "C1"), Story("place-water"), "place-water.name"), Is.Empty);
            Assert.That(Codes(Generated(PersonalStoryCatalog.TradeFact, "historical", "C7"), Story("trade-household"), "trade-household.vessel"), Does.Contain("history.unapproved"),
                "a declared source that belongs to another passage");
            Assert.That(Codes(Generated(sources: "C9")), Does.Contain("source.reference"));
        }

        [Test]
        public void AnUnknownLabelCarriesOnlyTheFixedReply()
        {
            Assert.That(Codes(Generated(PersonalAnswer.UnknownReply, "unknown")), Is.Empty);
            Assert.That(PersonalAnswerValidator.Words(PersonalAnswer.UnknownReply), Is.LessThanOrEqualTo(PersonalAnswerValidator.MaxWords));
            Assert.That(PersonalStoryValidator.IsPlainText(PersonalAnswer.UnknownReply), Is.True);
            Assert.That(Codes(Generated("The Zulu kingdom rose far to the south.", "unknown")), Does.Contain("unknown.reply"));
            Assert.That(Codes(Generated(PersonalAnswer.UnknownReply, "unknown", "C6")), Does.Contain("unknown.reply"));
        }

        [Test]
        public void ABundledAnswerIsOneOfThisPagesAuthoredAnswersUnchanged()
        {
            var story = Story();
            var authored = story.Chapters[1].Questions[0];
            PersonalAnswer Bundled(Action<PersonalAnswer> change = null)
            {
                var answer = new PersonalAnswer { Mode = "bundled", Kind = "answer", QuestionId = authored.Id, Answer = authored.Answer, Truth = authored.Truth, Sources = authored.Sources.ToArray() };
                change?.Invoke(answer);
                return answer;
            }
            Assert.That(Codes(Bundled(), story), Is.Empty);
            Assert.That(Codes(Bundled(a => a.Answer += " And more."), story), Does.Contain("answer.unauthored"));
            Assert.That(Codes(Bundled(a => a.Truth = "reconstructed"), story), Does.Contain("answer.unauthored"));
            Assert.That(Codes(Bundled(a => a.Sources = new string[0]), story), Does.Contain("answer.unauthored"));
            Assert.That(Codes(Bundled(a => a.QuestionId = null), story), Does.Contain("answer.unauthored"));
            Assert.That(Codes(Bundled(a => a.QuestionId = story.Chapters[0].Questions[0].Id), story), Does.Contain("answer.unauthored"), "a question from another page");
            Assert.That(Codes(Bundled(), story, "journeys.arrival"), Does.Contain("answer.unauthored"), "the same reply beside a different page");
            Assert.That(Codes(Bundled(a => a.Mode = "generated"), story), Does.Contain("answer.form"), "a generated reply never borrows an authored question's identity");
            // An authored fiction answer keeps its own disclosure and length; the sixty-word rule is for generated speech.
            var fiction = story.Chapters[0].Questions[0];
            Assert.That(Codes(new PersonalAnswer { Mode = "bundled", Kind = "answer", QuestionId = fiction.Id, Answer = fiction.Answer, Truth = fiction.Truth }, story, "journeys.arrival"), Is.Empty);
        }

        [Test]
        public void TheQuestionListOffersOnlyThisPagesAuthoredQuestions()
        {
            var story = Story();
            var authored = story.Chapters[1].Questions[0];
            PersonalAnswer List(params (string Id, string Label)[] options) => new PersonalAnswer
            { Mode = "bundled", Kind = "questions", Questions = options.Select(o => new PersonalAnswerOption { Id = o.Id, Label = o.Label }).ToArray() };
            Assert.That(Codes(List((authored.Id, authored.Label)), story), Is.Empty);
            Assert.That(Codes(List(), story), Is.Empty, "a page with nothing to offer is still a valid reply");
            Assert.That(List().IsQuestionList, Is.True);
            Assert.That(Codes(List((authored.Id, "Who was my ancestor?")), story), Does.Contain("question.unauthored"));
            Assert.That(Codes(List(("journeys.account", story.Chapters[0].Questions[0].Label)), story), Does.Contain("question.unauthored"), "a question from another page");
            Assert.That(Codes(List((authored.Id, authored.Label), (authored.Id, authored.Label)), story), Does.Contain("question.duplicate"));
            var generated = List((authored.Id, authored.Label)); generated.Mode = "generated";
            Assert.That(Codes(generated, story), Does.Contain("mode.mislabelled"));
            var mixed = List((authored.Id, authored.Label)); mixed.Answer = authored.Answer;
            Assert.That(Codes(mixed, story), Does.Contain("questions.form"));
        }

        [Test]
        public void AReplyNeedsItsTellingPageSchemaModeAndKind()
        {
            Assert.That(Codes(null), Is.EqualTo(new[] { "answer.missing" }));
            Assert.That(PersonalAnswerValidator.Validate(Generated(), null, Page).Issues.Single().Code, Is.EqualTo("answer.context"));
            Assert.That(Codes(Generated(), page: "journeys.missing"), Is.EqualTo(new[] { "answer.context" }));
            var schema = Generated(); schema.Schema = "sankofa.personal-story.v1";
            Assert.That(Codes(schema), Does.Contain("schema.unsupported"));
            var mode = Generated(); mode.Mode = "live";
            Assert.That(Codes(mode), Does.Contain("mode.invalid"));
            var kind = Generated(); kind.Kind = "command";
            Assert.That(Codes(kind), Does.Contain("kind.invalid"));
        }

        static string Reply(string body) => "{\"schema\":\"sankofa.personal-answer.v1\"," + body + "}";
        const string AnswerBody = "\"mode\":\"generated\",\"kind\":\"answer\",\"answer\":\"She waited because a message cannot sit beside someone at dusk.\",\"truth\":\"reconstructed\",\"sources\":[]";

        [Test]
        public void ReadingIsStrictBoundedAndFollowedByValidation()
        {
            var story = Story();
            var answer = PersonalAnswerJson.ReadValidated(Reply(AnswerBody), story, Page);
            Assert.That((answer.Mode, answer.Kind, answer.Truth, answer.QuestionId, answer.IsQuestionList), Is.EqualTo(("generated", "answer", "reconstructed", (string)null, false)));
            var authored = story.Chapters[1].Questions[0];
            var list = PersonalAnswerJson.ReadValidated(Reply("\"mode\":\"bundled\",\"kind\":\"questions\",\"questions\":[{\"id\":\"" + authored.Id + "\",\"label\":\"" + authored.Label + "\"}]"), story, Page);
            Assert.That(list.Questions.Select(q => q.Label), Is.EqualTo(new[] { authored.Label }));

            var four = string.Join(",", Enumerable.Repeat("{\"id\":\"a\",\"label\":\"b\"}", 4));
            var cases = new (string Name, string Text)[]
            {
                ("null", null), ("empty", ""), ("an array", "[]"),
                ("another schema", "{\"schema\":\"sankofa.personal-story.v1\"," + AnswerBody + "}"),
                ("no schema", "{" + AnswerBody + "}"),
                ("an unknown field", Reply(AnswerBody + ",\"scene\":\"turn-stage\"")),
                ("a scene command", Reply(AnswerBody + ",\"actions\":[\"flare\"]")),
                ("an unknown kind", Reply(AnswerBody.Replace("\"kind\":\"answer\"", "\"kind\":\"command\""))),
                ("a missing truth", Reply("\"mode\":\"generated\",\"kind\":\"answer\",\"answer\":\"She waited.\",\"sources\":[]")),
                ("missing sources", Reply("\"mode\":\"generated\",\"kind\":\"answer\",\"answer\":\"She waited.\",\"truth\":\"reconstructed\"")),
                ("an answer as a list", Reply("\"mode\":\"generated\",\"kind\":\"answer\",\"answer\":[\"She waited.\"],\"truth\":\"reconstructed\",\"sources\":[]")),
                ("a source as an object", Reply("\"mode\":\"generated\",\"kind\":\"answer\",\"answer\":\"She waited.\",\"truth\":\"reconstructed\",\"sources\":[{\"id\":\"C6\"}]")),
                ("too many sources", Reply("\"mode\":\"generated\",\"kind\":\"answer\",\"answer\":\"She waited.\",\"truth\":\"reconstructed\",\"sources\":[\"C1\",\"C6\",\"C7\",\"C1\"]")),
                ("a list that carries an answer", Reply("\"mode\":\"bundled\",\"kind\":\"questions\",\"questions\":[],\"answer\":\"She waited.\"")),
                ("an answer that carries a list", Reply(AnswerBody + ",\"questions\":[]")),
                ("too many questions", Reply("\"mode\":\"bundled\",\"kind\":\"questions\",\"questions\":[" + four + "]")),
                ("a question with an answer inside", Reply("\"mode\":\"bundled\",\"kind\":\"questions\",\"questions\":[{\"id\":\"a\",\"label\":\"b\",\"answer\":\"c\"}]")),
                ("deep nesting", Reply("\"mode\":\"bundled\",\"kind\":\"questions\",\"questions\":[[[]]]")),
                ("an oversized reply", Reply(AnswerBody + new string(' ', PersonalAnswerJson.MaxJsonLength))),
            };
            foreach (var (name, text) in cases) Assert.Throws<PersonalStoryFormatException>(() => PersonalAnswerJson.Parse(text), name);

            // Well-formed but unacceptable replies are refused by validation, with the reason.
            var claimed = Assert.Throws<PersonalStoryFormatException>(() => PersonalAnswerJson.ReadValidated(
                Reply("\"mode\":\"generated\",\"kind\":\"answer\",\"answer\":\"Everyone here kept cattle.\",\"truth\":\"historical\",\"sources\":[\"C6\"]"), story, Page));
            Assert.That(claimed.Message, Does.Contain("history.unapproved"));
        }

        [Test]
        public void WritingKeepsEveryFieldTheServiceIssued()
        {
            foreach (var body in new[]
            {
                AnswerBody,
                "\"mode\":\"bundled\",\"kind\":\"answer\",\"question_id\":\"journeys.context.more\",\"answer\":\"" + PersonalStoryCatalog.TradeFact + "\",\"truth\":\"historical\",\"sources\":[\"C6\"]",
                "\"mode\":\"bundled\",\"kind\":\"questions\",\"questions\":[{\"id\":\"journeys.context.more\",\"label\":\"What supports the regional context?\"}]",
                "\"mode\":\"bundled\",\"kind\":\"questions\",\"questions\":[]",
            })
            {
                var text = Reply(body);
                Assert.That(Canonical.Of(PersonalAnswerJson.Write(PersonalAnswerJson.Parse(text))), Is.EqualTo(Canonical.Of(text)));
            }
        }

        [Test]
        public void ATypedQuestionRequestCarriesTheTellingThePageAndTheQuestion()
        {
            var story = Story();
            var request = JsonText.ParseObject(PersonalAnswerJson.WriteRequest(story, "1700000000.abcdef", Page, "Why did she wait?"));
            Assert.That(request.Keys, Is.EquivalentTo(new[] { "story", "chapter_id", "token", "question" }));
            Assert.That(Canonical.Of(request["story"]), Is.EqualTo(Canonical.Of(PersonalStoryJson.Write(story))), "the telling is sent unchanged");
            Assert.That((request.GetString("chapter_id"), request.GetString("token"), request.GetString("question")), Is.EqualTo((Page, "1700000000.abcdef", "Why did she wait?")));
            foreach (var token in new[] { null, "" })
                Assert.That(JsonText.ParseObject(PersonalAnswerJson.WriteRequest(story, token, Page, "Why did she wait?")).Keys,
                    Is.EquivalentTo(new[] { "story", "chapter_id", "question" }), "a prepared telling is recognised by its content and needs no proof");

            Assert.Throws<ArgumentNullException>(() => PersonalAnswerJson.WriteRequest(null, null, Page, "Why?"));
            Assert.Throws<ArgumentException>(() => PersonalAnswerJson.WriteRequest(story, null, "journeys.missing", "Why?"));
            foreach (var question in new[] { null, "", "   ", new string('a', 501), "<b>Why?</b>", "see https://example.org", "why\u0000" })
                Assert.Throws<ArgumentException>(() => PersonalAnswerJson.WriteRequest(story, null, Page, question), question);
            Assert.DoesNotThrow(() => PersonalAnswerJson.WriteRequest(story, null, Page, new string('a', 500)));
        }

        [Test]
        public void ARecordedQuestionIsSixteenKilohertzMonoPcmOfAtMostTwelveSeconds()
        {
            var wav = PersonalAnswerAudio.EncodeWav(new[] { 0f, 1f, -1f, 0.5f, 2f, -2f, float.NaN }, 7);
            Assert.That(wav.Length, Is.EqualTo(44 + 14));
            Assert.That(System.Text.Encoding.ASCII.GetString(wav, 0, 4) + System.Text.Encoding.ASCII.GetString(wav, 8, 8) + System.Text.Encoding.ASCII.GetString(wav, 36, 4), Is.EqualTo("RIFFWAVEfmt data"));
            Assert.That((BitConverter.ToInt32(wav, 4), BitConverter.ToInt32(wav, 16), BitConverter.ToInt16(wav, 20), BitConverter.ToInt16(wav, 22)), Is.EqualTo((36 + 14, 16, (short)1, (short)1)));
            Assert.That((BitConverter.ToInt32(wav, 24), BitConverter.ToInt32(wav, 28), BitConverter.ToInt16(wav, 32), BitConverter.ToInt16(wav, 34), BitConverter.ToInt32(wav, 40)),
                Is.EqualTo((16000, 32000, (short)2, (short)16, 14)));
            Assert.That(Enumerable.Range(0, 7).Select(i => BitConverter.ToInt16(wav, 44 + i * 2)), Is.EqualTo(new short[] { 0, 32767, -32767, 16384, 32767, -32767, 0 }),
                "samples are clamped and a broken sample is silence");
            Assert.That(PersonalAnswerAudio.IsAccepted(wav), Is.True);
            Assert.That(PersonalAnswerAudio.EncodeWav(new float[5], 2).Length, Is.EqualTo(48), "only the recorded part of a buffer is written");

            var longest = PersonalAnswerAudio.EncodeWav(new float[PersonalAnswerAudio.MaxSamples], PersonalAnswerAudio.MaxSamples);
            Assert.That(PersonalAnswerAudio.IsAccepted(longest), Is.True, "exactly twelve seconds");
            Assert.Throws<ArgumentException>(() => PersonalAnswerAudio.EncodeWav(new float[PersonalAnswerAudio.MaxSamples + 1], PersonalAnswerAudio.MaxSamples + 1));
            Assert.Throws<ArgumentException>(() => PersonalAnswerAudio.EncodeWav(new float[4], 0));
            Assert.Throws<ArgumentException>(() => PersonalAnswerAudio.EncodeWav(new float[4], 5));
            Assert.Throws<ArgumentNullException>(() => PersonalAnswerAudio.EncodeWav(null, 1));

            byte[] Changed(Action<byte[]> change) { var copy = (byte[])wav.Clone(); change(copy); return copy; }
            void Write32(byte[] target, int at, int value) => BitConverter.GetBytes(value).CopyTo(target, at);
            var rejected = new (string Name, byte[] Wav)[]
            {
                ("null", null), ("empty", new byte[0]), ("a header alone", wav.Take(44).ToArray()),
                ("not RIFF", Changed(w => w[0] = (byte)'X')), ("not WAVE", Changed(w => w[8] = (byte)'X')),
                ("compressed", Changed(w => w[20] = 3)), ("stereo", Changed(w => w[22] = 2)),
                ("8 kHz", Changed(w => Write32(w, 24, 8000))), ("48 kHz", Changed(w => Write32(w, 24, 48000))),
                ("8-bit", Changed(w => w[34] = 8)), ("a wrong block size", Changed(w => w[32] = 4)),
                ("data longer than the file", Changed(w => Write32(w, 40, 16))),
                ("an odd number of bytes", Changed(w => Write32(w, 40, 13))),
                ("a negative length", Changed(w => Write32(w, 40, -2))),
                ("data before its format", wav.Take(12).Concat(wav.Skip(36)).Concat(wav.Skip(12).Take(24)).ToArray()),
                ("more than twelve seconds", TooLong()),
            };
            foreach (var (name, bytes) in rejected) Assert.That(PersonalAnswerAudio.IsAccepted(bytes), Is.False, name);

            // Recorders often add a LIST chunk; the format and data are still found.
            var note = new byte[] { (byte)'L', (byte)'I', (byte)'S', (byte)'T', 3, 0, 0, 0, 1, 2, 3, 0 };
            Assert.That(PersonalAnswerAudio.IsAccepted(wav.Take(36).Concat(note).Concat(wav.Skip(36)).ToArray()), Is.True);
        }

        static byte[] TooLong()
        {
            var longest = PersonalAnswerAudio.EncodeWav(new float[PersonalAnswerAudio.MaxSamples], PersonalAnswerAudio.MaxSamples);
            var wav = longest.Concat(new byte[2]).ToArray();
            BitConverter.GetBytes(PersonalAnswerAudio.MaxSamples * 2 + 2).CopyTo(wav, 40);
            return wav;
        }

        [Test]
        public void ARecordedQuestionRequestCarriesTheRecordingAndNoText()
        {
            var story = Story();
            var wav = PersonalAnswerAudio.EncodeWav(new[] { 0.25f, -0.25f }, 2);
            var request = JsonText.ParseObject(PersonalAnswerJson.WriteAudioRequest(story, null, Page, wav));
            Assert.That(request.Keys, Is.EquivalentTo(new[] { "story", "chapter_id", "audio" }));
            Assert.That(Convert.FromBase64String(request.GetString("audio")), Is.EqualTo(wav));
            Assert.Throws<ArgumentException>(() => PersonalAnswerJson.WriteAudioRequest(story, null, Page, new byte[] { 1, 2, 3 }));
            Assert.Throws<ArgumentException>(() => PersonalAnswerJson.WriteAudioRequest(story, null, Page, null));
            Assert.Throws<ArgumentException>(() => PersonalAnswerJson.WriteAudioRequest(story, null, "journeys.missing", wav));
        }

        [Test]
        public void OnlyAnIssuedAnswerWithItsProofCanBeSentForReading()
        {
            var answer = PersonalAnswerJson.Parse(Reply(AnswerBody));
            var request = JsonText.ParseObject(PersonalAnswerJson.WriteNarrationRequest(answer, "1700000000.abcdef"));
            Assert.That(request.Keys, Is.EquivalentTo(new[] { "answer", "token" }));
            Assert.That(Canonical.Of(request["answer"]), Is.EqualTo(Canonical.Of(Reply(AnswerBody))), "the answer is returned exactly as issued");
            Assert.Throws<ArgumentException>(() => PersonalAnswerJson.WriteNarrationRequest(answer, " "));
            Assert.Throws<ArgumentException>(() => PersonalAnswerJson.WriteNarrationRequest(answer, null));
            Assert.Throws<ArgumentNullException>(() => PersonalAnswerJson.WriteNarrationRequest(null, "1700000000.abcdef"));
            var list = PersonalAnswerJson.Parse(Reply("\"mode\":\"bundled\",\"kind\":\"questions\",\"questions\":[]"));
            Assert.Throws<ArgumentException>(() => PersonalAnswerJson.WriteNarrationRequest(list, "1700000000.abcdef"));
        }
    }

    /// <summary>Key-sorted compact JSON, so two documents compare by content the way the service's signatures do.</summary>
    static class Canonical
    {
        public static string Of(string json) => Of(JsonText.Parse(json));
        public static string Of(object value) => JsonText.Serialize(Sort(value), false);
        static object Sort(object value)
        {
            if (value is JsonObject item)
            {
                var sorted = new JsonObject();
                foreach (var key in item.Keys.OrderBy(k => k, StringComparer.Ordinal)) sorted.Add(key, Sort(item[key]));
                return sorted;
            }
            return value is List<object> list ? list.Select(Sort).ToList() : value;
        }
    }
}
