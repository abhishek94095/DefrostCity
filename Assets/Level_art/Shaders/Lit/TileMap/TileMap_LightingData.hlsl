#ifndef LIT_TILEMAP_LIGHTING_DATA_INCLUDED
#define LIT_TILEMAP_LIGHTING_DATA_INCLUDED

inline void InitializeSurfaceData(out SurfaceData outSurfaceData, half3 diffuse)
{
    // Set everything to 0 by default
    outSurfaceData = (SurfaceData) 0;

    outSurfaceData.albedo = diffuse;

    // NOTE: Need to set this or we get very dark shadows
    outSurfaceData.occlusion = 1;
}

inline void InitializeSurfaceData(out SurfaceData outSurfaceData, half3 diffuse, half smoothness, half3 normalTS )
{
    // Set everything to 0 by default
    outSurfaceData = (SurfaceData) 0;

    outSurfaceData.albedo = diffuse;

    outSurfaceData.smoothness = smoothness;

    // NOTE: Need to set this or we get very dark shadows
    outSurfaceData.occlusion = 1;
    
    outSurfaceData.normalTS = normalTS;
}

inline void InitializeSurfaceData(out SurfaceData outSurfaceData, half3 diffuse, half3 msa, half3 normalTS)
{
    // Set everything to 0 by default
    outSurfaceData = (SurfaceData) 0;
    
    outSurfaceData.albedo = diffuse;
    
    outSurfaceData.metallic = msa.r;
    outSurfaceData.smoothness = msa.g;
    outSurfaceData.occlusion = msa.b;
    
    outSurfaceData.normalTS = normalTS;
}

inline void InitializeSurfaceData(out SurfaceData outSurfaceData, half3 diffuse, half3 msa, half3 normalTS, half3 emission)
{
    // Set everything to 0 by default
    outSurfaceData = (SurfaceData) 0;
    
    outSurfaceData.albedo = diffuse;
    
    outSurfaceData.metallic = msa.r;
    outSurfaceData.smoothness = msa.g;
    outSurfaceData.occlusion = msa.b;
    
    outSurfaceData.normalTS = normalTS;
    
#ifdef ENABLE_EMISSIVE
    outSurfaceData.emission = emission;
#endif
}

void InitializeInputData(float3 positionWS, half3 normalWS, half3 bakedGI, out InputData inputData)
{
    inputData = (InputData) 0;

    inputData.positionWS = positionWS;

    inputData.normalWS = normalWS;
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(positionWS);;

    inputData.shadowCoord = TransformWorldToShadowCoord(positionWS);
    
    inputData.bakedGI = bakedGI;
}

void InitializeInputData(float3 positionWS, 
    half3 tangentWS, half3 bitangentWS, half3 normalWS, half3 normalTS, half3 bakedGI, half3 viewDirectionWS, 
    out InputData inputData)
{
    inputData = (InputData) 0;

    inputData.positionWS = positionWS;

    half3x3 tangentTransform = half3x3(tangentWS, bitangentWS, normalWS);
    inputData.tangentToWorld = tangentTransform;
    inputData.normalWS = TransformTangentToWorld(normalTS, tangentTransform);
    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    inputData.viewDirectionWS = viewDirectionWS;

    inputData.shadowCoord = TransformWorldToShadowCoord(positionWS);
    
    inputData.bakedGI = bakedGI;
}

void InitializeInputData(float3 positionWS,
    half3 tangentWS, half3 bitangentWS, half3 normalWS, half3 normalTS, half3 bakedGI, out InputData inputData)
{
    half3 viewDirectionWS = GetWorldSpaceNormalizeViewDir(positionWS);
    InitializeInputData(positionWS, tangentWS, bitangentWS, normalWS, normalTS, bakedGI, viewDirectionWS, inputData);
}


void InitializeInputData(float3 positionWS, half3 normalWS, half normalLengthMultiplier, half3 bakedGI,
    out InputData inputData)
{
    inputData = (InputData) 0;

    inputData.positionWS = positionWS;

    inputData.normalWS = NormalizeNormalPerPixel(normalWS) * normalLengthMultiplier;
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(positionWS);

    inputData.shadowCoord = TransformWorldToShadowCoord(positionWS);
    
    inputData.bakedGI = bakedGI;
}

void InitialiseLightingData(float3 positionWS,
    half3 tangentWS, half3 bitangentWS, half3 normalWS, half3 normalTS, half3 diffuse, half smoothness,
    half3 bakedGI,
    out SurfaceData surfaceData, out InputData inputData)
{
    InitializeSurfaceData(surfaceData, diffuse, smoothness, normalTS);
    InitializeInputData(positionWS, tangentWS, bitangentWS, normalWS, normalTS, bakedGI, inputData);
}

void InitialiseLightingData(float3 positionWS, half3 normalWS, half3 diffuse,
    half3 bakedGI,
    out SurfaceData surfaceData, out InputData inputData)
{
    InitializeSurfaceData(surfaceData, diffuse);
    InitializeInputData(positionWS, normalWS, bakedGI, inputData);
}

void InitialiseLightingData(float3 positionWS, half3 normalWS, half normalLengthMultiplier, half3 diffuse,
    half3 bakedGI,
    out SurfaceData surfaceData, out InputData inputData)
{
    InitializeSurfaceData(surfaceData, diffuse);
    InitializeInputData(positionWS, normalWS, normalLengthMultiplier, bakedGI, inputData);
}

#endif
