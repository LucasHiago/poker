using System.Runtime.InteropServices;

namespace Poker
{
	public unsafe class KeyboardState
	{
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct NativeKeyboardState
		{
			public uint NumKeys;
			public byte* KeyStates;
		}
		
		[DllImport("Native")]
		private static extern void GetKeyboardState(out NativeKeyboardState keyboardState);
		
		private readonly bool[] m_keyStates;
		
		private KeyboardState(bool[] keyStates)
		{
			m_keyStates = keyStates;
		}
		
		public static KeyboardState GetCurrent()
		{
			GetKeyboardState(out NativeKeyboardState keyboardState);
			
			bool[] keyStates = new bool[keyboardState.NumKeys];
			for (uint i = 0; i < keyboardState.NumKeys; i++)
				keyStates[i] = keyboardState.KeyStates[i] != 0;
			return new KeyboardState(keyStates);
		}
		
		public bool IsKeyDown(Keys key)
		{
			if ((int)key >= m_keyStates.Length || (int)key < 0)
				return false;
			return m_keyStates[(int)key];
		}
	}
}
