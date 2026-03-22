#ifndef LIT_TILEMAP_LIGHTING_INCLUDED
#define LIT_TILEMAP_LIGHTING_INCLUDED

// Define this to use the full version of a Unity lighting model for comparison
//#define USE_FULL_UNITY_PBR

#if defined(DIFFUSE_ONLY_LIGHTING) || defined(FULL_LIGHTING_NO_EMISSIVE) || defined(FULL_LIGHTING_WITH_EMISSIVE)
#define ENABLE_DIFFUSE_DIRECT
#define ENABLE_DIFFUSE_INDIRECT
#endif

#if defined(FULL_LIGHTING_NO_EMISSIVE) || defined(FULL_LIGHTING_WITH_EMISSIVE)
#define ENABLE_SPECULAR_DIRECT
#define ENABLE_SPECULAR_INDIRECT
#endif

#ifdef FULL_LIGHTING_WITH_EMISSIVE
#define ENABLE_EMISSIVE
#endif

// Knock out shadows and / or light cookies to see the lighting in isolation
#define ENABLE_SHADOWS
#define ENABLE_LIGHT_COOKIE

#ifdef USE_FULL_UNITY_PBR

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// NOTE: This is needed just to keep things compiling - but it isn't actually used when USE_FULL_UNITY_PBR is enabled
// Samples SH L0 and L1 terms only
half3 SampleSH_Partial(half3 normalWS)
{
    return max(half3(0, 0, 0), SHEvalLinearL0L1(normalWS, unity_SHAr, unity_SHAg, unity_SHAb));
}

half4 DoTileMapLighting(SurfaceData surfaceData, InputData inputData)
{
    // NOTE: Since I knocked this out in the LightingData include it needs to be done here to match
    inputData.bakedGI = SampleSH(inputData.normalWS);

    return UniversalFragmentPBR(inputData, surfaceData);
}

#else

// NOTE: Needed for SampleSH9 which is quite a complicated function!
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SphericalHarmonics.hlsl"

// NOTE: Used by Shadows.hlsl but was being pulled in via BRDF.hlsl which is no longer included
real LerpWhiteTo(real b, real t)
{
    real oneMinusT = 1.0 - t;
    return oneMinusT + b * t;
}

// NOTE: Needed for MainLightRealtimeShadow
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

// NOTE: Needed for SampleMainLightCookie
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LightCookie/LightCookie.hlsl"

// Samples SH L0 and L1 terms only
half3 SampleSH_Partial(half3 normalWS)
{
    return max(half3(0, 0, 0), SHEvalLinearL0L1(normalWS, unity_SHAr, unity_SHAg, unity_SHAb));
}

// Pulled in from EntityLighting.hlsl
#define LIGHTMAP_RGBM_MAX_GAMMA     real(5.0)       // NB: Must match value in RGBMRanges.h
#define LIGHTMAP_RGBM_MAX_LINEAR    real(34.493242) // LIGHTMAP_RGBM_MAX_GAMMA ^ 2.2

#ifdef UNITY_LIGHTMAP_RGBM_ENCODING
    #ifdef UNITY_COLORSPACE_GAMMA
        #define LIGHTMAP_HDR_MULTIPLIER LIGHTMAP_RGBM_MAX_GAMMA
        #define LIGHTMAP_HDR_EXPONENT   real(1.0)   // Not used in gamma color space
    #else
        #define LIGHTMAP_HDR_MULTIPLIER LIGHTMAP_RGBM_MAX_LINEAR
        #define LIGHTMAP_HDR_EXPONENT   real(2.2)
    #endif
#elif defined(UNITY_LIGHTMAP_DLDR_ENCODING)
    #ifdef UNITY_COLORSPACE_GAMMA
        #define LIGHTMAP_HDR_MULTIPLIER real(2.0)
    #else
        #define LIGHTMAP_HDR_MULTIPLIER real(4.59) // 2.0 ^ 2.2
    #endif
    #define LIGHTMAP_HDR_EXPONENT real(0.0)
#else // (UNITY_LIGHTMAP_FULL_HDR)
    #define LIGHTMAP_HDR_MULTIPLIER real(1.0)
    #define LIGHTMAP_HDR_EXPONENT real(1.0)
#endif

