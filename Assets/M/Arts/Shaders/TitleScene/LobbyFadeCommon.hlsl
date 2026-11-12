#ifndef PROJECTWAIT_M_LOBBY_FADE_COMMON
#define PROJECTWAIT_M_LOBBY_FADE_COMMON

float DropletSmooth(float x) { x=saturate(x); return x*x*(3-2*x); }
float LobbyKeepAt(float3 center)
{
    float span=max(.1,_DissolveRange.y-_DissolveRange.x);
    float d=saturate((distance(center,_DissolveOrigin.xyz)-_DissolveRange.x)/span);
    float band=clamp(_DissolveBandWidth/span,.02,1);
    float start=(_DissolveReveal>.5 ? d : 1-d)*(1-band);
    float local=DropletSmooth((_DissolveProgress-start)/band);
    return _DissolveReveal>.5 ? local : 1-local;
}
#endif
