using System;
using System.Collections.Generic;
using System.Linq;
using Sankofa.Core.Personal;

namespace Sankofa.Flow
{
    public enum PersonalStoryState { Intake, Preparing, Reading, Question, Sources, Closing, Error }

    /// <summary>Owns the accepted telling and navigation. Late network replies never change a newer visit.</summary>
    public sealed class PersonalStorySession
    {
        /// <summary>One perspective taken on this visit: which, who chose it, and how long the route was once it was taken.</summary>
        sealed class Step
        {
            public PersonalStoryChoice Side;
            public bool Chosen;
            public int RouteLength;
            /// <summary>The perspectives that were offered where this one was taken.</summary>
            public IReadOnlyList<PersonalStoryChoice> Offered;
        }

        int requestVersion;
        PersonalStoryState beforeDetails;
        PersonalStoryRequest request;
        readonly List<int> route = new List<int> { 0 };
        readonly List<Step> steps = new List<Step>();
        public PersonalStoryState State { get; private set; } = PersonalStoryState.Intake;
        public PersonalStoryDocument Story { get; private set; }
        public int ChapterIndex => route[route.Count - 1];
        public PersonalStoryChapter Chapter => Story != null ? Story.Chapters[ChapterIndex] : null;
        /// <summary>Pages read so far on this route, including the current one.</summary>
        public int PageNumber => route.Count;
        /// <summary>Pages on this route, assuming the authored default at any choice still ahead.</summary>
        public int PageCount => Story == null ? 0 : route.Count - 1 + PersonalStoryRoutes.RemainingPages(Story, ChapterIndex);
        public IReadOnlyList<PersonalStoryChapter> PagesBefore => Story == null ? new PersonalStoryChapter[0] : route.Take(route.Count - 1).Select(i => Story.Chapters[i]).ToArray();
        public bool OffersChoice => State == PersonalStoryState.Reading && PersonalStoryRoutes.HasChoices(Chapter);
        /// <summary>The perspective being followed now, and whether the visitor chose it or let the fire choose.</summary>
        public PersonalStoryChoice Followed => steps.Count > 0 ? steps[steps.Count - 1].Side : null;
        public bool FollowedByChoice => steps.Count > 0 && steps[steps.Count - 1].Chosen;
        /// <summary>Every perspective heard on this visit, in the order heard.</summary>
        public IReadOnlyList<PersonalStoryChoice> Heard => steps.Select(s => s.Side).ToArray();
        /// <summary>Perspectives of the same meeting not yet heard on this visit, in authored order.</summary>
        public IReadOnlyList<PersonalStoryChoice> NotFollowed
        {
            get
            {
                if (steps.Count == 0) return new PersonalStoryChoice[0];
                var offered = steps[steps.Count - 1].Offered;
                return offered.Where(c => !steps.Any(s => ReferenceEquals(s.Offered, offered) && s.Side == c)).ToArray();
            }
        }
        /// <summary>
        /// One side has been told to its end and another has not been heard on this visit. The visitor may hear it
        /// now, from where the sides part, without the opening again; or finish with <see cref="Next"/>.
        /// </summary>
        public bool OffersOtherSide
        {
            get
            {
                // Asked every frame while a side is told, so it allocates nothing.
                if (State != PersonalStoryState.Reading || steps.Count == 0 || PersonalStoryRoutes.HasChoices(Chapter)) return false;
                return HasUnheardSide() && PersonalStoryRoutes.Continue(Story, ChapterIndex) < 0;
            }
        }
        bool HasUnheardSide()
        {
            var offered = steps[steps.Count - 1].Offered;
            for (var i = 0; i < offered.Count; i++)
            {
                var heard = false;
                for (var s = 0; s < steps.Count && !heard; s++) heard = ReferenceEquals(steps[s].Offered, offered) && steps[s].Side == offered[i];
                if (!heard) return true;
            }
            return false;
        }
        public PersonalStoryQuestion Question { get; private set; }
        public bool Paused { get; private set; }
        public string Problem { get; private set; }
        public bool IsReplay { get; private set; }
        public int RequestVersion => requestVersion;
        public event Action Changed;

        public int Begin(PersonalStoryRequest input)
        {
            requestVersion++;
            request = input == null ? null : new PersonalStoryRequest(input.Region, input.Period, input.Interest, input.Fragment, input.Question);
            var validation = PersonalStoryValidator.ValidateRequest(request);
            Story = null; Question = null; ResetRoute(); IsReplay = false;
            Problem = validation.IsValid ? null : validation.Issues[0].Message;
            State = validation.IsValid ? PersonalStoryState.Preparing : PersonalStoryState.Error;
            Changed?.Invoke();
            return requestVersion;
        }

