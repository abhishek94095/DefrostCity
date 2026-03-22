#ifndef LIT_TILEMAP_DIFF_NORM_MSA_INCLUDED
#define LIT_TILEMAP_DIFF_NORM_MSA_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

#ifdef ENABLE_DEPTH_OFFSET
#include "TileMap_DepthOffset.hlsl"
#endif

#include "../TileMap_LightingData.hlsl"

struct VertexInput
{
    half4 vertex : POSITION;
    half3 normal : NORMAL;
    half4 tangent : TANGENT;
    half2 uv : TEXCOORD0;
#if defined(LIGHTMAP_ON)
    half2 staticLightmapUV : TEXCOORD1;
#endif
};

struct VertexOutput
{
    half4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    half2 uv : TEXCOORD1;
    half3 normalWS : TEXCOORD2;
    half3 tangentWS : TEXCOORD3;
    half3 bitangentWS : TEXCOORD4;
#if defined(LIGHTMAP_ON)
    half2 staticLightmapUV : TEXCOORD5;
#else
    #ifdef PER_VERTEX_SH
        half3 bakedGI : TEXCOORD5;
    #endif
#endif
};

VertexOutput vert(VertexInput IN)
{
    VertexOutput OUT = (VertexOutput) 0;

    VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.vertex.xyz);
    OUT.positionCS = positionInputs.positionCS;
    OUT.positionWS = positionInputs.positionWS;
    
#ifdef ENABLE_DEPTH_OFFSET
    OUT.positionCS = ApplyDepthOffset(OUT.positionCS);
#endif

    VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normal, IN.tangent);
    OUT.normalWS = normalInputs.normalWS;
    OUT.tangentWS = normalInputs.tangentWS;
    OUT.bitangentWS = normalInputs.bitangentWS;
#ifdef CAN_BE_MIRRORED
    // Handle mirrored meshes by detecting if front face culling is on and flipping the bitangent
    // this will cause the entire tangent frame to be flipped and fix up incorrect normals that would otherwise result
    if( _Cull == 1.0 )
    {
        OUT.bitangentWS *= -1.0;
    }
#endif

    OUT.uv = IN.uv;
    
#if defined(LIGHTMAP_ON)
    // Expanded macro from Lighting.hlsl
    OUT.staticLightmapUV = IN.staticLightmapUV * tilemap_Lightmap_ST.xy + tilemap_Lightmap_ST.zw;
#else
    #ifdef PER_VERTEX_SH
        // Calculate GI per vertex
        // Using the vertex normal not the normal map one
        OUT.bakedGI = SampleSH_Partial(normalInputs.normalWS);
    #endif
#endif

    return OUT;
}

half3 hueShift(half value, half3 diffuse)
{
    half3 color = RgbToHsv(diffuse);
    half hue = color.x + value;
    color.x = RotateHue(hue, 0.0, 1.0);
    return HsvToRgb(color);
}

half3 DoHueSatShift(half3 diffuse, half hueAdjust, half satAdjust, half levelAdjust)
{
    half3 updatedDiffuse = hueShift(hueAdjust, diffuse);

    //Sat-Desat Shift
    half lum = Luminance(updatedDiffuse);
    updatedDiffuse = (updatedDiffuse - lum) * satAdjust + lum;

    //Mid Level Shift
    half level = 1.0 / levelAdjust;
    updatedDiffuse = pow(abs(updatedDiffuse), level);

    return updatedDiffuse;
}

half3 GetBakedGI(VertexOutput IN)
{
#if defined(LIGHTMAP_ON)
    return SampleLightmap(IN.staticLightmapUV);
#else
    #ifdef PER_VERTEX_SH
        return IN.bakedGI;
    #else
        return SampleSH_Partial(IN.normalWS);
    #endif
#endif
}

void InitialiseLightingData(VertexOutput IN, 
    half3 normalTS, half3 diffuse, half3 msa,
    out SurfaceData surfaceData, out InputData inputData)
{
    InitializeSurfaceData(surfaceData, diffuse, msa, normalTS);
    InitializeInputData(IN.positionWS, IN.tangentWS, IN.bitangentWS, IN.normalWS, normalTS, GetBakedGI(IN), inputData);
}

void InitialiseLightingData(VertexOutput IN, 
    half3 normalTS, half3 diffuse, half3 msa, half3 emission,
    out SurfaceData surfaceData, out InputData inputData )
{
    InitializeSurfaceData(surfaceData, diffuse, msa, normalTS, emission);
    InitializeInputData(IN.positionWS, IN.tangentWS, IN.bitangentWS, IN.normalWS, normalTS, GetBakedGI(IN), inputData);
}

#endif
