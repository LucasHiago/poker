using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Poker
{
	public class BlurEffect : IDisposable
	{
		public static BlurEffect Instance;
		
		private enum Buffers : uint
		{
			Input = 0,
			Inter1 = 1,
			Inter2 = 2
		}
		
		[DllImport("Native")]
		private static extern IntPtr BlurFB_Create(uint width, uint height);
		[DllImport("Native")]
		private static extern void BlurFB_Destroy(IntPtr blurFB);
		[DllImport("Native")]
		private static extern void BlurFB_BindFramebuffer(IntPtr blurFB, Buffers buffer);
		[DllImport("Native")]
		private static extern void BlurFB_Resolve(IntPtr blurFB, bool toDefault);
		[DllImport("Native")]
		private static extern void BlurFB_BindTexture(IntPtr blurFB, Buffers buffer);
		[DllImport("Native")]
		private static extern void Blur_DrawFST();
		
		private readonly Shader m_shader;
		private readonly int m_blurVectorUniformLocation;
		
		private float m_oneOverScreenWidth;
		private float m_oneOverScreenHeight;
		
		private IntPtr m_framebuffer;
		
		public BlurEffect()
		{
			m_shader = new Shader();
			m_shader.AttachStage(Shader.StageType.Vertex, "Blur.vs.glsl");
			m_shader.AttachStage(Shader.StageType.Fragment, "Blur.fs.glsl");
			m_shader.Link();
			
			m_blurVectorUniformLocation = m_shader.GetUniformLocation("blurVector");
		}
		
		public void BindInputFramebuffer()
		{
			BlurFB_BindFramebuffer(m_framebuffer, Buffers.Input);
		}
		
		public void RenderBlur(float intensity)
		{
			Graphics.SetFixedFunctionState(0);
			
			if (intensity < 0.0001f)
			{
				BlurFB_Resolve(m_framebuffer, true);
				return;
			}
			
			BlurFB_Resolve(m_framebuffer, false);
			
			m_shader.Bind();
			
			const int NUM_PASSES = 2;
			for (int i = 0; i < NUM_PASSES; i++)
			{
				// ** Horizontal pass **
				
				BlurFB_BindFramebuffer(m_framebuffer, Buffers.Inter2);
				BlurFB_BindTexture(m_framebuffer, Buffers.Inter1);
				
				m_shader.SetUniform(m_blurVectorUniformLocation, new Vector2(intensity * m_oneOverScreenWidth, 0));
				Blur_DrawFST();
				
				// ** Vertical pass **
				
				if (i == NUM_PASSES - 1)
					Graphics.BindDefaultFramebuffer();
				else
					BlurFB_BindFramebuffer(m_framebuffer, Buffers.Inter1);
				
				BlurFB_BindTexture(m_framebuffer, Buffers.Inter2);
				
				m_shader.SetUniform(m_blurVectorUniformLocation, new Vector2(0, intensity * m_oneOverScreenHeight));
				Blur_DrawFST();
			}
		}
		
		public void SetResolution(uint width, uint height)
		{
			DestroyFramebuffer();
			m_framebuffer = BlurFB_Create(width, height);
			
			m_oneOverScreenWidth = 1.0f / width;
			m_oneOverScreenHeight = 1.0f / height;
		}
		
		private void DestroyFramebuffer()
		{
			if (m_framebuffer != IntPtr.Zero)
				BlurFB_Destroy(m_framebuffer);
		}
		
		~BlurEffect()
		{
			DestroyFramebuffer();
		}
		
		public void Dispose()
		{
			m_shader.Dispose();
			DestroyFramebuffer();
			GC.SuppressFinalize(this);
		}
	}
}