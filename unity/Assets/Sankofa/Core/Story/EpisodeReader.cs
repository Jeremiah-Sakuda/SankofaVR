using System;
using System.Collections.Generic;
using Sankofa.Core.Json;

namespace Sankofa.Core.Story
{
    /// <summary>A content file that cannot be read as an episode. Semantic problems are reported by <see cref="EpisodeValidator"/>.</summary>
    public sealed class EpisodeFormatException : Exception
    {
        public EpisodeFormatException(string path, string message) : base(path + ": " + message) => Path = path;
        public string Path { get; }
    }

    /// <summary>Reads the episode JSON format (schema 1). Unknown fields are rejected so typos cannot silently drop content.</summary>
    public static class EpisodeReader
    {
        public const int SupportedSchema = 1;

        public static Episode Read(string json)
        {
            JsonObject root;
            try
            {
                root = JsonText.ParseObject(json);
            }
            catch (FormatException e)
            {
                throw new EpisodeFormatException("$", e.Message);
            }

            var r = new Fields(root, "$");
            r.Only("schema", "id", "version", "title", "locale", "status", "note", "scenes", "objects", "claims", "welcome", "beats", "early_return", "keepsake");
            var schema = r.Int("schema");
            if (schema != SupportedSchema) throw new EpisodeFormatException("$.schema", $"unsupported schema {schema}; expected {SupportedSchema}");

            return new Episode
            {
                Schema = schema,
                Id = r.String("id"),
                Version = r.String("version"),
                Title = r.String("title"),
                Locale = r.String("locale"),
                Status = Parse(r.String("status"), "$.status", ContentStatuses),
                Note = r.OptionalString("note"),
                Scenes = r.Objects("scenes", ReadScene),
                Objects = r.Objects("objects", ReadObject),
                Claims = r.OptionalObjects("claims", ReadClaim),
                Welcome = r.Objects("welcome", ReadLine),
                Beats = r.Objects("beats", ReadBeat),
                EarlyReturn = r.OptionalObjects("early_return", ReadLine),
                Keepsake = r.OptionalString("keepsake"),
            };
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

        static Beat ReadBeat(Fields f)
        {
            f.Only("id", "scene", "lines", "invitation");
            return new Beat
            {
                Id = f.String("id"),
                Scene = f.String("scene"),
                Lines = f.Objects("lines", ReadLine),
                Invitation = f.OptionalObject("invitation", ReadInvitation),
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
                Accepted = f.Objects("accepted", ReadLine),
                Default = f.Objects("default", ReadLine),
            };
        }

        static Line ReadLine(Fields f)
        {
            f.Only("id", "text", "audio", "seconds", "pause_after", "fire", "when", "actions", "claims");
            var actions = f.OptionalObjects("actions", a =>
            {
                a.Only("do", "object");
                return new StoryAction(Parse(a.String("do"), a.Path + ".do", ActionKinds), a.String("object"));
            });
            var fire = f.OptionalString("fire");
            var when = f.OptionalString("when");
            return new Line
            {
                Id = f.String("id"),
                Text = f.String("text"),
                Audio = f.OptionalString("audio"),
                Seconds = f.Number("seconds"),
                PauseAfter = f.OptionalNumber("pause_after") ?? 0,
                Fire = fire == null ? FireState.Telling : Parse(fire, f.Path + ".fire", FireStates),
                When = when == null ? null : ParseCondition(when, f.Path + ".when"),
                Actions = actions,
                Claims = f.OptionalStrings("claims"),
            };
        }

        static Condition ParseCondition(string text, string path)
        {
            var colon = text.LastIndexOf(':');
            if (colon <= 0 || colon == text.Length - 1)
                throw new EpisodeFormatException(path, $"expected '<invitation>:accepted' or '<invitation>:default', got '{text}'");
            return new Condition(text.Substring(0, colon), Parse(text.Substring(colon + 1), path, Outcomes));
        }

        static readonly Dictionary<string, FireState> FireStates = new Dictionary<string, FireState>
        {
            { "resting", FireState.Resting }, { "welcoming", FireState.Welcoming }, { "listening", FireState.Listening },
            { "gathering", FireState.Gathering }, { "telling", FireState.Telling }, { "holding", FireState.Holding },
            { "transitioning", FireState.Transitioning },
        };
        static readonly Dictionary<string, SceneKind> SceneKinds = new Dictionary<string, SceneKind>
        {
            { "hearth", SceneKind.Hearth }, { "story", SceneKind.Story },
        };
        static readonly Dictionary<string, EvidenceState> EvidenceStates = new Dictionary<string, EvidenceState>
        {
            { "documented", EvidenceState.Documented }, { "visitor", EvidenceState.VisitorProvided }, { "reconstruction", EvidenceState.Reconstruction },
        };
        static readonly Dictionary<string, ActionKind> ActionKinds = new Dictionary<string, ActionKind>
        {
            { "reveal", ActionKind.Reveal }, { "illuminate", ActionKind.Illuminate }, { "offer", ActionKind.Offer }, { "place", ActionKind.Place },
        };
        static readonly Dictionary<string, InvitationKind> InvitationKinds = new Dictionary<string, InvitationKind>
        {
            { "receive", InvitationKind.Receive },
        };
        static readonly Dictionary<string, Outcome> Outcomes = new Dictionary<string, Outcome>
        {
            { "accepted", Outcome.Accepted }, { "default", Outcome.Default },
        };
        static readonly Dictionary<string, ContentStatus> ContentStatuses = new Dictionary<string, ContentStatus>
        {
            { "placeholder", ContentStatus.Placeholder }, { "draft", ContentStatus.Draft }, { "reviewed", ContentStatus.Reviewed },
        };

        static T Parse<T>(string value, string path, Dictionary<string, T> names)
        {
            if (names.TryGetValue(value, out var parsed)) return parsed;
            throw new EpisodeFormatException(path, $"unknown value '{value}'; expected one of {string.Join(", ", names.Keys)}");
        }

        /// <summary>Typed access to one JSON object, with errors that name the field's path.</summary>
        sealed class Fields
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
                        throw new EpisodeFormatException(Path + "." + key, "unknown field");
            }

