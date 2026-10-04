using System;
using System.Collections.Generic;
using System.Linq;
using Sankofa.Core.Story;
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

        public void RunUntil(Func<bool> done, double maxSeconds = 600)
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

        void StartPlaceholder(FlowSettings settings = null)
        {
            flow = new VisitFlow(Playable(ReadPlaceholder()), settings);
            view = new FakePresentation(flow);
            flow.Start();
            view.Pump();
        }

        void BeginStory()
        {
            StartPlaceholder();
            view.RunUntil(() => flow.Phase == VisitPhase.AwaitingBegin);
            Assert.That(flow.Begin(), Is.True);
            view.Pump();
        }

        bool Speaking(string lineId) => flow.CurrentLine?.Id == lineId && view.SpeakingToken != 0;
        bool AwaitingInvitation => flow.OpenInvitation != null && flow.CurrentLine == null;
        IEnumerable<ControlKind> ControlKinds => flow.Controls.Select(c => c.Kind);

        [Test]
        public void WelcomeEndsAtTheHearthUntilTheVisitorBegins()
        {
            StartPlaceholder();
            Assert.That(flow.Phase, Is.EqualTo(VisitPhase.Welcome));
            view.Advance(60);
            Assert.That(flow.Phase, Is.EqualTo(VisitPhase.AwaitingBegin), "no input keeps the visitor at the hearth");
            Assert.That(view.Spoken, Is.EqualTo(new[] { "welcome.sit", "welcome.begin" }));
            Assert.That(ControlKinds, Is.EqualTo(new[] { ControlKind.Begin, ControlKind.Pause }));
            Assert.That(flow.Begin(), Is.True);
            view.Pump();
            Assert.That(view.Spoken.Last(), Is.EqualTo("offer.light"));
        }

        [Test]
        public void VisitorCanBeginDuringTheWelcome()
        {
            StartPlaceholder();
            view.Advance(1);
            var welcomeToken = view.SpeakingToken;
            Assert.That(flow.Begin(), Is.True);
            view.Pump();
            Assert.That(view.Of<Cue.StopSpeaking>().Count(), Is.EqualTo(1));
            Assert.That(flow.LineFinished(welcomeToken), Is.False, "a late report from the interrupted welcome changes nothing");
            Assert.That(flow.Journal.HasHeard("welcome.sit"), Is.False, "an interrupted line is not recorded as heard");
            Assert.That(view.Spoken.Last(), Is.EqualTo("offer.light"));
        }

        [Test]
        public void PassiveVisitCompletesWithoutClaimingAChoice()
        {
            BeginStory();
            view.RunUntil(() => flow.Phase == VisitPhase.Closing);

            Assert.That(flow.Journal.Outcomes["receive-ember"], Is.EqualTo(Outcome.Default));
            var resolved = flow.Journal.Entries.Single(e => e.Kind == JournalKind.InvitationResolved);
            Assert.That(resolved.Detail, Is.EqualTo("default path; no visitor choice"));
            Assert.That(view.Spoken, Does.Contain("offer.default").And.Contain("reveal.placed"));
            Assert.That(view.Spoken, Does.Not.Contain("offer.accepted").And.Not.Contain("reveal.carried"));
            Assert.That(view.Of<Cue.Act>().Select(a => a.Action.ToString()), Does.Contain("Place ember"));

            var transitions = view.Of<Cue.Transition>().ToList();
            Assert.That(transitions.Select(t => t.Kind), Is.EqualTo(new[] { TransitionKind.Enter, TransitionKind.Return }));
            Assert.That(transitions[0].Continuities.Single().ToString(), Is.EqualTo("ember->lantern"));
            Assert.That(transitions[1].Continuities.Single().ToString(), Is.EqualTo("lantern->ember"));

            Assert.That(flow.Journal.Completed, Is.True);
            Assert.That(view.Of<Cue.Keepsake>().Single().ObjectId, Is.EqualTo("ember"));
            Assert.That(view.Of<Cue.Closed>().Single().Completed, Is.True);
            Assert.That(ControlKinds, Is.EqualTo(new[] { ControlKind.BeginAgain, ControlKind.Leave }));
        }

        [Test]
        public void ADeliberateActionIsAcknowledgedOnceAndChangesTheReveal()
        {
            BeginStory();
            Assert.That(flow.Accept("receive-ember"), Is.False, "nothing is offered yet");
            view.RunUntil(() => Speaking("offer.hand"));

            var accept = flow.Controls.Single(c => c.Kind == ControlKind.Accept);
            Assert.That(accept.InvitationId, Is.EqualTo("receive-ember"));
            Assert.That(accept.Label, Is.EqualTo("Take the ember"), "the nearby select alternative to the gesture");

            Assert.That(flow.Accept("receive-ember"), Is.True);
            Assert.That(flow.Accept("receive-ember"), Is.False, "the same action is acknowledged once");
            view.RunUntil(() => flow.Phase == VisitPhase.Closing);

            Assert.That(view.Of<Cue.InvitationClosed>().Single().Outcome, Is.EqualTo(Outcome.Accepted));
            Assert.That(view.Spoken, Does.Contain("offer.accepted").And.Contain("reveal.carried"));
            Assert.That(view.Spoken, Does.Not.Contain("reveal.placed"));
            var resolvedAt = flow.Journal.Entries.Single(e => e.Kind == JournalKind.InvitationResolved).Sequence;
            var offerHeardAt = flow.Journal.Entries.Single(e => e.Kind == JournalKind.LineHeard && e.Subject == "offer.hand").Sequence;
            Assert.That(resolvedAt, Is.LessThan(offerHeardAt), "accepted while the offer was spoken; the line still finished");
        }

        [Test]
        public void TheVisitorCannotAcceptAfterTheDefaultPath()
        {
            BeginStory();
            view.RunUntil(() => flow.Journal.Outcomes.ContainsKey("receive-ember"));
            Assert.That(flow.Accept("receive-ember"), Is.False);
            Assert.That(ControlKinds, Does.Not.Contain(ControlKind.Accept));
        }

        [Test]
        public void PauseInvalidatesPlaybackAndResumeDoesNotRepeatCompletedActions()
        {
            BeginStory();
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
            BeginStory();
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
            BeginStory();
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
            BeginStory();
            view.RunUntil(() => flow.Phase == VisitPhase.Transitioning);
            Assert.That(flow.Pause(), Is.True);
            view.RunUntil(() => flow.Phase == VisitPhase.Paused);
            Assert.That(flow.Scene.Id, Is.EqualTo("story.yard"));
            Assert.That(view.Spoken, Does.Not.Contain("enter.lantern"));
            Assert.That(flow.Resume(), Is.True);
            view.Pump();
            Assert.That(view.Spoken.Last(), Is.EqualTo("enter.lantern"));
        }

        [Test]
        public void ReturningEarlyDiscardsLateResultsAndCarriesTheLanternHome()
        {
            BeginStory();
            view.RunUntil(() => Speaking("enter.bowl"));
            var interrupted = view.SpeakingToken;

            Assert.That(flow.ReturnToHearth(), Is.True);
            view.Pump();
            Assert.That(flow.LineFinished(interrupted), Is.False);
            var back = view.Of<Cue.Transition>().Last();
            Assert.That(back.Kind, Is.EqualTo(TransitionKind.Return));
            Assert.That(back.Continuities.Single().ToString(), Is.EqualTo("lantern->ember"));

            view.RunUntil(() => flow.Phase == VisitPhase.Closing && flow.CurrentLine == null);
            Assert.That(view.Spoken.Last(), Is.EqualTo("early.later"));
            Assert.That(flow.Journal.ReturnedEarly, Is.True);
            Assert.That(flow.Journal.Completed, Is.False, "an early return is never presented as a finished story");
            Assert.That(view.Of<Cue.Closed>().Single().Completed, Is.False);
            Assert.That(flow.Journal.HasHeard("early.later"), Is.True, "the journal closes after the farewell is heard");
            Assert.That(view.Of<Cue.Keepsake>().Single().ObjectId, Is.EqualTo("ember"));
        }

        [Test]
        public void ReturningBeforeAnInvitationResolvesWithdrawsIt()
        {
            BeginStory();
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
            StartPlaceholder();
            view.ReportLines = false;
            view.ReportTransitions = false;
            view.RunUntil(() => flow.Phase == VisitPhase.AwaitingBegin);
            flow.Begin();
            view.RunUntil(() => flow.Phase == VisitPhase.Closing, 1000);
            Assert.That(flow.Journal.Completed, Is.True);
        }

        [Test]
        public void AMeasuredClipLengthExtendsTheWatchdog()
        {
            StartPlaceholder();
            view.ReportLines = false;
            var token = view.SpeakingToken;
            Assert.That(flow.ReportLineDuration(token, 30), Is.True);
            view.Advance(37);
            Assert.That(flow.CurrentLine?.Id, Is.EqualTo("welcome.sit"), "3.2 s × 2 + 8 would have expired; the measured 30 s + 8 has not");
            view.Advance(2);
            Assert.That(flow.Journal.HasHeard("welcome.sit"), Is.True);
        }

        [Test]
        public void LeavingEndsTheVisitFromAnyPhase()
        {
            BeginStory();
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
        public void BeginAgainStartsANewVisitWithoutTheWelcome()
        {
            BeginStory();
            view.RunUntil(() => flow.Phase == VisitPhase.Closing);
            var resets = view.Of<Cue.ResetStage>().Count();
            Assert.That(flow.BeginAgain(), Is.True);
            view.Pump();
            Assert.That(flow.Journal.VisitNumber, Is.EqualTo(2));
            Assert.That(view.Of<Cue.ResetStage>().Count(), Is.EqualTo(resets + 1));
            Assert.That(view.Spoken.Last(), Is.EqualTo("offer.light"));
            Assert.That(flow.Journal.Outcomes, Is.Empty, "a new visit starts with no outcomes");
        }

        [Test]
        public void ReturningVisitorsCanSkipTheWelcome()
        {
            flow = new VisitFlow(Playable(ReadPlaceholder()));
            view = new FakePresentation(flow);
            flow.Start(skipWelcome: true);
            view.Pump();
            Assert.That(flow.Phase, Is.EqualTo(VisitPhase.AwaitingBegin));
            Assert.That(view.Spoken, Is.Empty);
        }
    }
}