real3 UnpackLightmapRGBM(real4 rgbmInput, real4 decodeInstructions)
{
#ifdef UNITY_COLORSPACE_GAMMA
    return rgbmInput.rgb * (rgbmInput.a * decodeInstructions.x);
#else
    return rgbmInput.rgb * (PositivePow(rgbmInput.a, decodeInstructions.y) * decodeInstructions.x);
#endif
}

real3 UnpackLightmapDoubleLDR(real4 encodedColor, real4 decodeInstructions)
{
    return encodedColor.rgb * decodeInstructions.x;
}

real3 DecodeLightmap(real4 encodedIlluminance, real4 decodeInstructions)
{
#if defined(UNITY_LIGHTMAP_RGBM_ENCODING)
    return UnpackLightmapRGBM(encodedIlluminance, decodeInstructions);
#elif defined(UNITY_LIGHTMAP_DLDR_ENCODING)
    return UnpackLightmapDoubleLDR(encodedIlluminance, decodeInstructions);
#else // (UNITY_LIGHTMAP_FULL_HDR)
    return encodedIlluminance.rgb;
#endif
}

#define TEXTURE2D_LIGHTMAP_PARAM TEXTURE2D_PARAM
#define TEXTURE2D_LIGHTMAP_ARGS TEXTURE2D_ARGS
#define SAMPLE_TEXTURE2D_LIGHTMAP SAMPLE_TEXTURE2D
#define LIGHTMAP_EXTRA_ARGS float2 uv
#define LIGHTMAP_EXTRA_ARGS_USE uv

real3 SampleSingleLightmap(TEXTURE2D_LIGHTMAP_PARAM(lightmapTex, lightmapSampler), LIGHTMAP_EXTRA_ARGS, float4 transform, bool encodedLightmap, real4 decodeInstructions)
{
    // transform is scale and bias
    uv = uv * transform.xy + transform.zw;
    real3 illuminance = real3(0.0, 0.0, 0.0);
    // Remark: baked lightmap is RGBM for now, dynamic lightmap is RGB9E5
    if (encodedLightmap)
    {
        real4 encodedIlluminance = SAMPLE_TEXTURE2D_LIGHTMAP(lightmapTex, lightmapSampler, LIGHTMAP_EXTRA_ARGS_USE).rgba;
        illuminance = DecodeLightmap(encodedIlluminance, decodeInstructions);
    }
    else
    {
        illuminance = SAMPLE_TEXTURE2D_LIGHTMAP(lightmapTex, lightmapSampler, LIGHTMAP_EXTRA_ARGS_USE).rgb;
    }
    return illuminance;
}

// Pulled in from GlobalIllumination.hlsl

// NOTE: The names of these are changed from the builtin "unity_Lightmap" etc. this is because the
//       hybrid renderer needs to set them directly and unity does not allow "builtin" material parameters
//       to be set!
#define LIGHTMAP_NAME tilemap_Lightmap
#define LIGHTMAP_INDIRECTION_NAME tilemap_LightmapInd
#define LIGHTMAP_SAMPLER_NAME samplertilemap_Lightmap
#define LIGHTMAP_SAMPLE_EXTRA_ARGS staticLightmapUV

// We also need to declare these since we aren't using the built in ones anymore
TEXTURE2D(tilemap_Lightmap);
SAMPLER(samplertilemap_Lightmap);

half3 SampleLightmap(float2 staticLightmapUV)
{
#ifdef UNITY_LIGHTMAP_FULL_HDR
    bool encodedLightmap = false;
#else
    bool encodedLightmap = true;
#endif

    half4 decodeInstructions = half4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0h, 0.0h);

    // The shader library sample lightmap functions transform the lightmap uv coords to apply bias and scale.
    // However, universal pipeline already transformed those coords in vertex. We pass half4(1, 1, 0, 0) and
    // the compiler will optimize the transform away.
    half4 transformCoords = half4(1, 1, 0, 0);

    float3 diffuseLighting = 0;
    diffuseLighting = SampleSingleLightmap(LIGHTMAP_NAME, LIGHTMAP_SAMPLER_NAME, LIGHTMAP_SAMPLE_EXTRA_ARGS, transformCoords, encodedLightmap, decodeInstructions);
    return diffuseLighting;
}

