using System;
using System.Collections.Generic;
using Sankofa.Core.Json;

namespace Sankofa.Core.Story
{
    /// <summary>A content or plan file that cannot be read. Semantic problems are reported by the validators.</summary>
    public sealed class StoryFormatException : Exception
    {
        public StoryFormatException(string path, string message) : base(path + ": " + message) => Path = path;
        public string Path { get; }
    }

    /// <summary>
    /// Reads story packages and realised plans (schema 2). Unknown fields are rejected so typos cannot silently drop
    /// content; <c>kind</c> distinguishes a package from a plan so one is never mistaken for the other.
    /// </summary>
    public static class StoryReader
    {
        public const int SupportedSchema = 2;

        public static StoryPackage ReadPackage(string json) => ReadPackage(Parse(json));

        public static StoryPackage ReadPackage(JsonObject root)
        {
            var r = Root(root, "package");
            r.Only("schema", "kind", "id", "version", "title", "locale", "status", "note", "scenes", "objects", "claims",
                "welcome", "welcome_back", "choose", "routes", "beats", "early_return");
            return new StoryPackage
            {
                Schema = SupportedSchema,
                Id = r.String("id"),
                Version = r.String("version"),
                Title = r.String("title"),
                Locale = r.String("locale"),
                Status = Parse(r.String("status"), "$.status", ContentStatuses),
                Note = r.OptionalString("note"),
                Scenes = r.Objects("scenes", ReadScene),
                Objects = r.Objects("objects", ReadObject),
                Claims = r.OptionalObjects("claims", ReadClaim),
                Welcome = r.Objects("welcome", f => ReadLine(f, false)),
                WelcomeBack = r.OptionalObjects("welcome_back", f => ReadLine(f, false)),
                Choose = r.OptionalObjects("choose", f => ReadLine(f, false)),
                Routes = r.Objects("routes", ReadRoute),
                Beats = r.Objects("beats", f => ReadBeat(f, true)),
                EarlyReturn = r.OptionalObjects("early_return", f => ReadLine(f, false)),
            };
        }

        public static StoryPlan ReadPlan(string json) => ReadPlan(Parse(json));

        public static StoryPlan ReadPlan(JsonObject root)
        {
            var r = Root(root, "plan");
            r.Only("schema", "kind", "package", "version", "route", "label", "parent", "next", "title", "locale", "status", "note",
                "scenes", "objects", "claims", "beats", "early_return", "keepsake");
            return new StoryPlan
            {
                Schema = SupportedSchema,
                Package = r.String("package"),
                Version = r.String("version"),
                Route = r.String("route"),
                Label = r.String("label"),
                Parent = r.OptionalString("parent"),
                Next = r.OptionalString("next"),
                Title = r.String("title"),
                Locale = r.String("locale"),
                Status = Parse(r.String("status"), "$.status", ContentStatuses),
                Note = r.OptionalString("note"),
                Scenes = r.Objects("scenes", ReadScene),
                Objects = r.Objects("objects", ReadObject),
                Claims = r.OptionalObjects("claims", ReadClaim),
                Beats = r.Objects("beats", f => ReadBeat(f, false)),
                EarlyReturn = r.OptionalObjects("early_return", f => ReadLine(f, false)),
                Keepsake = r.OptionalString("keepsake"),
            };
        }

        static JsonObject Parse(string json)
        {
            try
            {
                return JsonText.ParseObject(json);
            }
            catch (FormatException e)
            {
                throw new StoryFormatException("$", e.Message);
            }
        }

        static Fields Root(JsonObject root, string kind)
        {
            var r = new Fields(root, "$");
            var schema = r.Int("schema");
            if (schema != SupportedSchema) throw new StoryFormatException("$.schema", $"unsupported schema {schema}; expected {SupportedSchema}");
            var actual = r.String("kind");
            if (actual != kind) throw new StoryFormatException("$.kind", $"expected a {kind}, got '{actual}'");
            return r;
        }

