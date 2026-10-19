using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Sankofa.Core.Json;
using Sankofa.Core.Personal;
using Sankofa.Flow;
using Sankofa.Runtime.App;
using Sankofa.Runtime.Input;
using Sankofa.Runtime.UI;
using Sankofa.Storage;
using UnityEngine;
using UnityEngine.Networking;

namespace Sankofa.Runtime.Personal
{
    /// <summary>Composition for personal intake, checked content, a reusable room-aware table and explicit Keep.</summary>
    [DefaultExecutionOrder(500)]
    public sealed class PersonalStoryApp : MonoBehaviour
    {
        [Tooltip("Public HTTPS service root. Never place provider credentials in this scene.")]
        [SerializeField] string serviceEndpoint = "";
        [Tooltip("Limited service access credential for a controlled build, never a Google key. APK contents can be extracted; rotate and limit server access.")]
        [SerializeField] string serviceAccessToken = "";
        StoryTableView view;
        StoryTablePlacement placement;
        StoryTableKeyboard keyboard;
        PersonalStoryStore store;
        readonly PersonalStorySession session = new PersonalStorySession();
        string region = "enkare-nairobi", period = "1850s", interest = "trade-household", seed = "";
        UnityWebRequest pending;
        Action<string> decision;
        bool systemOwnsPause, reducedMotion;
        string narrationToken;
        AudioSource voice;
        Coroutine voiceRequest;
        bool narrationEnabled;
        UnityWebRequest pendingAudio;
        bool largeText;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        public static string StoreFolderOverride;
        public PersonalStorySession Session => session;
        public StoryTableView View => view;
        public StoryTableKeyboard Keyboard => keyboard;
        public string Seed => seed;
        public bool HasKept { get { try { return store != null && store.HasKept; } catch { return false; } } }
        public string ServiceEndpoint { get => serviceEndpoint; set => serviceEndpoint = value ?? ""; }
        public string ServiceAccessToken { set => serviceAccessToken = value ?? ""; }
        public bool NarrationEnabled => narrationEnabled;

        void Awake()
        {
            view = FindFirstObjectByType<StoryTableView>(FindObjectsInactive.Include);
            if (view == null) { enabled = false; return; }
            var oldAnchor = FindFirstObjectByType<WorldAnchor>();
            if (oldAnchor != null) oldAnchor.enabled = false; // This scene has one placement owner.
            placement = gameObject.AddComponent<StoryTablePlacement>();
            keyboard = gameObject.AddComponent<StoryTableKeyboard>();
            keyboard.Configure(view, view.Panels.First());
            var panels = view.Panels.Concat(new[] { keyboard.Panel }).ToArray();
            var hands = FindObjectsByType<HandSource>(FindObjectsSortMode.None);
            foreach (var panel in panels) foreach (var hand in hands) { var source = hand; panel.AddFingertip(() => source.Fingertip); }
            foreach (var pointer in FindObjectsByType<PointerSource>(FindObjectsSortMode.None)) pointer.ConfigurePanels(panels);
            store = new PersonalStoryStore(StoreFolderOverride ?? Path.Combine(Application.persistentDataPath, "personal-story"));
            voice = gameObject.AddComponent<AudioSource>(); voice.playOnAwake = false; voice.volume = 0; voice.spatialBlend = .75f;
            narrationEnabled = false; // A developer/simulator session starts silently; Listen is explicit.
            view.StartLocalRequested += StartPrepared;
            view.StartRequested += StartGenerated;
            view.RouteSelected += id => interest = id;
            view.RegionSelected += id => region = id;
            view.PeriodSelected += id => period = id;
            view.SeedEditRequested += EditSeed;
            view.NextRequested += () => { StopVoice(); session.Next(); };
            view.BackRequested += () => { StopVoice(); session.Back(); };
            view.QuestionSelected += id => { if (int.TryParse(id, out var index)) { StopVoice(); session.Ask(index); } };
            view.AskRequested += () => { StopVoice(); session.Ask(0); };
            view.SourceDetailsRequested += ShowSources;
            view.SaveRequested += RequestKeep;
            view.PauseRequested += () => session.Pause(!session.Paused);
            view.RecenterRequested += () => placement.Recenter();
            view.RoomRequested += () => placement.FitRoomTable();
            view.ReopenRequested += () => Reopen();
            view.ForgetRequested += Forget;
            view.ComfortRequested += ShowOptions;
            view.ContinueRequested += Continue;
            view.RetryRequested += StartGenerated;
            view.LeaveRequested += RequestLeave;
            view.DecisionSelected += id => { var action = decision; decision = null; action?.Invoke(id); };
            keyboard.Edited += value => view.ShowDecision(value.Length >= StoryTableKeyboard.MaximumLength ? "Limit reached • 500 / 500" : $"Your question • {value.Length} / 500", (value.Length == 0 ? "What would you like to understand?" : value) + "\n\nNothing is sent while you type. Delete text to make room if you reach the limit.", Array.Empty<(string, string)>());
            keyboard.Committed += value => { seed = value; decision = null; session.Cancel(); };
            keyboard.Cancelled += () => { decision = null; Render(); };
            session.Changed += Render;
            placement.StatusChanged += message =>
            {
                if (session.State == PersonalStoryState.Intake) view.SetStatus(message);
                else if (session.State == PersonalStoryState.Reading) view.SetStatus(Capability(session.Story) + " • " + message);
            };
            OVRManager.HMDUnmounted += OnFocusLost;
            OVRManager.InputFocusLost += OnFocusLost;
            OVRManager.HMDMounted += OnFocusRestored;
            OVRManager.InputFocusAcquired += OnFocusRestored;
        }

