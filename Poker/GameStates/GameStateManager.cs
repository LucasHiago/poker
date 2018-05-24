using System;
using System.Collections.Generic;

namespace Poker
{
	public static class GameStateManager
	{
		struct GameStateEntry
		{
			public GameState GameState;
			public int DisplayWidth;
			public int DisplayHeight;
		}
		
		private static readonly List<GameStateEntry> s_gameStates = new List<GameStateEntry>();
		private static int s_currentGameStateIndex = -1;
		
		private static int m_displayWidth = -1;
		private static int m_displayHeight = -1;
		
		public static GameState CurrentGameState => s_gameStates[s_currentGameStateIndex].GameState;
		
		public static void InitializeGameState(GameState gameState)
		{
			s_gameStates.Add(new GameStateEntry { GameState = gameState, DisplayWidth = -1, DisplayHeight = -1 });
		}
		
		public static T SetGameState<T>() where T : GameState
		{
			s_currentGameStateIndex = s_gameStates.FindIndex(gs => gs.GameState.GetType() == typeof(T));
			if (s_currentGameStateIndex == -1)
				throw new Exception("Game state not found '" + typeof(T).Name + "'.");
			CurrentGameState.Activated();
			return (T)CurrentGameState;
		}
		
		public static void OnResize(int newWidth, int newHeight)
		{
			m_displayWidth = newWidth;
			m_displayHeight = newHeight;
		}
		
		public static void Update(float dt)
		{
			GameStateEntry gameStateEntry = s_gameStates[s_currentGameStateIndex];
			
			if (gameStateEntry.DisplayWidth != m_displayWidth ||
			    gameStateEntry.DisplayHeight != m_displayHeight)
			{
				gameStateEntry.GameState.OnResize(m_displayWidth, m_displayHeight);
				gameStateEntry.DisplayWidth = m_displayWidth;
				gameStateEntry.DisplayHeight = m_displayHeight;
				s_gameStates[s_currentGameStateIndex] = gameStateEntry;
			}
			
			gameStateEntry.GameState.Update(dt);
		}
		
		public static void Dispose()
		{
			for (int i = 0; i < s_gameStates.Count; i++)
			{
				if (s_gameStates[i].GameState is IDisposable disposable)
					disposable.Dispose();
			}
		}
	}
}
