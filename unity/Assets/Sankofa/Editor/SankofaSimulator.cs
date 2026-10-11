using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Sankofa.EditorTools
{
    /// <summary>
    /// Runs the hearth in Play mode against the Meta XR Simulator (installed at /Applications/MetaXRSimulator.app).
    /// From a terminal, with the editor closed:
    /// Unity -projectPath unity -executeMethod Sankofa.EditorTools.SankofaSimulator.PlayHearth
    /// The simulator exercises flow, audio, visuals and wiring; it is not evidence of device tracking, comfort or performance.
    /// </summary>
    public static class SankofaSimulator
    {
        const string ActivateMenu = "Window/Meta/Meta XR Simulator/Activate";

        [MenuItem("Sankofa/Play Hearth in XR Simulator")]
        public static void PlayHearth()
        {
            Play(SankofaSceneBuilder.HearthScene);
        }

        [MenuItem("Sankofa/Play Illustrated Theatre in XR Simulator")]
        public static void PlayIllustratedTheatre()
        {
            Play(SankofaSceneBuilder.TheatreScene);
        }

        static void Play(string scene)
        {
            if (EditorApplication.isPlaying) return;
            // Meta disables its Activate item once the simulator is active, so check the runtime variable instead.
            var runtime = System.Environment.GetEnvironmentVariable("XR_SELECTED_RUNTIME_JSON") ?? "";
            if (!runtime.Contains("MetaXRSimulator") && !EditorApplication.ExecuteMenuItem(ActivateMenu))
                Debug.LogWarning("[Sankofa] Could not activate the Meta XR Simulator from " + ActivateMenu + "; is it installed?");
            EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
            Debug.Log("[Sankofa] Entering Play mode in the XR Simulator: " + scene);
            EditorApplication.EnterPlaymode();
        }
    }
}
