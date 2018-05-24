#pragma once

#include <GL/glew.h>
#include <cstdint>

enum class VertexType : int32_t
{
	Standard = 0,
	Card = 1,
	Text = 2
};

class Mesh
{
public:
	Mesh(VertexType vertexType, uint32_t numVertices, void* vertices, uint32_t numIndices, uint32_t* indices);
	~Mesh();
	
	void Draw();
	void DrawInstanced(uint32_t numInstances);
	
private:
	inline GLuint GetVB() const
	{ return m_buffers[0]; }
	inline GLuint GetIB() const
	{ return m_buffers[1]; }
	
	uint32_t m_numIndices;
	
	GLuint m_buffers[2];
	GLuint m_vao;
};
