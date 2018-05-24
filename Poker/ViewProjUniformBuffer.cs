using System;
using System.Numerics;

namespace Poker
{
	public class ViewProjUniformBuffer : IDisposable
	{
		private readonly UniformBuffer m_uniformBuffer;
		
		public ViewProjUniformBuffer()
		{
			m_uniformBuffer = new UniformBuffer(sizeof(float) * (4 * 4 * 2 + 4));
		}
		
		public void Bind(uint unit)
		{
			m_uniformBuffer.Bind(unit);
		}
		
		public unsafe void Update(ref Matrix4x4 viewProj, Vector3 cameraPos)
		{
			void* bufferMemory = m_uniformBuffer.GetMapping();
			Matrix4x4* matricesMemory = (Matrix4x4*)bufferMemory;
			
			matricesMemory[0] = viewProj;
			Matrix4x4.Invert(viewProj, out matricesMemory[1]);
			
			float* cameraPosMemory = (float*)(matricesMemory + 2);
			cameraPosMemory[0] = cameraPos.X;
			cameraPosMemory[1] = cameraPos.Y;
			cameraPosMemory[2] = cameraPos.Z;
			
			m_uniformBuffer.Flush();
		}
		
		public void Dispose()
		{
			m_uniformBuffer.Dispose();
		}
	}
}
