using System;
using System.Runtime.InteropServices;

namespace Poker
{
	public static class SkyRenderer
	{
		private static Shader s_shader;
		
		[DllImport("Native")]
		private static extern void SKY_Load(string texturesDirPath);
		[DllImport("Native")]
		private static extern void SKY_Destroy();
		[DllImport("Native")]
		private static extern void SKY_Draw(IntPtr shaderHandle);
		
		public static void Load()
		{
			SKY_Load(Program.EXEDirectory + "/Res/Textures/Sky");
			
			s_shader = new Shader();
			s_shader.AttachStage(Shader.StageType.Vertex, "Sky.vs.glsl");
			s_shader.AttachStage(Shader.StageType.Fragment, "Sky.fs.glsl");
			s_shader.Link();
		}
		
		public static void Dispose()
		{
			SKY_Destroy();
			s_shader.Dispose();
		}
		
		public static void Draw()
		{
			SKY_Draw(s_shader.Handle);
		}
	}
}
