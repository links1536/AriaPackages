#pragma once

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#include "../Common.hlsl"
#include "CommonPassRequires.hlsl"

//========================================================================================================
//	ShadowCaster
//========================================================================================================
struct ShadowAttributes
{
	float4 positionOS	: POSITION;
	float3 normalOS		: NORMAL;
#if defined(COMMON_REQUIRES_TEXCOORD0)
	float2 texcoord		: TEXCOORD0;
#endif
#if defined(COMMON_REQUIRES_VERTEX_COLOR)
	float4 color		: COLOR;
#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct ShadowVaryings
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
#if defined(COMMON_REQUIRES_WORLD_POSITION)
	float3 positionWS	: TEXCOORD4;
#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

ShadowVaryings vertShadow(ShadowAttributes input)
{
	ShadowVaryings output = (ShadowVaryings)0;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

	VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
	VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

	float3 positionWS = vertexInput.positionWS;
	float3 normalWS = normalInput.normalWS;

#if _CASTING_PUNCTUAL_LIGHT_SHADOW
	float3 lightDirectionWS = normalize(_MainLightPosition.xyz - positionWS);
#else
	float3 lightDirectionWS = _MainLightPosition.xyz;
#endif

	// ニアクリップを起こさないようにする
	float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
	positionCS = ApplyShadowClamping(positionCS);

	output.positionCS = positionCS;

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

half4 fragShadow(ShadowVaryings input) : SV_Target
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

	// ShadowCasterは濃淡をつけられない関係でForwardより目立ってしまう
	// そのため、意図的に薄く表現させる
	alpha = alpha * alpha * alpha;

	CLIP_COMMON_ALPHA(input, alpha);
#endif

#ifdef LOD_FADE_CROSSFADE
	LODFadeCrossFade(input.positionCS);
#endif
	return 0;
}