// NOTE: Pulled in from ImageBasedLighting.hlsl
// This is actually the last mip index, we generate 7 mips of convolution
#define UNITY_SPECCUBE_LOD_STEPS 6

half perceptualRoughnessToMipmapLevel_Internal(half perceptualRoughness)
{
    return perceptualRoughness * UNITY_SPECCUBE_LOD_STEPS;
}

real3 DecodeHDREnvironment_Internal(real4 encodedIrradiance, real4 decodeInstructions)
{
    // Take into account texture alpha if decodeInstructions.w is true(the alpha value affects the RGB channels)
    real alpha = max(decodeInstructions.w * (encodedIrradiance.a - 1.0) + 1.0, 0.0);

    // If Linear mode is not supported we can skip exponent part
    return (decodeInstructions.x * PositivePow(alpha, decodeInstructions.y)) * encodedIrradiance.rgb;
}

half3 GlossyEnvironmentReflection_Internal(half3 reflectVector, float3 positionWS, half perceptualRoughness, half occlusion, float2 normalizedScreenSpaceUV)
{
    half3 irradiance;
    half mip = perceptualRoughnessToMipmapLevel_Internal(perceptualRoughness);
    half4 encodedIrradiance = half4(SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVector, mip));
    irradiance = DecodeHDREnvironment_Internal(encodedIrradiance, unity_SpecCube0_HDR);
    return irradiance * occlusion;
}

// NOTE: Pulled in from BRDF.hlsl
struct BRDFData
{
    half3 albedo;
    half3 diffuse;
    half3 specular;
    half reflectivity;
    half perceptualRoughness;
    half roughness;
    half roughness2;
    half grazingTerm;

    // We save some light invariant BRDF terms so we don't have to recompute
    // them in the light loop. Take a look at DirectBRDF function for detailed explaination.
    half normalizationTerm; // roughness * 4.0 + 2.0
    half roughness2MinusOne; // roughness^2 - 1.0
};

half3 EnvironmentBRDFSpecular_Internal(BRDFData brdfData, half fresnelTerm)
{
    float surfaceReduction = 1.0 / (brdfData.roughness2 + 1.0);
    return half3(surfaceReduction * lerp(brdfData.specular, brdfData.grazingTerm, fresnelTerm));
}

half3 GlobalIllumination_Internal(BRDFData brdfData,
    half3 bakedGI, half occlusion, float3 positionWS,
    half3 normalWS, half3 viewDirectionWS, float2 normalizedScreenSpaceUV)
{
    half3 color = 0.0;
    
#ifdef ENABLE_DIFFUSE_INDIRECT
    color = bakedGI * brdfData.diffuse;
#endif

#ifdef ENABLE_SPECULAR_INDIRECT
    half3 reflectVector = reflect(-viewDirectionWS, normalWS);
    half3 indirectSpecular = GlossyEnvironmentReflection_Internal(reflectVector, positionWS, brdfData.perceptualRoughness, 1.0h, normalizedScreenSpaceUV);
    half NoV = saturate(dot(normalWS, viewDirectionWS));
    half fresnelTerm = Pow4(1.0 - NoV);
    color += indirectSpecular * EnvironmentBRDFSpecular_Internal(brdfData, fresnelTerm);
#endif

    return color * occlusion;
}

