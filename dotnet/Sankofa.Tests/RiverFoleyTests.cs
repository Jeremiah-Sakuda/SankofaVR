using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using NUnit.Framework;

namespace Sankofa.Tests
{
    public sealed class RiverFoleyTests
    {
        [Test]
        public void IncludedFoleyMatchesTheReviewedSourceRecords()
        {
            var folder = Fixtures.RepositoryFile("unity/Assets/Sankofa/Art/Recorded/RiverThreshold");
            using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "sources.json")));
            var records = manifest.RootElement.GetProperty("recordings").EnumerateArray().ToArray();
            Assert.That(records.Select(r => r.GetProperty("file").GetString()), Is.EquivalentTo(new[] { "pot-touch.wav", "water-stir.wav" }));
            foreach (var record in records)
            {
                var file = Path.Combine(folder, record.GetProperty("file").GetString());
                var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))).ToLowerInvariant();
                Assert.That(hash, Is.EqualTo(record.GetProperty("sha256").GetString()), "audio changes require a new provenance/edit record");
                Assert.That(File.Exists(file + ".meta"), Is.True);
                Assert.That(record.GetProperty("licence").GetString(), Is.EqualTo("CC0 1.0"));
                Assert.That(record.GetProperty("page").GetString(), Does.StartWith("https://freesound.org/people/"));
                Assert.That(record.GetProperty("author").GetString(), Is.Not.Empty);
            }
            Assert.That(Directory.GetFiles(folder, "*.wav").Length, Is.EqualTo(records.Length), "no unregistered foley is included");
        }
    }
}
