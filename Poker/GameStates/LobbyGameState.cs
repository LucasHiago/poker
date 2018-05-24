using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using Poker.Net;

namespace Poker
{
	public class LobbyGameState : GameState, IDisposable
	{
		private class ClientEntry
		{
			public IClient Client;
			public float Y;
			public float Alpha;
		}
		
		private Connection m_connection;
		
		private readonly List<ClientEntry> m_clients = new List<ClientEntry>();
		
		private float m_playersListBeginX;
		private float m_playersListBeginY;
		
		private const float TITLE_HEIGHT = UI.SCALE * 100;
		
		private readonly Texture2D m_playerBackTexture;
		private readonly Texture2D m_titleTexture;
		private readonly Texture2D m_hostIconTexture;
		
		private RectangleF m_titleTextureRect;
		
		private readonly MenuButton m_backButton = new MenuButton("BACK");
		private readonly MenuButton m_startButton = new MenuButton("START");
		
		private readonly float m_playerEntryWidth;
		private readonly float m_playerEntryHeight;
		private readonly float m_playerPixelHeight;
		
		private float m_startAnimationProgress;
		
		private MouseState m_prevMS;
		
		public LobbyGameState()
		{
			m_playerBackTexture = Texture2D.Load("UI/LobbyPlayerBackground.png");
			m_titleTexture = Texture2D.Load("UI/LobbyTitle.png");
			m_hostIconTexture = Texture2D.Load("UI/HostIcon.png");
			
			m_playerEntryWidth = m_playerBackTexture.Width * UI.SCALE;
			m_playerEntryHeight = m_playerBackTexture.Height * UI.SCALE;
			m_playerPixelHeight = m_playerEntryHeight + 20 * UI.SCALE;
		}
		
		public void SetConnection(Connection connection)
		{
			m_connection = connection;
		}
		
		public override void Activated()
		{
			m_startAnimationProgress = 0;
			m_clients.Clear();
		}
		
		public override void Update(float dt)
		{
			if (m_connection == null)
				return;
			
			float y = 0;
			for (int i = 0; i < m_clients.Count; i++)
			{
				if (!m_clients[i].Client.IsConnected)
				{
					UI.AnimateDec(ref m_clients[i].Alpha, dt, 0.5f);
					if (m_clients[i].Alpha < 1E-6f)
					{
						m_clients.RemoveAt(i);
						i--;
					}
				}
				else
				{
					UI.AnimateInc(ref m_clients[i].Alpha, dt, 0.5f);
				}
				
				if (m_clients[i].Y > y)
				{
					float newY = m_clients[i].Y - dt * UI.ANIMATION_SPEED;
					m_clients[i].Y = Math.Max(newY, y);
				}
				
				y += 1;
			}
			
			foreach (var client in m_connection.Clients)
			{
				if (!m_clients.Exists(clientEnt => clientEnt.Client == client))
				{
					m_clients.Add(new ClientEntry { Client = client, Alpha = 0, Y = y++ });
				}
			}
			
			if (m_connection.InGame)
			{
				UI.AnimateInc(ref m_startAnimationProgress, dt);
				
				if (m_startAnimationProgress > 1.0f - 1E-6f)
				{
					MainGameState mainGameState = GameStateManager.SetGameState<MainGameState>();
					mainGameState.SetConnection(m_connection);
				}
				
				return;
			}
			
			MouseState ms = MouseState.GetCurrent();
			
			if (m_backButton.Update(dt, ms, m_prevMS))
			{
				m_connection.Disconnect();
			}
			
			Net.Server.Server server = m_connection as Net.Server.Server;
			
			bool canStart = m_clients.Count > 1 && server != null;
			if (m_startButton.Update(dt, ms, m_prevMS, canStart))
			{
				server.StartGame();
				m_startAnimationProgress = 0;
			}
			
			if (m_connection.Closed)
			{
				m_connection = null;
				m_clients.Clear();
				GameStateManager.SetGameState<MainMenuGameState>();
			}
			
			m_prevMS = ms;
		}
		