// Computes the scalar specular term for Minimalist CookTorrance BRDF
// NOTE: needs to be multiplied with reflectance f0, i.e. specular color to complete
half DirectBRDFSpecular_Internal(BRDFData brdfData, half3 normalWS, half3 lightDirectionWS, half3 viewDirectionWS)
{
    float3 lightDirectionWSFloat3 = float3(lightDirectionWS);
    float3 halfDir = SafeNormalize(lightDirectionWSFloat3 + float3(viewDirectionWS));

    float NoH = saturate(dot(float3(normalWS), halfDir));
    half LoH = half(saturate(dot(lightDirectionWSFloat3, halfDir)));

    // GGX Distribution multiplied by combined approximation of Visibility and Fresnel
    // BRDFspec = (D * V * F) / 4.0
    // D = roughness^2 / ( NoH^2 * (roughness^2 - 1) + 1 )^2
    // V * F = 1.0 / ( LoH^2 * (roughness + 0.5) )
    // See "Optimizing PBR for Mobile" from Siggraph 2015 moving mobile graphics course
    // https://community.arm.com/events/1155

    // Final BRDFspec = roughness^2 / ( NoH^2 * (roughness^2 - 1) + 1 )^2 * (LoH^2 * (roughness + 0.5) * 4.0)
    // We further optimize a few light invariant terms
    // brdfData.normalizationTerm = (roughness + 0.5) * 4.0 rewritten as roughness * 4.0 + 2.0 to a fit a MAD.
    float d = NoH * NoH * brdfData.roughness2MinusOne + 1.00001f;

    half LoH2 = LoH * LoH;
    half specularTerm = brdfData.roughness2 / ((d * d) * max(0.1h, LoH2) * brdfData.normalizationTerm);

    // On platforms where half actually means something, the denominator has a risk of overflow
    // clamp below was added specifically to "fix" that, but dx compiler (we convert bytecode to metal/gles)
    // sees that specularTerm have only non-negative terms, so it skips max(0,..) in clamp (leaving only min(100,...))
#if REAL_IS_HALF
    specularTerm = specularTerm - HALF_MIN;
    // Update: Conservative bump from 100.0 to 1000.0 to better match the full float specular look.
    // Roughly 65504.0 / 32*2 == 1023.5,
    // or HALF_MAX / ((mobile) MAX_VISIBLE_LIGHTS * 2),
    // to reserve half of the per light range for specular and half for diffuse + indirect + emissive.
    specularTerm = clamp(specularTerm, 0.0, 1000.0); // Prevent FP16 overflow on mobiles
#endif

    return specularTerm;
}

half3 LightingPhysicallyBased_Internal(BRDFData brdfData,
    half3 lightColor, half3 lightDirectionWS, half lightAttenuation,
    half3 normalWS, half3 viewDirectionWS)
{
    half NdotL = saturate(dot(normalWS, lightDirectionWS));
    half3 radiance = lightColor * (lightAttenuation * NdotL);

    half3 brdf = 0;

#ifdef ENABLE_DIFFUSE_DIRECT
    brdf += brdfData.diffuse;
#endif
#ifdef ENABLE_SPECULAR_DIRECT
    brdf += brdfData.specular * DirectBRDFSpecular_Internal(brdfData, normalWS, lightDirectionWS, viewDirectionWS);
#else
    // NOTE: This compensates for the lack of specular response 
    brdf += brdfData.specular * NdotL;
#endif

    return brdf * radiance;
}

// NOTE: Pulled in from RealtimeLights.hlsl
// Abstraction over Light shading data.
struct Light
{
    half3 direction;
    half3 color;
    half shadowAttenuation;
};

half3 LightingPhysicallyBased_Internal(BRDFData brdfData, Light light, half3 normalWS, half3 viewDirectionWS)
{
    return LightingPhysicallyBased_Internal(brdfData, light.color, light.direction, light.shadowAttenuation, normalWS, viewDirectionWS);
}

real PerceptualSmoothnessToPerceptualRoughness_Internal(real perceptualSmoothness)
{
    return (1.0 - perceptualSmoothness);
}

real PerceptualRoughnessToRoughness_Internal(real perceptualRoughness)
{
    return perceptualRoughness * perceptualRoughness;
}

inline void InitializeBRDFDataDirect_Internal(half3 albedo, half3 diffuse, half3 specular, half reflectivity, half oneMinusReflectivity, half smoothness, out BRDFData outBRDFData)
{
    outBRDFData = (BRDFData) 0;
    outBRDFData.albedo = albedo;
    outBRDFData.diffuse = diffuse;
    outBRDFData.specular = specular;
    outBRDFData.reflectivity = reflectivity;

    outBRDFData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness_Internal(smoothness);
    outBRDFData.roughness = max(PerceptualRoughnessToRoughness_Internal(outBRDFData.perceptualRoughness), HALF_MIN_SQRT);
    outBRDFData.roughness2 = max(outBRDFData.roughness * outBRDFData.roughness, HALF_MIN);
    outBRDFData.grazingTerm = saturate(smoothness + reflectivity);
    outBRDFData.normalizationTerm = outBRDFData.roughness * half(4.0) + half(2.0);
    outBRDFData.roughness2MinusOne = outBRDFData.roughness2 - half(1.0);
}

