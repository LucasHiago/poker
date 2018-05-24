using System.Numerics;
using System.Runtime.InteropServices;

namespace Poker
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct Vertex
	{
		public Vector3 Position;
		public Vector3 Normal;
		public Vector3 Tangent;
		public Vector2 TexCoord;
	}
}
