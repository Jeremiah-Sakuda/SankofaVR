// Solid, textured geometry for nearby objects. Painted pigment receives continuous light around the full form.
Shader "Sankofa/Painted Object"
{
    Properties
    {
        _MainTex ("Painted surface atlas", 2D) = "white" {}
        _Region ("Atlas region", Vector) = (0,0,1,1)
        _Color ("Pigment", Color) = (1,1,1,1)
        _Repeat ("Surface repeats", Vector) = (1,1,0,0)
        _Highlight ("Reach feedback", Range(0,1)) = 0
        _Visibility ("Story visibility", Range(0,1)) = 1
        _Ember ("Ember warmth", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _Region, _Repeat, _Color, _SankofaLightPos;
            float _Highlight, _Visibility, _Ember;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; fixed4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; float2 uv:TEXCOORD2; fixed4 color:COLOR; UNITY_VERTEX_OUTPUT_STEREO };
            v2f vert(appdata v)
            {
                v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_OUTPUT(v2f,o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos=UnityObjectToClipPos(v.vertex); o.world=mul(unity_ObjectToWorld,v.vertex).xyz;
                o.normal=UnityObjectToWorldNormal(v.normal); o.uv=v.uv; o.color=v.color; return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                clip(_Visibility-.001);
                // A small inset avoids another material bleeding across atlas quadrants at the mip boundaries.
                float2 tiled=frac(i.uv*_Repeat.xy);
                tiled.x=1-abs(tiled.x*2-1); // Mirror pigment around the body so an imperfect tile has no hard rear seam.
                float2 uv=_Region.xy+(.012+tiled*.976)*_Region.zw;
                half3 pigment=tex2D(_MainTex,uv).rgb*_Color.rgb*i.color.rgb;
                half3 n=normalize(i.normal);
                half3 key=normalize(float3(-.65,.9,-.7));
                half diffuse=saturate((dot(n,key)+.18)/1.18);
                half3 shade=lerp(half3(.34,.31,.38),half3(1.12,1.00,.84),smoothstep(0,1,diffuse));
                float3 toFire=_SankofaLightPos.xyz-i.world;
                half fire=saturate(dot(n,normalize(toFire)))*.10/(1+dot(toFire,toFire)*3);
                half3 colour=pigment*(shade+half3(fire,fire*.5,fire*.15));
                colour+=half3(1,.57,.18)*_Highlight*.12;
                colour+=half3(.5,.075,.008)*_Ember*(.35+.65*saturate(n.y));
                return fixed4(colour, 1);
            }
            ENDCG
        }
    }
}