            object Required(string key)
            {
                if (!o.TryGet(key, out var value) || value == null) throw new EpisodeFormatException(Path + "." + key, "required field is missing");
                return value;
            }

            public string String(string key) =>
                Required(key) as string ?? throw new EpisodeFormatException(Path + "." + key, "expected a string");

            public string OptionalString(string key) =>
                !o.TryGet(key, out var value) || value == null ? null : value as string ?? throw new EpisodeFormatException(Path + "." + key, "expected a string");

            public double Number(string key) =>
                Required(key) is double d ? d : throw new EpisodeFormatException(Path + "." + key, "expected a number");

            public double? OptionalNumber(string key) =>
                !o.TryGet(key, out var value) || value == null ? (double?)null : value is double d ? d : throw new EpisodeFormatException(Path + "." + key, "expected a number");

            public int Int(string key)
            {
                var d = Number(key);
                if (d != Math.Floor(d) || d < int.MinValue || d > int.MaxValue) throw new EpisodeFormatException(Path + "." + key, "expected an integer");
                return (int)d;
            }

            List<object> ArrayField(string key, bool required)
            {
                if (!o.TryGet(key, out var value) || value == null)
                {
                    if (required) throw new EpisodeFormatException(Path + "." + key, "required field is missing");
                    return new List<object>();
                }
                return value as List<object> ?? throw new EpisodeFormatException(Path + "." + key, "expected an array");
            }

            public IReadOnlyList<string> Strings(string key) => StringList(key, true);
            public IReadOnlyList<string> OptionalStrings(string key) => StringList(key, false);

            IReadOnlyList<string> StringList(string key, bool required)
            {
                var items = ArrayField(key, required);
                var result = new List<string>(items.Count);
                for (var i = 0; i < items.Count; i++)
                    result.Add(items[i] as string ?? throw new EpisodeFormatException($"{Path}.{key}[{i}]", "expected a string"));
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
                    var item = items[i] as JsonObject ?? throw new EpisodeFormatException(path, "expected an object");
                    result.Add(read(new Fields(item, path)));
                }
                return result;
            }

            public T OptionalObject<T>(string key, Func<Fields, T> read) where T : class
            {
                if (!o.TryGet(key, out var value) || value == null) return null;
                var item = value as JsonObject ?? throw new EpisodeFormatException(Path + "." + key, "expected an object");
                return read(new Fields(item, Path + "." + key));
            }
        }
    }
}
