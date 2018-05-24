#pragma once

#include <GL/glew.h>
#include <cmath>
#include <algorithm>

constexpr uint32_t MAX_QUEUED_FRAMES = 3;

extern uint32_t FrameQueueIndex;
extern uint32_t FrameIndex;

extern GLint UniformBufferOffsetAlignment;
extern GLint SSBOOffsetAlignment;

void Panic(const char* message);

void AttachShader(GLuint program, GLenum type, const char* shaderSource);
void LinkProgram(GLuint program);

inline uint32_t GetMipLevels(uint32_t width, uint32_t height)
{
	return static_cast<uint32_t>(std::log2(std::max(width, height))) + 1;
}

template <typename T>
constexpr inline T RoundToNextMultiple(T value, T multiple)
{
	T valModMul = value % multiple;
	return valModMul == 0 ? value : (value + multiple - valModMul);
}
