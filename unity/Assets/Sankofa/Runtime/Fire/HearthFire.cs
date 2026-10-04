using Sankofa.Core.Story;
using UnityEngine;

namespace Sankofa.Runtime.Fire
{
    /// <summary>
    /// Directed: the fire's look follows the story's cues (welcoming, listening, holding...) and breathes with speech.
    /// Simple: one steady look with identical narration, the baseline for comparing whether direction adds presence.
    /// </summary>
    public enum FireMode { Directed, Simple }

    /// <summary>How the fire looks. States differ in height, motion and lean, not colour alone.</summary>
    public struct FireLook
    {
        public float Height, Width, Intensity, Warmth, Speed, Turbulence, Lean, Sparks, Light, Speech;

        public FireLook(float height, float width, float intensity, float warmth, float speed, float turbulence, float lean, float sparks, float light, float speech)
        {
            Height = height;
            Width = width;
            Intensity = intensity;
            Warmth = warmth;
            Speed = speed;
            Turbulence = turbulence;
            Lean = lean;
            Sparks = sparks;
            Light = light;
            Speech = speech;
        }

        public static FireLook Lerp(FireLook a, FireLook b, float t) => new FireLook(
            Mathf.Lerp(a.Height, b.Height, t), Mathf.Lerp(a.Width, b.Width, t), Mathf.Lerp(a.Intensity, b.Intensity, t),
            Mathf.Lerp(a.Warmth, b.Warmth, t), Mathf.Lerp(a.Speed, b.Speed, t), Mathf.Lerp(a.Turbulence, b.Turbulence, t),
            Mathf.Lerp(a.Lean, b.Lean, t), Mathf.Lerp(a.Sparks, b.Sparks, t), Mathf.Lerp(a.Light, b.Light, t), Mathf.Lerp(a.Speech, b.Speech, t));

        /// <summary>First-pass direction for each state. Tune on the headset with sound; these are not accepted values.</summary>
        public static FireLook For(FireState state)
        {
            switch (state)
            {
                case FireState.Resting: return new FireLook(0.45f, 0.42f, 0.55f, 0.25f, 0.6f, 0.3f, 0f, 3, 0.55f, 0f);
                case FireState.Welcoming: return new FireLook(0.95f, 0.56f, 1.1f, 0.7f, 1.0f, 0.5f, 0.06f, 14, 1.15f, 0.6f);
                // Listening leans toward the visitor and steadies, as if attending.
                case FireState.Listening: return new FireLook(0.72f, 0.5f, 0.85f, 0.55f, 0.7f, 0.2f, 0.14f, 5, 0.85f, 0f);
                case FireState.Gathering: return new FireLook(1.0f, 0.6f, 1.05f, 0.5f, 1.3f, 0.8f, 0f, 40, 1.0f, 0.3f);
                case FireState.Holding: return new FireLook(0.62f, 0.48f, 0.8f, 0.45f, 0.45f, 0.15f, 0.08f, 2, 0.8f, 0f);
                case FireState.Transitioning: return new FireLook(2.6f, 1.4f, 2.2f, 0.85f, 1.6f, 0.9f, 0f, 80, 2.2f, 0f);
                default: return Telling;
            }
        }

        public static readonly FireLook Telling = new FireLook(0.88f, 0.55f, 1.0f, 0.6f, 1.0f, 0.5f, 0.04f, 10, 1.0f, 1.0f);
        public static readonly FireLook Simple = new FireLook(0.85f, 0.55f, 1.0f, 0.55f, 1.0f, 0.5f, 0f, 10, 1.0f, 0f);
    }

    public sealed class HearthFire : MonoBehaviour
    {
        [SerializeField] Transform[] layers;
        [SerializeField] Renderer[] cards;
        [Tooltip("Width and height of each flame layer relative to the fire's current size.")]
        [SerializeField] Vector2[] layerScale;
        [SerializeField] ParticleSystem sparks;
        [SerializeField] ParticleSystem smoke;
        [SerializeField] Renderer halo;
        [SerializeField] FireCrackle crackle;
        [SerializeField] Color lightColor = new Color(1f, 0.58f, 0.26f);
        [SerializeField] float lightAttenuation = 1.15f;
        [SerializeField] float lightPower = 1.7f;
        [SerializeField] float responseSeconds = 0.55f;

        static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int WarmthId = Shader.PropertyToID("_Warmth");
        static readonly int SpeedId = Shader.PropertyToID("_Speed");
        static readonly int TurbulenceId = Shader.PropertyToID("_Turbulence");
        static readonly int SeedId = Shader.PropertyToID("_Seed");
        static readonly int LeanId = Shader.PropertyToID("_Lean");
        static readonly int GlowIntensityId = Shader.PropertyToID("_Intensity");
        static readonly int EmberGlowId = Shader.PropertyToID("_SankofaEmberGlow");

        MaterialPropertyBlock block;
        FireLook current = FireLook.For(FireState.Resting);
        float breath;

