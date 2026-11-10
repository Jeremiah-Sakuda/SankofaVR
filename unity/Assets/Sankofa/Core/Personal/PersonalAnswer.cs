using System;
using System.Collections.Generic;
using System.Linq;

namespace Sankofa.Core.Personal
{
    /// <summary>
    /// A reply from the optional question service to one question asked beside the current page. It is either one
    /// answer with its evidence label, or the page's own authored questions offered back when no answer was produced.
    /// A reply is presentation only until <see cref="PersonalAnswerValidator"/> accepts it against the telling in progress.
    /// </summary>
    public sealed class PersonalAnswer
    {
        public const string CurrentSchema = "sankofa.personal-answer.v1";
        public const string Generated = "generated";
        public const string Bundled = "bundled";
        public const string AnswerKind = "answer";
        public const string QuestionsKind = "questions";
        public const string Historical = "historical";
        public const string Reconstructed = "reconstructed";
        public const string Unknown = "unknown";
        /// <summary>The fire's fixed reply to a question outside this place and period. The service sends exactly this text.</summary>
        public const string UnknownReply = "The fire does not know that story. Tonight it keeps only the tellings of Enkare Nairobi, before the railway. Ask me about this one.";

        public string Schema { get; set; } = CurrentSchema;
        /// <summary>"generated" when the configured provider handled the question; "bundled" when the page's authored questions stood in.</summary>
        public string Mode { get; set; }
        /// <summary>"answer" or "questions".</summary>
        public string Kind { get; set; }
        /// <summary>For a bundled answer, the authored question on the current page that it belongs to; otherwise null.</summary>
        public string QuestionId { get; set; }
        public string Answer { get; set; }
        /// <summary>"historical", "reconstructed" or "unknown". Show it beside the answer: the spoken text carries no label of its own.</summary>
        public string Truth { get; set; }
        public IReadOnlyList<string> Sources { get; set; } = new string[0];
        /// <summary>For the questions form, the current page's authored questions to offer, by ID and label.</summary>
        public IReadOnlyList<PersonalAnswerOption> Questions { get; set; } = new PersonalAnswerOption[0];

        public bool IsQuestionList => Kind == QuestionsKind;
    }

    public sealed class PersonalAnswerOption
    {
        public string Id { get; set; }
        public string Label { get; set; }
    }

    /// <summary>
    /// Client-side acceptance of a reply. The service has already decided each label from content; this repeats the
    /// checks the application can make itself, so a wrong or tampered reply is never spoken or shown.
    /// </summary>
    public static class PersonalAnswerValidator
    {
        public const int MaxWords = 60;
        public const int MaxAnswerLength = 600;
        const int MaxAuthoredAnswerLength = 1200;
        // The same phrases the service refuses. They are assertions, so an honest "I cannot speak of your ancestors" still passes.
        static readonly string[] IdentityAssertions =
        {
            "your ancestor was", "your ancestors were", "your ancestors lived", "your ancestors came", "your family lived", "your family came from",
            "your grandmother was", "your grandfather was", "you descend from", "you are descended from", "your lineage is", "your bloodline",
            "your tribe", "this proves your", "historically verified", "verified by ai",
        };

        /// <param name="answer">The parsed reply.</param>
        /// <param name="story">The accepted telling the question was asked about.</param>
        /// <param name="chapterId">The page that was open when the question was asked.</param>
        public static PersonalStoryValidation Validate(PersonalAnswer answer, PersonalStoryDocument story, string chapterId)
        {
            var report = new PersonalStoryValidation();
            if (answer == null) { report.Add("answer.missing", "$", "A reply is required."); return report; }
            var index = PersonalStoryRoutes.IndexOf(story, chapterId);
            if (index < 0) { report.Add("answer.context", "$", "A reply needs the telling and page it was asked about."); return report; }
            var asked = story.Chapters[index].Questions ?? new PersonalStoryQuestion[0];
            if (answer.Schema != PersonalAnswer.CurrentSchema) report.Add("schema.unsupported", "$.schema", "Unsupported answer schema.");
            if (answer.Mode != PersonalAnswer.Generated && answer.Mode != PersonalAnswer.Bundled) report.Add("mode.invalid", "$.mode", "Mode must be generated or bundled.");
            if (answer.Kind == PersonalAnswer.QuestionsKind) Offered(report, answer, asked);
            else if (answer.Kind == PersonalAnswer.AnswerKind) Answered(report, answer, story, asked);
            else report.Add("kind.invalid", "$.kind", "A reply is an answer or a list of questions.");
            return report;
        }

