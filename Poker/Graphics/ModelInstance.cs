using System;

namespace Poker
{
	public class ModelInstance
	{
		private readonly GLTF.Model m_model;
		private readonly MaterialSettings[] m_materialSettings;
		
		public ModelInstance(GLTF.Model model, MaterialSettings material = null)
		{
			m_model = model;
			m_materialSettings = new MaterialSettings[m_model.Meshes.Length];
			SetMaterial(material);
		}
		
		public void SetMaterial(MaterialSettings materialSettings)
		{
			for (int i = 0; i < m_materialSettings.Length; i++)
				m_materialSettings[i] = materialSettings;
		}
		
		public void SetMaterial(string meshName, MaterialSettings materialSettings)
		{
			int meshIndex = Array.FindIndex(m_model.Meshes, mesh => mesh.Name == meshName);
			if (meshIndex == -1)
				throw new ArgumentException("Mesh not found '" + meshName + "'.", nameof(meshName));
			m_materialSettings[meshIndex] = materialSettings;
		}
		
		public void Draw(BoardShader boardShader)
		{
			for (int i = 0; i < m_model.Meshes.Length; i++)
			{
				boardShader?.BindSettings(m_materialSettings[i]);
				m_model.Meshes[i].Draw();
			}
		}
	}
}
