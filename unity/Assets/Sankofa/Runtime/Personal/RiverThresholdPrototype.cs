using System;
using System.Linq;
using Sankofa.Core.Personal;
using Sankofa.Flow;
using Sankofa.Runtime.App;
using Sankofa.Runtime.Fire;
using Sankofa.Runtime.Input;
using Sankofa.Runtime.UI;
using TMPro;
using UnityEngine;

namespace Sankofa.Runtime.Personal
{
    /// <summary>Opt-in cinematic presentation of the existing water telling. Story decisions remain in PersonalStoryApp.</summary>
    [DefaultExecutionOrder(600)]
    public sealed class RiverThresholdPrototype : MonoBehaviour
    {
        public const float DisclosureAt = 18;
        public const float TitleAt = 27;
        static readonly float[] OpeningStarts = { 0, 4, DisclosureAt, TitleAt };
        static readonly float[] OpeningEnds = { 4, DisclosureAt, TitleAt, 32 };
        [SerializeField] Transform world, opening, riverside, heroPot;
        [SerializeField] Renderer painting, firePainting, veil, water;
        [SerializeField] Renderer[] people;
        [SerializeField] TMP_Text title, words, context, thresholdCaption;
        [SerializeField] PokePanel controls;
        [SerializeField] HearthFire openingFire;
        [Tooltip("Only assign a release-cleared score here. The editor can separately audition the ignored provisional track.")]
        [SerializeField] AudioClip clearedMusic;
        [SerializeField] AudioClip fireRecording, riverRecording;
        [SerializeField] AudioClip potTouch, waterStir, paperTurn;
        [SerializeField] Transform[] embers = Array.Empty<Transform>();

        readonly RiverThresholdSequence sequence = new RiverThresholdSequence();
        PersonalStoryApp app;
        StoryTableView view;
        StoryTablePlacement placement;
        Renderer[] tableDecor;
        AudioSource introVoice, music, fireBed, riverBed, effects;
        Renderer[] emberPaint;
        Transform page;
        Vector3 pagePosition;
        Quaternion pageRotation;
        MaterialPropertyBlock properties;
        RiverThresholdPhase paintedPhase = (RiverThresholdPhase)(-1);
        bool pinned, resumeExisting, voicePaused, roomPlacement;
        bool waterCued;
        int spokenBeat = -1;
        float sceneClock;
        Vector3 paintingPosition, potPosition;
        Vector3 paintingScale;

        public RiverThresholdSequence Sequence => sequence;
        public float PaintClock => sceneClock;
        public PokePanel Controls => controls;
        public PersonalStoryApp App => app;
        public string OpeningWords => words.text;
        public bool RiverVisible => riverside.gameObject.activeSelf;
        public bool AutomaticClock { get; set; } = true;
        public int RequestedSoundCues { get; private set; }
        public string LastSoundCue { get; private set; }

