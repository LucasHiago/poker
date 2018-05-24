using System;
using System.Numerics;

namespace Poker
{
	public class MenuBackground : IDisposable
	{
		public static MenuBackground Instance;
		
		private const float FOV = MathF.PI * 0.45f;
		private const float Z_NEAR = 0.1f;
		private const float Z_FAR = 1000.0f;
		private const float CAMERA_PITCH = 0.3f * MathF.PI;
		private const float CAMERA_DIST = 3.5f;
		
		private const int NUM_PLAYERS = 4;
		
		private Matrix4x4 m_projectionMatrix;
		
		private static readonly Vector3 LIGHT_DIRECTION = Vector3.Normalize(new Vector3(0.5f, -0.75f, 1.0f));
		
		private readonly ShadowMapper m_shadowMapper = new ShadowMapper(LIGHT_DIRECTION);
		
		private readonly ViewProjUniformBuffer m_viewProjUniformBuffer = new ViewProjUniformBuffer();
		
		private float m_cameraRotation;
		private float m_spinSpeed = 0;
		
		private readonly int m_visisbleCommunityCards;
		private readonly Card[] m_communityCards = new Card[5];
		
		public MenuBackground()
		{
			Random random = new Random();
			m_cameraRotation = (float)(random.NextDouble() * 2 * Math.PI);
			
			m_visisbleCommunityCards = random.Next(3, 6);
			
			//Generates a random deck
			byte[] deck = new byte[52];
			for (byte i = 0; i < 52; i++)
				deck[i] = i;
			Utils.Shuffle(deck, random);
			
			for (int i = 0; i < 5; i++)
				m_communityCards[i] = new Card(deck[i]);
		}
		
		public void OnResize(int newWidth, int newHeight)
		{
			float aspectRatio = (float)newWidth / newHeight;
			m_projectionMatrix = Graphics.CreateProjectionMatrix(FOV, aspectRatio, Z_NEAR, Z_FAR);
		}
		
		public void Update(float dt, bool spinFast)
		{
			if (spinFast)
				UI.AnimateInc(ref m_spinSpeed, dt, 0.5f);
			else
				UI.AnimateDec(ref m_spinSpeed, dt, 0.5f);
			
			float revPerSecond = 0.05f + m_spinSpeed * 0.35f;
			m_cameraRotation += dt * revPerSecond * 2 * MathF.PI;
		}
		
		private void PrepareCardsRenderer(CardRenderer cardRenderer)
		{
			Quaternion cardSourceRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2);
			
			cardRenderer.Reset();
			
			//Adds community cards
			for (int i = 0; i < 5; i++)
			{
				//Distance between the center of two community cards
				const float STRIDE = 0.4f;
				
				Vector3 position = new Vector3(0, 0.105f, STRIDE * 2 - i * STRIDE);
				
				Quaternion cardRotation = cardSourceRotation;
				if (m_visisbleCommunityCards > i)
				{
					cardRotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI);
				}
				
				cardRenderer.Add(position, cardRotation, m_communityCards[i], false);
			}
		}
		
		public void Draw()
		{
			var cardRenderer = CardRenderer.Instance;
			
			Quaternion rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, CAMERA_PITCH) *
			                      Quaternion.CreateFromAxisAngle(Vector3.UnitY, m_cameraRotation);
			
			Vector3 cameraPos = Vector3.Transform(new Vector3(0, 0, CAMERA_DIST), Quaternion.Inverse(rotation));
			
			Matrix4x4 viewMatrix = Matrix4x4.CreateFromQuaternion(rotation) *
			                       Matrix4x4.CreateTranslation(0, 0, -CAMERA_DIST);
			
			PrepareCardsRenderer(CardRenderer.Instance);
			
			Graphics.SetFixedFunctionState(FFState.DepthTest | FFState.DepthWrite);
			m_shadowMapper.RenderShadows(() =>
			{
				CardRenderer.Instance.DrawShadow();
				BoardModel.Instance.DrawShadow();
			});
			
			BlurEffect.Instance.BindInputFramebuffer();
			
			Graphics.ClearColor(1, 1, 1, 1);
			Graphics.ClearDepth();
			Graphics.SetFixedFunctionState(FFState.Multisample | FFState.DepthWrite | FFState.DepthTest);
			
			Matrix4x4 viewProj = viewMatrix * m_projectionMatrix;
			m_viewProjUniformBuffer.Update(ref viewProj, cameraPos);
			m_viewProjUniformBuffer.Bind(0);
			
			m_shadowMapper.Bind();
			
			// ** Opaque geometry **
			
			BoardModel.Instance.DrawNormal();
			
			// ** Alpha blended geometry **
			
			cardRenderer.Draw();
			
			BlurEffect.Instance.RenderBlur(1);
		}

		public void Dispose()
		{
			m_viewProjUniformBuffer.Dispose();
			m_shadowMapper.Dispose();
		}
	}
}