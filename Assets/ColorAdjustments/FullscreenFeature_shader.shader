Shader "MyShader/BrightnessSaturationContrast" { // 调整亮度、饱和度、对比度
    Properties{
        _MainTex("Base (RGB)", 2D) = "white" {} // 主纹理
        _Brightness("Brightness", Range(0.5, 3)) = 1 // 亮度
        _Saturation("Saturation", Range(0.1, 5)) = 1 // 饱和度
        _Contrast("Contrast", Range(0.4, 3)) = 1 // 对比度
    }

        SubShader{
            Pass {
                // 深度测试始终通过, 关闭深度写入
                //ZTest Always ZWrite Off

                CGPROGRAM
                #pragma vertex vert_img // 使用内置的vert_img顶点着色器
                #pragma fragment frag 
                #include "UnityCG.cginc"

                sampler2D _MainTex; // 主纹理
                half _Brightness; // 亮度
                half _Saturation; // 饱和度
                half _Contrast; // 对比度

                fixed4 frag(v2f_img i) : SV_Target { // v2f_img为内置结构体, 里面只包含pos和uv
                    fixed4 tex = tex2D(_MainTex, i.uv); // 纹理采样
                    fixed3 finalColor = tex.rgb * _Brightness; // 应用亮度_Brightness
                    fixed luminance = 0.2125 * tex.r + 0.7154 * tex.g + 0.0721 * tex.b; // 计算亮度
                    fixed3 luminanceColor = fixed3(luminance, luminance, luminance); // 饱和度为0、亮度为luminance的颜色
                    finalColor = lerp(luminanceColor, finalColor, _Saturation); // 应用饱和度_Saturation
                    fixed3 avgColor = fixed3(0.5, 0.5, 0.5); // 饱和度为0、亮度为0.5的颜色
                    finalColor = lerp(avgColor, finalColor, _Contrast); // 应用对比度_Contrast
                    return fixed4(finalColor, tex.a);
                }

                ENDCG
            }
        }

            Fallback Off
}