        static void Offered(PersonalStoryValidation report, PersonalAnswer answer, IReadOnlyList<PersonalStoryQuestion> asked)
        {
            if (answer.Mode == PersonalAnswer.Generated) report.Add("mode.mislabelled", "$.mode", "A list of authored questions is never a generated reply.");
            if (answer.Answer != null || answer.Truth != null || answer.QuestionId != null || (answer.Sources != null && answer.Sources.Count > 0))
                report.Add("questions.form", "$", "A list of questions carries no answer.");
            if (answer.Questions == null || answer.Questions.Count > 3) { report.Add("questions.count", "$.questions", "At most three authored questions can be offered."); return; }
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < answer.Questions.Count; i++)
            {
                var option = answer.Questions[i];
                var path = "$.questions[" + i + "]";
                if (option == null || !asked.Any(q => q != null && q.Id == option.Id && q.Label == option.Label))
                    report.Add("question.unauthored", path, "Only this page's own authored questions can be offered.");
                else if (!seen.Add(option.Id)) report.Add("question.duplicate", path, "Each authored question is offered once.");
            }
        }

        static void Answered(PersonalStoryValidation report, PersonalAnswer answer, PersonalStoryDocument story, IReadOnlyList<PersonalStoryQuestion> asked)
        {
            if (answer.Questions != null && answer.Questions.Count > 0) report.Add("answer.form", "$.questions", "An answer carries no list of questions.");
            var text = answer.Answer;
            if (string.IsNullOrWhiteSpace(text)) report.Add("text.required", "$.answer", "Non-empty text is required.");
            else
            {
                if (text.Length > MaxAuthoredAnswerLength) report.Add("text.length", "$.answer", "Text exceeds the allowed length.");
                if (!PersonalStoryValidator.IsPlainText(text)) report.Add("text.unsafe", "$.answer", "Use plain text without markup, control characters or embedded URLs.");
            }
            var cited = answer.Sources ?? new string[0];
            var declared = (story.Sources ?? new PersonalStorySource[0]).Where(s => s != null).Select(s => s.Id).ToArray();
            if (cited.Count > 3 || cited.Any(id => id == null || !declared.Contains(id)) || cited.Distinct().Count() != cited.Count)
                report.Add("source.reference", "$.sources", "Every source must be one of this telling's own sources, cited once.");
            if (answer.Truth != PersonalAnswer.Historical && answer.Truth != PersonalAnswer.Reconstructed && answer.Truth != PersonalAnswer.Unknown)
            { report.Add("truth.invalid", "$.truth", "Unknown evidence state."); return; }

            if (answer.Mode == PersonalAnswer.Bundled)
            {
                var authored = asked.FirstOrDefault(q => q != null && q.Id == answer.QuestionId);
                if (authored == null || authored.Answer != text || authored.Truth != answer.Truth || !(authored.Sources ?? new string[0]).SequenceEqual(cited))
                    report.Add("answer.unauthored", "$", "A bundled reply must be one of this page's authored answers, unchanged.");
                return;
            }
            if (answer.QuestionId != null) report.Add("answer.form", "$.question_id", "Only a bundled reply names an authored question.");
            if (text == null) return;
            if (text.Length > MaxAnswerLength || Words(text) > MaxWords)
                report.Add("text.length", "$.answer", "A spoken answer is at most sixty words.");
            switch (answer.Truth)
            {
                case PersonalAnswer.Historical:
                    if (!PersonalStoryCatalog.IsApprovedHistory(text, cited))
                        report.Add("history.unapproved", "$", "A historical label requires exact reviewed text and its claim-specific source; a citation alone is not verification.");
                    break;
                case PersonalAnswer.Reconstructed:
                    if (cited.Count != 0) report.Add("reconstruction.sources", "$.sources", "An imagined answer cannot cite a source.");
                    if (IdentityAssertions.Any(s => text.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0))
                        report.Add("identity.inferred", "$.answer", "An answer cannot assert the visitor's biography or ancestry.");
                    break;
                default:
                    if (cited.Count != 0 || text != PersonalAnswer.UnknownReply)
                        report.Add("unknown.reply", "$", "A question outside the supported context receives the fixed reply and no sources.");
                    break;
            }
        }

