#include "Texture2D.h"
#include "API.h"
#include "Utils.h"

#define STB_IMAGE_IMPLEMENTATION
#include "stb_image.h"

#include <iostream>
#include <cmath>
#include <algorithm>

const int COMPONENT_COUNTS[] = { 1, 4, 4 };
const GLenum TEXTURE_INTERNAL_FORMATS[] = { GL_R8, GL_RGBA8, GL_SRGB8_ALPHA8 };
const GLenum TEXTURE_FORMATS[] = { GL_RED, GL_RGBA, GL_RGBA };

Texture2D::Texture2D(const char* path, TextureType type)
{
	int typeIndex = static_cast<int>(type);
	
	int width;
	int height;
	stbi_uc* texData = stbi_load(path, &width, &height, nullptr, COMPONENT_COUNTS[typeIndex]);
	if (texData == nullptr)
	{
		std::cerr << "Error loading texture from '" << path << "': " << stbi_failure_reason() << std::endl;
		std::terminate();
	}
	
	m_width = width;
	m_height = height;
	m_levels = GetMipLevels(m_width, m_height);
	
	glGenTextures(1, &m_handle);
	
	glBindTexture(GL_TEXTURE_2D, m_handle);
	glTexStorage2D(GL_TEXTURE_2D, m_levels, TEXTURE_INTERNAL_FORMATS[typeIndex], width, height);
	glTexSubImage2D(GL_TEXTURE_2D, 0, 0, 0, width, height, TEXTURE_FORMATS[typeIndex], GL_UNSIGNED_BYTE, texData);
	
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_R, GL_CLAMP_TO_EDGE);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR_MIPMAP_LINEAR);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
	
	glGenerateMipmap(GL_TEXTURE_2D);
	
	stbi_image_free(texData);
}

Texture2D::~Texture2D()
{
	glDeleteTextures(1, &m_handle);
}

void Texture2D::SetRepeat(bool repeat)
{
	GLenum wrapMode = repeat ? GL_REPEAT : GL_CLAMP_TO_EDGE;
	
	glBindTexture(GL_TEXTURE_2D, m_handle);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, wrapMode);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_R, wrapMode);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, wrapMode);
}

void Texture2D::Bind(uint32_t unit) const
{
	glActiveTexture(GL_TEXTURE0 + unit);
	glBindTexture(GL_TEXTURE_2D, m_handle);
}

inline GLenum TranslateSwizzleMode(SwizzleMode mode)
{
	switch (mode)
	{
	case SwizzleMode::R: return GL_RED;
	case SwizzleMode::G: return GL_GREEN;
	case SwizzleMode::B: return GL_BLUE;
	case SwizzleMode::A: return GL_ALPHA;
	case SwizzleMode::One: return GL_ONE;
	case SwizzleMode::Zero: return GL_ZERO;
	}
}

void Texture2D::SetSwizzle(SwizzleMode r, SwizzleMode g, SwizzleMode b, SwizzleMode a)
{
	glBindTexture(GL_TEXTURE_2D, m_handle);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_SWIZZLE_R, TranslateSwizzleMode(r));
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_SWIZZLE_G, TranslateSwizzleMode(g));
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_SWIZZLE_B, TranslateSwizzleMode(b));
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_SWIZZLE_A, TranslateSwizzleMode(a));
}

void Texture2D::SetLodBias(float bias)
{
	glBindTexture(GL_TEXTURE_2D, m_handle);
	glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_LOD_BIAS, bias);
}

// C# Bindings
CS_VISIBLE Texture2D* Tex2D_Load(const char* path, TextureType type) { return new Texture2D(path, type); }
CS_VISIBLE void Tex2D_Destroy(Texture2D* texture) { delete texture; }

CS_VISIBLE void Tex2D_SetSwizzle(Texture2D* texture, SwizzleMode r, SwizzleMode g, SwizzleMode b, SwizzleMode a)
{
	return texture->SetSwizzle(r, g, b, a);
}

CS_VISIBLE void Tex2D_SetRepeat(Texture2D* texture, bool repeat)
{
	return texture->SetRepeat(repeat);
}

CS_VISIBLE uint32_t Tex2D_GetWidth(Texture2D* texture) { return texture->GetWidth(); }
CS_VISIBLE uint32_t Tex2D_GetHeight(Texture2D* texture) { return texture->GetHeight(); }

CS_VISIBLE void Tex2D_Bind(Texture2D* texture, uint32_t unit) { return texture->Bind(unit); }

CS_VISIBLE void Tex2D_SetLodBias(Texture2D* texture, float bias) { texture->SetLodBias(bias); }
