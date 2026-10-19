using Sankofa.Runtime.Fire;
using UnityEngine;

namespace Sankofa.Runtime.Personal
{
    /// <summary>Quiet virtual staging only. No recording, microphone or sound playback is created by the table.</summary>
    public sealed class StoryTableAtmosphere : MonoBehaviour
    {
        void OnEnable()
        {
            SceneLighting.SetAmbient(new Color(0.18f, 0.20f, 0.25f), new Color(0.10f, 0.075f, 0.055f));
            SceneLighting.SetMoon(new Vector3(-0.3f, 0.8f, -0.6f), new Color(0.045f, 0.055f, 0.075f));
            SceneLighting.SetMist(Color.black, 0, 0, 1, 100);
            SceneLighting.SetFog(new Color(0.024f, 0.028f, 0.038f), 30, 100);
            SceneLighting.SetGrade(1.1f, 0.96f, 0.10f, 0.10f);
        }
    }
}
