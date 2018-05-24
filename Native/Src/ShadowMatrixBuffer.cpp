#include "API.h"

#include <GL/glew.h>
#include <cstdint>

class ShadowMatrixBuffer
{
public:
	explicit ShadowMatrixBuffer(const float* matrixPtr)
	{
		glGenBuffers(1, &m_buffer);
		glBindBuffer(GL_UNIFORM_BUFFER, m_buffer);
		glBufferStorage(GL_UNIFORM_BUFFER, sizeof(float) * 4 * 4, matrixPtr, 0);
	}
	
	~ShadowMatrixBuffer()
	{
		glDeleteBuffers(1, &m_buffer);
	}
	
	void Bind(uint32_t unit)
	{
		glBindBufferBase(GL_UNIFORM_BUFFER, unit, m_buffer);
	}
	
private:
	GLuint m_buffer;
};

CS_VISIBLE ShadowMatrixBuffer* SMB_Create(float* matrixPtr)
{
	return new ShadowMatrixBuffer(matrixPtr);
}

CS_VISIBLE void SMB_Destroy(ShadowMatrixBuffer* buffer)
{
	delete buffer;
}

CS_VISIBLE void SMB_Bind(ShadowMatrixBuffer* buffer, uint32_t unit)
{
	buffer->Bind(unit);
}