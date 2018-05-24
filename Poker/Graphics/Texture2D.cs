using System;
using System.Runtime.InteropServices;

namespace Poker
{
	public class Texture2D : IDisposable
	{
		public enum Type : uint
		{
			Linear8 = 0,
			Linear32 = 1,
			sRGB32 = 2
		}
		
		public enum Swizzle : byte
		{
			Red = 0,
			Green = 1,
			Blue = 2,
			Alpha = 3,
			One = 4,
			Zero = 5
		}
		
		[DllImport("Native")]
		private static extern IntPtr Tex2D_Load(string path, Type type);
		[DllImport("Native")]
		private static extern void Tex2D_Destroy(IntPtr texture);
		[DllImport("Native")]
		private static extern void Tex2D_SetSwizzle(IntPtr texture, Swizzle r, Swizzle g, Swizzle b, Swizzle a);
		[DllImport("Native")]
		private static extern void Tex2D_SetRepeat(IntPtr texture, bool repeat);
		[DllImport("Native")]
		private static extern void Tex2D_SetLodBias(IntPtr texture, float bias);
		[DllImport("Native")]
		private static extern uint Tex2D_GetWidth(IntPtr texture);
		[DllImport("Native")]
		private static extern uint Tex2D_GetHeight(IntPtr texture);
		[DllImport("Native")]
		private static extern void Tex2D_Bind(IntPtr texture, int unit);
		
		public readonly IntPtr Handle;
		
		public uint Width => Tex2D_GetWidth(Handle);
		public uint Height => Tex2D_GetHeight(Handle);
		
		private Texture2D(IntPtr handle)
		{
			Handle = handle;
		}
		
		public static Texture2D LoadAbsPath(string path, Type type = Type.Linear32)
		{
			return new Texture2D(Tex2D_Load(path, type));
		}
		
		public static Texture2D Load(string name, Type type = Type.Linear32)
		{
			return LoadAbsPath(Program.EXEDirectory + "/Res/" + name, type);
		}
		
		~Texture2D()
		{
			Tex2D_Destroy(Handle);
		}
		
		public void Dispose()
		{
			Tex2D_Destroy(Handle);
			GC.SuppressFinalize(this);
		}
		
		public void Bind(int unit)
		{
			Tex2D_Bind(Handle, unit);
		}
		
		public void SetRepeat(bool repeat)
		{
			Tex2D_SetRepeat(Handle, repeat);
		}
		
		public void SetLodBias(float bias)
		{
			Tex2D_SetLodBias(Handle, bias);
		}
		
		public void SetSwizzle(Swizzle r, Swizzle g, Swizzle b, Swizzle a)
		{
			Tex2D_SetSwizzle(Handle, r, g, b, a);
		}
	}
}
