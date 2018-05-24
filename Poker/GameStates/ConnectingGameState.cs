using System.Net.Sockets;
using System.Numerics;
using Poker.Net.Client;

namespace Poker
{
	public class ConnectingGameState : GameState
	{
		public ServerConnection Connection;
		
		private Vector2 m_connectingTextPos;
		
		public override void Update(float dt)
		{
			if (Connection.CState == ServerConnection.State.Connected)
			{
				LobbyGameState lobbyGameState = GameStateManager.SetGameState<LobbyGameState>();
				lobbyGameState.SetConnection(Connection);
				Connection = null;
			}
			else if (Connection.CState != ServerConnection.State.Connecting)
			{
				MainMenuGameState mainMenuGS = GameStateManager.SetGameState<MainMenuGameState>();
				if (Connection.CState == ServerConnection.State.Rejected)
				{
					if (Connection.ConnectionResponseStatus == Net.ConnectionResponseStatus.NicknameTaken)
						mainMenuGS.SetError("That nickname is taken");
					else if (Connection.ConnectionResponseStatus == Net.ConnectionResponseStatus.ServerFull)
						mainMenuGS.SetError("The server is full");
				}
				else if (Connection.CState == ServerConnection.State.SocketError)
				{
					switch (Connection.SocketError)
					{
						case SocketError.HostUnreachable:
						case SocketError.HostNotFound:
						case SocketError.HostDown:
							mainMenuGS.SetError("Unknown host");
							break;
						case SocketError.ConnectionRefused:
							mainMenuGS.SetError("Connection refused");
							break;
						case SocketError.TimedOut:
							mainMenuGS.SetError("Connection timed out");
							break;
						default:
							mainMenuGS.SetError("Socket error: " + Connection.SocketError.ToString());
							break;
					}
				}
			}
		}
		
		private const string CONNECTING_TEXT = "Connecting...";
		private const float CONNECTING_TEXT_SCALE = 0.5f;
		
		public override void OnResize(int newWidth, int newHeight)
		{
			Vector2 textSize = Assets.RegularFont.MeasureString(CONNECTING_TEXT) * CONNECTING_TEXT_SCALE;
			m_connectingTextPos = (new Vector2(newWidth, newHeight) - textSize) / 2.0f;
			
			base.OnResize(newWidth, newHeight);
		}
		
		public override void Draw(DrawArgs drawArgs)
		{
			Graphics.ClearColor(1, 1, 1, 1);
			
			drawArgs.SpriteBatch.Begin();
			
			drawArgs.SpriteBatch.DrawString(Assets.RegularFont, CONNECTING_TEXT, m_connectingTextPos,
			                                new Color(0, 0, 0, 170), CONNECTING_TEXT_SCALE);
			
			Graphics.SetFixedFunctionState(FFState.AlphaBlend);
			drawArgs.SpriteBatch.End();
		}
	}
}
