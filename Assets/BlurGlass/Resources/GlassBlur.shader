// Blur from this blog post: https://blogs.unity3d.com/2015/02/06/extending-unity-5-rendering-pipeline-command-buffers/

Shader "PostEffect/GlassBlur" {
	Properties{
		//_MainTex("Base (RGB)", 2D) = "" {}
		_BlurTexture("Base (RGB)", 2D) = "" {}
	}

		HLSLINCLUDE

		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

		struct appdata_img {
		float4 vertex:POSITION;
		float2 uv:TEXCOORD0;
	};
	struct v2f {
		float4 pos : POSITION;
		float2 uv : TEXCOORD0;
	};

	float4 offsets;

	sampler2D _BlurTexture;

	v2f vert(appdata_img v) {
		v2f o;
		VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
		o.pos = vertexInput.positionCS;
		o.uv = v.uv;
		return o;
	}

	half4 frag(v2f i) : SV_Target{
		//ÆÁÄ»uv
		half2 screenUV = (i.pos.xy / _ScreenParams.xy);
		half4 mainCol = tex2D(_BlurTexture, screenUV);
		return mainCol;
	}

		ENDHLSL

		Subshader {
		Pass{
			   Tags
		   {
		   "LightMode" = "UniversalForward"
		   }

			//ZTest Always Cull Off ZWrite Off

			HLSLPROGRAM
			//#pragma fragmentoption ARB_precision_hint_fastest
			#pragma vertex vert
			#pragma fragment frag
			ENDHLSL
		}
	}


}
