// Things the visitor handles in the illustrated theatre (a gourd, a bead, the hide), drawn in the paintings' own
// language rather than as rendered objects: the scene's one warm light falls in three soft tone bands, a dark ink line
// gathers where the surface turns away, and a little pigment grain breaks up flat colour. Highlight answers attention
// and a reaching hand, as on other story objects. Opaque, one pass, stereo instanced.
Shader "Sankofa/Painted Prop"
{
    Properties
    {
        _Color ("Pigment", Color) = (0.6, 0.4, 0.22, 1)
        _Noise ("Grain", 2D) = "gray" {}
        _Ink ("Ink colour", Color) = (0.12, 0.07, 0.04, 1)
        _InkWidth ("Ink width", Range(0, 1)) = 0.42
        _Highlight ("Highlight", Range(0, 1)) = 0
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
            sampler2D _Noise;
            fixed4 _Ink;
            half _InkWidth;
            half _Highlight;
            float4 _SankofaLightPos;
            float4 _SankofaLightColor;
            float4 _SankofaAmbientSky;
            float4 _SankofaAmbientGround;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 world : TEXCOORD0;
                half3 normal : TEXCOORD1;
                float3 local : TEXCOORD2;
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
                o.local = v.vertex.xyz;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half3 n = normalize(i.normal);
                float3 view = normalize(_WorldSpaceCameraPos - i.world);
                // Grain and brush strokes fixed to the object, so they move with it and match in both eyes. Strokes
                // run around the object, as a painter models a rounded form.
                half grain = tex2D(_Noise, i.local.xy * 9 + i.local.z * 5).b;
                half strokes = tex2D(_Noise, float2(i.local.x * 7 + i.local.z * 5, i.local.y * 30)).r;
                half3 pigment = _Color.rgb * i.color.rgb * (0.9 + 0.2 * grain);
                pigment = lerp(dot(pigment, half3(0.3, 0.59, 0.11)).xxx, pigment, 0.95);

                // The painter's key: the fire's direction, lifted, so tops catch the light and undersides fall into
                // shadow. Three bands, shadow, half-tone and light, with stroke-broken edges.
                float3 toLight = _SankofaLightPos.xyz - i.world;
                float d2 = dot(toLight, toLight);
                half3 key = normalize(toLight * rsqrt(max(d2, 1e-4)) + float3(0, 0.8, 0));
                half t = dot(n, key) * 0.5 + 0.5 + (strokes - 0.5) * 0.2;
                half band = smoothstep(0.38, 0.44, t) * 0.5 + smoothstep(0.7, 0.76, t) * 0.5;
                // Shadows lean toward the paintings' violet without losing the pigment; light goes warm.
                half3 shadow = pigment * half3(0.5, 0.4, 0.42) + _SankofaAmbientGround.rgb * 0.08;
                half3 mid = pigment * 0.8;
                half3 lit = pigment * half3(1.12, 1.0, 0.82) + _SankofaLightColor.rgb * 0.04;
                half3 colour = band < 0.5 ? lerp(shadow, mid, band * 2) : lerp(mid, lit, band * 2 - 1);
                half attenuation = 1 / (1 + d2 * _SankofaLightPos.w);
                colour *= lerp(0.8, 1.05, saturate(attenuation * 2)) + _SankofaAmbientSky.rgb * 0.1;
                // The ink line: where the surface turns from the eye, with a little grain in its edge.
                half edge = 1 - saturate(dot(n, view));
                half ink = smoothstep(1 - _InkWidth, 1 - _InkWidth * 0.6, edge + (grain - 0.5) * 0.1);
                colour = lerp(colour, _Ink.rgb, ink * 0.9);
                colour += half3(1, 0.68, 0.3) * _Highlight * 0.18;
                return fixed4(SankofaDither(SankofaGrade(colour), i.pos.xy), 1);
            }
            ENDCG
        }
    }
}
