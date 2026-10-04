// The light the visitor passes through between the hearth and a story: drawn on the inside of a small sphere
// around the head, over the world but under the hands, so hands stay visible during transitions.
Shader "Sankofa/Curtain"
{
    Properties
    {
        _Color ("Colour", Color) = (1, 0.62, 0.3, 1)
        _Alpha ("Alpha", Range(0, 1)) = 0
        _Noise ("Noise", 2D) = "gray" {}
        _Motion ("Motion", Range(0, 1)) = 1
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
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half n = tex2D(_Noise, float2(i.uv.x * 3, i.uv.y * 1.5 - _Time.y * 0.25 * _Motion)).r;
                half shimmer = lerp(1, 0.8 + 0.4 * n, _Motion);
                return fixed4(_Color.rgb * shimmer, saturate(_Alpha * lerp(1, 0.9 + 0.1 * n, _Motion)));
            }
            ENDCG
        }
    }
}
