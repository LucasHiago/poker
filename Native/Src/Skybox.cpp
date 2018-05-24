#include "API.h"
#include "Utils.h"
#include "Shader.h"
#include "Graphics.h"
#include "stb_image.h"

#include <sstream>
#include <string>
#include <iostream>
#include <memory>
#include <GL/glew.h>

GLuint g_skyboxTexture;
GLuint g_skyboxVAO;

static const uint32_t RESOLUTION = 1024;

CS_VISIBLE void SKY_Load(const char* dirPath)
{
	glGenTextures(1, &g_skyboxTexture);
	
	glBindTexture(GL_TEXTURE_CUBE_MAP, g_skyboxTexture);
	glTexStorage2D(GL_TEXTURE_CUBE_MAP, 1, GL_RGBA8, RESOLUTION, RESOLUTION);
	
	const char* fileNames[] = { "PosX.png", "NegX.png", "PosY.png", "NegY.png", "PosZ.png", "NegZ.png" };
	for (int i = 0; i < 6; i++)
	{
		std::stringstream pathStream;
		pathStream << dirPath << "/" << fileNames[i];
		std::string path = pathStream.str();
		
		int width, height;
		stbi_uc* facePixels = stbi_load(path.c_str(), &width, &height, nullptr, 4);
		if (facePixels == nullptr)
		{
			std::cerr << "Error loading skybox image '" << path << "': " << stbi_failure_reason() << std::endl;
			std::terminate();
		}
		
		if (width != RESOLUTION || height != RESOLUTION)
		{
			std::cerr << "Skybox image '" << path << "' has incorrect resolution " << width << "x" << height <<
			             " (expected " << RESOLUTION << "x" << RESOLUTION << ")" << std::endl;
			std::terminate();
		}
		
		glTexSubImage2D(GL_TEXTURE_CUBE_MAP_POSITIVE_X + i, 0, 0, 0, RESOLUTION, RESOLUTION, GL_RGBA, GL_UNSIGNED_BYTE, facePixels);
		
		stbi_image_free(facePixels);
	}
	
	glGenVertexArrays(1, &g_skyboxVAO);
}

CS_VISIBLE void SKY_Destroy()
{
	glDeleteTextures(1, &g_skyboxTexture);
	glDeleteVertexArrays(1, &g_skyboxVAO);
}

CS_VISIBLE void SKY_Draw(Shader* shader)
{
	shader->Bind();
	SetFixedFunctionState(FF_DepthTest);
	
	glActiveTexture(GL_TEXTURE0);
	glBindTexture(GL_TEXTURE_CUBE_MAP, g_skyboxTexture);
	
	glBindVertexArray(g_skyboxVAO);
	
	glDrawArrays(GL_TRIANGLES, 0, 3);
}
