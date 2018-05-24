using System;
using System.Numerics;

namespace Poker
{
	public class TextMeshBuilder
	{
		private readonly SpriteFont m_font;
		private readonly Vector2 m_oneOverFontTexSize;
		
		private uint m_vertexPos;
		private uint m_indexPos;
		
		private TextVertex[] m_vertices;
		private uint[] m_indices;
		
		public TextMeshBuilder(SpriteFont font)
		{
			m_font = font;
			m_oneOverFontTexSize = new Vector2(1.0f / m_font.Texture.Width, 1.0f / m_font.Texture.Height);
		}
		
		private static readonly uint[] QUAD_INDICES = { 2, 1, 0, 3, 1, 2 };
		
		public void AddText(string text, float height, Vector3 centerPos, Vector3 up, Vector3 direction, uint textId = 0)
		{
			const int VERTICES_PER_CHAR = 4;
			const int INDICES_PER_CHAR = 6;
			
			int maxVertexPos = (int)m_vertexPos + text.Length * VERTICES_PER_CHAR;
			if (m_vertices == null)
				m_vertices = new TextVertex[maxVertexPos];
			else if (m_vertices.Length < maxVertexPos)
				Array.Resize(ref m_vertices, maxVertexPos);
			
			int maxIndexPos = (int)m_indexPos + text.Length * INDICES_PER_CHAR;
			if (m_indices == null)
				m_indices = new uint[maxIndexPos];
			else if (m_indices.Length < maxIndexPos)
				Array.Resize(ref m_indices, maxIndexPos);
			
			float xOffset = 0;
			
			uint initialVertexPos = m_vertexPos;
			
			float halfHeight = height / 2.0f;
			
			float scale = height / m_font.LineHeight;
			
			Vector3 GetWorldPos(float localX, float localY)
			{
				return centerPos + up * (localY - halfHeight) + direction * localX;
			}
			
			foreach (char c in text)
			{
				if (!m_font.GetCharacter(c, out SpriteFont.Character fontChar))
					continue;
				
				foreach (uint index in QUAD_INDICES)
					m_indices[m_indexPos++] = m_vertexPos + index;
				
				float kerning = 0; //TODO: Implement kerning
				
				Vector2 min = new Vector2(xOffset + (fontChar.XOffset + kerning) * scale, fontChar.YOffset * scale);
				Vector2 max = min + new Vector2(fontChar.Width, fontChar.Height) * scale;
				
				Vector2 minTex = new Vector2(fontChar.TextureX, fontChar.TextureY) * m_oneOverFontTexSize;
				Vector2 maxTex = minTex + new Vector2(fontChar.Width, fontChar.Height) * m_oneOverFontTexSize;
				
				m_vertices[m_vertexPos++] = new TextVertex(GetWorldPos(min.X, min.Y), new Vector2(minTex.X, minTex.Y), textId);
				m_vertices[m_vertexPos++] = new TextVertex(GetWorldPos(min.X, max.Y), new Vector2(minTex.X, maxTex.Y), textId);
				m_vertices[m_vertexPos++] = new TextVertex(GetWorldPos(max.X, min.Y), new Vector2(maxTex.X, minTex.Y), textId);
				m_vertices[m_vertexPos++] = new TextVertex(GetWorldPos(max.X, max.Y), new Vector2(maxTex.X, maxTex.Y), textId);
				
				xOffset += fontChar.XAdvance * scale;
			}
			
			Vector3 offset = (xOffset / 2.0f) * direction;
			for (uint i = initialVertexPos; i < m_vertexPos; i++)
			{
				m_vertices[i].Position -= offset;
			}
		}
		
		public Mesh CreateMesh()
		{
			if (m_vertexPos == 0 || m_indexPos == 0)
				return null;
			return new Mesh(m_vertices, m_indices, m_vertexPos, m_indexPos);
		}
	}
}