// NOTE: Pulled in from BRDF.hlsl
#define kDielectricSpec half4(0.04, 0.04, 0.04, 1.0 - 0.04) // standard dielectric reflectivity coef at incident angle (= 4%)

half OneMinusReflectivityMetallic_Internal(half metallic)
{
    // We'll need oneMinusReflectivity, so
    //   1-reflectivity = 1-lerp(dielectricSpec, 1, metallic) = lerp(1-dielectricSpec, 0, metallic)
    // store (1-dielectricSpec) in kDielectricSpec.a, then
    //   1-reflectivity = lerp(alpha, 0, metallic) = alpha + metallic*(0 - alpha) =
    //                  = alpha - metallic * alpha
    half oneMinusDielectricSpec = kDielectricSpec.a;
    return oneMinusDielectricSpec - metallic * oneMinusDielectricSpec;
}

inline void InitializeBRDFData_Internal(half3 albedo, half metallic, half3 specular, half smoothness, out BRDFData outBRDFData)
{
    half oneMinusReflectivity = OneMinusReflectivityMetallic_Internal(metallic);
    half reflectivity = half(1.0) - oneMinusReflectivity;
    half3 brdfDiffuse = albedo * oneMinusReflectivity;
    half3 brdfSpecular = lerp(kDielectricSpec.rgb, albedo, metallic);

    InitializeBRDFDataDirect_Internal(albedo, brdfDiffuse, brdfSpecular, reflectivity, oneMinusReflectivity, smoothness, outBRDFData);
}

// NOTE: Pulled in from SurfaceData.hlsl
struct SurfaceData
{
    half3 albedo;
    half3 specular;
    half metallic;
    half smoothness;
    half3 normalTS;
#ifdef ENABLE_EMISSIVE
    half3 emission;
#endif
    half occlusion;
#ifdef CUSTOM_LIGHTING
    half3 lightDirection;
    half3 lightColour;
    half shadow;
#endif
};

inline void InitializeBRDFData_Internal(inout SurfaceData surfaceData, out BRDFData brdfData)
{
    InitializeBRDFData_Internal(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, brdfData);
}

half4 UniversalFragmentPBR_Internal(InputData inputData, SurfaceData surfaceData)
{
    BRDFData brdfData;

    InitializeBRDFData_Internal(surfaceData, brdfData);

    // NOTE: Copy of the innards of GetLight with bits we don't need removed
    Light mainLight;
// In the WorldMap we use custom lighting - we don't want to pick up the tile map lighting setup
#ifdef CUSTOM_LIGHTING
    mainLight.direction = surfaceData.lightDirection;
    mainLight.color = surfaceData.lightColour;
    mainLight.shadowAttenuation = surfaceData.shadow;
#else
    mainLight.direction = half3(_MainLightPosition.xyz);
    #ifdef ENABLE_SHADOWS
        mainLight.shadowAttenuation = MainLightRealtimeShadow(inputData.shadowCoord);
    #else
        mainLight.shadowAttenuation = 1.0;
    #endif
    mainLight.color = _MainLightColor.rgb;
    #ifdef ENABLE_LIGHT_COOKIE
        real3 cookieColor = SampleMainLightCookie(inputData.positionWS);
        mainLight.color *= cookieColor;
    #endif
#endif

    half3 lightingColor = 
        GlobalIllumination_Internal(brdfData,
                                    inputData.bakedGI, surfaceData.occlusion, inputData.positionWS,
                                    inputData.normalWS, inputData.viewDirectionWS, inputData.normalizedScreenSpaceUV);
    
    lightingColor +=
        LightingPhysicallyBased_Internal(brdfData,
                                         mainLight,
                                         inputData.normalWS, inputData.viewDirectionWS);

#ifdef ENABLE_EMISSIVE
    lightingColor += surfaceData.emission;
#endif

    half4 finalColor = half4(lightingColor, 1.0);

#if REAL_IS_HALF
    // Clamp any half.inf+ to HALF_MAX
    return min(finalColor, HALF_MAX);
#else
    return finalColor;
#endif
}

half4 DoTileMapLighting(SurfaceData surfaceData, InputData inputData)
{
    return UniversalFragmentPBR_Internal(inputData, surfaceData);
}

#endif // USE_FULL_UNITY_PBR_LIGHTING

#endif
