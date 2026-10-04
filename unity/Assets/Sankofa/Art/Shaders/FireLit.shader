// Environment surfaces lit by the one warm light the story allows at a time (the hearth fire or the lantern),
// plus faint cool moonlight, all set globally by SceneLighting. Analytic and single-pass: no Unity lights or extra
// passes on Quest. Optional: baked tint and occlusion (vertex colour), fire and moon visibility (UV2) from
// SankofaBake, surface detail projected on the ground, triplanar or along UVs, wind for grass, and ground mist.
Shader "Sankofa/Fire Lit"
{
    Properties
    {
        _Color ("Albedo", Color) = (0.45, 0.38, 0.32, 1)
        _MainTex ("Albedo texture", 2D) = "white" {}
        _Emission ("Emission", Color) = (0, 0, 0, 0)
        _Highlight ("Highlight (illuminate action)", Range(0, 1)) = 0
        _HighlightColor ("Highlight colour", Color) = (1, 0.72, 0.38, 1)
        _Wrap ("Light wrap", Range(0, 1)) = 0.3
        _UseBake ("Use baked tint, occlusion and visibility", Float) = 0
        _Detail ("Detail (normal xy, height)", 2D) = "gray" {}
        _DetailScale ("Detail scale", Vector) = (1, 1, 0, 0)
        _DetailStrength ("Detail normal strength", Range(0, 2)) = 0.6
        _DetailAlbedo ("Detail albedo variation", Range(0, 1)) = 0.35
        _ScanAlbedo ("Scanned albedo", 2D) = "white" {}
        _ScanNormal ("Scanned normal (OpenGL)", 2D) = "bump" {}
        _ScanScale ("Scan scale", Vector) = (1, 1, 0, 0)
        _ScanNormalStrength ("Scan normal strength", Range(0, 2)) = 1
        _Rim ("Moonlit rim", Range(0, 2)) = 0.6
        _Wind ("Wind sway (metres at the tip)", Float) = 0
        [Enum(Off, 0, Back, 2)] _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull [_Cull]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature_local _ _DETAIL_PLANAR _DETAIL_TRIPLANAR _DETAIL_UV
            #pragma shader_feature_local _ _SCANNED
            #include "UnityCG.cginc"
            #include "SankofaGrade.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _Emission;
            half _Highlight;
            fixed4 _HighlightColor;
            half _Wrap;
            half _UseBake;
            sampler2D _Detail;
            float4 _DetailScale;
            half _DetailStrength;
            half _DetailAlbedo;
            sampler2D _ScanAlbedo;
            sampler2D _ScanNormal;
            float4 _ScanScale;
            half _ScanNormalStrength;
            half _Rim;
            half _Wind;

            // Set by SceneLighting.
            float4 _SankofaLightPos;     // xyz position, w attenuation coefficient
            float4 _SankofaLightColor;   // rgb colour × intensity
            float4 _SankofaAmbientSky;
            float4 _SankofaAmbientGround;
            float4 _SankofaFogColor;
            float4 _SankofaFogParams;    // x start, y 1 / (end − start)
            float4 _SankofaMoonDir;      // xyz towards the moon
            float4 _SankofaMoonColor;
            float4 _SankofaMist;         // x amount, y base height, z height falloff, w start distance
            float4 _SankofaMistColor;

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
                half2 visibility : TEXCOORD5;   // x fire, y moon
                fixed4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
                // Grass sways from the root (UV y is height along the blade).
                float sway = sin(_Time.y * 1.6 + world.x * 1.3 + world.z * 0.9) + 0.4 * sin(_Time.y * 3.7 + world.z * 2.1);
                world.xz += _Wind * v.uv.y * v.uv.y * sway * float2(0.8, 0.6);
                o.pos = mul(UNITY_MATRIX_VP, float4(world, 1));
                o.uv = v.uv;
                o.world = world;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.tangent = UnityObjectToWorldDir(v.tangent.xyz);
                o.bitangent = cross(o.normal, o.tangent) * v.tangent.w * unity_WorldTransformParams.w;
                o.color = lerp(fixed4(1, 1, 1, 1), v.color, _UseBake);
                o.visibility = lerp(half2(1, 1), v.bake, _UseBake);
                return o;
            }

            fixed4 frag(v2f i, fixed facing : VFACE) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half3 n = normalize(i.normal) * (facing > 0 ? 1 : -1);
                half detail = 0.5;
                half3 scan = half3(1, 1, 1);
            #if defined(_SCANNED) && defined(_DETAIL_TRIPLANAR)
                // A scanned surface, projected three ways and blended by facing; the procedural detail becomes a
                // large-scale variation that breaks up the scan's tiling.
                half3 w = pow(abs(n), 4);
                w /= (w.x + w.y + w.z);
                float2 s = _ScanScale.xy;
                scan = tex2D(_ScanAlbedo, i.world.zy * s).rgb * w.x + tex2D(_ScanAlbedo, i.world.xz * s).rgb * w.y + tex2D(_ScanAlbedo, i.world.xy * s).rgb * w.z;
                half3 nx = UnpackNormal(tex2D(_ScanNormal, i.world.zy * s));
                half3 ny = UnpackNormal(tex2D(_ScanNormal, i.world.xz * s));
                half3 nz = UnpackNormal(tex2D(_ScanNormal, i.world.xy * s));
                n = normalize(n + (half3(0, nx.y, nx.x) * w.x + half3(ny.x, 0, ny.y) * w.y + half3(nz.x, nz.y, 0) * w.z) * _ScanNormalStrength);
                detail = tex2D(_Detail, i.world.xz * _DetailScale.xy).b;
            #elif defined(_SCANNED) && defined(_DETAIL_UV)
                float2 suv = i.uv * _ScanScale.xy;
                scan = tex2D(_ScanAlbedo, suv).rgb;
                half3 sn = UnpackNormal(tex2D(_ScanNormal, suv));
                n = normalize(n + (normalize(i.tangent) * sn.x + normalize(i.bitangent) * sn.y) * _ScanNormalStrength);
                detail = tex2D(_Detail, i.uv * _DetailScale.xy).b;
            #elif defined(_DETAIL_PLANAR)
                half4 d = tex2D(_Detail, i.world.xz * _DetailScale.xy);
                n = normalize(n + half3(d.r * 2 - 1, 0, d.g * 2 - 1) * _DetailStrength);
                detail = d.b;
            #elif defined(_DETAIL_TRIPLANAR)
                half3 w = pow(abs(n), 4);
                w /= (w.x + w.y + w.z);
                half4 dx = tex2D(_Detail, i.world.zy * _DetailScale.xy);
                half4 dy = tex2D(_Detail, i.world.xz * _DetailScale.xy);
                half4 dz = tex2D(_Detail, i.world.xy * _DetailScale.xy);
                half3 bump = half3(0, dx.g * 2 - 1, dx.r * 2 - 1) * w.x + half3(dy.r * 2 - 1, 0, dy.g * 2 - 1) * w.y + half3(dz.r * 2 - 1, dz.g * 2 - 1, 0) * w.z;
                n = normalize(n + bump * _DetailStrength);
                detail = dx.b * w.x + dy.b * w.y + dz.b * w.z;
            #elif defined(_DETAIL_UV)
                half4 d = tex2D(_Detail, i.uv * _DetailScale.xy);
                n = normalize(n + (normalize(i.tangent) * (d.r * 2 - 1) + normalize(i.bitangent) * (d.g * 2 - 1)) * _DetailStrength);
                detail = d.b;
            #endif
                half3 albedo = _Color.rgb * scan * tex2D(_MainTex, TRANSFORM_TEX(i.uv, _MainTex)).rgb * i.color.rgb * lerp(1, 0.55 + 0.9 * detail, _DetailAlbedo);

                float3 toLight = _SankofaLightPos.xyz - i.world;
                float d2 = dot(toLight, toLight);
                half3 l = toLight * rsqrt(max(d2, 1e-4));
                half ndl = saturate((dot(n, l) + _Wrap) / (1 + _Wrap));
                half attenuation = 1 / (1 + d2 * _SankofaLightPos.w);

                float3 view = normalize(_WorldSpaceCameraPos - i.world);
                half3 moonDir = normalize(_SankofaMoonDir.xyz + 1e-5);
                half moon = saturate(dot(n, moonDir)) * i.visibility.y;
                // A cool rim where surfaces turn away from the visitor against the moonlit sky.
                half rim = pow(1 - saturate(dot(n, view)), 3) * saturate(dot(n, moonDir) * 0.5 + 0.5) * _Rim;

                half3 ambient = lerp(_SankofaAmbientGround.rgb, _SankofaAmbientSky.rgb, n.y * 0.5 + 0.5) * i.color.a;
                half3 lit = albedo * (ambient + _SankofaLightColor.rgb * ndl * attenuation * i.visibility.x + _SankofaMoonColor.rgb * (moon + rim) * i.color.a);
                lit += _Emission.rgb + _HighlightColor.rgb * _Highlight * (0.35 + 0.65 * ndl);

                float distance = length(i.world - _WorldSpaceCameraPos);
                half fog = saturate((distance - _SankofaFogParams.x) * _SankofaFogParams.y);
                lit = lerp(lit, _SankofaFogColor.rgb, fog);
                // Mist pools low over distant ground.
                half mist = _SankofaMist.x * saturate((distance - _SankofaMist.w) * 0.06) * exp(-max(i.world.y - _SankofaMist.y, 0) * _SankofaMist.z);
                lit = lerp(lit, _SankofaMistColor.rgb, saturate(mist));
                return fixed4(SankofaDither(SankofaGrade(lit), i.pos.xy), 1);
            }
            ENDCG
        }
    }
}
