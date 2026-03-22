#ifndef LIT_TILEMAP_TREES_INPUT_INCLUDED
#define LIT_TILEMAP_TREES_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#define DIFFUSE_ONLY_LIGHTING
#include "../TileMap_Lighting.hlsl"

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

// NOTE: If a shader is SRP batch compatible then it will
//       be SRP batched even if Instancing is enabled
//       To force it to using Instancing we break the SRP batching compatibility
//       by removing the CBUFFER_START / END definitions
// NOTE: Picking up the INSTANCING_ON define doesn't seem to work for some reason
//       so we need to #define FORCE_INSTANCING_ON before including this file
//       in shaders that we want to support instancing
#if !defined(FORCE_INSTANCING_ON)
CBUFFER_START(UnityPerMaterial)
#endif
uniform half4 _Amplitude;
uniform half4 _Frequency;
uniform half4 _Speed;
uniform half4 _BaseColor;
uniform half4 _Color;
uniform half _ColorVariation;
uniform half _ColorIntensity;
uniform half _BaseColorIntensity;
uniform half _LightIntensity;
float _BurnAmount, _BurnLength, _GlowStrength;
float4 _BurnColor;
float4 _GlowColor;
float _DissolveAmount;
#if !defined(FORCE_INSTANCING_ON)
CBUFFER_END
#endif

struct VertexInput 
{
    half4 vertex                : POSITION;
    half3 normal                : NORMAL;
    half2 texcoord              : TEXCOORD0;
    half4 vertexColor           : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput 
{
    half4 positionCS            : SV_POSITION;
    float3 positionWS           : TEXCOORD0;
    half3 normalWS              : TEXCOORD1;
    half2 uv                    : TEXCOORD2;
    half colourLerp             : TEXCOORD3;
#ifdef PER_VERTEX_SH
    half3 bakedGI               : TEXCOORD4;
#endif
};

VertexOutput vert(VertexInput IN) 
{
    VertexOutput OUT = (VertexOutput)0;

    UNITY_SETUP_INSTANCE_ID(IN);

    //Vertex Offset
    // Set world space offset
    half3 offset = TransformObjectToWorld(half3(0, 0, 0)) + IN.vertex.xyz;
    offset *= 3;
    // Set vert offset per axis using vert color to determine which part of the mesh are affected by vert offset
    IN.vertex.x += sin((IN.texcoord.y - _Time.y * _Speed.x) * _Frequency.x + offset.x) * _Amplitude.x * IN.vertexColor.a;
    IN.vertex.y += sin((IN.texcoord.x - _Time.y * _Speed.y) * _Frequency.y + offset.y) * _Amplitude.y * IN.vertexColor.a;
    IN.vertex.z += sin(((IN.texcoord.x + IN.texcoord.y) - _Time.y * _Speed.z) * _Frequency.z + offset.z) * _Amplitude.z * IN.vertexColor.a;

    OUT.normalWS = IN.normal;

    VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.vertex.xyz);
    OUT.positionCS = positionInputs.positionCS;
    OUT.positionWS = positionInputs.positionWS;

    float4x4 objectToWorld = GetObjectToWorldMatrix();
    float hueVariationAmount = frac((objectToWorld[0].w + objectToWorld[1].w + objectToWorld[2].w) * _ColorVariation);
    OUT.colourLerp = saturate(hueVariationAmount * _Color.a) * IN.vertexColor.a;

    OUT.uv = IN.texcoord;
    
#ifdef PER_VERTEX_SH
    // Calculate GI per vertex
    // Using the vertex normal
    OUT.bakedGI = SampleSH_Partial(IN.normal);
#endif

    return OUT;
}

#include "../TileMap_LightingData.hlsl"

half4 frag(VertexOutput IN) : COLOR
{
    half4 diffuse = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
    clip(diffuse.a - 0.5);
    
    diffuse.rgb = lerp(_BaseColor.rgb * diffuse.rgb * _BaseColorIntensity, _Color.rgb * diffuse.rgb * _ColorIntensity, IN.colourLerp);

#ifdef BURNVFX
    // Burn
	float burnProgress = smoothstep(0.05, 0.05 + _BurnLength, diffuse.a - _BurnAmount);
    diffuse.rgb *= lerp(_BurnColor.rgb, 1.0, burnProgress);

    float glowZone = saturate(1.0 - abs(burnProgress - 0.5) * 3.0);
    half3 glow = _GlowColor.rgb * _GlowColor.a * glowZone * _GlowStrength;
    diffuse.rgb += glow;
    
    // Dissolve
    float dissolve = diffuse.r - _DissolveAmount;
    dissolve += 0.1;
    clip( dissolve );
    
    float dissolveGlowZone = smoothstep(0.05, 0.01, dissolve );
    diffuse.rgb += ( dissolveGlowZone * _GlowColor.rgb * _GlowStrength );
#endif
    
    // Lighting
    SurfaceData surfaceData;
    InputData inputData;
    InitialiseLightingData(IN.positionWS, IN.normalWS, _LightIntensity, diffuse.rgb,
#ifdef PER_VERTEX_SH
        IN.bakedGI,
#else
        SampleSH_Partial(IN.normalWS),
#endif
        surfaceData, inputData);
    return DoTileMapLighting(surfaceData, inputData);
}

#endif
