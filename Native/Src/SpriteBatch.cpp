#include "API.h"
#include "Utils.h"
#include "Texture2D.h"
#include "Graphics.h"

#include <GL/glew.h>
#include <vector>
#include <algorithm>
#include <cstring>

static const char* spriteVertexShader =
R"(#version 440 core

layout(location=0) in vec4 positionAndUV_in;
layout(location=1) in vec4 color_in;

layout(location=0) out vec2 texCoord_out;
layout(location=1) out vec4 color_out;

uniform vec2 scale;
uniform vec2 bias;

void main()
{
	texCoord_out = positionAndUV_in.zw;
	color_out = color_in;
	gl_Position = vec4(scale * positionAndUV_in.xy + bias, 0.0, 1.0);
}
)";

static const char* spriteFragmentShader =
R"(#version 440 core

layout(location=0) in vec2 texCoord_in;
layout(location=1) in vec4 color_in;

layout(binding=0) uniform sampler2D texSampler;

layout(location=0) out vec4 color_out;

void main()
{
	color_out = texture(texSampler, texCoord_in) * color_in;
}
)";

#pragma pack(push, 1)
struct Vertex
{
	float x;
	float y;
	float u;
	float v;
	uint8_t color[4];
};

struct Sprite
{
	float x;
	float y;
	float width;
	float height;
	float srcX;
	float srcY;
	float srcWidth;
	float srcHeight;
	uint8_t color[4];
};
#pragma pack(pop)

class SpriteBatch
{
public:
	SpriteBatch()
	{
		m_program = glCreateProgram();
		AttachShader(m_program, GL_VERTEX_SHADER, spriteVertexShader);
		AttachShader(m_program, GL_FRAGMENT_SHADER, spriteFragmentShader);
		LinkProgram(m_program);
		m_biasUniformLocation = glGetUniformLocation(m_program, "bias");
		m_scaleUniformLocation = glGetUniformLocation(m_program, "scale");
		
		GLuint vertexArrays[MAX_QUEUED_FRAMES];
		glGenVertexArrays(MAX_QUEUED_FRAMES, vertexArrays);
		for (uint32_t i = 0; i < MAX_QUEUED_FRAMES; i++)
			m_frames[i].m_vao = vertexArrays[i];
	}
	
	~SpriteBatch()
	{
		glDeleteProgram(m_program);
		
		for (const FrameEntry& frame : m_frames)
		{
			glDeleteVertexArrays(1, &frame.m_vao);
		}
	}
	
	void SetDisplaySize(float displayWidth, float displayHeight)
	{
		m_displayWidth = displayWidth;
		m_displayHeight = displayHeight;
		m_displaySizeSet = true;
		m_displaySizeChanged = true;
	}
	
	void Begin()
	{
		if (!m_displaySizeSet)
			Panic("SpriteBatch display size not set.");
		
		m_vertices.clear();
		m_indices.clear();
		m_textures.clear();
		
		if (FrameIndex != m_currentFrameIndex)
		{
			Frame().m_vertexPos = 0;
			Frame().m_indexPos = 0;
			m_currentFrameIndex = FrameIndex;
		}
	}
	
	void Draw(const Texture2D& texture, const Sprite& sprite)
	{
		const uint32_t indices[] = { 0, 1, 2, 1, 2, 3 };
		for (uint32_t i : indices)
			m_indices.push_back(m_vertices.size() + i);
		
		float minU = sprite.srcX / static_cast<float>(texture.GetWidth());
		float minV = sprite.srcY / static_cast<float>(texture.GetHeight());
		float maxU = (sprite.srcX + sprite.srcWidth) / static_cast<float>(texture.GetWidth());
		float maxV = (sprite.srcY + sprite.srcHeight) / static_cast<float>(texture.GetHeight());
		
		for (int ox = 0; ox < 2; ox++)
		{
			for (int oy = 0; oy < 2; oy++)
			{
				m_vertices.emplace_back();
				m_vertices.back().x = sprite.x + sprite.width * ox;
				m_vertices.back().y = sprite.y + sprite.height * oy;
				m_vertices.back().u = ox ? maxU : minU;
				m_vertices.back().v = oy ? maxV : minV;
				std::copy_n(sprite.color, 4, m_vertices.back().color);
			}
		}
		
		if (!m_textures.empty() && m_textures.back().m_texture == &texture)
		{
			m_textures.back().m_numIndices += 6;
		}
		else
		{
			m_textures.push_back({ &texture, static_cast<uint32_t>(m_indices.size() - 6), 6 });
		}
	}
	
