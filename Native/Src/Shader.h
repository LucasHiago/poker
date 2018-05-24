#pragma once

#include <GL/glew.h>
#include <cstdint>

class Shader
{
public:
	enum class StageType : int32_t
	{
		Vertex = 0,
		Fragment = 1
	};
	
	Shader();
	~Shader();
	
	void AttachStage(StageType stageType, const char* code);
	void Link();
	
	void Bind();
	
	int GetUniformLocation(const char* name);
	
	void SetUniform(uint32_t location, int32_t value);
	void SetUniform(uint32_t location, float value);
	void SetUniform(uint32_t location, float x, float y);
	void SetUniform(uint32_t location, float x, float y, float z);
	void SetUniform(uint32_t location, float x, float y, float z, float w);
	void SetUniformMat4(uint32_t location, const float* matrix);
	
private:
	GLuint m_program;
};
