// Soft additive glow for embers, the lantern and spark particles. Multiplies vertex colour for particles.
Shader "Sankofa/Glow"
{
    Properties
    {
        _Color ("Colour", Color) = (1, 0.6, 0.25, 1)
        _Intensity ("Intensity", Float) = 1
        _Falloff ("Falloff", Float) = 2.2
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
            half _Intensity;
            half _Falloff;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
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
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half r = length(i.uv - 0.5) * 2;
                half glow = pow(saturate(1 - r), _Falloff);
                return fixed4(SankofaGrade(_Color.rgb * i.color.rgb * i.color.a * glow * _Intensity), 1);
            }
            ENDCG
        }
    }
}
