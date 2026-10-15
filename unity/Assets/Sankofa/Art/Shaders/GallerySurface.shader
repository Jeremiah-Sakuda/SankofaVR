// Authored architectural surfaces. Opaque at rest; a temporary material blends only during entry/return.
// Metric object coordinates keep material detail attached when the seated gallery is recentered.
Shader "Sankofa/Gallery Surface"
{
    Properties
    {
        _Color ("Material colour", Color) = (0.4,0.3,0.2,1)
        _Noise ("Material grain", 2D) = "gray" {}
        [Enum(Plaster,0,Timber,1,Stone,2,Metal,3,Light,4)] _Finish ("Finish", Float) = 0
        _Visibility ("Room presence", Range(0,1)) = 1
        [HideInInspector] _SrcBlend ("Source blend", Float) = 1
        [HideInInspector] _DstBlend ("Destination blend", Float) = 0
        [HideInInspector] _ZWrite ("Depth write", Float) = 1
        _Wash ("Soft wall wash", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Blend [_SrcBlend] [_DstBlend]
        ZWrite [_ZWrite]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "SankofaGrade.cginc"
            fixed4 _Color; sampler2D _Noise; half _Finish, _Wash, _Visibility;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f
            {
                float4 pos:SV_POSITION; float3 metric:TEXCOORD0; half3 normal:TEXCOORD1;
                float2 uv:TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO
            };
            v2f vert(appdata v)
            {
                v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_OUTPUT(v2f,o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos=UnityObjectToClipPos(v.vertex);
                float3 size=float3(length(unity_ObjectToWorld._m00_m10_m20),length(unity_ObjectToWorld._m01_m11_m21),length(unity_ObjectToWorld._m02_m12_m22));
                o.metric=v.vertex.xyz*size; o.normal=v.normal; o.uv=v.uv; return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half3 n=normalize(i.normal);
                // Object-local triplanar grain: no view-dependent shimmer or random per-frame noise.
                half3 w=abs(n); w/=max(w.x+w.y+w.z,0.001);
                half grain=tex2D(_Noise,i.metric.zy*3).r*w.x+tex2D(_Noise,i.metric.xz*3).r*w.y+tex2D(_Noise,i.metric.xy*3).r*w.z;
                half variation=0.97+grain*0.06;
                if(_Finish>0.5 && _Finish<1.5)
                {
                    half fibres=tex2D(_Noise,float2(i.metric.x*20+i.metric.z*20,i.metric.y*0.6)).g;
                    variation=0.82+fibres*0.30;
                }
                half light=0.48+0.32*saturate(dot(n,normalize(half3(-0.3,0.8,-0.6))));
                // Broad warm wash from the architectural cove; no animated hot spots on the painting.
                half wash=exp2(-pow((i.uv.y-0.78)*2.2,2))*_Wash;
                half edge=smoothstep(0,0.09,i.uv.y)*smoothstep(0,0.09,1-i.uv.y);
                half3 colour=_Color.rgb*variation*(light+wash*0.35)*(0.88+edge*0.12);
                if(_Finish>1.5 && _Finish<2.5 && abs(n.y)>0.5)
                {
                    // Large stone slabs, with filtered fine joints rather than high-frequency terrazzo specks.
                    float2 cell=abs(frac((i.metric.xz+0.6)/1.2)-0.5)*1.2;
                    float2 aa=max(fwidth(i.metric.xz),0.001);
                    half joint=1-min(smoothstep(0.003,0.003+aa.x,cell.x),smoothstep(0.003,0.003+aa.y,cell.y));
                    colour*=1-joint*0.22;
                    colour*=0.84+0.16*exp2(-dot(i.metric.xz,i.metric.xz)*0.09);
                    // Cool light enters the real side apertures, with broad, filtered arcade shadows.
                    half court=exp2(-pow((abs(i.metric.x)-3.3)*0.55,2));
                    float band=abs(frac((i.metric.z+i.metric.x*0.45)/2.45)-0.5);
                    half shade=smoothstep(0.18,0.25,band);
                    colour+=half3(0.032,0.047,0.064)*court*(1-shade*0.7);
                }
                if(_Finish>2.5 && _Finish<3.5)colour+=_Color.rgb*0.12;
                if(_Finish>3.5)colour=_Color.rgb*1.5;
                return fixed4(SankofaDither(SankofaGrade(colour),i.pos.xy),_Visibility);
            }
            ENDCG
        }
    }
}
