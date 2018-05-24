using System;

namespace Poker.GLTF
{
	public class InvalidGLTFException : Exception
	{
		public InvalidGLTFException(string message) : base(message) { }
	}
}
