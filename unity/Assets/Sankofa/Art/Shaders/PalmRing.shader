// Where to hold an open hand for something offered: a soft ring of firelight lying under the object, its arc
// filling as the palm holds. Drawn on a quad facing up; Fill is 0..1, Alpha fades it in and out.
Shader "Sankofa/Palm Ring"
{
    Properties
    {
        _Color ("Colour", Color) = (1, 0.66, 0.34, 1)
        _Fill ("Fill", Range(0, 1)) = 0
        _Alpha ("Alpha", Range(0, 1)) = 0
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

            fixed4 _Color;
            half _Fill;
            half _Alpha;

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
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv * 2 - 1;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float r = length(i.uv);
                // A faint full ring, and over it the arc held so far, brighter.
                half ring = smoothstep(0.12, 0.0, abs(r - 0.78));
                float around = frac(atan2(i.uv.x, i.uv.y) / (2 * UNITY_PI) + 1);
                half held = step(around, _Fill) * ring;
                half centre = smoothstep(0.7, 0.0, r) * 0.18;
                half light = (ring * 0.35 + held * 1.1 + centre) * _Alpha;
                return fixed4(SankofaGrade(_Color.rgb * light), 1);
            }
            ENDCG
        }
    }
}