	void End()
	{
		if (m_textures.empty())
			return;
		
		FrameEntry& frame = Frame();
		
		glBindVertexArray(frame.m_vao);
		
		uint64_t newVertexPos = frame.m_vertexPos + m_vertices.size();
		uint64_t newIndexPos = frame.m_indexPos + m_indices.size();
		
		if (newVertexPos > frame.m_vertexCapacity)
		{
			if (frame.m_vertexCapacity != 0)
				glDeleteBuffers(1, &frame.m_vertexBuffer);
			glGenBuffers(1, &frame.m_vertexBuffer);
			glBindBuffer(GL_ARRAY_BUFFER, frame.m_vertexBuffer);
			
			frame.m_vertexCapacity = RoundToNextMultiple<uint64_t>(newVertexPos, 1024);
			const uint64_t bufferBytes = sizeof(Vertex) * frame.m_vertexCapacity;
			
			glBufferStorage(GL_ARRAY_BUFFER, bufferBytes, nullptr, GL_MAP_WRITE_BIT | GL_MAP_PERSISTENT_BIT);
			
			frame.m_vertices = static_cast<Vertex*>(glMapBufferRange(GL_ARRAY_BUFFER, 0, bufferBytes,
				GL_MAP_WRITE_BIT | GL_MAP_PERSISTENT_BIT | GL_MAP_FLUSH_EXPLICIT_BIT | GL_MAP_UNSYNCHRONIZED_BIT));
			
			newVertexPos = m_vertices.size();
			frame.m_vertexPos = 0;
			
			glEnableVertexAttribArray(0);
			glEnableVertexAttribArray(1);
			
			glVertexAttribPointer(0, 4, GL_FLOAT, GL_FALSE, sizeof(Vertex), nullptr);
			glVertexAttribPointer(1, 4, GL_UNSIGNED_BYTE, GL_TRUE, sizeof(Vertex),
			                      reinterpret_cast<void*>(offsetof(Vertex, color)));
		}
		else
		{
			glBindBuffer(GL_ARRAY_BUFFER, frame.m_vertexBuffer);
		}
		
		if (newIndexPos > frame.m_indexCapacity)
		{
			if (frame.m_indexCapacity != 0)
				glDeleteBuffers(1, &frame.m_indexBuffer);
			glGenBuffers(1, &frame.m_indexBuffer);
			glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, frame.m_indexBuffer);
			
			frame.m_indexCapacity = RoundToNextMultiple<uint64_t>(newIndexPos, 1024);
			const uint64_t bufferBytes = sizeof(uint32_t) * frame.m_indexCapacity;
			
			glBufferStorage(GL_ELEMENT_ARRAY_BUFFER, bufferBytes, nullptr, GL_MAP_WRITE_BIT | GL_MAP_PERSISTENT_BIT);
			
			frame.m_indices = static_cast<uint32_t*>(glMapBufferRange(GL_ELEMENT_ARRAY_BUFFER, 0, bufferBytes,
				GL_MAP_WRITE_BIT | GL_MAP_PERSISTENT_BIT | GL_MAP_FLUSH_EXPLICIT_BIT | GL_MAP_UNSYNCHRONIZED_BIT));
			
			newIndexPos = m_indices.size();
			frame.m_indexPos = 0;
		}
		else
		{
			glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, frame.m_indexBuffer);
		}
		
		const uint64_t verticesBytes = m_vertices.size() * sizeof(Vertex);
		const uint64_t indicesBytes = m_indices.size() * sizeof(uint32_t);
		
		std::memcpy(frame.m_vertices + frame.m_vertexPos, m_vertices.data(), verticesBytes);
		std::memcpy(frame.m_indices + frame.m_indexPos, m_indices.data(), indicesBytes);
		
		glFlushMappedBufferRange(GL_ARRAY_BUFFER, frame.m_vertexPos * sizeof(Vertex), verticesBytes);
		glFlushMappedBufferRange(GL_ELEMENT_ARRAY_BUFFER, frame.m_indexPos * sizeof(uint32_t), indicesBytes);
		
		glUseProgram(m_program);
		
		if (m_displaySizeChanged)
		{
			glUniform2f(m_scaleUniformLocation, 2.0f / m_displayWidth, -2.0f / m_displayHeight);
			glUniform2f(m_biasUniformLocation, -1.0f, 1.0f);
			m_displaySizeChanged = false;
		}
		
		for (const TextureEntry& textureEntry : m_textures)
		{
			textureEntry.m_texture->Bind(0);
			
			const uint32_t firstIndex = textureEntry.m_firstIndex + frame.m_indexPos;
			glDrawElementsBaseVertex(GL_TRIANGLES, textureEntry.m_numIndices, GL_UNSIGNED_INT,
			                         reinterpret_cast<void*>(sizeof(uint32_t) * firstIndex), frame.m_vertexPos);
		}
		
		frame.m_vertexPos = newVertexPos;
		frame.m_indexPos = newIndexPos;
	}
	