        static SceneDef ReadScene(Fields f)
        {
            f.Only("id", "kind");
            return new SceneDef { Id = f.String("id"), Kind = Parse(f.String("kind"), f.Path + ".kind", SceneKinds) };
        }

        static StoryObject ReadObject(Fields f)
        {
            f.Only("id", "scene", "capabilities", "continues");
            var capabilities = new List<ActionKind>();
            var raw = f.Strings("capabilities");
            for (var i = 0; i < raw.Count; i++) capabilities.Add(Parse(raw[i], $"{f.Path}.capabilities[{i}]", ActionKinds));
            return new StoryObject
            {
                Id = f.String("id"),
                Scene = f.String("scene"),
                Capabilities = capabilities,
                Continues = f.OptionalString("continues"),
            };
        }

        static Claim ReadClaim(Fields f)
        {
            f.Only("id", "state", "text", "sources");
            return new Claim
            {
                Id = f.String("id"),
                State = Parse(f.String("state"), f.Path + ".state", EvidenceStates),
                Text = f.String("text"),
                Sources = f.OptionalStrings("sources"),
            };
        }

        static Route ReadRoute(Fields f)
        {
            f.Only("id", "label", "follows", "beats", "keepsake", "next");
            return new Route
            {
                Id = f.String("id"),
                Label = f.String("label"),
                Follows = f.OptionalString("follows"),
                Beats = f.Strings("beats"),
                Keepsake = f.OptionalString("keepsake"),
                Next = f.OptionalString("next"),
            };
        }

        static Beat ReadBeat(Fields f, bool package)
        {
            f.Only("id", "scene", "lines", "invitation", "questions");
            return new Beat
            {
                Id = f.String("id"),
                Scene = f.String("scene"),
                Lines = f.Objects("lines", l => ReadLine(l, package)),
                Invitation = f.OptionalObject("invitation", ReadInvitation),
                Questions = f.OptionalObjects("questions", ReadQuestion),
            };
        }

        static Invitation ReadInvitation(Fields f)
        {
            f.Only("id", "kind", "object", "alternative", "wait_seconds", "accepted", "default");
            return new Invitation
            {
                Id = f.String("id"),
                Kind = Parse(f.String("kind"), f.Path + ".kind", InvitationKinds),
                Object = f.String("object"),
                Alternative = f.String("alternative"),
                WaitSeconds = f.Number("wait_seconds"),
                Accepted = f.Objects("accepted", l => ReadLine(l, false)),
                Default = f.Objects("default", l => ReadLine(l, false)),
            };
        }

        static Question ReadQuestion(Fields f)
        {
            f.Only("id", "label", "about", "lines");
            return new Question
            {
                Id = f.String("id"),
                Label = f.String("label"),
                About = f.OptionalString("about"),
                Lines = f.Objects("lines", l => ReadLine(l, false)),
            };
        }

        /// <summary>Recall conditions exist only in packages; the composer resolves them before a plan exists.</summary>
        static Line ReadLine(Fields f, bool allowRecall)
        {
            if (allowRecall) f.Only("id", "text", "audio", "seconds", "pause_after", "fire", "when", "recall", "actions", "claims");
            else f.Only("id", "text", "audio", "seconds", "pause_after", "fire", "when", "actions", "claims");
            var actions = f.OptionalObjects("actions", a =>
            {
                a.Only("do", "object", "at");
                return new StoryAction(Parse(a.String("do"), a.Path + ".do", ActionKinds), a.String("object"), a.OptionalString("at"));
            });
            var fire = f.OptionalString("fire");
            var when = f.OptionalString("when");
            var recall = allowRecall ? f.OptionalString("recall") : null;
            return new Line
            {
                Id = f.String("id"),
                Text = f.String("text"),
                Audio = f.OptionalString("audio"),
                Seconds = f.Number("seconds"),
                PauseAfter = f.OptionalNumber("pause_after") ?? 0,
                Fire = fire == null ? FireState.Telling : Parse(fire, f.Path + ".fire", FireStates),
                When = when == null ? null : ParseCondition(when, f.Path + ".when"),
                Recall = recall == null ? null : ParseCondition(recall, f.Path + ".recall"),
                Actions = actions,
                Claims = f.OptionalStrings("claims"),
            };
        }

