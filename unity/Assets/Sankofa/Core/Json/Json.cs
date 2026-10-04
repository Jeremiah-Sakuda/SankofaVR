using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Sankofa.Core.Json
{
    /// <summary>Insertion-ordered JSON object so written files are stable and diffable.</summary>
    public sealed class JsonObject : IEnumerable<KeyValuePair<string, object>>
    {
        readonly List<string> keys = new List<string>();
        readonly Dictionary<string, object> values = new Dictionary<string, object>();

        public int Count => keys.Count;
        public IEnumerable<string> Keys => keys;

        public object this[string key]
        {
            get => values.TryGetValue(key, out var value) ? value : throw new KeyNotFoundException($"Missing JSON field '{key}'");
            set
            {
                if (!values.ContainsKey(key)) keys.Add(key);
                values[key] = value;
            }
        }

        /// <summary>Collection-initializer and fluent helper.</summary>
        public JsonObject Add(string key, object value)
        {
            this[key] = value;
            return this;
        }

        public bool Contains(string key) => values.ContainsKey(key);
        public bool TryGet(string key, out object value) => values.TryGetValue(key, out value);

        public string GetString(string key) => (string)this[key];
        public string GetStringOrNull(string key) => TryGet(key, out var v) ? (string)v : null;
        public double GetNumber(string key) => Convert.ToDouble(this[key], CultureInfo.InvariantCulture);
        public int GetInt(string key)
        {
            var d = GetNumber(key);
            if (d != Math.Floor(d) || d < int.MinValue || d > int.MaxValue)
                throw new FormatException($"JSON field '{key}' is not an integer");
            return (int)d;
        }
        public bool GetBool(string key) => (bool)this[key];
        public JsonObject GetObject(string key) => (JsonObject)this[key];
        public List<object> GetArray(string key) => (List<object>)this[key];

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            foreach (var key in keys) yield return new KeyValuePair<string, object>(key, values[key]);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>Small dependency-free JSON writer and reader for the engine-free core.</summary>
    public static class JsonText
    {
        public static string Serialize(object value, bool indented = true)
        {
            var sb = new StringBuilder();
            Write(sb, value, indented, 0);
            if (indented) sb.Append('\n');
            return sb.ToString();
        }

        public static object Parse(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            var reader = new Reader(text);
            var value = reader.ReadValue();
            reader.SkipWhitespace();
            if (!reader.AtEnd) throw reader.Error("Unexpected trailing content");
            return value;
        }

        public static JsonObject ParseObject(string text) =>
            Parse(text) as JsonObject ?? throw new FormatException("Expected a JSON object");

        public static string FormatNumber(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentException("JSON cannot represent NaN or infinity");
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        static void Write(StringBuilder sb, object value, bool indented, int depth)
        {
            switch (value)
            {
                case null: sb.Append("null"); break;
                case string s: WriteString(sb, s); break;
                case bool b: sb.Append(b ? "true" : "false"); break;
                case double d: sb.Append(FormatNumber(d)); break;
                case float f: sb.Append(FormatNumber(f)); break;
                case int i: sb.Append(i.ToString(CultureInfo.InvariantCulture)); break;
                case long l: sb.Append(l.ToString(CultureInfo.InvariantCulture)); break;
                case JsonObject o: WriteObject(sb, o, indented, depth); break;
                case IEnumerable e: WriteArray(sb, e, indented, depth); break;
                default:
                    if (value.GetType().IsEnum)
                    {
                        WriteString(sb, value.ToString());
                        break;
                    }
                    throw new ArgumentException($"Unsupported JSON value type {value.GetType().Name}");
            }
        }

        static void WriteObject(StringBuilder sb, JsonObject o, bool indented, int depth)
        {
            if (o.Count == 0)
            {
                sb.Append("{}");
                return;
            }
            sb.Append('{');
            var first = true;
            foreach (var pair in o)
            {
                if (!first) sb.Append(',');
                first = false;
                NewLine(sb, indented, depth + 1);
                WriteString(sb, pair.Key);
                sb.Append(indented ? ": " : ":");
                Write(sb, pair.Value, indented, depth + 1);
            }
            NewLine(sb, indented, depth);
            sb.Append('}');
        }

        static void WriteArray(StringBuilder sb, IEnumerable items, bool indented, int depth)
        {
            var list = new List<object>();
            foreach (var item in items) list.Add(item);
            if (list.Count == 0)
            {
                sb.Append("[]");
                return;
            }
            // Keep arrays of plain numbers on one line for readable event plans.
            var compact = indented && list.TrueForAll(x => x is double || x is float || x is int || x is long);
            sb.Append('[');
            for (var i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(compact ? ", " : ",");
                if (!compact) NewLine(sb, indented, depth + 1);
                Write(sb, list[i], indented, depth + 1);
            }
            if (!compact) NewLine(sb, indented, depth);
            sb.Append(']');
        }

        static void NewLine(StringBuilder sb, bool indented, int depth)
        {
            if (!indented) return;
            sb.Append('\n');
            sb.Append(' ', depth * 2);
        }

        static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (var c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        sealed class Reader
        {
            readonly string text;
            int pos;

            public Reader(string text) => this.text = text;
            public bool AtEnd => pos >= text.Length;

            public FormatException Error(string message) => new FormatException($"{message} at character {pos}");

            public void SkipWhitespace()
            {
                while (pos < text.Length && char.IsWhiteSpace(text[pos])) pos++;
            }

            public object ReadValue()
            {
                SkipWhitespace();
                if (AtEnd) throw Error("Unexpected end of JSON");
                var c = text[pos];
                switch (c)
                {
                    case '{': return ReadObject();
                    case '[': return ReadArray();
                    case '"': return ReadString();
                    case 't': Expect("true"); return true;
                    case 'f': Expect("false"); return false;
                    case 'n': Expect("null"); return null;
                    default:
                        if (c == '-' || (c >= '0' && c <= '9')) return ReadNumber();
                        throw Error($"Unexpected character '{c}'");
                }
            }

            void Expect(string literal)
            {
                if (string.CompareOrdinal(text, pos, literal, 0, literal.Length) != 0) throw Error($"Expected {literal}");
                pos += literal.Length;
            }

            JsonObject ReadObject()
            {
                var o = new JsonObject();
                pos++;
                SkipWhitespace();
                if (!AtEnd && text[pos] == '}')
                {
                    pos++;
                    return o;
                }
                while (true)
                {
                    SkipWhitespace();
                    if (AtEnd || text[pos] != '"') throw Error("Expected property name");
                    var key = ReadString();
                    if (o.Contains(key)) throw Error($"Duplicate property '{key}'");
                    SkipWhitespace();
                    if (AtEnd || text[pos] != ':') throw Error("Expected ':'");
                    pos++;
                    o[key] = ReadValue();
                    SkipWhitespace();
                    if (AtEnd) throw Error("Unterminated object");
                    if (text[pos] == ',') { pos++; continue; }
                    if (text[pos] == '}') { pos++; return o; }
                    throw Error("Expected ',' or '}'");
                }
            }

            List<object> ReadArray()
            {
                var list = new List<object>();
                pos++;
                SkipWhitespace();
                if (!AtEnd && text[pos] == ']')
                {
                    pos++;
                    return list;
                }
                while (true)
                {
                    list.Add(ReadValue());
                    SkipWhitespace();
                    if (AtEnd) throw Error("Unterminated array");
                    if (text[pos] == ',') { pos++; continue; }
                    if (text[pos] == ']') { pos++; return list; }
                    throw Error("Expected ',' or ']'");
                }
            }

            string ReadString()
            {
                var sb = new StringBuilder();
                pos++;
                while (true)
                {
                    if (AtEnd) throw Error("Unterminated string");
                    var c = text[pos++];
                    if (c == '"') return sb.ToString();
                    if (c != '\\')
                    {
                        if (c < 0x20) throw Error("Control character in string");
                        sb.Append(c);
                        continue;
                    }
                    if (AtEnd) throw Error("Unterminated escape");
                    var e = text[pos++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (pos + 4 > text.Length) throw Error("Short unicode escape");
                            sb.Append((char)int.Parse(text.Substring(pos, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            pos += 4;
                            break;
                        default: throw Error($"Invalid escape '\\{e}'");
                    }
                }
            }

            double ReadNumber()
            {
                var start = pos;
                if (text[pos] == '-') pos++;
                while (pos < text.Length && "0123456789.eE+-".IndexOf(text[pos]) >= 0) pos++;
                var token = text.Substring(start, pos - start);
                if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                    double.IsInfinity(value))
                    throw Error($"Invalid number '{token}'");
                return value;
            }
        }
    }
}