        void Awake()
        {
            app = FindFirstObjectByType<PersonalStoryApp>();
            view = app.View;
            placement = app.GetComponent<StoryTablePlacement>();
            placement.Recentered += Recentered;
            placement.RoomPlacementRequested += RoomPlacementRequested;
            properties = new MaterialPropertyBlock();
            app.SuppressWelcome = true;
            app.HoldPresentation(true);
            app.PreparedStoryStarting += BeforePreparedStory;
            app.KeptStoryReopened += ReopenKeptStory;
            app.TablePresentationRequested += ReturnToTable;
            controls.Pressed += Press;
            var hands = FindObjectsByType<HandSource>(FindObjectsSortMode.None);
            foreach (var hand in hands) { var source = hand; controls.AddFingertip(() => source.Fingertip); }
            var panels = view.Panels.Concat(FindObjectsByType<StoryTableKeyboard>(FindObjectsSortMode.None).Select(k => k.Panel)).Append(controls).ToArray();
            foreach (var pointer in FindObjectsByType<PointerSource>(FindObjectsSortMode.None)) pointer.ConfigurePanels(panels);
            // Keep the existing stage clock and route data alive while its small scenery is visually set aside.
            page = view.TableRoot.Find("Story page"); pagePosition = page.localPosition; pageRotation = page.localRotation;
            var fire = view.TableRoot.Find("Griot fire");
            tableDecor = view.TableRoot.GetComponentsInChildren<Renderer>(true)
                .Where(r => !r.transform.IsChildOf(page) && !r.transform.IsChildOf(fire)
                    && (r.transform.IsChildOf(view.Stage.transform) || r.transform.IsChildOf(view.TableRoot.Find("Virtual table"))
                        || r.name == "Stage drum" || r.transform.IsChildOf(view.TableRoot.Find("Kept strand")))).ToArray();
            paintingPosition = painting.transform.localPosition; paintingScale = painting.transform.localScale;
            potPosition = heroPot.localPosition;
            introVoice = gameObject.AddComponent<AudioSource>(); introVoice.playOnAwake = false;
            introVoice.spatialBlend = 0; introVoice.volume = .85f; introVoice.dopplerLevel = 0;
            music = gameObject.AddComponent<AudioSource>(); music.playOnAwake = false; music.loop = true;
            music.clip = clearedMusic; music.spatialBlend = 0; music.volume = 0;
#if UNITY_EDITOR
            // Explicitly authorised local audition. Never serialise this unverified asset into a distributable scene.
            if (music.clip == null && Environment.GetEnvironmentVariable("SANKOFA_PROVISIONAL_MUSIC") == "1")
                music.clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/LocalOnly/RiverThreshold/intro-music.mp3");
#endif
            fireBed = Bed(fireRecording); riverBed = Bed(riverRecording);
            effects = new GameObject("Threshold foley").AddComponent<AudioSource>(); effects.transform.SetParent(transform,false);
            effects.playOnAwake=false; effects.spatialBlend=.65f; effects.dopplerLevel=0; effects.minDistance=1; effects.maxDistance=8;
            emberPaint=embers.Select(e=>e.GetComponent<Renderer>()).ToArray();
        }

        AudioSource Bed(AudioClip clip)
        {
            var source = new GameObject("Opening recording").AddComponent<AudioSource>(); source.transform.SetParent(transform,false);
            source.playOnAwake=false; source.loop=true; source.clip=clip; source.volume=0; source.dopplerLevel=0;
            source.spatialBlend=.7f; source.minDistance=1; source.maxDistance=10; return source;
        }

        void Start()
        {
            sequence.ReducedMotion = ExperienceSettings.Load().ReducedMotion;
            SceneLighting.SetGrade(.88f,.92f,.10f,.10f);
            Paint();
        }

        void BeforePreparedStory(string interest)
        {
            if (interest != "place-water" || !sequence.RequestWater()) return;
            resumeExisting = false;
            app.HoldPresentation(true);
            Cue("pot-touch",potTouch,.24f);
            Paint();
        }

        void ReopenKeptStory(string interest)
        {
            if (interest != "place-water" || !sequence.RequestWater()) return;
            // The saved document is already open. The reveal must never compose or replace its realised content.
            resumeExisting = true;
            app.HoldPresentation(true);
            Paint();
        }

        void Update()
        {
            if (AutomaticClock) Advance(Time.deltaTime);
        }

        void ReturnToTable()
        {
            if (sequence.Phase != RiverThresholdPhase.Riverside) return;
            sequence.StayAtTable(); Paint();
        }

        /// <summary>Same clock used by real frames and deterministic desktop capture; no story or camera seeking.</summary>
        public void Advance(float elapsed)
        {
            sequence.ReducedMotion = app.ReducedMotionEnabled;
            var before = sequence.Phase;
            sequence.Tick(elapsed, app.Session.Paused);
            if (!app.Session.Paused) sceneClock += Mathf.Clamp(elapsed, 0, .1f);
            if (before != sequence.Phase)
            {
                if (sequence.Phase == RiverThresholdPhase.Choosing) FinishOpening();
                if (sequence.Phase == RiverThresholdPhase.Riverside)
                {
                    app.HoldPresentation(false);
                    if (!resumeExisting) app.StartPrepared();
                }
            }
            // Ending, confirmed leave and recovery all return to a complete table intake.
            if ((sequence.Phase == RiverThresholdPhase.Riverside || sequence.Phase == RiverThresholdPhase.Table)
                && app.Session.State == PersonalStoryState.Intake)
            { returnedToChoices = true; sequence.ReturnToChoices(); }
            Paint();
        }

