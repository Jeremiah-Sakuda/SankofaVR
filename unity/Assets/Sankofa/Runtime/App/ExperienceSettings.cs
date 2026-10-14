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

        public CaptionSize CaptionSize = CaptionSize.Large;
        public bool ReducedMotion;
        /// <summary>Development comparison only: the steady baseline fire against the directed one.</summary>
        public FireMode FireMode = FireMode.Directed;
        /// <summary>Night ambience: 0 full, 1 low, 2 off. Some visitors find background sound distracting.</summary>
        public int Ambience;
        /// <summary>Recorded narration: 0 full, 1 low, 2 captions only. Silence never skips story time.</summary>
        public int Narration;
        /// <summary>Bring the world closer without moving the visitor: standard, near, closer.</summary>
        public int Reach;
        /// <summary>Where the world sits for this visitor's height and reach: -1 lower, 0 as built, 1 higher.</summary>
        public int Height;

        /// <summary>Metres the whole world moves, so every touch point, gift and panel moves together.</summary>
        public float HeightOffset => Height * 0.08f;
        public string HeightName => Height < 0 ? "lower" : Height > 0 ? "higher" : "middle";

        public float AmbienceLevel => Ambience == 2 ? 0f : Ambience == 1 ? 0.5f : 1f;
        public string AmbienceName => Ambience == 2 ? "off" : Ambience == 1 ? "low" : "full";
        public float NarrationLevel => Narration == 2 ? 0 : Narration == 1 ? 0.5f : 1;
        public string NarrationName => Narration == 2 ? "captions only" : Narration == 1 ? "low" : "full";
        public float ReachOffset => -Reach * 0.06f;
        public string ReachName => Reach == 2 ? "closer" : Reach == 1 ? "near" : "standard";

        public static ExperienceSettings Load() => new ExperienceSettings
        {
            CaptionSize = (CaptionSize)Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "captions", (int)CaptionSize.Large), 0, 2),
            ReducedMotion = PlayerPrefs.GetInt(Prefix + "reducedMotion", 0) == 1,
            FireMode = PlayerPrefs.GetInt(Prefix + "fireMode", 0) == 1 ? FireMode.Simple : FireMode.Directed,
            Ambience = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "ambience", 0), 0, 2),
            Narration = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "narration", 0), 0, 2),
            Reach = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "reach", 0), 0, 2),
            Height = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "height", 0), -1, 1),
        };

        public void Save()
        {
            PlayerPrefs.SetInt(Prefix + "captions", (int)CaptionSize);
            PlayerPrefs.SetInt(Prefix + "reducedMotion", ReducedMotion ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "fireMode", FireMode == FireMode.Simple ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "ambience", Ambience);
            PlayerPrefs.SetInt(Prefix + "narration", Narration);
            PlayerPrefs.SetInt(Prefix + "reach", Reach);
            PlayerPrefs.SetInt(Prefix + "height", Height);
            PlayerPrefs.Save();
        }
    }
}
