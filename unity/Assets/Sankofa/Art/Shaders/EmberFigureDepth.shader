// The depth of every story figure's surfaces, drawn one queue before their light, so each figure's colour pass draws
// only its nearest surface: body, wrap and shawl together read as one silhouette. Posed exactly as the colour shader.
Shader "Sankofa/Ember Figure Depth"
{
    Properties
    {
        _Kneel ("Kneel", Range(0, 1)) = 0
        _Height ("Standing height (m)", Float) = 1.6
        _ArmNeg ("Arm on -X forward", Range(-0.3, 1)) = 0
        _ArmPos ("Arm on +X forward", Range(-0.3, 1)) = 0
        _ArmIn ("Hands together", Range(0, 1)) = 0
        _Procedural ("Posed by this shader", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent-1" "RenderType"="Transparent" "IgnoreProjector"="True" }

        CGINCLUDE
        #include "EmberFigure.cginc"
        ENDCG

        Pass
        {
            ZWrite On
            ColorMask 0
            Cull Back
            CGPROGRAM
            #pragma vertex vertDepth
            #pragma fragment fragDepth
            #pragma multi_compile_instancing

            struct v2fDepth
            {
                float4 pos : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2fDepth vertDepth(appdata v)
            {
                v2fDepth o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2fDepth, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 p = v.vertex.xyz;
                float3 n = v.normal;
                Pose(p, n, v.color);
                o.pos = UnityObjectToClipPos(float4(p, 1));
                return o;
            }

            fixed4 fragDepth(v2fDepth i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                return 0;
            }
            ENDCG
        }
    }
}
