#pragma once

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#include "../Common.hlsl"
#include "CommonPassRequires.hlsl"

float _ObjectId;
float _PassValue;

//========================================================================================================
//	SceneSelection
//========================================================================================================

struct SelectionAttributes
{
	float4 positionOS	: POSITION;
#if defined(COMMON_REQUIRES_TEXCOORD0)
	float2 texcoord		: TEXCOORD0;
#endif
#if defined(COMMON_REQUIRES_VERTEX_COLOR)
	float4 color		: COLOR;
#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct SelectionVaryings
{
	float4 positionCS	: SV_POSITION;
#if defined(COMMON_REQUIRES_TEXCOORD0)
	float2 uv			: TEXCOORD0;
#endif
#if defined(COMMON_REQUIRES_VERTEX_COLOR)
	half4 color			: TEXCOORD1;
#endif
#if defined(COMMON_REQUIRES_SCREEN_POSITION)
	float4 positionSS	: TEXCOORD2;
	float  depthVS		: TEXCOORD3;
#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

SelectionVaryings vertSelection(SelectionAttributes input)
{
	SelectionVaryings output = (SelectionVaryings)0;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

	VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

	// 座標系
	output.positionCS = vertexInput.positionCS;

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

half4 fragSelection(SelectionVaryings input) : SV_Target
{
	UNITY_SETUP_INSTANCE_ID(input);

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

#ifdef LOD_FADE_CROSSFADE
	LODFadeCrossFade(input.positionCS);
#endif
	return float4(_ObjectId, _PassValue, 1, 1);
}