        public bool Accept(int version, PersonalStoryDocument story)
        {
            if (version != requestVersion || State != PersonalStoryState.Preparing) return false;
            var validation = PersonalStoryValidator.Validate(story, request);
            if (!validation.IsValid) { Fail(version, "This telling could not be checked. Your choices are still here."); return false; }
            Story = story; ResetRoute(); Question = null; Problem = null;
            State = PersonalStoryState.Reading;
            Changed?.Invoke();
            return true;
        }

        public bool OpenSaved(PersonalStoryDocument story)
        {
            var validation = PersonalStoryValidator.Validate(story);
            if (!validation.IsValid) return false;
            requestVersion++;
            request = null; Story = story; Question = null; ResetRoute(); Problem = null; IsReplay = true;
            State = PersonalStoryState.Reading;
            Changed?.Invoke(); return true;
        }

        public void Fail(int version, string message)
        {
            if (version != requestVersion || State != PersonalStoryState.Preparing) return;
            Problem = message; State = PersonalStoryState.Error; Changed?.Invoke();
        }

        public void Cancel()
        {
            requestVersion++; Story = null; Question = null; Problem = null; request = null;
            ResetRoute(); IsReplay = false; State = PersonalStoryState.Intake;
            Changed?.Invoke();
        }

        /// <summary>Turns the page. At a choice, a visitor who simply continues follows the authored default, which is not recorded as their choice.</summary>
        public bool Next()
        {
            if (Paused || State != PersonalStoryState.Reading) return false;
            if (PersonalStoryRoutes.HasChoices(Chapter)) return Follow(0, false);
            var next = PersonalStoryRoutes.Continue(Story, ChapterIndex);
            if (next >= 0) route.Add(next);
            else State = PersonalStoryState.Closing;
            Question = null; Changed?.Invoke(); return true;
        }

        /// <summary>Follows one offered perspective. It changes what the telling reveals next; characters still make their own decisions.</summary>
        public bool Choose(int index)
        {
            if (Paused || !OffersChoice || index < 0 || index >= Chapter.Choices.Count) return false;
            return Follow(index, true);
        }

        bool Follow(int index, bool chosen)
        {
            var choices = Chapter.Choices;
            var target = PersonalStoryRoutes.IndexOf(Story, choices[index].Next);
            if (target <= ChapterIndex) return false;
            route.Add(target);
            steps.Add(new Step { Side = choices[index], Chosen = chosen, RouteLength = route.Count, Offered = choices });
            Question = null; Changed?.Invoke(); return true;
        }

        /// <summary>
        /// Tells another side of the same meeting once one side has ended. <paramref name="index"/> counts within
        /// <see cref="NotFollowed"/>. Pass <paramref name="chosen"/> false when the fire turned the stage because the
        /// visitor waited, so that is never recorded as their choice.
        /// </summary>
        public bool HearOtherSide(int index, bool chosen)
        {
            if (Paused || !OffersOtherSide) return false;
            var unheard = NotFollowed;
            if (index < 0 || index >= unheard.Count) return false;
            var target = PersonalStoryRoutes.IndexOf(Story, unheard[index].Next);
            if (target < 0 || route.Contains(target)) return false;
            route.Add(target);
            steps.Add(new Step { Side = unheard[index], Chosen = chosen, RouteLength = route.Count, Offered = steps[steps.Count - 1].Offered });
            Question = null; Changed?.Invoke(); return true;
        }

        public bool Back()
        {
            if (Paused || (State != PersonalStoryState.Reading && State != PersonalStoryState.Closing)) return false;
            if (State == PersonalStoryState.Closing) State = PersonalStoryState.Reading;
            else if (route.Count > 1)
            {
                route.RemoveAt(route.Count - 1);
                // Stepping back past where a side was taken undoes it: back to a choice reopens it, and back from a
                // second side returns to the first side's ending with the second no longer heard.
                steps.RemoveAll(s => s.RouteLength > route.Count);
            }
            else return false;
            Question = null; Changed?.Invoke(); return true;
        }

        void ResetRoute() { route.Clear(); route.Add(0); steps.Clear(); }

        public bool Ask(int index)
        {
            if (Paused || State != PersonalStoryState.Reading || index < 0 || index >= Chapter.Questions.Count) return false;
            Question = Chapter.Questions[index]; State = PersonalStoryState.Question;
            Changed?.Invoke(); return true;
        }

        public bool Sources()
        {
            if (Paused || Story == null || State == PersonalStoryState.Preparing || State == PersonalStoryState.Intake) return false;
            beforeDetails = State; State = PersonalStoryState.Sources;
            Changed?.Invoke(); return true;
        }

        public bool ReturnToStory()
        {
            if (Paused) return false;
            if (State == PersonalStoryState.Sources) State = beforeDetails == PersonalStoryState.Sources ? PersonalStoryState.Reading : beforeDetails;
            else if (State == PersonalStoryState.Question) { State = PersonalStoryState.Reading; Question = null; }
            else return false;
            Changed?.Invoke(); return true;
        }

        public void Pause(bool value)
        {
            if (Paused == value) return;
            Paused = value; Changed?.Invoke();
        }
    }
}
