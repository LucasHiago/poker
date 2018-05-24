using System;

namespace Poker
{
	public class BoardModel : IDisposable
	{
		public static BoardModel Instance;
		
		private readonly Texture2D m_greenRubberDiffuseTexture;
		private readonly Texture2D m_greenRubberNormalMap;
		private readonly Texture2D m_greenRubberSpecularMap;
		
		private readonly Texture2D m_woodDiffuseTexture;
		private readonly Texture2D m_woodNormalMap;
		private readonly Texture2D m_woodSpecularMap;
		
		private readonly ModelInstance m_boardModel;
		
		private readonly BoardShader m_boardShader;
		
		public BoardModel()
		{
			m_boardShader = new BoardShader();
			
			m_greenRubberDiffuseTexture = Texture2D.Load("Textures/RubberD.png", Texture2D.Type.sRGB32);
			m_greenRubberNormalMap = Texture2D.Load("Textures/RubberN.png", Texture2D.Type.Linear32);
			m_greenRubberSpecularMap = Texture2D.Load("Textures/RubberS.png", Texture2D.Type.Linear8);
			
			m_greenRubberDiffuseTexture.SetRepeat(true);
			m_greenRubberNormalMap.SetRepeat(true);
			m_greenRubberSpecularMap.SetRepeat(true);
			
			MaterialSettings greenRubberMaterial = new MaterialSettings
			{
				SpecularIntensity = 0.4f,
				SpecularExponent = 10.0f,
				DiffuseTexture = m_greenRubberDiffuseTexture,
				NormalMap = m_greenRubberNormalMap,
				SpecularMap = m_greenRubberSpecularMap,
				TextureScale = 4
			};
			
			m_woodDiffuseTexture = Texture2D.Load("Textures/WoodD.png", Texture2D.Type.sRGB32);
			m_woodNormalMap = Texture2D.Load("Textures/WoodN.png", Texture2D.Type.Linear32);
			m_woodSpecularMap = Texture2D.Load("Textures/WoodS.png", Texture2D.Type.Linear8);
			
			m_woodDiffuseTexture.SetRepeat(true);
			m_woodNormalMap.SetRepeat(true);
			m_woodSpecularMap.SetRepeat(true);
			
			MaterialSettings woodMaterial = new MaterialSettings
			{
				SpecularIntensity = 0.9f,
				SpecularExponent = 10.0f,
				DiffuseTexture = m_woodDiffuseTexture,
				NormalMap = m_woodNormalMap,
				SpecularMap = m_woodSpecularMap,
				TextureScale = 2
			};
			
			m_boardModel = new ModelInstance(Assets.BoardModel);
			m_boardModel.SetMaterial("Board_0", greenRubberMaterial);
			m_boardModel.SetMaterial("Board_1", woodMaterial);
		}
		
		public void DrawShadow()
		{
			m_boardShader.BindShadow();
			m_boardModel.Draw(null);
		}
		
		public void DrawNormal()
		{
			m_boardShader.Bind();
			m_boardModel.Draw(m_boardShader);
		}
		
		public void Dispose()
		{
			m_boardShader.Dispose();
			m_greenRubberDiffuseTexture.Dispose();
			m_greenRubberNormalMap.Dispose();
			m_greenRubberSpecularMap.Dispose();
			m_woodDiffuseTexture.Dispose();
			m_woodNormalMap.Dispose();
			m_woodSpecularMap.Dispose();
		}
	}
}