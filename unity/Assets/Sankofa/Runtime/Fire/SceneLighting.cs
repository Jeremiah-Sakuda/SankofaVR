using UnityEngine;

namespace Sankofa.Runtime.Fire
{
    /// <summary>
    /// Global uniforms for the Sankofa/Fire Lit shader. One warm light at a time (the hearth fire or the lantern),
    /// hemisphere ambient and distance fog. Cheaper on Quest than Unity lights and fully directed by the story.
    /// </summary>
    public static class SceneLighting
    {
        static readonly int LightPos = Shader.PropertyToID("_SankofaLightPos");
        static readonly int LightColor = Shader.PropertyToID("_SankofaLightColor");
        static readonly int AmbientSky = Shader.PropertyToID("_SankofaAmbientSky");
        static readonly int AmbientGround = Shader.PropertyToID("_SankofaAmbientGround");
        static readonly int FogColor = Shader.PropertyToID("_SankofaFogColor");
        static readonly int FogParams = Shader.PropertyToID("_SankofaFogParams");
        static readonly int MoonDir = Shader.PropertyToID("_SankofaMoonDir");
        static readonly int MoonColor = Shader.PropertyToID("_SankofaMoonColor");
        static readonly int Mist = Shader.PropertyToID("_SankofaMist");
        static readonly int MistColor = Shader.PropertyToID("_SankofaMistColor");
        static readonly int Grade = Shader.PropertyToID("_SankofaGrade");

        /// <summary>Attenuation: brightness falls to half at sqrt(1 / attenuation) metres.</summary>
        public static void SetLight(Vector3 position, Color color, float intensity, float attenuation)
        {
            Shader.SetGlobalVector(LightPos, new Vector4(position.x, position.y, position.z, attenuation));
            Shader.SetGlobalVector(LightColor, (Vector4)(color * intensity));
        }

        public static void SetAmbient(Color sky, Color ground)
        {
            Shader.SetGlobalVector(AmbientSky, (Vector4)sky);
            Shader.SetGlobalVector(AmbientGround, (Vector4)ground);
        }

        /// <summary>Faint cool moonlight from a direction; black for a scene without a moon.</summary>
        public static void SetMoon(Vector3 towardMoon, Color color)
        {
            Shader.SetGlobalVector(MoonDir, (Vector4)towardMoon.normalized);
            Shader.SetGlobalVector(MoonColor, (Vector4)color);
        }

        /// <summary>Mist lying low over distant ground: amount, base height, height falloff, start distance.</summary>
        public static void SetMist(Color color, float amount, float baseHeight, float falloff, float startDistance)
        {
            Shader.SetGlobalVector(MistColor, (Vector4)color);
            Shader.SetGlobalVector(Mist, new Vector4(amount, baseHeight, falloff, startDistance));
        }

        /// <summary>The shared look (see SankofaGrade.cginc): exposure, saturation, shadow coolness, highlight warmth.</summary>
        public static void SetGrade(float exposure, float saturation, float coolShadows, float warmHighlights) =>
            Shader.SetGlobalVector(Grade, new Vector4(exposure, saturation, coolShadows, warmHighlights));

        public static void SetFog(Color color, float start, float end)
        {
            Shader.SetGlobalVector(FogColor, (Vector4)color);
            Shader.SetGlobalVector(FogParams, new Vector4(start, 1f / Mathf.Max(0.01f, end - start), 0, 0));
        }
    }
}
