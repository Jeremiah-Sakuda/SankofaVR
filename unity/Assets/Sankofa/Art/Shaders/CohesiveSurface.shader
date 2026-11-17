Shader "Sankofa/Cohesive Surface"
{
    Properties
    {
        _MainTex ("Pigment atlas",2D)="white" {}
        _Region ("Pigment region",Vector)=(0,0,1,1)
        _Color ("Tint",Color)=(1,1,1,1)
        _WorldUV ("World projection",Float)=0
        _Scale ("Pigment scale",Float)=1
        _VertexColor ("Vertex colour",Range(0,1))=0
        _Wind ("Reed movement",Float)=0
        _Water ("Water",Float)=0
        _GroundFade ("Blend far bank into painting",Float)=0
        [HideInInspector] _SrcBlend ("Source blend",Float)=1
        [HideInInspector] _DstBlend ("Destination blend",Float)=0
        [HideInInspector] _ZWrite ("Depth writes",Float)=1
        _Clock ("Application clock",Float)=0
        _Motion ("Motion allowed",Float)=1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        Blend [_SrcBlend] [_DstBlend]
        ZWrite [_ZWrite]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _Region,_Color;
            float _WorldUV,_Scale,_VertexColor,_Wind,_Water,_GroundFade,_Clock,_Motion;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; fixed4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; float2 uv:TEXCOORD2; fixed4 color:COLOR; UNITY_VERTEX_OUTPUT_STEREO };
            v2f vert(appdata v)
            {
                v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_OUTPUT(v2f,o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 world=mul(unity_ObjectToWorld,v.vertex).xyz;
                world.x+=_Wind*_Motion*sin(_Clock*.65+world.z*2+world.x)*v.uv.y*v.uv.y*.025;
                o.world=world; o.pos=UnityWorldToClipPos(world); o.normal=UnityObjectToWorldNormal(v.normal);
                o.uv=v.uv; o.color=v.color; return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float coverage=_GroundFade>.5 ? smoothstep(0,1,saturate((4.9-i.uv.y)/2.4)) : 1;
                float2 source=lerp(i.uv,i.world.xz,_WorldUV)*_Scale;
                source+=float2(_Clock*.012*_Motion*_Water,0);
                float2 tile=1-abs(frac(source)*2-1);
                half3 paint=tex2D(_MainTex,_Region.xy+(.02+tile*.96)*_Region.zw).rgb*_Color.rgb;
                if(_GroundFade>.5)
                {
                    float2 fine=1-abs(frac(source*5.17)*2-1);
                    paint*=.72+tex2D(_MainTex,_Region.xy+(.02+fine*.96)*_Region.zw).rgb*1.3;
                }
                paint*=lerp(half3(1,1,1),i.color.rgb,_VertexColor);
                half3 n=normalize(i.normal);
                float d=saturate(dot(n,normalize(float3(-.65,.7,-.55)))*.6+.4);
                half3 shade=lerp(half3(.24,.29,.43),half3(1.25,1.05,.81),smoothstep(0,1,d));
                float rim=pow(1-saturate(dot(n,normalize(_WorldSpaceCameraPos-i.world))),3);
                half3 colour=paint*shade+paint*half3(.15,.23,.38)*rim;
                if(_Wind>.5) colour=paint*lerp(half3(.55,.65,.74),half3(1.2,1.08,.75),i.uv.x)*lerp(.7,1.25,i.uv.y);
                if(_Water>.5)
                {
                    float stroke=pow(saturate(sin(i.world.z*27+sin(i.world.x*4+_Clock*.24*_Motion)*2.7)),22);
                    colour=paint*half3(.55,.7,.85)+stroke*half3(.012,.017,.022);
                }
                float fog=saturate((distance(i.world,_WorldSpaceCameraPos)-3)/18);
                return fixed4(lerp(colour,half3(.032,.046,.073),fog*.65),coverage);
            }
            ENDCG
        }
    }
}
