// A world-anchored painted card / soft veil. Both eyes share the same authored reveal and clock.
Shader "Sankofa/Cinematic Paint"
{
    Properties
    {
        _MainTex ("Painting", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Opacity ("Opacity", Range(0,1)) = 1
        _Edge ("Soft painted edge", Range(0,0.5)) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Depth test", Float) = 4
        _ZWrite ("Depth write", Float) = 0
        _WorldTextureScale ("Ground texture repetitions per metre; zero uses card UV", Float) = 0
        _Clock ("Application-owned animation seconds", Float) = 0
        _Motion ("Permitted motion", Range(0,1)) = 1
        [Enum(Still,0,Fire,1,River,2,Lamplight,3)] _Animation ("Painted motion", Float) = 0
        _UVRect ("Painted detail crop", Vector) = (0,0,1,1)
        _EmberCutout ("Use warm pigment as ember alpha", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite [_ZWrite] ZTest [_ZTest] Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            fixed4 _Color;
            float _Opacity, _Edge, _WorldTextureScale;
            float _Clock, _Motion, _Animation, _EmberCutout;
            float4 _UVRect;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float3 world:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f,o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.world=mul(unity_ObjectToWorld,v.vertex).xyz; return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 uv=i.uv;
                float fire=smoothstep(.5,.65,uv.x)*smoothstep(.17,.3,uv.y)*(1-smoothstep(.7,.9,uv.y));
                float river=(1-smoothstep(.45,.55,uv.y))*(1-smoothstep(.06,.18,abs(uv.x-(.98-.93*uv.y))));
                if(_Animation>.5 && _Animation<1.5)
                {
                    uv.x+=sin(_Clock*2.2+uv.y*23)*.0035*fire*_Motion;
                    uv.y+=cos(_Clock*1.7+uv.x*19)*.002*fire*_Motion;
                }
                if(_Animation>1.5 && _Animation<2.5) uv.x+=sin(uv.y*80-_Clock*1.8)*.0018*river*_Motion;
                uv=_UVRect.xy+uv*_UVRect.zw;
                fixed4 c=tex2D(_MainTex,_WorldTextureScale>0 ? i.world.xz*_WorldTextureScale : uv)*_Color;
                if(_Animation>.5 && _Animation<1.5) c.rgb*=1+.07*sin(_Clock*3.3)*fire*_Motion;
                if(_Animation>1.5 && _Animation<2.5) c.rgb+=half3(.015,.025,.03)*sin(i.uv.y*110-_Clock*2)*river*_Motion;
                if(_Animation>2.5) c.rgb*=1+.028*sin(_Clock*.77)*smoothstep(.5,.8,i.uv.x)*_Motion;
                if(_EmberCutout>.5) c.a*=smoothstep(.08,.30,c.r-c.b);
                float2 edge=min(i.uv,1-i.uv);
                c.a*=_Opacity*(_Edge<=0 ? 1 : smoothstep(0,_Edge,min(edge.x,edge.y)));
                return c;
            }
            ENDCG
        }
    }
}
