#pragma once

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#include "../Common.hlsl"
#include "CommonPassRequires.hlsl"

//_NormalMap, sampler_NormalMap, input.uv), _NormalScale
#if defined(ENALBE_COMMON_NORMALMAP)
	#define REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR
#endif

//========================================================================================================
//	DepthNormal
//========================================================================================================

struct AttributesNormal
{
	float4 positionOS	: POSITION;
	float3 normalOS		: NORMAL;
	float4 tangentOS	: TANGENT;
	float2 texcoord		: TEXCOORD0;
#if defined(COMMON_REQUIRES_VERTEX_COLOR)
	float4 color		: COLOR;
#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VaryingsNormal
{
	float4 positionCS	: SV_POSITION;
	float2 uv			: TEXCOORD0;
	float3 normalWS		: TEXCOORD1;
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
	float4 tangentWS	: TEXCOORD2;
#endif
#if defined(COMMON_REQUIRES_VERTEX_COLOR)
	half4 color			: TEXCOORD3;
#endif
	
#if defined(COMMON_REQUIRES_SCREEN_POSITION)
	float4 positionSS	: TEXCOORD4;
	float  depthVS		: TEXCOORD5;
#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

VaryingsNormal vertNormal(AttributesNormal input)
{
	VaryingsNormal output = (VaryingsNormal)0;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

	VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
	VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

	// 座標系
	output.positionCS = vertexInput.positionCS;

	// 法線など
	float3 normalWS = normalInput.normalWS;
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR) || defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
	real   sign = input.tangentOS.w * GetOddNegativeScale();
	float4 tangentWS = float4(normalInput.tangentWS.xyz, sign);
#endif
	output.normalWS = normalWS;
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
	output.tangentWS = tangentWS;
#endif

	// UV
#if defined(COMMON_REQUIRES_TEXCOORD0)
	output.uv = GET_COMMON_TEXCOORD0(input.texcoord);
#endif

	// 頂点カラー
#if defined(COMMON_REQUIRES_VERTEX_COLOR)
	output.color = GET_COMMON_VERTEX_COLOR(input.color);
#endif

#if defined(COMMON_REQUIRES_SCREEN_POSITION)
	output.positionSS = vertexInput.positionNDC;
	output.depthVS = vertexInput.positionVS.z;
#endif

	return output;
}

// フラグメントシェーダー
half4 fragNormal(VaryingsNormal input, FRONT_FACE_TYPE faceType : FRONT_FACE_SEMANTIC) : SV_Target
{
	UNITY_SETUP_INSTANCE_ID(input);

	// 表裏の判定
	half facing = IS_FRONT_VFACE(faceType, 1, -1);

	// αクリッピング
#if defined(CLIP_COMMON_ALPHA)
	half alpha = 1;
	#if defined(GET_COMMON_SOFT_PARTICLE_ALPHA)
	{
		float2 screenUV = input.positionSS.xy / input.positionSS.w;
		float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
		float vertexDepth = -input.depthVS;
		alpha *= GET_COMMON_SOFT_PARTICLE_ALPHA(sceneDepth, vertexDepth);
	}
	#endif
	CLIP_COMMON_ALPHA(input, alpha);
#endif

	// 法線
#if defined(ENALBE_COMMON_NORMALMAP)
	float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(COMMON_NORMAL_TEXTURE, COMMON_NORMAL_SAMPLER, input.uv), COMMON_NORMAL_SCALE);

	float   sgn = input.tangentWS.w;      // should be either +1 or -1
	float3  bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
	half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);
	float3  normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
#else
	float3 normalWS = input.normalWS;
#endif

	// 裏面の時は法線が逆なので元に戻す
	normalWS *= facing;
	normalWS = NormalizeNormalPerPixel(normalWS);

#if defined(_GBUFFER_NORMALS_OCT)
	float2 octNormalWS = PackNormalOctQuadEncode(normalWS);           // values between [-1, +1], must use fp32 on some platforms.
	float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);   // values between [ 0,  1]
	half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);      // values between [ 0,  1]
	return half4(packedNormalWS, 0.0);
#else
	return half4(normalWS, 0.0);
#endif
}

