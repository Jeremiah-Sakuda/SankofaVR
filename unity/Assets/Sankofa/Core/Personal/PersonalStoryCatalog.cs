using System;
using System.Collections.Generic;
using System.Linq;

namespace Sankofa.Core.Personal
{
    /// <summary>
    /// Bounded desk-researched context from the existing source register. Cultural and pronunciation acceptance
    /// remain open. A provider's source label cannot add a claim to this inventory.
    /// </summary>
    public static class PersonalStoryCatalog
    {
        public const string Region = "enkare-nairobi";
        public const string Period = "1850s";
        public const string NamingFact = "Nairobi's name is commonly traced to a Maa phrase meaning cool waters. Spelling and pronunciation still need review by a Maa speaker.";
        public const string TradeFact = "Before 1900, trade linked pastoral Maasai and predominantly agricultural Kikuyu communities around Kiambu; settlement and economic activity overlapped.";
        public const string VesselFact = "A National Museums of Kenya exhibit describes a Maasai gourd used for milk or blood and smoked inside with oloirien wood. This museum example does not establish the design of every vessel used in the 1850s.";
        public const string ReconstructionPrefix = "Imagined scene: ";
        public const string AccountPrefix = "Visitor-provided account, not independently verified: ";

        static readonly string[] interests = { "journeys", "trade-household", "place-water" };
        static readonly string[] illustrations = { "scenery-atlas", "cast-atlas", "modern-river" };
        static readonly PersonalStorySource[] sources =
        {
            new PersonalStorySource { Id = "C1", Title = "Nairobi City County Assembly: clean drinking water motion", Url = "https://nairobiassembly.go.ke/motion/provision-of-free-clean-drinking-water-in-hotels-and-other-public-areas/" },
            new PersonalStorySource { Id = "C6", Title = "John Overton (1988): The Origins of the Kikuyu Land Problem", Url = "https://www.cambridge.org/core/journals/african-studies-review/article/abs/origins-of-the-kikuyu-land-problem-land-alienation-and-land-use-in-kiambu-kenya-18951920/01BDC3BCB4D2A9AC40F00F52500F1916" },
            new PersonalStorySource { Id = "C7", Title = "National Museums of Kenya: 28 Incredible Containers Used by Kenyan Communities", Url = "https://artsandculture.google.com/story/_gUx7TIiep4KKA" },
        };
        public static IReadOnlyList<string> Interests => Array.AsReadOnly(interests);
        public static IReadOnlyList<string> Illustrations => Array.AsReadOnly(illustrations);
        public static IReadOnlyList<PersonalStorySource> Sources => sources.Select(Copy).ToArray();
        public static PersonalStorySource Source(string id) => sources.Where(s => s.Id == id).Select(Copy).FirstOrDefault();
        static PersonalStorySource Copy(PersonalStorySource s) => new PersonalStorySource { Id = s.Id, Title = s.Title, Url = s.Url };

        public static string NormalizeRegion(string value)
        {
            switch ((value ?? "").Trim().ToLowerInvariant())
            {
                case "enkare-nairobi": case "enkare nairobi": case "nairobi": case "nairobi, kenya": return Region;
                default: return null;
            }
        }
        public static string NormalizePeriod(string value)
        {
            switch ((value ?? "").Trim().ToLowerInvariant())
            {
                case "": case "1850s": case "1850": case "before the railway": case "mid-1800s": return Period;
                default: return null;
            }
        }
        public static string NormalizeInterest(string value)
        {
            switch ((value ?? "").Trim().ToLowerInvariant())
            {
                case "journeys": case "journey": case "migration": case "movement": return "journeys";
                case "trade-household": case "trade": case "household": case "daily life": return "trade-household";
                case "place-water": case "place": case "water": case "river": return "place-water";
                default: return null;
            }
        }
        public static bool IsApprovedHistory(string text, IReadOnlyList<string> cited)
        {
            var expected = text == NamingFact ? "C1" : text == TradeFact ? "C6" : text == VesselFact ? "C7" : null;
            return expected != null && cited != null && cited.Count == 1 && cited[0] == expected;
        }
    }
}
