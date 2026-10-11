Shader "Sankofa/Illustration"
{
    Properties
    {
        _MainTex ("Illustration atlas", 2D) = "white" {}
        _FrameA ("Previous pose UV rect", Vector) = (0, 0, 1, 1)
        _FrameB ("Pose UV rect", Vector) = (0, 0, 1, 1)
        _PoseBlend ("Pose dissolve", Range(0, 1)) = 1
        _Visibility ("Story visibility", Range(0, 1)) = 1
        _Unfold ("Paint reveal", Range(0, 1)) = 1
        _Highlight ("Attention invitation", Range(0, 1)) = 0
        _Selection ("Accepted hand response", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Cull Off
        ZWrite On
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "SankofaGrade.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _FrameA, _FrameB;
            float _PoseBlend, _Visibility, _Unfold, _Highlight, _Selection;
            float4 _SankofaLightPos, _SankofaLightColor;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 position : SV_POSITION; float2 uv : TEXCOORD0; float3 world : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.position = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = v.uv;
                return o;
            }
            float2 AtlasUV(float2 uv, float4 frame)
            {
                float2 inset = _MainTex_TexelSize.xy * 1.5;
                return frame.xy + inset + uv * (frame.zw - inset * 2);
            }
            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                fixed4 a = tex2D(_MainTex, AtlasUV(i.uv, _FrameA));
                fixed4 b = tex2D(_MainTex, AtlasUV(i.uv, _FrameB));
                half alpha = lerp(a.a, b.a, _PoseBlend);
                clip(alpha - 0.22);
                // Stable object-space grain works identically for both eyes and writes depth; no transparent sorting.
                float grain = frac(sin(dot(floor(i.uv * 280), float2(12.9898, 78.233))) * 43758.5453);
                clip(_Visibility - grain * 0.98 - 0.001);
                clip(_Unfold * 1.15 - i.uv.y - grain * 0.12);
                half3 paint = lerp(a.rgb * a.a, b.rgb * b.a, _PoseBlend) / max(alpha, 0.001);
                float3 toward = _SankofaLightPos.xyz - i.world;
                half attenuation = 1 / (1 + dot(toward, toward) * _SankofaLightPos.w);
                half3 light = half3(0.82, 0.86, 0.94) + _SankofaLightColor.rgb * attenuation * 0.1;
                half response = _Highlight * 0.15 + _Selection * 0.12;
                half3 color = paint * (light + half3(1, 0.68, 0.28) * response);
                return fixed4(SankofaDither(SankofaGrade(color), i.position.xy), 1);
            }
            ENDCG
        }
    }
}
