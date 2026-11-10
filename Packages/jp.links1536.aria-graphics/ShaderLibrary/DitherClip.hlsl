#pragma once

//#define _DITHER_MASK_LOD3D
//#define _DITHER_MASK_LOD2D

#if defined(_DITHER_MASK_LOD3D)

	TEXTURE3D(_DitherMaskLOD);
	SAMPLER(sampler_DitherMaskLOD);

#elif defined(_DITHER_MASK_LOD2D)

	TEXTURE2D(_DitherMaskLOD2D);
	SAMPLER(sampler_DitherMaskLOD2D);

#else

	// 結局これが一番きれい
	static const int bayerMatrix[16] = {
		 0,  8,  2, 10,
		12,  4, 14,  6,
		 3, 11,  1,  9,
		15,  7, 13,  5
	};
#endif

void DitherFade(float4 positionCS, half alpha)
{
#if defined(_DITHER_MASK_LOD3D)
	float3 uv = float3(positionCS.xy * 0.25, alpha * 0.9375);
	half dither = SAMPLE_TEXTURE3D(_DitherMaskLOD, sampler_DitherMaskLOD, uv).a;
#elif defined(_DITHER_MASK_LOD2D)
	float2 uv = positionCS.xy;
	uv *= 0.25;
	uv.y = frac(uv.y) * 0.0625;
	uv.y += alpha * 0.9375;

	half dither = SAMPLE_TEXTURE2D(_DitherMaskLOD2D, sampler_DitherMaskLOD2D, uv).a;
#else
	// ピクセル座標を整数に変換し、4で割った余りを計算してインデックス(0-3)を得る
	uint x = (uint)positionCS.x % 4;
	uint y = (uint)positionCS.y % 4;

	// 配列は1次元なので、y * 4 + x でインデックス化
	uint index = y * 4 + x;

	// 閾値を取得 (0-15 の値を 16.0 で割って 0.0-1.0 に正規化)
	float threshold = bayerMatrix[index] / 16.0;

	// 比較して白黒判定
	half dither = step(threshold, alpha);
#endif

	clip(dither - 0.5);
}