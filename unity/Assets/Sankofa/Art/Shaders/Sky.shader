// Sky on the inside of a large sphere: zenith to horizon, the soft band of the Milky Way, a glow low on one part of
// the horizon (the last of the sun at dusk, a city's sodium haze), an overcast layer for a grey morning, and the
// moon where the scene has one. Drawn first, behind everything; distant hills fog toward the horizon colour.
Shader "Sankofa/Sky"
{
    Properties
    {
        _Zenith ("Zenith", Color) = (0.004, 0.006, 0.018, 1)
        _Horizon ("Horizon", Color) = (0.03, 0.035, 0.06, 1)
        _Band ("Milky Way", Color) = (0.04, 0.04, 0.052, 1)
        _BandAxis ("Milky Way axis", Vector) = (0.35, 0.55, 0.76, 0)
        _Noise ("Noise", 2D) = "gray" {}
        _GlowColor ("Horizon glow", Color) = (0, 0, 0, 1)
        _GlowDir ("Horizon glow direction", Vector) = (0, 0, 1, 0)
        _GlowWidth ("Horizon glow width", Float) = 0.6
        _GlowHeight ("Horizon glow height", Float) = 0.25
        _Overcast ("Overcast", Range(0, 1)) = 0
        _CloudColor ("Cloud", Color) = (0.3, 0.31, 0.33, 1)
        _ShowMoon ("Show the moon", Float) = 1
    }
    SubShader
    {
        // After the opaque scene, at the far plane: only the pixels nothing else covered are shaded.
        Tags { "Queue"="Geometry+500" "RenderType"="Background" "IgnoreProjector"="True" }
        Cull Front
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "SankofaGrade.cginc"

            fixed4 _Zenith;
            fixed4 _Horizon;
            fixed4 _Band;
            float4 _BandAxis;
            sampler2D _Noise;
            fixed4 _GlowColor;
            float4 _GlowDir;
            half _GlowWidth;
            half _GlowHeight;
            half _Overcast;
            fixed4 _CloudColor;
            half _ShowMoon;
            float4 _SankofaMoonDir;
            float4 _SankofaMoonColor;

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 world : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                // Drawn at the far plane whatever the sphere's size, so it never needs a deeper depth range.
            #if defined(UNITY_REVERSED_Z)
                o.pos.z = 1e-6 * o.pos.w;
            #else
                o.pos.z = o.pos.w * (1 - 1e-6);
            #endif
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 dir = normalize(i.world - _WorldSpaceCameraPos);
                half h = dir.y;
                half3 col = lerp(_Horizon.rgb, _Zenith.rgb, pow(saturate(h), 0.5));
                // The Milky Way: a soft great-circle band broken up by noise.
                half across = dot(dir, normalize(_BandAxis.xyz));
                half band = exp(-across * across / 0.02);
                float2 suv = float2(atan2(dir.x, dir.z) * 0.159, h);
                half clouds = tex2D(_Noise, suv * float2(5, 3)).r * tex2D(_Noise, suv * float2(14, 9)).g * tex2D(_Noise, suv * float2(31, 19)).b * 6;
                clouds = clouds * clouds;
                col += _Band.rgb * band * clouds * smoothstep(0.02, 0.3, h) * (1 - _Overcast);
                // A glow low on the horizon, strongest toward its direction and fading upward.
                float3 flat = normalize(float3(dir.x, 0, dir.z) + 1e-5);
                float3 glowFlat = normalize(float3(_GlowDir.x, 0, _GlowDir.z) + 1e-5);
                half toward = saturate(dot(flat, glowFlat) * 0.5 + 0.5);
                half glow = pow(toward, 1 / max(_GlowWidth, 0.05)) * exp(-max(h, 0) / max(_GlowHeight, 0.02));
                col += _GlowColor.rgb * glow;
                // Overcast: a soft, slowly varied cloud deck that hides the stars and the band.
                float2 cuv = dir.xz / max(h + 0.25, 0.05);
                half deck = tex2D(_Noise, cuv * 0.35 + _Time.x * 0.02).r * 0.6 + tex2D(_Noise, cuv * 1.1).g * 0.4;
                // Preserve broad cloud masses instead of averaging the noise into a uniform grey wash.
                half mass = smoothstep(0.28, 0.72, deck);
                half3 cloud = _CloudColor.rgb * (0.48 + 1.05 * mass) * lerp(1.05, 0.9, saturate(h));
                col = lerp(col, cloud, _Overcast * smoothstep(-0.05, 0.12, h));
                // The moon: a small bright disc in a wide, faint halo, only when the scene has one.
                half moonLight = dot(_SankofaMoonColor.rgb, half3(0.33, 0.33, 0.33));
                if (moonLight > 0 && _ShowMoon > 0.5)
                {
                    // Full precision: near the moon the angle changes in the fourth decimal place.
                    float facing = dot(dir, normalize(_SankofaMoonDir.xyz));
                    float disc = smoothstep(0.99985, 0.99992, facing);
                    float halo = pow(saturate(facing), 220.0) * 0.6 + pow(saturate(facing), 18.0) * 0.08;
                    col += half3(0.75, 0.8, 0.9) * disc * 1.6 + _SankofaMoonColor.rgb * halo * 6;
                }
                return fixed4(SankofaDither(SankofaGrade(col), i.pos.xy), 1);
            }
            ENDCG
        }
    }
}