private:
	float m_displayWidth;
	float m_displayHeight;
	bool m_displaySizeSet = false;
	bool m_displaySizeChanged = false;
	
	struct TextureEntry
	{
		const Texture2D* m_texture;
		uint32_t m_firstIndex;
		uint32_t m_numIndices;
	};
	
	std::vector<Vertex> m_vertices;
	std::vector<uint32_t> m_indices;
	std::vector<TextureEntry> m_textures;
	
	struct FrameEntry
	{
		uint64_t m_vertexPos = 0;
		uint64_t m_indexPos = 0;
		
		uint64_t m_vertexCapacity = 0;
		uint64_t m_indexCapacity = 0;
		
		GLuint m_vertexBuffer;
		GLuint m_indexBuffer;
		
		Vertex* m_vertices;
		uint32_t* m_indices;
		
		GLuint m_vao;
	};
	
	inline FrameEntry& Frame()
	{ return m_frames[FrameQueueIndex]; }
	
	FrameEntry m_frames[MAX_QUEUED_FRAMES];
	uint32_t m_currentFrameIndex = 0;
	
	GLint m_scaleUniformLocation;
	GLint m_biasUniformLocation;
	GLuint m_program;
};

// C# Bindings
CS_VISIBLE SpriteBatch* SB_Create() { return new SpriteBatch; }
CS_VISIBLE void SB_Destroy(SpriteBatch* spriteBatch) { delete spriteBatch; }

CS_VISIBLE void SB_SetDisplaySize(SpriteBatch* spriteBatch, float displayWidth, float displayHeight)
{
	spriteBatch->SetDisplaySize(displayWidth, displayHeight);
}

CS_VISIBLE void SB_Begin(SpriteBatch* spriteBatch)
{
	spriteBatch->Begin();
}

CS_VISIBLE void SB_End(SpriteBatch* spriteBatch)
{
	spriteBatch->End();
}

CS_VISIBLE void SB_Draw(SpriteBatch* spriteBatch, const Texture2D* texture, const Sprite* sprite)
{
	spriteBatch->Draw(*texture, *sprite);
}