        void Start()
        {
            if (view == null) return;
            var eye = Camera.main;
            placement.Configure(view, eye != null ? eye.transform : null);
            reducedMotion = ExperienceSettings.Load().ReducedMotion;
            view.SetReducedMotion(reducedMotion);
            Render();
        }

        public void SetSeed(string question) { seed = question ?? ""; view.SetSeedText(seed); }
        PersonalStoryRequest Request() => new PersonalStoryRequest(region, period, interest, question: seed);

        public void StartPrepared()
        {
            if (session.Paused) return;
            AbortPending(); StopVoice(); ClearClips();
            keyboard.Close(); decision = null; narrationToken = null; view.ResetReadingPosition();
            var request = Request(); var version = session.Begin(request);
            if (session.State != PersonalStoryState.Preparing) return;
            var result = BundledPersonalStoryComposer.Compose(request);
            if (result.Success) session.Accept(version, result.Story); else session.Fail(version, result.Message);
        }

        public void StartGenerated()
        {
            if (session.Paused) return;
            AbortPending(); StopVoice(); ClearClips(); keyboard.Close(); decision = null; narrationToken = null; view.ResetReadingPosition();
            var input = Request(); var version = session.Begin(input);
            if (session.State != PersonalStoryState.Preparing) return;
            var endpoint = Endpoint();
            if (endpoint == null) { session.Fail(version, "The online storyteller is not connected. Your prepared story is ready below."); return; }
            StartCoroutine(Generate(version, input, endpoint));
        }

