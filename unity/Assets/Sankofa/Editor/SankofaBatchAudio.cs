using UnityEditor;
using UnityEngine;

namespace Sankofa.EditorTools
{
    /// <summary>
    /// Batch runs (scene builds, tests, review captures) never play through the speakers. The PlayMode suites run the
    /// story at several times real speed, and whatever escaped their own muting came out of the computer sped up.
    /// Tests check sound from the clips' data, which muting the editor's output does not affect.
    /// </summary>
    [InitializeOnLoad]
    static class SankofaBatchAudio
    {
        static SankofaBatchAudio()
        {
            if (!Application.isBatchMode) return;
            // The editor's mute is the Game view's own toggle, so give back whatever the user had set.
            // Recorded once per run, since this runs again after every script reload.
            const string Key = "Sankofa.BatchAudio.WasMuted";
            if (SessionState.GetInt(Key, -1) < 0) SessionState.SetInt(Key, EditorUtility.audioMasterMute ? 1 : 0);
            EditorUtility.audioMasterMute = true;
            EditorApplication.quitting += () => EditorUtility.audioMasterMute = SessionState.GetInt(Key, 0) == 1;
        }
    }
}
