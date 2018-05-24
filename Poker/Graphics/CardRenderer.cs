using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

namespace Poker
{
	public class CardRenderer : IDisposable
	{
		public static CardRenderer Instance;
		
		private struct CardEntry : IComparable<CardEntry>
		{
			public RectangleF SrcRectangle;
			public Vector3 Position;
			public Quaternion Rotation;
			public bool CastShadows;
			
			public int CompareTo(CardEntry other)
			{
				return Position.Y.CompareTo(other.Position.Y);
			}
		}
		
		private readonly List<CardEntry> m_cards = new List<CardEntry>();
		
		private readonly Shader m_shader;
		private readonly int m_worldTransformLocation;
		private readonly int m_texSourceRegionLocation;
		
		private readonly Shader m_shadowShader;
		private readonly int m_worldTransformLocationShadow;
		
		private readonly Mesh m_mesh;
		
		private readonly float m_xScale;
		
		private const float SIZE = 0.2f;
		
		public CardRenderer()
		{
			m_shader = new Shader();
			m_shader.AttachStage(Shader.StageType.Vertex, "Card.vs.glsl");
			m_shader.AttachStage(Shader.StageType.Fragment, "Card.fs.glsl");
			m_shader.Link();
			
			m_shadowShader = new Shader();
			m_shadowShader.AttachStage(Shader.StageType.Vertex, "CardShadow.vs.glsl");
			m_shadowShader.AttachStage(Shader.StageType.Fragment, "CardShadow.fs.glsl");
			m_shadowShader.Link();
			
			m_worldTransformLocation = m_shader.GetUniformLocation("worldTransform");
			m_texSourceRegionLocation = m_shader.GetUniformLocation("texSourceRegion");
			
			m_worldTransformLocationShadow = m_shadowShader.GetUniformLocation("worldTransform");
			
			m_xScale = (float)Assets.CardsTexture.CardWidth / Assets.CardsTexture.CardHeight;
			
			Vector2[] vertices = { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1,  1), new Vector2(1,  1) };
			uint[] indices = { 0, 1, 2, 2, 1, 3 };
			m_mesh = new Mesh(vertices, indices);
		}
		
		public void Reset()
		{
			m_cards.Clear();
		}
		
		public void Add(Vector3 position, Quaternion rotation, Card card, bool castShadows)
		{
			CardEntry entry = new CardEntry
			{
				SrcRectangle = Assets.CardsTexture.GetSourceRectangle(card),
				Position = position,
				Rotation = rotation,
				CastShadows = castShadows
			};
			
			m_cards.Add(entry);
		}
		
		public void AddUnknown(Vector3 position, Quaternion rotation, bool castShadows)
		{
			Add(position, rotation, new Card(Suits.Spades, Card.RANK_ACE), castShadows);
		}
		
		private Matrix4x4 GetCardTransform(int cardIndex)
		{
			return Matrix4x4.CreateScale(m_xScale * SIZE, SIZE, SIZE) *
			       Matrix4x4.CreateFromQuaternion(m_cards[cardIndex].Rotation) *
			       Matrix4x4.CreateTranslation(m_cards[cardIndex].Position);
		}
		
		public void Draw()
		{
			Graphics.SetFixedFunctionState(FFState.AlphaBlend | FFState.DepthTest | FFState.Multisample);
			
			m_shader.Bind();
			
			Assets.CardsTexture.Bind(0);
			Assets.CardBackTexture.Bind(1);
			
			float xSrcScale = 1.0f / Assets.CardsTexture.Texture.Width;
			float ySrcScale = 1.0f / Assets.CardsTexture.Texture.Height;
			
			m_cards.Sort();
			
			for (int i = 0; i < m_cards.Count; i++)
			{
				Matrix4x4 worldTransform = GetCardTransform(i);
				m_shader.SetUniform(m_worldTransformLocation, ref worldTransform);
				
				float minSrcX = m_cards[i].SrcRectangle.Left * xSrcScale;
				float minSrcY = m_cards[i].SrcRectangle.Top * ySrcScale;
				float maxSrcX = m_cards[i].SrcRectangle.Right * xSrcScale;
				float maxSrcY = m_cards[i].SrcRectangle.Bottom * ySrcScale;
				m_shader.SetUniform(m_texSourceRegionLocation, new Vector4(minSrcX, minSrcY, maxSrcX, maxSrcY));
				
				m_mesh.Draw();
			}
		}
		
		public void DrawShadow()
		{
			m_shadowShader.Bind();
			
			Assets.CardBackTexture.Bind(0);
			
			for (int i = 0; i < m_cards.Count; i++)
			{
				if (!m_cards[i].CastShadows)
					continue;
				
				Matrix4x4 worldTransform = GetCardTransform(i);
				m_shadowShader.SetUniform(m_worldTransformLocationShadow, ref worldTransform);
				
				m_mesh.Draw();
			}
		}
		
		public void Dispose()
		{
			m_shader.Dispose();
			m_shadowShader.Dispose();
			m_mesh.Dispose();
		}
	}
}
