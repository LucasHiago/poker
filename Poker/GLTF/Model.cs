using System;
using System.Collections.Generic;

namespace Poker.GLTF
{
	public class Model : IDisposable
	{
		public readonly Mesh[] Meshes;
		
		public Model(Mesh[] meshes)
		{
			Meshes = meshes;
		}
		
		public void Dispose()
		{
			foreach (Mesh mesh in Meshes)
				mesh.Dispose();
		}
	}
}
