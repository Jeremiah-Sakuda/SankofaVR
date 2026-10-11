// Theatrical people: solid bronze bodies, quiet woven garments, restrained ember outlines and separate
// dark facial/hair details. Lit by the same analytic fire/lantern and ambient as the stage. A memory echo
// uses a brighter additive treatment. The companion depth pass prevents layered body and cloth surfaces
// doubling their light. Generic CC0 character faces depict fictional composites, not real likenesses.
Shader "Sankofa/Ember Figure"
{
    Properties
    {
        _Noise ("Noise", 2D) = "gray" {}
        _Color ("Edge colour", Color) = (1, 0.55, 0.22, 1)
        _Core ("Core colour", Color) = (0.35, 0.1, 0.03, 1)
        _Intensity ("Intensity", Float) = 1
        _Visibility ("Reveal opacity", Range(0, 1)) = 1
        _Feature ("Hair and facial details", Range(0, 1)) = 0
        _Memory ("Memory echo", Range(0, 1)) = 0
        _Fabric ("Fabric surface", Range(0, 1)) = 0
        _Rim ("Edge sharpness", Float) = 2.2
        _Drift ("Ember drift", Float) = 0.25
        _Highlight ("Highlight", Range(0, 1)) = 0
        _Kneel ("Kneel", Range(0, 1)) = 0
        _Height ("Standing height (m)", Float) = 1.6
        _ArmNeg ("Arm on -X forward", Range(-0.3, 1)) = 0
        _ArmPos ("Arm on +X forward", Range(-0.3, 1)) = 0
        _ArmIn ("Hands together", Range(0, 1)) = 0
        _Procedural ("Posed by this shader", Float) = 1
        _GroundY ("Ground height (world)", Float) = 0
        _Smoke ("Smoke opacity", Range(0, 1)) = 0.3
        _HeadSphere ("Head (world centre, radius); inside it the light is calm", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }

        CGINCLUDE
        #include "EmberFigure.cginc"
        half _Fabric, _Memory, _Feature, _Visibility;
        float4 _SankofaLightPos, _SankofaLightColor;
        float4 _SankofaAmbientSky, _SankofaMoonDir, _SankofaMoonColor;
        ENDCG

        Pass
        {
            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 world : TEXCOORD0;
                float2 uv : TEXCOORD2;
                half3 normal : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 p = v.vertex.xyz;
                float3 n = v.normal;
                Pose(p, n, v.color);
                o.pos = UnityObjectToClipPos(float4(p, 1));
                o.world = mul(unity_ObjectToWorld, float4(p, 1)).xyz;
                o.uv = v.uv;
                o.normal = UnityObjectToWorldNormal(normalize(n));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 view = normalize(_WorldSpaceCameraPos - i.world);
                // A narrow edge, so the light sits at the silhouette rather than shining across the body like glass.
                half edge = pow(1 - saturate(abs(dot(normalize(i.normal), view))), _Rim * 1.6);
                // Embers rise through the figure; a slow flicker keeps it alive while it stands still.
                float2 p = i.world.xz * 6 + i.world.y * 3;
                // The head, wherever it is (standing, bowing or kneeling), is calm, even light: no spark or flicker
                // ever settles where eyes or a mouth would be. A sphere about the head bone, so a bowed back is not
                // mistaken for it; cloth passes a radius of 0 and never has a head.
                half head = _HeadSphere.w > 0 ? 1 - smoothstep(_HeadSphere.w, _HeadSphere.w + 0.04, distance(i.world, _HeadSphere.xyz)) : 0;
                // Small sparks rather than patches: finer across and up, and only the brightest of the noise.
                half embers = tex2D(_Noise, float2(p.x * 0.3, i.world.y * 4.5 - _Time.y * _Drift * 2.5)).b;
                embers = smoothstep(0.72, 0.97, embers) * 0.8 * (1 - head);
                half flicker = 0.85 + 0.15 * sin(_Time.y * 3.1 + i.world.y * 4);
                // The edge burns rather than shines: tongues of light run up the silhouette and break it, so no
                // smooth contour reads as a modelled surface.
                half tongues = tex2D(_Noise, float2((i.world.x + i.world.z) * 1.9, i.world.y * 0.9 - _Time.y * 0.55)).r;
                edge *= lerp(0.45 + 1.1 * smoothstep(0.3, 0.75, tongues), 0.9, head);
                // Height above the ground the figure stands on: 0 at the feet, 1 at a standing crown.
                half rise = (i.world.y - _GroundY) / max(_Height, 0.01);
                // Flame rising through the body: tongues drawn out upward, in two layers drifting at different rates,
                // strongest toward the hem as a fire burns from below, with dark smoke between them, so the body
                // reads as fire in smoke rather than tinted glass.
                half f1 = tex2D(_Noise, float2((i.world.x - i.world.z) * 2.6, i.world.y * 0.75 - _Time.y * 0.9)).r;
                half f2 = tex2D(_Noise, float2((i.world.x + i.world.z) * 5.0, i.world.y * 1.4 - _Time.y * 1.6)).g;
                half flame = smoothstep(0.45, 0.9, f1 * 0.6 + f2 * 0.5) * lerp(1.2, 0.5, saturate(rise)) * (1 - head);
                // Warm, solid theatrical bodies with quiet fabric: volume comes from the light direction,
                // not a uniform orange fill. Only the silhouette carries the fire's ember signature.
                half3 normal = normalize(i.normal);
                float3 toLight = _SankofaLightPos.xyz - i.world;
                half light = saturate(dot(normal, normalize(toLight)) * 0.7 + 0.3);
                half atten = 1 / (1 + dot(toLight, toLight) * _SankofaLightPos.w);
                half3 illumination = 0.22 + _SankofaAmbientSky.rgb * 0.8 + _SankofaLightColor.rgb * light * atten;
                illumination += _SankofaMoonColor.rgb * saturate(dot(normal, normalize(_SankofaMoonDir.xyz)));
                half weave = sin(i.uv.x * 600) * sin(i.uv.y * 650) * 0.035;
                half border = (1 - smoothstep(0.025, 0.065, abs(frac(i.uv.y) - 0.08))) * _Fabric;
                half3 pigment = lerp(half3(0.10, 0.065, 0.045), _Color.rgb * 0.17, _Fabric);
                pigment = lerp(pigment, half3(0.014, 0.009, 0.006), _Feature);
                half3 c = pigment * illumination * (1 + weave) + _Color.rgb * border * 0.055;
                c += _Color.rgb * (edge * 0.04 + embers * 0.012 + flame * 0.008) * (1 - _Fabric * 0.6) * (1 - _Feature);
                half wisp = tex2D(_Noise, float2(i.world.x * 2.3 + i.world.z * 1.7, i.world.y * 2.0 - _Time.y * 0.35)).g;
                half feet = saturate((rise - 0.005 - wisp * 0.02) / 0.035);
                half presence = _Intensity * feet;
                c = lerp(c, _Color.rgb * (0.12 + edge * 0.7 + embers * 0.12), _Memory);
                c *= presence * (1 + _Highlight * 0.25);
                // Premultiplied: the glow adds, and the smoke hides what is behind (a figure at full smoke is opaque).
                // Illumination flicker changes brightness, never makes a clothed person transparent.
                return fixed4(SankofaGrade(c), saturate(_Smoke * _Visibility * feet));
            }
            ENDCG
        }
    }
}
