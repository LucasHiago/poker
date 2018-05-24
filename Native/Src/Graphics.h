#pragma once

#include "API.h"
#include <cstdint>

enum
{
	FF_Multisample     = 1,
	FF_DepthTest       = 2,
	FF_DepthWrite      = 4,
	FF_AlphaBlend      = 8,
	FF_FramebufferSRGB = 16,
	FF_ScissorTest     = 32
};

extern uint32_t DisplayWidth;
extern uint32_t DisplayHeight;

CS_VISIBLE void SetFixedFunctionState(uint8_t state);
