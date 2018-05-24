#include <GL/glew.h>
#include <cstring>

#include "API.h"
#include "Utils.h"

#pragma pack(push, 1)
struct Chip
{
	float x;
	float y;
	float z;
	float rotation;
};
#pragma pack(pop)

class ChipsBuffer
{
public:
	inline ChipsBuffer(size_t maxChips)
	{
		m_pageSize = RoundToNextMultiple<size_t>(maxChips * sizeof(float) * 4, SSBOOffsetAlignment);
		size_t bufferSize = m_pageSize * MAX_QUEUED_FRAMES;
		
		glGenBuffers(1, &m_buffer);
		glBindBuffer(GL_SHADER_STORAGE_BUFFER, m_buffer);
		glBufferStorage(GL_SHADER_STORAGE_BUFFER, bufferSize, nullptr,
		                GL_MAP_WRITE_BIT | GL_MAP_PERSISTENT_BIT);
		
		m_bufferMapping = static_cast<char*>(glMapBufferRange(GL_SHADER_STORAGE_BUFFER, 0, bufferSize,
				GL_MAP_WRITE_BIT | GL_MAP_PERSISTENT_BIT | GL_MAP_FLUSH_EXPLICIT_BIT | GL_MAP_UNSYNCHRONIZED_BIT));
	}
	
	inline ~ChipsBuffer()
	{
		glDeleteBuffers(1, &m_buffer);
	}
	
	inline void Upload(void* chips, uint64_t count)
	{
		if (count == 0)
			return;
		
		const size_t bufferOffset = m_pageSize * FrameQueueIndex;
		const size_t uploadSize = count * sizeof(float) * 4;
		
		std::memcpy(m_bufferMapping + bufferOffset, chips, uploadSize);
		
		glBindBuffer(GL_SHADER_STORAGE_BUFFER, m_buffer);
		glFlushMappedBufferRange(GL_SHADER_STORAGE_BUFFER, bufferOffset, uploadSize);
	}
	
	inline void Bind(uint32_t unit)
	{
		glBindBufferRange(GL_SHADER_STORAGE_BUFFER, unit, m_buffer, m_pageSize * FrameQueueIndex, m_pageSize);
	}
	
private:
	size_t m_pageSize;
	GLuint m_buffer;
	char* m_bufferMapping;
};

CS_VISIBLE ChipsBuffer* CB_Create(uint64_t maxChips)
{
	return new ChipsBuffer(maxChips);
}

CS_VISIBLE void CB_Destroy(ChipsBuffer* buffer)
{
	delete buffer;
}

CS_VISIBLE void CB_Upload(ChipsBuffer* buffer, void* chips, uint64_t count)
{
	buffer->Upload(chips, count);
}

CS_VISIBLE void CB_Bind(ChipsBuffer* buffer, uint32_t unit)
{
	buffer->Bind(unit);
}