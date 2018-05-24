using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Poker
{
	public unsafe class Mesh : IDisposable
	{
		private enum VertexType
		{
			Standard = 0,
			Card     = 1,
			Text     = 2
		}
		
		[DllImport("Native")]
		private static extern IntPtr Mesh_Create(VertexType vertexType, uint numVertices, void* vertices,
		                                         uint numIndices, uint* indices);
		[DllImport("Native")]
		private static extern void Mesh_Destroy(IntPtr mesh);
		[DllImport("Native")]
		private static extern void Mesh_Draw(IntPtr mesh);
		[DllImport("Native")]
		private static extern void Mesh_DrawInstanced(IntPtr mesh, uint numInstances);
		
		private readonly IntPtr m_handle;
		
		public Mesh(Vertex[] vertices, uint[] indices, uint numVertices = 0, uint numIndices = 0)
		{
			if (numVertices == 0)
				numVertices = (uint)vertices.Length;
			if (numIndices == 0)
				numIndices = (uint)indices.Length;
			
			fixed (uint* indicesPtr = indices)
			{
				fixed (Vertex* verticesPtr = vertices)
				{
					m_handle = Mesh_Create(VertexType.Standard, numVertices, verticesPtr, numIndices, indicesPtr);
				}
			}
		}
		
		public Mesh(Vector2[] vertices, uint[] indices, uint numVertices = 0, uint numIndices = 0)
		{
			if (numVertices == 0)
				numVertices = (uint)vertices.Length;
			if (numIndices == 0)
				numIndices = (uint)indices.Length;
			
			fixed (uint* indicesPtr = indices)
			{
				fixed (Vector2* verticesPtr = vertices)
				{
					m_handle = Mesh_Create(VertexType.Card, numVertices, verticesPtr, numIndices, indicesPtr);
				}
			}
		}
		
		public Mesh(TextVertex[] vertices, uint[] indices, uint numVertices = 0, uint numIndices = 0)
		{
			if (numVertices == 0)
				numVertices = (uint)vertices.Length;
			if (numIndices == 0)
				numIndices = (uint)indices.Length;
			
			fixed (uint* indicesPtr = indices)
			{
				fixed (TextVertex* verticesPtr = vertices)
				{
					m_handle = Mesh_Create(VertexType.Text, numVertices, verticesPtr, numIndices, indicesPtr);
				}
			}
		}
		
		public void Dispose()
		{
			Mesh_Destroy(m_handle);
			GC.SuppressFinalize(this);
		}
		
		~Mesh()
		{
			Mesh_Destroy(m_handle);
		}
		
		public void Draw()
		{
			Mesh_Draw(m_handle);
		}
		
		public void DrawInstanced(uint numInstances)
		{
			Mesh_DrawInstanced(m_handle, numInstances);
		}
	}
}
