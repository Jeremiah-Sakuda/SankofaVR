// Individual portrait poses use the existing stage's atlas-row contract; story timing stays in PopUpStage.
Shader "Sankofa/Cohesive Figure"
{
    Properties
    {
        _MainTex ("Rest", 2D) = "white" {}
        _Offer ("Offer", 2D) = "white" {}
        _Reflect ("Reflect", 2D) = "white" {}
        _FrameA ("Previous stage frame", Vector) = (0,.666667,.5,.333333)
        _FrameB ("Current stage frame", Vector) = (0,.666667,.5,.333333)
        _PoseBlend ("Pose blend", Range(0,1)) = 1
        _Visibility ("Visibility", Range(0,1)) = 1
        _Unfold ("Unfold", Range(0,1)) = 1
        _Highlight ("Highlight", Range(0,1)) = 0
        _RiverDissolve ("River portrait ink dissolve", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Cull Off
        ZWrite On
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            sampler2D _MainTex, _Offer, _Reflect;
            float4 _FrameA, _FrameB;
            float _PoseBlend, _Visibility, _Unfold, _Highlight, _RiverDissolve;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            v2f vert(appdata v)
            {
                v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_OUTPUT(v2f,o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float grain=frac(sin(dot(floor(i.uv*450),float2(12.9898,78.233)))*43758.5453);
                // Only the river adapter enables this. The small paper stage retains its original pose treatment.
                float nextPose = _RiverDissolve > .5 ? step(grain, saturate(_PoseBlend)) : step(.5, _PoseBlend);
                float row=2-round((nextPose < .5 ? _FrameA.y : _FrameB.y)*3);
                fixed4 pigment=row<.5 ? tex2D(_MainTex,i.uv) : row<1.5 ? tex2D(_Offer,i.uv) : tex2D(_Reflect,i.uv);
                clip(pigment.a-.5);
                clip(_Visibility-grain*.98-.001); clip(_Unfold*1.15-i.uv.y-grain*.12);
                float dip=_RiverDissolve > .5 ? 0 : 1-abs(_PoseBlend*2-1);
                return fixed4(pigment.rgb*(1-.32*smoothstep(0,1,dip))*(.90+_Highlight*.10),1);
            }
            ENDCG
        }
    }
}
