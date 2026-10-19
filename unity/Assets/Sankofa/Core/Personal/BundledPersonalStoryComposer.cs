using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sankofa.Core.Personal
{
    /// <summary>Deterministic offline composition of three authored tellings; never claims fresh generation.</summary>
    public static class BundledPersonalStoryComposer
    {
        public static PersonalStoryComposition Compose(PersonalStoryRequest request)
        {
            var input = PersonalStoryValidator.ValidateRequest(request);
            if (!input.IsValid)
            {
                var issue = input.Issues[0];
                return issue.Code.StartsWith("coverage.", StringComparison.Ordinal) ? Failure(issue.Code, issue.Message) :
                    Failure("input.invalid", "Please use plain text within the input limits.");
            }

            var interest = SelectInterest(request);
            var story = interest == "trade-household" ? Trade() : interest == "place-water" ? Water() : Journeys();
            var referenced = story.Chapters.SelectMany(c => c.Sources.Concat(c.Questions.SelectMany(q => q.Sources))).Distinct().ToArray();
            story.Sources = referenced.Select(PersonalStoryCatalog.Source).ToArray();
            return new PersonalStoryComposition
            {
                Story = story,
                Code = "bundled",
                Message = "Bundled story selected from your question and interest. No fresh AI generation was used; personal details are not treated as verified history.",
            };
        }

        /// <summary>Question first, then thematic fragment, then explicit interest. No identity or genealogy inference.</summary>
        public static string SelectInterest(PersonalStoryRequest request)
        {
            if (request == null) return "journeys";
            return Theme(request.Question) ?? Theme(request.Fragment) ?? PersonalStoryCatalog.NormalizeInterest(request.Interest) ?? "journeys";
        }

        static string Theme(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var words = new HashSet<string>(Regex.Matches(text.ToLowerInvariant(), "[a-z]+(?:-[a-z]+)?").Cast<Match>().Select(m => m.Value));
            var water = Score(words, "water", "river", "stream", "name", "naming", "place", "landscape", "bank", "banks");
            var trade = Score(words, "trade", "trading", "household", "flour", "milk", "gourd", "vessel", "food", "cooking", "exchange", "carried", "carry");
            var journeys = Score(words, "journey", "journeys", "migration", "moved", "move", "moving", "travel", "travelled", "path", "paths", "meeting", "encounter");
            var maximum = Math.Max(water, Math.Max(trade, journeys));
            // Ambiguous fragments do not overrule a deliberate interest choice.
            if (maximum == 0 || new[] { water, trade, journeys }.Count(v => v == maximum) > 1) return null;
            return maximum == water ? "place-water" : maximum == trade ? "trade-household" : "journeys";
        }
        static int Score(HashSet<string> words, params string[] choices) => choices.Count(words.Contains);
        static PersonalStoryComposition Failure(string code, string message) => new PersonalStoryComposition { Code = code, Message = message };
        static PersonalStoryDocument Story(string interest, string title, params PersonalStoryChapter[] chapters) => new PersonalStoryDocument
        {
            Id = "bundled.enkare-nairobi.1850s." + interest + ".v1", Title = title,
            Region = PersonalStoryCatalog.Region, Period = PersonalStoryCatalog.Period, Interest = interest, Mode = "bundled", Chapters = chapters,
        };
        static PersonalStoryChapter Scene(string id, string title, string text, string art, params PersonalStoryQuestion[] questions) => new PersonalStoryChapter
        {
            Id = id, Title = title, Narration = PersonalStoryCatalog.ReconstructionPrefix + text,
            Illustration = art, Truth = "reconstructed", Questions = questions,
        };
        static PersonalStoryChapter History(string id, string title, string text, string source, string art, params PersonalStoryQuestion[] questions) => new PersonalStoryChapter
        {
            Id = id, Title = title, Narration = text, Illustration = art, Truth = "historical", Sources = new[] { source }, Questions = questions,
        };
        static PersonalStoryQuestion FactQuestion(string id, string label, string answer, string source) => new PersonalStoryQuestion
        { Id = id, Label = label, Answer = answer, Truth = "historical", Sources = new[] { source } };
        static PersonalStoryQuestion FictionQuestion(string id, string label, string answer) => new PersonalStoryQuestion
        { Id = id, Label = label, Answer = PersonalStoryCatalog.ReconstructionPrefix + answer, Truth = "reconstructed" };

        static PersonalStoryDocument Journeys() => Story("journeys", "Where paths meet",
            Scene("journeys.arrival", "Two paths toward a meeting",
                "Come, sit by the fire. A woman has reached the meeting place before her travelling companion. Across the water, the path is still empty. She lifts her bundle: if she leaves now, she can reach home before dark. But if her companion arrives after she has gone, there will be nobody to meet her. The bundle settles against her shoulder. She takes a step, then looks once more toward the bend.", "scenery-atlas",
                FictionQuestion("journeys.account", "Does this tell my ancestor's journey?", "No ancestor's journey is established here. A fragment can guide curiosity about movement and encounters; it cannot establish anyone's biography.")),
            History("journeys.context", "A region of overlapping lives", PersonalStoryCatalog.TradeFact, "C6", "scenery-atlas",
                FactQuestion("journeys.context.more", "What supports the regional context?", PersonalStoryCatalog.TradeFact, "C6")),
            Scene("journeys.meeting", "What a traveller carries onward",
                "She chooses to put the bundle down. For a while there is only the water and the fire. Then the missing woman comes round the bend, holding her gourd close. 'I thought you would have gone.' The waiting woman moves her bundle aside. 'I nearly did.' They sit in the space it leaves. When they rise, the path home is darker, but neither follows it alone.", "cast-atlas",
                FictionQuestion("journeys.next", "What could I ask a relative next?", "You could ask which places or journeys they remember, and whose account each detail comes from. Their answers would remain personal accounts unless separately supported.")));

        static PersonalStoryDocument Trade() => Story("trade-household", "A vessel and an agreement",
            Scene("trade-household.load", "A household's small decisions",
                "Come closer to the fire. A woman tips her flour gourd and listens. Less than she thought. At home, a small portion still waits beside the hearth. She could add it to the gourd and arrive with the full load she promised. Instead she leaves that portion where it is. Now she must carry a different weight to the meeting: the words that will explain the shortfall.", "cast-atlas",
                FactQuestion("trade-household.context", "Did communities trade in this region?", PersonalStoryCatalog.TradeFact, "C6")),
            History("trade-household.vessel", "A vessel with a source", PersonalStoryCatalog.VesselFact, "C7", "cast-atlas",
                FactQuestion("trade-household.vessel.more", "What does the museum example tell us?", PersonalStoryCatalog.VesselFact, "C7")),
            Scene("trade-household.agreement", "The agreement belongs to the women",
                "At the water, she opens the gourd before the other woman can offer hers. 'This is all I could bring.' She keeps her hands beside it, waiting to be turned away. The other woman draws her own bundle back. Then she separates a smaller share and sets it between them. 'Then let us begin with what is here.' They choose the smaller exchange together. Back at the hearth, the woman sets her gourd beside the portion she left. Neither is full. Together, they are enough for tonight.", "cast-atlas",
                FictionQuestion("trade-household.next", "Is this exchange a historical custom?", "This particular exchange and its terms are invented. A documented regional trade relationship does not prove this bargain or a universal custom.")));

        static PersonalStoryDocument Water() => Story("place-water", "A name beside the water",
            Scene("place-water.listening", "Begin with the water",
                "Come, sit where the fire catches the water. A woman brings a younger traveller to the bank she remembers. 'Here,' she says. But the traveller points to the next bend. 'You said the path opened beside the reeds.' The woman looks at the water, then at the bundle she has set down. She had promised to show a place. Now she is no longer sure she has found it.", "scenery-atlas",
                FictionQuestion("place-water.map", "Is this the exact historical riverbank?", "The painted bank is a reconstruction. It does not locate a particular household, meeting or ancestor.")),
            History("place-water.name", "A name with limits", PersonalStoryCatalog.NamingFact, "C1", "scenery-atlas",
                FactQuestion("place-water.name.more", "What does Enkare Nairobi mean?", PersonalStoryCatalog.NamingFact, "C1")),
            Scene("place-water.return", "A place can hold another question",
                "She chooses not to pretend. 'I remember the water. I may have the bend wrong.' She lifts the bundle and makes room for the traveller beside her. Instead of pointing, she describes the sound she remembers. The traveller closes her eyes to listen. The telling passes from one voice to another while the water keeps moving. Much later, by a river with a city beyond it, another visitor pauses to listen before choosing where to stand.", "modern-river",
                FictionQuestion("place-water.next", "What could I ask about a remembered place?", "Ask what the place was called, who used that name, and whether there is a photograph, map or personal account to compare. A remembered name alone cannot verify ancestry.")));
    }
}
