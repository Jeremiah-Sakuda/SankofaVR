using System;
using System.Collections.Generic;
using System.Linq;
using Sankofa.Core.Story;
using Sankofa.Core.Visits;
using Sankofa.Flow;
using NUnit.Framework;
using static Sankofa.Tests.Fixtures;

namespace Sankofa.Tests
{
    /// <summary>Plays lines and transitions for their durations and reports completion by token, like the Unity presentation.</summary>
    sealed class FakePresentation
    {
        readonly VisitFlow flow;
        int speakToken;
        double speakRemaining;
        int transitionToken;
        double transitionRemaining;

        public FakePresentation(VisitFlow flow) => this.flow = flow;

        public readonly List<Cue> Cues = new List<Cue>();
        public bool ReportLines = true;
        public bool ReportTransitions = true;
        public double TransitionSeconds = 2.5;
        public int SpeakingToken => speakToken;

        public List<string> Spoken => Cues.OfType<Cue.Speak>().Select(s => s.Line.Id).ToList();
        public IEnumerable<T> Of<T>() where T : Cue => Cues.OfType<T>();

        public void Pump()
        {
            foreach (var cue in flow.DrainCues())
            {
                Cues.Add(cue);
                switch (cue)
                {
                    case Cue.Speak s:
                        speakToken = s.Token;
                        speakRemaining = s.Line.Seconds;
                        break;
                    case Cue.StopSpeaking _:
                        speakToken = 0;
                        break;
                    case Cue.Transition t:
                        transitionToken = t.Token;
                        transitionRemaining = TransitionSeconds;
                        break;
                }
            }
        }

        public void Advance(double seconds, double step = 0.05)
        {
            Pump();
            for (var t = 0.0; t < seconds - 1e-9; t += step)
            {
                var dt = Math.Min(step, seconds - t);
                if (speakToken != 0 && (speakRemaining -= dt) <= 0)
                {
                    var token = speakToken;
                    speakToken = 0;
                    if (ReportLines) flow.LineFinished(token);
                }
                if (transitionToken != 0 && (transitionRemaining -= dt) <= 0)
                {
                    var token = transitionToken;
                    transitionToken = 0;
                    if (ReportTransitions) flow.TransitionFinished(token);
                }
                flow.Tick(dt);
                Pump();
            }
        }

        public void RunUntil(Func<bool> done, double maxSeconds = 900)
        {
            Pump();
            for (var t = 0.0; t < maxSeconds; t += 0.05)
            {
                if (done()) return;
                Advance(0.05);
            }
            Assert.Fail("Condition not reached within " + maxSeconds + " s. Last cues: " + string.Join(" | ", Cues.Skip(Math.Max(0, Cues.Count - 8))));
        }
    }

    public class VisitFlowTests
    {
        VisitFlow flow;
        FakePresentation view;
        MemoryStore store;
        int ids;

        void Open(MemoryStore existing = null, FlowSettings settings = null, bool skipWelcome = false)
        {
            store = existing ?? new MemoryStore();
            flow = new VisitFlow(Playable(ReadPlaceholder()), settings, store, null,
                () => "v" + (++ids), () => new DateTimeOffset(2026, 10, 4, 21, 0, 0, TimeSpan.Zero).AddMinutes(ids));
            view = new FakePresentation(flow);
            flow.Start(skipWelcome);
            view.Pump();
        }

        void AtChoice() => view.RunUntil(() => flow.Phase == VisitPhase.Choosing && flow.CurrentLine == null);

        void Begin(string route)
        {
            Open();
            AtChoice();
            Assert.That(flow.Choose(route), Is.True);
            view.Pump();
        }

        /// <summary>Plays on, accepting each listed invitation as soon as it opens, until the visit closes at the hearth.</summary>
        void PlayToClosing(params string[] accept)
        {
            var wanted = new HashSet<string>(accept);
            view.RunUntil(() =>
            {
                if (flow.OpenInvitation != null && wanted.Remove(flow.OpenInvitation.Id))
                    Assert.That(flow.Accept(flow.OpenInvitation.Id), Is.True);
                return flow.Phase == VisitPhase.Closing && flow.CurrentLine == null;
            });
        }

        [Test]
        public void LeavingAFinishedVisitThatIsNotKeptAsksFirst()
        {
            Begin("makers");
            PlayToClosing();
            Assert.That(flow.Leave(), Is.True);
            view.Pump();
            Assert.That(flow.Phase, Is.EqualTo(VisitPhase.Closing), "nothing is lost on the first press");
            Assert.That(ControlKinds, Is.EqualTo(new[] { ControlKind.ConfirmLeave, ControlKind.Keep, ControlKind.CancelLeave }));
            Assert.That(flow.CancelLeave(), Is.True);
            Assert.That(ControlKinds, Is.EqualTo(new[] { ControlKind.Keep, ControlKind.BeginAgain, ControlKind.Leave }));
            Assert.That(flow.ConfirmLeave(), Is.False, "nothing to confirm once the visitor stayed");

            Assert.That(flow.Leave(), Is.True);
            Assert.That(flow.ConfirmLeave(), Is.True);
            view.Pump();
            Assert.That(flow.Phase, Is.EqualTo(VisitPhase.Ended));
            Assert.That(store.Files, Is.Empty, "leaving without keeping keeps nothing");
        }

