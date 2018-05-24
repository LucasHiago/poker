#include "Utils.h"
#include <iostream>

uint32_t FrameQueueIndex = 0;
uint32_t FrameIndex = 0;

GLint UniformBufferOffsetAlignment = 0;
GLint SSBOOffsetAlignment = 0;

void Panic(const char* message)
{
	std::cerr << message << std::endl;
	std::exit(1);
}

void AttachShader(GLuint program, GLenum type, const char* shaderSource)
{
	GLuint shader = glCreateShader(type);
	
	const GLchar* shaderSourceGL = reinterpret_cast<const GLchar*>(shaderSource);
	glShaderSource(shader, 1, &shaderSourceGL, nullptr);
	
	glCompileShader(shader);
	
	GLint compileStatus;
	glGetShaderiv(shader, GL_COMPILE_STATUS, &compileStatus);
	if (compileStatus == GL_FALSE)
	{
		GLint infoLogLength;
		glGetShaderiv(shader, GL_INFO_LOG_LENGTH, &infoLogLength);
		
		char* infoLogBuffer = static_cast<char*>(alloca(infoLogLength));
		glGetShaderInfoLog(shader, infoLogLength, nullptr, reinterpret_cast<GLchar*>(infoLogBuffer));
		
		std::cout << shaderSource << std::endl;
		Panic(infoLogBuffer);
	}
	
	glAttachShader(program, shader);
}

void LinkProgram(GLuint program)
{
	glLinkProgram(program);
	
	GLint linkStatus;
	glGetProgramiv(program, GL_LINK_STATUS, &linkStatus);
	if (linkStatus == GL_FALSE)
	{
		GLint infoLogLength;
		glGetProgramiv(program, GL_INFO_LOG_LENGTH, &infoLogLength);
		
		char* infoLogBuffer = static_cast<char*>(alloca(infoLogLength));
		glGetProgramInfoLog(program, infoLogLength, nullptr, reinterpret_cast<GLchar*>(infoLogBuffer));
		
		Panic(infoLogBuffer);
	}
}
