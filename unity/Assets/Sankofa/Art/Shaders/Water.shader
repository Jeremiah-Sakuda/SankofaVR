// Moving water for the stream and the spill over the rock: opaque (no transparency cost on Quest), with two layers
// of broad scrolling ripples along the flow (UV.y runs downstream) that calm with distance, so they never alias into
// speckle; a smooth reflection of the sky's gradient, brightest toward the horizon; soft glints of the scene's one
// warm light and the moon; and the shared fog, mist and grade. Highlight answers a touch.
Shader "Sankofa/Water"
{
    Properties
    {
        _Noise ("Noise", 2D) = "gray" {}
        _Deep ("Deep colour", Color) = (0.02, 0.035, 0.03, 1)
        _Shallow ("Shallow colour", Color) = (0.08, 0.1, 0.08, 1)
        _Sky ("Reflected sky", Color) = (0.12, 0.14, 0.2, 1)
        _Flow ("Flow speed", Float) = 0.35
        _Scale ("Ripple scale", Float) = 2.2
        _Strength ("Ripple strength", Range(0, 1)) = 0.35
        _Gloss ("Glint sharpness", Float) = 60
        _Foam ("Foam", Range(0, 1)) = 0
        _Highlight ("Highlight (touch)", Range(0, 1)) = 0
        _HighlightColor ("Highlight colour", Color) = (0.55, 0.75, 0.9, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "SankofaGrade.cginc"

            sampler2D _Noise;
            fixed4 _Deep;
            fixed4 _Shallow;
            fixed4 _Sky;
            half _Flow;
            half _Scale;
            half _Strength;
            half _Gloss;
            half _Foam;
            half _Highlight;
            fixed4 _HighlightColor;

            float4 _SankofaLightPos;
            float4 _SankofaLightColor;
            float4 _SankofaFogColor;
            float4 _SankofaFogParams;
            float4 _SankofaMoonDir;
            float4 _SankofaMoonColor;
            float4 _SankofaMist;
            float4 _SankofaMistColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 world : TEXCOORD1;
                half3 normal : TEXCOORD2;
                half3 tangent : TEXCOORD3;
                half3 bitangent : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.tangent = UnityObjectToWorldDir(v.tangent.xyz);
                o.bitangent = cross(o.normal, o.tangent) * v.tangent.w;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float t = _Time.y * _Flow;
                float distance = length(i.world - _WorldSpaceCameraPos);
                // The broadest noise only (A and R), so a ripple is a swell rather than grain; calmer far away.
                float2 a = tex2D(_Noise, i.uv * _Scale * 0.5 + float2(0.13, -t * 0.5)).ar * 2 - 1;
                float2 b = tex2D(_Noise, i.uv * _Scale * 1.1 + float2(-t * 0.8, 0.37)).ar * 2 - 1;
                half calm = saturate(1.6 / max(distance, 0.5));
                half2 ripple = (a + b * 0.5) * _Strength * calm;
                half3 n = normalize(normalize(i.normal) + normalize(i.tangent) * ripple.x + normalize(i.bitangent) * ripple.y);

                float3 view = normalize(_WorldSpaceCameraPos - i.world);
                half fresnel = pow(1 - saturate(dot(n, view)), 3);
                half3 body = lerp(_Deep.rgb, _Shallow.rgb, saturate(0.5 + ripple.x * 2));
                // The sky as the water mirrors it: brighter toward the horizon, darker overhead.
                half3 reflected = reflect(-view, n);
                half3 sky = _Sky.rgb * lerp(1.7, 0.6, saturate(reflected.y * 1.6));
                half3 lit = lerp(body, sky, saturate(0.25 + fresnel * 0.9));

                float3 toLight = _SankofaLightPos.xyz - i.world;
                float d2 = dot(toLight, toLight);
                half3 l = toLight * rsqrt(max(d2, 1e-4));
                half attenuation = 1 / (1 + d2 * _SankofaLightPos.w);
                half glint = pow(saturate(dot(n, normalize(l + view))), _Gloss);
                half3 moonDir = normalize(_SankofaMoonDir.xyz + 1e-5);
                half moonGlint = pow(saturate(dot(n, normalize(moonDir + view))), _Gloss * 0.5);
                // Water scatters little light: a warm source shows as its glint, with only a trace on the surface.
                lit += _SankofaLightColor.rgb * (glint * 0.7 + 0.02) * attenuation + _SankofaMoonColor.rgb * moonGlint * 0.8;

                half foam = _Foam * smoothstep(0.55, 0.9, tex2D(_Noise, i.uv * _Scale * 3 + float2(0, -t * 2.5)).b);
                lit = lerp(lit, half3(0.5, 0.52, 0.5) * (0.3 + attenuation * _SankofaLightColor.rgb), foam);
                // Touched or invited, the water catches a little more light along its moving surface, not a flat tint.
                lit += _HighlightColor.rgb * _Highlight * (0.03 + 0.12 * saturate(0.5 + ripple.x * 3) * calm);

                lit = lerp(lit, _SankofaFogColor.rgb, saturate((distance - _SankofaFogParams.x) * _SankofaFogParams.y));
                half mist = _SankofaMist.x * saturate((distance - _SankofaMist.w) * 0.06) * exp(-max(i.world.y - _SankofaMist.y, 0) * _SankofaMist.z);
                lit = lerp(lit, _SankofaMistColor.rgb, saturate(mist));
                return fixed4(SankofaDither(SankofaGrade(lit), i.pos.xy), 1);
            }
            ENDCG
        }
    }
}