        [Test]
        public void LeavingAPausedStoryAsksFirst()
        {
            Begin("makers");
            view.RunUntil(() => flow.Phase == VisitPhase.Story);
            Assert.That(flow.Pause("visitor"), Is.True);
            view.Pump();
            Assert.That(flow.Leave(), Is.True);
            view.Pump();
            Assert.That(flow.Phase, Is.EqualTo(VisitPhase.Paused), "one press beside Resume loses nothing");
            Assert.That(ControlKinds, Is.EqualTo(new[] { ControlKind.ConfirmLeave, ControlKind.CancelLeave }));
            Assert.That(flow.CancelLeave(), Is.True);
            Assert.That(ControlKinds, Does.Contain(ControlKind.Resume));
            Assert.That(flow.Leave(), Is.True);
            Assert.That(flow.ConfirmLeave(), Is.True);
            view.Pump();
            Assert.That(flow.Phase, Is.EqualTo(VisitPhase.Ended));
        }

        [Test]
        public void KeepingWhileAskedKeepsAndThenLeavesWithoutAskingAgain()
        {
            Begin("makers");
            PlayToClosing();
            flow.Leave();
            Assert.That(flow.Keep(), Is.True);
            Assert.That(flow.ConfirmingLeave, Is.False);
            Assert.That(flow.Leave(), Is.True);
            view.Pump();
            Assert.That(flow.Phase, Is.EqualTo(VisitPhase.Ended), "a kept visit is not lost by leaving");
            Assert.That(store.Files.Count, Is.EqualTo(1));
        }

        /// <summary>A visitor who chose the makers' story, took the ember and the piece of the bowl but did not touch it, and kept the visit.</summary>
        SavedVisit KeepAMakersVisit()
        {
            Begin("makers");
            PlayToClosing("receive-ember", "receive-shard");
            Assert.That(flow.Keep(), Is.True);
            view.Pump();
            return flow.KeptVisit;
        }

        /// <summary>Edits the plan inside a kept visit's file, as a later version or a damaged file might.</summary>
        void Tamper(string id, Action<Sankofa.Core.Json.JsonObject> change)
        {
            var json = Sankofa.Core.Json.JsonText.ParseObject(store.Files[id]);
            change(json.GetObject("plan"));
            store.Files[id] = Sankofa.Core.Json.JsonText.Serialize(json);
        }

        void Reopen()
        {
            var kept = store;
            Open(kept);
        }

        bool Speaking(string lineId) => flow.CurrentLine?.Id == lineId && view.SpeakingToken != 0;
        bool AwaitingInvitation => flow.OpenInvitation != null && flow.CurrentLine == null;
        IEnumerable<ControlKind> ControlKinds => flow.Controls.Select(c => c.Kind);
        IEnumerable<string> QuestionIds => flow.Questions.Select(q => q.Id);

        // ---- the hearth and the choice ----

        [Test]
        public void TheWelcomeEndsAtTheChoiceAndWaitsForTheVisitor()
        {
            Open();
            Assert.That(flow.Phase, Is.EqualTo(VisitPhase.Welcome));
            Assert.That(flow.Controls.Where(c => c.Kind == ControlKind.Choose).Select(c => c.Label), Is.EqualTo(new[] { "Hands that make", "Roads that carry" }),
                "the visitor can choose during the welcome");
            view.Advance(90);
            Assert.That(flow.Phase, Is.EqualTo(VisitPhase.Choosing), "no input keeps the visitor at the hearth");
            Assert.That(view.Spoken, Is.EqualTo(new[] { "welcome.sit", "welcome.fire", "welcome.choose" }));
            Assert.That(ControlKinds, Is.EqualTo(new[] { ControlKind.Choose, ControlKind.Choose, ControlKind.Pause }));
            Assert.That(view.Of<Cue.Shelf>().Single().Visit, Is.Null, "nothing kept, nothing recalled");
            Assert.That(flow.Choose("makers"), Is.True);
            view.Pump();
            Assert.That(view.Spoken.Last(), Is.EqualTo("makers.gather"));
            Assert.That(view.Of<Cue.VisitStarted>().Single().Mode, Is.EqualTo(VisitMode.Chosen));
        }

        [Test]
        public void TheVisitorCanChooseDuringTheWelcome()
        {
            Open();
            view.Advance(1);
            var welcomeToken = view.SpeakingToken;
            Assert.That(flow.Choose("travellers"), Is.True);
            view.Pump();
            Assert.That(view.Of<Cue.StopSpeaking>().Count(), Is.EqualTo(1));
            Assert.That(flow.LineFinished(welcomeToken), Is.False, "a late report from the interrupted welcome changes nothing");
            Assert.That(view.Spoken.Last(), Is.EqualTo("travellers.gather"));
        }

