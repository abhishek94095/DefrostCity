#ifndef LITDIFFDYEHUESATSHIFT_INPUT_INCLUDED
#define LITDIFFDYEHUESATSHIFT_INPUT_INCLUDED

TEXTURE2D(_IridTex);
SAMPLER(sampler_IridTex);

half3 hueShift(half value, half3 diffuse)
{
    half3 color = RgbToHsv(diffuse);
    half hue = color.x + value;
    color.x = RotateHue(hue, 0.0, 1.0);
    return HsvToRgb(color);
}

half3 colorMask(half mask, half value, half range, half fuzziness)
{
    half maskValue = distance(half3(value, value, value), mask);
    half3 color = saturate(1.0 - (maskValue - range) / max(fuzziness, 1e-5));
    return color;
}

half3 reScale(half mask, half2 InMinMax, half2 OutMinMax)
{
    return OutMinMax.x + (mask - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
}

half3 HueSatShift(half3 diffuse, half dyeMask, half3 hueSatLevel)
{
    //Hue Shift
    half3 color = hueShift(hueSatLevel.x, diffuse - colorMask(dyeMask, 0.15, 0.1, 0.1));

    //Sat-Desat Shift
    half lum = Luminance(color);
    color = (color - lum) * hueSatLevel.y + lum;

    //Mid Level Shift
    half level = 1.0 / hueSatLevel.z;
    color = pow(abs(color), half3(level, level, level));

    return color;
}

half3 Dye(half3 color, half3 diffuse, half dyeMask, half3 dye30, half3 dye50, half3 dye60, half3 dye80, half hueAdjust90)
{
    //Dyeables

    // Get 0% dye (body) 
    half3 dye = colorMask(dyeMask, 0, 0, 0.1);
    // Add gradients for claws and horns with their custom masks
    dye += lerp(dye30, dye50, reScale(dyeMask, half2(0.3, 0.5), half2(0, 1.0))) * colorMask(dyeMask, 0.4, 0.2, 0.1);
    dye += lerp(dye60, dye80, reScale(dyeMask, half2(0.6, 0.8), half2(0, 1.0))) * colorMask(dyeMask, 0.7, 0.2, 0.1);
    //dye += step(0.15, colorMask(dyeMask, 0.15, 0.02, 0.12)) * dye15; //speckles removed
    
    // Multiply in the color 
    dye *= color;
    // Add hue shift for eye color
    dye += hueShift(hueAdjust90, colorMask(dyeMask, 0.9, 0.05, 0.1) * diffuse);
    // Add no dye mask section
    dye += (colorMask(dyeMask, 0.15, 0.1, 0.1) * diffuse);
    
    return dye;
}

half Fresnel(half3 normal, half3 normalWS, half3 positionWS, half fresnelPower)
{
    half fresnel = pow((1.0 - saturate(dot(normalize(normalWS), normalize(GetWorldSpaceNormalizeViewDir(positionWS))))), fresnelPower);
    return fresnel;
}

half3 Irid(half iridMask, half fresnel, half iridHueAdjust)
{
    //Fresnel & Irid
    half3 irid = SAMPLE_TEXTURE2D(_IridTex, sampler_IridTex, half2(fresnel, fresnel)).rgb * iridMask;
    irid = hueShift(iridHueAdjust, irid);

    return irid;
}

half3 ApplyColorIrid(half3 color, half3 irid, half fresnel)
{
    color = lerp(color, irid, (1 - fresnel) * fresnel);

    return color;
}


#endif
