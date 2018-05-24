using System;
using System.Collections.Generic;
using System.Numerics;

namespace Poker
{
	public class PlayerNameRenderer : IDisposable
	{
		private readonly Shader m_shader;
		
		private readonly UniformBuffer m_colorUB;
		
		private Mesh m_mesh;
		private SpriteFont m_font;
		
		public PlayerNameRenderer()
		{
			m_shader = new Shader();
			m_shader.AttachStage(Shader.StageType.Vertex, "PlayerName.vs.glsl");
			m_shader.AttachStage(Shader.StageType.Fragment, "PlayerName.fs.glsl");
			m_shader.Link();
			
			m_colorUB = new UniformBuffer(sizeof(float) * 4 * 10);
		}
		
		public void Dispose()
		{
			m_shader.Dispose();
			m_colorUB.Dispose();
			m_mesh?.Dispose();
		}
		
		public void SetMesh(SpriteFont font, Mesh mesh)
		{
			m_mesh?.Dispose();
			m_mesh = mesh;
			m_font = font;
		}
		
		public unsafe void Draw(IEnumerable<Color> playerColors)
		{
			float* colorPtr = (float*)m_colorUB.GetMapping();
			foreach (Color color in playerColors)
			{
				colorPtr[0] = color.R / 255.0f;
				colorPtr[1] = color.G / 255.0f;
				colorPtr[2] = color.B / 255.0f;
				colorPtr[3] = color.A / 255.0f;
				colorPtr += 4;
			}
			
			m_colorUB.Flush();
			m_colorUB.Bind(1);
			
			m_font.Texture.Bind(0);
			
			m_shader.Bind();
			
			Graphics.SetFixedFunctionState(FFState.AlphaBlend | FFState.DepthTest);
			
			m_mesh.Draw();
		}
	}
}
