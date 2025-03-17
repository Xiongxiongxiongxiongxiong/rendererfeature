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

            float _Brightness;
            float _Saturation;
            float _Contrast;

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
                    fixed3 finalColor = tex.rgb * _Brightness; // Ӧ������_Brightness
                    fixed luminance = 0.2125 * tex.r + 0.7154 * tex.g + 0.0721 * tex.b; // ��������
                    fixed3 luminanceColor = fixed3(luminance, luminance, luminance); // ���Ͷ�Ϊ0������Ϊluminance����ɫ
                    finalColor = lerp(luminanceColor, finalColor, _Saturation); // Ӧ�ñ��Ͷ�_Saturation
                    fixed3 avgColor = fixed3(0.5, 0.5, 0.5); // ���Ͷ�Ϊ0������Ϊ0.5����ɫ
                    finalColor = lerp(avgColor, finalColor, _Contrast); // Ӧ�öԱȶ�_Contrast
                return finalColor;
            }
            
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
                half3 cl=ColorBalance(color);
                half3 c=Fullscreen(cl);
                
                return half4(c,1);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
