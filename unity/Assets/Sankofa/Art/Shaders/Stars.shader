// Stars as tiny camera-facing quads that twinkle gently. Corners arrive in UV, size and phase in UV2,
// tint and brightness in vertex colour (see SankofaMeshes.Stars).
Shader "Sankofa/Stars"
{
    Properties
    {
        _Twinkle ("Twinkle", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "Queue"="Background+1" "RenderType"="Background" "IgnoreProjector"="True" }
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

            half _Twinkle;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 corner : TEXCOORD0;
                float2 data : TEXCOORD1;
                fixed4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 corner : TEXCOORD0;
                fixed4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 view = UnityObjectToViewPos(v.vertex);
                view.xy += v.corner * v.data.x;
                o.pos = mul(UNITY_MATRIX_P, float4(view, 1));
                o.corner = v.corner;
                half twinkle = 1 - _Twinkle * (0.5 + 0.5 * sin(_Time.y * (1.3 + frac(v.data.y) * 2.1) + v.data.y));
                o.color = v.color * twinkle;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half r = length(i.corner);
                half glow = saturate(1 - r);
                glow *= glow;
                return fixed4(SankofaGrade(i.color.rgb * glow), 1);
            }
            ENDCG
        }
    }
}
