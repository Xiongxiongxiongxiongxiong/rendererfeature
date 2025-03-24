Shader "Universal Render Pipeline/Custom/URP_Complete_Shader"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        _Metallic("Metallic", Range(0, 1)) = 0.0
        _Smoothness("Smoothness", Range(0, 1)) = 0.5
        [Toggle(_METALLICGLOSSMAP)] _UseMetallicMap("Use Metallic Map", Float) = 0
        _MetallicGlossMap("Metallic (R) Smoothness (A)", 2D) = "white" {}
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
            "ShaderModel"="4.5"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // Universal Pipeline Keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            
            // Reflection Probe Keywords
            #pragma multi_compile _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile _ _REFLECTION_PROBE_BOX_PROJECTION

            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup
            #pragma instancing_options lodfade
            #pragma instancing_options assumeuniformscaling

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            // #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Assets/shader/Shadows_XH.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 texcoord     : TEXCOORD0;
                float2 lightmapUV  : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 positionWS   : TEXCOORD1;
                float3 normalWS     : TEXCOORD2;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 3);
                float4 shadowCoord : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_MetallicGlossMap);
            SAMPLER(sampler_MetallicGlossMap);
            // GPU Instancing Properties
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _MetallicGlossMap_ST)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
                UNITY_DEFINE_INSTANCED_PROP(half, _Metallic)
                UNITY_DEFINE_INSTANCED_PROP(half, _Smoothness)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
            UNITY_INSTANCING_BUFFER_END(Props)

            // GPU Instancing Matrix Setup
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                float4x4 instanceMatrix;
            #endif

            void setup()
            {
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    unity_ObjectToWorld = instanceMatrix;
                    unity_WorldToObject = inverse(instanceMatrix);
                #endif
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                #ifdef UNITY_INSTANCING_ENABLED
                    float4 worldPos = mul(UNITY_MATRIX_M, input.positionOS);
                #else
                    float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                #endif

                output.positionCS = TransformWorldToHClip(worldPos.xyz);
                output.positionWS = worldPos.xyz;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                // Handle UV and Lightmap
                float4 baseMap_ST = UNITY_ACCESS_INSTANCED_PROP(Props, _BaseMap_ST);
                output.uv = input.texcoord * baseMap_ST.xy + baseMap_ST.zw;
                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
                OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

                // Shadows
                output.shadowCoord = TransformWorldToShadowCoord(output.positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                // Material Properties
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) 
                               * UNITY_ACCESS_INSTANCED_PROP(Props, _BaseColor);
                half4 RMGA = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, input.uv) ;
                half metallic = UNITY_ACCESS_INSTANCED_PROP(Props, _Metallic);
                half smoothness = UNITY_ACCESS_INSTANCED_PROP(Props, _Smoothness);

                // Lighting Data
                InputData lightingInput = (InputData)0;
                lightingInput.positionWS = input.positionWS;
                lightingInput.normalWS = normalize(input.normalWS);
                lightingInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                lightingInput.shadowCoord = input.shadowCoord;
                lightingInput.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, lightingInput.normalWS);

                // Shadowmask
                #if defined(LIGHTMAP_ON) && defined(SHADOWS_SHADOWMASK)
                    lightingInput.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);
                #else
                    lightingInput.shadowMask = unity_ProbesOcclusion;
                #endif

                // Reflection Probes
                half3 reflectVector = reflect(-lightingInput.viewDirectionWS, lightingInput.normalWS);
                half perceptualRoughness = 1.0 - smoothness;
                half3 reflection = GlossyEnvironmentReflection(
                    reflectVector,
                    lightingInput.positionWS,
                    perceptualRoughness,
                    1.0
                );

                // Surface Data
                SurfaceData surfaceInput;
                ZERO_INITIALIZE(SurfaceData, surfaceInput);
                surfaceInput.albedo = baseColor.rgb;
                surfaceInput.alpha = baseColor.a;
                surfaceInput.metallic = metallic*RMGA.x;
                surfaceInput.smoothness = smoothness;
                surfaceInput.occlusion = 1.0;
                surfaceInput.emission = reflection * metallic;
                surfaceInput.specular = lerp(0.04, baseColor.rgb, metallic);

                // Final Lighting Calculation
                return UniversalFragmentPBR(lightingInput, surfaceInput);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #include "Assets/shader/ShadowCasterPass_XH.hlsl"
            //#include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            
            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
    //CustomEditor "UnityEditor.ShaderGUI" // 修改这一行
    //CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.LitShader"
}