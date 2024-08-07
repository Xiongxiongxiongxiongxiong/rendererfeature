Shader "Hidden/Custom/ColorBalance"
{
    Properties
    {
        _MainTex("_MainTex",2D) ="white" {}
        _Shadows ("Shadows", Color) = (1,1,1,1)
        _Midtones ("Midtones", Color) = (1,1,1,1)
        _Highlights ("Highlights", Color) = (1,1,1,1)
        _BrightColor ("Bright Color", float) = 1
        _MidColor ("Mid Color", float) = 1
        _DarkColor ("Dark Color", float) = 1
    }
    SubShader
    {
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

            float4 _Shadows;
            float4 _Midtones;
            float4 _Highlights;

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _BrightColor;
            float _MidColor;
            float _DarkColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 color = tex2D(_MainTex, i.uv);
                float luminance = dot(color.rgb, float3(0.2126, 0.7152, 0.0722));
               // fixed luminance = 0.2125 * color.r + 0.7154 * color.g + 0.0721 * color.b;

                // if (luminance < 0.33)
                // {
                //     color.rgb *= _Shadows.rgb * _BrightColor;
                // }
                // else if (luminance > 0.66)
                // {
                //     color.rgb *= _Highlights.rgb * _DarkColor;;//_Midtones.rgb * _MidColor;
                // }
                // else
                // {
                //     color.rgb *= _Midtones.rgb * _MidColor;
                // }

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

                color.rgb = finalColor;


                

                // half4 shadowColor = _Shadows * _DarkColor;
                // half4 midtoneColor = _Midtones * _MidColor;
                // half4 highlightColor = _Highlights * _BrightColor;
                //
                // if (luminance < 0.33)
                // {
                //     float t = saturate((luminance - 0.0) / (0.33 - 0.0));
                //     color.rgb = lerp(color.rgb, shadowColor.rgb, t);
                // }
                // else if (luminance < 0.66)
                // {
                //     float t = saturate((luminance - 0.33) / (0.66 - 0.33));
                //     color.rgb = lerp(shadowColor.rgb, midtoneColor.rgb, t);
                // }
                // else
                // {
                //     float t = saturate((luminance - 0.66) / (1.0 - 0.66));
                //     color.rgb = lerp(midtoneColor.rgb, highlightColor.rgb, t);
                // }





                
                // color.rgb = color.rgb * _Shadows.rgb;
                // color.rgb = color.rgb * _Midtones.rgb;
                // color.rgb = color.rgb * _Highlights.rgb;
                return color;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
