#include "API.h"

#include <GL/glew.h>
#include <cstdint>

extern bool usingDefaultFB;

class ShadowMap
{
public:
	explicit ShadowMap(uint32_t resolution)
		: m_resolution(resolution)
	{
		glGenTextures(1, &m_texture);
		glBindTexture(GL_TEXTURE_2D, m_texture);
		glTexStorage2D(GL_TEXTURE_2D, 1, GL_DEPTH_COMPONENT16, resolution, resolution);
		
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_COMPARE_FUNC, GL_LESS);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_COMPARE_MODE, GL_COMPARE_REF_TO_TEXTURE);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_BORDER);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_BORDER);
		
		const float borderColor[] = { 1, 1, 1, 1 };
		glTexParameterfv(GL_TEXTURE_2D, GL_TEXTURE_BORDER_COLOR, borderColor);
		
		glGenFramebuffers(1, &m_fbo);
		glBindFramebuffer(GL_READ_FRAMEBUFFER, m_fbo);
		glFramebufferTexture2D(GL_READ_FRAMEBUFFER, GL_DEPTH_ATTACHMENT, GL_TEXTURE_2D, m_texture, 0);
	}
	
	void BindFramebuffer()
	{
		glBindFramebuffer(GL_FRAMEBUFFER, m_fbo);
		glViewport(0, 0, m_resolution, m_resolution);
		usingDefaultFB = false;
	}
	
	void BindTexture(uint32_t unit)
	{
		glActiveTexture(GL_TEXTURE0 + unit);
		glBindTexture(GL_TEXTURE_2D, m_texture);
	}
	
private:
	uint32_t m_resolution;
	GLuint m_texture;
	GLuint m_fbo;
};

CS_VISIBLE ShadowMap* SM_Create(uint32_t resolution)
{
	return new ShadowMap(resolution);
}

CS_VISIBLE void SM_Destroy(ShadowMap* shadowMap)
{
	delete shadowMap;
}

CS_VISIBLE void SM_BindFramebuffer(ShadowMap* shadowMap)
{
	shadowMap->BindFramebuffer();
}

CS_VISIBLE void SM_BindTexture(ShadowMap* shadowMap, uint32_t unit)
{
	shadowMap->BindTexture(unit);
}
