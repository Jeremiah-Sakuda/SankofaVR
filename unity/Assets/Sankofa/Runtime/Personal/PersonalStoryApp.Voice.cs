using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sankofa.Core.Personal;
using Sankofa.Core.Story;
using Sankofa.Flow;
using UnityEngine;
using UnityEngine.Networking;

namespace Sankofa.Runtime.Personal
{
    /// <summary>
    /// Asking the fire aloud. A visitor agrees once, the fire listens for up to twelve seconds, and the recording and
    /// the pages reached so far go to the story service. Every failure, refusal or missing piece falls back to the
    /// page's own authored questions, which need no service and no microphone.
    /// </summary>
    public sealed partial class PersonalStoryApp
    {
        [Tooltip("Lets a visitor ask the fire aloud when a story service is configured. A question is recorded only after the visitor agrees, and only while the fire is listening.")]
        [SerializeField] bool spokenQuestions;
        IQuestionRecorder recorder = new MicrophoneRecorder();
        bool voiceConsent, listening;
        int askVersion;
        UnityWebRequest pendingAsk;
        string storyToken;
        const float ShortestQuestionSeconds = 0.4f;

        public bool SpokenQuestions
        {
            get => spokenQuestions;
            set { spokenQuestions = value; if (view != null && decision == null) Render(); }
        }
        /// <summary>The fire is recording a question now.</summary>
        public bool Listening => listening;
        /// <summary>A switch, a configured service and a microphone are all present.</summary>
        public bool SpokenQuestionsAvailable => SpokenQuestionsWanted && Endpoint() != null && recorder != null && recorder.Available;
        bool SpokenQuestionsWanted
        {
            get
            {
#if UNITY_EDITOR
                if (Environment.GetEnvironmentVariable("SANKOFA_SPOKEN_QUESTIONS") == "1") return true;
#endif
                return spokenQuestions;
            }
        }
        /// <summary>Replaces the microphone, as a test does; any recording under way is discarded.</summary>
        public void UseRecorder(IQuestionRecorder value) { StopListening(false); recorder = value; }

        void AskTheFire()
        {
            if (session.State != PersonalStoryState.Reading || session.Paused || session.Chapter == null) return;
            StopVoice();
            if (!SpokenQuestionsAvailable) { if (session.Chapter.Questions.Count > 0) session.Ask(0); return; }
            if (voiceConsent) { StartCoroutine(Listen()); return; }
            var actions = new List<(string, string)> { ("agree", "Ask aloud") };
            if (session.Chapter.Questions.Count > 0) actions.Add(("questions", "Show the questions"));
            actions.Add(("return", "Return to the story"));
            Decide("Ask the fire aloud?",
                "Your spoken question, up to twelve seconds, and the pages of this telling you have reached are sent to the Sankofa service, which uses Google Gemini to answer. The recording is not kept on this device. The page's own questions need no recording.",
                actions, id =>
                {
                    if (id == "agree") { voiceConsent = true; StartCoroutine(Listen()); }
                    else if (id == "questions") ShowOwnQuestions(null);
                    else Render();
                });
        }

        IEnumerator Listen()
        {
            var version = ++askVersion;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
            {
                bool? granted = null;
                var callbacks = new UnityEngine.Android.PermissionCallbacks();
                callbacks.PermissionGranted += id => granted = true;
                callbacks.PermissionDenied += id => granted = false;
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone, callbacks);
                for (var waited = 0f; granted == null && waited < 30f; waited += Time.unscaledDeltaTime) yield return null;
                if (granted != true)
                {
                    if (version == askVersion) ShowOwnQuestions("The fire cannot hear you without the microphone.");
                    yield break;
                }
            }
#endif
            if (version != askVersion || session.State != PersonalStoryState.Reading || session.Paused) yield break;
            if (recorder == null || !recorder.Begin()) { ShowOwnQuestions("The fire could not listen just now."); yield break; }
            listening = true;
            Decide("The fire is listening", "Ask about this story, then touch Send. It listens for up to twelve seconds.",
                new[] { ("send", "Send my question"), ("cancel", "Never mind") },
                id => { if (id == "send") SendQuestion(); else StopListening(true); });
            view.SetFireState(FireState.Listening);
        }

        void StopListening(bool render)
        {
            if (listening) { recorder?.Cancel(); listening = false; }
            if (render) { decision = null; Render(); }
        }

        void TickVoice()
        {
            if (!listening) return;
            // Leaving the page or pausing discards the recording; nothing half-heard is sent.
            if (session.State != PersonalStoryState.Reading || session.Paused) { StopListening(false); decision = null; return; }
            if (recorder.Seconds >= PersonalAnswerAudio.MaxSeconds - 0.15f) { decision = null; SendQuestion(); }
        }

        void SendQuestion()
        {
            if (!listening) return;
            listening = false;
            var samples = recorder.End(out var count);
            if (samples == null || count < ShortestQuestionSeconds * PersonalAnswerAudio.SampleRate) { ShowOwnQuestions("The fire did not hear a question."); return; }
            var endpoint = Endpoint();
            string body = null;
            try { body = PersonalAnswerJson.WriteAudioRequest(session.Story, storyToken, session.Chapter.Id, PersonalAnswerAudio.EncodeWav(samples, count)); }
            catch (Exception) { }
            if (body == null || endpoint == null) { ShowOwnQuestions("The fire could not carry that question."); return; }
            StartCoroutine(Ask(body, endpoint, ++askVersion, session.RequestVersion, session.Chapter.Id));
        }

