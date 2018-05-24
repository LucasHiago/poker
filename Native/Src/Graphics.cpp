#include "API.h"
#include "Utils.h"
#include "Graphics.h"

#include <GL/glew.h>

bool usingDefaultFB = true;

bool g_multisampleEnabled = false;
bool g_depthTestEnabled = true;
bool g_depthWriteEnabled = true;
bool g_alphaBlendEnabled = false;
bool g_framebufferSRGB = false;
bool g_scissorTestEnabled = false;

uint32_t DisplayWidth = 0;
uint32_t DisplayHeight = 0;

inline void SetFeatureEnabled(GLenum feature, bool enabled)
{
	if (enabled)
		glEnable(feature);
	else
		glDisable(feature);
}

void SetFixedFunctionState(uint8_t state)
{
	bool multisample = (state & FF_Multisample) != 0;
	if (multisample != g_multisampleEnabled)
	{
		SetFeatureEnabled(GL_MULTISAMPLE, multisample);
		g_multisampleEnabled = multisample;
	}
	
	bool depthTest = (state & FF_DepthTest) != 0;
	if (depthTest != g_depthTestEnabled)
	{
		SetFeatureEnabled(GL_DEPTH_TEST, depthTest);
		g_depthTestEnabled = depthTest;
	}
	
	bool depthWrite = (state & FF_DepthWrite) != 0;
	if (depthWrite != g_depthWriteEnabled)
	{
		glDepthMask(depthWrite);
		g_depthWriteEnabled = depthWrite;
	}
	
	bool alphaBlend = (state & FF_AlphaBlend) != 0;
	if (alphaBlend != g_alphaBlendEnabled)
	{
		SetFeatureEnabled(GL_BLEND, alphaBlend);
		g_alphaBlendEnabled = alphaBlend;
	}
	
	bool framebufferSRGB = (state & FF_FramebufferSRGB) != 0;
	if (framebufferSRGB != g_framebufferSRGB)
	{
		SetFeatureEnabled(GL_FRAMEBUFFER_SRGB, framebufferSRGB);
		g_framebufferSRGB = framebufferSRGB;
	}
	
	bool scissorTest = (state & FF_ScissorTest) != 0;
	if (scissorTest != g_scissorTestEnabled)
	{
		SetFeatureEnabled(GL_SCISSOR_TEST, scissorTest);
		g_scissorTestEnabled = scissorTest;
	}
}

CS_VISIBLE void SetScissorRectangle(int32_t x, int32_t y, int32_t w, int32_t h)
{
	glScissor(x, DisplayHeight - (y + h), w, h);
}

CS_VISIBLE void FB_BindDefault()
{
	usingDefaultFB = true;
	glBindFramebuffer(GL_FRAMEBUFFER, 0);
	glViewport(0, 0, DisplayWidth, DisplayHeight);
}

CS_VISIBLE void FB_ClearDepth()
{
	if (!g_depthWriteEnabled)
		glDepthMask(GL_TRUE);
	
	float depthClear = 1;
	glClearBufferfv(GL_DEPTH, 0, &depthClear);
	
	if (!g_depthWriteEnabled)
		glDepthMask(GL_FALSE);
}

#pragma pack(push, 1)
struct ClearColor
{
	float r;
	float g;
	float b;
	float a;
};
#pragma pack(pop)

CS_VISIBLE void FB_ClearColor(ClearColor clearColor)
{
	glClearBufferfv(GL_COLOR, 0, &clearColor.r);
}

CS_VISIBLE void FB_DiscardColor()
{
	GLenum attachment = GL_COLOR_ATTACHMENT0;
	glInvalidateFramebuffer(GL_FRAMEBUFFER, 1, &attachment);
}