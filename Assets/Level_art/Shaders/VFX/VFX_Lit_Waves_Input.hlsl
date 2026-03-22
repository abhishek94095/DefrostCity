TEXTURE2D(_BaseNoise);
SAMPLER(sampler_BaseNoise);
TEXTURE2D(_Mask);
SAMPLER(sampler_Mask);

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
uniform float4 _BaseNoise_ST;
uniform half4 _Color1;
uniform half4 _Color2;
uniform half _SpeedIn;
uniform half _SpeedSin;
uniform float4 _Mask_ST;
uniform half _WavesOn;
uniform half _SplashOn;
#if !defined(FORCE_INSTANCING_ON)
CBUFFER_END
#endif

struct VertexInput
{
    float4 vertex : POSITION;
    float2 texcoord : TEXCOORD0;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    float3 normalWS : TEXCOORD2;
    float3 tangentWS : TEXCOORD3;
    float3 bitangentWS : TEXCOORD4;
    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 5);
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

inline void InitializeSurfaceData(out SurfaceData outSurfaceData, half4 albedoAlpha)
{
    outSurfaceData.alpha = albedoAlpha.a;
    outSurfaceData.albedo = albedoAlpha.rgb;

    outSurfaceData.metallic = 0;
    outSurfaceData.specular = half3(0.0, 0.0, 0.0);

    outSurfaceData.smoothness = 0;
    outSurfaceData.normalTS = half3(0.5, 0.5, 0.5);
    outSurfaceData.occlusion = 1;
    outSurfaceData.emission = 0;

    outSurfaceData.clearCoatMask = half(0.0);
    outSurfaceData.clearCoatSmoothness = half(0.0);
}

void InitializeInputData(VertexOutput IN, half3 normalTS, out InputData inputData)
{
    inputData = (InputData) 0;

    inputData.positionWS = IN.positionWS;
    float3 viewDirWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);

    half3x3 tangentTransform = half3x3(IN.tangentWS, IN.bitangentWS, IN.normalWS);
    inputData.tangentToWorld = tangentTransform;
    inputData.normalWS = TransformTangentToWorld(normalTS, tangentTransform);
    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    inputData.viewDirectionWS = viewDirWS;

    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
    inputData.bakedGI = SAMPLE_GI(IN.staticLightmapUV, IN.vertexSH, inputData.normalWS);
}

VertexOutput vert(VertexInput IN)
{
    VertexOutput OUT = (VertexOutput) 0;
    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

    VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.vertex.xyz);
    OUT.positionCS = positionInputs.positionCS;
    OUT.positionWS = positionInputs.positionWS;
                
    VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normal, IN.tangent);
    OUT.normalWS = normalInputs.normalWS;
    OUT.tangentWS = normalInputs.tangentWS;
    OUT.bitangentWS = normalInputs.bitangentWS;

    OUT.uv = IN.texcoord;
                
    OUTPUT_SH(OUT.normalWS, OUT.vertexSH);
    return OUT;
}

half4 frag(VertexOutput IN) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(IN);

    // WAVES
    // Get and transform all noise tex samples
    float wNoise1 = SAMPLE_TEXTURE2D(_BaseNoise, sampler_BaseNoise, TRANSFORM_TEX(float2(IN.uv.x + sin(_SpeedSin / 50 * _Time.y), IN.uv.y * 2.5 + sin(_Time.y * _SpeedSin) * 0.2), _BaseNoise)).r * 2;
    float wNoise2 = SAMPLE_TEXTURE2D(_BaseNoise, sampler_BaseNoise, TRANSFORM_TEX(float2(IN.uv.x, IN.uv.y), _BaseNoise) + _Time.y * -_SpeedIn).r * 2;
    float wNoise3 = SAMPLE_TEXTURE2D(_BaseNoise, sampler_BaseNoise, IN.positionWS.rb * 1.5 + _Time.y * float2(-0.01, 0.01)).g * 3;

    // Create a uv mask based on v position in uv space
    float wMask = (abs(IN.uv.y) - 0.8) + pow(abs(IN.uv.y), 20);
    // Multiple lerped color with wave and intensity
    float4 wave = lerp(_Color1, _Color2, wMask);
    // Add together all noise and mask values
    wave *= (wNoise1 + wNoise2 + wNoise3 + wMask) * 1.2;
    wave.a *= wMask;

    // SPLASH
    // Set movement values for uvs
    float sMovement = IN.uv.y + sin(_Time.y * 1.3);
    // Get and transform noise samples
    float sNoise1 = SAMPLE_TEXTURE2D(_BaseNoise, sampler_BaseNoise, TRANSFORM_TEX(float2(IN.uv.x, cos(_Time.y) * -0.15 + IN.uv.y * 0.3), _BaseNoise)).r;
    float sNoise2 = SAMPLE_TEXTURE2D(_BaseNoise, sampler_BaseNoise, TRANSFORM_TEX(float2(IN.uv.x, sMovement * 0.6), _BaseNoise)).r;

    // Set splash values with noise and movements
    float sValue = 1 - smoothstep(sNoise1 + sNoise2, sNoise1 + sNoise2 + 1.1, sMovement + 0.65 * 2.2);
    sValue *= (1 - IN.uv.y) - 0.05;
    // Lerp colors along splash
    float4 splash = lerp(_Color1, _Color2, sValue);

    // Adjust the alpha to apply the mask
    float mask = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, IN.positionWS.rb * 0.05 + _Time.y * 0.02).r;
    float4 color = (wave * _WavesOn) + (splash * _SplashOn);
    color.a *= mask;

    SurfaceData surfaceData;
    InitializeSurfaceData(surfaceData, color);
    InputData inputData;
    InitializeInputData(IN, surfaceData.normalTS, inputData);

    float4 finalColor = UniversalFragmentPBR(inputData, surfaceData);
    finalColor.rgb = MixFog(finalColor.rgb, inputData.fogCoord);

    return finalColor;
}
