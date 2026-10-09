using UnityEngine;

namespace Sankofa.Runtime.Stage
{
    /// <summary>
    /// The stage's own time: it stops while the visit is paused, so figures, the ewe, traffic and incidental sounds
    /// hold still with the story. The application sets <see cref="Paused"/> from the flow's phase.
    /// </summary>
    public static class StageClock
    {
        public static bool Paused { get; set; }
        public static float DeltaTime => Paused ? 0f : Time.deltaTime;
        public static float AnimatorSpeed => Paused ? 0f : 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => Paused = false;
    }
}
