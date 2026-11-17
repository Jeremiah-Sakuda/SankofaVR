using UnityEngine;

namespace Sankofa.Runtime.Personal
{
    public sealed partial class PersonalStoryApp
    {
        AudioClip readingVoiceClip;
        string readingVoiceKey;
        int readingVoiceSample;

        // A notice can interrupt an unfinished page without making its first words play over later captions.
        // This checkpoint is local presentation state, never saved story content or a pending provider response.
        void SuspendReadingVoice()
        {
            var clip = readingVoiceClip;
            var key = readingVoiceKey;
            var sample = readingVoiceSample;
            var current = PageKey();
            if (current != null && !PageToldOnce && voice != null && voice.clip != null && (voice.isPlaying || voiceHeld))
            {
                clip = voice.clip;
                key = current;
                sample = voice.timeSamples;
            }
            // Keep the existing cancellation contract: an in-flight request cannot finish behind a notice.
            StopVoice();
            readingVoiceClip = clip;
            readingVoiceKey = key;
            readingVoiceSample = sample;
        }

        bool ResumeReadingVoice()
        {
            if (readingVoiceClip == null || session.Paused || presentationHeld) return false;
            var clip = readingVoiceClip;
            var sample = readingVoiceSample;
            var matches = narrationEnabled && readingVoiceKey == PageKey();
            ClearReadingVoice();
            if (!matches) return false;
            PlayClip(clip, sample);
            view.SetNarrationProgress((float)sample / clip.frequency, clip.length);
            // Source/answer screens temporarily have no reading key. Restoring this key keeps the resumed
            // recording's completion flag from being cleared on the next frame.
            turnKey = PageKey();
            pageTold = false;
            pageRest = 0;
            return true;
        }

        void ClearReadingVoice()
        {
            readingVoiceClip = null;
            readingVoiceKey = null;
            readingVoiceSample = 0;
        }
    }
}
