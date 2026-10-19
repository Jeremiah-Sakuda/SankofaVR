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