        static Condition ParseCondition(string text, string path)
        {
            var colon = text.LastIndexOf(':');
            if (colon <= 0 || colon == text.Length - 1)
                throw new StoryFormatException(path, $"expected '<invitation>:accepted' or '<invitation>:default', got '{text}'");
            return new Condition(text.Substring(0, colon), Parse(text.Substring(colon + 1), path, Outcomes));
        }

        internal static readonly Dictionary<string, FireState> FireStates = new Dictionary<string, FireState>
        {
            { "resting", FireState.Resting }, { "welcoming", FireState.Welcoming }, { "listening", FireState.Listening },
            { "gathering", FireState.Gathering }, { "telling", FireState.Telling }, { "holding", FireState.Holding },
            { "transitioning", FireState.Transitioning },
        };
        internal static readonly Dictionary<string, SceneKind> SceneKinds = new Dictionary<string, SceneKind>
        {
            { "hearth", SceneKind.Hearth }, { "story", SceneKind.Story },
        };
        internal static readonly Dictionary<string, EvidenceState> EvidenceStates = new Dictionary<string, EvidenceState>
        {
            { "documented", EvidenceState.Documented }, { "visitor", EvidenceState.VisitorProvided }, { "reconstruction", EvidenceState.Reconstruction },
        };
        internal static readonly Dictionary<string, ActionKind> ActionKinds = new Dictionary<string, ActionKind>
        {
            { "reveal", ActionKind.Reveal }, { "illuminate", ActionKind.Illuminate }, { "offer", ActionKind.Offer },
            { "place", ActionKind.Place }, { "withdraw", ActionKind.Withdraw }, { "touch", ActionKind.Touch },
        };
        internal static readonly Dictionary<string, InvitationKind> InvitationKinds = new Dictionary<string, InvitationKind>
        {
            { "receive", InvitationKind.Receive }, { "touch", InvitationKind.Touch },
        };
        internal static readonly Dictionary<string, Outcome> Outcomes = new Dictionary<string, Outcome>
        {
            { "accepted", Outcome.Accepted }, { "default", Outcome.Default },
        };
        internal static readonly Dictionary<string, ContentStatus> ContentStatuses = new Dictionary<string, ContentStatus>
        {
            { "placeholder", ContentStatus.Placeholder }, { "draft", ContentStatus.Draft }, { "reviewed", ContentStatus.Reviewed },
        };

        internal static T Parse<T>(string value, string path, Dictionary<string, T> names)
        {
            if (names.TryGetValue(value, out var parsed)) return parsed;
            throw new StoryFormatException(path, $"unknown value '{value}'; expected one of {string.Join(", ", names.Keys)}");
        }

        /// <summary>The JSON name of an enum value, for writing.</summary>
        internal static string Name<T>(T value, Dictionary<string, T> names)
        {
            foreach (var pair in names)
                if (EqualityComparer<T>.Default.Equals(pair.Value, value)) return pair.Key;
            throw new ArgumentException($"No JSON name for {value}");
        }

        /// <summary>Typed access to one JSON object, with errors that name the field's path.</summary>
        internal sealed class Fields
        {
            readonly JsonObject o;

            public Fields(JsonObject o, string path)
            {
                this.o = o;
                Path = path;
            }

            public string Path { get; }

            public void Only(params string[] allowed)
            {
                foreach (var key in o.Keys)
                    if (System.Array.IndexOf(allowed, key) < 0)
                        throw new StoryFormatException(Path + "." + key, "unknown field");
            }

