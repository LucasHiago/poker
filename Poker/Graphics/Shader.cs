using System;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Poker
{
	public unsafe class Shader : IDisposable
	{
		public enum StageType
		{
			Vertex = 0,
			Fragment = 1
		}
		
		[DllImport("Native")]
		private static extern IntPtr SH_Create();
		[DllImport("Native")]
		private static extern void SH_Destroy(IntPtr shader);
		[DllImport("Native")]
		private static extern void SH_AttachStage(IntPtr shader, StageType stageType, string code);
		[DllImport("Native")]
		private static extern int SH_GetUniformLocation(IntPtr shader, string name);
		[DllImport("Native")]
		private static extern void SH_Link(IntPtr shader);
		[DllImport("Native")]
		private static extern void SH_Bind(IntPtr shader);
		
		[DllImport("Native")]
		private static extern void SH_SetUniformI(IntPtr shader, int location, int value);
		[DllImport("Native")]
		private static extern void SH_SetUniformF(IntPtr shader, int location, float value);
		[DllImport("Native")]
		private static extern void SH_SetUniformF2(IntPtr shader, int location, float x, float y);
		[DllImport("Native")]
		private static extern void SH_SetUniformF3(IntPtr shader, int location, float x, float y, float z);
		[DllImport("Native")]
		private static extern void SH_SetUniformF4(IntPtr shader, int location, float x, float y, float z, float w);
		[DllImport("Native")]
		private static extern void SH_SetUniformMat4(IntPtr shader, int location, float* value);
		
		public readonly IntPtr Handle;
		
		private static ZipArchive s_archive;
		
		public static void OpenArchive()
		{
			s_archive = ZipFile.OpenRead(Program.EXEDirectory + "/Res/Shaders/Shaders");
		}
		
		public Shader()
		{
			Handle = SH_Create();
		}
		
		public void Dispose()
		{
			SH_Destroy(Handle);
			GC.SuppressFinalize(this);
		}
		
		~Shader()
		{
			SH_Destroy(Handle);
		}
		
		public void AttachStage(StageType type, string name)
		{
			ZipArchiveEntry entry = s_archive.GetEntry(name);
			if (entry == null)
				throw new ArgumentException("Shader stage not found: '" + name + "'.", nameof(name));
			
			StringBuilder sourceCodeBuilder = new StringBuilder();
			
			using (StreamReader reader = new StreamReader(entry.Open()))
			{
				while (!reader.EndOfStream)
				{
					string line = reader.ReadLine();
					if (line.StartsWith("#line") || line.StartsWith("#extension GL_GOOGLE_include_directive"))
						continue;
					sourceCodeBuilder.AppendLine(line);
				}
			}
			
			SH_AttachStage(Handle, type, sourceCodeBuilder.ToString());
		}
		
		public void Link()
		{
			SH_Link(Handle);
		}
		
		public void Bind()
		{
			SH_Bind(Handle);
		}
		
		public int GetUniformLocation(string name)
		{
			return SH_GetUniformLocation(Handle, name);
		}
		
		public void SetUniform(int location, int value)
		{
			SH_SetUniformI(Handle, location, value);
		}
		
		public void SetUniform(string name, float value)
		{
			SH_SetUniformF(Handle, GetUniformLocation(name), value);
		}
		
		public void SetUniform(int location, float value)
		{
			SH_SetUniformF(Handle, location, value);
		}
		
		public void SetUniform(int location, ref Matrix4x4 value)
		{
			fixed (float* valuePtr = &value.M11)
			{
				SH_SetUniformMat4(Handle, location, valuePtr);
			}
		}
		
		public void SetUniform(int cameraPositionLocation, Vector2 value)
		{
			SH_SetUniformF2(Handle, cameraPositionLocation, value.X, value.Y);
		}
		
		public void SetUniform(int cameraPositionLocation, Vector3 value)
		{
			SH_SetUniformF3(Handle, cameraPositionLocation, value.X, value.Y, value.Z);
		}
		
		public void SetUniform(int cameraPositionLocation, Vector4 value)
		{
			SH_SetUniformF4(Handle, cameraPositionLocation, value.X, value.Y, value.Z, value.W);
		}
	}
}
