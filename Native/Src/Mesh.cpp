#include "Mesh.h"
#include "API.h"

const uint32_t VERTEX_SIZES[] =
{
	/* Standard */ sizeof(float) * (3 + 3 + 3 + 2),
	/* Card     */ sizeof(float) * 2,
	/* Text     */ sizeof(float) * (3 + 2) + sizeof(uint32_t),
};

Mesh::Mesh(VertexType vertexType, uint32_t numVertices, void* vertices, uint32_t numIndices, uint32_t* indices)
	: m_numIndices(numIndices)
{
	glGenBuffers(2, m_buffers);
	
	glGenVertexArrays(1, &m_vao);
	glBindVertexArray(m_vao);
	
	const uint32_t vertexSize = VERTEX_SIZES[static_cast<int>(vertexType)];
	
	glBindBuffer(GL_ARRAY_BUFFER, GetVB());
	glBufferStorage(GL_ARRAY_BUFFER, vertexSize * numVertices, vertices, 0);
	
	glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, GetIB());
	glBufferStorage(GL_ELEMENT_ARRAY_BUFFER, sizeof(uint32_t) * numIndices, indices, 0);
	
	switch (vertexType)
	{
	case VertexType::Standard:
	{
		const uint32_t NUM_ATTRIB_ARRAYS = 4;
		
		for (int i = 0; i < NUM_ATTRIB_ARRAYS; i++)
			glEnableVertexAttribArray(i);
		glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, vertexSize, reinterpret_cast<void*>(sizeof(float) * 0));
		glVertexAttribPointer(1, 3, GL_FLOAT, GL_FALSE, vertexSize, reinterpret_cast<void*>(sizeof(float) * 3));
		glVertexAttribPointer(2, 3, GL_FLOAT, GL_FALSE, vertexSize, reinterpret_cast<void*>(sizeof(float) * 6));
		glVertexAttribPointer(3, 2, GL_FLOAT, GL_FALSE, vertexSize, reinterpret_cast<void*>(sizeof(float) * 9));
		break;
	}
	case VertexType::Card:
	{
		glEnableVertexAttribArray(0);
		glVertexAttribPointer(0, 2, GL_FLOAT, GL_FALSE, vertexSize, nullptr);
		break;
	}
	case VertexType::Text:
	{
		const uint32_t NUM_ATTRIB_ARRAYS = 3;
		
		for (int i = 0; i < NUM_ATTRIB_ARRAYS; i++)
			glEnableVertexAttribArray(i);
		glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, vertexSize, reinterpret_cast<void*>(sizeof(float) * 0));
		glVertexAttribPointer(1, 2, GL_FLOAT, GL_FALSE, vertexSize, reinterpret_cast<void*>(sizeof(float) * 3));
		glVertexAttribIPointer(2, 1, GL_UNSIGNED_INT, vertexSize, reinterpret_cast<void*>(sizeof(float) * 5));
		break;
	}
	}
}

Mesh::~Mesh()
{
	glDeleteBuffers(2, m_buffers);
	glDeleteVertexArrays(1, &m_vao);
}

void Mesh::Draw()
{
	glBindVertexArray(m_vao);
	glDrawElements(GL_TRIANGLES, m_numIndices, GL_UNSIGNED_INT, nullptr);
}

void Mesh::DrawInstanced(uint32_t numInstances)
{
	glBindVertexArray(m_vao);
	glDrawElementsInstanced(GL_TRIANGLES, m_numIndices, GL_UNSIGNED_INT, nullptr, numInstances);
}

CS_VISIBLE Mesh* Mesh_Create(VertexType vertexType, uint32_t numVertices, void* vertices, uint32_t numIndices, uint32_t* indices)
{
	return new Mesh(vertexType, numVertices, vertices, numIndices, indices);
}

CS_VISIBLE void Mesh_Destroy(Mesh* mesh) { delete mesh; }

CS_VISIBLE void Mesh_Draw(Mesh* mesh) { mesh->Draw(); }
CS_VISIBLE void Mesh_DrawInstanced(Mesh* mesh, uint32_t numInstances) { mesh->DrawInstanced(numInstances); }
