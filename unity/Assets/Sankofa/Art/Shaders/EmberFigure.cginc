// Shared by the ember figure's colour shader and its depth-only shader: the properties and the pose (kneel, arm
// swings), so both draw exactly the same surface.
#ifndef SANKOFA_EMBER_FIGURE_INCLUDED
#define SANKOFA_EMBER_FIGURE_INCLUDED
#include "UnityCG.cginc"
#include "SankofaGrade.cginc"

sampler2D _Noise;
fixed4 _Color;
fixed4 _Core;
half _Intensity;
half _Rim;
half _Drift;
half _Highlight;
float _Kneel;
float _Height;
float _ArmNeg;
float _ArmPos;
float _ArmIn;
float _Procedural;
float _GroundY;
half _Smoke;
float4 _HeadSphere;

struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    float3 normal : NORMAL;
    fixed4 color : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

// Below the hip the wrapped legs fold under and spread behind; above it the body drops by the folded
// length and leans forward from the hip. A kneeling body is about seven tenths of its standing height.
float3 Kneel(float3 p)
{
    float h = max(_Height, 0.01);
    float hip = 0.48 * h;
    float kneel = _Kneel * _Procedural;
    float fold = 0.6 * kneel;
    float below = 1 - saturate(p.y / hip);
    float upper = step(hip, p.y);
    float3 folded = p;
    folded.y = p.y * (1 - fold);
    folded.xz *= 1 + 0.3 * kneel * below;
    folded.z -= 0.14 * h * kneel * below;
    float3 raised = p;
    raised.y = p.y - hip * fold;
    raised.z += (p.y - hip) * 0.22 * kneel;
    return lerp(folded, raised, upper);
}

// An arm's swing about its shoulder: forward (and up) by raise, in toward the body's centre by inward.
float3 Swing(float3 d, float side, float raise, float inward)
{
    float a = -raise * 1.35;
    float ca = cos(a), sa = sin(a);
    d = float3(d.x, d.y * ca - d.z * sa, d.y * sa + d.z * ca);
    float b = -side * inward * 0.4;
    float cb = cos(b), sb = sin(b);
    return float3(d.x * cb - d.y * sb, d.x * sb + d.y * cb, d.z);
}

// The posed vertex and its normal: the same rotation turns both, so the rim stays true on a raised arm.
void Pose(inout float3 p, inout float3 n, fixed4 arms)
{
    arms *= _Procedural;
    float h = max(_Height, 0.01);
    float3 pivotNeg = float3(-0.14 * h, 0.77 * h, 0.02 * h);
    float3 pivotPos = float3(0.14 * h, 0.77 * h, 0.02 * h);
    p = lerp(p, pivotNeg + Swing(p - pivotNeg, -1, _ArmNeg, _ArmIn), arms.r);
    n = lerp(n, Swing(n, -1, _ArmNeg, _ArmIn), arms.r);
    p = lerp(p, pivotPos + Swing(p - pivotPos, 1, _ArmPos, _ArmIn), arms.g);
    n = lerp(n, Swing(n, 1, _ArmPos, _ArmIn), arms.g);
    p = Kneel(p);
}
#endif
