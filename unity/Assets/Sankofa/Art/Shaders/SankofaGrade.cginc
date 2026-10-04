// The look of every Sankofa surface, applied at the end of each shader in place of post-processing, which Quest
// cannot afford: exposure, a filmic tone curve (so the flame's core rolls off instead of clipping to white), a
// little desaturation, and a split tone with cool shadows and warm highlights. Set by SceneLighting.SetGrade;
// zeros fall back to neutral so the editor view is never black.
#ifndef SANKOFA_GRADE_INCLUDED
#define SANKOFA_GRADE_INCLUDED

float4 _SankofaGrade; // x exposure, y saturation, z shadow coolness, w highlight warmth

half3 SankofaGrade(half3 c)
{
    half exposure = _SankofaGrade.x > 0 ? _SankofaGrade.x : 1;
    half saturation = _SankofaGrade.y > 0 ? _SankofaGrade.y : 1;
    c *= exposure;
    // Narkowicz's fit of the ACES filmic curve.
    c = saturate((c * (2.51 * c + 0.03)) / (c * (2.43 * c + 0.59) + 0.14));
    half luma = dot(c, half3(0.2126, 0.7152, 0.0722));
    c = lerp(luma.xxx, c, saturation);
    // Split tone as a multiplication, so black stays black and additive effects never tint their whole quad.
    half3 shadows = lerp(half3(1, 1, 1), half3(0.93, 0.98, 1.1), _SankofaGrade.z);
    half3 highlights = lerp(half3(1, 1, 1), half3(1.06, 1.0, 0.92), _SankofaGrade.w);
    c *= lerp(shadows, highlights, smoothstep(0.0, 0.5, luma));
    return max(c, 0);
}

/// Breaks up 8-bit banding in smooth night gradients with a tiny screen-space noise.
half3 SankofaDither(half3 c, float2 screen)
{
    float n = frac(sin(dot(screen, float2(12.9898, 78.233))) * 43758.5453);
    return c + (n - 0.5) / 255.0;
}

#endif
