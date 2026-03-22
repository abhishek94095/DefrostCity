Shader "WBGB/Lit/DiffDye BannerComposite" 
{
    Properties
    {
        [NoScaleOffset]
        _MainTex("Diffuse (RGB), Dye (A)", 2D) = "white" {}
        _BackgroundColor("Background Color", Color) = (1, 1, 1, 1)
        _PatternColor("Pattern Color", Color) = (1, 1, 1, 1)
        _PatternTex("Pattern Texture", 2D) = "white" {}
        _SigilColor("Sigil Dye Color", Color) = (1, 1, 1, 1)
        _SigilTex("Dyeable Sigil", 2D) = "white" {}
        [NoScaleOffset]
        _SigilBaseTex("Non-Dyeable Sigil Base", 2D) = "black" {}
        _SigilBaseAlpha("Sigil Base Alpha", Float) = 1
    }
    SubShader
    {
        Tags { "IgnoreProjector" = "True" "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        
        Pass 
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            #include_with_pragmas "TileMap_MultiCompileOptions_LightingPass.hlsl"
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_PatternTex);
            SAMPLER(sampler_PatternTex);
            TEXTURE2D(_SigilTex);
            SAMPLER(sampler_SigilTex);
            TEXTURE2D(_SigilBaseTex);
            SAMPLER(sampler_SigilBaseTex);

            CBUFFER_START(UnityPerMaterial)
            uniform half4 _PatternTex_ST;
            uniform half4 _SigilTex_ST;
            uniform half4 _PatternColor;
            uniform half4 _BackgroundColor;
            uniform half4 _SigilColor;
            uniform half _SigilBaseAlpha;
            CBUFFER_END

            struct VertexInput 
            {
                half4 vertex                : POSITION;
                half3 normal                : NORMAL;
                half4 tangent               : TANGENT;
                float2 texcoord              : TEXCOORD0;
                half4 vertexColor           : COLOR;
                float2 staticLightmapUV     : TEXCOORD1;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VertexOutput 
            {
                half4 positionCS            : SV_POSITION;
                float3 positionWS           : TEXCOORD0;
                half3 normalWS              : TEXCOORD1;
                float2 uv                    : TEXCOORD2;
                half2 uvBanner              : TEXCOORD3;
                half2 uvSigil              : TEXCOORD4;
                half4 vertexColor           : COLOR;
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 6);

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            inline void InitializeSurfaceData(out SurfaceData outSurfaceData, half3 albedo)
            {
                outSurfaceData.alpha = 1.0;
                outSurfaceData.albedo = albedo;

                outSurfaceData.metallic = 0;
                outSurfaceData.specular = half3(0.0, 0.0, 0.0);

                outSurfaceData.smoothness = 0;
                outSurfaceData.normalTS = half3(0.0, 0.0, 0.0);
                outSurfaceData.occlusion = 1;
                outSurfaceData.emission = 0;

                outSurfaceData.clearCoatMask = half(0.0);
                outSurfaceData.clearCoatSmoothness = half(0.0);
            }

            void InitializeInputData(VertexOutput IN, out InputData inputData)
            {
                inputData = (InputData)0;

                inputData.positionWS = IN.positionWS;
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);

                inputData.normalWS = IN.normalWS;
                inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
                inputData.viewDirectionWS = viewDirWS;

                inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(IN.staticLightmapUV);
                inputData.bakedGI = SAMPLE_GI(IN.staticLightmapUV, IN.vertexSH, inputData.normalWS);
            }
            
            half3 ColorMask(half3 In, half3 MaskColor)
            {
                float mask = distance(MaskColor, In);
                half4 result = saturate(1 - (mask - 0.2) / max(0.1, 1e-5));
                return result.rgb;
            }
            
            VertexOutput vert(VertexInput IN) 
            {
                VertexOutput OUT = (VertexOutput)0;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normal, IN.tangent);
                OUT.normalWS = normalInputs.normalWS;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.vertex.xyz);
                OUT.positionCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;

                OUT.uv = IN.texcoord;
                OUT.uvBanner = TRANSFORM_TEX(IN.texcoord, _PatternTex);
                OUT.uvSigil = TRANSFORM_TEX(IN.texcoord, _SigilTex);
                OUTPUT_SH(OUT.normalWS, OUT.vertexSH);

                return OUT;
            }

            half4 frag(VertexOutput IN) : COLOR
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                
                half4 diffuse = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                // Get banner textures
                half4 foreground = SAMPLE_TEXTURE2D(_PatternTex, sampler_PatternTex, IN.uvBanner) * _PatternColor;
                half4 sigil = SAMPLE_TEXTURE2D(_SigilTex, sampler_SigilTex, IN.uvSigil) * _SigilColor;
                half4 sigilBase = SAMPLE_TEXTURE2D(_SigilBaseTex, sampler_SigilBaseTex, IN.uvSigil);

                // Now we can get the two colors by using a LERP between the background and foreground, with the alpha being the lerp amount
                half4 banner = lerp(_BackgroundColor, foreground, foreground.a) * 1.5;

                // Lerp the current pixel with the sigil color at this pixel, lerp by the sigil alpha value of this pixel
                banner = lerp(banner, sigil, sigil.a);

                // Then we lerp this new pixel value with the current pixel value, based on the alpha of the undyeable sigil pixel
                banner = lerp(banner, sigilBase, sigilBase.a * _SigilBaseAlpha);

                half3 alpha = half3(diffuse.a, diffuse.a, diffuse.a);
                half3 dye = ColorMask(alpha, half3(1.0, 1.0, 1.0));
                dye += ColorMask(alpha, half3(0.7, 0.7, 0.7)) * banner.rgb;
                dye += ColorMask(alpha, half3(0.5, 0.5, 0.5)) * _BackgroundColor.rgb;
                
                //PBR Lighting Setup
                SurfaceData surfaceData;
                InitializeSurfaceData(surfaceData, diffuse.rgb * dye);
                InputData inputData;
                InitializeInputData(IN, inputData);

                half4 litColor = UniversalFragmentPBR(inputData, surfaceData);

                // brightening up a little bit to account for drastic shadowing
                return litColor;
            }
            ENDHLSL
        }

        //Shadow Caster Pass
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow

            #include_with_pragmas "TileMap_MultiCompileOptions_ShadowPass.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
            uniform half4 _PatternTex_ST;
            uniform half4 _PatternColor;
            uniform half4 _BackgroundColor;
            uniform half4 _SigilColor;
            uniform half _SigilBaseAlpha;
            uniform half4 _Amplitude;
            uniform half4 _Frequency;
            uniform half4 _Speed;
            CBUFFER_END

            float3 _LightDirection;
            float3 _LightPosition;

            struct VertexInput
            {
                float4 positionOS               : POSITION;
                float3 normalOS                 : NORMAL;
                float2 texcoord                 : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VertexOutput
            {
                float2 uv                       : TEXCOORD0;
                float4 positionCS               : SV_POSITION;
            };

            float4 GetShadowPositionHClip(VertexInput IN)
            {
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);

            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif

                return positionCS;
            }

            VertexOutput vertShadow(VertexInput IN)
            {
                VertexOutput OUT;
                UNITY_SETUP_INSTANCE_ID(IN);

                OUT.uv = IN.texcoord;
                OUT.positionCS = GetShadowPositionHClip(IN);
                return OUT;
            }

            half4 fragShadow(VertexOutput IN) : SV_TARGET
            {
                return 0;
            }

            ENDHLSL
        }
    }
}
