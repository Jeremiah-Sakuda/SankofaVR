// The city across the river at night: dark blocks whose facades carry a grid of windows, a random few lit warm or
// cool. Window cells come from world position, so any box becomes a building without textures. Fog and the shared
// grade keep it in the scene's depth.
Shader "Sankofa/City"
{
    Properties
    {
        _Wall ("Wall colour", Color) = (0.03, 0.03, 0.04, 1)
        _Warm ("Warm window", Color) = (1, 0.72, 0.42, 1)
        _Cool ("Cool window", Color) = (0.6, 0.75, 1, 1)
        _Cell ("Window spacing (m)", Vector) = (3.2, 3.4, 0, 0)
        _Lit ("Fraction lit", Range(0, 1)) = 0.22
        _Glow ("Window brightness", Float) = 0.9
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
            #include "SankofaGrade.cginc"

            fixed4 _Wall;
            fixed4 _Warm;
            fixed4 _Cool;
            float4 _Cell;
            half _Lit;
            half _Glow;
            float4 _SankofaFogColor;
            float4 _SankofaFogParams;
            float4 _SankofaAmbientSky;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

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
                o.pos = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half3 n = normalize(i.normal);
                half3 lit = _Wall.rgb * (0.4 + _SankofaAmbientSky.rgb * 4);
                if (abs(n.y) < 0.5)
                {
                    // Along the facade: horizontal coordinate from whichever axis the wall runs along.
                    float u = abs(n.x) > abs(n.z) ? i.world.z : i.world.x;
                    float2 cell = float2(u / _Cell.x, i.world.y / _Cell.y);
                    float2 id = floor(cell);
                    float2 f = frac(cell);
                    half window = step(0.32, f.x) * step(f.x, 0.68) * step(0.38, f.y) * step(f.y, 0.72);
                    float r = hash(id + floor(i.world.xz / 40));
                    half on = step(1 - _Lit, r);
                    half3 tint = lerp(_Warm.rgb, _Cool.rgb, step(0.7, hash(id * 1.7)));
                    lit += window * on * tint * _Glow * (0.6 + 0.4 * hash(id * 3.1));
                }
                float distance = length(i.world - _WorldSpaceCameraPos);
                lit = lerp(lit, _SankofaFogColor.rgb, saturate((distance - _SankofaFogParams.x) * _SankofaFogParams.y) * 0.6);
                return fixed4(SankofaDither(SankofaGrade(lit), i.pos.xy), 1);
            }
            ENDCG
        }
    }
}
