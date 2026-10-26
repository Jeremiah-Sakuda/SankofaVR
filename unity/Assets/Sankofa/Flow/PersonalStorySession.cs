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
        int requestVersion;
        PersonalStoryState beforeDetails;
        PersonalStoryRequest request;
        readonly List<int> route = new List<int> { 0 };
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
        /// <summary>The perspective followed at the last choice point, and whether the visitor chose it or let the default stand.</summary>
        public PersonalStoryChoice Followed { get; private set; }
        public bool FollowedByChoice { get; private set; }
        /// <summary>Perspectives offered alongside the one followed, which a later reading could take.</summary>
        public IReadOnlyList<PersonalStoryChoice> NotFollowed { get; private set; } = new PersonalStoryChoice[0];
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
            Followed = choices[index]; FollowedByChoice = chosen;
            NotFollowed = choices.Where((c, i) => i != index).ToArray();
            route.Add(target);
            Question = null; Changed?.Invoke(); return true;
        }

        public bool Back()
        {
            if (Paused || (State != PersonalStoryState.Reading && State != PersonalStoryState.Closing)) return false;
            if (State == PersonalStoryState.Closing) State = PersonalStoryState.Reading;
            else if (route.Count > 1)
            {
                route.RemoveAt(route.Count - 1);
                // Stepping back to a choice reopens it; the earlier path is no longer the one being followed.
                if (PersonalStoryRoutes.HasChoices(Chapter)) ClearFollowed();
            }
            else return false;
            Question = null; Changed?.Invoke(); return true;
        }

        void ResetRoute() { route.Clear(); route.Add(0); ClearFollowed(); }
        void ClearFollowed() { Followed = null; FollowedByChoice = false; NotFollowed = new PersonalStoryChoice[0]; }

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