		public override void Draw(DrawArgs drawArgs)
		{
			Graphics.ClearColor(1, 1, 1, 1);
			
			if (m_connection == null)
				return;
			
			drawArgs.SpriteBatch.Begin();
			
			drawArgs.SpriteBatch.Draw(m_titleTexture, m_titleTextureRect, new Color(255, 255, 255));
			
			foreach (ClientEntry client in m_clients)
			{
				float y = m_playersListBeginY + client.Y * m_playerPixelHeight;
				RectangleF RectangleF = new RectangleF(m_playersListBeginX, (int)y, m_playerEntryWidth, m_playerEntryHeight);
				
				//Draws the player background texture
				Color backColor = UI.DEFAULT_BUTTON_COLOR;
				if (client.Client.ClientId == m_connection.SelfClientId)
				{
					const float SELF_BRIGHTNESS_SCALE = 1.25f;
					backColor.R = (byte)(backColor.R * SELF_BRIGHTNESS_SCALE);
					backColor.G = (byte)(backColor.G * SELF_BRIGHTNESS_SCALE);
					backColor.B = (byte)(backColor.B * SELF_BRIGHTNESS_SCALE);
				}
				backColor.A = (byte)(backColor.A * client.Alpha);
				drawArgs.SpriteBatch.Draw(m_playerBackTexture, RectangleF, backColor);
				
				string name = client.Client.Name;
				Vector2 nameSize = Assets.RegularFont.MeasureString(name);
				float textHeight = UI.TEXT_HEIGHT_PERCENTAGE * RectangleF.Height;
				
				Vector2 textPos = new Vector2(
					RectangleF.Left + RectangleF.Height * 0.5f,
					y + (RectangleF.Height - textHeight) / 2
				);
				
				//Draws the player name
				Color textColor = new Color(1, 1, 1, client.Alpha);
				drawArgs.SpriteBatch.DrawString(Assets.RegularFont, name, textPos, textColor, textHeight / nameSize.Y);
				
				//Draws the host icon if this is the host
				if (client.Client.ClientId == 0)
				{
					float hostIconSize = RectangleF.Height * UI.TEXT_HEIGHT_PERCENTAGE;
					float hostIconOffY = (RectangleF.Height - hostIconSize) / 2.0f;
					float hostIconOffX = hostIconOffY * 1.0f;
					RectangleF hostIconRect = new RectangleF(RectangleF.Right - hostIconOffX - hostIconSize,
						RectangleF.Top + hostIconOffY, hostIconSize, hostIconSize);
					
					drawArgs.SpriteBatch.Draw(m_hostIconTexture, hostIconRect, new Color(255, 255, 255));
				}
			}
			
			m_backButton.Draw(drawArgs.SpriteBatch);
			m_startButton.Draw(drawArgs.SpriteBatch);
			
			if (m_connection.InGame)
			{
				
			}
			
			Graphics.SetFixedFunctionState(FFState.AlphaBlend);
			drawArgs.SpriteBatch.End();
		}
		
		public override void OnResize(int newWidth, int newHeight)
		{
			float titleY = newHeight * 0.1f;
			
			float titleWidth = TITLE_HEIGHT * m_titleTexture.Width / m_titleTexture.Height;
			m_titleTextureRect = new RectangleF((newWidth - titleWidth) / 2.0f, titleY, titleWidth, TITLE_HEIGHT);
			
			m_playersListBeginX = (newWidth - m_playerEntryWidth) / 2.0f;
			m_playersListBeginY = m_titleTextureRect.Bottom + 50 * UI.SCALE;
			
			float buttonWidth = Assets.SmallButtonTexture.Width * UI.SCALE * 0.8f;
			float buttonHeight = Assets.SmallButtonTexture.Height * UI.SCALE * 0.8f;
			float buttonY = newHeight - (buttonHeight + 50 * UI.SCALE);
			m_backButton.Rectangle = new RectangleF(newWidth / 2.0f - buttonWidth, buttonY, buttonWidth, buttonHeight);
			m_startButton.Rectangle = new RectangleF(newWidth / 2.0f, buttonY, (int)buttonWidth, (int)buttonHeight);
		}
		
		public void Dispose()
		{
			m_playerBackTexture.Dispose();
			m_hostIconTexture.Dispose();
			m_titleTexture.Dispose();
		}
	}
}
