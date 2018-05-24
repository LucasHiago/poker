using System.Drawing;
using System.Runtime.InteropServices;

namespace Poker
{
	public enum ButtonState : byte
	{
		Released = 0,
		Pressed = 1
	}
	
	public class MouseState
	{
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct NativeMouseState
		{
			public int CursorX;
			public int CursorY;
			public int ScrollX;
			public int ScrollY;
			public ButtonState LeftButton;
			public ButtonState RightButton;
		}
		
		[DllImport("Native")]
		private static extern NativeMouseState GetMouseState();
		
		private NativeMouseState m_nativeMouseState;
		
		public Point Position => new Point(m_nativeMouseState.CursorX, m_nativeMouseState.CursorY);
		
		public ButtonState LeftButton => m_nativeMouseState.LeftButton;
		public ButtonState RightButton => m_nativeMouseState.RightButton;
		public int ScrollX => m_nativeMouseState.ScrollX;
		public int ScrollY => m_nativeMouseState.ScrollY;
		
		private MouseState(NativeMouseState mouseState)
		{
			m_nativeMouseState = mouseState;
		}
		
		public static MouseState GetCurrent()
		{
			return new MouseState(GetMouseState());
		}
	}
}
