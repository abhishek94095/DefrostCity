Shader "WBGB/Lit/Dye/DiffDye3"
{
    Properties
    {
        [NoScaleOffset]
        _MainTex("Diffuse (RGB), Dye (A)", 2D) = "white" {}
        _Dye75("Primary Dye 75%", Color) = (1,1,1,1)
        _Dye50("Secondary Dye 50%", Color) = (1,1,1,1)
        _Dye25("Metal Dye 25%", Range(0,1)) = 1.0
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
            #include "Lit_DiffDye3_Input.hlsl"
            
            #pragma vertex vert
            #pragma fragment frag

            #include_with_pragmas "TileMap_MultiCompileOptions_LightingPass.hlsl"

            struct VertexInput
            {
                float4 vertex                   : POSITION;
                float3 normal                   : NORMAL;
                float4 tangent                  : TANGENT;
                float2 texcoord                 : TEXCOORD0;
                float2 staticLightmapUV : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VertexOutput
            {
                half4 positionCS               : SV_POSITION;
                float3 positionWS              : TEXCOORD0;
                half3 normalWS                 : TEXCOORD1;
                half2 uv                       : TEXCOORD2;
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

            VertexOutput vert (VertexInput IN)
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
                OUTPUT_SH(OUT.normalWS.xyz, OUT.vertexSH);

                return OUT;
            }

            half4 frag(VertexOutput IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                half3 color = Dye(IN.uv);

                SurfaceData surfaceData;
                InitializeSurfaceData(surfaceData, color);
                InputData inputData;
                InitializeInputData(IN, inputData);

                half4 litColor = UniversalFragmentPBR(inputData, surfaceData);
                litColor.rgb = MixFog(litColor.rgb, inputData.fogCoord);

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
            #include "Lit_DiffDye3_Input.hlsl"

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
