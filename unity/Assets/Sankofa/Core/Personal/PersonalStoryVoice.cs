using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Sankofa.Core.Personal
{
    /// <summary>
    /// What the griot says aloud and the page shows, as distinct from a story's stored text. A stored imagined chapter
    /// opens with the disclosure "Imagined scene: ", and validation still requires it. The page marks every imagined
    /// page with its own label and the griot says the disclosure once in the welcome, so neither the caption nor the
    /// recording repeats it on every page.
    /// </summary>
    public static class PersonalStoryVoice
    {
        /// <summary>What the page says while the visitor chooses; the griot says it last, and it stays.</summary>
        public const string Invitation = "Every family carries something. Choose what you would carry back to the fire, and its story will rise.";

        static readonly string[] welcome =
        {
            "Come. Sit with me.",
            "I keep stories from many fires. Tonight's comes from Enkare Nairobi, before the railway.",
            "The people in it are imagined. What is documented, I will mark.",
            Invitation,
        };
        static readonly string[] returning = { "You came back. Sit with me.", Invitation };

        /// <summary>What a first-time visitor hears before choosing. The disclosure is said once, here.</summary>
        public static IReadOnlyList<string> Welcome => Array.AsReadOnly(welcome);
        /// <summary>What a visitor with a kept telling hears instead.</summary>
        public static IReadOnlyList<string> ReturnWelcome => Array.AsReadOnly(returning);

        /// <summary>The heirloom that begins each telling, as the griot names it.</summary>
        public static string Heirloom(string telling) => telling == "journeys" ? "bundle" : telling == "place-water" ? "water pot" : "gourd";

        /// <summary>What the griot tells a returning visitor about a side they have not heard, and how to reach it.</summary>
        public static string ReturnInvitation(string telling, string sideLabel)
        {
            // A short action label can change without changing the griot's already recorded invitation.
            // Keep these exact spoken words so the manifest and its text-hashed recording remain valid.
            if (telling == "place-water")
            {
                if (sideLabel == "Hear the one who remembers") sideLabel = "Stay with the one who remembers";
                else if (sideLabel == "Hear the younger traveller") sideLabel = "Walk with the younger traveller";
            }
            var side = string.IsNullOrEmpty(sideLabel) ? "" : char.ToLowerInvariant(sideLabel[0]) + sideLabel.Substring(1);
            return "Choose the " + Heirloom(telling) + ", and you could " + side + ".";
        }

        /// <summary>
        /// The welcome for a visitor whose strand has beads: the greeting, the side the strand suggests next, then the
        /// invitation. A complete or empty strand has no side to name.
        /// </summary>
        public static IReadOnlyList<string> ReturnWelcomeFor(KeptStrand strand)
        {
            var next = strand != null && strand.Count > 0 ? strand.NextUnheard() : null;
            var side = next == null ? null : KeptStrand.Sides(next.Telling).FirstOrDefault(s => s.Id == next.Side);
            return side == null ? ReturnWelcome : Array.AsReadOnly(new[] { returning[0], ReturnInvitation(next.Telling, side.Label), Invitation });
        }

        /// <summary>Every line the griot says outside a telling, with a stable id for its recording.</summary>
        public static IReadOnlyList<KeyValuePair<string, string>> Lines => new[]
        {
            new KeyValuePair<string, string>("welcome.come", welcome[0]),
            new KeyValuePair<string, string>("welcome.fires", welcome[1]),
            new KeyValuePair<string, string>("welcome.imagined", welcome[2]),
            new KeyValuePair<string, string>("welcome.invitation", Invitation),
            new KeyValuePair<string, string>("welcome.return", returning[0]),
        }.Concat(KeptStrand.Tellings.SelectMany(telling => KeptStrand.Sides(telling)
            .Select(side => new KeyValuePair<string, string>("return." + side.Id, ReturnInvitation(telling, side.Label))))).ToArray();

        /// <summary>A chapter's or answer's words as spoken and captioned: its stored text without the leading disclosure.</summary>
        public static string Spoken(string stored)
        {
            if (string.IsNullOrEmpty(stored)) return "";
            return stored.StartsWith(PersonalStoryCatalog.ReconstructionPrefix, StringComparison.Ordinal)
                ? stored.Substring(PersonalStoryCatalog.ReconstructionPrefix.Length) : stored;
        }

        /// <summary>The recording's resource name for exactly these spoken words, so changed words never play an old clip.</summary>
        public static string ClipName(string spoken)
        {
            using (var hash = SHA256.Create())
                return "p-" + BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(spoken ?? ""))).Replace("-", "").ToLowerInvariant().Substring(0, 16);
        }
    }
}
