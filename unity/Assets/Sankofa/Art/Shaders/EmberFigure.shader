// People and animals of a story, shown as the griot shows them: shapes of firelight, brightest at their edges,
// with embers drifting through them. Additive and unlit by the scene, so no face or likeness is ever drawn.
// Intensity fades them in and out; Highlight brightens one when the story turns to them. Kneel folds a standing
// body (feet at the origin, facing +Z, Height tall) down onto its knees, leaning a little forward. ArmNeg and ArmPos
// swing the arm on each side forward from the shoulder (vertex colour red and green mark them); ArmIn brings the
// hands in toward each other, to hold or present something.
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
            float _Kneel;
            float _Height;
            float _ArmNeg;
            float _ArmPos;
            float _ArmIn;

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
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Below the hip the wrapped legs fold under and spread behind; above it the body drops by the folded
            // length and leans forward from the hip. A kneeling body is about seven tenths of its standing height.
            float3 Kneel(float3 p)
            {
                float h = max(_Height, 0.01);
                float hip = 0.48 * h;
                float fold = 0.6 * _Kneel;
                float below = 1 - saturate(p.y / hip);
                float upper = step(hip, p.y);
                float3 folded = p;
                folded.y = p.y * (1 - fold);
                folded.xz *= 1 + 0.3 * _Kneel * below;
                folded.z -= 0.14 * h * _Kneel * below;
                float3 raised = p;
                raised.y = p.y - hip * fold;
                raised.z += (p.y - hip) * 0.22 * _Kneel;
                return lerp(folded, raised, upper);
            }

            // Swings an arm about its shoulder: forward (and up) by raise, and in toward the body's centre by inward.
            float3 Arm(float3 p, float side, float raise, float inward)
            {
                float h = max(_Height, 0.01);
                float3 pivot = float3(side * 0.14 * h, 0.77 * h, 0.02 * h);
                float3 d = p - pivot;
                float a = -raise * 1.35;
                float ca = cos(a), sa = sin(a);
                d = float3(d.x, d.y * ca - d.z * sa, d.y * sa + d.z * ca);
                float b = -side * inward * 0.4;
                float cb = cos(b), sb = sin(b);
                d = float3(d.x * cb - d.y * sb, d.x * sb + d.y * cb, d.z);
                return pivot + d;
            }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 p = v.vertex.xyz;
                p = lerp(p, Arm(p, -1, _ArmNeg, _ArmIn), v.color.r);
                p = lerp(p, Arm(p, 1, _ArmPos, _ArmIn), v.color.g);
                v.vertex.xyz = Kneel(p);
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
