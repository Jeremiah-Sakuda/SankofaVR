// The city across the river at night. Window cells come from world position along each facade, so any box becomes a
// building without textures. Each building's vertex colour sets its tone (r), its style (g: punched windows, or a
// glass curtain wall above 0.8), how many of its windows are lit (b) and the height of its base (a; 1 marks a plain
// part with no windows). Storeys are marked by slab edges, unlit glass reflects a little of the sky, some lit rooms
// are half curtained, and street-level shops are lit more often than the floors above. Fog and the shared grade keep
// it in the scene's depth.
Shader "Sankofa/City"
{
    Properties
    {
        _Wall ("Wall colour", Color) = (0.03, 0.03, 0.04, 1)
        _Warm ("Warm window", Color) = (1, 0.72, 0.42, 1)
        _Cool ("Cool window", Color) = (0.6, 0.75, 1, 1)
        _Shop ("Shop light", Color) = (0.85, 1, 0.9, 1)
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
            fixed4 _Shop;
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
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 world : TEXCOORD0;
                half3 normal : TEXCOORD1;
                float4 building : TEXCOORD2;
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
                o.building = v.color;
                return o;
            }

            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half3 n = normalize(i.normal);
                half tone = i.building.r;
                half style = i.building.g;
                half plain = step(0.97, i.building.a);
                float baseY = i.building.a / 0.95 * 40 - 8;
                // Concrete, plaster and paint: warm or cool, lighter or darker, building by building.
                half3 wall = _Wall.rgb * lerp(0.6, 2.0, tone) * lerp(half3(1.08, 1.0, 0.9), half3(0.9, 0.96, 1.1), step(0.5, frac(tone * 7)));
                half3 ambient = 0.4 + _SankofaAmbientSky.rgb * 4;
                half3 lit = wall * ambient * lerp(0.7, 1, saturate(n.y * 0.5 + 0.5));
                if (abs(n.y) < 0.5 && plain < 0.5)
                {
                    // Along the facade, whichever way it faces.
                    float u = dot(i.world.xz, normalize(float2(-n.z, n.x)));
                    half glassWall = step(0.8, style);
                    float width = _Cell.x * lerp(0.75, 1.4, frac(style * 3.7)) * lerp(1, 0.55, glassWall);
                    float2 cell = float2(u / width, (i.world.y - baseY) / _Cell.y);
                    float2 id = floor(cell) + floor(tone * 97);
                    float2 f = frac(cell);
                    half street = 1 - step(1, cell.y);
                    // Punched windows; a glass wall is nearly all window; shops are wide fronts at street level.
                    half2 lo = lerp(lerp(half2(0.3, 0.36), half2(0.06, 0.12), glassWall), half2(0.08, 0.1), street);
                    half2 hi = lerp(lerp(half2(0.7, 0.74), half2(0.94, 0.92), glassWall), half2(0.92, 0.66), street);
                    half window = step(lo.x, f.x) * step(f.x, hi.x) * step(lo.y, f.y) * step(f.y, hi.y);
                    half slab = (1 - step(0.06, f.y)) * (1 - glassWall);
                    half share = _Lit * lerp(0.4, 1.8, i.building.b) * lerp(1, 0.6, glassWall);
                    half on = step(1 - lerp(share, 0.55, street), hash(id));
                    // Some lit rooms are half curtained; the lit part is the lower half of the pane.
                    half curtained = step(0.72, hash(id * 5.3)) * (1 - street);
                    half lamp = on * lerp(1, step(f.y, lerp(lo.y, hi.y, 0.5)), curtained);
                    half3 tint = lerp(_Warm.rgb, _Cool.rgb, step(lerp(0.7, 0.35, glassWall), hash(id * 1.7)));
                    tint = lerp(tint, _Shop.rgb * lerp(0.9, 1.1, hash(id * 2.3)), street * step(0.5, hash(id * 4.1)));
                    // Dark glass reflects a little of the glowing sky; slab edges catch it too.
                    lit = lerp(lit, wall * 0.45 + _SankofaAmbientSky.rgb * 0.15, window * (1 - lamp));
                    lit += slab * wall * 0.6;
                    lit += window * lamp * tint * _Glow * (0.55 + 0.45 * hash(id * 3.1));
                }
                float distance = length(i.world - _WorldSpaceCameraPos);
                lit = lerp(lit, _SankofaFogColor.rgb, saturate((distance - _SankofaFogParams.x) * _SankofaFogParams.y) * 0.6);
                return fixed4(SankofaDither(SankofaGrade(lit), i.pos.xy), 1);
            }
            ENDCG
        }
    }
}