        [Test]
        public void ContinuationsAreNotOfferedWithoutAKeptVisit()
        {
            Open();
            AtChoice();
            Assert.That(flow.Choose("makers.again"), Is.False);
            Assert.That(flow.Follow(), Is.False);
            Assert.That(flow.Replay(), Is.False);
            Assert.That(flow.Choose("nowhere"), Is.False);
        }

        // ---- a visit ----

        [Test]
        public void APassiveVisitCompletesWithoutClaimingAChoice()
        {
            Begin("makers");
            PlayToClosing();

            Assert.That(flow.Journal.Outcomes.Values.Distinct(), Is.EqualTo(new[] { Outcome.Default }));
            Assert.That(flow.Journal.Entries.Where(e => e.Kind == JournalKind.InvitationResolved).Select(e => e.Detail).Distinct(),
                Is.EqualTo(new[] { "default path; no visitor choice" }));
            Assert.That(view.Spoken, Does.Contain("offer.default").And.Contain("yard.bowl.placed").And.Contain("yard.bowl.seen").And.Contain("yard.shard.held"));
            Assert.That(view.Spoken, Does.Not.Contain("offer.accepted").And.Not.Contain("yard.bowl.carried").And.Not.Contain("yard.bowl.felt"));

            var transitions = view.Of<Cue.Transition>().ToList();
            Assert.That(transitions.Select(t => t.Kind), Is.EqualTo(new[] { TransitionKind.Enter, TransitionKind.Return }));
            Assert.That(transitions[0].Continuities.Select(c => c.ToString()), Is.EqualTo(new[] { "ember->lantern" }));
            Assert.That(transitions[1].Continuities.Select(c => c.ToString()), Is.EquivalentTo(new[] { "lantern->ember", "shard->shard.kept" }),
                "the light and the piece the fire kept both come home");

            Assert.That(flow.Journal.Completed, Is.True);
            Assert.That(view.Of<Cue.Keepsake>().Single().ObjectId, Is.EqualTo("shard.kept"));
            Assert.That(view.Of<Cue.Closed>().Single().Completed, Is.True);
            Assert.That(ControlKinds, Is.EqualTo(new[] { ControlKind.Keep, ControlKind.BeginAgain, ControlKind.Leave }));
        }

        [Test]
        public void DeliberateActionsAreAcknowledgedOnceAndChangeTheStory()
        {
            Begin("makers");
            Assert.That(flow.Accept("receive-ember"), Is.False, "nothing is offered yet");
            view.RunUntil(() => Speaking("offer.hand"));
            var accept = flow.Controls.Single(c => c.Kind == ControlKind.Accept);
            Assert.That(accept.Target, Is.EqualTo("receive-ember"));
            Assert.That(accept.Label, Is.EqualTo("Take the ember"), "the nearby select alternative to the gesture");
            Assert.That(flow.Accept("receive-ember"), Is.True);
            Assert.That(flow.Accept("receive-ember"), Is.False, "the same action is acknowledged once");

            view.RunUntil(() => flow.OpenInvitation?.Id == "touch-bowl");
            Assert.That(flow.Controls.Single(c => c.Kind == ControlKind.Accept).Label, Is.EqualTo("Touch the bowl"));
            Assert.That(flow.Accept("touch-bowl"), Is.True);
            PlayToClosing("receive-shard");

            Assert.That(view.Spoken, Does.Contain("offer.accepted").And.Contain("yard.bowl.carried").And.Contain("yard.bowl.felt")
                .And.Contain("yard.mark.felt").And.Contain("yard.shard.taken"));
            Assert.That(view.Spoken, Does.Not.Contain("yard.bowl.placed").And.Not.Contain("yard.mark.seen"));
            var resolvedAt = flow.Journal.Entries.First(e => e.Kind == JournalKind.InvitationResolved).Sequence;
            var offerHeardAt = flow.Journal.Entries.Single(e => e.Kind == JournalKind.LineHeard && e.Subject == "offer.hand").Sequence;
            Assert.That(resolvedAt, Is.LessThan(offerHeardAt), "accepted while the offer was spoken; the line still finished");
        }

