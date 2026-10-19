using System;
using System.Collections.Generic;
using System.Linq;
using Sankofa.Core.Json;

namespace Sankofa.Core.Personal
{
    public sealed class PersonalStoryFormatException : FormatException
    {
        public PersonalStoryFormatException(string path, string message) : base(path + ": " + message) => Path = path;
        public string Path { get; }
    }

    /// <summary>Strict, bounded provider/persistence boundary. Parsing is followed by semantic validation before use.</summary>
    public static class PersonalStoryJson
    {
        public static PersonalStoryDocument Parse(string text)
        {
            Preflight(text);
            JsonObject root;
            try { root = JsonText.ParseObject(text); }
            catch (Exception e) when (e is FormatException || e is ArgumentException)
            { throw new PersonalStoryFormatException("$", "Invalid JSON: " + e.Message); }
            var f = new Fields(root, "$");
            f.Only("schema", "id", "title", "region", "period", "interest", "mode", "chapters", "sources");
            var schema = f.String("schema");
            if (schema != PersonalStoryDocument.CurrentSchema) throw new PersonalStoryFormatException("$.schema", "Unsupported personal-story schema.");
            return new PersonalStoryDocument
            {
                Schema = schema, Id = f.String("id"), Title = f.String("title"), Region = f.String("region"),
                Period = f.String("period"), Interest = f.String("interest"), Mode = f.String("mode"),
                Chapters = f.Objects("chapters", 6, Chapter), Sources = f.Objects("sources", 3, Source),
            };
        }

        public static PersonalStoryDocument ReadValidated(string text, PersonalStoryRequest originalRequest = null)
        {
            var story = Parse(text);
            var report = PersonalStoryValidator.Validate(story, originalRequest);
            if (!report.IsValid) throw new PersonalStoryFormatException(report.Issues[0].Path, report.ToString());
            return story;
        }

        public static string Write(PersonalStoryDocument story)
        {
            if (story == null) throw new ArgumentNullException(nameof(story));
            return JsonText.Serialize(new JsonObject()
                .Add("schema", story.Schema).Add("id", story.Id).Add("title", story.Title)
                .Add("region", story.Region).Add("period", story.Period).Add("interest", story.Interest).Add("mode", story.Mode)
                .Add("chapters", story.Chapters.Select(c => new JsonObject()
                    .Add("id", c.Id).Add("title", c.Title).Add("narration", c.Narration).Add("illustration", c.Illustration)
                    .Add("truth", c.Truth).Add("sources", c.Sources)
                    .Add("questions", c.Questions.Select(q => new JsonObject().Add("id", q.Id).Add("label", q.Label)
                        .Add("answer", q.Answer).Add("truth", q.Truth).Add("sources", q.Sources)))))
                .Add("sources", story.Sources.Select(s => new JsonObject().Add("id", s.Id).Add("title", s.Title).Add("url", s.Url))));
        }

        public static string WriteRequest(PersonalStoryRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            return JsonText.Serialize(new JsonObject().Add("region", request.Region).Add("period", request.Period ?? "")
                .Add("interest", request.Interest ?? "").Add("fragment", request.Fragment ?? "").Add("question", request.Question ?? ""));
        }

        static PersonalStoryChapter Chapter(Fields f)
        {
            f.Only("id", "title", "narration", "illustration", "truth", "sources", "questions");
            return new PersonalStoryChapter
            {
                Id = f.String("id"), Title = f.String("title"), Narration = f.String("narration"), Illustration = f.String("illustration"),
                Truth = f.String("truth"), Sources = f.Strings("sources", 3), Questions = f.Objects("questions", 3, Question),
            };
        }
        static PersonalStoryQuestion Question(Fields f)
        {
            f.Only("id", "label", "answer", "truth", "sources");
            return new PersonalStoryQuestion
            { Id = f.String("id"), Label = f.String("label"), Answer = f.String("answer"), Truth = f.String("truth"), Sources = f.Strings("sources", 3) };
        }
        static PersonalStorySource Source(Fields f)
        {
            f.Only("id", "title", "url");
            return new PersonalStorySource { Id = f.String("id"), Title = f.String("title"), Url = f.String("url") };
        }

        static void Preflight(string text)
        {
            if (text == null || text.Length == 0 || text.Length > PersonalStoryValidator.MaxJsonLength)
                throw new PersonalStoryFormatException("$", "JSON must be non-empty and within the size limit.");
            var depth = 0; var quoted = false; var escaped = false;
            foreach (var c in text)
            {
                if (quoted)
                {
                    if (escaped) escaped = false;
                    else if (c == '\\') escaped = true;
                    else if (c == '"') quoted = false;
                }
                else if (c == '"') quoted = true;
                else if (c == '{' || c == '[')
                {
                    if (++depth > 10) throw new PersonalStoryFormatException("$", "JSON exceeds the nesting limit.");
                }
                else if (c == '}' || c == ']') depth--;
            }
        }

        sealed class Fields
        {
            readonly JsonObject value;
            public string Path { get; }
            public Fields(JsonObject value, string path) { this.value = value; Path = path; }
            public void Only(params string[] fields)
            {
                foreach (var key in value.Keys)
                    if (!fields.Contains(key)) throw new PersonalStoryFormatException(Path + "." + key, "Unknown field.");
            }
            public string String(string key)
            {
                if (!value.TryGet(key, out var item) || !(item is string text)) throw new PersonalStoryFormatException(Path + "." + key, "Expected a string.");
                return text;
            }
            List<object> Array(string key, int maximum)
            {
                if (!value.TryGet(key, out var item) || !(item is List<object> array)) throw new PersonalStoryFormatException(Path + "." + key, "Expected an array.");
                if (array.Count > maximum) throw new PersonalStoryFormatException(Path + "." + key, "Too many items.");
                return array;
            }
            public IReadOnlyList<T> Objects<T>(string key, int maximum, Func<Fields, T> read)
            {
                var items = Array(key, maximum);
                var results = new List<T>();
                for (var i = 0; i < items.Count; i++)
                {
                    var path = Path + "." + key + "[" + i + "]";
                    if (!(items[i] is JsonObject obj)) throw new PersonalStoryFormatException(path, "Expected an object.");
                    results.Add(read(new Fields(obj, path)));
                }
                return results;
            }
            public IReadOnlyList<string> Strings(string key, int maximum)
            {
                var items = Array(key, maximum);
                var results = new List<string>();
                for (var i = 0; i < items.Count; i++)
                {
                    if (!(items[i] is string text)) throw new PersonalStoryFormatException(Path + "." + key + "[" + i + "]", "Expected a string.");
                    results.Add(text);
                }
                return results;
            }
        }
    }
}
