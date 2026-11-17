// Curved flame ribbons occupy a volume; they never rotate to face the camera. The application owns their clock.
Shader "Sankofa/Painted Flame"
{
    Properties
    {
        _MainTex ("Painted fire study",2D)="white" {}
        _Clock ("Application clock",Float)=0
        _Motion ("Permitted movement",Range(0,1))=1
        _Presence ("Narrator warmth",Range(.5,1.5))=1
        _Seed ("Tongue variation",Float)=0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Clock,_Motion,_Presence,_Seed;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            v2f vert(appdata v)
            {
                v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_OUTPUT(v2f,o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float4 p=v.vertex; float h=v.uv.y;
                p.x+=sin(_Clock*2.2+h*5+_Seed)*.012*h*h*_Motion;
                p.z+=cos(_Clock*1.7+h*7+_Seed)*.009*h*h*_Motion;
                o.pos=UnityObjectToClipPos(p); o.uv=v.uv; return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 uv=float2(.15+i.uv.x*.70,.35+i.uv.y*.62);
                uv.x+=sin(_Clock*2.1+i.uv.y*18+_Seed)*.004*i.uv.y*_Motion;
                fixed4 c=tex2D(_MainTex,uv);
                // Keep the flame's painted strokes, dropping the dark wood in the source; real logs are separate meshes.
                c.a*=smoothstep(.12,.38,c.r-c.b)*smoothstep(0,.12,i.uv.y);
                c.a*=smoothstep(0,.08,min(i.uv.x,1-i.uv.x));
                c.rgb*=.80*_Presence*(1+.035*sin(_Clock*3+_Seed)*_Motion);
                return c;
            }
            ENDCG
        }
    }
}
