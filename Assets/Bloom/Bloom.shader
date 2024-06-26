Shader "Custom/Bloom"
{
    Properties
    {
        _MainTex("Base (RGB)", 2D) = "white" {}
        _Threshold("Threshold", Range(0, 1)) = 0.5
        _Intensity("Intensity", Range(0, 10)) = 1.0
    }
        SubShader
        {
            Tags { "RenderType" = "Opaque" }
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
                float _Threshold;
                float _Intensity;

                v2f vert(appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                    return o;
                }

                half4 frag(v2f i) : SV_Target
                {
                    half4 color = tex2D(_MainTex, i.uv);
                    half brightness = max(color.r, max(color.g, color.b));
                    half4 bloom = (brightness - _Threshold) * _Intensity;
                    bloom = max(bloom, 0.0);
                    return color + bloom;
                }
                ENDCG
            }
        }
}
