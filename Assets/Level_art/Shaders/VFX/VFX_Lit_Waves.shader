Shader "WBGB/VFX/Lit Waves" 
{
    Properties 
    {
        [NoScaleOffset]
        _BaseNoise("Noise (RG)", 2D) = "white" {}
        _Color1("Color Base", Color) = (1,1,1,1)
        _Color2("Color Edge", Color) = (1,1,1,1)
        _SpeedIn("Speed In", Float) = 1
        _SpeedSin("Speed Sin", Float) = 1
        [NoScaleOffset]
        _Mask("Mask (R)", 2D) = "white" {}
        _WavesOn("Waves", Float) = 0
        _SplashOn("Splash", Float) = 0
    }
    SubShader 
    {
        Tags { "IgnoreProjector" = "True" "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Pass 
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite Off
            Cull Off
            Blend SrcAlpha One

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma only_renderers d3d11 glcore gles gles3 metal vulkan
            #pragma target 2.0

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #include "VFX_Lit_Waves_Input.hlsl"

            #include_with_pragmas "../Lit/TileMap_MultiCompileOptions_LightingPass.hlsl"

            ENDHLSL
        }
    }
}
