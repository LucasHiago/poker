using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Poker
{
	public unsafe class ChipsRenderer : IDisposable
	{
		public static ChipsRenderer Instance;
		
		[StructLayout(LayoutKind.Sequential, Pack=1)]
		private struct Chip
		{
			public readonly float X;
			public readonly float Y;
			public readonly float Z;
			public readonly float Rotation;
			
			public Chip(Vector3 pos, float rotation)
			{
				X = pos.X;
				Y = pos.Y;
				Z = pos.Z;
				Rotation = rotation;
			}
		}
		
		[DllImport("Native")]
		private static extern IntPtr CB_Create(ulong maxChips);
		[DllImport("Native")]
		private static extern void CB_Destroy(IntPtr handle);
		[DllImport("Native")]
		private static extern void CB_Upload(IntPtr handle, Chip* chips, ulong chipsCount);
		[DllImport("Native")]
		private static extern void CB_Bind(IntPtr handle, uint unit);
		
		private readonly IntPtr m_chipsBufferHandle;
		private readonly Shader m_shader;
		private readonly Shader m_shadowShader;
		private readonly GLTF.Model m_chipModel;
		
		private readonly int m_albedoLocation;
		
		public const float CHIP_SCALE = 0.05f;
		public const float CHIP_HEIGHT = 0.1f * CHIP_SCALE;
		
		private const ulong MAX_CHIPS = 1024 * Net.Protocol.MAX_CLIENTS;
		
		private int m_numChips;
		private readonly Chip[] m_chips = new Chip[MAX_CHIPS];
		
		public ChipsRenderer()
		{
			m_chipsBufferHandle = CB_Create(MAX_CHIPS);
			
			m_shader = new Shader();
			m_shader.AttachStage(Shader.StageType.Vertex, "Chip.vs.glsl");
			m_shader.AttachStage(Shader.StageType.Fragment, "Chip.fs.glsl");
			m_shader.Link();
			
			m_shadowShader = new Shader();
			m_shadowShader.AttachStage(Shader.StageType.Vertex, "ChipShadow.vs.glsl");
			m_shadowShader.Link();
			
			m_albedoLocation = m_shader.GetUniformLocation("albedo");
			
			m_shader.SetUniform("specularIntensity", SPECULAR_INTENSITY);
			m_shader.SetUniform("specularExponent", SPECULAR_EXPONENT);
			m_shader.SetUniform("scale", CHIP_SCALE);
			m_shadowShader.SetUniform("scale", CHIP_SCALE);
			
			m_chipModel = GLTF.GLTFImporter.Import(Program.EXEDirectory + "/Res/Models/Chip.gltf");
		}
		
		~ChipsRenderer()
		{
			CB_Destroy(m_chipsBufferHandle);
		}
		
		public void Dispose()
		{
			m_shader.Dispose();
			m_shadowShader.Dispose();
			m_chipModel.Dispose();
			
			CB_Destroy(m_chipsBufferHandle);
			GC.SuppressFinalize(this);
		}
		
		public void Begin()
		{
			m_numChips = 0;
		}
		
		public void End()
		{
			fixed (Chip* chip = m_chips)
			{
				CB_Upload(m_chipsBufferHandle, chip, (ulong)m_numChips);
			}
		}
		
		public void Add(Vector3 position, float rotation)
		{
			m_chips[m_numChips++] = new Chip(position, rotation);
		}
		
		private readonly Vector3 RED_ALBEDO = new Vector3(1, 0, 0);
		private readonly Vector3 WHITE_ALBEDO = new Vector3(1, 1, 1);
		private const float SPECULAR_INTENSITY = 1;
		private const float SPECULAR_EXPONENT = 5;
		
		public void DrawShadows()
		{
			m_shadowShader.Bind();
			
			CB_Bind(m_chipsBufferHandle, 0);
			
			foreach (GLTF.Mesh mesh in m_chipModel.Meshes)
				mesh.DrawInstanced((uint)m_numChips);
		}
		
		public void Draw()
		{
			m_shader.Bind();
			
			CB_Bind(m_chipsBufferHandle, 0);
			
			m_shader.SetUniform(m_albedoLocation, RED_ALBEDO);
			m_chipModel.Meshes[0].DrawInstanced((uint)m_numChips);
			
			m_shader.SetUniform(m_albedoLocation, WHITE_ALBEDO);
			m_chipModel.Meshes[1].DrawInstanced((uint)m_numChips);
		}
	}
}
