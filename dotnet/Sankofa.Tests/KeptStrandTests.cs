using System;
using System.Linq;
using NUnit.Framework;
using Sankofa.Core.Personal;

namespace Sankofa.Tests
{
    public sealed class KeptStrandTests
    {
        const string Waiting = "journeys.follow.waiting", Late = "journeys.follow.late";
        const string Less = "trade-household.follow.less", More = "trade-household.follow.more";
        const string Remembers = "place-water.follow.remembers", Traveller = "place-water.follow.traveller";

        static KeptStrand Strand(params (string Telling, string Side)[] beads)
        {
            var strand = new KeptStrand();
            foreach (var bead in beads) Assert.That(strand.Add(bead.Telling, bead.Side), Is.True);
            return strand;
        }
        static string Next(KeptStrand strand) => strand.NextUnheard()?.ToString();

        [Test]
        public void BeadsAreTheSidesThePreparedTellingsOffer()
        {
            Assert.That(KeptStrand.Tellings, Is.EqualTo(PersonalStoryCatalog.Interests));
            Assert.That(KeptStrand.Capacity, Is.EqualTo(6), "three tellings with two sides each");
            foreach (var telling in PersonalStoryCatalog.Interests)
            {
                var story = BundledPersonalStoryComposer.Compose(new PersonalStoryRequest("enkare-nairobi", "1850s", telling)).Story;
                var offered = story.Chapters.Single(PersonalStoryRoutes.HasChoices).Choices;
                var sides = KeptStrand.Sides(telling);
                Assert.That(sides.Select(s => s.Id), Is.EqualTo(offered.Select(c => c.Id)), "authored order, default first");
                Assert.That(sides.Select(s => s.Label), Is.EqualTo(offered.Select(c => c.Label)));
                Assert.That(sides.Select(s => s.Next), Is.EqualTo(offered.Select(c => c.Next)));
                sides[0].Label = "changed by a caller";
                Assert.That(KeptStrand.Sides(telling)[0].Label, Is.EqualTo(offered[0].Label), "callers receive copies");
            }
            Assert.That(KeptStrand.Sides("ancestry"), Is.Empty);
        }

        [Test]
        public void KeepingASideTwiceChangesNothing()
        {
            var strand = new KeptStrand();
            Assert.That(strand.Remaining, Is.EqualTo(6));
            Assert.That(strand.Add("journeys", Late), Is.True);
            Assert.That(strand.Add("place-water", Remembers), Is.True);
            Assert.That(strand.Add("journeys", Late), Is.False);
            Assert.That(strand.Beads.Select(b => b.ToString()), Is.EqualTo(new[] { "journeys/" + Late, "place-water/" + Remembers }), "order kept, oldest first");
            Assert.That((strand.Count, strand.Remaining, strand.IsComplete), Is.EqualTo((2, 4, false)));
            Assert.That(strand.Has("journeys", Late), Is.True);
            Assert.That(strand.Has("journeys", Waiting), Is.False);
        }

        [TestCase("ancestry", "ancestry.follow.elder")]
        [TestCase("journeys", "journeys.follow.missing")]
        [TestCase("journeys", "trade-household.follow.less")]
        [TestCase("journeys", "journeys.meeting")]
        [TestCase("Journeys", "journeys.follow.late")]
        [TestCase("journeys", null)]
        [TestCase(null, "journeys.follow.late")]
        public void AnUnknownTellingOrSideIsNeverABead(string telling, string side)
        {
            var strand = new KeptStrand();
            Assert.Throws<ArgumentException>(() => strand.Add(telling, side));
            Assert.That(strand.Count, Is.EqualTo(0));
            Assert.That(KeptStrand.IsKnown(telling, side), Is.False);
        }

        [Test]
        public void TheNextSuggestionPrefersTheOtherSideThenAnUnstartedTellingThenWhatRemains()
        {
            Assert.That(Next(new KeptStrand()), Is.EqualTo("journeys/" + Waiting), "a first visit begins with the first telling's authored side");
            Assert.That(Next(Strand(("journeys", Late))), Is.EqualTo("journeys/" + Waiting), "the other side of the telling just kept");
            Assert.That(Next(Strand(("trade-household", Less))), Is.EqualTo("trade-household/" + More));
            Assert.That(Next(Strand(("journeys", Waiting), ("journeys", Late))), Is.EqualTo("trade-household/" + Less), "then a telling not yet started");
            Assert.That(Next(Strand(("place-water", Traveller), ("journeys", Waiting))), Is.EqualTo("journeys/" + Late), "recency decides, not catalogue order");
            Assert.That(Next(Strand(("journeys", Waiting), ("trade-household", Less), ("trade-household", More))), Is.EqualTo("place-water/" + Remembers),
                "an unstarted telling comes before finishing an older one");
            Assert.That(Next(Strand(("journeys", Waiting), ("trade-household", Less), ("place-water", Remembers), ("place-water", Traveller))),
                Is.EqualTo("trade-household/" + More), "with every telling started, the most recently kept unfinished one");
            var five = Strand(("journeys", Waiting), ("trade-household", Less), ("trade-household", More), ("place-water", Remembers), ("place-water", Traveller));
            Assert.That(Next(five), Is.EqualTo("journeys/" + Late));
            Assert.That(five.Add("journeys", Late), Is.True);
            Assert.That((five.NextUnheard(), five.Remaining, five.IsComplete), Is.EqualTo(((KeptBead)null, 0, true)));
        }