        void Press(string id)
        {
            if (id == "pause") { app.Session.Pause(!app.Session.Paused); Paint(); return; }
            if (id == "motion")
            {
                sequence.ReducedMotion = !sequence.ReducedMotion;
                app.SetReducedMotion(sequence.ReducedMotion);
                Paint(); return;
            }
            if (app.Session.Paused || id != "primary") return;
            switch (sequence.Phase)
            {
                case RiverThresholdPhase.Opening: sequence.Choose(); FinishOpening(); break;
                case RiverThresholdPhase.Crossing:
                    if (resumeExisting) sequence.StayAtTable(); else sequence.ReturnToChoices();
                    resumeExisting = false; app.HoldPresentation(false); break;
                case RiverThresholdPhase.Riverside: sequence.StayAtTable(); break;
                case RiverThresholdPhase.Choosing:
                    if (app.Session.State == PersonalStoryState.Intake)
                    { sequence.ReplayOpening(); spokenBeat = -1; music.Stop(); app.HoldPresentation(true); }
                    break;
                case RiverThresholdPhase.Table:
                    if (roomPlacement) placement.Recenter();
                    resumeExisting = true;
                    if (sequence.RequestWater()) app.HoldPresentation(true);
                    break;
            }
            Paint();
        }

        bool returnedToChoices;
        void FinishOpening()
        {
            introVoice.Stop();
            app.HoldPresentation(false);
            view.SetInvitation("Touch the water pot, or point at its label, pinch and release.\nStay seated. Let the river come to you.");
        }

        void LateUpdate()
        {
            if (!pinned)
            {
                // Use the stand's settled heading and seat height once. Neither this nor any reveal writes to the rig.
                AnchorToSeat();
            }
            if (sequence.Phase == RiverThresholdPhase.Riverside)
            {
                page.localRotation = Quaternion.Euler(35,0,0); // The small table's page-turn tilt does not tip this reading stand.
                foreach (var r in tableDecor) if (r != null) r.enabled = false;
                for (var i = 0; i < people.Length; i++)
                {
                    var source = view.Stage.FigureRoot(i == 0 ? "wambui" : "naserian");
                    if (source == null) continue;
                    var paint = source.GetComponentsInChildren<Renderer>().FirstOrDefault(r => r.name == "Painted figure");
                    if (paint == null) continue;
                    paint.GetPropertyBlock(properties);
                    people[i].SetPropertyBlock(properties);
                }
            }
        }

        void Recentered() { roomPlacement = false; AnchorToSeat(); Paint(); }
        void RoomPlacementRequested()
        {
            roomPlacement = true;
            if (sequence.Phase == RiverThresholdPhase.Riverside) sequence.StayAtTable();
            Paint();
        }
        void AnchorToSeat()
        {
            // Only explicit recentering or initial tracking recovery moves this world, never stand height or head motion.
            var eye = Camera.main;
            var position = view.TableRoot.position - view.TableRoot.rotation * new Vector3(0, .68f, .72f);
            if (eye != null && eye.transform.position.y >= .45f) position = eye.transform.position - Vector3.up * 1.15f;
            else if (pinned) position.y = world.position.y; // Raising a stand must not raise the riverbank.
            world.SetPositionAndRotation(position, Quaternion.Euler(0,view.TableRoot.eulerAngles.y,0));
            pinned = true;
        }

