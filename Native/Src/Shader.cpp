#include "Shader.h"
#include "API.h"
#include "Utils.h"

Shader::Shader()
{
	m_program = glCreateProgram();
}

Shader::~Shader()
{
	glDeleteProgram(m_program);
}

void Shader::AttachStage(StageType stageType, const char* code)
{
	GLenum glType = stageType == StageType::Vertex ? GL_VERTEX_SHADER : GL_FRAGMENT_SHADER;
	
	AttachShader(m_program, glType, code);
}

void Shader::Link()
{
	LinkProgram(m_program);
}

void Shader::Bind()
{
	glUseProgram(m_program);
}

int Shader::GetUniformLocation(const char* name)
{
	return glGetUniformLocation(m_program, name);
}

void Shader::SetUniform(uint32_t location, int32_t value)
{
	glProgramUniform1i(m_program, location, value);
}

void Shader::SetUniform(uint32_t location, float value)
{
	glProgramUniform1f(m_program, location, value);
}

void Shader::SetUniform(uint32_t location, float x, float y)
{
	glProgramUniform2f(m_program, location, x, y);
}

void Shader::SetUniform(uint32_t location, float x, float y, float z)
{
	glProgramUniform3f(m_program, location, x, y, z);
}

void Shader::SetUniform(uint32_t location, float x, float y, float z, float w)
{
	glProgramUniform4f(m_program, location, x, y, z, w);
}

void Shader::SetUniformMat4(uint32_t location, const float* matrix)
{
	glProgramUniformMatrix4fv(m_program, location, 1, GL_FALSE, matrix);
}


CS_VISIBLE Shader* SH_Create() { return new Shader(); }
CS_VISIBLE void SH_Destroy(Shader* shader) { delete shader; };

CS_VISIBLE void SH_Bind(Shader* shader) { shader->Bind(); }
CS_VISIBLE void SH_Link(Shader* shader) { shader->Link(); }

CS_VISIBLE void SH_AttachStage(Shader* shader, Shader::StageType stageType, const char* code)
{
	shader->AttachStage(stageType, code);
}

CS_VISIBLE int32_t SH_GetUniformLocation(Shader* shader, const char* uniformName)
{
	return shader->GetUniformLocation(uniformName);
}

CS_VISIBLE void SH_SetUniformI(Shader* shader, int32_t location, int32_t value)
{
	shader->SetUniform(location, value);
}

CS_VISIBLE void SH_SetUniformF(Shader* shader, int32_t location, float value)
{
	shader->SetUniform(location, value);
}

CS_VISIBLE void SH_SetUniformF2(Shader* shader, int32_t location, float x, float y)
{
	shader->SetUniform(location, x, y);
}

CS_VISIBLE void SH_SetUniformF3(Shader* shader, int32_t location, float x, float y, float z)
{
	shader->SetUniform(location, x, y, z);
}

CS_VISIBLE void SH_SetUniformF4(Shader* shader, int32_t location, float x, float y, float z, float w)
{
	shader->SetUniform(location, x, y, z, w);
}

CS_VISIBLE void SH_SetUniformMat4(Shader* shader, int32_t location, float* value)
{
	shader->SetUniformMat4(location, value);
}
