#ifndef PROJECTWAIT_SCENE_LIT_FADE
#define PROJECTWAIT_SCENE_LIT_FADE
// URP owns LitInput's UnityPerMaterial layout. These temporary material uniforms
// deliberately live outside that buffer instead of editing the package's input.
float4 _DissolveOrigin, _DissolveRange;
float _DissolveEnabled, _DissolveProgress, _DissolveReveal, _DissolveCellSize, _DissolveBandWidth;
#include "../TitleScene/LobbyDissolveCommon.hlsl"
#endif
