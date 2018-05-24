#include <GL/glew.h>
#include <iostream>

#include "API.h"
#include "Utils.h"

struct UniformBuffer
{
	uint64_t size;
	uint64_t frameStride;
	GLuint buffer;
	char* mappedMemory;
};

CS_VISIBLE UniformBuffer* UB_Create(uint64_t size)
{
	UniformBuffer* uniformBuffer = new UniformBuffer;
	uniformBuffer->size = size;
	uniformBuffer->frameStride = RoundToNextMultiple<uint64_t>(size, UniformBufferOffsetAlignment);
	
	uint64_t wholeBufferSize = uniformBuffer->frameStride * MAX_QUEUED_FRAMES;
	
	glGenBuffers(1, &uniformBuffer->buffer);
	glBindBuffer(GL_UNIFORM_BUFFER, uniformBuffer->buffer);
	glBufferStorage(GL_UNIFORM_BUFFER, wholeBufferSize, nullptr, GL_MAP_WRITE_BIT | GL_MAP_PERSISTENT_BIT);
	
	uniformBuffer->mappedMemory = static_cast<char*>(glMapBufferRange(GL_UNIFORM_BUFFER, 0, wholeBufferSize,
		GL_MAP_WRITE_BIT | GL_MAP_PERSISTENT_BIT | GL_MAP_FLUSH_EXPLICIT_BIT | GL_MAP_UNSYNCHRONIZED_BIT));
	
	return uniformBuffer;
}

CS_VISIBLE void UB_Destroy(UniformBuffer* uniformBuffer)
{
	glDeleteBuffers(1, &uniformBuffer->buffer);
	delete uniformBuffer;
}

CS_VISIBLE void* UB_GetMapping(UniformBuffer* uniformBuffer)
{
	return uniformBuffer->mappedMemory + uniformBuffer->frameStride * FrameQueueIndex;
}

CS_VISIBLE void UB_Flush(UniformBuffer* uniformBuffer)
{
	glBindBuffer(GL_UNIFORM_BUFFER, uniformBuffer->buffer);
	glFlushMappedBufferRange(GL_UNIFORM_BUFFER, uniformBuffer->frameStride * FrameQueueIndex, uniformBuffer->size);
}

CS_VISIBLE void UB_Bind(UniformBuffer* uniformBuffer, uint32_t unit)
{
	glBindBufferRange(GL_UNIFORM_BUFFER, unit, uniformBuffer->buffer,
	                  uniformBuffer->frameStride * FrameQueueIndex, uniformBuffer->size);
}