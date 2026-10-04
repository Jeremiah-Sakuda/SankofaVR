// Thin smoke above the fire: soft particles lit warm from below by the flames, cooling to the night ambient
// as they rise. Kept sparse because transparency is expensive on Quest.
Shader "Sankofa/Smoke"
{
    Properties
    {
        _Noise ("Noise", 2D) = "gray" {}
        _Density ("Density", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
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
            half _Density;
            float4 _SankofaLightPos;
            float4 _SankofaLightColor;
            float4 _SankofaAmbientSky;

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
                half3 light : TEXCOORD1;
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
                float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
                half above = saturate((world.y - _SankofaLightPos.y) / 1.2);
                o.light = lerp(_SankofaLightColor.rgb * 0.16, _SankofaAmbientSky.rgb * 0.7, above);
                o.color.a *= 1 - above * 0.85;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half r = length(i.uv - 0.5) * 2;
                half shape = saturate(1 - r);
                half wisps = tex2D(_Noise, i.uv * 0.7 + i.color.rg * 3.1).r;
                half alpha = shape * shape * (0.4 + wisps) * _Density * i.color.a;
                return fixed4(SankofaGrade(i.light), alpha);
            }
            ENDCG
        }
    }
}
