Shader "WBGB/Lit/Dye/DiffDye6 Norm MSA Hue" 
{
    Properties
    {
        [NoScaleOffset]
        _MainTex("Diffuse (RGB), Dye (A)", 2D) = "white" {}
        [NoScaleOffset]
        _NormalTex("Normal", 2D) = "bump" {}
        [NoScaleOffset]
        _MetalSmoothAOGlowTex("Metallic (R), Smoothness (G), Ambient Occlusion (B), Glow (A)", 2D) = "white" {}
        [NoScaleOffset]
        _PatternTex("Pattern (Greyscale)", 2D) = "black" {}
        _PatternTexIntensity("Pattern Intensity", Range(0,1)) = 1
        _HueAdjust("Hue Adjust", Range(0, 1)) = 0
        _SatAdjust("Saturation Adjust", Range(0, 2)) = 1
        _LevelAdjust("Level Adjust", Range(0, 2)) = 1
        _Dye30("Dye Color 30% - Claw Base", Color) = (1, 1, 1, 1)
        _Dye50("Dye Color 50% - Claw Tip", Color) = (1, 1, 1, 1)
        _Dye60("Dye Color 60% - Horn Base", Color) = (1, 1, 1, 1)
        _Dye80("Dye Color 80% - Horn Tip", Color) = (1, 1, 1, 1)
        _90HueAdjust("90% - Hue Adjust - Eye", Range(0, 1)) = 0
        _EmissionColor("Emission Color", Color) = (0,0,0,1)
        _EmissionIntensity ("Emission Intensity", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipline" = "UniversalPipeline" }

        Pass 
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Lit_DiffDye6_HueSatShift_Input.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            #include_with_pragmas "DragonPit_MultiCompileOptions_LightingPass.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_MetalSmoothAOGlowTex);
            SAMPLER(sampler_MetalSmoothAOGlowTex);
            TEXTURE2D(_NormalTex);
            SAMPLER(sampler_NormalTex);
            TEXTURE2D(_PatternTex);
            SAMPLER(sampler_PatternTex);

            CBUFFER_START(UnityPerMaterial)
            uniform half _PatternTexIntensity;
            uniform half _HueAdjust;
            uniform half _SatAdjust;
            uniform half _LevelAdjust;
            uniform half4 _Dye30;
            uniform half4 _Dye50;
            uniform half4 _Dye60;
            uniform half4 _Dye80;
            uniform half _90HueAdjust;
            half4 _EmissionColor;
            half _EmissionIntensity;
            CBUFFER_END

            struct VertexInput
            {
                half4 vertex            : POSITION;
                half3 normal            : NORMAL;
                half4 tangent           : TANGENT;
                half2 uv                : TEXCOORD0;
                half2 uv2               : TEXCOORD1;
                float2 staticLightmapUV : TEXCOORD2;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VertexOutput
            {
                half4 positionCS        : SV_POSITION;
                float3 positionWS       : TEXCOORD0;
                half3 normalWS          : TEXCOORD1;
                half3 tangentWS         : TEXCOORD2;
                half3 bitangentWS       : TEXCOORD3;
                half2 uv                : TEXCOORD4;
                half2 uv2               : TEXCOORD5;
                half fogFactor          : TEXCOORD7;
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 6);

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            inline void InitializeSurfaceData(out SurfaceData outSurfaceData, half3 albedoAlpha)
            {
                outSurfaceData.alpha = 1;
                outSurfaceData.albedo = albedoAlpha.rgb;

                outSurfaceData.metallic = 0;
                outSurfaceData.specular = half3(0.0, 0.0, 0.0);

                outSurfaceData.smoothness = 0;
                outSurfaceData.normalTS = half3(0.0, 0.0, 0.0);
                outSurfaceData.occlusion = 1;
                outSurfaceData.emission = 0;

                outSurfaceData.clearCoatMask = half(0.0);
                outSurfaceData.clearCoatSmoothness = half(0.0);
            }

            void InitializeInputData(VertexOutput IN, half3 normalTS, out InputData inputData)
            {
                inputData = (InputData)0;

                inputData.positionWS = IN.positionWS;
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);

                half3x3 tangentTransform = half3x3(IN.tangentWS, IN.bitangentWS, IN.normalWS);
                inputData.tangentToWorld = tangentTransform;
                inputData.normalWS = TransformTangentToWorld(normalTS, tangentTransform);
                inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
                inputData.viewDirectionWS = viewDirWS;

                inputData.fogCoord = InitializeInputDataFog(float4(IN.positionWS, 1.0), IN.fogFactor);
                inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(IN.staticLightmapUV);
                inputData.bakedGI = SAMPLE_GI(IN.staticLightmapUV, IN.vertexSH, inputData.normalWS);
            }

            VertexOutput vert(VertexInput IN)
            {
                VertexOutput OUT = (VertexOutput)0;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normal, IN.tangent);
                OUT.normalWS = normalInputs.normalWS;
                OUT.tangentWS = normalInputs.tangentWS;
                OUT.bitangentWS = normalInputs.bitangentWS;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.vertex.xyz);
                OUT.positionCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;
                OUT.fogFactor = ComputeFogFactor(OUT.positionCS.z);

                OUTPUT_SH(OUT.normalWS, OUT.vertexSH);
                OUT.uv = IN.uv;
                OUT.uv2 = IN.uv2;

                return OUT;
            }

            half4 frag(VertexOutput IN) : COLOR
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                // Textures
                half4 diffuse = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half dyeMask = diffuse.a;
                half3 normal = UnpackNormal(SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, IN.uv));
                half4 msa = SAMPLE_TEXTURE2D(_MetalSmoothAOGlowTex, sampler_MetalSmoothAOGlowTex, IN.uv).rgba;

                // HueShift
                half3 color = HueSatShift(diffuse.rgb, dyeMask, half3(_HueAdjust, _SatAdjust, _LevelAdjust));

                // Dyes
                color = Dye(color, diffuse.rgb, dyeMask, _Dye30.rgb, _Dye50.rgb, _Dye60.rgb, _Dye80.rgb, _90HueAdjust);

                // Pattern
                half pattern = SAMPLE_TEXTURE2D(_PatternTex, sampler_PatternTex, IN.uv2).r;
                color *= 1 - pattern * _PatternTexIntensity;

                SurfaceData surfaceData;
                InitializeSurfaceData(surfaceData, color);
                surfaceData.normalTS = normal;
                surfaceData.metallic = msa.r;
                surfaceData.smoothness = msa.g;
                surfaceData.occlusion = msa.b;
                surfaceData.emission = msa.a * _EmissionColor.rgb * _EmissionIntensity;

                InputData inputData;
                InitializeInputData(IN, surfaceData.normalTS, inputData);

                half4 litColor = UniversalFragmentPBR(inputData, surfaceData);
                litColor.rgb = MixFog(litColor.rgb, inputData.fogCoord);

                return litColor;
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
            Cull Back

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow

            #include_with_pragmas "TileMap_MultiCompileOptions_ShadowPass.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
            uniform half _PatternTexIntensity;
            uniform half _HueAdjust;
            uniform half _SatAdjust;
            uniform half _LevelAdjust;
            uniform half4 _Dye30;
            uniform half4 _Dye50;
            uniform half4 _Dye60;
            uniform half4 _Dye80;
            uniform half _90HueAdjust;
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
