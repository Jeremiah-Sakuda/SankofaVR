// People and animals of a story, shown as the griot shows them: shapes of firelight, brightest at their edges,
// with embers drifting through them. Additive and unlit by the scene, so no face or likeness is ever drawn.
// Intensity fades them in and out; Highlight brightens one when the story turns to them. Kneel folds a standing
// body (feet at the origin, facing +Z, Height tall) down onto its knees, leaning a little forward. ArmNeg and ArmPos
// swing the arm on each side forward from the shoulder (vertex colour red and green mark them); ArmIn brings the
// hands in toward each other, to hold or present something. Its depth is drawn first, for every part of every figure,
// by Ember Figure Depth (one queue earlier), so the nearest surface of the whole figure, body and cloth together, is
// the only one lit: no arm doubles its glow and no body shows through its cloth. The feet dissolve into smoke. A
// skinned body (Procedural 0) is posed by its animator instead. A thin smoky core dims what is behind a figure, so it
// reads as a presence rather than glass.
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
        _Kneel ("Kneel", Range(0, 1)) = 0
        _Height ("Standing height (m)", Float) = 1.6
        _ArmNeg ("Arm on -X forward", Range(-0.3, 1)) = 0
        _ArmPos ("Arm on +X forward", Range(-0.3, 1)) = 0
        _ArmIn ("Hands together", Range(0, 1)) = 0
        _Procedural ("Posed by this shader", Float) = 1
        _GroundY ("Ground height (world)", Float) = 0
        _Smoke ("Smoke opacity", Range(0, 1)) = 0.3
        _CrownY ("Neck height (world); above it the head is calm light", Float) = 1000
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }

        CGINCLUDE
        #include "EmberFigure.cginc"
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
                o.normal = UnityObjectToWorldNormal(normalize(n));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 view = normalize(_WorldSpaceCameraPos - i.world);
                half edge = pow(1 - saturate(abs(dot(normalize(i.normal), view))), _Rim);
                // Embers rise through the figure; a slow flicker keeps it alive while it stands still.
                float2 p = i.world.xz * 6 + i.world.y * 3;
                // The head, above the neck wherever it is (standing, bowing or kneeling), is calm, even light: no spark
                // or flicker ever settles where eyes or a mouth would be.
                half head = smoothstep(_CrownY, _CrownY + 0.06, i.world.y);
                // Small sparks rather than patches: finer across and up, and only the brightest of the noise.
                half embers = tex2D(_Noise, float2(p.x * 0.3, i.world.y * 4.5 - _Time.y * _Drift * 2.5)).b;
                embers = smoothstep(0.72, 0.97, embers) * 0.8 * (1 - head);
                half flicker = 0.85 + 0.15 * sin(_Time.y * 3.1 + i.world.y * 4);
                // The edge burns rather than shines: tongues of light run up the silhouette and break it, so no
                // smooth contour reads as a modelled surface.
                half tongues = tex2D(_Noise, float2((i.world.x + i.world.z) * 1.9, i.world.y * 0.9 - _Time.y * 0.55)).r;
                edge *= lerp(0.45 + 1.1 * smoothstep(0.3, 0.75, tongues), 1.0, head);
                // Flame rising through the body: two layers of noise drifting upward at different rates, leaving
                // darker smoke between the tongues, so the body reads as fire rather than tinted glass.
                half f1 = tex2D(_Noise, float2((i.world.x - i.world.z) * 2.2, i.world.y * 1.3 - _Time.y * 0.7)).r;
                half f2 = tex2D(_Noise, float2((i.world.x + i.world.z) * 4.0, i.world.y * 2.4 - _Time.y * 1.3)).g;
                half flame = smoothstep(0.5, 0.85, f1 * 0.6 + f2 * 0.5) * (1 - head);
                half3 c = _Core.rgb * 0.2 + _Color.rgb * (edge * 2.0 + embers * 0.9 + flame * 0.55);
                // The feet are smoke: a ragged edge that drifts upward, so the figure stands in the scene without
                // a hard line where it meets the ground.
                half wisp = tex2D(_Noise, float2(i.world.x * 2.3 + i.world.z * 1.7, i.world.y * 2.0 - _Time.y * 0.35)).g;
                // Height above the ground the figure stands on: 0 at the feet, 1 at a standing crown.
                half rise = (i.world.y - _GroundY) / max(_Height, 0.01);
                half feet = saturate((rise - 0.01 - wisp * 0.06) / 0.06);
                // A soft, even fill in the head, so it reads as a head of light rather than a hollow rim, and carries a
                // little more of the light, where a listener looks.
                c += _Color.rgb * 0.35 * head;
                half crown = 1 + 0.2 * head;
                half presence = _Intensity * feet;
                c *= presence * lerp(flicker, 1, head) * (1 + _Highlight * 0.8) * crown;
                // Premultiplied: the glow adds, the smoke dims what is behind; thinner between the tongues of flame.
                return fixed4(SankofaGrade(c), saturate(_Smoke * presence * lerp(0.7 + 0.3 * flame, 1, head)));
            }
            ENDCG
        }
    }
}
