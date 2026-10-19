using System;
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
        public PersonalStoryState State { get; private set; } = PersonalStoryState.Intake;
        public PersonalStoryDocument Story { get; private set; }
        public int ChapterIndex { get; private set; }
        public PersonalStoryChapter Chapter => Story != null ? Story.Chapters[ChapterIndex] : null;
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
            Story = null; Question = null; ChapterIndex = 0; IsReplay = false;
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
            Story = story; ChapterIndex = 0; Question = null; Problem = null;
            State = PersonalStoryState.Reading;
            Changed?.Invoke();
            return true;
        }

        public bool OpenSaved(PersonalStoryDocument story)
        {
            var validation = PersonalStoryValidator.Validate(story);
            if (!validation.IsValid) return false;
            requestVersion++;
            request = null; Story = story; Question = null; ChapterIndex = 0; Problem = null; IsReplay = true;
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
            ChapterIndex = 0; IsReplay = false; State = PersonalStoryState.Intake;
            Changed?.Invoke();
        }

        public bool Next()
        {
            if (Paused || State != PersonalStoryState.Reading) return false;
            if (ChapterIndex + 1 < Story.Chapters.Count) ChapterIndex++;
            else State = PersonalStoryState.Closing;
            Question = null; Changed?.Invoke(); return true;
        }

        public bool Back()
        {
            if (Paused || (State != PersonalStoryState.Reading && State != PersonalStoryState.Closing)) return false;
            if (State == PersonalStoryState.Closing) State = PersonalStoryState.Reading;
            else if (ChapterIndex > 0) ChapterIndex--;
            else return false;
            Question = null; Changed?.Invoke(); return true;
        }

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
