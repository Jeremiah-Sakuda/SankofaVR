using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sankofa.Core.Story;
using Sankofa.Core.Visits;
using NUnit.Framework;

namespace Sankofa.Tests
{
    static class Fixtures
    {
        public const string PlaceholderPackage = "dotnet/Sankofa.Tests/Fixtures/placeholder-hearth-stories.json";
        public const string EnkareNairobiPackage = "unity/Assets/Sankofa/Content/enkare-nairobi.json";

        public static string RepositoryFile(string relative)
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "unity", "Assets")))
                dir = dir.Parent;
            if (dir == null) throw new DirectoryNotFoundException("Repository root with unity/Assets not found above the test directory");
            return Path.Combine(dir.FullName, relative);
        }

        public static StoryPackage ReadPlaceholder() => StoryReader.ReadPackage(File.ReadAllText(RepositoryFile(PlaceholderPackage)));
        public static StoryPackage ReadEnkareNairobi() => StoryReader.ReadPackage(File.ReadAllText(RepositoryFile(EnkareNairobiPackage)));

        public static PlayablePackage Playable(StoryPackage package)
        {
            var playable = PackageValidator.Accept(package, out var report);
            Assert.That(playable, Is.Not.Null, report.ToString());
            return playable;
        }

        public static PlayablePlan Playable(StoryPlan plan)
        {
            var playable = PlanValidator.Accept(plan, out var report);
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

        public static StoryAction Do(ActionKind kind, string objectId, string at = null) => new StoryAction(kind, objectId, at);

        /// <summary>
        /// An offer with a receive invitation at the hearth, entry to a story scene with an outcome-dependent
        /// reveal, and a return to the hearth with the ember as keepsake. Valid as built; tests change one thing at a time.
        /// </summary>
        public static StoryPlan Minimal()
        {
            return new StoryPlan
            {
                Schema = 2,
                Package = "test.minimal",
                Version = "1.0.0",
                Route = "test",
                Label = "Test",
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
                    new StoryObject { Id = "ember", Scene = "hearth", Capabilities = new[] { ActionKind.Offer, ActionKind.Place, ActionKind.Withdraw } },
                    new StoryObject { Id = "lantern", Scene = "yard", Capabilities = new[] { ActionKind.Reveal, ActionKind.Illuminate }, Continues = "ember" },
                    new StoryObject { Id = "bowl", Scene = "yard", Capabilities = new[] { ActionKind.Reveal, ActionKind.Illuminate, ActionKind.Touch } },
                },
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

        /// <summary>A stage that has only the listed scenes and objects.</summary>
        public sealed class Inventory : IStageInventory
        {
            readonly HashSet<string> scenes;
            readonly HashSet<string> objects;

            public Inventory(IEnumerable<string> scenes, IEnumerable<string> objects)
            {
                this.scenes = new HashSet<string>(scenes);
                this.objects = new HashSet<string>(objects);
            }

            public bool HasScene(string sceneId) => scenes.Contains(sceneId);
            public bool HasObject(string objectId, string sceneId) => objects.Contains(objectId);
        }
    }

    /// <summary>Kept visits held as their JSON text, so every save and load goes through the real codec.</summary>
    sealed class MemoryStore : IVisitStore
    {
        public readonly Dictionary<string, string> Files = new Dictionary<string, string>();
        public readonly List<string> Problems = new List<string>();
        public bool FailSaves;
        public bool FailDeletes;

        public VisitStoreContents Load() => new VisitStoreContents(Files.Values.Select(SavedVisitCodec.Read).ToList(), Problems);

        public void Save(SavedVisit visit)
        {
            if (FailSaves) throw new VisitStoreException("The visit could not be kept: test storage is full");
            Files[visit.Id] = SavedVisitCodec.Write(visit);
        }

        public void Replace(SavedVisit visit)
        {
            if (FailSaves) throw new VisitStoreException("The visit could not be kept: test storage is full");
            Files.Clear();
            Files[visit.Id] = SavedVisitCodec.Write(visit);
        }

        public void Delete(string id)
        {
            if (FailDeletes) throw new VisitStoreException("The visit could not be forgotten: test storage is locked");
            Files.Remove(id);
        }
    }
}
