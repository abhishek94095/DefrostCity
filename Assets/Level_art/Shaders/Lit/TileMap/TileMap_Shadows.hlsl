#ifndef LIT_TILEMAP_SHADOWS_INCLUDED
#define LIT_TILEMAP_SHADOWS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

float3 _LightDirection;

struct VertexInput
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    float4 positionCS : SV_POSITION;
};

float4 GetShadowPositionHClip(float3 positionOS, half3 normalOS)
{
    float3 positionWS = TransformObjectToWorld(positionOS);
    float3 normalWS = TransformObjectToWorldNormal(normalOS);

    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

#if UNITY_REVERSED_Z
    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#else
    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#endif

    return positionCS;
}


VertexOutput vertShadow(VertexInput IN)
{
    VertexOutput OUT;
    UNITY_SETUP_INSTANCE_ID(IN);

    float4 vertexOS = IN.positionOS;
    half3 normalOS = IN.normalOS;

    OUT.positionCS = GetShadowPositionHClip(IN.positionOS.xyz, IN.normalOS);
    return OUT;
}

half4 fragShadow(VertexOutput IN) : SV_TARGET
{
    return 0;
}

#endif
