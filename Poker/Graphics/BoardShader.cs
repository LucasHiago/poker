using System;
using System.Numerics;

namespace Poker
{
	public class BoardShader : IDisposable
	{
		private readonly Shader m_shader;
		private readonly Shader m_shadowShader;
		
		private readonly int m_specularExponentLocation;
		private readonly int m_specularIntensityLocation;
		private readonly int m_textureScaleLocation;
		
		public BoardShader()
		{
			m_shader = new Shader();
			m_shader.AttachStage(Shader.StageType.Vertex, "Board.vs.glsl");
			m_shader.AttachStage(Shader.StageType.Fragment, "Board.fs.glsl");
			m_shader.Link();
			
			m_shadowShader = new Shader();
			m_shadowShader.AttachStage(Shader.StageType.Vertex, "BoardShadow.vs.glsl");
			m_shadowShader.Link();
			
			m_specularExponentLocation = m_shader.GetUniformLocation("specularExponent");
			m_specularIntensityLocation = m_shader.GetUniformLocation("specularIntensity");
			m_textureScaleLocation = m_shader.GetUniformLocation("textureScale");
		}
		
		public void Bind()
		{
			m_shader.Bind();
		}
		
		public void BindShadow()
		{
			m_shadowShader.Bind();
		}
		
		public void BindSettings(MaterialSettings settings)
		{
			settings.DiffuseTexture.Bind(0);
			settings.NormalMap.Bind(1);
			settings.SpecularMap.Bind(2);
			
			m_shader.SetUniform(m_textureScaleLocation, settings.TextureScale);
			m_shader.SetUniform(m_specularExponentLocation, settings.SpecularExponent);
			m_shader.SetUniform(m_specularIntensityLocation, settings.SpecularIntensity);
		}
		
		public void Dispose()
		{
			m_shader.Dispose();
			m_shadowShader.Dispose();
		}
	}
}
