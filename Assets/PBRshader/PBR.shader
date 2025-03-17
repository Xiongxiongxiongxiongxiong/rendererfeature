Shader "Custom/PBRShader"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _BaseMap ("Base Map", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _MRAOMap ("Metallic (R), Roughness (G), AO (B)", 2D) = "white" {} // MRAO贴图
        _EmissionMap ("Emission Map", 2D) = "black" {}  // 自发光贴图
        _EmissionColor ("Emission Color", Color) = (0, 0, 0, 1) // 自发光颜色
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalRenderPipeline" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1; // 第二套UV（Lightmap UV）
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1; // 传递Lightmap UV
                float3 normalWS : TEXCOORD2;
                float3 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                float3 viewDirWS : TEXCOORD5;
                float3 worldPos : TEXCOORD6;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            TEXTURE2D(_MRAOMap); // MRAO贴图
            SAMPLER(sampler_MRAOMap);
            TEXTURE2D(_EmissionMap); // 自发光贴图
            SAMPLER(sampler_EmissionMap);

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _BaseMap_ST;
            float4 _EmissionColor; // 自发光颜色
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.lightmapUV = IN.lightmapUV * unity_LightmapST.xy + unity_LightmapST.zw; // 转换Lightmap UV
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.tangentWS = TransformObjectToWorldDir(IN.tangentOS.xyz);
                OUT.bitangentWS = cross(OUT.normalWS, OUT.tangentWS) * IN.tangentOS.w;
                OUT.viewDirWS = GetCameraPositionWS() - TransformObjectToWorld(IN.positionOS.xyz);
                OUT.worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }

            float3 CalculateNormal(float4 normalMapSample, float3 normalWS, float3 tangentWS, float3 bitangentWS)
            {
                float3 normalTS = UnpackNormal(normalMapSample);
                float3x3 TBN = float3x3(tangentWS, bitangentWS, normalWS);
                return normalize(mul(normalTS, TBN));
            }

            float3 SampleLightmap(float2 lightmapUV)
            {
                return SampleSingleLightmap(TEXTURE2D_ARGS(unity_Lightmap, samplerunity_Lightmap), lightmapUV, float4(1, 1, 0, 0), false, false);
            }

            float SampleShadowmask(float3 worldPos)
            {
                // 采样Shadowmask贴图
                float4 shadowmask = SAMPLE_TEXTURE2D(unity_ShadowMask, samplerunity_ShadowMask, worldPos.xz);
                return shadowmask.r; // 使用R通道
            }

            float4 frag(Varyings IN) : SV_Target
            {
                // 采样基础颜色
                float4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                // 采样法线贴图
                float4 normalMapSample = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, IN.uv);
                float3 normalWS = CalculateNormal(normalMapSample, IN.normalWS, IN.tangentWS, IN.bitangentWS);

                // 采样MRAO贴图
                float3 mrao = SAMPLE_TEXTURE2D(_MRAOMap, sampler_MRAOMap, IN.uv).rgb;
                float metallic = mrao.r; // R通道：金属度
                float roughness = mrao.g; // G通道：粗糙度
                float occlusion = mrao.b; // B通道：AO

                // 自发光
                float3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, IN.uv).rgb * _EmissionColor.rgb;

                // 采样烘焙光照贴图
                float3 lightmapColor = SampleLightmap(IN.lightmapUV);

                // 判断是否有光照贴图，如果没有，则使用实时光照
                bool hasLightmap = length(lightmapColor) > 0.01;

                // 采样Shadowmask
                float shadowmask = SampleShadowmask(IN.worldPos);

                // PBR Lighting
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float3 viewDir = normalize(IN.viewDirWS);
                float3 halfDir = normalize(lightDir + viewDir);

                float NdotL = max(dot(normalWS, lightDir), 0.0);
                float NdotV = max(dot(normalWS, viewDir), 0.0);
                float NdotH = max(dot(normalWS, halfDir), 0.0);
                float VdotH = max(dot(viewDir, halfDir), 0.0);

                // Diffuse term
                float3 diffuse = baseColor.rgb * NdotL;

                // Specular term (Cook-Torrance)
                float alpha = roughness * roughness;
                float alphaSq = alpha * alpha;
                float denom = (NdotH * NdotH * (alphaSq - 1.0) + 1.0);
                float D = alphaSq / (PI * denom * denom);

                float k = (roughness + 1.0) * (roughness + 1.0) / 8.0;
                float G1 = NdotV / (NdotV * (1.0 - k) + k);
                float G2 = NdotL / (NdotL * (1.0 - k) + k);
                float G = G1 * G2;

                float3 F0 = lerp(float3(0.04, 0.04, 0.04), baseColor.rgb, metallic);
                float3 F = F0 + (1.0 - F0) * pow(1.0 - VdotH, 5.0);

                float3 specular = (D * G * F) / (4.0 * NdotL * NdotV + 0.0001);

                // Combine diffuse and specular
                float3 finalColor = (diffuse * (1.0 - metallic) + specular) * mainLight.color;

                // Apply AO
                finalColor *= occlusion;

                // Apply Shadowmask
                finalColor *= shadowmask;

                // Add lightmap or real-time lighting
                if (hasLightmap)
                {
                    finalColor *= lightmapColor; // 使用光照贴图颜色
                }
                else
                {
                    finalColor += diffuse * mainLight.color; // 使用实时光照
                }

                // Add emission
                finalColor += emission;

                return float4(finalColor, baseColor.a);
            }
            ENDHLSL
        }
    }
}