        public static int Words(string text) =>
            string.IsNullOrEmpty(text) ? 0 : text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    /// <summary>
    /// The recorded-question format the service accepts: 16 kHz, mono, 16-bit PCM WAV of at most twelve seconds.
    /// Recording, permission and the decision to send belong to the application; nothing here touches a microphone.
    /// </summary>
    public static class PersonalAnswerAudio
    {
        public const int SampleRate = 16000;
        public const int MaxSeconds = 12;
        public const int MaxSamples = SampleRate * MaxSeconds;
        const int HeaderLength = 44;
        const int MaxWavLength = MaxSamples * 2 + 256;

        /// <summary>Writes the first <paramref name="count"/> mono samples, each from -1 to 1 at 16 kHz, as a WAV file.</summary>
        public static byte[] EncodeWav(float[] samples, int count)
        {
            if (samples == null) throw new ArgumentNullException(nameof(samples));
            if (count < 1 || count > samples.Length || count > MaxSamples)
                throw new ArgumentException("A recorded question is between one sample and twelve seconds at 16 kHz.", nameof(count));
            var data = count * 2;
            var wav = new byte[HeaderLength + data];
            Ascii(wav, 0, "RIFF"); Int32(wav, 4, 36 + data); Ascii(wav, 8, "WAVE");
            Ascii(wav, 12, "fmt "); Int32(wav, 16, 16); Int16(wav, 20, 1); Int16(wav, 22, 1);
            Int32(wav, 24, SampleRate); Int32(wav, 28, SampleRate * 2); Int16(wav, 32, 2); Int16(wav, 34, 16);
            Ascii(wav, 36, "data"); Int32(wav, 40, data);
            for (var i = 0; i < count; i++)
            {
                var sample = samples[i];
                // A broken capture must not become full-scale noise.
                var value = float.IsNaN(sample) ? 0 : (int)Math.Round(Math.Max(-1f, Math.Min(1f, sample)) * 32767f);
                Int16(wav, HeaderLength + i * 2, value);
            }
            return wav;
        }

        /// <summary>The same checks the service makes, so an unusable recording is never sent.</summary>
        public static bool IsAccepted(byte[] wav)
        {
            if (wav == null || wav.Length < HeaderLength || wav.Length > MaxWavLength || !Is(wav, 0, "RIFF") || !Is(wav, 8, "WAVE")) return false;
            var position = 12; var described = false;
            while (position + 8 <= wav.Length)
            {
                var size = ReadInt32(wav, position + 4);
                var body = position + 8;
                if (size < 0 || size > wav.Length - body) return false;
                if (Is(wav, position, "fmt "))
                {
                    if (size < 16 || ReadInt16(wav, body) != 1 || ReadInt16(wav, body + 2) != 1 || ReadInt32(wav, body + 4) != SampleRate ||
                        ReadInt16(wav, body + 12) != 2 || ReadInt16(wav, body + 14) != 16) return false;
                    described = true;
                }
                else if (Is(wav, position, "data"))
                    return described && size >= 2 && size % 2 == 0 && size <= MaxSamples * 2;
                position = body + size + (size & 1);
            }
            return false;
        }

        static void Ascii(byte[] target, int at, string text) { for (var i = 0; i < text.Length; i++) target[at + i] = (byte)text[i]; }
        static void Int16(byte[] target, int at, int value) { target[at] = (byte)value; target[at + 1] = (byte)(value >> 8); }
        static void Int32(byte[] target, int at, int value) { Int16(target, at, value); Int16(target, at + 2, value >> 16); }
        static bool Is(byte[] source, int at, string text)
        {
            if (at + text.Length > source.Length) return false;
            for (var i = 0; i < text.Length; i++) if (source[at + i] != (byte)text[i]) return false;
            return true;
        }
        static int ReadInt16(byte[] source, int at) => source[at] | (source[at + 1] << 8);
        static int ReadInt32(byte[] source, int at) => ReadInt16(source, at) | (ReadInt16(source, at + 2) << 16);
    }
}
