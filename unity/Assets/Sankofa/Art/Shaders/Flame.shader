// One flame layer: a camera-facing card whose teardrop shape is eroded by scrolling noise.
// HearthFire drives the parameters per fire state; several layers with different seeds make the fire.
Shader "Sankofa/Flame"
{
    Properties
    {
        _Noise ("Noise", 2D) = "gray" {}
        _Intensity ("Intensity", Float) = 1
        _Warmth ("Warmth", Range(0, 1)) = 0.5
        _Speed ("Speed", Float) = 1
        _Turbulence ("Turbulence", Range(0, 1)) = 0.5
        _Seed ("Seed", Float) = 0
        _Lean ("Lean toward the visitor", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One One
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "SankofaGrade.cginc"

            sampler2D _Noise;
            half _Intensity;
            half _Warmth;
            half _Speed;
            half _Turbulence;
            half _Seed;
            half _Lean;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float4 p = v.vertex;
                // The card faces the visitor along −Z; leaning moves the flame tip toward them.
                float h = v.uv.y;
                p.z -= _Lean * h * h;
                o.pos = UnityObjectToClipPos(p);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float t = _Time.y * _Speed;
                float2 uv = i.uv;
                half n1 = tex2D(_Noise, float2(uv.x * 0.8 + _Seed, uv.y * 0.55 - t * 0.85)).r;
                half n2 = tex2D(_Noise, float2(uv.x * 1.9 - _Seed * 1.3, uv.y * 1.25 - t * 1.7)).g;
                half n3 = tex2D(_Noise, float2(uv.x * 3.7 + _Seed * 0.7, uv.y * 2.6 - t * 2.9)).b;
                half n = n1 * 0.5 + n2 * 0.33 + n3 * 0.17;

                half h = uv.y;
                half x = uv.x + (n - 0.5) * (0.12 + 0.3 * _Turbulence) * h;
                half width = lerp(0.36, 0.035, pow(h, 0.75));
                half dx = abs(x - 0.5) / width;
                half shape = saturate(1 - dx * dx) * smoothstep(0.0, 0.07, h) * saturate(1.05 - h);
                half body = saturate(shape * 1.7 - (n * 0.85 + h * 0.65) * (0.55 + 0.35 * _Turbulence));

                half3 edge = lerp(half3(0.45, 0.06, 0.015), half3(0.85, 0.24, 0.04), _Warmth);
                half3 core = lerp(half3(1.0, 0.55, 0.2), half3(1.0, 0.82, 0.5), _Warmth);
                half3 colour = lerp(edge, core, saturate(body * 1.6)) * body;
                // White-hot heart low in the flame.
                half heart = saturate(1 - dx * 2.2) * smoothstep(0.03, 0.12, h) * saturate(1 - h * 2.4) * body;
                colour += half3(1.0, 0.9, 0.7) * heart * 0.6;
                // A faint blue base where the flame leaves the wood.
                colour += half3(0.05, 0.09, 0.3) * saturate(1 - dx) * smoothstep(0.0, 0.03, h) * saturate(1 - h * 9) * (0.6 + 0.4 * n2);
                return fixed4(SankofaGrade(colour * _Intensity), 1);
            }
            ENDCG
        }
    }
}
