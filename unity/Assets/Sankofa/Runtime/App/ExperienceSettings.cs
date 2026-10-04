using Sankofa.Runtime.Fire;
using Sankofa.Runtime.UI;
using UnityEngine;

namespace Sankofa.Runtime.App
{
    /// <summary>
    /// The visitor's comfort choices, remembered on this headset only. Not story history: saving a visit is a
    /// separate, explicit choice.
    /// </summary>
    public sealed class ExperienceSettings
    {
        const string Prefix = "sankofa.settings.";

        public CaptionSize CaptionSize = CaptionSize.Medium;
        public bool ReducedMotion;
        /// <summary>Development comparison only: the steady baseline fire against the directed one.</summary>
        public FireMode FireMode = FireMode.Directed;
        /// <summary>Night ambience: 0 full, 1 low, 2 off. Some visitors find background sound distracting.</summary>
        public int Ambience;

        public float AmbienceLevel => Ambience == 2 ? 0f : Ambience == 1 ? 0.5f : 1f;
        public string AmbienceName => Ambience == 2 ? "off" : Ambience == 1 ? "low" : "full";

        public static ExperienceSettings Load() => new ExperienceSettings
        {
            CaptionSize = (CaptionSize)Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "captions", (int)CaptionSize.Medium), 0, 2),
            ReducedMotion = PlayerPrefs.GetInt(Prefix + "reducedMotion", 0) == 1,
            FireMode = PlayerPrefs.GetInt(Prefix + "fireMode", 0) == 1 ? FireMode.Simple : FireMode.Directed,
            Ambience = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "ambience", 0), 0, 2),
        };

        public void Save()
        {
            PlayerPrefs.SetInt(Prefix + "captions", (int)CaptionSize);
            PlayerPrefs.SetInt(Prefix + "reducedMotion", ReducedMotion ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "fireMode", FireMode == FireMode.Simple ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "ambience", Ambience);
            PlayerPrefs.Save();
        }
    }
}
