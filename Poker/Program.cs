using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Poker.Net;
using Poker.Net.Client;
using Poker.Net.Server;

namespace Poker
{
	public static unsafe class Program
	{
		private delegate void InitCallback();
		private delegate void CloseCallback();
		private delegate void FrameCallback(float dt);
		private delegate void ResizeCallback(int width, int height);
		private delegate void TextInputCallback(byte* utf8String, int byteLength);
		private delegate void KeyPressCallback(Keys key);
		
		[DllImport("Native")]
		private static extern bool RunGame(InitCallback initCallback, CloseCallback closeCallback,
		                                   FrameCallback frameCallback, ResizeCallback resizeCallback,
		                                   TextInputCallback textInputCallback, KeyPressCallback keyPressCallback);
		
		[DllImport("Native")]
		public static extern bool ExitGame();
		
		public static string EXEDirectory { get; private set; }
		
		private static SpriteBatch s_spriteBatch;
		
		private static bool s_host;
		private static bool s_join;
		private static string s_nickname;
		
		public static void Main(string[] args)
		{
			if (args.Length == 2)
			{
				s_host = args[0] == "host";
				s_join = args[0] == "join";
				s_nickname = args[1];
			}
			
			EXEDirectory = AppDomain.CurrentDomain.BaseDirectory;
			RunGame(Initialize, Close, RunFrame, Resized, TextInput, KeyPress);
		}
		
		private static void Initialize()
		{
			Shader.OpenArchive();
			
			s_spriteBatch = new SpriteBatch();
			
			Assets.Load();
			SkyRenderer.Load();
			
			MenuBackground.Instance = new MenuBackground();
			CardRenderer.Instance = new CardRenderer();
			ChipsRenderer.Instance = new ChipsRenderer();
			BlurEffect.Instance = new BlurEffect();
			BoardModel.Instance = new BoardModel();
			
			MainMenuGameState mainMenuGameState = new MainMenuGameState();
			mainMenuGameState.JoinGame += (ip, nickname) =>
			{
				ConnectingGameState connectingGameState = GameStateManager.SetGameState<ConnectingGameState>();
				connectingGameState.Connection = new ServerConnection(ip, nickname);
			};
			mainMenuGameState.HostGame += (nickname) =>
			{
				LobbyGameState lobbyGameState = GameStateManager.SetGameState<LobbyGameState>();
				lobbyGameState.SetConnection(new Server(nickname));
			};
			
			GameStateManager.InitializeGameState(mainMenuGameState);
			GameStateManager.InitializeGameState(new MainGameState());
			GameStateManager.InitializeGameState(new LobbyGameState());
			GameStateManager.InitializeGameState(new ConnectingGameState());
			
			Connection connection = null;
			
			if (s_host)
			{
				connection = new Server(s_nickname);
			}
			else if (s_join)
			{
				connection = new ServerConnection(IPAddress.Loopback, s_nickname);
			}
			
			if (connection != null)
			{
				while (connection.Connecting)
					Thread.Sleep(100);
				LobbyGameState lobbyGameState = GameStateManager.SetGameState<LobbyGameState>();
				lobbyGameState.SetConnection(connection);
			}
			else
			{
				GameStateManager.SetGameState<MainMenuGameState>();
			}
		}
		
		private static void Close()
		{
			Utils.DisposeAndNull(ref s_spriteBatch);
			GameStateManager.Dispose();
			SkyRenderer.Dispose();
			Assets.Unload();
			CardRenderer.Instance.Dispose();
			ChipsRenderer.Instance.Dispose();
			BlurEffect.Instance.Dispose();
			BoardModel.Instance.Dispose();
			MenuBackground.Instance.Dispose();
		}
		
		private static void Resized(int width, int height)
		{
			MenuBackground.Instance.OnResize(width, height);
			BlurEffect.Instance.SetResolution((uint)width, (uint)height);
			s_spriteBatch.SetDisplaySize(width, height);
			GameStateManager.OnResize(width, height);
		}
		
		private static void TextInput(byte* utf8String, int byteLength)
		{
			string text = Encoding.UTF8.GetString(utf8String, byteLength);
			GameStateManager.CurrentGameState.OnTextInput(text);
		}
		
		private static void KeyPress(Keys key)
		{
			GameStateManager.CurrentGameState.OnKeyPress(key);
		}
		
		private static void RunFrame(float dt)
		{
			GameState drawGS = GameStateManager.CurrentGameState;
			GameStateManager.Update(dt);
			
			DrawArgs drawArgs = new DrawArgs
			{
				DeltaTime = dt,
				SpriteBatch = s_spriteBatch
			};
			drawGS.Draw(drawArgs);
		}
	}
}
