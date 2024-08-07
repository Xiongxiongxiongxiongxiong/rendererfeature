Shader "Custom/GaussianBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurSize ("Blur Size", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _BlurSize;
            float4 _MainTex_TexelSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 color = fixed4(0,0,0,0);
                fixed4 color1 = fixed4(0,0,0,0);
                float2 uv = i.uv;
                float2 texelSize = _MainTex_TexelSize.xy;



                float weight[5];
                weight[0] = 0.227027;
                weight[1] = 0.1945946;
                weight[2] = 0.1216216;
                weight[3] = 0.054054;
                weight[4] = 0.016216;

                
                // Gaussian weights
// float weight[9] = {1.0f/16.0f, 2.0f/16.0f, 1.0f/16.0f,
// 2.0f/16.0f, 4.0f/16.0f, 2.0f/16.0f,
// 1.0f/16.0f, 2.0f/16.0f, 1.0f/16.0f};

                // Horizontal Blur
                for (int x = -4; x <= 4; x++)
                {
                    color += tex2D(_MainTex, uv + float2(texelSize.x * x * _BlurSize, 0.0)) * weight[abs(x)];
                }
                for (int y = -4; y <= 4; y++)
                {
                    color1 += tex2D(_MainTex, uv + float2(0.0, texelSize.y * y * _BlurSize)) * weight[abs(y)];
                }
               float4 col = lerp(color,color1,0.5);
                return col;
            }
            ENDCG
        }

//        Pass
//        {
//            CGPROGRAM
//            #pragma vertex vert
//            #pragma fragment frag
//
//            #include "UnityCG.cginc"
//
//            struct appdata
//            {
//                float4 vertex : POSITION;
//                float2 uv : TEXCOORD0;
//            };
//
//            struct v2f
//            {
//                float2 uv : TEXCOORD0;
//                float4 vertex : SV_POSITION;
//            };
//
//            sampler2D _MainTex;
//            float4 _MainTex_ST;
//            float _BlurSize;
//            float4 _MainTex_TexelSize;
//
//            v2f vert (appdata v)
//            {
//                v2f o;
//                o.vertex = UnityObjectToClipPos(v.vertex);
//                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
//                return o;
//            }
//
//            fixed4 frag (v2f i) : SV_Target
//            {
//                fixed4 color = fixed4(0,0,0,0);
//                float2 uv = i.uv;
//                float2 texelSize = _MainTex_TexelSize.xy;
//
//                // Gaussian weights
//                float weight[5];
//                weight[0] = 0.227027;
//                weight[1] = 0.1945946;
//                weight[2] = 0.1216216;
//                weight[3] = 0.054054;
//                weight[4] = 0.016216;
//
//                // Vertical Blur
//                for (int y = -4; y <= 4; y++)
//                {
//                    color += tex2D(_MainTex, uv + float2(0.0, texelSize.y * y * _BlurSize)) * weight[abs(y)];
//                }
//
//                return color;
//            }
//            ENDCG
//        }
    }
    FallBack "Diffuse"
}