        void Paint()
        {
            var phase = sequence.Phase;
            var t = (float)sequence.Seconds;
            var intro = phase == RiverThresholdPhase.Opening;
            var crossing = phase == RiverThresholdPhase.Crossing;
            var outside = phase == RiverThresholdPhase.Riverside;
            var held = intro || crossing;
            view.SetSpatialStageVisible(!outside);
            app.StoryAmbiencePosition = outside ? world.TransformPoint(new Vector3(1.6f,0,3)) : (Vector3?)null;
            if (paintedPhase != phase)
            {
                foreach (var r in tableDecor) if (r != null) r.enabled = true;
                // The view restores its own choice/invitation visibility on Update. Forcing every panel open
                // here briefly exposes "Set the gourd down" during intake, where no invitation exists.
                if (crossing) waterCued=false;
                else effects.Stop();
                page.localPosition = outside ? new Vector3(0,.18f,.02f) : pagePosition;
                page.localRotation = outside ? Quaternion.Euler(35,0,0) : pageRotation;
                paintedPhase = phase;
            }
            controls.transform.SetParent(held ? world : page, false);
            view.TableRoot.gameObject.SetActive(!held);
            var choosing = phase == RiverThresholdPhase.Choosing && app.Session.State == PersonalStoryState.Intake;
            opening.gameObject.SetActive(intro || (choosing && !roomPlacement));
            riverside.gameObject.SetActive(outside || (crossing && t >= 2.4f));
            foreach (var person in people) person.enabled = outside; // People rise with the first told page, never disappear at the handoff.
            heroPot.gameObject.SetActive(crossing && t < 3.2f);
            thresholdCaption.gameObject.SetActive(crossing);
            thresholdCaption.text = t < 2.8f ? "A small thing can carry you a long way." : "Enkare Nairobi · around the 1850s\nAn imagined encounter beside the river";
            // An ink dissolve changes the surroundings at full scale. There is no camera travel or environment zoom.
            var cover = crossing ? (t < 2.4f ? Smooth(t / 2.4f) : 1 - Smooth((t - 2.4f) / 3.6f)) : 0;
            Opacity(veil, cover * .99f);
            if (crossing)
            {
                heroPot.localPosition = potPosition + (sequence.ReducedMotion ? Vector3.zero : Vector3.up * (.045f * Smooth(t / 2)));
                heroPot.localRotation = Quaternion.Euler(0, sequence.ReducedMotion ? 0 : t * 3, 0);
                if(!waterCued && t>=1.2f && !app.Session.Paused) { waterCued=true; Cue("water-stir",waterStir,.25f); }
            }
            if (intro) PaintOpening(t);
            else if (choosing)
            {
                Opacity(painting,.24f); Opacity(firePainting,0);
                title.text=returnedToChoices ? "YOUR STORY TABLE" : "THE WATER POT";
                words.text="A story waits inside\nan ordinary thing."; context.text="";
                title.alpha=.8f; words.alpha=.85f;
            }
            openingFire.enabled = !app.Session.Paused;
            openingFire.ReducedMotion = sequence.ReducedMotion;
            var primary = intro ? "Skip intro" : crossing ? "Cancel" : outside ? "At the table" : phase == RiverThresholdPhase.Table ? "Beside the river" : "Replay intro";
            var buttons = new[] { ("primary", primary), ("pause", app.Session.Paused ? "Resume" : "Pause"), ("motion", sequence.ReducedMotion ? "Motion: reduced" : "Less motion") };
            controls.Show(outside || phase == RiverThresholdPhase.Table ? buttons.Take(1).ToArray()
                : phase == RiverThresholdPhase.Choosing && app.Session.State != PersonalStoryState.Intake ? Array.Empty<(string, string)>() : buttons);
            // These controls remain within single-hand reach; story controls retain their adjustable stand placement.
            controls.transform.localPosition = held ? new Vector3(0, .82f, .70f) : new Vector3(0,-.178f,-.008f);
            controls.transform.localRotation = held ? Quaternion.Euler(26,0,0) : Quaternion.identity;
            SyncAudio(intro || crossing || choosing);
            water.GetPropertyBlock(properties);
            properties.SetFloat("_Clock",sceneClock); properties.SetFloat("_Motion",sequence.ReducedMotion ? 0 : 1);
            water.SetPropertyBlock(properties);
            foreach(var paint in new[]{painting,firePainting})
            {
                paint.GetPropertyBlock(properties); properties.SetFloat("_Clock",sceneClock);
                properties.SetFloat("_Motion",sequence.ReducedMotion ? 0 : 1); paint.SetPropertyBlock(properties);
            }
            for(var i=0;i<embers.Length;i++)
            {
                embers[i].gameObject.SetActive(!sequence.ReducedMotion);
                var age=Mathf.Repeat(sceneClock*.065f+i*.618034f,1);
                embers[i].localPosition=new Vector3(1.35f-age*.72f+Mathf.Sin(i*1.7f)*.3f,.55f+age*1.65f,2.8f-(i%3)*.1f);
                embers[i].localRotation=Quaternion.Euler(0,0,age*30+i*19);
                Opacity(emberPaint[i],Mathf.Sin(age*Mathf.PI)*.65f*(choosing ? .3f : 1));
            }
        }

