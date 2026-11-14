using System;
using System.Linq;
using Sankofa.Core.Json;

namespace Sankofa.Core.Personal
{
    /// <summary>
    /// The client side of the question service's <c>POST /ask</c> contract: strict reading of a reply, and the request
    /// bodies for asking and for voicing an issued answer. Nothing here performs a network call.
    /// </summary>
    public static class PersonalAnswerJson
    {
        public const int MaxJsonLength = 8000;

        public static PersonalAnswer Parse(string text)
        {
            var root = StrictJson.Root(text, MaxJsonLength, 3);
            var schema = root.String("schema");
            if (schema != PersonalAnswer.CurrentSchema) throw new PersonalStoryFormatException("$.schema", "Unsupported answer schema.");
            var answer = new PersonalAnswer { Schema = schema, Mode = root.String("mode"), Kind = root.String("kind") };
            if (answer.Kind == PersonalAnswer.QuestionsKind)
            {
                root.Only("schema", "mode", "kind", "questions");
                answer.Questions = root.Objects("questions", 3, item =>
                {
                    item.Only("id", "label");
                    return new PersonalAnswerOption { Id = item.String("id"), Label = item.String("label") };
                });
            }
            else if (answer.Kind == PersonalAnswer.AnswerKind)
            {
                root.Only("schema", "mode", "kind", "question_id", "answer", "truth", "sources");
                answer.QuestionId = root.Has("question_id") ? root.String("question_id") : null;
                answer.Answer = root.String("answer");
                answer.Truth = root.String("truth");
                answer.Sources = root.Strings("sources", 3);
            }
            else throw new PersonalStoryFormatException("$.kind", "A reply is an answer or a list of questions.");
            return answer;
        }

        /// <summary>Reads a reply and accepts it only against the telling and page the question was asked about.</summary>
        public static PersonalAnswer ReadValidated(string text, PersonalStoryDocument story, string chapterId)
        {
            var answer = Parse(text);
            var report = PersonalAnswerValidator.Validate(answer, story, chapterId);
            if (!report.IsValid) throw new PersonalStoryFormatException(report.Issues[0].Path, report.ToString());
            return answer;
        }

        /// <summary>The reply exactly as the service issued it, so its narration proof still matches.</summary>
        public static string Write(PersonalAnswer answer) => JsonText.Serialize(Object(answer));

        static JsonObject Object(PersonalAnswer answer)
        {
            if (answer == null) throw new ArgumentNullException(nameof(answer));
            var value = new JsonObject().Add("schema", answer.Schema).Add("mode", answer.Mode).Add("kind", answer.Kind);
            if (answer.IsQuestionList)
                return value.Add("questions", (answer.Questions ?? new PersonalAnswerOption[0]).Select(q => new JsonObject().Add("id", q.Id).Add("label", q.Label)));
            if (answer.QuestionId != null) value.Add("question_id", answer.QuestionId);
            return value.Add("answer", answer.Answer).Add("truth", answer.Truth).Add("sources", answer.Sources ?? new string[0]);
        }

        /// <summary>
        /// A typed question about the current page. <paramref name="token"/> is the story proof the service sent with a
        /// telling it created; pass null for a prepared telling, which the service recognises by its exact content.
        /// </summary>
        public static string WriteRequest(PersonalStoryDocument story, string token, string chapterId, string question)
        {
            if (string.IsNullOrWhiteSpace(question) || question.Length > PersonalStoryValidator.MaxQuestionLength || !PersonalStoryValidator.IsPlainText(question))
                throw new ArgumentException("A question is plain text of at most " + PersonalStoryValidator.MaxQuestionLength + " characters.", nameof(question));
            return JsonText.Serialize(Request(story, token, chapterId).Add("question", question), false);
        }

        /// <summary>A recorded question about the current page, in the format <see cref="PersonalAnswerAudio"/> describes.</summary>
        public static string WriteAudioRequest(PersonalStoryDocument story, string token, string chapterId, byte[] wav)
        {
            if (!PersonalAnswerAudio.IsAccepted(wav))
                throw new ArgumentException("A recorded question is a 16 kHz mono 16-bit PCM WAV of at most twelve seconds.", nameof(wav));
            return JsonText.Serialize(Request(story, token, chapterId).Add("audio", Convert.ToBase64String(wav)), false);
        }

        /// <summary>Asks the narration endpoint to read an answer this service issued, using the proof sent with that answer.</summary>
        public static string WriteNarrationRequest(PersonalAnswer answer, string token)
        {
            if (answer == null) throw new ArgumentNullException(nameof(answer));
            if (answer.IsQuestionList) throw new ArgumentException("Only an answer can be read aloud.", nameof(answer));
            if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("Reading an answer aloud needs the proof issued with it.", nameof(token));
            return JsonText.Serialize(new JsonObject().Add("answer", Object(answer)).Add("token", token), false);
        }

        static JsonObject Request(PersonalStoryDocument story, string token, string chapterId)
        {
            if (story == null) throw new ArgumentNullException(nameof(story));
            if (PersonalStoryRoutes.IndexOf(story, chapterId) < 0) throw new ArgumentException("The question must be asked beside a page of this telling.", nameof(chapterId));
            var request = new JsonObject().Add("story", JsonText.ParseObject(PersonalStoryJson.Write(story))).Add("chapter_id", chapterId);
            if (!string.IsNullOrEmpty(token)) request.Add("token", token);
            return request;
        }
    }
}
