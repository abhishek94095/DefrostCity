Shader "WBGB/VFX/VertColor Trp Noise Scroll"
{
    Properties
    {
        _MainTex("Diffuse (RGBA)", 2D) = "white" {}
        _Noise("Noise (R)", 2D) = "white" {}
        _ScrollSpeedU("Scroll Speed U", Float) = 0
        _ScrollSpeedV("Scroll Speed V", Float) = 0
        _NoisePower("Noise Power", Float) = 0.1
        _RColor("R Color", Color) = (0.5,0.5,0.5,1)
        _GColor("G Color", Color) = (0.5,0.5,0.5,1)
        _AColor("A Color", Color) = (0.5,0.5,0.5,1)
        _Color("Overall Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "IgnoreProjector" = "True" "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass 
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            #pragma only_renderers d3d11 glcore gles gles3 metal vulkan
            #pragma target 2.0

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_Noise);
            SAMPLER(sampler_Noise);

            CBUFFER_START(UnityPerMaterial)
            uniform float4 _MainTex_ST;
            uniform float4 _Noise_ST;
            uniform half _ScrollSpeedU;
            uniform half _ScrollSpeedV;
            uniform half _NoisePower;
            uniform half4 _RColor;
            uniform half4 _GColor;
            uniform half4 _AColor;
            uniform half4 _Color;
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

                OUT.uv = IN.texcoord;
                OUT.vertexColor = IN.vertexColor;
                OUT.positionCS = TransformObjectToHClip(IN.vertex.xyz);

                return OUT;
            }

            half4 frag(VertexOutput IN) : COLOR 
            {
                float2 uvScroll = IN.uv + float2((_ScrollSpeedU * _Time.y), (_Time.y * _ScrollSpeedV));
                half noise = SAMPLE_TEXTURE2D(_Noise, sampler_Noise, (TRANSFORM_TEX(uvScroll, _Noise))).r;
                float2 noiseScroll = float2(IN.uv.r,((_NoisePower * noise * IN.uv.g) + IN.uv.g));
                half4 diffuse = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, TRANSFORM_TEX(noiseScroll, _MainTex));

                half3 finalColor = IN.vertexColor.rgb * ((diffuse.r * _RColor.rgb * _RColor.a) + (diffuse.g * _GColor.rgb * _GColor.a) + (diffuse.a * _AColor.rgb * _AColor.a));
                finalColor *= _Color.rgb;
                half alpha = IN.vertexColor.a * diffuse.b;

                return half4(finalColor, alpha * _Color.a);
            }
            ENDHLSL
        }
    }
}
