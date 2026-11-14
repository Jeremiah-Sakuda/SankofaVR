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
        public IEnumerator EachHeirloomBeginsADifferentPreparedTelling()
        {
            var openings = new HashSet<string>(); var contexts = new HashSet<string>();
            foreach (var interest in PersonalStoryCatalog.Interests)
            {
                app.Session.Cancel(); app.SetSeed(""); yield return null;
                // Without a story service, touching an heirloom is the beginning: one action, no second button.
                yield return Press("Interest choices", interest);
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
        public IEnumerator TheGriotWelcomesAFirstVisitAndChoosingNeverWaitsForIt()
        {
            Assert.That(app.Welcoming, Is.False, "editor and batch sessions start on the standing invitation");
            Assert.That(view.Caption, Is.EqualTo(StoryTableView.StandingInvitation));
            Time.timeScale = 6;
            try
            {
                app.BeginWelcome(); yield return null;
                Assert.That(app.Welcoming, Is.True);
                Assert.That(Panel("Interest choices").Hidden, Is.False, "an heirloom can be chosen while the griot is still speaking");
                var seen = new List<string>();
                for (var waited = 0f; waited < 30f && app.Welcoming; waited += Time.unscaledDeltaTime)
                {
                    if (seen.Count == 0 || seen[seen.Count - 1] != view.Caption) seen.Add(view.Caption);
                    Assert.That(view.Caption, Is.EqualTo(app.WelcomeLine), "the page shows the line being said");
                    yield return null;
                }
                Assert.That(seen, Is.EqualTo(PersonalStoryApp.FirstWelcome), "every line is shown once, in order");
                Assert.That(string.Join(" ", seen), Does.Contain("imagined").And.Contain("documented"), "the disclosure is part of the welcome");
                Assert.That(view.Caption, Is.EqualTo(StoryTableView.StandingInvitation), "the invitation to choose remains once the welcome is said");

                app.BeginWelcome(); yield return null;
                yield return Press("Interest choices", "trade-household");
                yield return null;
                Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Reading), "choosing during the welcome begins the telling at once");
                Assert.That(app.Welcoming, Is.False);
                Assert.That(view.Caption, Does.Not.Contain("Sit with me"));
            }
            finally { Time.timeScale = 1; }
        }

        [UnityTest]
        public IEnumerator PagesTurnThemselvesOnceToldAndAnUnansweredChoiceGoesToTheFire()
        {
            Assert.That(app.TurnsPages, Is.False, "editor and batch sessions are hand-turned unless the presentation is asked for");
            app.TurnsPages = true;
            Time.timeScale = 8;
            try
            {
                Assert.That(Panel("Start choices").Ids, Does.Not.Contain("local"), "an heirloom begins the telling; the page offers no second start");
                yield return Press("Interest choices", "journeys");
                Assert.That(Panel("Turn a page").Ids, Has.None.EqualTo("next").And.None.EqualTo("back"), "self-turning pages carry no page-turning buttons");
                var pages = new List<string>(); var part = 0; var parts = 1; var choiceAtFigures = false; var otherSideAtHerFigure = false;
                for (var waited = 0f; waited < 90f && app.Session.State == PersonalStoryState.Reading; waited += Time.unscaledDeltaTime)
                {
                    var id = app.Session.Chapter.Id;
                    if (pages.Count == 0 || pages[pages.Count - 1] != id)
                    {
                        if (pages.Count > 0) Assert.That(part, Is.EqualTo(parts - 1), "every part of " + pages[pages.Count - 1] + " was shown before it turned");
                        pages.Add(id);
                    }
                    part = view.ReadingPart; parts = view.ReadingPartCount;
                    if (app.Session.OffersChoice && !Panel("Follow wambui").Hidden)
                    {
                        choiceAtFigures = true;
                        Assert.That(Panel("Questions beside the story").Ids, Is.Empty, "the choice stands at the figures, not twice");
                    }
                    if (app.OtherSideOffered && !Panel("Follow wambui").Hidden)
                    {
                        // Once the waiting woman's side is told, the late woman's side is offered at her figure.
                        otherSideAtHerFigure = true;
                        Assert.That(id, Is.EqualTo("journeys.meeting"));
                        Assert.That(view.Stage.Progress, Is.EqualTo(1f), "a side is told to its end before the other is offered");
                        Assert.That(Panel("Follow wambui").Ids, Is.EqualTo(new[] { "choice:0" }));
                        Assert.That(Panel("Follow naserian").Hidden, Is.True, "the side already heard is not offered again");
                        Assert.That(view.Caption, Does.EndWith("turn it."), "the page says how the stage is turned");
                        Assert.That(Panel("Turn a page").Ids, Does.Contain("next"), "a visitor may end here rather than hear the other side");
                    }
                    yield return null;
                }
                Assert.That(choiceAtFigures, Is.True, "both sides were offered at the figures before the fire chose");
                Assert.That(otherSideAtHerFigure, Is.True, "the unheard side was offered before the fire turned the stage");
                Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Closing), "a visitor who only watches reaches the ending");
                Assert.That(pages, Is.EqualTo(new[] { "journeys.arrival", "journeys.context", "journeys.meeting", "journeys.late" }), "left alone, the fire turns the stage and both sides are heard");
                Assert.That(app.Session.Heard.Select(side => side.Id), Is.EqualTo(new[] { "journeys.follow.waiting", "journeys.follow.late" }));
                Assert.That(app.Session.FollowedByChoice, Is.False, "an unanswered choice is the fire's, never recorded as the visitor's");
                Assert.That(view.Caption, Does.StartWith("You heard both sides of this meeting."));
                Assert.That(app.HasKept, Is.False, "watching never saves");
                yield return new WaitForSecondsRealtime(1f);
                Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Closing), "the ending waits for the visitor");

                // A pause holds the page: nothing turns while the visitor is away.
                app.Session.Cancel(); yield return null;
                yield return Press("Interest choices", "journeys");
                var first = app.Session.Chapter.Id;
                yield return Press("Always available controls", "pause");
                yield return new WaitForSecondsRealtime(4f);
                Assert.That(app.Session.Chapter.Id, Is.EqualTo(first));
                Assert.That(app.Session.PageNumber, Is.EqualTo(1));
                yield return Press("Always available controls", "pause");
            }
            finally { Time.timeScale = 1; }
        }

        [UnityTest]
        public IEnumerator KeepingThreadsABeadAndTheGriotNamesAnUnheardSideOnReturn()
        {
            Assert.That(app.Strand.Count, Is.Zero);
            Assert.That(view.StrandKept, Is.Zero);
            yield return Press("Interest choices", "journeys");
            yield return ReachEnding();
            Assert.That(app.Strand.Count, Is.Zero, "reading to the end threads nothing; only Keep does");
            yield return Press("Turn a page", "save");
            Assert.That(app.HasKept, Is.True);
            Assert.That(app.Strand.Beads.Select(b => b.ToString()), Is.EqualTo(new[] { "journeys/journeys.follow.waiting" }), "the fire's side earns a bead once the visitor keeps it");
            Assert.That(view.StrandKept, Is.EqualTo(1));
            Assert.That(view.Caption, Does.Contain("1 of 6"));
            Assert.That(new Sankofa.Storage.PersonalStrandStore(directory).Load().Count, Is.EqualTo(1), "the strand is on the device, not only in memory");

            yield return Press("Turn a page", "again");
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Intake));
            Time.timeScale = 6;
            try
            {
                app.BeginWelcome(); yield return null;
                Assert.That(view.Caption, Is.EqualTo("You came back. Sit with me."));
                var seen = new List<string>();
                for (var waited = 0f; waited < 30f && app.Welcoming; waited += Time.unscaledDeltaTime)
                {
                    if (seen.Count == 0 || seen[seen.Count - 1] != view.Caption) seen.Add(view.Caption);
                    yield return null;
                }
                Assert.That(seen, Is.EqualTo(new[] { "You came back. Sit with me.", "Choose the bundle, and you could find the one who is late.", StoryTableView.StandingInvitation }));
            }
            finally { Time.timeScale = 1; }

            // Keeping the same side again adds no second bead; forgetting removes the telling and the strand.
            yield return Press("Kept story choices", "reopen");
            yield return ReachEnding();
            yield return Press("Turn a page", "save");
            yield return Press("Return and recovery actions", "replace");
            Assert.That(app.Strand.Count, Is.EqualTo(1));
            yield return Press("Turn a page", "again");
            yield return Press("Kept story choices", "forget");
            yield return Press("Return and recovery actions", "forget");
            Assert.That(app.HasKept, Is.False);
            Assert.That(app.Strand.Count, Is.Zero);
            Assert.That(view.StrandKept, Is.Zero);
            Assert.That(new Sankofa.Storage.PersonalStrandStore(directory).Load().Count, Is.Zero);
        }

        /// <summary>A hand whose palm is at a point in the world, fingers forward, as a tracked hand would report it.</summary>
        static Sankofa.Interaction.HandSample PalmAt(Vector3 palm)
        {
            System.Numerics.Vector3 N(Vector3 v) => new System.Numerics.Vector3(v.x, v.y, v.z);
            return new Sankofa.Interaction.HandSample
            {
                Wrist = N(palm + new Vector3(0, 0, -0.045f)), MiddleProximal = N(palm + new Vector3(0, 0, 0.045f)),
                IndexProximal = N(palm + new Vector3(0.025f, 0, 0.04f)), LittleProximal = N(palm + new Vector3(-0.03f, 0, 0.035f)),
                IndexTip = N(palm + new Vector3(0.03f, 0, 0.12f)), MiddleTip = N(palm + new Vector3(0, 0, 0.13f)), RingTip = N(palm + new Vector3(-0.015f, 0, 0.125f)),
            };
        }
        static Sankofa.Runtime.Input.HandSource AHand() => UnityEngine.Object.FindObjectsByType<Sankofa.Runtime.Input.HandSource>(FindObjectsSortMode.None).First();
        IEnumerator Until(Func<bool> met, float seconds)
        {
            for (var deadline = Time.realtimeSinceStartup + seconds; !met() && Time.realtimeSinceStartup < deadline;) yield return null;
        }

        [UnityTest]
        public IEnumerator OnceASideIsToldAHandSweptAcrossTheStageTurnsItToTheOther()
        {
            app.TurnsPages = true;
            view.SetReducedMotion(false);
            var stage = view.Stage; var hand = AHand();
            // A point in the stage's own space: x toward the visitor's right, y up, z away, from the turntable's centre.
            Vector3 OverStage(float x, float y, float z) => stage.transform.parent.TransformPoint(stage.transform.localPosition + new Vector3(x, y, z));
            Func<Sankofa.Interaction.HandSample> Sweep(float from, float to, float seconds)
            {
                var began = Time.time;
                return () => PalmAt(OverStage(Mathf.Lerp(from, to, Mathf.Clamp01((Time.time - began) / seconds)), 0.20f, -0.36f));
            }
            Time.timeScale = 8;
            try
            {
                yield return Press("Interest choices", "trade-household");
                yield return Until(() => app.Session.OffersChoice && !Panel("Follow naserian").Hidden, 30f);
                Assert.That(app.Session.OffersChoice, Is.True);
                yield return Press("Follow naserian", "choice:1");
                Assert.That(app.Session.Chapter.Id, Is.EqualTo("trade-household.expected"));
                Assert.That(app.Session.FollowedByChoice, Is.True);
                Assert.That(app.OtherSideOffered, Is.False, "the other side is not offered while this one is still being told");
                yield return Until(() => stage.Standing, 10f);
                // A sweep made before the side is told turns nothing.
                Time.timeScale = 1;
                hand.Simulated = Sweep(-0.18f, 0.18f, 0.4f);
                yield return new WaitForSeconds(0.6f);
                hand.Simulated = null;
                Assert.That(app.Session.Chapter.Id, Is.EqualTo("trade-household.expected"));
                Assert.That(stage.Turning, Is.False);
                Time.timeScale = 8;
                yield return Until(() => app.OtherSideOffered, 30f);
            }
            finally { Time.timeScale = 1; }
            Assert.That(app.OtherSideOffered, Is.True, "once her side is told, the other woman's is offered");
            yield return null; yield return null;
            Assert.That(view.Caption, Does.EndWith("turn it."));
            Assert.That(Panel("Follow wambui").Hidden, Is.False, "the woman not yet heard can also be chosen at her figure");
            Assert.That(Panel("Follow wambui").Ids, Is.EqualTo(new[] { "choice:0" }));
            Assert.That(Panel("Turn a page").Ids, Does.Contain("next"));

            // Part of a sweep leans the stage with the hand; when the hand stops, the stage settles back and nothing turns.
            hand.Simulated = Sweep(-0.04f, 0.08f, 0.3f);
            yield return Until(() => stage.Lean > 3f, 1.5f);
            Assert.That(stage.Lean, Is.GreaterThan(3f), "the stage leans toward the hand before it gives way");
            yield return Until(() => stage.Lean < 0.2f, 2.5f);
            Assert.That(stage.Lean, Is.LessThan(0.2f), "a hand that stops lets the stage settle");
            Assert.That(app.Session.Chapter.Id, Is.EqualTo("trade-household.expected"));

            // A whole sweep turns it: the stage folds, turns the way the hand went, and rises on the other woman.
            hand.Simulated = Sweep(0.18f, -0.18f, 0.4f);
            yield return Until(() => app.Session.Chapter.Id == "trade-household.agreement", 2f);
            hand.Simulated = null;
            Assert.That(app.Session.Chapter.Id, Is.EqualTo("trade-household.agreement"));
            Assert.That(app.Session.FollowedByChoice, Is.True, "turning the stage by hand is the visitor's own choice");
            Assert.That(app.Session.Heard.Select(side => side.Id), Is.EqualTo(new[] { "trade-household.follow.more", "trade-household.follow.less" }));
            Assert.That(stage.Turning, Is.True);
            Assert.That(app.OtherSideOffered, Is.False, "with both sides heard there is nothing left to turn to");
            Time.timeScale = 8;
            try
            {
                yield return Until(() => !stage.Turning && stage.Rise >= 1, 10f);
                Assert.That(stage.Mirrored, Is.True);
                Assert.That(stage.FigurePlace("wambui").z, Is.LessThan(stage.FigurePlace("naserian").z), "the woman now followed stands nearest the visitor");
                yield return Until(() => app.Session.State == PersonalStoryState.Closing, 40f);
            }
            finally { Time.timeScale = 1; }
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Closing));
            Assert.That(view.Caption, Does.StartWith("You heard both sides of this meeting."));
            var words = view.GetComponentsInChildren<TMPro.TMP_Text>(true).Single(t => t.name == "Narrative captions");
            words.ForceMeshUpdate(true, true);
            Assert.That(words.isTextOverflowing, Is.False, "the ending fits its page");

            // Keeping threads a bead for each side heard in this sitting.
            yield return Press("Turn a page", "save");
            Assert.That(app.Strand.Beads.Select(b => b.ToString()), Is.EqualTo(new[] { "trade-household/trade-household.follow.more", "trade-household/trade-household.follow.less" }));
            Assert.That(view.StrandKept, Is.EqualTo(2));
            Assert.That(view.Caption, Does.Contain("Both sides are threaded").And.Contain("2 of 6"));
            words.ForceMeshUpdate(true, true);
            Assert.That(words.isTextOverflowing, Is.False, "what was kept fits its page");
            Assert.That(view.BeadSwelling, Is.True, "the new beads swell onto the strand");
            yield return Until(() => !view.BeadSwelling, 3f);
            Assert.That(view.BeadSwelling, Is.False);
        }

        [UnityTest]
        public IEnumerator AVisitorMayEndAfterOneSideAndKeepOnlyThatBead()
        {
            app.TurnsPages = true;
            Time.timeScale = 8;
            try
            {
                yield return Press("Interest choices", "place-water");
                yield return Until(() => app.OtherSideOffered, 60f);
                Assert.That(app.OtherSideOffered, Is.True);
                Assert.That(app.Session.Chapter.Id, Is.EqualTo("place-water.return"), "left to the fire, the first side is the authored one");
            }
            finally { Time.timeScale = 1; }
            yield return Press("Turn a page", "next");
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Closing));
            Assert.That(view.Caption, Does.StartWith("The fire chose to stay with the one who remembers."), "ending early is never recorded as hearing both sides");
            yield return Press("Turn a page", "save");
            Assert.That(app.Strand.Beads.Select(b => b.ToString()), Is.EqualTo(new[] { "place-water/place-water.follow.remembers" }), "a side not heard earns no bead");
            Assert.That(view.Caption, Does.Contain("1 of 6"));
        }

        [UnityTest]
        public IEnumerator WherePagesTurnThemselvesAPagesQuestionsWaitUntilItIsToldAndAnAnswerDoesNotTellItAgain()
        {
            app.TurnsPages = true;
            yield return Press("Interest choices", "trade-household");
            var questions = Panel("Questions beside the story"); var transport = Panel("Turn a page");
            var page = app.Session.Chapter.Id;
            Assert.That(app.PageToldOnce, Is.False);
            Assert.That(questions.Ids, Is.Empty, "a question shown now would say what the page has not yet said");
            Assert.That(transport.Ids, Does.Not.Contain("ask"));
            Assert.That(transport.Ids, Does.Contain("sources"), "what is documented and what is imagined can be asked at any moment");

            view.Stage.Seek(1);
            yield return Until(() => app.PageToldOnce, 3f);
            yield return null;
            Assert.That(app.PageToldOnce, Is.True);
            Assert.That(questions.Ids, Is.EqualTo(new[] { "0" }), "once the page is told, its question is offered");
            Assert.That(questions.Hidden, Is.False);
            Assert.That(transport.Ids, Does.Contain("ask"));

            yield return Press(questions, "0");
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Question));
            Assert.That(view.ModalText, Does.Contain("She left a portion at home"));
            yield return Press("Return and recovery actions", "continue");
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Reading));
            Assert.That(app.Session.Chapter.Id, Is.EqualTo(page));
            Assert.That(app.PageToldOnce, Is.True, "coming back from an answer does not tell the page again");
            Assert.That(view.Stage.Progress, Is.EqualTo(1f));
            Assert.That(questions.Ids, Is.EqualTo(new[] { "0" }), "its question is still there to ask");

            // Left alone, the told page waits a little for a question, then turns, and the next page starts untold.
            yield return Until(() => app.Session.PageNumber == 2, PersonalStoryApp.QuestionWaitSeconds + 4f);
            Assert.That(app.Session.PageNumber, Is.EqualTo(2));
            Assert.That(app.PageToldOnce, Is.False);
        }

        [UnityTest]
        public IEnumerator AHandThatReachesAnHeirloomBeginsItsTelling()
        {
            var hand = AHand();
            Assert.That(view.HeirloomCount, Is.EqualTo(3));
            // A hand coming near makes the heirloom rise a little to meet it, and begins nothing.
            hand.Simulated = () => PalmAt(view.HeirloomPosition(2) + new Vector3(0, 0.16f, -0.05f));
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(view.HeirloomReach(2), Is.GreaterThan(0.2f));
            Assert.That(view.HeirloomReach(0), Is.LessThan(view.HeirloomReach(2)), "the heirloom nearest the hand answers most");
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Intake));
            // A hand passing through does not choose; closing on it for a moment does.
            hand.Simulated = () => PalmAt(view.HeirloomPosition(2));
            yield return Until(() => app.Session.State == PersonalStoryState.Reading, 2f);
            hand.Simulated = null;
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Reading), "the water pot itself begins its telling, not only its card");
            Assert.That(app.Session.Story.Interest, Is.EqualTo("place-water"));
            Assert.That(view.HeirloomsOffered, Is.False);
        }

        [UnityTest]
        public IEnumerator RestingAHandOnATableSetsTheStoryThereAndTheStandCanAlwaysBeRestored()
        {
            var placement = UnityEngine.Object.FindFirstObjectByType<StoryTablePlacement>();
            var hand = AHand();
            var stand = view.TableRoot.Find("Virtual table");
            var head = Camera.main.transform;
            var eye = head.position.y < .45f ? new Vector3(head.position.x, 1.15f, head.position.z) : head.position;
            var forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            var right = Vector3.Cross(Vector3.up, forward);
            Vector3 Reach(float ahead, float below, float aside = 0) => eye + forward * ahead + right * aside + Vector3.down * below;
            placement.Recenter();
            var standAt = view.TableRoot.position;
            Assert.That(placement.UsesSurface, Is.False);
            Assert.That(placement.PassthroughActive, Is.False, "the story begins in its own dark room, never in the visitor's without being asked");
            Assert.That(stand.gameObject.activeSelf, Is.True);

            // In the editor there is no room scan, so asking for the visitor's own table goes straight to their hand.
            yield return Press("Always available controls", "options");
            yield return Press("Return and recovery actions", "room");
            Assert.That(placement.AwaitingHand, Is.True);
            Assert.That(view.ModalText, Does.Contain("rest an open hand"));
            Assert.That(Panel("Return and recovery actions").Ids, Is.EqualTo(new[] { "stand" }));

            // A hand in the lap and a hand moving across the table set nothing.
            hand.Simulated = () => PalmAt(Reach(0.2f, 0.55f));
            yield return new WaitForSecondsRealtime(1.4f);
            Assert.That(placement.AwaitingHand, Is.True, "a hand in the lap is not a table");
            var slid = Time.realtimeSinceStartup;
            hand.Simulated = () => PalmAt(Reach(0.5f, 0.40f, -0.2f + (Time.realtimeSinceStartup - slid) * 0.2f));
            yield return new WaitForSecondsRealtime(1.4f);
            Assert.That(placement.AwaitingHand, Is.True, "a hand still moving has not come to rest");

            // A hand laid still, off to one side, shows how high the table is and how near it begins.
            var table = Reach(0.5f, 0.40f, 0.15f);
            hand.Simulated = () => PalmAt(table);
            yield return Until(() => !placement.AwaitingHand, 4f);
            hand.Simulated = null;
            Assert.That(placement.UsesSurface, Is.True);
            Assert.That(view.TableRoot.position.y, Is.EqualTo(table.y - StoryTablePlacement.PalmAboveSurface).Within(0.002f), "the story stands at the height the hand found");
            var offset = view.TableRoot.position - eye;
            Assert.That(Vector3.Dot(offset, right), Is.EqualTo(0).Within(0.01f), "a hand resting to one side does not pull the story out of view");
            Assert.That(Vector3.Dot(offset, forward), Is.EqualTo(0.80f).Within(0.01f), "the story begins just beyond the hand");
            Assert.That(stand.gameObject.activeSelf, Is.False, "the virtual stand gives way to the real table");
            yield return null; yield return null;
            Assert.That(Panel("Return and recovery actions").Hidden, Is.True, "the request closes once the story is set down");
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Intake));
            Assert.That(view.CapabilityLabel, Does.Contain("On your table"));

            // Recenter always brings the stand back.
            yield return Press("Always available controls", "options");
            yield return Press("Return and recovery actions", "recenter");
            Assert.That(placement.UsesSurface, Is.False);
            Assert.That(stand.gameObject.activeSelf, Is.True);
            Assert.That(Vector3.Distance(view.TableRoot.position, standAt), Is.LessThan(0.001f));

            // Declining the request keeps the stand, and so does never resting a hand at all.
            yield return Press("Always available controls", "options");
            yield return Press("Return and recovery actions", "room");
            yield return Press("Return and recovery actions", "stand");
            Assert.That(placement.AwaitingHand, Is.False);
            Assert.That(placement.UsesSurface, Is.False);
            Assert.That(stand.gameObject.activeSelf, Is.True);
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Intake));
        }

        [UnityTest]
        public IEnumerator WherePagesTurnThemselvesTheFirstPageOffersTheVisitorsOwnTable()
        {
            var placement = UnityEngine.Object.FindFirstObjectByType<StoryTablePlacement>();
            app.TurnsPages = true; yield return null;
            Assert.That(Panel("Start choices").Ids, Is.EqualTo(new[] { "edit", "about", "room" }), "using the visitor's own table is offered on the first page, not buried in a menu");
            yield return Press("Start choices", "room");
            Assert.That(placement.AwaitingHand, Is.True);
            yield return Press("Return and recovery actions", "stand");
            Assert.That(placement.AwaitingHand, Is.False);
            Assert.That(view.IntakeVisible, Is.True);
            Assert.That(Panel("Start choices").Ids, Is.EqualTo(new[] { "edit", "about", "room" }));
        }

        /// <summary>A microphone that records a short tone, so no real audio device is touched.</summary>
        sealed class FakeRecorder : IQuestionRecorder
        {
            public int Begun, Ended, Cancelled;
            public float[] Samples = Enumerable.Range(0, PersonalAnswerAudio.SampleRate).Select(i => Mathf.Sin(i * 0.05f) * 0.3f).ToArray();
            public bool Available { get; set; } = true;
            public bool Recording { get; private set; }
            public float Seconds { get; set; }
            public bool Begin() { Begun++; Recording = true; return true; }
            public float[] End(out int count) { Ended++; Recording = false; count = Samples.Length; return Samples; }
            public void Cancel() { if (Recording) Cancelled++; Recording = false; }
        }

        IEnumerator ReachAPageWithQuestions()
        {
            for (var step = 0; step < 20 && app.Session.Chapter.Questions.Count == 0; step++) yield return Press("Turn a page", "next");
            Assert.That(app.Session.Chapter.Questions.Count, Is.GreaterThan(0));
        }

        IEnumerator WaitForAction(string id, float seconds)
        {
            var actions = Panel("Return and recovery actions");
            for (var deadline = Time.realtimeSinceStartup + seconds; Time.realtimeSinceStartup < deadline && !(actions.Ids.Contains(id) && !actions.Hidden);) yield return null;
            Assert.That(actions.Ids, Does.Contain(id));
        }

        [UnityTest]
        public IEnumerator AskingTheFireAloudNeedsConsentAndFallsBackToThePagesOwnQuestions()
        {
            var microphone = new FakeRecorder(); app.UseRecorder(microphone);
            yield return Press("Interest choices", "journeys");
            yield return ReachAPageWithQuestions();
            var page = app.Session.Chapter.Id;

            // Without the switch and a service, asking opens the page's own question and records nothing.
            Assert.That(app.SpokenQuestionsAvailable, Is.False);
            yield return Press("Turn a page", "ask");
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Question));
            Assert.That(microphone.Begun, Is.Zero);
            yield return Press("Return and recovery actions", "continue");

            // With both, a page still being told takes no spoken question: the answer could say what the page has not.
            app.SpokenQuestions = true; app.ServiceEndpoint = "http://127.0.0.1:9"; yield return null;
            Assert.That(app.SpokenQuestionsAvailable, Is.True);
            Assert.That(view.Stage.Progress, Is.LessThan(1f));
            yield return Press("Turn a page", "ask");
            Assert.That(view.ModalText, Does.Contain("then listen for your question"));
            yield return Press("Return and recovery actions", "wait");
            Assert.That(app.Listening, Is.False);
            Assert.That(microphone.Begun, Is.Zero);

            // Once the page is told, the visitor is asked first, and nothing is recorded until they agree.
            view.Stage.Seek(1);
            yield return WaitForAction("agree", 5f);
            Assert.That(view.ModalText, Does.Contain("sent to the Sankofa service"));
            Assert.That(microphone.Begun, Is.Zero, "nothing is recorded before the visitor agrees");
            yield return Press("Return and recovery actions", "agree");
            Assert.That(app.Listening, Is.True);
            Assert.That(microphone.Begun, Is.EqualTo(1));
            yield return Press("Return and recovery actions", "cancel");
            Assert.That(app.Listening, Is.False);
            Assert.That(microphone.Cancelled, Is.EqualTo(1), "a question the visitor abandons is discarded, not sent");
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Reading));

            // Pausing while the fire listens also discards the recording.
            yield return Press("Turn a page", "ask");
            Assert.That(app.Listening, Is.True, "consent is asked once in a visit");
            app.Session.Pause(true); yield return null; yield return null;
            Assert.That(app.Listening, Is.False);
            Assert.That(microphone.Cancelled, Is.EqualTo(2));
            app.Session.Pause(false); yield return null;

            // Sent to a service that cannot be reached, the question falls back to what the page itself can answer.
            yield return Press("Turn a page", "ask");
            yield return Press("Return and recovery actions", "send");
            Assert.That(microphone.Ended, Is.EqualTo(1));
            yield return WaitForAction("q0", 25f);
            Assert.That(view.ModalText, Does.Contain("could not answer"));
            yield return Press("Return and recovery actions", "q0");
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Question));
            Assert.That(app.Session.Chapter.Id, Is.EqualTo(page));

            // A recording with nothing in it, as from a muted microphone, is not sent at all.
            yield return Press("Return and recovery actions", "continue");
            microphone.Samples = new float[PersonalAnswerAudio.SampleRate];
            yield return Press("Turn a page", "ask");
            yield return Press("Return and recovery actions", "send");
            Assert.That(view.ModalText, Does.Contain("did not hear a question"));
            Assert.That(app.Session.Chapter.Id, Is.EqualTo(page));
        }

        [UnityTest]
        public IEnumerator AnAnswerIsShownWithItsLabelAndARefusedOneNeverReachesThePage()
        {
            yield return Press("Interest choices", "journeys");
            yield return ReachAPageWithQuestions();
            var page = app.Session.Chapter.Id;
            string Reply(string answer, string truth, params string[] sources) => PersonalAnswerJson.Write(new PersonalAnswer
                { Mode = PersonalAnswer.Generated, Kind = PersonalAnswer.AnswerKind, Answer = answer, Truth = truth, Sources = sources });

            app.ReceiveAnswer(Reply("She stayed because leaving would mean nobody was there to meet her friend.", PersonalAnswer.Reconstructed), null);
            yield return null;
            Assert.That(view.ModalText, Does.Contain("She stayed because").And.Contain("imagined"), "an imagined answer always shows its label");
            yield return Press("Return and recovery actions", "return");
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Reading));
            Assert.That(app.Session.Chapter.Id, Is.EqualTo(page));

            app.ReceiveAnswer(Reply(PersonalAnswer.UnknownReply, PersonalAnswer.Unknown), null);
            yield return null;
            Assert.That(view.ModalText, Does.Contain("does not know that story").And.Contain("Outside tonight's story"));
            yield return Press("Return and recovery actions", "return");

            // New words that claim to be documented history are refused; the page's own questions are offered instead.
            app.ReceiveAnswer(Reply("Forty traders met at this river every dry season.", PersonalAnswer.Historical, "C6"), null);
            yield return null;
            Assert.That(view.ModalText, Does.Not.Contain("Forty traders"));
            Assert.That(view.ModalText, Does.Contain("could not answer"));
            Assert.That(Panel("Return and recovery actions").Ids, Does.Contain("q0"));
            yield return Press("Return and recovery actions", "return");

            // A reply that is not the service's format at all is refused the same way.
            app.ReceiveAnswer("{\"answer\":\"Anything\"}", null);
            yield return null;
            Assert.That(view.ModalText, Does.Not.Contain("Anything"));
            Assert.That(app.Session.Chapter.Id, Is.EqualTo(page));
        }

        [Test]
        public void ARecordingAtAnotherRateOrInStereoBecomesMonoAtTheServicesRate()
        {
            var stereo48k = new float[48000 * 2];
            for (var i = 0; i < 48000; i++) { stereo48k[i * 2] = Mathf.Sin(i * 0.01f); stereo48k[i * 2 + 1] = -1; }
            var mono = MicrophoneRecorder.Mono16k(stereo48k, 2, 48000, out var count);
            Assert.That(count, Is.EqualTo(PersonalAnswerAudio.SampleRate), "one second stays one second");
            Assert.That(mono.Take(count).Min(), Is.GreaterThan(-1f), "only the first channel is used");
            Assert.That(PersonalAnswerAudio.IsAccepted(PersonalAnswerAudio.EncodeWav(mono, count)), Is.True);
            var native = new float[8000];
            Assert.That(MicrophoneRecorder.Mono16k(native, 1, PersonalAnswerAudio.SampleRate, out count), Is.SameAs(native));
            Assert.That(count, Is.EqualTo(8000));
            Assert.That(MicrophoneRecorder.Mono16k(new float[0], 1, 16000, out count), Is.Null);
        }

        /// <summary>Opt-in: the real service process on this machine, with no provider, answers Unity's own request.</summary>
        [UnityTest]
        public IEnumerator ALocalServiceAnswersUnitysRecordedQuestionWithThePagesOwnQuestions()
        {
            var service = Environment.GetEnvironmentVariable("SANKOFA_TEST_LOCAL_ASK");
            if (string.IsNullOrEmpty(service)) { Assert.Ignore("Set SANKOFA_TEST_LOCAL_ASK to a loopback story service to run this."); yield break; }
            var microphone = new FakeRecorder(); app.UseRecorder(microphone);
            app.SpokenQuestions = true; app.ServiceEndpoint = service; app.OnlineCreation = false; yield return null;
            Assert.That(Panel("Start choices").Ids, Does.Not.Contain("create"), "a question-only service adds no online creation at the intake");
            yield return Press("Interest choices", "journeys");
            yield return ReachAPageWithQuestions();
            view.Stage.Seek(1); yield return null;
            yield return Press("Turn a page", "ask");
            yield return Press("Return and recovery actions", "agree");
            yield return Press("Return and recovery actions", "send");
            yield return WaitForAction("q0", 25f);
            Assert.That(view.ModalText, Does.Contain("offers what it can answer"), "the service's own reply was read and accepted, not a transport failure");
        }

        [UnityTest]
        public IEnumerator FreeformQuestionChangesPreparedThreadWithoutAssumingFamilyIdentity()
        {
            app.SetSeed("What could a household carry in a gourd?");
            yield return Press("Interest choices", "journeys");
            Assert.That(app.Session.Story.Interest, Is.EqualTo("trade-household"), "recognised words in the question choose the thread over the heirloom");
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
            var shownSources = new List<string> { view.ModalText };
            while (view.ModalPart + 1 < view.ModalPartCount)
            {
                yield return Press("Return and recovery actions", "modal-next");
                shownSources.Add(view.ModalText);
            }
            Assert.That(string.Join(" ", shownSources), Does.Contain("C6").And.Contain("cambridge.org").And.Not.Contain("John Overton"));
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
            typeof(PersonalStoryApp).GetField("narrationToken", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(app, "synthetic-previous-visit-token");
            yield return Press(kept, "reopen");
            Assert.That(app.Session.IsReplay, Is.True);
            Assert.That(PersonalStoryJson.Write(app.Session.Story), Is.EqualTo(first));
            Assert.That(view.CapabilityLabel, Does.Contain("no new generation"));
            Assert.That(typeof(PersonalStoryApp).GetField("narrationToken", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(app), Is.Null, "reopening cannot fetch fresh speech with an earlier visit's token");
            yield return Press("Always available controls", "leave");
            yield return Press("Return and recovery actions", "leave");
            yield return Press("Interest choices", "place-water");
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
            // A well-formed loopback address with nothing listening: online creation is offered, then fails locally.
            app.ServiceEndpoint = "http://127.0.0.1:9";
            app.SetSeed("What does the river name mean?");
            var version = app.Session.RequestVersion;
            yield return Press("Start choices", "create");
            Assert.That(app.Session.RequestVersion, Is.EqualTo(version), "opening consent sends nothing");
            yield return Press("Online consent actions", "create");
            for (var deadline = Time.realtimeSinceStartup + 10f; app.Session.State == PersonalStoryState.Preparing && Time.realtimeSinceStartup < deadline;)
                yield return null;
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Error));
            Assert.That(app.Seed, Is.EqualTo("What does the river name mean?"));
            Assert.That(Panel("Return and recovery actions").Ids, Does.Contain("local"));
            yield return Press("Return and recovery actions", "local");
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Reading));
            Assert.That(app.Session.Story.Mode, Is.EqualTo("bundled"));
            Assert.That(app.Session.Story.Interest, Is.EqualTo("place-water"));
        }

        [UnityTest]
        public IEnumerator WithoutAServiceTheIntakeOffersNoOnlineActionThatCanOnlyFail()
        {
            Assert.That(app.OnlineAvailable, Is.False);
            var version = app.Session.RequestVersion;
            var start = Panel("Start choices");
            Assert.That(start.Ids, Is.EquivalentTo(new[] { "local", "edit", "about" }));
            yield return Press(start, "about");
            var shown = new List<string> { view.ModalText };
            while (view.ModalPart + 1 < view.ModalPartCount)
            {
                yield return Press("Return and recovery actions", "modal-next");
                shown.Add(view.ModalText);
            }
            Assert.That(string.Join(" ", shown), Does.Contain("whose side to follow").And.Contain("CC BY 3.0").And.Contain("celldroid (CC BY 4.0)"), "every attributed recording the visitor hears is credited");
            Assert.That(app.Session.RequestVersion, Is.EqualTo(version), "reading how it works starts no request");
            yield return Press("Return and recovery actions", "return");
            Assert.That(view.IntakeVisible, Is.True);
            Assert.That(app.Session.State, Is.EqualTo(PersonalStoryState.Intake));
            app.ServiceEndpoint = "http://127.0.0.1:9";
            yield return null;
            Assert.That(Panel("Start choices").Ids, Does.Contain("create"), "a configured service restores the explicit online path");
        }

        [UnityTest]
        public IEnumerator ChoosingASideChangesTheEndingAndContinuingLetsTheFireChoose()
        {
            var endings = new List<string>();
            foreach (var choose in new[] { true, false })
            {
                app.Session.Cancel(); app.SetSeed("");
                yield return Press("Interest choices", "journeys");
                for (var step = 0; step < 20 && !app.Session.OffersChoice; step++) yield return Press("Turn a page", "next");
                Assert.That(app.Session.OffersChoice, Is.True, "each prepared telling reaches a perspective choice");
                while (view.ReadingPart + 1 < view.ReadingPartCount) yield return Press("Turn a page", "next");
                Assert.That(view.Caption, Does.Contain(StoryTableView.DefaultChoicePrompt));
                var side = Panel("Questions beside the story");
                Assert.That(side.Ids, Is.EquivalentTo(new[] { "choice:0", "choice:1" }));
                if (choose)
                {
                    // Choosing by touching the painted figure: the one who is late carries the gourd.
                    view.Stage.Seek(1); yield return null; yield return null;
                    Assert.That(Panel("Follow naserian").Ids, Is.EqualTo(new[] { "choice:0" }));
                    yield return Press("Follow wambui", "choice:1");
                }
                else yield return Press("Turn a page", "next");
                Assert.That(app.Session.FollowedByChoice, Is.EqualTo(choose), "continuing is the fire's choice, never recorded as the visitor's");
                Assert.That(app.Session.Chapter.Id, Is.EqualTo(choose ? "journeys.late" : "journeys.meeting"));
                endings.Add(app.Session.Chapter.Narration);
                // With hand-turned pages the side not yet heard is offered on this page, and ending here is one action.
                Assert.That(app.OtherSideOffered, Is.True);
                Assert.That(Panel("Questions beside the story").Ids, Is.EqualTo(new[] { "choice:0" }));
                yield return ReachEnding();
                Assert.That(view.Caption, Does.StartWith(choose ? "You chose to find the one who is late." : "The fire chose to stay with the one who waits."));
                Assert.That(view.Caption, Does.Contain(choose ? "stay with the one who waits" : "find the one who is late"), "the unheard side is named as a reason to return");
                Assert.That(app.HasKept, Is.False);
            }
            Assert.That(endings.Distinct().Count(), Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator TheFirstPageSaysWhatTheVisitorsQuestionChanged()
        {
            app.SetSeed("What did a household carry in a gourd?");
            yield return Press("Start choices", "local");
            Assert.That(app.PersonalReason, Does.Contain("trade and household").And.Contain("gourd"));
            var subtitle = view.GetComponentsInChildren<TMPro.TMP_Text>(true).Single(t => t.name == "Story setting");
            Assert.That(subtitle.text, Is.EqualTo(app.PersonalReason));
            Assert.That(PersonalStoryJson.Write(app.Session.Story), Does.Not.Contain(app.PersonalReason), "the reason is never saved as story content");
            for (var step = 0; step < 6 && app.Session.PageNumber == 1; step++) yield return Press("Turn a page", "next");
            Assert.That(app.Session.PageNumber, Is.EqualTo(2));
            Assert.That(subtitle.text, Does.Contain("Enkare Nairobi"));
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
