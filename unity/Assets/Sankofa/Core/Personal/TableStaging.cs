using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Sankofa.Core.Json;

namespace Sankofa.Core.Personal
{
    /// <summary>
    /// How the table's pop-up stage performs each prepared chapter: who stands where, in which painted pose, and at
    /// what point of the chapter's narration. Presentation data only. It never changes story text, choices or
    /// state, and a chapter without staging is still complete; the stage simply holds its last arrangement.
    /// </summary>
    public sealed class TableStaging
    {
        public const string Unfold = "unfold", Turn = "turn", Stay = "stay";
        public const string GourdWith = "with", GourdBetween = "between", GourdLifted = "lifted", GourdHidden = "hidden";
        /// <summary>The painted cast atlas has two figures; the gourd is the one tabletop prop that travels between them.</summary>
        public static readonly IReadOnlyList<string> Figures = new[] { "wambui", "naserian" };
        public const string Gourd = "gourd";
        public const int Poses = 3;
        /// <summary>Stage-local reach of a figure on the turntable, in metres.</summary>
        public const double StageRadius = 0.3;

        public sealed class Beat
        {
            /// <summary>0..1: the fraction of the chapter's narration at which this beat lands.</summary>
            public double At;
            public string Who;
            public double? X, Z;
            public int? Pose;
            public bool? Mirror, Shown;
            /// <summary>For the gourd: with a figure, between them, lifted into the light, or hidden.</summary>
            public string Place, Holder;
        }

        public sealed class Chapter
        {
            public string Id;
            /// <summary>How the stage arrives at this chapter: rising from flat, turning to the other side, or staying up.</summary>
            public string Entry = Unfold;
            public List<Beat> Beats = new List<Beat>();
        }

        readonly Dictionary<string, Chapter> chapters = new Dictionary<string, Chapter>(StringComparer.Ordinal);
        readonly Dictionary<string, string> choiceFigures = new Dictionary<string, string>(StringComparer.Ordinal);

        public IReadOnlyCollection<string> ChapterIds => chapters.Keys;
        public IReadOnlyDictionary<string, string> ChoiceFigures => choiceFigures;
        public Chapter For(string chapterId) => chapterId != null && chapters.TryGetValue(chapterId, out var c) ? c : null;
        /// <summary>The painted figure who stands for a perspective choice, so the visitor can choose by touching her.</summary>
        public string FigureFor(string choiceId) => choiceId != null && choiceFigures.TryGetValue(choiceId, out var f) ? f : null;

        public static TableStaging Parse(string json)
        {
            var root = JsonText.ParseObject(json ?? throw new ArgumentNullException(nameof(json)));
            if (root.GetInt("version") != 1) throw new FormatException("Unsupported staging version");
            var staging = new TableStaging();
            foreach (var pair in root.GetObject("choices"))
            {
                var figure = pair.Value as string;
                if (!Figures.Contains(figure)) throw new FormatException($"Choice {pair.Key} names an unknown figure");
                staging.choiceFigures[pair.Key] = figure;
            }
            foreach (var pair in root.GetObject("chapters"))
            {
                var data = pair.Value as JsonObject ?? throw new FormatException($"Chapter {pair.Key} is not an object");
                var chapter = new Chapter { Id = pair.Key, Entry = data.GetStringOrNull("entry") ?? Unfold };
                if (chapter.Entry != Unfold && chapter.Entry != Turn && chapter.Entry != Stay) throw new FormatException($"Chapter {pair.Key} has an unknown entry");
                var previous = 0.0;
                foreach (var item in data.GetArray("beats"))
                {
                    var beat = ReadBeat(item as JsonObject ?? throw new FormatException($"A beat of {pair.Key} is not an object"), pair.Key);
                    if (beat.At < previous) throw new FormatException($"Beats of {pair.Key} are out of order");
                    previous = beat.At;
                    chapter.Beats.Add(beat);
                }
                staging.chapters[pair.Key] = chapter;
            }
            return staging;
        }

        static Beat ReadBeat(JsonObject data, string chapter)
        {
            var beat = new Beat { At = data.GetNumber("at"), Who = data.GetString("who") };
            if (beat.At < 0 || beat.At > 1) throw new FormatException($"A beat of {chapter} falls outside its narration");
            double? Number(string key) => data.TryGet(key, out var v) ? Convert.ToDouble(v, CultureInfo.InvariantCulture) : (double?)null;
            bool? Flag(string key) => data.TryGet(key, out var v) ? (bool)v : (bool?)null;
            beat.X = Number("x"); beat.Z = Number("z"); beat.Mirror = Flag("mirror"); beat.Shown = Flag("shown");
            var pose = Number("pose");
            if (pose.HasValue) beat.Pose = (int)pose.Value;
            if (beat.Who == Gourd)
            {
                beat.Place = data.GetString("place");
                beat.Holder = data.GetStringOrNull("holder");
                if (beat.Place != GourdWith && beat.Place != GourdBetween && beat.Place != GourdLifted && beat.Place != GourdHidden)
                    throw new FormatException($"The gourd in {chapter} has an unknown place");
                if (beat.Place == GourdWith && !Figures.Contains(beat.Holder)) throw new FormatException($"The gourd in {chapter} needs a figure to hold it");
                return beat;
            }
            if (!Figures.Contains(beat.Who)) throw new FormatException($"A beat of {chapter} names an unknown figure");
            if (beat.Pose.HasValue && (beat.Pose < 0 || beat.Pose >= Poses)) throw new FormatException($"A pose in {chapter} is not painted");
            if (Math.Abs(beat.X ?? 0) > StageRadius || Math.Abs(beat.Z ?? 0) > StageRadius) throw new FormatException($"A figure in {chapter} leaves the stage");
            return beat;
        }
    }
}
