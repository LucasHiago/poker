using System.Numerics;
using System.Runtime.InteropServices;

namespace Poker
{
	[StructLayout(LayoutKind.Sequential, Pack=1)]
	public struct TextVertex
	{
		public Vector3 Position;
		public Vector2 TexCoord;
		public uint TextId;
		
		public TextVertex(Vector3 position, Vector2 texCoord, uint textId)
		{
			Position = position;
			TexCoord = texCoord;
			TextId = textId;
		}
	}
}
