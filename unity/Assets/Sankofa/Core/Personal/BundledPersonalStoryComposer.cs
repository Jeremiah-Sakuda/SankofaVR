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
                "Come, sit with the fire. In this bundled telling, follow an imagined traveller toward the water. She slows where another path joins hers; a meeting, rather than a destination, becomes the question. These are fictional people, never a recovered account of your family.", "scenery-atlas",
                FictionQuestion("journeys.account", "Does this tell my ancestor's journey?", "No ancestor's journey is established here. A fragment can guide curiosity about movement and encounters; it cannot establish anyone's biography.")),
            History("journeys.context", "A region of overlapping lives", PersonalStoryCatalog.TradeFact, "C6", "scenery-atlas",
                FactQuestion("journeys.context.more", "What supports the regional context?", PersonalStoryCatalog.TradeFact, "C6")),
            Scene("journeys.meeting", "What a traveller carries onward",
                "The traveller rests her bundle beside the water. The other woman makes room by the fire. No map in this telling can identify a family route; what changes is the traveller's next question: who might she meet along a shared path? Let the painted paths settle, and keep this telling only if you choose.", "cast-atlas",
                FictionQuestion("journeys.next", "What could I ask a relative next?", "You could ask which places or journeys they remember, and whose account each detail comes from. Their answers would remain personal accounts unless separately supported.")));

        static PersonalStoryDocument Trade() => Story("trade-household", "A vessel and an agreement",
            Scene("trade-household.load", "A household's small decisions",
                "Come closer to the fire. This bundled telling begins with an imagined household deciding what can leave home in a flour gourd. One portion stays beside the hearth; another is carried to a meeting. The portions, people and agreement are fiction, not a customary price or your family's past.", "cast-atlas",
                FactQuestion("trade-household.context", "Did communities trade in this region?", PersonalStoryCatalog.TradeFact, "C6")),
            History("trade-household.vessel", "A vessel with a source", PersonalStoryCatalog.VesselFact, "C7", "cast-atlas",
                FactQuestion("trade-household.vessel.more", "What does the museum example tell us?", PersonalStoryCatalog.VesselFact, "C7")),
            Scene("trade-household.agreement", "The agreement belongs to the women",
                "At the painted meeting, the women consider a gourd and the needs of the homes they will return to. They choose an agreement together. The scene offers a question about care and exchange, without turning one fictional bargain into a rule for a whole community. The fire leaves the gourd in view as the telling closes; saving it is your choice.", "cast-atlas",
                FictionQuestion("trade-household.next", "Is this exchange a historical custom?", "This particular exchange and its terms are invented. A documented regional trade relationship does not prove this bargain or a universal custom.")));

        static PersonalStoryDocument Water() => Story("place-water", "A name beside the water",
            Scene("place-water.listening", "Begin with the water",
                "Come, sit with the fire. This bundled telling turns toward the painted bank before following any person. Reeds interrupt the reflection; an imagined visitor pauses to ask what a place name can carry. The bank is an illustration, not an exact historical map or evidence of a family home.", "scenery-atlas",
                FictionQuestion("place-water.map", "Is this the exact historical riverbank?", "The painted bank is a reconstruction. It does not locate a particular household, meeting or ancestor.")),
            History("place-water.name", "A name with limits", PersonalStoryCatalog.NamingFact, "C1", "scenery-atlas",
                FactQuestion("place-water.name.more", "What does Enkare Nairobi mean?", PersonalStoryCatalog.NamingFact, "C1")),
            Scene("place-water.return", "A place can hold another question",
                "The imagined visitor leaves the bank as the reflection opens into a painted modern river. This contemporary coda is imagined too; the picture makes no claim that the river is fully restored. A name can invite attention without proving where anyone's relatives lived. Keep the question beside this telling if you choose to save it.", "modern-river",
                FictionQuestion("place-water.next", "What could I ask about a remembered place?", "Ask what the place was called, who used that name, and whether there is a photograph, map or personal account to compare. A remembered name alone cannot verify ancestry.")));
    }
}
