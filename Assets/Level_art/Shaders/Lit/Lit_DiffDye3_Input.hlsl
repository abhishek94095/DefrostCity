#ifndef LITDIFFDYE3_INPUT_INCLUDED
#define LITDIFFDYE3_INPUT_INCLUDED

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

// Some troops, e.g. Cavalry use dye per instance
// Most don't so don't need to do this
#if defined(PER_INSTANCE_DYE) && defined(INSTANCING_ON)
// This value doesn't change per instance
half _Dye25;

UNITY_INSTANCING_BUFFER_START(DyeInstanceBuffer)
    UNITY_DEFINE_INSTANCED_PROP(half4, _Dye75)
    UNITY_DEFINE_INSTANCED_PROP(half4, _Dye50)
UNITY_INSTANCING_BUFFER_END(DyeInstanceBuffer)
#else
CBUFFER_START(UnityPerMaterial)
uniform half4 _Dye75;
uniform half4 _Dye50;
uniform half _Dye25;
float _GlowStrength;
float4 _GlowColor;
float _DissolveAmount;
CBUFFER_END
#endif

// Will return 1.0 if the mask is close to the value
// 0.0 otherwise
half colorMask(half mask, half value)
{
    // NOTE: A tolerance of 0.055 matches the previous behaviour that
    //       was skewed by a 3d distance calculation 
    //       - verified using graphtoy.com to compare the original behaviour to the new
    return step(abs(mask - value), 0.055);
}

half3 Dye(half2 uv)
{
    half4 diffuse = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
    half alpha = diffuse.a;
    // If alpha is close to 1.0 then they dye will be white and the texture diffuse colour will be used
    half3 dye = colorMask(alpha, 1.0);

#if defined(PER_INSTANCE_DYE) && defined(INSTANCING_ON)
    half4 _Dye75 = UNITY_ACCESS_INSTANCED_PROP(DyeInstanceBuffer, _Dye75);
    half4 _Dye50 = UNITY_ACCESS_INSTANCED_PROP(DyeInstanceBuffer, _Dye50);
#endif

    // If alpha is close to 0.7 then the dye will be the _Dye75 colour 
    // - the texture diffuse colour is expected to be greyscale
    dye += colorMask(alpha, 0.7) * _Dye75.rgb;
    // If alpha is close to 0.4 then the dye will be the _Dye50 colour 
    // - the texture diffuse colour is expected to be greyscale
    dye += colorMask(alpha, 0.4) * _Dye50.rgb;
    // If alpha is close to 0.2 then the dye will be the _Dye25 greyscale 
    // - the texture diffuse colour is expected to be greyscale
    dye += colorMask(alpha, 0.2) * _Dye25.rrr;

    // Multiply with the texture diffuse
    // Anything that doesn't meet the dye alpha values will be black
    return dye * diffuse.rgb;
}

#endif
