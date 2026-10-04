// Night sky on the inside of a large sphere: deep zenith, a faint glow at the horizon and the soft band of
// the Milky Way. Drawn first, behind everything; distant hills fog toward the same horizon colour.
Shader "Sankofa/Sky"
{
    Properties
    {
        _Zenith ("Zenith", Color) = (0.004, 0.006, 0.018, 1)
        _Horizon ("Horizon", Color) = (0.03, 0.035, 0.06, 1)
        _Band ("Milky Way", Color) = (0.04, 0.04, 0.052, 1)
        _BandAxis ("Milky Way axis", Vector) = (0.35, 0.55, 0.76, 0)
        _Noise ("Noise", 2D) = "gray" {}
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "IgnoreProjector"="True" }
        Cull Front
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "SankofaGrade.cginc"

            fixed4 _Zenith;
            fixed4 _Horizon;
            fixed4 _Band;
            float4 _BandAxis;
            sampler2D _Noise;
            float4 _SankofaMoonDir;
            float4 _SankofaMoonColor;

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 world : TEXCOORD0;
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
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 dir = normalize(i.world - _WorldSpaceCameraPos);
                half h = dir.y;
                half3 col = lerp(_Horizon.rgb, _Zenith.rgb, pow(saturate(h), 0.5));
                // The Milky Way: a soft great-circle band broken up by noise.
                half across = dot(dir, normalize(_BandAxis.xyz));
                half band = exp(-across * across / 0.02);
                float2 suv = float2(atan2(dir.x, dir.z) * 0.159, h);
                half clouds = tex2D(_Noise, suv * float2(5, 3)).r * tex2D(_Noise, suv * float2(14, 9)).g * tex2D(_Noise, suv * float2(31, 19)).b * 6;
                clouds = clouds * clouds;
                col += _Band.rgb * band * clouds * smoothstep(0.02, 0.3, h);
                // The moon: a small bright disc in a wide, faint halo, only when the scene has one.
                half moonLight = dot(_SankofaMoonColor.rgb, half3(0.33, 0.33, 0.33));
                if (moonLight > 0)
                {
                    // Full precision: near the moon the angle changes in the fourth decimal place.
                    float facing = dot(dir, normalize(_SankofaMoonDir.xyz));
                    float disc = smoothstep(0.99985, 0.99992, facing);
                    float halo = pow(saturate(facing), 220.0) * 0.6 + pow(saturate(facing), 18.0) * 0.08;
                    col += half3(0.75, 0.8, 0.9) * disc * 1.6 + _SankofaMoonColor.rgb * halo * 6;
                }
                return fixed4(SankofaDither(SankofaGrade(col), i.pos.xy), 1);
            }
            ENDCG
        }
    }
}
