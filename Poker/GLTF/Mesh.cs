namespace Poker.GLTF
{
	public class Mesh : Poker.Mesh
	{
		public readonly string Name;
		
		public Mesh(string name, Vertex[] vertices, uint[] indices)
			: base(vertices, indices)
		{
			Name = name;
		}
	}
}
