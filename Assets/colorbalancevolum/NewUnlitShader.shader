Shader "Hidden/Custom/ColorBalance"
{
    Properties
    {
        _Shadows ("Shadows", Color) = (1,1,1,1)
        _Midtones ("Midtones", Color) = (1,1,1,1)
        _Highlights ("Highlights", Color) = (1,1,1,1)
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
                color.rgb = color.rgb * _Shadows.rgb;
                color.rgb = color.rgb * _Midtones.rgb;
                color.rgb = color.rgb * _Highlights.rgb;
                return color;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
