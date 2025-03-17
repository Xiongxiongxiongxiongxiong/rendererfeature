Shader "Hidden/Custom/ColorBalance"
{
    Properties
    {
        _MainTex("_MainTex",2D) ="white" {}
        //
        _Shadows ("Shadows", Color) = (1,1,1,1)
        _Midtones ("Midtones", Color) = (1,1,1,1)
        _Highlights ("Highlights", Color) = (1,1,1,1)
        _BrightColor ("Bright Color", float) = 1
        _MidColor ("Mid Color", float) = 1
        _DarkColor ("Dark Color", float) = 1
        //
        _Brightness("Brightness", Range(0.5, 3)) = 1 // ����
        _Saturation("Saturation", Range(0.1, 5)) = 1 // ���Ͷ�
        _Contrast("Contrast", Range(0.4, 3)) = 1 // �Աȶ�
        //
        _BloomThreshold("Bloom Threshold", Range(0,1)) = 0.8
        _BloomIntensity("Bloom Intensity", Float) = 1.0
        _BloomRadius("Bloom Radius", Range(0,8)) = 4
    }
    SubShader
    {
        Pass
        {
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

            float4 _Shadows;
            float4 _Midtones;
            float4 _Highlights;
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
           // sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _BrightColor;
            float _MidColor;
            float _DarkColor;

            float _Brightness;
            float _Saturation;
            float _Contrast;
            float _BloomThreshold;
            half3 ColorBalance(half4 color)
            {
                float luminance = dot(color.rgb, float3(0.2126, 0.7152, 0.0722));
                half4 shadowColor =color* _Shadows * _DarkColor;
                half4 midtoneColor = color*_Midtones * _MidColor;
                half4 highlightColor = color*_Highlights * _BrightColor;

                half3 finalColor;
                if (luminance < 0.33)
                {
                    float t = smoothstep(0.0, 0.33, luminance);
                    finalColor = lerp(color.rgb, shadowColor.rgb, t);
                }
                else if (luminance < 0.66)
                {
                    float t = smoothstep(0.33, 0.66, luminance);
                    finalColor = lerp(shadowColor.rgb, midtoneColor.rgb, t);
                }
                else
                {
                    float t = smoothstep(0.66, 1.0, luminance);
                    finalColor = lerp(midtoneColor.rgb, highlightColor.rgb, t);
                }

                return finalColor;
            }
            half3 Fullscreen(half3 tex)
            {
                    half3 finalColor = tex.rgb * _Brightness; // Ӧ������_Brightness
                    half luminance = 0.2125 * tex.r + 0.7154 * tex.g + 0.0721 * tex.b; // ��������
                    half3 luminanceColor = half3(luminance, luminance, luminance); // ���Ͷ�Ϊ0������Ϊluminance����ɫ
                    finalColor = lerp(luminanceColor, finalColor, _Saturation); // Ӧ�ñ��Ͷ�_Saturation
                    half3 avgColor = half3(0.5, 0.5, 0.5); // ���Ͷ�Ϊ0������Ϊ0.5����ɫ
                    finalColor = lerp(avgColor, finalColor, _Contrast); // Ӧ�öԱȶ�_Contrast
                return finalColor;
            }
            half Lum(half3 color)
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
                half3 cl=ColorBalance(color);
                half3 c=Fullscreen(cl);
                
                half luminance = Lum(color.rgb);
                half bright = saturate(luminance - _BloomThreshold);
                
                return half4(c,1);
            }
            ENDHLSL
        }
        
         // ---------------------------
        // Pass 1: 高光提取（Bloom预处理）
        // ---------------------------
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
   // FallBack "Diffuse"
}
