using System.Linq;
using NUnit.Framework;
using Sankofa.Core.Personal;
using Sankofa.Flow;

namespace Sankofa.Tests
{
    public sealed class PersonalSessionTests
    {
        static PersonalStoryRequest Request(string interest = "journeys") => new PersonalStoryRequest("enkare-nairobi", "1850s", interest);
        static PersonalStoryDocument Story(string interest = "journeys") => BundledPersonalStoryComposer.Compose(Request(interest)).Story;

        [Test]
        public void ACancelledRequestCannotResurrectAStory()
        {
            var s = new PersonalStorySession(); var token = s.Begin(Request()); s.Cancel();
            Assert.That(s.Accept(token, Story()), Is.False);
            Assert.That(s.State, Is.EqualTo(PersonalStoryState.Intake)); Assert.That(s.Story, Is.Null);
        }

        [Test]
        public void AnOlderResponseCannotReplaceANewerQuestion()
        {
            var s = new PersonalStorySession(); var old = s.Begin(Request()); var current = s.Begin(Request("place-water"));
            Assert.That(s.Accept(old, Story()), Is.False); Assert.That(s.Accept(current, Story("place-water")), Is.True);
            Assert.That(s.Story.Interest, Is.EqualTo("place-water"));
        }

        [Test]
        public void PausingBlocksStoryNavigationWithoutLosingThePage()
        {
            var s = new PersonalStorySession(); s.Accept(s.Begin(Request()), Story()); s.Next(); var page = s.Chapter.Id;
            s.Pause(true); Assert.That(s.Next(), Is.False); Assert.That(s.Back(), Is.False); Assert.That(s.Ask(0), Is.False);
            Assert.That(s.Chapter.Id, Is.EqualTo(page)); s.Pause(false); Assert.That(s.Next(), Is.True);
        }

        [Test]
        public void QuestionsAndSourceDetailsReturnToTheSameTelling()
        {
            var s = new PersonalStorySession(); s.Accept(s.Begin(Request()), Story());
            var page = s.Chapter.Id; Assert.That(s.Ask(0), Is.True); var answer = s.Question.Answer;
            Assert.That(s.Sources(), Is.True); Assert.That(s.ReturnToStory(), Is.True);
            Assert.That(s.State, Is.EqualTo(PersonalStoryState.Question)); Assert.That(s.Question.Answer, Is.EqualTo(answer));
            Assert.That(s.ReturnToStory(), Is.True); Assert.That(s.Chapter.Id, Is.EqualTo(page));
        }

        [Test]
        public void ClosingDoesNotForceSavingAndReopeningNeedsNoGeneration()
        {
            var s = new PersonalStorySession(); s.Accept(s.Begin(Request()), Story()); var realised = s.Story;
            for (var i = 0; i < realised.Chapters.Count; i++) s.Next();
            Assert.That(s.State, Is.EqualTo(PersonalStoryState.Closing)); Assert.That(s.Next(), Is.False);
            s.Cancel(); Assert.That(s.OpenSaved(realised), Is.True); Assert.That(s.IsReplay, Is.True);
            Assert.That(s.Story, Is.SameAs(realised)); Assert.That(s.ChapterIndex, Is.Zero);
        }

        [Test]
        public void ContinuingAtAChoiceFollowsTheAuthoredDefaultWithoutClaimingTheVisitorChose()
        {
            var s = new PersonalStorySession(); s.Accept(s.Begin(Request()), Story());
            Assert.That(s.PageNumber, Is.EqualTo(1)); Assert.That(s.PageCount, Is.EqualTo(3)); Assert.That(s.OffersChoice, Is.False);
            Assert.That(s.Choose(0), Is.False, "the opening offers no perspective choice");
            Assert.That(s.Next(), Is.True); Assert.That(s.OffersChoice, Is.True);
            var offered = s.Chapter.Choices;
            Assert.That(s.Next(), Is.True);
            Assert.That(s.Chapter.Id, Is.EqualTo(offered[0].Next));
            Assert.That(s.Followed, Is.SameAs(offered[0])); Assert.That(s.FollowedByChoice, Is.False);
            Assert.That(s.NotFollowed, Is.EqualTo(new[] { offered[1] }));
            Assert.That(s.PageNumber, Is.EqualTo(3)); Assert.That(s.PageCount, Is.EqualTo(3));
            Assert.That(s.PagesBefore.Select(c => c.Id), Is.EqualTo(new[] { s.Story.Chapters[0].Id, s.Story.Chapters[1].Id }));
            Assert.That(s.Next(), Is.True); Assert.That(s.State, Is.EqualTo(PersonalStoryState.Closing));
        }

        [Test]
        public void ChoosingTheOtherSideRevealsADifferentEndingAndBackReopensTheChoice()
        {
            foreach (var interest in PersonalStoryCatalog.Interests)
            {
                var s = new PersonalStorySession(); s.Accept(s.Begin(Request(interest)), Story(interest)); s.Next();
                var offered = s.Chapter.Choices;
                s.Pause(true); Assert.That(s.Choose(1), Is.False); s.Pause(false);
                Assert.That(s.Choose(2), Is.False); Assert.That(s.Choose(-1), Is.False);
                Assert.That(s.Choose(1), Is.True);
                Assert.That(s.Chapter.Id, Is.EqualTo(offered[1].Next));
                Assert.That(s.FollowedByChoice, Is.True); Assert.That(s.Followed, Is.SameAs(offered[1]));
                Assert.That(s.PageNumber, Is.EqualTo(3)); Assert.That(s.PageCount, Is.EqualTo(3));
                Assert.That(s.Choose(0), Is.False, "an ending offers no further choice");
                Assert.That(s.Back(), Is.True);
                Assert.That(s.OffersChoice, Is.True); Assert.That(s.Followed, Is.Null);
                Assert.That(s.Choose(0), Is.True); Assert.That(s.Chapter.Id, Is.EqualTo(offered[0].Next));
                Assert.That(s.Next(), Is.True); Assert.That(s.State, Is.EqualTo(PersonalStoryState.Closing));
                Assert.That(s.Back(), Is.True); Assert.That(s.Chapter.Id, Is.EqualTo(offered[0].Next), "the last page of the followed side stays readable");
            }
        }

        [Test]
        public void ReopeningAKeptTellingOffersTheChoiceAgainWithoutAnEarlierRoute()
        {
            var s = new PersonalStorySession(); s.Accept(s.Begin(Request()), Story()); s.Next(); s.Choose(1);
            var kept = s.Story; s.Cancel();
            Assert.That(s.OpenSaved(kept), Is.True);
            Assert.That(s.ChapterIndex, Is.Zero); Assert.That(s.PageNumber, Is.EqualTo(1)); Assert.That(s.Followed, Is.Null);
            s.Next(); Assert.That(s.OffersChoice, Is.True);
        }

        [Test]
        public void UnsupportedRequestsNeverEnterPreparing()
        {
            var s = new PersonalStorySession(); s.Begin(new PersonalStoryRequest("unreviewed-context", "1850s", "journeys"));
            Assert.That(s.State, Is.EqualTo(PersonalStoryState.Error)); Assert.That(s.Problem, Is.Not.Empty);
        }

        [Test]
        public void InvalidProviderContentNeverBecomesPlayable()
        {
            var s = new PersonalStorySession(); var token = s.Begin(Request()); var invalid = Story(); invalid.Chapters[0].Illustration = "https://untrusted.example/image";
            Assert.That(s.Accept(token, invalid), Is.False); Assert.That(s.Story, Is.Null); Assert.That(s.State, Is.EqualTo(PersonalStoryState.Error));
        }
    }
}
