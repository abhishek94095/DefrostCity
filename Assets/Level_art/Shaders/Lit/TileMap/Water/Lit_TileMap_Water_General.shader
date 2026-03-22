Shader "WBGB/Lit/TileMap/Water General"
{
    Properties 
    {
        [NoScaleOffset]
        _MacroWater("Macro Water Color (RGB), Blend (A)", 2D) = "white" {}
        [NoScaleOffset]
        _Water("Water (RGB)", 2D) = "white" {}
        _WaterTiling("Water Tiling", Float) = 1
        [NoScaleOffset]
        _Coast("Coast (RGB)", 2D) = "white" {}
        _CoastTiling("Coast Tiling", Float) = 1
        _NormalTiling("Normal Tiling", Float) = 1
        [NoScaleOffset][Normal]
        _NormalA("Normal Map", 2D) = "bump" {}
        [NoScaleOffset]
        _FlowMap("Flow Map (RG), Noise (B)", 2D) = "white" {}

        // Hidden cull mode property - used to support mirrored rendering
        // which flips the geometry inside out
        [HideInInspector]
        _Cull("__cull", Float) = 2.0
    }
    SubShader 
    {
        Tags { "Queue" = "AlphaTest-30" "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Pass 
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include_with_pragmas "../../TileMap_MultiCompileOptions_LightingPass.hlsl"

            #include "Lit_TileMap_Water_Input.hlsl"


            ENDHLSL
        }
    }
}