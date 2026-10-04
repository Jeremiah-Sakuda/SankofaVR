// People and animals of a story, shown as the griot shows them: shapes of firelight, brightest at their edges,
// with embers drifting through them. Additive and unlit by the scene, so no face or likeness is ever drawn.
// Intensity fades them in and out; Highlight brightens one when the story turns to them.
Shader "Sankofa/Ember Figure"
{
    Properties
    {
        _Noise ("Noise", 2D) = "gray" {}
        _Color ("Edge colour", Color) = (1, 0.55, 0.22, 1)
        _Core ("Core colour", Color) = (0.35, 0.1, 0.03, 1)
        _Intensity ("Intensity", Float) = 1
        _Rim ("Edge sharpness", Float) = 2.2
        _Drift ("Ember drift", Float) = 0.25
        _Highlight ("Highlight", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One One
        ZWrite Off
        Cull Back
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "SankofaGrade.cginc"

            sampler2D _Noise;
            fixed4 _Color;
            fixed4 _Core;
            half _Intensity;
            half _Rim;
            half _Drift;
            half _Highlight;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 world : TEXCOORD0;
                half3 normal : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 view = normalize(_WorldSpaceCameraPos - i.world);
                half edge = pow(1 - saturate(abs(dot(normalize(i.normal), view))), _Rim);
                // Embers rise through the figure; a slow flicker keeps it alive while it stands still.
                float2 p = i.world.xz * 6 + i.world.y * 3;
                half embers = tex2D(_Noise, float2(p.x * 0.11, i.world.y * 1.7 - _Time.y * _Drift)).b;
                embers = smoothstep(0.62, 0.95, embers);
                half flicker = 0.85 + 0.15 * sin(_Time.y * 3.1 + i.world.y * 4);
                half3 c = _Core.rgb * 0.35 + _Color.rgb * (edge * 1.6 + embers * 0.9);
                c *= _Intensity * flicker * (1 + _Highlight * 0.8);
                return fixed4(SankofaGrade(c), 1);
            }
            ENDCG
        }
    }
}