        [Test]
        public void TheTwoInterestsPlayDifferentScenesObjectsAndKeepsakes()
        {
            Begin("makers");
            PlayToClosing();
            var makers = view.Cues.ToList();
            Begin("travellers");
            PlayToClosing();
            var travellers = view.Cues.ToList();

            string StoryScene(List<Cue> cues) => cues.OfType<Cue.Transition>().First().To.Id;
            IEnumerable<string> Objects(List<Cue> cues) => cues.OfType<Cue.Act>().Select(a => a.Action.Object).Distinct();
            Assert.That(StoryScene(makers), Is.EqualTo("story.yard"));
            Assert.That(StoryScene(travellers), Is.EqualTo("story.road"));
            Assert.That(Objects(makers).Intersect(Objects(travellers)), Is.EqualTo(new[] { "ember" }));
            Assert.That(makers.OfType<Cue.Keepsake>().Single().ObjectId, Is.Not.EqualTo(travellers.OfType<Cue.Keepsake>().Single().ObjectId));
            Assert.That(travellers.OfType<Cue.Transition>().First().Continuities.Single().ToString(), Is.EqualTo("ember->road.lantern"));
        }

        [Test]
        public void PauseInvalidatesPlaybackAndResumeDoesNotRepeatCompletedActions()
        {
            Begin("makers");
            view.RunUntil(() => Speaking("offer.hand"));
            var interrupted = view.SpeakingToken;

            Assert.That(flow.Pause(), Is.True);
            view.Pump();
            Assert.That(flow.LineFinished(interrupted), Is.False);
            Assert.That(ControlKinds, Is.EqualTo(new[] { ControlKind.Resume, ControlKind.ReturnToHearth, ControlKind.Leave }));
            Assert.That(flow.Accept("receive-ember"), Is.False, "input waits while paused");

            view.Advance(120);
            Assert.That(flow.Phase, Is.EqualTo(VisitPhase.Paused));
            Assert.That(flow.Journal.Outcomes, Is.Empty, "paused time never expires the invitation");

            Assert.That(flow.Resume(), Is.True);
            view.Pump();
            Assert.That(view.SpeakingToken, Is.Not.EqualTo(interrupted).And.Not.EqualTo(0));
            Assert.That(view.Spoken.Count(id => id == "offer.hand"), Is.EqualTo(2), "resumed from the line's start");
            Assert.That(view.Of<Cue.Act>().Count(a => a.Action.Kind == ActionKind.Offer), Is.EqualTo(1), "the offer is not repeated");
            Assert.That(view.Of<Cue.InvitationOpened>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void ALateReportFromBeforeAPauseCannotEndTheResumedLine()
        {
            Begin("makers");
            view.RunUntil(() => Speaking("offer.hand"));
            var beforePause = view.SpeakingToken;
            flow.Pause();
            flow.Resume();
            view.Pump();
            Assert.That(flow.LineFinished(beforePause), Is.False);
            Assert.That(flow.Journal.HasHeard("offer.hand"), Is.False);
            Assert.That(flow.CurrentLine?.Id, Is.EqualTo("offer.hand"));
        }

        [Test]
        public void AnOpenInvitationKeepsItsRemainingTimeAcrossAPause()
        {
            Begin("makers");
            view.RunUntil(() => AwaitingInvitation);
            view.Advance(5);
            flow.Pause();
            view.Advance(100);
            flow.Resume();
            view.Advance(8.5);
            Assert.That(flow.Journal.Outcomes, Is.Empty, "about 9 of 14 seconds remained");
            view.Advance(1);
            Assert.That(flow.Journal.Outcomes["receive-ember"], Is.EqualTo(Outcome.Default));
        }

        [Test]
        public void PauseDuringATransitionTakesEffectWhenItLands()
        {
            Begin("makers");
            view.RunUntil(() => flow.Phase == VisitPhase.Transitioning);
            Assert.That(flow.Pause(), Is.True);
            view.RunUntil(() => flow.Phase == VisitPhase.Paused);
            Assert.That(flow.Scene.Id, Is.EqualTo("story.yard"));
            Assert.That(view.Spoken, Does.Not.Contain("yard.lantern"));
            Assert.That(flow.Resume(), Is.True);
            view.Pump();
            Assert.That(view.Spoken.Last(), Is.EqualTo("yard.lantern"));
        }

        [Test]
        public void ReturningEarlyDiscardsLateResultsAndCarriesOnlyWhatTheVisitorHas()
        {
            Begin("makers");
            view.RunUntil(() => Speaking("yard.dusk"));
            var interrupted = view.SpeakingToken;

            Assert.That(flow.ReturnToHearth(), Is.True);
            view.Pump();
            Assert.That(flow.LineFinished(interrupted), Is.False);
            var back = view.Of<Cue.Transition>().Last();
            Assert.That(back.Kind, Is.EqualTo(TransitionKind.Return));
            Assert.That(back.Continuities.Single().ToString(), Is.EqualTo("lantern->ember"), "the piece was never given, so it cannot come home");

            view.RunUntil(() => flow.Phase == VisitPhase.Closing && flow.CurrentLine == null);
            Assert.That(view.Spoken.Last(), Is.EqualTo("early.later"));
            Assert.That(flow.Journal.ReturnedEarly, Is.True);
            Assert.That(flow.Journal.Completed, Is.False, "an early return is never presented as a finished story");
            Assert.That(view.Of<Cue.Closed>().Single().Completed, Is.False);
            Assert.That(view.Of<Cue.Keepsake>(), Is.Empty);
            Assert.That(ControlKinds, Is.EqualTo(new[] { ControlKind.BeginAgain, ControlKind.Leave }), "only a finished visit can be kept");
        }

        [Test]
        public void ReturningBeforeAnInvitationResolvesWithdrawsIt()
        {
            Begin("makers");
            view.RunUntil(() => Speaking("offer.hand"));
            Assert.That(flow.ReturnToHearth(), Is.True);
            view.Pump();
            Assert.That(view.Of<Cue.InvitationClosed>().Single().Outcome, Is.Null);
            Assert.That(view.Of<Cue.Transition>(), Is.Empty, "already at the hearth");
            Assert.That(flow.Accept("receive-ember"), Is.False);
            view.RunUntil(() => flow.CurrentLine == null);
            Assert.That(flow.Journal.Outcomes, Is.Empty);
            Assert.That(flow.Journal.Entries.Any(e => e.Kind == JournalKind.InvitationWithdrawn), Is.True);
        }

        [Test]
        public void MissingCompletionReportsDoNotStrandTheVisitor()
        {
            Open();
            view.ReportLines = false;
            view.ReportTransitions = false;
            AtChoice();
            flow.Choose("travellers");
            view.RunUntil(() => flow.Phase == VisitPhase.Closing, 2000);
            Assert.That(flow.Journal.Completed, Is.True);
        }

        [Test]
        public void AMeasuredClipLengthExtendsTheWatchdog()
        {
            Open();
            view.ReportLines = false;
            var token = view.SpeakingToken;
            Assert.That(flow.ReportLineDuration(token, 30), Is.True);
            view.Advance(37);
            Assert.That(flow.CurrentLine?.Id, Is.EqualTo("welcome.sit"), "3.2 s × 2 + 8 would have expired; the measured 30 s + 8 has not");
            Assert.That(view.Spoken, Does.Not.Contain("welcome.fire"));
            view.Advance(3);
            Assert.That(view.Spoken, Does.Contain("welcome.fire"));
        }

        [Test]
        public void LeavingEndsTheVisitFromAnyPhase()
        {
            Begin("makers");
            view.RunUntil(() => flow.Phase == VisitPhase.Transitioning);
            var transition = view.Of<Cue.Transition>().Last().Token;
            Assert.That(flow.Leave(), Is.True);
            view.Pump();
            Assert.That(flow.Phase, Is.EqualTo(VisitPhase.Ended));
            Assert.That(flow.TransitionFinished(transition), Is.False);
            Assert.That(flow.Controls, Is.Empty);
            var count = view.Cues.Count;
            view.Advance(30);
            Assert.That(view.Cues.Count, Is.EqualTo(count), "nothing happens after leaving");
            Assert.That(flow.Leave(), Is.False);
        }

        [Test]
        public void BeginAgainReturnsToTheChoiceWithoutTheWelcome()
        {
            Begin("makers");
            PlayToClosing();
            var resets = view.Of<Cue.ResetStage>().Count();
            Assert.That(flow.BeginAgain(), Is.True);
            view.Pump();
            Assert.That(flow.Phase, Is.EqualTo(VisitPhase.Choosing));
            Assert.That(view.Spoken.Last(), Is.EqualTo("choose.again"));
            Assert.That(view.Of<Cue.ResetStage>().Count(), Is.EqualTo(resets + 1));
            Assert.That(flow.Choose("travellers"), Is.True);
            view.Pump();
            Assert.That(flow.Journal.VisitNumber, Is.EqualTo(2));
            Assert.That(flow.Journal.Outcomes, Is.Empty, "a new visit starts with no outcomes");
        }

        // ---- questions and attention ----

        [Test]
        public void AQuestionInterruptsTheFireWhichAnswersAndResumesFromASafeBoundary()
        {
            Begin("makers");
            view.RunUntil(() => flow.CurrentLine?.Id == "yard.lantern");
            Assert.That(QuestionIds, Is.EqualTo(new[] { "q.yard.real" }), "questions about the bowl wait until it is there");
            view.RunUntil(() => Speaking("yard.bowl.reveal"));
            Assert.That(QuestionIds, Is.EqualTo(new[] { "q.bowl.maker", "q.bowl.line" }));
            var interrupted = view.SpeakingToken;

            Assert.That(flow.Ask("q.bowl.maker"), Is.True);
            view.Pump();
            Assert.That(view.Of<Cue.QuestionAsked>().Single().Question.Id, Is.EqualTo("q.bowl.maker"), "acknowledged before the answer");
            Assert.That(view.Spoken.Last(), Is.EqualTo("q.bowl.maker.answer"));
            Assert.That(flow.LineFinished(interrupted), Is.False);
            Assert.That(QuestionIds, Is.Empty, "one question at a time");
            Assert.That(flow.Ask("q.bowl.line"), Is.False);

            view.RunUntil(() => Speaking("yard.bowl.reveal"));
            Assert.That(view.Spoken.Count(id => id == "yard.bowl.reveal"), Is.EqualTo(2), "the interrupted line resumes from its start");
            Assert.That(view.Of<Cue.Act>().Count(a => a.Action.Kind == ActionKind.Reveal && a.Action.Object == "bowl"), Is.EqualTo(1), "its reveal is not repeated");
            Assert.That(QuestionIds, Is.EqualTo(new[] { "q.bowl.line" }), "each question is answered once");
            Assert.That(flow.Journal.Asked, Is.EqualTo(new[] { "q.bowl.maker" }));
        }

        [Test]
        public void AQuestionDuringAnInvitationLeavesTimeToActAfterTheAnswer()
        {
            Begin("makers");
            view.RunUntil(() => flow.OpenInvitation?.Id == "touch-bowl" && flow.CurrentLine == null);
            view.Advance(10);
            Assert.That(flow.Ask("q.bowl.line"), Is.True);
            view.RunUntil(() => flow.Answering == null);
            Assert.That(flow.OpenInvitation?.Id, Is.EqualTo("touch-bowl"), "the invitation waited while the fire answered");
            view.Advance(5);
            Assert.That(flow.Journal.Outcomes.ContainsKey("touch-bowl"), Is.False, "at least six seconds remain after the answer");
            Assert.That(flow.Accept("touch-bowl"), Is.True);
        }

        [Test]
        public void QuestionsBelongToTheirBeat()
        {
            Begin("makers");
            view.RunUntil(() => Speaking("yard.shard.offer"));
            Assert.That(flow.Ask("q.bowl.maker"), Is.False, "that question belongs to the previous beat");
            Assert.That(flow.Questions, Is.Empty, "this beat has none");
            view.RunUntil(() => flow.Phase == VisitPhase.Transitioning);
            Assert.That(flow.Questions, Is.Empty, "nothing is asked while the world changes");
        }

        [Test]
        public void ReturningDuringAnAnswerAbandonsIt()
        {
            Begin("travellers");
            view.RunUntil(() => QuestionIds.Contains("q.road.real"));
            Assert.That(flow.Ask("q.road.real"), Is.True);
            view.Pump();
            var answer = view.SpeakingToken;
            Assert.That(flow.ReturnToHearth(), Is.True);
            view.Pump();
            Assert.That(flow.LineFinished(answer), Is.False);
            view.RunUntil(() => flow.Phase == VisitPhase.Closing && flow.CurrentLine == null);
            Assert.That(view.Spoken.Last(), Is.EqualTo("early.later"));
        }

        [Test]
        public void NoticingSurfacesAQuestionWithoutChangingTheStory()
        {
            Begin("makers");
            view.RunUntil(() => Speaking("yard.dusk"));
            Assert.That(flow.Notice("bowl"), Is.False, "not yet visible");
            view.RunUntil(() => Speaking("yard.bowl.reveal"));
            var before = view.Cues.Count;
            Assert.That(flow.Notice("bowl"), Is.True);
            view.Pump();
            var noticed = view.Cues.Skip(before).ToList();
            Assert.That(noticed.Single(), Is.InstanceOf<Cue.Noticed>(), "nothing in the story changes");
            Assert.That(((Cue.Noticed)noticed[0]).QuestionId, Is.EqualTo("q.bowl.maker"));
            Assert.That(flow.Notice("bowl"), Is.True);
            var entries = flow.Journal.Entries.Where(e => e.Kind == JournalKind.Noticed).ToList();
            Assert.That(entries.Single().Detail, Does.Contain("head direction").And.Contain("not eye tracking"));
            Assert.That(flow.Notice("waymarker"), Is.False, "not part of this story");
        }

        // ---- keeping, returning, replaying and forgetting ----

        [Test]
        public void KeepingAVisitStoresTheRealisedPlanAndWhatHappened()
        {
            var kept = KeepAMakersVisit();
            Assert.That(store.Files.Keys, Is.EqualTo(new[] { kept.Id }));
            Assert.That(view.Of<Cue.Kept>().Single().Visit.Id, Is.EqualTo(kept.Id));
            var read = SavedVisitCodec.Read(store.Files[kept.Id]);
            Assert.That(read.Route, Is.EqualTo("makers"));
            Assert.That(read.Keepsake, Is.EqualTo("shard.kept"));
            Assert.That(read.Next, Is.EqualTo("makers.again"));
            Assert.That(read.Outcomes["receive-ember"], Is.EqualTo(Outcome.Accepted));
            Assert.That(read.Outcomes["touch-bowl"], Is.EqualTo(Outcome.Default));
            Assert.That(StoryWriter.PlanJson(read.Plan), Is.EqualTo(StoryWriter.PlanJson(flow.Plan.Plan)), "the plan exactly as it played");
            Assert.That(ControlKinds, Is.EqualTo(new[] { ControlKind.BeginAgain, ControlKind.Leave }));
            Assert.That(flow.Keep(), Is.False, "kept once");
        }

        [Test]
        public void AReturningVisitorIsWelcomedBackWithTheirKeepsake()
        {
            var kept = KeepAMakersVisit();
            Reopen();
            Assert.That(view.Spoken.First(), Is.EqualTo("back.sit"));
            Assert.That(view.Of<Cue.Shelf>().Single().ObjectId, Is.EqualTo("shard.kept"));
            AtChoice();
            Assert.That(ControlKinds, Is.EqualTo(new[] { ControlKind.Follow, ControlKind.Replay, ControlKind.NewStory, ControlKind.Forget, ControlKind.Pause }));
            Assert.That(flow.Controls[0].Label, Is.EqualTo("Bring the piece back"));
            Assert.That(flow.Controls[1].Target, Is.EqualTo(kept.Id));
            Assert.That(flow.NewStory(), Is.True);
            view.Pump();
            Assert.That(ControlKinds, Is.EqualTo(new[] { ControlKind.Choose, ControlKind.Choose, ControlKind.Back, ControlKind.Pause }));
            Assert.That(flow.Back(), Is.True);
            Assert.That(flow.Menu, Is.EqualTo(ChoiceMenu.Returning));
        }

        [Test]
        public void FollowingTheThreadIsANewChildVisitThatRecallsOnlyWhatWasKept()
        {
            var kept = KeepAMakersVisit();
            Reopen();
            AtChoice();
            Assert.That(flow.Follow(), Is.True);
            view.Pump();
            Assert.That(view.Of<Cue.VisitStarted>().Single().Mode, Is.EqualTo(VisitMode.Continued));
            Assert.That(flow.Plan.Plan.Parent, Is.EqualTo(kept.Id));
            Assert.That(flow.Journal.RelatedVisit, Is.EqualTo(kept.Id));
            PlayToClosing("touch-mend");
            Assert.That(view.Spoken, Does.Contain("again.makers.carried"), "the piece was taken by hand");
            Assert.That(view.Spoken, Does.Not.Contain("again.makers.held").And.Not.Contain("again.makers.felt"), "the bowl was never touched");
            var enter = view.Of<Cue.Transition>().First();
            Assert.That(enter.Continuities.Select(c => c.ToString()), Is.EquivalentTo(new[] { "ember->lantern", "shard.kept->shard" }));
            Assert.That(view.Of<Cue.Act>().Any(a => a.Action.Kind == ActionKind.Place && a.Action.At == "bowl"), Is.True);
            Assert.That(view.Of<Cue.Transition>().Last().Continuities.Select(c => c.ToString()), Is.EqualTo(new[] { "lantern->ember" }),
                "the piece stays in the mended bowl");
            Assert.That(view.Of<Cue.Keepsake>().Single().ObjectId, Is.EqualTo("ember"));

            Assert.That(flow.Controls.Single(c => c.Kind == ControlKind.Keep).Target, Is.EqualTo(kept.Id), "keeping this replaces the earlier visit");
            Assert.That(flow.Keep(), Is.True);
            Assert.That(store.Files.Count, Is.EqualTo(1));
            Assert.That(flow.KeptVisit.Parent, Is.EqualTo(kept.Id));
            Assert.That(flow.CanFollow, Is.False, "this thread has ended");
        }

        [Test]
        public void ReplayPlaysTheKeptPlanExactlyWithoutNewChoices()
        {
            var kept = KeepAMakersVisit();
            var original = flow.Journal.Heard.ToList();
            Reopen();
            AtChoice();
            Assert.That(flow.Replay(), Is.True);
            view.Pump();
            Assert.That(view.Of<Cue.VisitStarted>().Single().Mode, Is.EqualTo(VisitMode.Replayed));
            view.RunUntil(() => flow.OpenInvitation != null);
            Assert.That(view.Of<Cue.InvitationOpened>().Last().Replay, Is.True);
            Assert.That(flow.Accept(flow.OpenInvitation.Id), Is.False, "the kept outcome stands");
            Assert.That(ControlKinds, Does.Not.Contain(ControlKind.Accept));
            PlayToClosing();
            Assert.That(flow.Journal.Heard, Is.EqualTo(original), "every line as it was heard, in order");
            Assert.That(flow.Journal.Outcomes, Is.EquivalentTo(kept.Outcomes));
            Assert.That(flow.Journal.Entries.Where(e => e.Kind == JournalKind.InvitationResolved).All(e => e.Detail.StartsWith("as kept")), Is.True);
            Assert.That(ControlKinds, Does.Not.Contain(ControlKind.Keep), "a replay is not a new visit to keep");
        }

        [Test]
        public void AKeptPlanTheStageCannotShowIsNeverReplacedByAnotherStory()
        {
            var kept = KeepAMakersVisit();
            Tamper(kept.Id, plan => plan["keepsake"] = "drum");
            Reopen();
            AtChoice();
            Assert.That(flow.CanReplay, Is.True, "the plan still reads");
            Assert.That(flow.Replay(), Is.False);
            view.Pump();
            Assert.That(view.Of<Cue.StoreFailed>().Single().Operation, Is.EqualTo(StoreOperation.Replay));
            Assert.That(view.Of<Cue.VisitStarted>(), Is.Empty);
            Assert.That(ControlKinds, Does.Not.Contain(ControlKind.Replay));
            Assert.That(ControlKinds, Does.Contain(ControlKind.Follow), "the thread does not depend on the old plan");
        }

        [Test]
        public void AKeptVisitWhosePlanNoLongerReadsCanStillBeContinuedOrForgotten()
        {
            var kept = KeepAMakersVisit();
            Tamper(kept.Id, plan => plan["mystery"] = true);
            Reopen();
            AtChoice();
            Assert.That(flow.KeptVisit.Plan, Is.Null);
            Assert.That(flow.KeptVisit.PlanProblem, Does.Contain("mystery"));
            Assert.That(ControlKinds, Is.EqualTo(new[] { ControlKind.Follow, ControlKind.NewStory, ControlKind.Forget, ControlKind.Pause }));
        }

        [Test]
        public void ForgettingNeedsConfirmationAndRemovesTheKeepsakeAndTheThread()
        {
            var kept = KeepAMakersVisit();
            Reopen();
            AtChoice();
            Assert.That(flow.Forget(), Is.True);
            Assert.That(ControlKinds, Is.EqualTo(new[] { ControlKind.ConfirmForget, ControlKind.CancelForget }));
            Assert.That(flow.CancelForget(), Is.True);
            Assert.That(store.Files, Is.Not.Empty);
            flow.Forget();
            Assert.That(flow.ConfirmForget(), Is.True);
            view.Pump();
            Assert.That(store.Files, Is.Empty);
            Assert.That(view.Of<Cue.Forgotten>().Single().VisitId, Is.EqualTo(kept.Id));
            Assert.That(view.Of<Cue.Shelf>().Last().Visit, Is.Null);
            Assert.That(flow.Menu, Is.EqualTo(ChoiceMenu.Interests));
            Assert.That(flow.Follow(), Is.False);
            Reopen();
            Assert.That(view.Spoken.First(), Is.EqualTo("welcome.sit"), "a forgotten visit is not recalled");
        }

        [Test]
        public void AFailedSaveLeavesTheVisitUsableAndRetryable()
        {
            Begin("travellers");
            PlayToClosing();
            store.FailSaves = true;
            Assert.That(flow.Keep(), Is.False);
            view.Pump();
            Assert.That(view.Of<Cue.StoreFailed>().Single().Operation, Is.EqualTo(StoreOperation.Keep));
            Assert.That(ControlKinds, Does.Contain(ControlKind.Keep), "try again");
            Assert.That(flow.KeptVisit, Is.Null);
            store.FailSaves = false;
            Assert.That(flow.Keep(), Is.True);
            Assert.That(store.Files.Count, Is.EqualTo(1));
        }

        [Test]
        public void AFailedForgetKeepsEverythingAsItWas()
        {
            KeepAMakersVisit();
            Reopen();
            AtChoice();
            store.FailDeletes = true;
            flow.Forget();
            Assert.That(flow.ConfirmForget(), Is.False);
            view.Pump();
            Assert.That(view.Of<Cue.StoreFailed>().Single().Operation, Is.EqualTo(StoreOperation.Forget));
            Assert.That(flow.KeptVisit, Is.Not.Null);
            Assert.That(flow.Menu, Is.EqualTo(ChoiceMenu.Returning));
        }

        [Test]
        public void UnreadableKeptVisitsAreReportedAndTheHearthStillOpens()
        {
            var broken = new MemoryStore();
            broken.Problems.Add("v9.json: $.schema: unsupported kept-visit schema 7; expected 1");
            Open(broken);
            Assert.That(view.Of<Cue.StoreFailed>().Single().Operation, Is.EqualTo(StoreOperation.Load));
            Assert.That(view.Spoken.First(), Is.EqualTo("welcome.sit"));
        }

        [Test]
        public void ReturningVisitorsCanSkipTheWelcome()
        {
            Open(skipWelcome: true);
            Assert.That(flow.Phase, Is.EqualTo(VisitPhase.Choosing));
            Assert.That(view.Spoken, Is.Empty);
        }

        [Test]
        public void WithoutAStoreNothingCanBeKept()
        {
            flow = new VisitFlow(Playable(ReadPlaceholder()));
            view = new FakePresentation(flow);
            flow.Start(skipWelcome: true);
            flow.Choose("makers");
            PlayToClosing();
            Assert.That(ControlKinds, Is.EqualTo(new[] { ControlKind.BeginAgain, ControlKind.Leave }));
            Assert.That(flow.Keep(), Is.False);
        }
    }
}
