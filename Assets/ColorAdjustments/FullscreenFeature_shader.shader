Shader "MyShader/BrightnessSaturationContrast" { // �������ȡ����Ͷȡ��Աȶ�
    Properties{
        _MainTex("Base (RGB)", 2D) = "white" {} // ������
        _Brightness("Brightness", Range(0.5, 3)) = 1 // ����
        _Saturation("Saturation", Range(0.1, 5)) = 1 // ���Ͷ�
        _Contrast("Contrast", Range(0.4, 3)) = 1 // �Աȶ�
    }

        SubShader{
            Pass {
                // ��Ȳ���ʼ��ͨ��, �ر����д��
                //ZTest Always ZWrite Off

                CGPROGRAM
                #pragma vertex vert_img // ʹ�����õ�vert_img������ɫ��
                #pragma fragment frag 
                #include "UnityCG.cginc"

                sampler2D _MainTex; // ������
                half _Brightness; // ����
                half _Saturation; // ���Ͷ�
                half _Contrast; // �Աȶ�

                fixed4 frag(v2f_img i) : SV_Target { // v2f_imgΪ���ýṹ��, ����ֻ����pos��uv
                    fixed4 tex = tex2D(_MainTex, i.uv); // �������
                    fixed3 finalColor = tex.rgb * _Brightness; // Ӧ������_Brightness
                    fixed luminance = 0.2125 * tex.r + 0.7154 * tex.g + 0.0721 * tex.b; // ��������
                    fixed3 luminanceColor = fixed3(luminance, luminance, luminance); // ���Ͷ�Ϊ0������Ϊluminance����ɫ
                    finalColor = lerp(luminanceColor, finalColor, _Saturation); // Ӧ�ñ��Ͷ�_Saturation
                    fixed3 avgColor = fixed3(0.5, 0.5, 0.5); // ���Ͷ�Ϊ0������Ϊ0.5����ɫ
                    finalColor = lerp(avgColor, finalColor, _Contrast); // Ӧ�öԱȶ�_Contrast
                    return fixed4(finalColor, tex.a);
                }

                ENDCG
            }
        }

            Fallback Off
}