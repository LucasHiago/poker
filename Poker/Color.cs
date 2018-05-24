using System;
using System.Diagnostics.Contracts;

namespace Poker
{
	public struct Color
	{
		public static Color White => new Color(255, 255, 255);
		public static Color Black => new Color(0, 0, 0);
		
		public byte R;
		public byte G;
		public byte B;
		public byte A;
		
		public Color(float r, float g, float b, float a = 1)
		{
			R = (byte)(r * 255.0f);
			G = (byte)(g * 255.0f);
			B = (byte)(b * 255.0f);
			A = (byte)(a * 255.0f);
		}
		
		public Color(int r, int g, int b, int a = 255)
			: this((byte)r, (byte)g, (byte)b, (byte)a) { }
		
		public Color(byte r, byte g, byte b, byte a = 255)
		{
			R = r;
			G = g;
			B = b;
			A = a;
		}
		
		[Pure]
		public Color ScaleAlpha(float amount)
		{
			return new Color(R, G, B, (byte)Utils.Clamp(A * amount, 0, 255));
		}
		
		public static Color Lerp(Color a, Color b, float x)
		{
			return new Color((byte)Utils.Lerp(a.R, b.R, x), (byte)Utils.Lerp(a.G, b.G, x),
			                 (byte)Utils.Lerp(a.B, b.B, x), (byte)Utils.Lerp(a.A, b.A, x));
		}
	}
}