        string Endpoint()
        {
            var value = serviceEndpoint;
#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(value)) value = Environment.GetEnvironmentVariable("SANKOFA_STORY_SERVICE") ?? "http://127.0.0.1:8787";
#endif
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)) return null;
            if (uri.Scheme != "https" && !(Application.isEditor && uri.IsLoopback && uri.Scheme == "http")) return null;
            if (!string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)) return null;
            return uri.AbsoluteUri.TrimEnd('/');
        }

        void Authorize(UnityWebRequest request)
        {
            var token = serviceAccessToken;
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(token)) token = Environment.GetEnvironmentVariable("SANKOFA_STORY_ACCESS_TOKEN");
#endif
            // A client credential is distinct from server-side Google credentials and is never logged.
            if (!string.IsNullOrEmpty(token) && token.Length <= 512 && token.All(c => c >= '!' && c <= '~'))
                request.SetRequestHeader("Authorization", "Bearer " + token);
        }

        IEnumerator Generate(int version, PersonalStoryRequest input, string endpoint)
        {
            using (var call = new UnityWebRequest(endpoint + "/create-story", "POST"))
            {
                pending = call;
                call.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(PersonalStoryJson.WriteRequest(input)));
                call.downloadHandler = new DownloadHandlerBuffer(); call.timeout = 35; call.redirectLimit = 0;
                call.SetRequestHeader("Content-Type", "application/json");
                Authorize(call);
                yield return call.SendWebRequest();
                if (pending == call) pending = null;
                if (version != session.RequestVersion || session.State != PersonalStoryState.Preparing) yield break;
                if (call.result != UnityWebRequest.Result.Success || call.downloadedBytes > PersonalStoryValidator.MaxJsonLength * 4)
                { session.Fail(version, "The storyteller could not finish this request. Try again or read the prepared story."); yield break; }
                PersonalStoryDocument story = null;
                try { story = PersonalStoryJson.ReadValidated(call.downloadHandler.text, input); }
                catch (Exception) { session.Fail(version, "This telling could not be checked. Try again or read the prepared story."); }
                if (story != null && session.Accept(version, story))
                {
                    narrationToken = call.GetResponseHeader("X-Sankofa-Narration-Token");
                    if (story.Mode == "bundled") view.SetStatus("Prepared story • online creation was unavailable");
                    if (narrationEnabled) SpeakCurrent();
                }
            }
        }

        void Render()
        {
            if (view == null) return;
            view.SetPaused(session.Paused); keyboard.Suspend(session.Paused);
            if (session.Paused) { voice.Pause(); return; }
            if (voice.clip != null && narrationEnabled && !voice.isPlaying) voice.UnPause();
            if (decision != null || keyboard.IsOpen) return;
            var story = session.Story;
            switch (session.State)
            {
                case PersonalStoryState.Intake:
                    view.RenderStory("Sankofa", "For the stories you never got to hear", placement.Status);
                    view.SetIntakeSelection(interest, region, period); view.SetSeedText(seed); view.ShowIntake();
                    view.SetHasKeptStory(HasKept);
                    view.SetControlState(false, false, false, false);
                    break;
                case PersonalStoryState.Preparing: view.ShowPreparing("Listening to your question, gathering the next pages…"); break;
                case PersonalStoryState.Error: view.ShowError(session.Problem); break;
                case PersonalStoryState.Reading:
                    view.RenderStory(story.Title, "Enkare Nairobi • around the 1850s", Capability(story));
                    view.RenderChapter(new StoryTableChapterViewData
                    {
                        Index = session.ChapterIndex, Count = story.Chapters.Count, Title = session.Chapter.Title,
                        Caption = session.Chapter.Narration, ArtKey = session.Chapter.Illustration,
                        ImageLabel = TruthLabel(session.Chapter.Truth),
                        PreviousPageTitles = story.Chapters.Take(session.ChapterIndex).Select(c => c.Title).TakeLast(2).ToArray(),
                        Questions = session.Chapter.Questions.Select(q => q.Label).ToArray(),
                    });
                    view.SetControlState(session.ChapterIndex > 0, true, session.Chapter.Questions.Count > 0, false);
                    if (narrationEnabled && voice.clip == null && voiceRequest == null) SpeakCurrent();
                    break;
                case PersonalStoryState.Question:
                    view.RenderQuestion(session.Question.Label, session.Question.Answer, TruthLabel(session.Question.Truth)); break;
                case PersonalStoryState.Sources:
                    view.ShowSourceDetails("What this telling knows", SourcesText()); break;
                case PersonalStoryState.Closing:
                    view.ShowClosing(ReturnInvitation(story.Interest) + "\n\nKeep these exact pages on this device, or return to your choices without saving.");
                    view.SetControlState(true, false, false, true); break;
            }
        }

        static string TruthLabel(string truth) => truth == "historical" ? "Documented context • sources available" : truth == "personal-account" ? "Your account • not independently verified" : "Imagined scene • not an account of your ancestors";
        string Capability(PersonalStoryDocument story) => session.IsReplay
            ? "Kept telling • originally " + (story.Mode == "generated" ? "generated" : "prepared") + " • no new generation"
            : story.Mode == "generated" ? "Generated telling • prepared illustrations" : "Prepared story • works offline";
        static string ReturnInvitation(string thread) => thread == "trade-household"
            ? "What stayed at home while the gourd travelled? A question about household life can begin a different telling."
            : thread == "place-water"
                ? "Who followed the path beside the water? A question about journeys can follow the people next."
                : "What did the travellers bring to their meeting? A question about trade can follow what they carried.";
        string SourcesText()
        {
            var references = session.State == PersonalStoryState.Sources && session.Question != null ? session.Question.Sources : session.Chapter.Sources;
            var names = session.Story.Sources.Where(s => references.Contains(s.Id)).Select(s => s.Id + " • " + s.Title + "\n" + s.Url);
            return TruthLabel(session.Question != null ? session.Question.Truth : session.Chapter.Truth) + "\n\n" + string.Join("\n\n", names) + "\n\nThe illustrations are contemporary reconstructions. This is one researched setting; cultural and pronunciation review remain open.";
        }

        void EditSeed()
        {
            if (session.Paused) return;
            AbortPending(); keyboard.Open(seed);
        }
        void Continue()
        {
            if (keyboard.IsOpen) { keyboard.Close(); Render(); return; }
            if (session.ReturnToStory()) return;
            AbortPending(); StopVoice(); session.Cancel();
        }
        void ShowSources() { StopVoice(); session.Sources(); }
        void ShowOptions()
        {
            StopVoice();
            Decide("Make yourself comfortable", "These choices affect presentation. They do not change or save your story.",
                new[] { ("motion", reducedMotion ? "Gentle motion on" : "Still pages"), ("text", largeText ? "Standard text" : "Larger text"),
                    ("lower", "Lower the stand"), ("raise", "Raise the stand"), ("listen", narrationEnabled ? "Captions only" : "Read aloud"), ("return", "Return") }, id =>
                {
                    if (id == "motion") { reducedMotion = !reducedMotion; view.SetReducedMotion(reducedMotion); }
                    else if (id == "text") { largeText = !largeText; view.SetLargeText(largeText); }
                    else if (id == "lower") placement.AdjustHeight(-.06f);
                    else if (id == "raise") placement.AdjustHeight(.06f);
                    else if (id == "listen") { narrationEnabled = !narrationEnabled; voice.volume = narrationEnabled ? .85f : 0; }
                    Render();
                });
        }
        void Decide(string title, string text, IReadOnlyList<(string Id, string Label)> actions, Action<string> onAnswer)
        { decision = onAnswer; view.ShowDecision(title, text, actions); }

        void RequestKeep()
        {
            if (session.State != PersonalStoryState.Closing || session.Paused) return;
            if (HasKept)
                Decide("Replace your kept story?", "Only one story is kept on this device. The new telling will replace the earlier one.", new[] { ("replace", "Replace kept story"), ("cancel", "Keep the earlier story") }, id => { if (id == "replace") Keep(); else Render(); });
            else Keep();
        }
        void Keep()
        {
            try { store.Keep(session.Story); view.ShowClosing("Kept on this device. You can reopen the same telling or forget it from your story table."); }
            catch (Exception) { view.ShowClosing("This story could not be kept. It is still open; try Keep again or leave without saving."); }
            view.SetControlState(true, false, false, true);
        }
        public bool Reopen()
        {
            if (session.Paused) return false;
            try { var kept = store.Load(); if (kept == null) return false; AbortPending(); StopVoice(); ClearClips(); view.ResetReadingPosition(); return session.OpenSaved(kept); }
            catch (Exception) { ShelfProblem("Your kept story could not be opened. It has not been replaced.", () => Reopen()); return false; }
        }
        public void Forget()
        {
            if (session.Paused) return;
            Decide("Forget your kept story?", "This removes the story kept on this device.", new[] { ("forget", "Forget kept story"), ("cancel", "Keep it") }, id =>
            {
                if (id == "forget") { try { store.Forget(); } catch (Exception) { ShelfProblem("The kept story could not be removed.", Forget); return; } }
                Render();
            });
        }
        void ShelfProblem(string message, Action retry)
        {
            Decide("Your kept story", message + " No story request will be sent.", new[] { ("shelf-retry", "Try the kept story again"), ("shelf-return", "Return") },
                id => { if (id == "shelf-retry") retry(); else Render(); });
        }
        void RequestLeave()
        {
            if (session.State == PersonalStoryState.Intake)
            {
                Decide("Your story table", "Your choices are private until you choose online creation. You can reopen or forget the one story explicitly kept here.",
                    HasKept ? new[] { ("reopen", "Reopen kept story"), ("forget", "Forget kept story"), ("exit", "Close Sankofa"), ("cancel", "Return") } : new[] { ("exit", "Close Sankofa"), ("cancel", "Return") }, id =>
                    { if (id == "reopen") Reopen(); else if (id == "forget") Forget(); else if (id == "exit") Quit(); else Render(); });
                return;
            }
            Decide("Return to your story table?", "This telling stays unsaved unless you choose Keep at its ending.", new[] { ("leave", "Return to my choices"), ("cancel", "Stay with the story") }, id =>
            { if (id == "leave") { AbortPending(); StopVoice(); keyboard.Close(); session.Pause(false); session.Cancel(); } else Render(); });
        }
        void Quit() { AbortPending(); StopVoice(); if (Application.isEditor) { session.Pause(false); session.Cancel(); } else Application.Quit(); }
        void OnFocusLost() { if (!session.Paused) { systemOwnsPause = true; session.Pause(true); } }
        void OnFocusRestored() { if (systemOwnsPause) { systemOwnsPause = false; session.Pause(false); } }
        void OnApplicationPause(bool value) { if (value) OnFocusLost(); else OnFocusRestored(); }
        void AbortPending() { if (pending != null) { pending.Abort(); pending = null; } }
        void StopVoice()
        {
            if (voice != null) { voice.Stop(); voice.clip = null; }
            if (pendingAudio != null) { pendingAudio.Abort(); pendingAudio = null; }
            if (voiceRequest != null) { StopCoroutine(voiceRequest); voiceRequest = null; }
        }
        static string AudioKey(string text)
        {
            using (var hash = SHA256.Create()) return "p-" + BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant().Substring(0, 16);
        }
        void SpeakCurrent()
        {
            if (!narrationEnabled || session.State != PersonalStoryState.Reading || session.Paused || session.Chapter == null) return;
            var key = AudioKey(session.Chapter.Narration);
            var clip = Resources.Load<AudioClip>("Narration/personal-table/" + key);
            if (clip == null) clips.TryGetValue(key, out clip);
            if (clip != null) { PlayClip(clip); return; }
            var endpoint = Endpoint();
            if (endpoint == null || string.IsNullOrEmpty(narrationToken)) { view.SetStatus("This telling is available as captions • no saved voice recording"); return; }
            voiceRequest = StartCoroutine(FetchVoice(key, endpoint, session.RequestVersion, session.Chapter.Id));
        }
        IEnumerator FetchVoice(string key, string endpoint, int version, string chapterId)
        {
            var body = new JsonObject().Add("story", JsonText.ParseObject(PersonalStoryJson.Write(session.Story))).Add("chapter_id", chapterId).Add("token", narrationToken);
            using (var call = UnityWebRequestMultimedia.GetAudioClip(endpoint + "/narration", AudioType.MPEG))
            {
                pendingAudio = call; call.method = "POST";
                call.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonText.Serialize(body)));
                call.SetRequestHeader("Content-Type", "application/json"); call.timeout = 30; call.redirectLimit = 0;
                Authorize(call);
                view.SetStatus("Preparing the voice • the captions remain available");
                yield return call.SendWebRequest();
                if (pendingAudio == call) pendingAudio = null;
                voiceRequest = null;
                if (version != session.RequestVersion || session.Chapter == null || session.Chapter.Id != chapterId) yield break;
                if (call.result != UnityWebRequest.Result.Success || call.downloadedBytes > 8 * 1024 * 1024)
                { view.SetStatus("Voice is unavailable • continue with captions"); yield break; }
                var clip = DownloadHandlerAudioClip.GetContent(call);
                if (clip == null) { view.SetStatus("Voice is unavailable • continue with captions"); yield break; }
                clips[key] = clip;
                if (narrationEnabled && !session.Paused && session.State == PersonalStoryState.Reading) PlayClip(clip);
            }
        }
        void PlayClip(AudioClip clip)
        {
            if (!narrationEnabled || clip == null || session.Paused) return;
            voice.transform.position = view.TableRoot.TransformPoint(new Vector3(-.47f, .20f, .20f));
            voice.clip = clip; voice.volume = .85f; voice.Play();
        }
        void ClearClips() { foreach (var clip in clips.Values) if (clip != null) Destroy(clip); clips.Clear(); }
        void OnDestroy()
        {
            AbortPending(); StopVoice(); ClearClips();
            OVRManager.HMDUnmounted -= OnFocusLost; OVRManager.InputFocusLost -= OnFocusLost;
            OVRManager.HMDMounted -= OnFocusRestored; OVRManager.InputFocusAcquired -= OnFocusRestored;
            session.Changed -= Render;
        }
    }
}