        void PaintOpening(float t)
        {
            int beat = t < 4 ? 0 : t < DisclosureAt ? 1 : t < TitleAt ? 2 : 3;
            title.text = beat == 3 ? "S A N K O F A" : beat == 1 ? "THE GRIOT" : "";
            words.text = beat == 0 ? "Come.\nSit with me." : beat == 1 ? "I keep stories from many fires.\nTonight's comes from\nEnkare Nairobi, before the railway."
                : beat == 2 ? "The people in it are imagined.\nWhat is documented, I will mark."
                : "For the stories\nyou never got to hear.";
            context.text = beat == 1 ? "A keeper of stories. A voice between generations.\nInspired by West African griot traditions."
                : beat == 3 ? "Choose a water pot. Find yourself beside a river." : "";
            var alpha = Mathf.Min(Smooth((t - OpeningStarts[beat]) / .9f), Smooth((OpeningEnds[beat] - t) / .7f));
            title.alpha = words.alpha = context.alpha = alpha;
            var imageBlend = Smooth((t-11)/2.4f);
            Opacity(painting, imageBlend); Opacity(firePainting, Smooth(t/2)*(1-imageBlend));
            var drift = sequence.ReducedMotion ? 0 : Smooth(t / 32);
            painting.transform.localScale = paintingScale * (1 + drift * .035f);
            painting.transform.localPosition = paintingPosition + Vector3.right * (.045f * drift);
            firePainting.transform.localScale = paintingScale * (1 + drift*.025f);
            if (spokenBeat == beat) return;
            spokenBeat = beat; introVoice.Stop();
            if(beat==2) Cue("painted-page",paperTurn,.18f);
            // Exact local recordings only. New editorial titles are captioned, not mismatched to an older monologue.
            var line = beat < 3 ? PersonalStoryVoice.Welcome[beat] : null;
            if (line != null && app.NarrationEnabled)
            {
                introVoice.clip = Resources.Load<AudioClip>("Narration/personal-table/" + PersonalStoryVoice.ClipName(line));
                if (introVoice.clip != null) introVoice.Play();
            }
        }

        void SyncAudio(bool scoreActive)
        {
            fireBed.transform.position=world.TransformPoint(new Vector3(-.4f,.6f,1.2f));
            riverBed.transform.position=world.TransformPoint(new Vector3(1.6f,0,3));
            if (app.Session.Paused)
            {
                if (introVoice.isPlaying) { introVoice.Pause(); voicePaused = true; }
                music.Pause(); fireBed.Pause(); riverBed.Pause(); effects.Pause(); return;
            }
            if (voicePaused) { introVoice.UnPause(); voicePaused = false; }
            effects.UnPause();
            if (!scoreActive || !app.NarrationEnabled) { music.Stop(); fireBed.Stop(); riverBed.Stop(); return; }
            foreach(var bed in new[]{fireBed,riverBed}) if(bed.clip!=null && !bed.isPlaying) { bed.UnPause(); if(!bed.isPlaying) bed.Play(); }
            var crossing = sequence.Phase==RiverThresholdPhase.Crossing ? Smooth((float)sequence.Seconds/6) : 0;
            fireBed.volume=.11f*(1-crossing); riverBed.volume=.14f*crossing;
            if (music.clip != null && !music.isPlaying) music.UnPause();
            if (music.clip != null && !music.isPlaying) music.Play();
            var scoreWeight=sequence.Phase==RiverThresholdPhase.Crossing ? 1-Smooth(((float)sequence.Seconds-3)/3) : 1;
            music.volume = Mathf.MoveTowards(music.volume, (introVoice.isPlaying ? .14f : .38f)*scoreWeight, Time.deltaTime * .2f);
        }

        void Cue(string id, AudioClip clip, float volume)
        {
            if(clip==null || app.Session.Paused) return;
            RequestedSoundCues++; LastSoundCue=id;
            if(!app.NarrationEnabled) return;
            effects.transform.position=world.TransformPoint(new Vector3(0,.9f,1.2f));
            effects.PlayOneShot(clip,volume);
        }

        void Opacity(Renderer target, float value)
        {
            target.GetPropertyBlock(properties); properties.SetFloat("_Opacity", value); target.SetPropertyBlock(properties);
        }
        static float Smooth(float t) => Mathf.SmoothStep(0, 1, Mathf.Clamp01(t));
        void OnDestroy()
        {
            if (placement != null) { placement.Recentered -= Recentered; placement.RoomPlacementRequested -= RoomPlacementRequested; }
            if (app != null) { app.PreparedStoryStarting -= BeforePreparedStory; app.KeptStoryReopened -= ReopenKeptStory; app.TablePresentationRequested -= ReturnToTable; app.StoryAmbiencePosition = null; app.HoldPresentation(false); }
            if (controls != null) controls.Pressed -= Press;
        }
    }
}
