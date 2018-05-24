using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Poker
{
	public unsafe class ShadowMapper : IDisposable
	{
		[DllImport("Native")]
		private static extern IntPtr SM_Create(uint resolution);
		[DllImport("Native")]
		private static extern void SM_Destroy(IntPtr handle);
		[DllImport("Native")]
		private static extern void SM_BindFramebuffer(IntPtr handle);
		[DllImport("Native")]
		private static extern void SM_BindTexture(IntPtr handle, uint unit);
		
		[DllImport("Native")]
		private static extern IntPtr SMB_Create(float* matrixPtr);
		[DllImport("Native")]
		private static extern void SMB_Destroy(IntPtr bufferHandle);
		[DllImport("Native")]
		private static extern void SMB_Bind(IntPtr bufferHandle, uint unit);
		
		private IntPtr m_shadowMapHandle = IntPtr.Zero;
		private readonly IntPtr m_shadowMatrixBuffer;
		
		private uint m_resolution = 1024;
		private bool m_resolutionChanged = true;
		
		public ShadowMapper(Vector3 lightDirection)
		{
			const float VOLUME_SIZE = 10;
			Vector3 shadowUp = Vector3.Normalize(Vector3.Cross(lightDirection, Vector3.UnitY));
			Matrix4x4 shadowMatrix = Matrix4x4.CreateLookAt(Vector3.Zero, lightDirection, shadowUp) *
			                         Matrix4x4.CreateOrthographic(VOLUME_SIZE, VOLUME_SIZE, -100, 100);
			
			m_shadowMatrixBuffer = SMB_Create(&shadowMatrix.M11);
		}
		
		public void Dispose()
		{
			SMB_Destroy(m_shadowMatrixBuffer);
			DestroyShadowMap();
			GC.SuppressFinalize(this);
		}
		
		~ShadowMapper()
		{
			SMB_Destroy(m_shadowMatrixBuffer);
			DestroyShadowMap();
		}
		
		private void DestroyShadowMap()
		{
			if (m_shadowMapHandle == IntPtr.Zero)
				return;
			SM_Destroy(m_shadowMapHandle);
			m_shadowMapHandle = IntPtr.Zero;
		}
		
		public void SetResolution(uint resolution)
		{
			m_resolution = resolution;
			m_resolutionChanged = true;
		}
		
		public void RenderShadows(Action renderCallback)
		{
			if (m_resolutionChanged)
			{
				DestroyShadowMap();
				m_shadowMapHandle = SM_Create(m_resolution);
				m_resolutionChanged = false;
			}
			
			SM_BindFramebuffer(m_shadowMapHandle);
			Graphics.ClearDepth();
			
			SMB_Bind(m_shadowMatrixBuffer, 0);
			
			renderCallback();
			
			Graphics.BindDefaultFramebuffer();
		}
		
		public void Bind()
		{
			const uint SHADOW_MAP_UNIT = 3;
			const uint MATRIX_BUFFER_UNIT = 1;
			
			SM_BindTexture(m_shadowMapHandle, SHADOW_MAP_UNIT);
			SMB_Bind(m_shadowMatrixBuffer, MATRIX_BUFFER_UNIT);
		}
	}
}
