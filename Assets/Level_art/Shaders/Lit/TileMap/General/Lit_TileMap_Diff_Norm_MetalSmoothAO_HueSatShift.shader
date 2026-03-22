Shader "WBGB/Lit/TileMap/Diff Norm MSA Hue"
{
    Properties
    {
        [NoScaleOffset]
        [MainTexture] _BaseMap("Main Texture", 2D) = "white" {}
        [NoScaleOffset]
        _BumpMap("Normal Map", 2D) = "bump" {}
        [NoScaleOffset]
        _MetalSmoothAO("Metallic (R), Smoothness (G), Ambient Occlusion (B)", 2D) = "black"{}

        _HueAdjust("Hue Adjust", Range(0, 1)) = 0
        _SatAdjust("Saturation Adjust", Range(0, 2)) = 1
        _LevelAdjust("Level Adjust", Range(0, 2)) = 1

        // Hidden cull mode property - used to support mirrored rendering
        // which flips the geometry inside out
        [HideInInspector]
        _Cull("__cull", Float) = 2.0

        // Hidden light map property, required to support the hybrid renderer
        [HideInInspector]
        tilemap_Lightmap ("", 2D) = "white" {}
    }
    SubShader
    {
        Tags{"RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}

        Pass
        {
            Name "ForwardLit"
            Tags{"LightMode" = "UniversalForward"}

            Blend One Zero
            Cull [_Cull]

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            #include_with_pragmas "../../TileMap_MultiCompileOptions_LightingPass.hlsl"

            TEXTURE2D(_MetalSmoothAO);
            SAMPLER(sampler_MetalSmoothAO);
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            CBUFFER_START(UnityPerMaterial)
            half _HueAdjust;
            half _SatAdjust;
            half _LevelAdjust;
            float4 tilemap_Lightmap_ST;
            float _Cull;
            CBUFFER_END

            #define CAN_BE_MIRRORED
            #define FULL_LIGHTING_NO_EMISSIVE
            #include "../TileMap_Lighting.hlsl"
            #include "../TileMap_Diff_Norm_MSA.hlsl"

            half4 frag(VertexOutput IN) : SV_Target
            {
                half4 diffuse = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, IN.uv)).rgb;
                half3 msa = SAMPLE_TEXTURE2D(_MetalSmoothAO, sampler_MetalSmoothAO, IN.uv).rgb;

                diffuse.rgb = DoHueSatShift(diffuse.rgb, _HueAdjust, _SatAdjust, _LevelAdjust);

                // Lighting
                SurfaceData surfaceData;
                InputData inputData;
                InitialiseLightingData(IN, normalTS, diffuse.rgb, msa, surfaceData, inputData);
                return DoTileMapLighting(surfaceData, inputData);
            }

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow

            #include_with_pragmas "../../TileMap_MultiCompileOptions_ShadowPass.hlsl"

            #include "../TileMap_Shadows.hlsl"

            ENDHLSL
        }
    }
}
