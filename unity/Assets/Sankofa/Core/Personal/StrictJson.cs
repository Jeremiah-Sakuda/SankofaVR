using System;
using System.Collections.Generic;
using System.Linq;
using Sankofa.Core.Json;

namespace Sankofa.Core.Personal
{
    /// <summary>
    /// Bounded, strict reading for the kept strand and the answer contract: a size and nesting limit before parsing,
    /// then named fields only. Unknown fields, wrong types and oversized lists are format errors, never ignored.
    /// </summary>
    internal sealed class StrictJson
    {
        readonly JsonObject value;
        public string Path { get; }
        StrictJson(JsonObject value, string path) { this.value = value; Path = path; }

        public static StrictJson Root(string text, int maximumLength, int maximumDepth)
        {
            if (string.IsNullOrEmpty(text) || text.Length > maximumLength)
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
                    if (++depth > maximumDepth) throw new PersonalStoryFormatException("$", "JSON exceeds the nesting limit.");
                }
                else if (c == '}' || c == ']') depth--;
            }
            try { return new StrictJson(JsonText.ParseObject(text), "$"); }
            catch (Exception e) when (e is FormatException || e is ArgumentException)
            { throw new PersonalStoryFormatException("$", "Invalid JSON: " + e.Message); }
        }

        public void Only(params string[] fields)
        {
            foreach (var key in value.Keys)
                if (!fields.Contains(key)) throw new PersonalStoryFormatException(Path + "." + key, "Unknown field.");
        }

        public bool Has(string key) => value.TryGet(key, out _);

        public string String(string key)
        {
            if (!value.TryGet(key, out var item) || !(item is string text)) throw new PersonalStoryFormatException(Path + "." + key, "Expected a string.");
            return text;
        }

        public int Integer(string key)
        {
            if (!value.TryGet(key, out var item) || !(item is double number) || number != Math.Floor(number) || Math.Abs(number) > 1000000)
                throw new PersonalStoryFormatException(Path + "." + key, "Expected a whole number.");
            return (int)number;
        }

        List<object> Array(string key, int maximum)
        {
            if (!value.TryGet(key, out var item) || !(item is List<object> array)) throw new PersonalStoryFormatException(Path + "." + key, "Expected an array.");
            if (array.Count > maximum) throw new PersonalStoryFormatException(Path + "." + key, "Too many items.");
            return array;
        }

        public IReadOnlyList<T> Objects<T>(string key, int maximum, Func<StrictJson, T> read)
        {
            var items = Array(key, maximum);
            var results = new List<T>();
            for (var i = 0; i < items.Count; i++)
            {
                var path = Path + "." + key + "[" + i + "]";
                if (!(items[i] is JsonObject item)) throw new PersonalStoryFormatException(path, "Expected an object.");
                results.Add(read(new StrictJson(item, path)));
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
