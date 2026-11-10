#pragma once

#if defined(COMMON_REQUIRES_TEXCOORD0) && !defined(GET_COMMON_TEXCOORD0)
	#define GET_COMMON_TEXCOORD0(texcoord) (texcoord)
#endif

#if defined(COMMON_REQUIRES_VERTEX_COLOR) && !defined(GET_COMMON_VERTEX_COLOR)
	#define GET_COMMON_VERTEX_COLOR(color) (color)
#endif


#if defined(GET_COMMON_SOFT_PARTICLE_ALPHA)
	#define COMMON_REQUIRES_SCREEN_POSITION
#endif

