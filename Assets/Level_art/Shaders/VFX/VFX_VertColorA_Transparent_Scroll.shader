Shader "WBGB/VFX/VertColorA Trp Scroll" 
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _ScrollSpeedU("Scroll Speed U", Float) = 0
        _ScrollSpeedV("Scroll Speed V", Float) = 0
        _Color("Color", Color) = (1,1,1,0)
        _Intensity("Intensity", Float) = 1

        // Hidden cull mode property - used to support mirrored rendering
        // which flips the geometry inside out
        [HideInInspector]
        _Cull("__cull", Float) = 2.0
    }
    SubShader
    {
        Tags { "IgnoreProjector" = "True" "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        
        Pass 
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull [_Cull]

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            #pragma only_renderers d3d11 glcore gles gles3 metal vulkan
            #pragma target 2.0

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            uniform half _ScrollSpeedU;
            uniform half _ScrollSpeedV;
            uniform float4 _MainTex_ST;
            uniform half4 _Color;
            uniform half _Intensity;
            CBUFFER_END

            struct VertexInput 
            {
                half4 vertex            : POSITION;
                float2 texcoord          : TEXCOORD0;
                half4 vertexColor       : COLOR;
            };
            
            struct VertexOutput 
            {
                half4 positionCS        : SV_POSITION;
                float2 uv                : TEXCOORD0;
                half4 vertexColor       : COLOR;
            };

            VertexOutput vert(VertexInput IN) 
            {
                VertexOutput OUT = (VertexOutput)0;

                OUT.uv = TRANSFORM_TEX(IN.texcoord, _MainTex);
                OUT.uv.xy += frac(_Time.y * float2(_ScrollSpeedU, _ScrollSpeedV));

                OUT.positionCS = TransformObjectToHClip(IN.vertex.xyz);
                OUT.vertexColor = IN.vertexColor;
                return OUT;
            }

            half4 frag(VertexOutput IN) : COLOR 
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                color *= IN.vertexColor.a * color.a * _Color * _Intensity;

                return color;
            }
            ENDHLSL
        }
    }
}
