Shader "Hidden/URP/ColorGrading"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
        _Lift ("_Lift", Color) = (1,1,1,1)
        _Gamma ("_Gamma", Color) = (1,1,1,1)
        _Gain ("_Gain", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ColorGradingPass"
            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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

            // 颜色调整参数
            float3 _Lift;
            float3 _Gamma;
            float3 _Gain;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            // 应用 Lift、Gamma、Gain
            float3 ApplyColorGrading(float3 color)
            {
                color = color + _Lift * (1.0 - color);  // Lift
                color = pow(color, 1.0 / _Gamma);      // Gamma
                color = color * _Gain;                  // Gain
                return color;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                color.rgb = ApplyColorGrading(color.rgb);
                return color;
            }
            ENDHLSL
        }
    }
}