        IEnumerator Ask(string body, string endpoint, int version, int request, string chapterId)
        {
            Decide("The fire turns your question over", "A moment. You can return to the story at any time.",
                new[] { ("cancel", "Return to the story") }, id => { askVersion++; AbortAsk(); Render(); });
            view.SetFireState(FireState.Gathering);
            using (var call = new UnityWebRequest(endpoint + "/ask", "POST"))
            {
                pendingAsk = call;
                call.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                call.downloadHandler = new DownloadHandlerBuffer(); call.timeout = 20; call.redirectLimit = 0;
                call.SetRequestHeader("Content-Type", "application/json");
                Authorize(call);
                yield return call.SendWebRequest();
                if (pendingAsk == call) pendingAsk = null;
                // A late reply never interrupts a different page, a new telling or a visitor who went back to the story.
                if (version != askVersion || request != session.RequestVersion || session.State != PersonalStoryState.Reading
                    || session.Chapter == null || session.Chapter.Id != chapterId) yield break;
                var ok = call.result == UnityWebRequest.Result.Success && call.responseCode == 200 && call.downloadedBytes <= 64 * 1024;
                ReceiveAnswer(ok ? call.downloadHandler.text : null, ok ? call.GetResponseHeader("X-Sankofa-Answer-Token") : null);
            }
        }

        /// <summary>
        /// Shows a reply from the service for the current page. Anything the validator refuses, and any failure, shows
        /// the page's own questions instead. A reply that names one of those questions opens it as the page would.
        /// </summary>
        public void ReceiveAnswer(string reply, string answerToken)
        {
            if (session.State != PersonalStoryState.Reading || session.Chapter == null) return;
            PersonalAnswer answer = null;
            if (reply != null) { try { answer = PersonalAnswerJson.ReadValidated(reply, session.Story, session.Chapter.Id); } catch (Exception) { } }
            if (answer == null) { ShowOwnQuestions("The fire could not answer that aloud."); return; }
            if (answer.IsQuestionList)
            {
                ShowOwnQuestions("The fire offers what it can answer about this page.", answer.Questions.Select(q => q.Id).ToArray());
                return;
            }
            decision = null;
            var own = answer.Mode == PersonalAnswer.Bundled && answer.QuestionId != null
                ? Enumerable.Range(0, session.Chapter.Questions.Count).Where(i => session.Chapter.Questions[i].Id == answer.QuestionId).DefaultIfEmpty(-1).First() : -1;
            if (own >= 0) { session.Ask(own); return; }
            Decide("The fire answers", answer.Answer + "\n\n" + AnswerLabel(answer.Truth), new[] { ("return", "Return to the story") }, id => { StopVoice(); Render(); });
            view.SetFireState(FireState.Telling);
            if (narrationEnabled && !string.IsNullOrEmpty(answerToken)) voiceRequest = StartCoroutine(SpeakAnswer(answer, answerToken, askVersion));
        }

        /// <summary>An answer carries no spoken disclosure, so its label is always shown with it.</summary>
        static string AnswerLabel(string truth) => truth == PersonalAnswer.Historical ? "Documented context • sources available"
            : truth == PersonalAnswer.Unknown ? "Outside tonight's story" : "The griot's answer • imagined";

        void ShowOwnQuestions(string message, IReadOnlyList<string> only = null)
        {
            decision = null;
            var questions = session.Chapter != null ? session.Chapter.Questions : new PersonalStoryQuestion[0];
            var offered = Enumerable.Range(0, questions.Count).Where(i => only == null || only.Contains(questions[i].Id)).Take(3).ToArray();
            if (offered.Length == 0)
            {
                Decide("The fire is quiet on this page", message ?? "This page has no questions of its own.", new[] { ("return", "Return to the story") }, id => Render());
                return;
            }
            var actions = offered.Select(i => ("q" + i, questions[i].Label)).Concat(new[] { ("return", "Return to the story") }).ToArray();
            Decide("Ask about this page", message ?? "Choose a question this page can answer.", actions, id =>
            {
                if (id.Length > 1 && id[0] == 'q' && int.TryParse(id.Substring(1), out var index)) session.Ask(index); else Render();
            });
        }

        IEnumerator SpeakAnswer(PersonalAnswer answer, string token, int version)
        {
            var endpoint = Endpoint();
            if (endpoint == null) { voiceRequest = null; yield break; }
            using (var call = UnityWebRequestMultimedia.GetAudioClip(endpoint + "/narration", AudioType.MPEG))
            {
                pendingAudio = call; call.method = "POST";
                call.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(PersonalAnswerJson.WriteNarrationRequest(answer, token)));
                call.SetRequestHeader("Content-Type", "application/json"); call.timeout = 30; call.redirectLimit = 0;
                Authorize(call);
                yield return call.SendWebRequest();
                if (pendingAudio == call) pendingAudio = null;
                voiceRequest = null;
                // The answer may have been closed while its voice was prepared; it then stays unspoken.
                if (version != askVersion || decision == null || call.result != UnityWebRequest.Result.Success || call.downloadedBytes > 8 * 1024 * 1024) yield break;
                var clip = DownloadHandlerAudioClip.GetContent(call);
                if (clip == null) yield break;
                clips["answer:" + version] = clip;
                PlayClip(clip);
                pageSpoken = false;
            }
        }

        void AbortAsk() { if (pendingAsk != null) { pendingAsk.Abort(); pendingAsk = null; } }
    }
}
