using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sankofa.Core.Personal
{
    public static class PersonalStoryValidator
    {
        public const int MaxFragmentLength = 1000;
        public const int MaxQuestionLength = 500;
        public const int MaxNarrationLength = 2000;
        public const int MaxJsonLength = 48000;
        static readonly Regex IdPattern = new Regex("^[a-zA-Z0-9][a-zA-Z0-9._-]{0,119}$", RegexOptions.CultureInvariant);
        static readonly string[] IdentityAssertions =
        {
            "your ancestor was", "your ancestors were", "your ancestors lived", "your family lived", "your family came from",
            "your grandmother was", "your grandfather was", "you descend from", "your lineage is", "this proves your",
        };

        public static PersonalStoryValidation ValidateRequest(PersonalStoryRequest request)
        {
            var report = new PersonalStoryValidation();
            if (request == null) { report.Add("request.missing", "$", "A request is required."); return report; }
            Text(report, request.Region, "$.region", 80, true);
            Text(report, request.Period, "$.period", 80, false);
            Text(report, request.Interest, "$.interest", 80, false);
            Text(report, request.Fragment, "$.fragment", MaxFragmentLength, false);
            Text(report, request.Question, "$.question", MaxQuestionLength, false);
            if (PersonalStoryCatalog.NormalizeRegion(request.Region) == null)
                report.Add("coverage.region", "$.region", "This place is outside the current context. Choose Enkare Nairobi explicitly to explore that setting.");
            if (PersonalStoryCatalog.NormalizePeriod(request.Period) == null)
                report.Add("coverage.period", "$.period", "This period is outside the current context. The bundled telling covers the 1850s, before the railway.");
            if (!string.IsNullOrWhiteSpace(request.Interest) && PersonalStoryCatalog.NormalizeInterest(request.Interest) == null)
                report.Add("coverage.interest", "$.interest", "Choose journeys, trade and household, or place and water for this context.");
            return report;
        }

        public static PersonalStoryValidation Validate(PersonalStoryDocument story, PersonalStoryRequest originalRequest = null)
        {
            var report = new PersonalStoryValidation();
            if (story == null) { report.Add("story.missing", "$", "A story is required."); return report; }
            if (story.Schema != PersonalStoryDocument.CurrentSchema) report.Add("schema.unsupported", "$.schema", "Unsupported personal-story schema.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            Id(report, ids, story.Id, "$.id");
            Text(report, story.Title, "$.title", 160);
            if (story.Region != PersonalStoryCatalog.Region) report.Add("coverage.region", "$.region", "Unsupported region; no substitution is allowed.");
            if (story.Period != PersonalStoryCatalog.Period) report.Add("coverage.period", "$.period", "Unsupported period.");
            if (!PersonalStoryCatalog.Interests.Contains(story.Interest)) report.Add("coverage.interest", "$.interest", "Unsupported interest.");
            if (story.Mode != "generated" && story.Mode != "bundled") report.Add("mode.invalid", "$.mode", "Mode must be generated or bundled.");

            var sources = new HashSet<string>(StringComparer.Ordinal);
            if (story.Sources == null || story.Sources.Count > 3) report.Add("sources.count", "$.sources", "At most three approved sources are allowed.");
            else for (var i = 0; i < story.Sources.Count; i++)
            {
                var source = story.Sources[i];
                var path = "$.sources[" + i + "]";
                if (source == null) { report.Add("source.missing", path, "A source must be an object."); continue; }
                var approved = PersonalStoryCatalog.Source(source.Id);
                if (approved == null || source.Url != approved.Url || source.Title != approved.Title)
                    report.Add("source.unapproved", path, "Source IDs, titles and URLs must match the approved source register.");
                if (source.Id == null || !sources.Add(source.Id)) report.Add("source.duplicate", path + ".id", "Source IDs must be unique.");
            }
            if (story.Chapters == null || story.Chapters.Count < 3 || story.Chapters.Count > 6)
                report.Add("chapters.count", "$.chapters", "A complete telling needs three to six chapters.");
            if (story.Chapters == null) return report;
            for (var i = 0; i < Math.Min(story.Chapters.Count, 6); i++)
            {
                var chapter = story.Chapters[i];
                var path = "$.chapters[" + i + "]";
                if (chapter == null) { report.Add("chapter.missing", path, "A chapter must be an object."); continue; }
                Id(report, ids, chapter.Id, path + ".id");
                Text(report, chapter.Title, path + ".title", 160);
                Text(report, chapter.Narration, path + ".narration", MaxNarrationLength);
                if (!PersonalStoryCatalog.Illustrations.Contains(chapter.Illustration)) report.Add("asset.unapproved", path + ".illustration", "Use an approved art ID, never an asset URL.");
                if (chapter.Truth == "historical" && chapter.Illustration == "modern-river") report.Add("asset.period", path + ".illustration", "A contemporary reconstruction cannot illustrate documented 1850s context.");
                Evidence(report, chapter.Truth, chapter.Narration, chapter.Sources, sources, path, originalRequest);
                if (chapter.Questions == null || chapter.Questions.Count > 3) report.Add("questions.count", path + ".questions", "At most three contextual questions are allowed per chapter.");
                if (chapter.Questions == null) continue;
                for (var q = 0; q < Math.Min(chapter.Questions.Count, 3); q++)
                {
                    var question = chapter.Questions[q];
                    var qpath = path + ".questions[" + q + "]";
                    if (question == null) { report.Add("question.missing", qpath, "A question must be an object."); continue; }
                    Id(report, ids, question.Id, qpath + ".id");
                    Text(report, question.Label, qpath + ".label", 160);
                    Text(report, question.Answer, qpath + ".answer", 1200);
                    Evidence(report, question.Truth, question.Answer, question.Sources, sources, qpath, originalRequest);
                }
            }
            Choices(report, story, ids);
            return report;
        }

        /// <summary>Perspective choices only move forward, every chapter is reachable and every route is a complete telling.</summary>
        static void Choices(PersonalStoryValidation report, PersonalStoryDocument story, HashSet<string> ids)
        {
            var count = Math.Min(story.Chapters.Count, 6);
            for (var i = 0; i < count; i++)
            {
                var chapter = story.Chapters[i];
                if (chapter == null) continue;
                var path = "$.chapters[" + i + "].choices";
                if (chapter.Choices == null || chapter.Choices.Count == 1 || chapter.Choices.Count > 3)
                { report.Add("choices.count", path, "A chapter offers no choices, or two to three perspectives to follow."); continue; }
                var targets = new HashSet<string>(StringComparer.Ordinal);
                for (var c = 0; c < chapter.Choices.Count; c++)
                {
                    var choice = chapter.Choices[c];
                    var cpath = path + "[" + c + "]";
                    if (choice == null) { report.Add("choice.missing", cpath, "A choice must be an object."); continue; }
                    Id(report, ids, choice.Id, cpath + ".id");
                    Text(report, choice.Label, cpath + ".label", 80);
                    var target = PersonalStoryRoutes.IndexOf(story, choice.Next);
                    if (target <= i || target >= count) report.Add("choice.target", cpath + ".next", "A choice must lead forward to a chapter in this telling.");
                    else if (!targets.Add(choice.Next)) report.Add("choice.duplicate", cpath + ".next", "Each choice must lead to a different chapter.");
                }
            }
            if (report.Issues.Any(i => i.Code.StartsWith("choice", StringComparison.Ordinal)) || count < 3) return;

            var reached = new bool[count];
            var shortest = int.MaxValue;
            void Walk(int index, int pages)
            {
                reached[index] = true;
                var chapter = story.Chapters[index];
                if (chapter == null) return;
                if (PersonalStoryRoutes.HasChoices(chapter))
                    foreach (var choice in chapter.Choices) Walk(PersonalStoryRoutes.IndexOf(story, choice.Next), pages + 1);
                else
                {
                    var next = PersonalStoryRoutes.Continue(story, index);
                    if (next < 0) shortest = Math.Min(shortest, pages); else Walk(next, pages + 1);
                }
            }
            Walk(0, 1);
            for (var i = 0; i < count; i++)
                if (!reached[i]) report.Add("chapters.unreachable", "$.chapters[" + i + "]", "Every chapter must be reachable from the opening.");
            if (shortest < 3) report.Add("chapters.route", "$.chapters", "Every route must be a complete telling of at least three pages.");
        }

        static void Evidence(PersonalStoryValidation report, string truth, string text, IReadOnlyList<string> cited,
            HashSet<string> sources, string path, PersonalStoryRequest request)
        {
            if (cited == null || cited.Count > 3) report.Add("evidence.sources", path + ".sources", "A bounded source list is required.");
            else
            {
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var id in cited)
                    if (id == null || !sources.Contains(id) || !seen.Add(id)) report.Add("source.reference", path + ".sources", "Every source reference must be declared once.");
            }
            switch (truth)
            {
                case "historical":
                    if (!PersonalStoryCatalog.IsApprovedHistory(text, cited)) report.Add("history.unapproved", path, "A historical label requires exact reviewed text and its claim-specific source; a citation alone is not verification.");
                    break;
                case "reconstructed":
                    if (text == null || !text.StartsWith(PersonalStoryCatalog.ReconstructionPrefix, StringComparison.Ordinal))
                        report.Add("reconstruction.disclosure", path, "Reconstruction must begin with the explicit imagined-scene disclosure.");
                    if (text != null && IdentityAssertions.Any(s => text.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0))
                        report.Add("identity.inferred", path, "An imagined scene cannot assert the visitor's biography or ancestry.");
                    break;
                case "personal-account":
                    if (request == null || !ValidateRequest(request).IsValid || string.IsNullOrWhiteSpace(request.Fragment) ||
                        text != PersonalStoryCatalog.AccountPrefix + request.Fragment.Trim())
                        report.Add("account.unverified", path, "Only the exact original visitor fragment with an unverified-account disclosure can be accepted as a personal account.");
                    if (cited == null || cited.Count != 0) report.Add("account.sources", path + ".sources", "Personal accounts cannot acquire historical verification through source citations.");
                    break;
                default: report.Add("truth.invalid", path + ".truth", "Unknown evidence state."); break;
            }
        }

        static void Id(PersonalStoryValidation report, HashSet<string> ids, string value, string path)
        {
            if (value == null || !IdPattern.IsMatch(value)) report.Add("id.invalid", path, "Use a bounded stable identifier.");
            else if (!ids.Add(value)) report.Add("id.duplicate", path, "Every story, chapter and question ID must be unique.");
        }
        static void Text(PersonalStoryValidation report, string value, string path, int maximum, bool required = true)
        {
            if (string.IsNullOrWhiteSpace(value)) { if (required) report.Add("text.required", path, "Non-empty text is required."); return; }
            if (value.Length > maximum) report.Add("text.length", path, "Text exceeds the allowed length.");
            if (!IsPlainText(value)) report.Add("text.unsafe", path, "Use plain text without markup, control characters or embedded URLs.");
        }
        public static bool IsPlainText(string value)
        {
            if (value == null) return true;
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (c == '<' || c == '>' || c == '`' || c == '[' || c == ']' ||
                    (char.IsControl(c) && c != '\n' && c != '\r' && c != '\t') ||
                    (c >= '\u202a' && c <= '\u202e') || (c >= '\u2066' && c <= '\u2069')) return false;
                if (char.IsHighSurrogate(c)) { if (i + 1 >= value.Length || !char.IsLowSurrogate(value[++i])) return false; }
                else if (char.IsLowSurrogate(c)) return false;
            }
            return value.IndexOf("http:", StringComparison.OrdinalIgnoreCase) < 0 && value.IndexOf("https:", StringComparison.OrdinalIgnoreCase) < 0 &&
                value.IndexOf("javascript:", StringComparison.OrdinalIgnoreCase) < 0 && value.IndexOf("data:", StringComparison.OrdinalIgnoreCase) < 0;
        }
    }
}
