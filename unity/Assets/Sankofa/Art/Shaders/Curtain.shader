// The light the visitor passes through between the hearth and a story: drawn on the inside of a small sphere
// around the head, over the world but under the hands, so hands stay visible during transitions. With motion, it is
// a burn: flame spreads across the view outward from where the light is now (Source, Cover), then the new place
// opens outward from its own light (Target, Hole), each front ragged and glowing. Without motion it is a plain fade
// (Alpha).
Shader "Sankofa/Curtain"
{
    Properties
    {
        _Color ("Colour", Color) = (1, 0.62, 0.3, 1)
        _Alpha ("Alpha", Range(0, 1)) = 0
        _Noise ("Noise", 2D) = "gray" {}
        _Motion ("Motion", Range(0, 1)) = 1
        _Source ("Toward the light it burns from (world)", Vector) = (0, 0, 1, 0)
        _Target ("Toward the light it opens from (world)", Vector) = (0, 0, 1, 0)
        _Cover ("Burnt across, 0 to 1.2", Float) = 0
        _Hole ("Opened, 0 to 1.2", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Front
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            fixed4 _Color;
            half _Alpha;
            half _Motion;
            sampler2D _Noise;
            float4 _Source;
            float4 _Target;
            half _Cover;
            half _Hole;

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
                float3 dir : TEXCOORD1;
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
                o.dir = mul((float3x3)unity_ObjectToWorld, v.vertex.xyz);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half n = tex2D(_Noise, float2(i.uv.x * 3, i.uv.y * 1.5 - _Time.y * 0.25 * _Motion)).r;
                half shimmer = lerp(1, 0.8 + 0.4 * n, _Motion);
                if (_Motion < 0.5) return fixed4(_Color.rgb * shimmer, saturate(_Alpha));
                // Angle from each light, 0 toward it and 1 directly away, made ragged by rising flame tongues.
                float3 d = normalize(i.dir);
                half tongues = tex2D(_Noise, float2(i.uv.x * 7, i.uv.y * 3 - _Time.y * 0.6)).g - 0.5;
                half fromSource = acos(clamp(dot(d, normalize(_Source.xyz)), -1, 1)) / UNITY_PI + tongues * 0.14 - d.y * 0.06;
                half fromTarget = acos(clamp(dot(d, normalize(_Target.xyz)), -1, 1)) / UNITY_PI + tongues * 0.14;
                half burnt = saturate((_Cover - fromSource) / 0.05);
                half open = saturate((_Hole - fromTarget) / 0.05);
                half cover = burnt * (1 - open);
                // The fronts glow white-hot where flame meets the world; behind them the light settles to a deep,
                // moving ember rather than a flat bright field.
                half edge = saturate(burnt * (1 - burnt) * 4 * step(_Cover, 1.15)) + saturate(open * (1 - open) * 4) * step(0.001, _Hole);
                half behind = saturate((_Cover - fromSource) / 0.35) * saturate((fromTarget - _Hole) / 0.35 + step(_Hole, 0.001));
                half flicker = tex2D(_Noise, float2(i.uv.x * 4 + _Time.y * 0.05, i.uv.y * 2 - _Time.y * 0.3)).b;
                // Rising tongues of brighter flame through a dark ember (linear colour: these are deep values).
                half tongue = smoothstep(0.55, 0.9, tex2D(_Noise, float2(i.uv.x * 9, i.uv.y * 2.5 - _Time.y * 0.45)).r);
                half3 deep = half3(0.07, 0.02, 0.006) * (0.6 + 0.8 * flicker) + half3(0.5, 0.18, 0.05) * tongue * 0.5;
                half3 colour = lerp(_Color.rgb * shimmer, deep, behind) + edge * half3(1, 0.78, 0.45) * 1.2;
                // Behind the fronts the world stays faintly visible, a steady reference while the place changes.
                return fixed4(colour, saturate(cover * lerp(1, 0.86, behind) + edge * 0.8));
            }
            ENDCG
        }
    }
}