            object Required(string key)
            {
                if (!o.TryGet(key, out var value) || value == null) throw new StoryFormatException(Path + "." + key, "required field is missing");
                return value;
            }

            public string String(string key) =>
                Required(key) as string ?? throw new StoryFormatException(Path + "." + key, "expected a string");

            public string OptionalString(string key) =>
                !o.TryGet(key, out var value) || value == null ? null : value as string ?? throw new StoryFormatException(Path + "." + key, "expected a string");

            public double Number(string key) =>
                AsNumber(Required(key)) ?? throw new StoryFormatException(Path + "." + key, "expected a number");

            public double? OptionalNumber(string key) =>
                !o.TryGet(key, out var value) || value == null ? (double?)null : AsNumber(value) ?? throw new StoryFormatException(Path + "." + key, "expected a number");

            /// <summary>Parsed JSON holds doubles; a plan written in memory may still hold the integers it was built with.</summary>
            static double? AsNumber(object value)
            {
                switch (value)
                {
                    case double d: return d;
                    case int i: return i;
                    case long l: return l;
                    case float f: return f;
                    default: return null;
                }
            }

            public bool OptionalBool(string key, bool fallback) =>
                !o.TryGet(key, out var value) || value == null ? fallback : value is bool b ? b : throw new StoryFormatException(Path + "." + key, "expected true or false");

            public int Int(string key)
            {
                var d = Number(key);
                if (d != Math.Floor(d) || d < int.MinValue || d > int.MaxValue) throw new StoryFormatException(Path + "." + key, "expected an integer");
                return (int)d;
            }

            List<object> ArrayField(string key, bool required)
            {
                if (!o.TryGet(key, out var value) || value == null)
                {
                    if (required) throw new StoryFormatException(Path + "." + key, "required field is missing");
                    return new List<object>();
                }
                return value as List<object> ?? throw new StoryFormatException(Path + "." + key, "expected an array");
            }

            public IReadOnlyList<string> Strings(string key) => StringList(key, true);
            public IReadOnlyList<string> OptionalStrings(string key) => StringList(key, false);

            IReadOnlyList<string> StringList(string key, bool required)
            {
                var items = ArrayField(key, required);
                var result = new List<string>(items.Count);
                for (var i = 0; i < items.Count; i++)
                    result.Add(items[i] as string ?? throw new StoryFormatException($"{Path}.{key}[{i}]", "expected a string"));
                return result;
            }

            public IReadOnlyList<T> Objects<T>(string key, Func<Fields, T> read) => ObjectList(key, read, true);
            public IReadOnlyList<T> OptionalObjects<T>(string key, Func<Fields, T> read) => ObjectList(key, read, false);

            IReadOnlyList<T> ObjectList<T>(string key, Func<Fields, T> read, bool required)
            {
                var items = ArrayField(key, required);
                var result = new List<T>(items.Count);
                for (var i = 0; i < items.Count; i++)
                {
                    var path = $"{Path}.{key}[{i}]";
                    var item = items[i] as JsonObject ?? throw new StoryFormatException(path, "expected an object");
                    result.Add(read(new Fields(item, path)));
                }
                return result;
            }

            public T OptionalObject<T>(string key, Func<Fields, T> read) where T : class
            {
                if (!o.TryGet(key, out var value) || value == null) return null;
                var item = value as JsonObject ?? throw new StoryFormatException(Path + "." + key, "expected an object");
                return read(new Fields(item, Path + "." + key));
            }

            public JsonObject Object(string key) =>
                Required(key) as JsonObject ?? throw new StoryFormatException(Path + "." + key, "expected an object");

            public JsonObject OptionalJsonObject(string key) =>
                !o.TryGet(key, out var value) || value == null ? null : value as JsonObject ?? throw new StoryFormatException(Path + "." + key, "expected an object");
        }
    }
}
