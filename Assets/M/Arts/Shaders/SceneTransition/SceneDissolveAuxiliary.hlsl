#ifndef PROJECTWAIT_SCENE_DISSOLVE_AUXILIARY
#define PROJECTWAIT_SCENE_DISSOLVE_AUXILIARY
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
float3 _LightDirection, _LightPosition;
struct SceneAuxInput
{
    float4 position : POSITION;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};
struct SceneAuxOutput
{
    float4 position : SV_POSITION;
    float3 world : TEXCOORD0;
    float3 normal : TEXCOORD1;
    float2 uv : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};
SceneAuxOutput SceneDepthVertex(SceneAuxInput input)
{
    SceneAuxOutput output = (SceneAuxOutput)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.world = TransformObjectToWorld(input.position.xyz);
    output.normal = TransformObjectToWorldNormal(input.normal);
    output.position = TransformWorldToHClip(output.world);
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
    return output;
}
SceneAuxOutput SceneShadowVertex(SceneAuxInput input)
{
    SceneAuxOutput output = SceneDepthVertex(input);
    #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
        float3 direction = normalize(_LightPosition-output.world);
    #else
        float3 direction = _LightDirection;
    #endif
    output.position = ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(output.world, normalize(output.normal), direction)));
    return output;
}
half SceneAuxFragment(SceneAuxOutput input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    clip(LobbyDissolveSDF(input.world, input.normal));
    #if defined(_ALPHATEST_ON)
        Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap,sampler_BaseMap)).a, _BaseColor, _Cutoff);
    #endif
    return input.position.z;
}
#endif
