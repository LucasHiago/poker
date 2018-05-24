#include "API.h"

#include <GL/glew.h>
#include <cstdint>

extern bool usingDefaultFB;

const int NUM_SAMPLES = 4;

inline void InitTexture(GLenum target)
{
	glTexParameteri(target, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
	glTexParameteri(target, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
	glTexParameteri(target, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
	glTexParameteri(target, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
}

class BlurFB
{
public:
	enum
	{
		BUF_Input  = 0,
		BUF_Inter1 = 1,
		BUF_Inter2 = 2,
		BUF_Depth  = 3
	};
	
	BlurFB(uint32_t width, uint32_t height)
		: m_width(width), m_height(height)
	{
		glGenTextures(4, m_textures);
		glGenFramebuffers(3, m_framebuffers);
		
		glBindTexture(GL_TEXTURE_2D_MULTISAMPLE, m_textures[BUF_Input]);
		glTexStorage2DMultisample(GL_TEXTURE_2D_MULTISAMPLE, NUM_SAMPLES, GL_RGBA8, width, height, false);
		//InitTexture(GL_TEXTURE_2D_MULTISAMPLE);
		
		glBindTexture(GL_TEXTURE_2D, m_textures[BUF_Inter1]);
		glTexStorage2D(GL_TEXTURE_2D, 1, GL_RGBA8, width, height);
		InitTexture(GL_TEXTURE_2D);
		
		glBindTexture(GL_TEXTURE_2D, m_textures[BUF_Inter2]);
		glTexStorage2D(GL_TEXTURE_2D, 1, GL_RGBA8, width, height);
		InitTexture(GL_TEXTURE_2D);
		
		glBindTexture(GL_TEXTURE_2D_MULTISAMPLE, m_textures[BUF_Depth]);
		glTexStorage2DMultisample(GL_TEXTURE_2D_MULTISAMPLE, NUM_SAMPLES, GL_DEPTH_COMPONENT32, width, height, false);
		//InitTexture(GL_TEXTURE_2D_MULTISAMPLE);
		
		glBindFramebuffer(GL_READ_FRAMEBUFFER, m_framebuffers[BUF_Input]);
		glFramebufferTexture2D(GL_READ_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D_MULTISAMPLE, m_textures[BUF_Input], 0);
		glFramebufferTexture2D(GL_READ_FRAMEBUFFER, GL_DEPTH_ATTACHMENT, GL_TEXTURE_2D_MULTISAMPLE, m_textures[BUF_Depth], 0);
		
		for (int i = 1; i < 3; i++)
		{
			glBindFramebuffer(GL_READ_FRAMEBUFFER, m_framebuffers[i]);
			glFramebufferTexture2D(GL_READ_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, m_textures[i], 0);
		}
	}
	
	~BlurFB()
	{
		glDeleteTextures(4, m_textures);
		glDeleteFramebuffers(3, m_framebuffers);
	}
	
	void Resolve(bool toDefault)
	{
		glBindFramebuffer(GL_DRAW_FRAMEBUFFER, toDefault ? 0 : m_framebuffers[BUF_Inter1]);
		glBindFramebuffer(GL_READ_FRAMEBUFFER, m_framebuffers[BUF_Input]);
		glBlitFramebuffer(0, 0, m_width, m_height, 0, 0, m_width, m_height, GL_COLOR_BUFFER_BIT, GL_NEAREST);
	}
	
	void BindFramebuffer(uint32_t index)
	{
		glBindFramebuffer(GL_FRAMEBUFFER, m_framebuffers[index]);
		usingDefaultFB = false;
	}
	
	void BindTexture(uint32_t index)
	{
		glActiveTexture(GL_TEXTURE0);
		glBindTexture(index == 0 ? GL_TEXTURE_2D_MULTISAMPLE : GL_TEXTURE_2D, m_textures[index]);
	}
	
private:
	uint32_t m_width, m_height;
	GLuint m_textures[4];
	GLuint m_framebuffers[3];
};

//C# bindings

CS_VISIBLE BlurFB* BlurFB_Create(uint32_t width, uint32_t height)
{
	return new BlurFB(width, height);
}

CS_VISIBLE void BlurFB_Destroy(BlurFB* blurFB)
{
	delete blurFB;
}

CS_VISIBLE void BlurFB_BindFramebuffer(BlurFB* blurFB, uint32_t index)
{
	blurFB->BindFramebuffer(index);
}

CS_VISIBLE void BlurFB_Resolve(BlurFB* blurFB, bool toDefault)
{
	blurFB->Resolve(toDefault);
}

CS_VISIBLE void BlurFB_BindTexture(BlurFB* blurFB, uint32_t index)
{
	blurFB->BindTexture(index);
}

CS_VISIBLE void Blur_DrawFST()
{
	glDrawArrays(GL_TRIANGLES, 0, 3);
}
