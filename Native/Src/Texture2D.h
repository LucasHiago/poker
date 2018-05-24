#pragma once

#include <GL/glew.h>
#include <cstdint>

enum class TextureType : uint32_t
{
	Linear8 = 0,
	Linear32 = 1,
	sRGB32 = 2
};

enum class SwizzleMode : uint8_t
{
	R = 0,
	G = 1,
	B = 2,
	A = 3,
	One = 4,
	Zero = 5
};

class Texture2D
{
public:
	Texture2D(const char* path, TextureType type);
	~Texture2D();
	
	inline uint32_t GetWidth() const
	{ return m_width; }
	
	inline uint32_t GetHeight() const
	{ return m_height; }
	
	void Bind(uint32_t unit) const;
	
	void SetLodBias(float bias);
	
	void SetSwizzle(SwizzleMode r, SwizzleMode g, SwizzleMode b, SwizzleMode a);
	
	void SetRepeat(bool repeat);
	
private:
	GLuint m_handle;
	uint32_t m_width;
	uint32_t m_height;
	uint32_t m_levels;
};