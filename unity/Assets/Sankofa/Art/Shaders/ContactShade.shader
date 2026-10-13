// A small painted contact patch, revealed with its owner. It grounds the cutout at the seat's intended view.
Shader "Sankofa/Contact Shade"
{
    Properties
    {
        _Color ("Earth shade", Color) = (0.12, 0.065, 0.09, 0.32)
        _Visibility ("Story visibility", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-10" }
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
            fixed4 _Color;
            half _Visibility;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 position : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.position = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half2 offset = (i.uv - 0.5) * 2;
                half shade = 1 - smoothstep(0.05, 1, dot(offset, offset));
                return fixed4(_Color.rgb, _Color.a * shade * _Visibility);
            }
            ENDCG
        }
    }
}
