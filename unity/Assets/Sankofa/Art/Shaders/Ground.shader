// The hearth's ground: two scanned surfaces projected from above, dry earth and burned ground, blended by a
// per-vertex burn amount (UV.x) around the fire, with a large-scale procedural variation that hides tiling.
// Lit like Fire Lit: the fire and the moon with their baked visibility (UV2), occlusion and tint in the vertex
// colour, fog, low mist and the shared grade. The burned layer is only sampled inside the ash ring.
Shader "Sankofa/Ground"
{
    Properties
    {
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _Albedo ("Dry albedo", 2D) = "white" {}
        _Normal ("Dry normal (OpenGL)", 2D) = "bump" {}
        _Scale ("Dry scale (repeats per metre)", Float) = 0.5
        _Albedo2 ("Burned albedo", 2D) = "white" {}
        _Normal2 ("Burned normal (OpenGL)", 2D) = "bump" {}
        _Scale2 ("Burned scale (repeats per metre)", Float) = 0.7
        _NormalStrength ("Normal strength", Range(0, 2)) = 1
        _Macro ("Large-scale variation", 2D) = "gray" {}
        _MacroScale ("Variation scale", Float) = 0.07
        _MacroAmount ("Variation amount", Range(0, 1)) = 0.35
        _Wrap ("Light wrap", Range(0, 1)) = 0.2
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

            fixed4 _Tint;
            sampler2D _Albedo;
            sampler2D _Normal;
            float _Scale;
            sampler2D _Albedo2;
            sampler2D _Normal2;
            float _Scale2;
            half _NormalStrength;
            sampler2D _Macro;
            float _MacroScale;
            half _MacroAmount;
            half _Wrap;

            float4 _SankofaLightPos;
            float4 _SankofaLightColor;
            float4 _SankofaAmbientSky;
            float4 _SankofaAmbientGround;
            float4 _SankofaFogColor;
            float4 _SankofaFogParams;
            float4 _SankofaMoonDir;
            float4 _SankofaMoonColor;
            float4 _SankofaMist;
            float4 _SankofaMistColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float2 bake : TEXCOORD1;
                fixed4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 world : TEXCOORD0;
                half3 normal : TEXCOORD1;
                half burn : TEXCOORD2;
                half2 visibility : TEXCOORD3;
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
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.burn = v.uv.x;
                o.visibility = v.bake;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 p = i.world.xz;
                half3 albedo = tex2D(_Albedo, p * _Scale).rgb;
                half3 tangentNormal = UnpackNormal(tex2D(_Normal, p * _Scale));
                if (i.burn > 0.01)
                {
                    albedo = lerp(albedo, tex2D(_Albedo2, p * _Scale2).rgb, i.burn);
                    tangentNormal = normalize(lerp(tangentNormal, UnpackNormal(tex2D(_Normal2, p * _Scale2)), i.burn));
                }
                // Projected from above: the map's x runs along world X, its y along world Z.
                half3 n = normalize(normalize(i.normal) * tangentNormal.z + half3(tangentNormal.x, 0, tangentNormal.y) * _NormalStrength);
                half macro = tex2D(_Macro, p * _MacroScale).b;
                albedo *= _Tint.rgb * i.color.rgb * lerp(1, 0.6 + 0.8 * macro, _MacroAmount);

                float3 toLight = _SankofaLightPos.xyz - i.world;
                float d2 = dot(toLight, toLight);
                half ndl = saturate((dot(n, toLight * rsqrt(max(d2, 1e-4))) + _Wrap) / (1 + _Wrap));
                half attenuation = 1 / (1 + d2 * _SankofaLightPos.w);
                half moon = saturate(dot(n, normalize(_SankofaMoonDir.xyz + 1e-5))) * i.visibility.y;
                half3 ambient = lerp(_SankofaAmbientGround.rgb, _SankofaAmbientSky.rgb, n.y * 0.5 + 0.5) * i.color.a;
                half3 lit = albedo * (ambient + _SankofaLightColor.rgb * ndl * attenuation * i.visibility.x + _SankofaMoonColor.rgb * moon * i.color.a);

                float distance = length(i.world - _WorldSpaceCameraPos);
                lit = lerp(lit, _SankofaFogColor.rgb, saturate((distance - _SankofaFogParams.x) * _SankofaFogParams.y));
                half mist = _SankofaMist.x * saturate((distance - _SankofaMist.w) * 0.06) * exp(-max(i.world.y - _SankofaMist.y, 0) * _SankofaMist.z);
                lit = lerp(lit, _SankofaMistColor.rgb, saturate(mist));
                return fixed4(SankofaDither(SankofaGrade(lit), i.pos.xy), 1);
            }
            ENDCG
        }
    }
}
