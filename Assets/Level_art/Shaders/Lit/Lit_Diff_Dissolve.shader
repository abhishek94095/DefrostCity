Shader "WBGB/Lit/Diff Dissolve"
{
    Properties
    {
        [NoScaleOffset]
        [MainTexture] _BaseMap("Main Texture", 2D) = "white" {}
        _Tiling("Tiling", Float) = 1
        [Space]
        _GlowColor("Glow Color", Color) = (1.0, 0.3, 0.1, 1)
        _GlowStrength("Glow Strength", Range(0, 5)) = 1.0
        [Space]
        _DissolveAmount("Dissolve Progress", Range(0,1)) = 0.0

        // Hidden cull mode property - used to support mirrored rendering
        // which flips the geometry inside out
        [HideInInspector]
        _Cull("__cull", Float) = 2.0
    }

    SubShader
    {
        Tags{"RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline"}

        Pass
        {
            Name "ForwardLit"
            Tags{"LightMode" = "UniversalForward"}
            
            Blend SrcAlpha OneMinusSrcAlpha
            Cull [_Cull]
            AlphaToMask On

            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            #pragma vertex vert
            #pragma fragment frag

            #include_with_pragmas "DragonPit_MultiCompileOptions_LightingPass.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
            half _Tiling;
            float _GlowStrength;
            float4 _GlowColor;
            float _DissolveAmount;
            CBUFFER_END

            struct VertexInput
            {
                float4 positionOS               : POSITION;
                float3 normalOS                 : NORMAL;
                float4 tangentOS                : TANGENT;
                float2 texcoord                 : TEXCOORD0; 
                float2 staticLightmapUV         : TEXCOORD1;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VertexOutput
            {
                half4 positionCS                : SV_POSITION;
                half2 uv                        : TEXCOORD0;
                float3 positionWS               : TEXCOORD1;
                half3 normalWS                  : TEXCOORD2;
                half3 tangentWS                 : TEXCOORD3;
                half3 bitangentWS               : TEXCOORD4;
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 6);

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            inline void InitializeSurfaceData(float2 uv, out SurfaceData outSurfaceData)
            {
                half4 albedoAlpha = half4(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv / _Tiling));
                outSurfaceData.alpha = albedoAlpha.a;

                outSurfaceData.albedo = albedoAlpha.rgb;
                outSurfaceData.metallic = 0;
                outSurfaceData.specular = half3(0.0, 0.0, 0.0);

                outSurfaceData.smoothness = 0;
                outSurfaceData.normalTS = half3(0.5, 0.5, 0.5);
                outSurfaceData.occlusion = 1;
                outSurfaceData.emission = 0;

                outSurfaceData.clearCoatMask = half(0.0);
                outSurfaceData.clearCoatSmoothness = half(0.0);
            }
            
            //Function called in the fragment shader, responsible for sampling normal maps, fog, and gi
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

            //Vertex shader stage
            VertexOutput vert (VertexInput IN)
            {
                VertexOutput OUT = (VertexOutput)0;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);
                OUT.normalWS = normalInputs.normalWS;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;

                OUTPUT_SH(OUT.normalWS, OUT.vertexSH);
                OUTPUT_LIGHTMAP_UV(IN.staticLightmapUV, unity_LightmapST, OUT.staticLightmapUV);
                OUT.uv = IN.texcoord;

                return OUT;
            }

            // Fragment shader stage
            half4 frag(VertexOutput IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                SurfaceData surfaceData;
                InitializeSurfaceData(IN.uv, surfaceData);
                InputData inputData;
                InitializeInputData(IN, inputData);

                half4 litColor = UniversalFragmentPBR(inputData, surfaceData);
                
                // Shared mask value
                float maskVal = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).b;

                // Dissolve
                float dissolve = maskVal - _DissolveAmount;
                dissolve += 0.1;
                clip( dissolve );

                float glowZone = smoothstep(0.05, 0.01, dissolve );
                float3 glow = glowZone * _GlowColor.rgb * _GlowStrength;
                litColor.rgb += glow;

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
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow

            #include_with_pragmas "TileMap_MultiCompileOptions_ShadowPass.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
            half _Tiling;
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
