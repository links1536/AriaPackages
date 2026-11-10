#pragma once

float4 GetParticleColor(float4 color)
{
#if defined(UNITY_PARTICLE_INSTANCING_ENABLED)
#if !defined(UNITY_PARTICLE_INSTANCE_DATA_NO_COLOR)

	UNITY_PARTICLE_INSTANCE_DATA data = unity_ParticleInstanceData[unity_InstanceID];

	color = lerp(1, color, unity_ParticleUseMeshColors);
	color *= float4(UnpackFromR8G8B8A8(data.color));

#endif
#endif

	return color;
}
