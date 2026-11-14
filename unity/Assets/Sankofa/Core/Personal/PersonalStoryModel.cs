using System.Collections.Generic;
using System.Linq;

namespace Sankofa.Core.Personal
{
    /// <summary>Visitor material guides a question; it is not evidence of ancestry or historical events.</summary>
    public sealed class PersonalStoryRequest
    {
        public PersonalStoryRequest() { }
        public PersonalStoryRequest(string region, string period, string interest, string fragment = null, string question = null)
        { Region = region; Period = period; Interest = interest; Fragment = fragment; Question = question; }
        public string Region { get; set; }
        public string Period { get; set; }
        public string Interest { get; set; }
        public string Fragment { get; set; }
        public string Question { get; set; }
    }

    public sealed class PersonalStoryDocument
    {
        public const string CurrentSchema = "sankofa.personal-story.v1";
        public string Schema { get; set; } = CurrentSchema;
        public string Id { get; set; }
        public string Title { get; set; }
        public string Region { get; set; }
        public string Period { get; set; }
        public string Interest { get; set; }
        public string Mode { get; set; }
        public IReadOnlyList<PersonalStoryChapter> Chapters { get; set; } = new PersonalStoryChapter[0];
        public IReadOnlyList<PersonalStorySource> Sources { get; set; } = new PersonalStorySource[0];
    }

    public sealed class PersonalStoryChapter
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Narration { get; set; }
        public string Illustration { get; set; }
        public string Truth { get; set; }
        public IReadOnlyList<string> Sources { get; set; } = new string[0];
        public IReadOnlyList<PersonalStoryQuestion> Questions { get; set; } = new PersonalStoryQuestion[0];
        /// <summary>Optional: whose side of the scene to follow next. The first choice is the authored default.</summary>
        public IReadOnlyList<PersonalStoryChoice> Choices { get; set; } = new PersonalStoryChoice[0];
    }

    /// <summary>A perspective the visitor may follow. It changes what is revealed, never what a character decides.</summary>
    public sealed class PersonalStoryChoice
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public string Next { get; set; }
    }

    /// <summary>
    /// Route rules shared by validation, the session and presentation. A chapter with choices continues to the chosen
    /// chapter; a chapter a choice leads to ends the telling unless it offers choices itself; any other chapter continues
    /// to the one after it, and the last chapter ends the telling.
    /// </summary>
    public static class PersonalStoryRoutes
    {
        public static int IndexOf(PersonalStoryDocument story, string chapterId)
        {
            if (story?.Chapters == null || chapterId == null) return -1;
            for (var i = 0; i < story.Chapters.Count; i++) if (story.Chapters[i]?.Id == chapterId) return i;
            return -1;
        }

        public static bool HasChoices(PersonalStoryChapter chapter) => chapter?.Choices != null && chapter.Choices.Count > 0;

        public static bool IsBranchTarget(PersonalStoryDocument story, int index)
        {
            var id = story.Chapters[index]?.Id;
            if (id == null) return false;
            // Plain loops: a telling asks this while a page is being told, and must not leave garbage each time.
            for (var c = 0; c < story.Chapters.Count; c++)
            {
                var chapter = story.Chapters[c];
                if (!HasChoices(chapter)) continue;
                for (var x = 0; x < chapter.Choices.Count; x++) if (chapter.Choices[x]?.Next == id) return true;
            }
            return false;
        }

        /// <summary>The chapter after <paramref name="index"/> without a visitor choice; -1 ends the telling.</summary>
        public static int Continue(PersonalStoryDocument story, int index)
        {
            var chapter = story.Chapters[index];
            if (HasChoices(chapter)) return IndexOf(story, chapter.Choices[0]?.Next);
            if (IsBranchTarget(story, index) || index + 1 >= story.Chapters.Count) return -1;
            return index + 1;
        }

        /// <summary>Pages still to read from <paramref name="index"/>, following authored defaults, including this one.</summary>
        public static int RemainingPages(PersonalStoryDocument story, int index)
        {
            var pages = 0;
            for (var i = index; i >= 0 && pages <= story.Chapters.Count; i = Continue(story, i)) pages++;
            return pages;
        }
    }

    public sealed class PersonalStoryQuestion
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public string Answer { get; set; }
        public string Truth { get; set; }
        public IReadOnlyList<string> Sources { get; set; } = new string[0];
    }

    public sealed class PersonalStorySource
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
    }

    public sealed class PersonalStoryComposition
    {
        public bool Success => Story != null;
        public PersonalStoryDocument Story { get; internal set; }
        public string Code { get; internal set; }
        public string Message { get; internal set; }
        /// <summary>
        /// Why this thread was chosen, in the catalog's own words. It quotes only recognised theme words, never
        /// arbitrary visitor text, and is presentation only: it is not part of the story or a kept telling.
        /// </summary>
        public string Reason { get; internal set; }
        public IReadOnlyList<string> Cues { get; internal set; } = new string[0];
    }

    public sealed class PersonalStoryIssue
    {
        public PersonalStoryIssue(string code, string path, string message) { Code = code; Path = path; Message = message; }
        public string Code { get; }
        public string Path { get; }
        public string Message { get; }
        public override string ToString() => Code + " " + Path + ": " + Message;
    }

    public sealed class PersonalStoryValidation
    {
        readonly List<PersonalStoryIssue> issues = new List<PersonalStoryIssue>();
        public bool IsValid => issues.Count == 0;
        public IReadOnlyList<PersonalStoryIssue> Issues => issues;
        internal void Add(string code, string path, string message) => issues.Add(new PersonalStoryIssue(code, path, message));
        public override string ToString() => string.Join("\n", issues.Select(i => i.ToString()));
    }
}
