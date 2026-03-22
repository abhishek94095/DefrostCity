#ifndef LIT_TILEMAP_WATER_INPUT_INCLUDED
#define LIT_TILEMAP_WATER_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#define FULL_LIGHTING_NO_EMISSIVE
#include "../TileMap_Lighting.hlsl"

TEXTURE2D(_MacroWater);
SAMPLER(sampler_MacroWater);
TEXTURE2D(_Water);
SAMPLER(sampler_Water);
TEXTURE2D(_Coast);
SAMPLER(sampler_Coast);
TEXTURE2D(_NormalA);
SAMPLER(sampler_NormalA);
TEXTURE2D(_FlowMap);
SAMPLER(sampler_FlowMap);

CBUFFER_START(UnityPerMaterial)
uniform half _WaterTiling;
uniform half _CoastTiling;
uniform half _NormalTiling;
uniform float4 _MacroWater_TexelSize;
CBUFFER_END

struct VertexInput
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
#ifdef UVSPACE
    half2 texcoord : TEXCOORD0;
#endif
};

struct VertexOutput
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    float3 tangentWS : TEXCOORD2;
    float3 bitangentWS : TEXCOORD3;
    half3 bakedGI : TEXCOORD4;
#ifdef UVSPACE
    half2 uv : TEXCOORD5;
#endif
};

VertexOutput vert(VertexInput IN)
{
    VertexOutput OUT = (VertexOutput) 0;

    VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normal, IN.tangent);
    OUT.normalWS = normalInputs.normalWS;
    OUT.tangentWS = normalInputs.tangentWS;
    OUT.bitangentWS = normalInputs.bitangentWS;

    VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.vertex.xyz);
    OUT.positionCS = positionInputs.positionCS;
    OUT.positionWS = positionInputs.positionWS;
    
    // Calculate GI per vertex
    // Using the vertex normal not the normal map one
    OUT.bakedGI = SampleSH_Partial(normalInputs.normalWS);
    
#ifdef UVSPACE
    OUT.uv = IN.texcoord;
#endif

    return OUT;
}

float3 flowUV(float2 uv, float3 flowMap, float time, float phaseOffset)
{
    float2 flow = flowMap.rg * 2 - 1;
    time += flowMap.b;
    
    float progress = frac(time + phaseOffset);
    float3 uvw;
    uvw.xy = uv - flow * progress;
    uvw.xy += phaseOffset;
    uvw.xy += (time - progress) * float2(0.3, 0.35);
    
    uvw.z = 1 - abs(1 - 2 * progress);
    return uvw;
}

#include "../TileMap_LightingData.hlsl"

half4 frag(VertexOutput IN) : SV_Target
{
    //World Space Tiling
    float2 worldPos_Mask = IN.positionWS.rb;
    // _MacroBlend_TexelSize.xy contains 1.0 / width and 1.0 / height
    // multiplying by that gives us a uv which maps the world position to the texture
    // NOTE: We need to take into account that the pixels are being mapped to hexagons
    //       when mapping world space to texture space
    const float2 hexSizeMultipliers = float2(1.0, 1.0 / 0.866);
    float2 macroUV = (worldPos_Mask * hexSizeMultipliers * _MacroWater_TexelSize.xy);
    float2 normalTiling = worldPos_Mask / _NormalTiling;
    
    //Flow
    float time = _Time.y * 0.25;
#ifdef UVSPACE
    float3 flowMap = SAMPLE_TEXTURE2D(_FlowMap, sampler_FlowMap, IN.uv).rgb;
    float3 phase1 = flowUV(IN.uv, flowMap, time, 0);
    float3 phase2 = flowUV(IN.uv, flowMap, time, 0.5);
#else
    float3 flowMap = SAMPLE_TEXTURE2D(_FlowMap, sampler_FlowMap, macroUV).rgb;
    float3 phase1 = flowUV(macroUV, flowMap, time, 0);
    float3 phase2 = flowUV(macroUV, flowMap, time, 0.5);
#endif
    
    //Normal maps
    float3 normal1 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalA, sampler_NormalA, normalTiling + phase1.xy)).rgb * phase1.z;
    float3 normal2 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalA, sampler_NormalA, normalTiling + phase2.xy)).rgb * phase2.z;

    //Water and Coast Textures
#ifdef UVSPACE
    float4 macro = SAMPLE_TEXTURE2D(_MacroWater, sampler_MacroWater, IN.uv);
#else
    float4 macro = SAMPLE_TEXTURE2D(_MacroWater, sampler_MacroWater, macroUV);
#endif

    float3 water = SAMPLE_TEXTURE2D(_Water, sampler_Water, worldPos_Mask / _WaterTiling).rgb;
    float3 coast = SAMPLE_TEXTURE2D(_Coast, sampler_Coast, worldPos_Mask / _CoastTiling).rgb;
    float3 finalColor = lerp(coast, water, macro.a) * macro.rgb;

    const half SMOOTHNESS = 0.875;
    half3 normalTS = normal1 + normal2;
    const half3 NORMAL_OFFSET_WS = half3(0.2, -0.2, -0.2);

    // Lighting
    SurfaceData surfaceData;
    InputData inputData;
    InitialiseLightingData(IN.positionWS, IN.tangentWS, IN.bitangentWS, IN.normalWS + NORMAL_OFFSET_WS,
        normalTS, finalColor, SMOOTHNESS, 
        IN.bakedGI,
        surfaceData, inputData);
    return DoTileMapLighting(surfaceData, inputData);
}

#endif
