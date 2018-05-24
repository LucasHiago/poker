using System;
using System.Drawing;
using System.Numerics;

namespace Poker
{
	public static class Utils
	{
		public static void DisposeAndNull<T>(ref T t) where T : class, IDisposable
		{
			t?.Dispose();
			t = null;
		}
		
		public static void Shuffle<T>(T[] array, Random random)
		{
			int n = array.Length;
			while (n > 1)
			{
				int k = random.Next(n--);
				T tmp = array[n];
				array[n] = array[k];
				array[k] = tmp;
			}
		}
		
		public static float ToRadians(float deg)
		{
			return deg * MathF.PI / 180.0f;
		}
		
		public static float Lerp(float a, float b, float x)
		{
			return a + (b - a) * x;
		}
		
		public static float NMod(float a, float b)
		{
			return a - b * MathF.Floor(a / b);
		}
		
		public static float SmoothStep(float t)
		{
			return t * t * (3.0f - 2.0f * t);
		}
		
		public static float Clamp(float x, float min, float max)
		{
			if (x < min)
				return min;
			if (x > max)
				return max;
			return x;
		}
		
		public static Vector2 ToVector2(this Point point)
		{
			return new Vector2(point.X, point.Y);
		}
		
		public static Vector2 ToVector2(this PointF point)
		{
			return new Vector2(point.X, point.Y);
		}
		
		public static Point Center(this Rectangle rectangle)
		{
			return new Point(rectangle.X + rectangle.Width / 2, rectangle.Y + rectangle.Height / 2);
		}
		
		public static Vector2 Center(this RectangleF rectangle)
		{
			return new Vector2(rectangle.X + rectangle.Width / 2.0f, rectangle.Y + rectangle.Height / 2.0f);
		}
		
		public static bool Contains(this RectangleF rectangle, Vector2 position)
		{
			return rectangle.Contains(position.X, position.Y);
		}
	}
}
