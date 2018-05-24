using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Poker
{
	[Flags]
	public enum FFState : byte
	{
		Multisample     = 1,
		DepthTest       = 2,
		DepthWrite      = 4,
		AlphaBlend      = 8,
		FramebufferSrgb = 16,
		ScissorTest     = 32
	}
	
	public static class Graphics
	{
		[StructLayout(LayoutKind.Sequential, Pack=1)]
		private struct ClearColorValue
		{
			public readonly float R;
			public readonly float G;
			public readonly float B;
			public readonly float A;
			
			public ClearColorValue(float r, float g, float b, float a)
			{
				R = r;
				G = g;
				B = b;
				A = a;
			}
		}
		
		[DllImport("Native")]
		private static extern void FB_ClearColor(ClearColorValue color);
		
		public static void ClearColor(float r, float g, float b, float a)
		{
			FB_ClearColor(new ClearColorValue(r, g, b, a));
		}
		
		[DllImport("Native", EntryPoint="FB_ClearDepth")]
		public static extern void ClearDepth();
		[DllImport("Native", EntryPoint="FB_DiscardColor")]
		public static extern void DiscardColor();
		
		[DllImport("Native", EntryPoint="FB_BindDefault")]
		public static extern void BindDefaultFramebuffer();
		
		[DllImport("Native")]
		public static extern void SetFixedFunctionState(FFState state);
		
		[DllImport("Native")]
		public static extern void SetScissorRectangle(int x, int y, int width, int height);
		
		public static Matrix4x4 CreateProjectionMatrix(float fov, float aspectRatio, float zNear, float zFar)
		{
			float halfFov = fov / 2.0f;
			float h = MathF.Cos(halfFov) / MathF.Sin(halfFov);
			float w = h / aspectRatio;
			
			Matrix4x4 matrix = new Matrix4x4();
			matrix.M11 = w;
			matrix.M22 = h;
			matrix.M34 = -1;
			matrix.M33 = -(zFar + zNear) / (zFar - zNear);
			matrix.M43 = -(2.0f * zFar * zNear) / (zFar - zNear);
			
			return matrix;
		}
	}
}
