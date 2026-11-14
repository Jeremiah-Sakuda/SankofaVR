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

            var interest = SelectInterest(request, out var source, out var cues);
            var story = interest == "trade-household" ? Trade() : interest == "place-water" ? Water() : Journeys();
            var referenced = story.Chapters.SelectMany(c => c.Sources.Concat(c.Questions.SelectMany(q => q.Sources))).Distinct().ToArray();
            story.Sources = referenced.Select(PersonalStoryCatalog.Source).ToArray();
            return new PersonalStoryComposition
            {
                Story = story,
                Code = "bundled",
                Message = "Bundled story selected from your question and interest. No fresh AI generation was used; personal details are not treated as verified history.",
                Reason = Explain(source, cues, interest, request),
                Cues = cues,
            };
        }

        /// <summary>Question first, then thematic fragment, then explicit interest. No identity or genealogy inference.</summary>
        public static string SelectInterest(PersonalStoryRequest request) => SelectInterest(request, out _, out _);

        static string SelectInterest(PersonalStoryRequest request, out string source, out IReadOnlyList<string> cues)
        {
            source = "interest"; cues = new string[0];
            if (request == null) return "journeys";
            var theme = Theme(request.Question, out cues);
            if (theme != null) { source = "question"; return theme; }
            theme = Theme(request.Fragment, out cues);
            if (theme != null) { source = "fragment"; return theme; }
            cues = new string[0];
            return PersonalStoryCatalog.NormalizeInterest(request.Interest) ?? "journeys";
        }

        static readonly string[] WaterWords = { "water", "waters", "river", "rivers", "stream", "streams", "spring", "springs", "name", "names", "named", "naming", "place", "places", "landscape", "bank", "banks", "reeds", "nairobi" };
        static readonly string[] TradeWords = { "trade", "traded", "trading", "trader", "traders", "household", "households", "home", "flour", "milk", "gourd", "gourds", "vessel", "vessels", "food", "meal", "meals", "cooking", "exchange", "exchanged", "barter", "market", "markets", "goods", "carried", "carry", "carrying" };
        static readonly string[] JourneyWords = { "journey", "journeys", "journeyed", "migration", "migrated", "moved", "move", "moving", "travel", "travelled", "traveled", "travelling", "traveling", "walk", "walked", "walking", "path", "paths", "road", "roads", "meeting", "meet", "met", "encounter", "wait", "waited", "waiting", "late", "arrive", "arrived" };

        static string Theme(string text, out IReadOnlyList<string> cues)
        {
            cues = new string[0];
            if (string.IsNullOrWhiteSpace(text)) return null;
            var words = Regex.Matches(text.ToLowerInvariant(), "[a-z]+(?:-[a-z]+)?").Cast<Match>().Select(m => m.Value).Distinct().ToArray();
            var water = words.Where(WaterWords.Contains).ToArray();
            var trade = words.Where(TradeWords.Contains).ToArray();
            var journeys = words.Where(JourneyWords.Contains).ToArray();
            var maximum = Math.Max(water.Length, Math.Max(trade.Length, journeys.Length));
            // Ambiguous fragments do not overrule a deliberate interest choice.
            if (maximum == 0 || new[] { water.Length, trade.Length, journeys.Length }.Count(v => v == maximum) > 1) return null;
            cues = maximum == water.Length ? water : maximum == trade.Length ? trade : journeys;
            return maximum == water.Length ? "place-water" : maximum == trade.Length ? "trade-household" : "journeys";
        }

        /// <summary>Uses only catalog vocabulary, so no visitor sentence, name or fragment is ever repeated back.</summary>
        static string Explain(string source, IReadOnlyList<string> cues, string interest, PersonalStoryRequest request)
        {
            var thread = interest == "trade-household" ? "trade and household" : interest == "place-water" ? "place and water" : "journeys";
            if (source != "interest")
            {
                var words = cues.Take(3).Select(c => "\u201c" + c + "\u201d").ToArray();
                var quoted = words.Length == 1 ? words[0] : string.Join(", ", words.Take(words.Length - 1)) + " and " + words[words.Length - 1];
                return "Your " + source + " mentioned " + quoted + ", so the fire follows " + thread + ".";
            }
            var wrote = !string.IsNullOrWhiteSpace(request?.Question) || !string.IsNullOrWhiteSpace(request?.Fragment);
            return wrote
                ? "Your words did not point to one prepared thread, so the fire follows the one you chose: " + thread + "."
                : "You chose " + thread + ".";
        }
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
        static PersonalStoryChapter Offer(PersonalStoryChapter chapter, params (string Id, string Label, string Next)[] choices)
        {
            chapter.Choices = choices.Select(c => new PersonalStoryChoice { Id = c.Id, Label = c.Label, Next = c.Next }).ToArray();
            return chapter;
        }
        static PersonalStoryQuestion FactQuestion(string id, string label, string answer, string source) => new PersonalStoryQuestion
        { Id = id, Label = label, Answer = answer, Truth = "historical", Sources = new[] { source } };
        static PersonalStoryQuestion FictionQuestion(string id, string label, string answer) => new PersonalStoryQuestion
        { Id = id, Label = label, Answer = PersonalStoryCatalog.ReconstructionPrefix + answer, Truth = "reconstructed" };

        static PersonalStoryDocument Journeys() => Story("journeys", "Where paths meet",
            Scene("journeys.arrival", "Two paths toward a meeting",
                "Come, sit by the fire. A woman has reached the meeting place before her travelling companion. Across the water, the path is still empty. She lifts her bundle: if she leaves now, she can reach home before dark. But if her companion arrives after she has gone, there will be nobody to meet her. The bundle settles against her shoulder. She takes a step, then looks once more toward the bend.", "scenery-atlas",
                FictionQuestion("journeys.leaving", "Why is she thinking of leaving?", "Home is still a walk away, and the light is going. If she leaves, she can reach it before dark. If she stays, someone may find her waiting."),
                FictionQuestion("journeys.account", "Does this tell my ancestor's journey?", "No ancestor's journey is established here. A fragment can guide curiosity about movement and encounters; it cannot establish anyone's biography.")),
            Offer(History("journeys.context", "A region of overlapping lives", PersonalStoryCatalog.TradeFact, "C6", "scenery-atlas",
                FactQuestion("journeys.context.more", "What supports the regional context?", PersonalStoryCatalog.TradeFact, "C6")),
                ("journeys.follow.waiting", "Stay with the one who waits", "journeys.meeting"),
                ("journeys.follow.late", "Find the one who is late", "journeys.late")),
            Scene("journeys.meeting", "What a traveller carries onward",
                "She chooses to put the bundle down. For a while there is only the water and the fire. Then the missing woman comes round the bend, holding her gourd close. 'I thought you would have gone.' The waiting woman moves her bundle aside. 'I nearly did.' They sit in the space it leaves. When they rise, the path home is darker, but neither follows it alone.", "cast-atlas",
                FictionQuestion("journeys.next", "What could I ask a relative next?", "You could ask which places or journeys they remember, and whose account each detail comes from. Their answers would remain personal accounts unless separately supported.")),
            Scene("journeys.late", "A promise still walking",
                "On the far side of the water, the late woman is still walking. The path has taken longer than she planned, and the gourd she carries grows heavier with every step. The light is going. If she turns back now, she can send word tomorrow, and nobody will blame her. But a message cannot sit beside someone at dusk. She chooses to keep walking, holding the gourd close, not knowing whether anyone is still there. At the bend she stops. A bundle has been set down by the water, and beside it, a woman is waiting.", "scenery-atlas",
                FictionQuestion("journeys.late.ask", "What could I ask about a journey someone made?", "You could ask what made the journey long, who waited at the other end, and whether word was ever sent ahead. Their answers would remain personal accounts unless separately supported.")));

        static PersonalStoryDocument Trade() => Story("trade-household", "A vessel and an agreement",
            Scene("trade-household.load", "A household's small decisions",
                "Come closer to the fire. A woman tips her flour gourd and listens. Less than she thought. At home, a small portion still waits beside the hearth. She could add it to the gourd and arrive with the full load she promised. Instead she leaves that portion where it is. Now she must carry a different weight to the meeting: the words that will explain the shortfall.", "cast-atlas",
                FictionQuestion("trade-household.rest", "Why didn't she bring the rest?", "She left a portion at home. Bringing it would fill the promised gourd, but empty that place beside her own hearth. She is carrying the cost of that choice to the meeting.")),
            // The documented page is the trade this meeting belongs to. The museum vessel is a milk or blood gourd, not
            // this flour gourd, so it is offered as context for a visitor who asks and never told as part of the scene.
            Offer(History("trade-household.vessel", "An exchange in context", PersonalStoryCatalog.TradeFact, "C6", "cast-atlas",
                FactQuestion("trade-household.vessel.more", "What do museums record about gourds?", PersonalStoryCatalog.VesselFact, "C7")),
                ("trade-household.follow.less", "Follow the one who brought less", "trade-household.agreement"),
                ("trade-household.follow.more", "Wait with the one who expected more", "trade-household.expected")),
            Scene("trade-household.agreement", "The agreement belongs to the women",
                "At the water, she opens the gourd before the other woman can offer hers. 'This is all I could bring.' She keeps her hands beside it, waiting to be turned away. The other woman draws her own bundle back. Then she separates a smaller share and sets it between them. 'Then let us begin with what is here.' They choose the smaller exchange together. Back at the hearth, the woman sets her gourd beside the portion she left. Neither is full. Together, they are enough for tonight.", "cast-atlas",
                FictionQuestion("trade-household.next", "Is this exchange a historical custom?", "This particular exchange and its terms are invented. A documented regional trade relationship does not prove this bargain or a universal custom.")),
            Scene("trade-household.expected", "The one who expected more",
                "The other woman reached the water first, her own share wrapped and ready. She has counted on the flour; at home, tonight's meal was planned around it. When the gourd arrives, she can see how lightly it is carried before a word is spoken. She could draw her share back and walk home, having lost nothing but an afternoon. She could ask for what was promised. Instead she watches the woman's hands resting beside the gourd, as if waiting to be turned away. She chooses to make the exchange smaller rather than let it end. 'Then let us begin with what is here.'", "cast-atlas",
                FictionQuestion("trade-household.expected.cost", "What did accepting less cost her?", "In this telling, a smaller share means a smaller meal at home tonight. The scene invents that cost to show her choice; it is not a record of any household's portions or of a custom.")));

        static PersonalStoryDocument Water() => Story("place-water", "A name beside the water",
            Scene("place-water.listening", "Begin with the water",
                "Come, sit where the fire catches the water. A woman brings a younger traveller to the bank she remembers. 'Here,' she says. But the traveller points to the next bend. 'You said the path opened beside the reeds.' The woman looks at the water, then at the bundle she has set down. She had promised to show a place. Now she is no longer sure she has found it.", "scenery-atlas",
                FictionQuestion("place-water.matters", "Why does finding this place matter?", "She promised to show the traveller a place she remembers. The uncertainty is not only about the bend. It is whether she can share what that place meant to her."),
                FictionQuestion("place-water.map", "Is this the exact historical riverbank?", "The painted bank is a reconstruction. It does not locate a particular household, meeting or ancestor.")),
            Offer(History("place-water.name", "A name with limits", PersonalStoryCatalog.NamingClaim, "C1", "scenery-atlas",
                FactQuestion("place-water.name.more", "What does Enkare Nairobi mean?", PersonalStoryCatalog.NamingFact, "C1")),
                ("place-water.follow.remembers", "Stay with the one who remembers", "place-water.return"),
                ("place-water.follow.traveller", "Walk with the younger traveller", "place-water.traveller")),
            Scene("place-water.return", "A place can hold another question",
                "She chooses not to pretend. 'I remember the water. I may have the bend wrong.' She lifts the bundle and makes room for the traveller beside her. Instead of pointing, she describes the sound she remembers. The traveller closes her eyes to listen. The telling passes from one voice to another while the water keeps moving. Much later, by a river with a city beyond it, another visitor pauses to listen before choosing where to stand.", "modern-river",
                FictionQuestion("place-water.next", "What could I ask about a remembered place?", "Ask what the place was called, who used that name, and whether there is a photograph, map or personal account to compare. A remembered name alone cannot verify ancestry.")),
            Scene("place-water.traveller", "The one who was shown",
                "The younger traveller came to be shown a place, and she was sure of the reeds. Now she watches the older woman look from the water to the bundle and back, and understands that the bend may have moved, or the memory has. She could press: 'You said the reeds.' She could walk ahead to prove it. Instead she chooses a different question. 'What did it sound like?' The older woman closes her eyes to answer. Years from now, the traveller will remember that sound better than any bend, and she will be the one who is not quite sure where it was.", "scenery-atlas",
                FictionQuestion("place-water.traveller.ask", "What could I ask someone who remembers a place?", "Ask what it sounded, smelled or looked like, and who else went there. A vivid memory can hold true detail even when its exact location is uncertain; it remains a personal account unless other evidence supports it.")));
    }
}
