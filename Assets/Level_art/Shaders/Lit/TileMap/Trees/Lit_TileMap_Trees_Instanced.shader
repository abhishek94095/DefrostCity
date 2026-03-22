Shader "WBGB/Lit/TileMap/Trees Instanced"
{
    Properties
    {
        [NoScaleOffset]
        _MainTex("Diffuse (RGBA)", 2D) = "white" {}
        _Amplitude("Amplitude", Vector) = (0, 0, 0, 0)
        _Speed("Speed", Vector) = (0, 0, 0, 0)
        _Frequency("Frequency", Vector) = (0, 0, 0, 0)
        _BaseColor("Base Color Tint", Color) = (1, 1, 1, 1)
        _BaseColorIntensity("Base Color Intensity", Float) = 1
        _Color("Color Tint", Color) = (1, 1, 1, 1)
        _ColorIntensity("Color Intensity", Float) = 1
        _ColorVariation("Color Variation Spread", Float) = 1
        _LightIntensity("Light Intensity", Float) = 1

        // Hidden cull mode property - used to support mirrored rendering
        // which flips the geometry inside out
        [HideInInspector]
        _Cull("__cull", Float) = 2.0
    }
    SubShader
    {
        Tags { "Queue" = "AlphaTest-10" "IgnoreProjector" = "True" "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        
        Pass 
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include_with_pragmas "../../TileMap_MultiCompileOptions_LightingPass.hlsl"

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling

            #define FORCE_INSTANCING_ON
            #include "Lit_TileMap_Trees_Input.hlsl"

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

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling

            #include "../TileMap_Shadows.hlsl"

            ENDHLSL
        }
    }
}
