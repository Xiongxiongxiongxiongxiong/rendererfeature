Shader "Hidden/BloomEffectURP"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
        _BloomThreshold("Bloom Threshold", Range(0,1)) = 0.8
        _BloomIntensity("Bloom Intensity", Float) = 1.0
        _BloomRadius("Bloom Radius", Range(0,8)) = 4
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        // Pass 0: Brightness extraction
        Pass
        {
            Name "BloomExtract"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_extract

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float _BloomThreshold;

            half Luminance(half3 color)
            {
                return dot(color, half3(0.2126, 0.7152, 0.0722));
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag_extract(Varyings IN) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half luminance = Luminance(color.rgb);
                half bright = saturate(luminance - _BloomThreshold);
                return color * bright;
            }
            ENDHLSL
        }

        // Pass 1: Horizontal Blur
        Pass
        {
            Name "BloomBlurHorizontal"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_blur

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float _BloomRadius;
            float4 _MainTex_TexelSize;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 offsets[5] : TEXCOORD1;
            };

            static const half weights[5] = { 0.0545, 0.2442, 0.4026, 0.2442, 0.0545 };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                float2 texelSize = _MainTex_TexelSize.xy * _BloomRadius;
                
                OUT.uv = IN.uv;
                OUT.offsets[0] = float4(IN.uv + float2(-2.0, 0.0) * texelSize, 0, 0);
                OUT.offsets[1] = float4(IN.uv + float2(-1.0, 0.0) * texelSize, 0, 0);
                OUT.offsets[2] = float4(IN.uv, 0, 0);
                OUT.offsets[3] = float4(IN.uv + float2(1.0, 0.0) * texelSize, 0, 0);
                OUT.offsets[4] = float4(IN.uv + float2(2.0, 0.0) * texelSize, 0, 0);

                return OUT;
            }

            half4 frag_blur(Varyings IN) : SV_Target
            {
                half4 color = 0;
                for(int i = 0; i < 5; i++)
                {
                    color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.offsets[i].xy) * weights[i];
                }
                return color;
            }
            ENDHLSL
        }

        // Pass 2: Vertical Blur
        Pass
        {
            Name "BloomBlurVertical"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_blur

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float _BloomRadius;
            float4 _MainTex_TexelSize;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 offsets[5] : TEXCOORD1;
            };

            static const half weights[5] = { 0.0545, 0.2442, 0.4026, 0.2442, 0.0545 };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                float2 texelSize = _MainTex_TexelSize.xy * _BloomRadius;
                
                OUT.uv = IN.uv;
                OUT.offsets[0] = float4(IN.uv + float2(0.0, -2.0) * texelSize, 0, 0);
                OUT.offsets[1] = float4(IN.uv + float2(0.0, -1.0) * texelSize, 0, 0);
                OUT.offsets[2] = float4(IN.uv, 0, 0);
                OUT.offsets[3] = float4(IN.uv + float2(0.0, 1.0) * texelSize, 0, 0);
                OUT.offsets[4] = float4(IN.uv + float2(0.0, 2.0) * texelSize, 0, 0);

                return OUT;
            }

            half4 frag_blur(Varyings IN) : SV_Target
            {
                half4 color = 0;
                for(int i = 0; i < 5; i++)
                {
                    color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.offsets[i].xy) * weights[i];
                }
                return color;
            }
            ENDHLSL
        }

        // Pass 3: Composite
        Pass
        {
            Name "BloomComposite"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_composite

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_BloomTex);
            SAMPLER(sampler_BloomTex);
            float _BloomIntensity;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag_composite(Varyings IN) : SV_Target
            {
                half4 sceneColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half4 bloomColor = SAMPLE_TEXTURE2D(_BloomTex, sampler_BloomTex, IN.uv);
                return sceneColor + bloomColor * _BloomIntensity;
            }
            ENDHLSL
        }
    }
}