        public FireState State { get; private set; } = FireState.Resting;
        public FireMode Mode { get; set; } = FireMode.Directed;
        public bool ReducedMotion { get; set; }
        /// <summary>0..1 loudness of the narration, so the fire can breathe with the voice.</summary>
        public float SpeechLevel { get; set; }
        /// <summary>The hearth lights the scene only while the visitor is at the hearth.</summary>
        public bool LightsScene { get; set; } = true;
        public FireLook Current => current;

        public void SetState(FireState state) => State = state;

        void Awake() => block = new MaterialPropertyBlock();

        void OnEnable()
        {
            if (crackle != null) crackle.Popped += Burst;
        }

        void OnDisable()
        {
            if (crackle != null) crackle.Popped -= Burst;
        }

        /// <summary>A pop in the crackle throws a few sparks, so sound and sight agree.</summary>
        void Burst(float strength)
        {
            if (sparks == null || Mode == FireMode.Simple) return;
            sparks.Emit(Mathf.RoundToInt((ReducedMotion ? 1 : 3) + strength * (ReducedMotion ? 2 : 7)));
        }

        FireLook Goal()
        {
            if (Mode == FireMode.Simple) return FireLook.Simple;
            var goal = FireLook.For(State);
            if (ReducedMotion && State == FireState.Transitioning)
            {
                goal = FireLook.Telling;
                goal.Intensity = 1.3f;
                goal.Light = 1.3f;
            }
            return goal;
        }

        void Update()
        {
            var dt = Time.deltaTime;
            var goal = Goal();
            var response = State == FireState.Transitioning ? responseSeconds * 0.6f : responseSeconds;
            current = FireLook.Lerp(current, goal, 1f - Mathf.Exp(-dt / Mathf.Max(0.01f, response)));

            // Speech breathing: a quick rise and slower fall, so the flame swells on stressed syllables.
            var target = Mode == FireMode.Directed ? current.Speech * Mathf.Clamp01(SpeechLevel) : 0f;
            breath = Mathf.Lerp(breath, target, 1f - Mathf.Exp(-dt / (target > breath ? 0.06f : 0.25f)));

            var t = Time.time;
            var flicker = 1f + (Mathf.PerlinNoise(t * 6.3f, 0.37f) - 0.5f) * 0.22f * (0.4f + current.Turbulence);

            for (var i = 0; i < layers.Length; i++)
            {
                var scale = layerScale != null && i < layerScale.Length ? layerScale[i] : new Vector2(1f - 0.16f * i, 1f - 0.1f * i);
                // Each tongue rises and falls on its own.
                var lick = 1f + (Mathf.PerlinNoise(t * 1.9f, i * 3.7f) - 0.5f) * 0.35f * current.Turbulence;
                layers[i].localScale = new Vector3(current.Width * scale.x, current.Height * (1f + breath * 0.22f) * scale.y * lick, 1f);
                cards[i].GetPropertyBlock(block);
                block.SetFloat(IntensityId, current.Intensity * (1f + breath * 0.35f) * flicker * (i == 0 ? 1f : 0.72f));
                block.SetFloat(WarmthId, current.Warmth);
                block.SetFloat(SpeedId, current.Speed * (1f + 0.12f * i));
                block.SetFloat(TurbulenceId, current.Turbulence);
                block.SetFloat(SeedId, 0.31f * i + 0.17f);
                block.SetFloat(LeanId, current.Lean);
                cards[i].SetPropertyBlock(block);
            }

            if (sparks != null)
            {
                var emission = sparks.emission;
                emission.rateOverTime = ReducedMotion ? current.Sparks * 0.4f : current.Sparks;
            }
            if (smoke != null)
            {
                var emission = smoke.emission;
                emission.rateOverTime = 0.6f + current.Height * 0.9f;
            }
            if (halo != null)
            {
                halo.GetPropertyBlock(block);
                block.SetFloat(GlowIntensityId, current.Light * flicker * (0.32f + breath * 0.12f));
                halo.SetPropertyBlock(block);
            }
            Shader.SetGlobalFloat(EmberGlowId, Mathf.Clamp(current.Intensity * flicker * (1f + breath * 0.3f), 0.35f, 2.2f));
            if (crackle != null) crackle.Level = Mathf.Clamp01(current.Intensity * 0.75f);
            // The light's centre wanders a few centimetres, as a real fire's does, so shading moves.
            var wander = new Vector3(Mathf.PerlinNoise(t * 2.1f, 3.3f) - 0.5f, Mathf.PerlinNoise(t * 1.7f, 7.7f) - 0.5f, Mathf.PerlinNoise(t * 2.3f, 9.1f) - 0.5f) * 0.07f;
            if (LightsScene)
                SceneLighting.SetLight(transform.position + wander + Vector3.up * (0.25f + current.Height * 0.2f), lightColor,
                    current.Light * lightPower * flicker * (1f + breath * 0.25f), lightAttenuation);
        }
    }
}
