using System.Collections.Generic;
using System.IO;
using Sankofa.Core.Story;
using NUnit.Framework;

namespace Sankofa.Tests
{
    static class Fixtures
    {
        public const string PlaceholderEpisode = "unity/Assets/Sankofa/Content/placeholder-carried-ember.json";

        public static string RepositoryFile(string relative)
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "unity", "Assets")))
                dir = dir.Parent;
            if (dir == null) throw new DirectoryNotFoundException("Repository root with unity/Assets not found above the test directory");
            return Path.Combine(dir.FullName, relative);
        }

        public static Episode ReadPlaceholder() => EpisodeReader.Read(File.ReadAllText(RepositoryFile(PlaceholderEpisode)));

        public static PlayableEpisode Playable(Episode episode)
        {
            var playable = EpisodeValidator.Accept(episode, out var report);
            Assert.That(playable, Is.Not.Null, report.ToString());
            return playable;
        }

        public static Line Line(string id, double seconds = 2, string when = null, params StoryAction[] actions)
        {
            Condition condition = null;
            if (when != null)
            {
                var parts = when.Split(':');
                condition = new Condition(parts[0], parts[1] == "accepted" ? Outcome.Accepted : Outcome.Default);
            }
            return new Line { Id = id, Text = "Text of " + id, Seconds = seconds, When = condition, Actions = actions };
        }

        public static StoryAction Do(ActionKind kind, string objectId) => new StoryAction(kind, objectId);

        /// <summary>
        /// Hearth welcome, an offer with a receive invitation, entry to a story scene with an outcome-dependent
        /// reveal, and a return to the hearth. Valid as built; tests change one thing at a time.
        /// </summary>
        public static Episode Minimal()
        {
            return new Episode
            {
                Schema = 1,
                Id = "test.minimal",
                Version = "1.0.0",
                Title = "Minimal",
                Locale = "en",
                Status = ContentStatus.Reviewed,
                Scenes = new List<SceneDef>
                {
                    new SceneDef { Id = "hearth", Kind = SceneKind.Hearth },
                    new SceneDef { Id = "yard", Kind = SceneKind.Story },
                },
                Objects = new List<StoryObject>
                {
                    new StoryObject { Id = "ember", Scene = "hearth", Capabilities = new[] { ActionKind.Offer, ActionKind.Place } },
                    new StoryObject { Id = "lantern", Scene = "yard", Capabilities = new[] { ActionKind.Reveal, ActionKind.Illuminate }, Continues = "ember" },
                    new StoryObject { Id = "bowl", Scene = "yard", Capabilities = new[] { ActionKind.Reveal, ActionKind.Illuminate } },
                },
                Welcome = new List<Line> { Line("w1"), Line("w2") },
                Beats = new List<Beat>
                {
                    new Beat
                    {
                        Id = "offer",
                        Scene = "hearth",
                        Lines = new List<Line> { Line("o1"), Line("o2", 2, null, Do(ActionKind.Offer, "ember")) },
                        Invitation = new Invitation
                        {
                            Id = "take",
                            Kind = InvitationKind.Receive,
                            Object = "ember",
                            Alternative = "Take the ember",
                            WaitSeconds = 10,
                            Accepted = new List<Line> { Line("o.yes") },
                            Default = new List<Line> { Line("o.no", 2, null, Do(ActionKind.Place, "ember")) },
                        },
                    },
                    new Beat
                    {
                        Id = "story",
                        Scene = "yard",
                        Lines = new List<Line>
                        {
                            Line("s1", 2, null, Do(ActionKind.Reveal, "bowl")),
                            Line("s.carried", 2, "take:accepted", Do(ActionKind.Illuminate, "bowl")),
                            Line("s.placed", 2, "take:default", Do(ActionKind.Illuminate, "bowl")),
                        },
                    },
                    new Beat { Id = "home", Scene = "hearth", Lines = new List<Line> { Line("h1") } },
                },
                EarlyReturn = new List<Line> { Line("early") },
                Keepsake = "ember",
            };
        }

        public static IEnumerable<string> Codes(ValidationReport report)
        {
            foreach (var issue in report.Errors) yield return issue.Code;
        }
    }
}
