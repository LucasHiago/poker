using System;

namespace Poker
{
	public static class Log
	{
		public static void Write(string message)
		{
			Console.WriteLine(message);
		}
		
		public static void Error(string message)
		{
			Console.WriteLine("Error: " + message);
		}
	}
}
