#ifndef PROJECTWAIT_M_LOBBY_DISSOLVE_INCLUDED
#define PROJECTWAIT_M_LOBBY_DISSOLVE_INCLUDED
#include "LobbyFadeCommon.hlsl"

// Uniforms are declared in the owner's UnityPerMaterial buffer. No camera/depth
// texture dependency: the same world-space mask works in both XR eyes and shadows.
float LobbyDissolveSDF(float3 positionWS, float3 normalWS)
{
    if (_DissolveEnabled < .5) return 2;
    bool reveal = _DissolveReveal > .5;
    if (_DissolveProgress <= 0) return reveal ? -2 : 2;
    if (_DissolveProgress >= 1) return reveal ? 2 : -2;

    // Always hand off to fixed sphere geometry; no legacy/comparison mode.
    float keep = saturate((LobbyKeepAt(positionWS) - .72) / .28);
    if (keep <= .00001) return -2;
    if (keep >= .99999) return 2;

    // Constant metre spacing projects naturally to SMALL distant / LARGE nearby
    // dots. Atlas UVs are deliberately untouched. Blend surface projections to
    // avoid a hard axis seam on the rounded shrubs and car body.
    float3 grid = positionWS / max(.02, _DissolveCellSize);
    float3 cell = frac(grid) - .5;
    float3 weights = pow(abs(normalize(normalWS)), 8);
    weights /= max(dot(weights, float3(1, 1, 1)), .0001);
    float dots = dot(float3(length(cell.yz), length(cell.xz), length(cell.xy)), weights);
    return lerp(-.001, .708, keep) - dots;
}

float LobbyDissolveCoverage(float3 positionWS, float3 normalWS)
{
    float sdf = LobbyDissolveSDF(positionWS, normalWS);
    clip(sdf);
    // MSAA alpha-to-coverage softens only the thin dot boundary, not the colour bands.
    return saturate(sdf / max(fwidth(sdf), .0001));
}

#endif
