Shader "FullScreenGray"

{



 Properties

{

//固定名字

 [HideInInspector]_BlitTexture ("BlitTexture", 2D) = "white" { }

}



SubShader

 {



 Tags

 {

 "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline"

 }



 Cull Off

 Blend Off

 ZTest Off

 ZWrite Off



 Pass

 {

 Name "DrawProcedural"



 HLSLPROGRAM

 #pragma vertex vert

 #pragma fragment frag

 #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"



 CBUFFER_START(UnityPerMaterial)

 float4 _Tint;

 CBUFFER_END





 struct Attributes

 {

  float2 uv : TEXCOORD0;

  float3 normalOS : NORMAL;

  float3 tangentOS : TANGENT;

  uint vertexID : VERTEXID_SEMANTIC;

 };



 struct Varyings

 {

  float4 positionCS : SV_POSITION;

  float2 uv :TEXCOORD0;

 };



 Varyings vert(Attributes v)

 {

  Varyings o = (Varyings)0;

  o.positionCS = GetFullScreenTriangleVertexPosition(v.vertexID);



  return o;

 };

 TEXTURE2D_X(_BlitTexture);



 float4 Unity_Universal_SampleBuffer_BlitSource_float(float2 uv)

 {

  uint2 pixelCoords = uint2(uv * _ScreenSize.xy);

  return LOAD_TEXTURE2D_X_LOD(_BlitTexture, pixelCoords, 0);

 }



 float4 frag(Varyings i) : SV_TARGET

 {

  float2 uv= i.positionCS.xy/_ScreenParams.xy;

  float4 col = Unity_Universal_SampleBuffer_BlitSource_float(uv);



  float3 gray = float3(0.299, 0.587, 0.114);

  float graycolor = dot(col.rgb, gray).xxx;



  return float4(1,1,1,1);//graycolor;

 };

 ENDHLSL

}

 }

 Fallback "Unlit"

} 