#if !NETCOREAPP2_0
using System;

namespace Poker
{
	public static class MathF
	{
		public const float PI = (float)Math.PI;
		
		public static float Sin(float x)
		{
			return (float)Math.Sin(x);
		}
		
		public static float Cos(float x)
		{
			return (float)Math.Cos(x);
		}
		
		public static float Floor(float x)
		{
			return (float)Math.Floor(x);
		}
	}
}
#endif
