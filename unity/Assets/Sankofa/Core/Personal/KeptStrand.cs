using System;
using System.Collections.Generic;
using System.Linq;
using Sankofa.Core.Json;

namespace Sankofa.Core.Personal
{
    /// <summary>
    /// One kept side of one prepared telling. <see cref="Telling"/> is the telling's interest ID and <see cref="Side"/>
    /// is the ID of the perspective choice that was followed, exactly as <see cref="BundledPersonalStoryComposer"/> offers it.
    /// </summary>
    public sealed class KeptBead : IEquatable<KeptBead>
    {
        public KeptBead(string telling, string side) { Telling = telling; Side = side; }
        public string Telling { get; }
        public string Side { get; }
        public bool Equals(KeptBead other) => other != null && other.Telling == Telling && other.Side == Side;
        public override bool Equals(object obj) => Equals(obj as KeptBead);
        public override int GetHashCode() => ((Telling ?? "").GetHashCode() * 397) ^ (Side ?? "").GetHashCode();
        public override string ToString() => Telling + "/" + Side;
    }

    /// <summary>
    /// Which sides of which prepared tellings the visitor has kept, in the order they were kept. It holds identifiers
    /// only: no story text, intake, answers, placement or time. A side is kept once; keeping it again changes nothing.
    /// Tellings from the optional service have no sides and so no bead.
    /// </summary>
    public sealed class KeptStrand
    {
        public const string SchemaId = "sankofa.kept-strand";
        public const int SchemaVersion = 1;

        static readonly Lazy<KeyValuePair<string, PersonalStoryChoice[]>[]> offered =
            new Lazy<KeyValuePair<string, PersonalStoryChoice[]>[]>(() => PersonalStoryCatalog.Interests.Select(telling =>
                new KeyValuePair<string, PersonalStoryChoice[]>(telling,
                    BundledPersonalStoryComposer.Compose(new PersonalStoryRequest(PersonalStoryCatalog.Region, PersonalStoryCatalog.Period, telling)).Story
                        .Chapters.Where(PersonalStoryRoutes.HasChoices).SelectMany(c => c.Choices).ToArray())).ToArray());

        readonly List<KeptBead> beads = new List<KeptBead>();

        /// <summary>The prepared tellings, in catalogue order.</summary>
        public static IReadOnlyList<string> Tellings => offered.Value.Select(t => t.Key).ToArray();

        /// <summary>
        /// The sides a telling offers, in authored order with the authored default first. Each is a copy of the story's
        /// own choice: its ID, the label to name it by and the chapter it leads to. Empty for an unknown telling.
        /// </summary>
        public static IReadOnlyList<PersonalStoryChoice> Sides(string telling) =>
            offered.Value.Where(t => t.Key == telling).SelectMany(t => t.Value)
                .Select(c => new PersonalStoryChoice { Id = c.Id, Label = c.Label, Next = c.Next }).ToArray();

        /// <summary>Every bead there is to keep: one for each side of each prepared telling.</summary>
        public static int Capacity => offered.Value.Sum(t => t.Value.Length);

        public static bool IsKnown(string telling, string side) =>
            telling != null && side != null && offered.Value.Any(t => t.Key == telling && t.Value.Any(c => c.Id == side));

        /// <summary>Beads in the order they were kept, oldest first.</summary>
        public IReadOnlyList<KeptBead> Beads => beads.ToArray();
        public int Count => beads.Count;
        /// <summary>How many sides are still to be kept.</summary>
        public int Remaining => Capacity - beads.Count;
        public bool IsComplete => Remaining == 0;

        public bool Has(string telling, string side) => beads.Contains(new KeptBead(telling, side));

        /// <summary>
        /// Keeps one side of one telling. Returns true when a bead was added and false when that side was already kept,
        /// leaving its place in the order unchanged. An unknown telling or side is an error, never a new kind of bead.
        /// </summary>
        public bool Add(string telling, string side)
        {
            if (!IsKnown(telling, side)) throw new ArgumentException("A bead must be one side of a prepared telling.");
            var bead = new KeptBead(telling, side);
            if (beads.Contains(bead)) return false;
            beads.Add(bead);
            return true;
        }

        /// <summary>
        /// The side to suggest on the next visit, or null when every side is kept: the other side of the telling kept most
        /// recently, then a telling not yet started (its authored first side), then whatever else remains, most recent first.
        /// </summary>
        public KeptBead NextUnheard()
        {
            if (beads.Count > 0)
            {
                var other = Unkept(beads[beads.Count - 1].Telling);
                if (other != null) return other;
            }
            foreach (var telling in offered.Value)
                if (beads.All(b => b.Telling != telling.Key))
                {
                    var first = Unkept(telling.Key);
                    if (first != null) return first;
                }
            for (var i = beads.Count - 1; i >= 0; i--)
            {
                var remaining = Unkept(beads[i].Telling);
                if (remaining != null) return remaining;
            }
            return null;
        }

        KeptBead Unkept(string telling) =>
            offered.Value.Where(t => t.Key == telling).SelectMany(t => t.Value)
                .Select(c => new KeptBead(telling, c.Id)).FirstOrDefault(b => !beads.Contains(b));
    }

    /// <summary>Strict, bounded persistence form of the strand. A file with anything unexpected in it is not a strand.</summary>
    public static class KeptStrandJson
    {
        public const int MaxJsonLength = 2048;

        public static KeptStrand Parse(string text)
        {
            var root = StrictJson.Root(text, MaxJsonLength, 3);
            root.Only("schema", "version", "beads");
            if (root.String("schema") != KeptStrand.SchemaId) throw new PersonalStoryFormatException("$.schema", "Unsupported kept-strand schema.");
            if (root.Integer("version") != KeptStrand.SchemaVersion) throw new PersonalStoryFormatException("$.version", "Unsupported kept-strand version.");
            var strand = new KeptStrand();
            var index = 0;
            foreach (var bead in root.Objects("beads", KeptStrand.Capacity, item =>
            {
                item.Only("telling", "side");
                return new KeptBead(item.String("telling"), item.String("side"));
            }))
            {
                var path = "$.beads[" + index++ + "]";
                if (!KeptStrand.IsKnown(bead.Telling, bead.Side)) throw new PersonalStoryFormatException(path, "Unknown telling or side.");
                if (!strand.Add(bead.Telling, bead.Side)) throw new PersonalStoryFormatException(path, "A side can be kept only once.");
            }
            return strand;
        }

        public static string Write(KeptStrand strand)
        {
            if (strand == null) throw new ArgumentNullException(nameof(strand));
            return JsonText.Serialize(new JsonObject()
                .Add("schema", KeptStrand.SchemaId).Add("version", KeptStrand.SchemaVersion)
                .Add("beads", strand.Beads.Select(b => new JsonObject().Add("telling", b.Telling).Add("side", b.Side))));
        }
    }
}
