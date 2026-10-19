using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Sankofa.Core.Personal;
using Sankofa.Flow;
using Sankofa.Runtime.Personal;
using Sankofa.Runtime.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Sankofa.Tests
{
    /// <summary>Actual scene/app/panel integration with synthetic pointer selections; desktop software evidence only.</summary>
    public sealed class PersonalStoryAppTests
    {
        PersonalStoryApp app;
        StoryTableView view;
        string directory;
        readonly List<string> unexpectedErrors = new List<string>();

        [UnitySetUp]
        public IEnumerator LoadActualTableWithIsolatedShelf()
        {
            unexpectedErrors.Clear();
            // Collect and fail every application error ourselves so one known no-headset SDK error can be
            // tolerated without hiding null references, app exceptions or unrelated native errors.
            LogAssert.ignoreFailingMessages = true;
            Application.logMessageReceived += CollectError;
            directory = Path.Combine(Path.GetTempPath(), "sankofa-app-test-" + Guid.NewGuid().ToString("N"));
            PersonalStoryApp.StoreFolderOverride = directory;
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/Sankofa/Scenes/StoryTable.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene("StoryTable");
#endif
            yield return null;
            yield return null;
            app = UnityEngine.Object.FindFirstObjectByType<PersonalStoryApp>();
            Assert.That(app, Is.Not.Null, "the generated scene must compose the actual root application");
            view = app.View;
            Assert.That(view, Is.Not.Null);
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Intake));
            Assert.That(app.HasKept, Is.False, "tests cannot read a visitor's real shelf");
            Assert.That(app.NarrationEnabled, Is.False);
            app.ServiceEndpoint = "invalid-endpoint"; // No test is allowed to contact a configured live service.
        }

        [UnityTearDown]
        public IEnumerator CleanIsolatedShelfAndCheckActualErrors()
        {
            var loaded = SceneManager.GetSceneByName("StoryTable");
            if (loaded.IsValid() && loaded.isLoaded)
            {
                var empty = SceneManager.CreateScene("Personal app test cleanup " + Guid.NewGuid().ToString("N"));
                SceneManager.SetActiveScene(empty);
                yield return SceneManager.UnloadSceneAsync(loaded);
            }
            yield return null;
            PersonalStoryApp.StoreFolderOverride = null;
            if (directory != null && Directory.Exists(directory)) Directory.Delete(directory, true);
            Application.logMessageReceived -= CollectError;
            LogAssert.ignoreFailingMessages = false;
            Assert.That(unexpectedErrors, Is.Empty, string.Join("\n", unexpectedErrors));
        }

        void CollectError(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            if (type == LogType.Error && message == "Unable to process a controller whose SampleRateHz is 0 now.") return;
            unexpectedErrors.Add(type + ": " + message + "\n" + stack);
        }

        PokePanel Panel(string name) => view.Panels.Single(p => p.name == name);
        IEnumerator Press(PokePanel panel, string id)
        {
            Assert.That(panel.gameObject.activeInHierarchy && !panel.Hidden, Is.True, panel.name + " must be visible and usable");
            Assert.That(panel.Ids, Does.Contain(id), panel.name);
            yield return new WaitForSecondsRealtime(.33f);
            panel.PressFromPointer(id);
            yield return null;
            yield return null;
        }
        IEnumerator Press(string panel, string id) => Press(Panel(panel), id);
        IEnumerator ReachEnding()
        {
            for (var step = 0; step < 40 && app.Session.State == PersonalStoryState.Reading; step++)
                yield return Press("Turn a page", "next");
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Closing), "all reading parts and chapters have a complete ending");
        }

        [UnityTest]
        public IEnumerator ActualPreparedButtonsProduceThreeDifferentDevelopments()
        {
            var openings = new HashSet<string>(); var contexts = new HashSet<string>();
            foreach (var interest in PersonalStoryCatalog.Interests)
            {
                app.Session.Cancel(); app.SetSeed("");
                yield return Press("Interest choices", interest);
                yield return Press("Start choices", "local");
                Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Reading));
                Assert.That(app.Session.Story.Interest, Is.EqualTo(interest));
                Assert.That(app.Session.Story.Mode, Is.EqualTo("bundled"));
                Assert.That(view.CapabilityLabel, Does.Contain("Prepared"));
                openings.Add(app.Session.Story.Chapters[0].Narration);
                contexts.Add(app.Session.Story.Chapters[1].Narration);
                Assert.That(view.Caption, Is.Not.Empty);
                Assert.That(app.HasKept, Is.False, "starting and reading never save automatically");
            }
            Assert.That(openings.Count, Is.EqualTo(3));
            Assert.That(contexts.Count, Is.EqualTo(3), "differences occur before the ending");
        }

        [UnityTest]
        public IEnumerator FreeformQuestionChangesPreparedThreadWithoutAssumingFamilyIdentity()
        {
            yield return Press("Interest choices", "journeys");
            app.SetSeed("What could a household carry in a gourd?");
            yield return Press("Start choices", "local");
            Assert.That(app.Session.Story.Interest, Is.EqualTo("trade-household"));
            var trade = app.Session.Story.Chapters[0].Narration;
            app.Session.Cancel(); app.SetSeed("What does the river name mean?");
            yield return Press("Start choices", "local");
            Assert.That(app.Session.Story.Interest, Is.EqualTo("place-water"));
            Assert.That(app.Session.Story.Chapters[0].Narration, Is.Not.EqualTo(trade));
            Assert.That(PersonalStoryJson.Write(app.Session.Story), Does.Not.Contain("What does the river name mean?"));
        }

        [UnityTest]
        public IEnumerator ContextQuestionsSourcesAndPauseReturnToTheSameReadingPart()
        {
            app.StartPrepared(); yield return null;
            var chapter = app.Session.Chapter.Id;
            if (view.ReadingPartCount > 1) yield return Press("Turn a page", "next");
            var part = view.ReadingPart; var caption = view.Caption;
            yield return Press("Questions beside the story", "0");
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Question));
            var answer = app.Session.Question.Answer;
            yield return Press("Return and recovery actions", "sources");
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Sources));
            yield return Press("Return and recovery actions", "continue");
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Question));
            Assert.That(app.Session.Question.Answer, Is.EqualTo(answer));
            yield return Press("Return and recovery actions", "continue");
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Reading));
            Assert.That(app.Session.Chapter.Id, Is.EqualTo(chapter));
            Assert.That(view.ReadingPart, Is.EqualTo(part));
            Assert.That(view.Caption, Is.EqualTo(caption));
        }

        [UnityTest]
        public IEnumerator PauseBlocksStoryActionsAndTheSceneStartsSilently()
        {
            app.StartPrepared(); yield return null;
            var json = PersonalStoryJson.Write(app.Session.Story); var chapter = app.Session.ChapterIndex;
            yield return Press("Always available controls", "pause");
            Assert.That(app.Session.Paused, Is.True);
            Assert.That(Panel("Turn a page").Hidden, Is.True);
            Assert.That(app.Session.Next(), Is.False);
            Assert.That(app.Session.Ask(0), Is.False);
            app.StartPrepared();
            Assert.That(app.Reopen(), Is.False);
            Assert.That(app.Session.ChapterIndex, Is.EqualTo(chapter));
            Assert.That(PersonalStoryJson.Write(app.Session.Story), Is.EqualTo(json));
            Assert.That(app.GetComponents<AudioSource>().All(a => !a.isPlaying && a.volume == 0), Is.True);
            yield return Press("Always available controls", "pause");
            Assert.That(app.Session.Paused, Is.False);
            Assert.That(Panel("Turn a page").Hidden, Is.False);
        }

        [UnityTest]
        public IEnumerator KeepReplacementReopenAndForgetRequireExplicitPanelActions()
        {
            app.StartPrepared(); yield return null;
            var first = PersonalStoryJson.Write(app.Session.Story);
            yield return ReachEnding();
            yield return Press("Turn a page", "back");
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Reading), "the last page remains readable before deciding to Keep");
            Assert.That(PersonalStoryJson.Write(app.Session.Story), Is.EqualTo(first));
            yield return ReachEnding();
            Assert.That(app.HasKept, Is.False);
            Assert.That(File.Exists(Path.Combine(directory, "kept.story")), Is.False);
            yield return Press("Turn a page", "save");
            Assert.That(app.HasKept, Is.True);
            Assert.That(File.ReadAllText(Path.Combine(directory, "kept.story")), Is.EqualTo(first));
            yield return Press("Turn a page", "again");
            Assert.That(view.IntakeVisible, Is.True);
            var kept = view.Panels.Single(p => p.Ids.Contains("reopen") && p.Ids.Contains("forget"));
            Assert.That(kept.Hidden, Is.False, "the saved telling must be discoverable on intake");
            yield return Press("Always available controls", "pause");
            Assert.That(kept.Hidden, Is.True, "saved-story actions must not bypass pause");
            Assert.That(app.Reopen(), Is.False);
            app.Forget();
            Assert.That(Panel("Return and recovery actions").Hidden, Is.True, "Forget cannot open a decision while paused");
            yield return Press("Always available controls", "pause");
            yield return Press(kept, "reopen");
            Assert.That(app.Session.IsReplay, Is.True);
            Assert.That(PersonalStoryJson.Write(app.Session.Story), Is.EqualTo(first));
            Assert.That(view.CapabilityLabel, Does.Contain("no new generation"));
            yield return Press("Always available controls", "leave");
            yield return Press("Return and recovery actions", "leave");
            yield return Press("Interest choices", "place-water");
            yield return Press("Start choices", "local");
            var second = PersonalStoryJson.Write(app.Session.Story);
            Assert.That(second, Is.Not.EqualTo(first));
            yield return ReachEnding();
            yield return Press("Turn a page", "save");
            Assert.That(File.ReadAllText(Path.Combine(directory, "kept.story")), Is.EqualTo(first), "replacement is not automatic");
            yield return Press("Return and recovery actions", "cancel");
            Assert.That(File.ReadAllText(Path.Combine(directory, "kept.story")), Is.EqualTo(first));
            yield return Press("Turn a page", "save");
            yield return Press("Return and recovery actions", "replace");
            Assert.That(File.ReadAllText(Path.Combine(directory, "kept.story")), Is.EqualTo(second));
            yield return Press("Turn a page", "again");
            yield return Press(kept, "forget");
            Assert.That(app.HasKept, Is.True, "forgetting requires its own confirmation");
            yield return Press("Return and recovery actions", "cancel");
            Assert.That(app.HasKept, Is.True);
            yield return Press(kept, "forget");
            yield return Press("Return and recovery actions", "forget");
            Assert.That(app.HasKept, Is.False);
            Assert.That(app.Reopen(), Is.False);
            Assert.That(kept.Hidden, Is.True);
        }

        [UnityTest]
        public IEnumerator KeyboardCancelAndCommitKeepTextLocalWithoutStartingARequest()
        {
            app.SetSeed("a question");
            var version = app.Session.RequestVersion;
            yield return Press("Start choices", "edit");
            Assert.That(app.Keyboard.IsOpen, Is.True);
            yield return Press(app.Keyboard.Panel, "clear");
            yield return Press(app.Keyboard.Panel, "key:x");
            yield return Press(app.Keyboard.Panel, "cancel");
            Assert.That(app.Seed, Is.EqualTo("a question"));
            Assert.That(app.Session.RequestVersion, Is.EqualTo(version));
            Assert.That(view.IntakeVisible, Is.True);
            yield return Press("Start choices", "edit");
            yield return Press(app.Keyboard.Panel, "clear");
            yield return Press(app.Keyboard.Panel, "key:w");
            yield return Press(app.Keyboard.Panel, "done");
            Assert.That(app.Seed, Is.EqualTo("w"));
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Intake));
            Assert.That(app.HasKept, Is.False);
            Assert.That(Directory.Exists(directory), Is.False, "typing alone does not persist personal text");
        }

        [UnityTest]
        public IEnumerator OnlineFailurePreservesSeedAndOffersActualPreparedRecovery()
        {
            app.SetSeed("What does the river name mean?");
            var version = app.Session.RequestVersion;
            yield return Press("Start choices", "create");
            Assert.That(app.Session.RequestVersion, Is.EqualTo(version), "opening consent sends nothing");
            yield return Press("Online consent actions", "create");
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Error));
            Assert.That(app.Seed, Is.EqualTo("What does the river name mean?"));
            Assert.That(Panel("Return and recovery actions").Ids, Does.Contain("local"));
            yield return Press("Return and recovery actions", "local");
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Reading));
            Assert.That(app.Session.Story.Mode, Is.EqualTo("bundled"));
            Assert.That(app.Session.Story.Interest, Is.EqualTo("place-water"));
        }

        [UnityTest]
        public IEnumerator LocalShelfFailuresNeverOfferAnOnlineGenerationRetry()
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "kept.story"), "damaged synthetic shelf");
            app.SetSeed("Synthetic private question that has not been approved for export");
            app.Session.Cancel(); yield return null;
            var shelf = view.Panels.Single(p => p.Ids.Contains("reopen") && p.Ids.Contains("forget"));
            yield return Press(shelf, "reopen");
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Intake));
            Assert.That(view.ModalText, Does.Contain("No story request will be sent"));
            Assert.That(Panel("Return and recovery actions").Ids, Does.Not.Contain("retry"));
            yield return Press("Return and recovery actions", "shelf-retry");
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Intake));
            yield return Press("Return and recovery actions", "shelf-return");
            Directory.CreateDirectory(Path.Combine(directory, "kept.story.tmp")); // Deliberate local write failure.
            yield return Press(shelf, "forget");
            yield return Press("Return and recovery actions", "forget");
            Assert.That(view.ModalText, Does.Contain("No story request will be sent"));
            Assert.That(Panel("Return and recovery actions").Ids, Does.Not.Contain("retry"));
            yield return Press("Return and recovery actions", "shelf-return");
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Intake));
            Assert.That(app.Seed, Does.Contain("not been approved"));
        }

        [UnityTest]
        public IEnumerator AcceptedGeneratedDtoRendersWithItsOriginalCapabilityLabel()
        {
            // Adapter injection exercises accepted rendering/state, not HTTP or proof of model generation.
            var request = new PersonalStoryRequest("enkare-nairobi", "1850s", "journeys");
            var dto = BundledPersonalStoryComposer.Compose(request).Story;
            dto.Mode = "generated"; dto.Id = "synthetic.generated-adapter-check";
            dto.Chapters[0].Narration = PersonalStoryCatalog.ReconstructionPrefix + string.Join(" ", Enumerable.Repeat("A fictional traveller waits beside the water and wonders which path to take next.", 14));
            var accepted = PersonalStoryJson.ReadValidated(PersonalStoryJson.Write(dto), request);
            var version = app.Session.Begin(request);
            Assert.That(app.Session.Accept(version, accepted), Is.True);
            yield return null;
            Assert.That(view.IntakeVisible, Is.False);
            Assert.That(view.CapabilityLabel, Does.Contain("Generated telling"));
            Assert.That(view.Caption, Is.Not.Empty);
            Assert.That(app.HasKept, Is.False);
            Assert.That(view.ReadingPartCount, Is.GreaterThan(1));
            yield return Press("Turn a page", "next");
            Assert.That(view.ReadingPart, Is.EqualTo(1));
            yield return Press("Always available controls", "leave");
            yield return Press("Return and recovery actions", "cancel");
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Reading));
            Assert.That(app.Session.Story.Id, Is.EqualTo(accepted.Id));
            Assert.That(view.ReadingPart, Is.EqualTo(1), "cancelling a leave decision preserves the current reading part");
            yield return Press("Always available controls", "leave");
            yield return Press("Return and recovery actions", "leave");
            version = app.Session.Begin(request);
            Assert.That(app.Session.Accept(version, accepted), Is.True);
            Assert.That(view.ReadingPart, Is.Zero, "a new visit begins at its first reading part");
        }
    }

    /// <summary>
    /// Explicit opt-in Unity HTTP/model smoke. Kept outside the regular PersonalStoryAppTests filter so
    /// the main software suite has no skipped cases and never sends input to a live provider.
    /// </summary>
    [Category("LiveService")]
    public sealed class PersonalTableLiveHttpTests
    {
        PersonalStoryApp app;
        string directory;
        readonly List<string> errors = new List<string>();

        [UnitySetUp]
        public IEnumerator LoadMutedSceneOnlyForExplicitLiveRun()
        {
            if (Environment.GetEnvironmentVariable("SANKOFA_TEST_LIVE_SERVICE") != "1")
                Assert.Ignore("Live service requests require explicit SANKOFA_TEST_LIVE_SERVICE=1.");
            errors.Clear();
            LogAssert.ignoreFailingMessages = true;
            Application.logMessageReceived += Error;
            directory = Path.Combine(Path.GetTempPath(), "sankofa-live-app-test-" + Guid.NewGuid().ToString("N"));
            PersonalStoryApp.StoreFolderOverride = directory;
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/Sankofa/Scenes/StoryTable.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene("StoryTable");
#endif
            yield return null; yield return null;
            app = UnityEngine.Object.FindFirstObjectByType<PersonalStoryApp>();
            Assert.That(app, Is.Not.Null);
            app.ServiceEndpoint = "http://127.0.0.1:8787";
            Assert.That(app.NarrationEnabled, Is.False);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            var loaded = SceneManager.GetSceneByName("StoryTable");
            if (directory != null && loaded.IsValid() && loaded.isLoaded)
            {
                var empty = SceneManager.CreateScene("Live app cleanup " + Guid.NewGuid().ToString("N"));
                SceneManager.SetActiveScene(empty);
                yield return SceneManager.UnloadSceneAsync(loaded);
            }
            yield return null;
            PersonalStoryApp.StoreFolderOverride = null;
            if (directory != null && Directory.Exists(directory)) Directory.Delete(directory, true);
            Application.logMessageReceived -= Error;
            LogAssert.ignoreFailingMessages = false;
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }

        void Error(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            if (type == LogType.Error && message == "Unable to process a controller whose SampleRateHz is 0 now.") return;
            errors.Add(type + ": " + message + "\n" + stack);
        }

        [UnityTest]
        public IEnumerator ActualHttpWithSameInterestAndDifferentQuestionOnlySeedsChangesReconstructedActions()
        {
            var stories = new List<PersonalStoryDocument>();
            var route = app.View.Panels.Single(p => p.name == "Interest choices");
            yield return new WaitForSecondsRealtime(.33f);
            route.PressFromPointer("journeys");
            yield return null; yield return null;
            // Public synthetic scenarios only. The app's Request() places Seed in question and leaves fragment empty.
            var questions = new[]
            {
                "An imagined traveller waits for a neighbour along a path. What might make the traveller choose to stay?",
                "An imagined trader has a flour shortage. How might she explain the shortfall to a neighbour?",
            };
            foreach (var question in questions)
            {
                app.Session.Cancel();
                app.SetSeed(question);
                app.StartGenerated(); // Actual root request, UnityWebRequest and response validation; no injected DTO.
                var deadline = Time.realtimeSinceStartup + 45f;
                while (app.Session.State == PersonalStoryState.Preparing && Time.realtimeSinceStartup < deadline)
                    yield return new WaitForSecondsRealtime(.1f);
                Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Reading), "live HTTP request must finish successfully: " + app.Session.Problem);
                Assert.That(app.Session.Story.Mode, Is.EqualTo("generated"), "an explicit bundled fallback does not satisfy this live generation smoke");
                Assert.That(app.Session.Story.Interest, Is.EqualTo("journeys"), "only the question changes between requests");
                Assert.That(PersonalStoryValidator.Validate(app.Session.Story).IsValid, Is.True);
                Assert.That(app.View.CapabilityLabel, Does.Contain("Generated telling"));
                Assert.That(app.View.Caption, Is.Not.Empty);
                Assert.That(app.NarrationEnabled, Is.False);
                Assert.That(app.GetComponents<AudioSource>().All(a => !a.isPlaying && a.volume == 0), Is.True);
                Assert.That(app.HasKept, Is.False);
                stories.Add(app.Session.Story);
                // These opt-in scenarios contain no visitor material. Preserve their realised responses locally
                // for human comparison of the actions, without printing prompts or provider payloads to logs.
                var evidence = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "artifacts", "story-table"));
                Directory.CreateDirectory(evidence);
                File.WriteAllText(Path.Combine(evidence, "live-question-only-" + stories.Count + ".json"), PersonalStoryJson.Write(app.Session.Story));
            }
            var first = stories[0].Chapters.Where(c => c.Truth == "reconstructed").Select(c => c.Narration).ToArray();
            var second = stories[1].Chapters.Where(c => c.Truth == "reconstructed").Select(c => c.Narration).ToArray();
            Assert.That(first.Length, Is.GreaterThanOrEqualTo(2));
            Assert.That(second.Length, Is.EqualTo(first.Length));
            Assert.That(first[0], Is.Not.EqualTo(second[0]), "the first reconstructed action changes before the ending");
            Assert.That(first[1], Is.Not.EqualTo(second[1]), "the developed scene also changes");
            Assert.That(stories[0].Id, Is.Not.EqualTo(stories[1].Id));
            // Exercise the actual app narration transport while remaining muted. This primes an in-memory
            // clip; it does not enable narration or call AudioSource.Play. The live server requires the same
            // ephemeral access token for generation and narration during this smoke.
            var fetch = typeof(PersonalStoryApp).GetMethod("FetchVoice", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var routine = (IEnumerator)fetch.Invoke(app, new object[] { "synthetic-audio-smoke", app.ServiceEndpoint, app.Session.RequestVersion, app.Session.Chapter.Id });
            yield return app.StartCoroutine(routine);
            var cache = (Dictionary<string, AudioClip>)typeof(PersonalStoryApp).GetField("clips", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(app);
            Assert.That(cache.ContainsKey("synthetic-audio-smoke"), Is.True, "authenticated actual narration transport must return a decodable clip");
            Assert.That(app.NarrationEnabled, Is.False);
            Assert.That(app.GetComponents<AudioSource>().All(a => !a.isPlaying && a.volume == 0), Is.True);
        }
    }
}
