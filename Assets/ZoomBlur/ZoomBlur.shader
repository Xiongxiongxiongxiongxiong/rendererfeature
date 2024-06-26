Shader "PostEffect/ZoomBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

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
            float2 _FocusScreenPosition;
            float _FocusDetail;
            float _FousPower;
            int _ReferenceResolutionX;

            

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 screenPosint = _FocusScreenPosition+_ScreenParams.xy/2;
                half2 uv =i.uv;
                half2  mousePos = (screenPosint.xy/_ScreenParams.xy);
                float2 focus = uv- mousePos;
                half aspectX = _ScreenParams.x/_ReferenceResolutionX;
                half4 outColor = half4(0,0,0,1);
                for (  int  i = 0; i<_FocusDetail;i++)
                {
                    half power = 1-_FousPower*(1/_ScreenParams.x*aspectX)*float(i);
                    outColor.rgb += tex2D(_MainTex,focus* power+ mousePos).rgb;
                }
                outColor.rgb *= 1/float(_FocusDetail);

                return outColor;
            }
            ENDCG
        }
    }
}
