Shader "Unlit/CustomBloom"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" {}
        _Bloom("Bloom", 2D) = "white" {}
        _Intensity("Size",Range(1,10)) = 1
        _Threshold("Threshold",Range(0,1)) = 0
    }
    SubShader
    {
        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        struct appdata
        {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct v2f_bloom
        {
            float2 uv : TEXCOORD0;
            float4 vertex : SV_POSITION;
        };
        
        struct v2f
        {
            float2 uv[5] : TEXCOORD0;
            float4 vertex : SV_POSITION;
        };
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        half4 _MainTex_TexelSize;
        half4 _MainTex_ST;

        TEXTURE2D(_BloomTex);
        SAMPLER(sampler_BloomTex);
        half4 _BloomTex_TexelSize;
        half4 _BloomTex_ST;

        float _Intensity;
        float _Threshold;

        v2f vertH (appdata v)
        {
            v2f o;
            o.vertex = TransformObjectToHClip(v.vertex);
            float2 uv = v.uv;
            o.uv[0] = uv; 
            o.uv[1] = uv + float2(_MainTex_TexelSize.x,0) * _Intensity;
            o.uv[2] = uv - float2(_MainTex_TexelSize.x,0) * _Intensity;
            o.uv[3] = uv + float2(_MainTex_TexelSize.x * 2,0) * _Intensity; 
            o.uv[4] = uv - float2(_MainTex_TexelSize.x * 2,0) * _Intensity; 

            return o;
        }

        v2f vertV (appdata v)
        {
            v2f o;
            o.vertex = TransformObjectToHClip(v.vertex);
            float2 uv = v.uv;
            o.uv[0] = uv; 
            o.uv[1] = uv + float2(_MainTex_TexelSize.y,0) * _Intensity;
            o.uv[2] = uv - float2(_MainTex_TexelSize.y,0) * _Intensity;
            o.uv[3] = uv + float2(_MainTex_TexelSize.y * 2,0) * _Intensity; 
            o.uv[4] = uv - float2(_MainTex_TexelSize.y * 2,0) * _Intensity; 

            return o;
        }

        float4 frag (v2f i) : SV_Target
        {
            float weight[3] = {0.4026,0.2442,0.0545};
            float4 col = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv[0]) * weight[0];
            for (int j = 1;j < 3;j++)
            {
                col += SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv[2 * j - 1]) * weight[j];
                col += SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv[2 * j]) * weight[j];
            }
            return col;
        }

        v2f_bloom vert_bloom (appdata v)
        {
            v2f_bloom o;
            o.vertex = TransformObjectToHClip(v.vertex);
            o.uv = TRANSFORM_TEX(v.uv,_BloomTex);
            return o;
        }

        float4 fragBloom(v2f_bloom i) : SV_Target
        {
            float4 col = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
            float b = 0.2125 * col.r + 0.7154 * col.g + 0.0721 * col.b;
            b = clamp(b - _Threshold,0,1);
            return col * b;
        }

        float4 fragFinal(v2f_bloom i) : SV_Target
        {
            float4 col = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv) + SAMPLE_TEXTURE2D(_BloomTex,sampler_BloomTex,i.uv);
            return col;
        }
        ENDHLSL
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
        Cull Off ZWrite Off ZTest Always
        LOD 100
        
        Pass
        {
            name "passBloom"
            HLSLPROGRAM
            #pragma vertex vert_bloom
            #pragma fragment fragBloom
            ENDHLSL
        }
        
        Pass
        {
            name "passh"
            HLSLPROGRAM
            #pragma vertex vertH
            #pragma fragment frag
            ENDHLSL
        }
        
        Pass
        {
            name "passv"
            HLSLPROGRAM
            #pragma vertex vertV
            #pragma fragment frag
            ENDHLSL
        }
        
        Pass
        {
            name "passFinal"
            HLSLPROGRAM
            #pragma vertex vert_bloom
            #pragma fragment fragFinal
            ENDHLSL
        }
    }
}

