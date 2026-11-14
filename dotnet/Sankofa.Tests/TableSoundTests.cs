using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using NUnit.Framework;

namespace Sankofa.Tests
{
    /// <summary>
    /// The story table's short stage sounds are cuts of field recordings already in the repository. This holds each
    /// file to its record: which recording it came from, under whose name and licence, and that neither has changed.
    /// It says nothing about how they sound; nobody has listened to them.
    /// </summary>
    public sealed class TableSoundTests
    {
        static string Recorded => Fixtures.RepositoryFile("unity/Assets/Sankofa/Art/Recorded");
        static string Sounds => Fixtures.RepositoryFile("unity/Assets/Sankofa/Resources/Sounds");

        static string Sha256(string path)
        {
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }

        [Test]
        public void EveryStageSoundIsACutOfARecordingWhoseSourceIsOnRecord()
        {
            using var derived = JsonDocument.Parse(File.ReadAllText(Path.Combine(Recorded, "derived-sounds.json")));
            using var sources = JsonDocument.Parse(File.ReadAllText(Path.Combine(Recorded, "sources.json")));
            var recordings = sources.RootElement.GetProperty("recordings").EnumerateArray().ToDictionary(r => r.GetProperty("file").GetString());
            var sounds = derived.RootElement.GetProperty("sounds").EnumerateArray().ToArray();
            Assert.That(sounds, Is.Not.Empty);
            foreach (var sound in sounds)
            {
                var file = sound.GetProperty("file").GetString();
                var path = Path.Combine(Sounds, file);
                Assert.That(File.Exists(path), Is.True, file);
                Assert.That(File.Exists(path + ".meta"), Is.True, file + " has its Unity meta file");
                Assert.That(Sha256(path), Is.EqualTo(sound.GetProperty("sha256").GetString()), file + " is the cut on record");
                var source = sound.GetProperty("source").GetString();
                Assert.That(recordings.ContainsKey(source), Is.True, source + " is a registered recording");
                var origin = recordings[source];
                Assert.That(Sha256(Path.Combine(Recorded, source)), Is.EqualTo(sound.GetProperty("source_sha256").GetString()), source + " is unchanged since the cut");
                Assert.That(origin.GetProperty("sha256").GetString(), Is.EqualTo(sound.GetProperty("source_sha256").GetString()));
                foreach (var field in new[] { "author", "licence", "page", "title" })
                    Assert.That(sound.GetProperty(field).GetString(), Is.EqualTo(origin.GetProperty(field).GetString()), file + " keeps its source's " + field);
            }
            // Nothing sits in the folder without a record, and nothing on record is missing.
            var onDisk = Directory.GetFiles(Sounds, "*.wav").Select(Path.GetFileName).OrderBy(n => n).ToArray();
            Assert.That(onDisk, Is.EqualTo(sounds.Select(s => s.GetProperty("file").GetString()).OrderBy(n => n).ToArray()));
        }

        [Test]
        public void TheSoundsTheAppAsksForAreShortMonoRecordings()
        {
            var app = File.ReadAllText(Fixtures.RepositoryFile("unity/Assets/Sankofa/Runtime/Personal/PersonalStoryApp.cs"));
            foreach (var name in new[] { "stage-fold", "stage-rise" })
            {
                Assert.That(app, Does.Contain("\"" + name + "\""), "the app plays " + name);
                var bytes = File.ReadAllBytes(Path.Combine(Sounds, name + ".wav"));
                Assert.That(System.Text.Encoding.ASCII.GetString(bytes, 0, 4), Is.EqualTo("RIFF"));
                Assert.That(System.Text.Encoding.ASCII.GetString(bytes, 8, 8), Is.EqualTo("WAVEfmt "));
                var channels = BitConverter.ToInt16(bytes, 22); var rate = BitConverter.ToInt32(bytes, 24); var bits = BitConverter.ToInt16(bytes, 34);
                Assert.That((channels, bits), Is.EqualTo(((short)1, (short)16)), name);
                var seconds = (bytes.Length - 44) / (double)(rate * 2);
                Assert.That(seconds, Is.InRange(0.3, 1.5), name + " is a short sound, not an ambience");
            }
            // The table credits the recording they were cut from.
            Assert.That(app, Does.Contain("windy tent").And.Contain("bruno.auzet"));
        }
    }
}
