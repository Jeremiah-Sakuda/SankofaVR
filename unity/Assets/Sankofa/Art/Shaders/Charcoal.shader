// Burning wood and coals: charred, firelit surfaces whose cracks glow and slowly pulse. UV2.y masks where a
// surface can glow (the ends inside the fire); _SankofaEmberGlow, set by HearthFire, lets the embers breathe
// with the flames. Uses the same baked occlusion and fire visibility as Fire Lit.
Shader "Sankofa/Charcoal"
{
    Properties
    {
        _Color ("Char", Color) = (0.07, 0.055, 0.045, 1)
        _Detail ("Detail (normal xy, height)", 2D) = "gray" {}
        _DetailScale ("Detail scale", Vector) = (2, 3, 0, 0)
        _DetailStrength ("Detail normal strength", Range(0, 2)) = 0.9
        _ScanNormal ("Scanned bark normal (OpenGL)", 2D) = "bump" {}
        _ScanScale ("Scan scale", Vector) = (1, 1, 0, 0)
        _ScanNormalStrength ("Scan normal strength", Range(0, 2)) = 0
        _EmberColor ("Ember colour", Color) = (1, 0.32, 0.06, 1)
        _CrackThreshold ("Crack threshold", Range(0, 1)) = 0.58
        _Glow ("Glow", Float) = 1
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

            fixed4 _Color;
            sampler2D _Detail;
            float4 _DetailScale;
            half _DetailStrength;
            sampler2D _ScanNormal;
            float4 _ScanScale;
            half _ScanNormalStrength;
            fixed4 _EmberColor;
            half _CrackThreshold;
            half _Glow;
            half _SankofaEmberGlow;

            float4 _SankofaLightPos;
            float4 _SankofaLightColor;
            float4 _SankofaAmbientSky;
            float4 _SankofaAmbientGround;
            float4 _SankofaMoonDir;
            float4 _SankofaMoonColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
                float2 bake : TEXCOORD1;
                fixed4 color : COLOR;
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
                half2 bake : TEXCOORD5;
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
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.tangent = UnityObjectToWorldDir(v.tangent.xyz);
                o.bitangent = cross(o.normal, o.tangent) * v.tangent.w * unity_WorldTransformParams.w;
                o.bake = v.bake;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half4 d = tex2D(_Detail, i.uv * _DetailScale.xy);
                half3 sn = UnpackNormal(tex2D(_ScanNormal, i.uv * _ScanScale.xy));
                half3 bump = half3(d.r * 2 - 1, d.g * 2 - 1, 0) * _DetailStrength + half3(sn.x, sn.y, 0) * _ScanNormalStrength;
                half3 n = normalize(normalize(i.normal) + normalize(i.tangent) * bump.x + normalize(i.bitangent) * bump.y);

                float3 toLight = _SankofaLightPos.xyz - i.world;
                float d2 = dot(toLight, toLight);
                half ndl = saturate(dot(n, toLight * rsqrt(max(d2, 1e-4))) * 0.75 + 0.25);
                half attenuation = 1 / (1 + d2 * _SankofaLightPos.w);
                half3 ambient = lerp(_SankofaAmbientGround.rgb, _SankofaAmbientSky.rgb, n.y * 0.5 + 0.5) * i.color.a;
                half3 albedo = _Color.rgb * (0.7 + 0.6 * d.b);
                half moon = saturate(dot(n, normalize(_SankofaMoonDir.xyz + 1e-5))) * i.color.a;
                half3 lit = albedo * (ambient + _SankofaLightColor.rgb * ndl * attenuation * i.bake.x + _SankofaMoonColor.rgb * moon);

                // Valleys between the char crack open and glow; a slow second sample makes the glow wander.
                half crack = smoothstep(_CrackThreshold, _CrackThreshold + 0.1, 1 - d.b);
                half pulse = tex2D(_Detail, i.uv * _DetailScale.xy * 0.31 + float2(_Time.y * 0.021, -_Time.y * 0.043)).b;
                half glow = i.bake.y * _Glow * _SankofaEmberGlow * (0.12 + crack * 1.7) * (0.45 + 1.1 * pulse);
                return fixed4(SankofaGrade(lit + _EmberColor.rgb * glow), 1);
            }
            ENDCG
        }
    }
}