        [Test]
        public void TheSuggestionIsAlwaysASideNotYetKept()
        {
            var all = KeptStrand.Tellings.SelectMany(t => KeptStrand.Sides(t).Select(s => (Telling: t, Side: s.Id))).ToArray();
            // Every order of keeping the six sides, by index permutation.
            foreach (var order in Permutations(Enumerable.Range(0, all.Length).ToArray()))
            {
                var strand = new KeptStrand();
                foreach (var i in order)
                {
                    var next = strand.NextUnheard();
                    Assert.That(next, Is.Not.Null);
                    Assert.That(KeptStrand.IsKnown(next.Telling, next.Side) && !strand.Has(next.Telling, next.Side), Is.True);
                    strand.Add(all[i].Telling, all[i].Side);
                }
                Assert.That(strand.NextUnheard(), Is.Null);
            }
        }

        static System.Collections.Generic.IEnumerable<int[]> Permutations(int[] items, int from = 0)
        {
            if (from == items.Length - 1) { yield return (int[])items.Clone(); yield break; }
            for (var i = from; i < items.Length; i++)
            {
                (items[from], items[i]) = (items[i], items[from]);
                foreach (var result in Permutations(items, from + 1)) yield return result;
                (items[from], items[i]) = (items[i], items[from]);
            }
        }

        [Test]
        public void TheStoredFormIsStableAndRoundTrips()
        {
            Assert.That(KeptStrandJson.Write(new KeptStrand()), Is.EqualTo("{\n  \"schema\": \"sankofa.kept-strand\",\n  \"version\": 1,\n  \"beads\": []\n}\n"));
            var strand = Strand(("place-water", Traveller), ("journeys", Late), ("journeys", Waiting));
            var text = KeptStrandJson.Write(strand);
            Assert.That(text, Does.Contain("\"telling\": \"place-water\"").And.Contain("\"side\": \"" + Traveller + "\""));
            var read = KeptStrandJson.Parse(text);
            Assert.That(read.Beads, Is.EqualTo(strand.Beads));
            Assert.That(KeptStrandJson.Write(read), Is.EqualTo(text));
            Assert.That(Next(read), Is.EqualTo(Next(strand)));
        }

        static string Document(string beads, string schema = "\"sankofa.kept-strand\"", string version = "1", string extra = "") =>
            "{\"schema\":" + schema + ",\"version\":" + version + ",\"beads\":" + beads + extra + "}";
        static string Bead(string telling, string side) => "{\"telling\":\"" + telling + "\",\"side\":\"" + side + "\"}";

        [Test]
        public void ReadingIsStrictAndBounded()
        {
            var seven = "[" + string.Join(",", Enumerable.Repeat(Bead("journeys", Late), 7)) + "]";
            var cases = new (string Name, string Text)[]
            {
                ("null", null), ("empty", ""), ("not an object", "[]"), ("not JSON", "kept"),
                ("trailing content", Document("[]") + "{}"),
                ("another schema", Document("[]", schema: "\"sankofa.personal-story.v1\"")),
                ("schema as a number", Document("[]", schema: "1")),
                ("a later version", Document("[]", version: "2")),
                ("a fractional version", Document("[]", version: "1.5")),
                ("version as text", Document("[]", version: "\"1\"")),
                ("an unknown field", Document("[]", extra: ",\"visitor\":\"a name\"")),
                ("a missing list", "{\"schema\":\"sankofa.kept-strand\",\"version\":1}"),
                ("beads as an object", Document("{}")),
                ("a bead as text", Document("[\"journeys\"]")),
                ("an unknown telling", Document("[" + Bead("ancestry", Late) + "]")),
                ("an unknown side", Document("[" + Bead("journeys", "journeys.follow.missing") + "]")),
                ("a side of another telling", Document("[" + Bead("journeys", Less) + "]")),
                ("a bead kept twice", Document("[" + Bead("journeys", Late) + "," + Bead("journeys", Late) + "]")),
                ("an extra bead field", Document("[{\"telling\":\"journeys\",\"side\":\"" + Late + "\",\"kept_at\":\"2026-11-10\"}]")),
                ("a bead without a side", Document("[{\"telling\":\"journeys\"}]")),
                ("more beads than sides", Document(seven)),
                ("a duplicate property", "{\"schema\":\"sankofa.kept-strand\",\"schema\":\"sankofa.kept-strand\",\"version\":1,\"beads\":[]}"),
                ("deep nesting", Document("[[[[]]]]")),
                ("an oversized file", Document("[]" + new string(' ', KeptStrandJson.MaxJsonLength))),
            };
            foreach (var (name, text) in cases)
                Assert.Throws<PersonalStoryFormatException>(() => KeptStrandJson.Parse(text), name);
            Assert.That(KeptStrandJson.Parse(Document("[" + Bead("journeys", Late) + "]")).Count, Is.EqualTo(1));
        }
    }
}
