namespace Poker.Net
{
	public static class Protocol
	{
		public const int SERVER_PORT = 9908;
		public const int MAX_CLIENTS = 8;
		public const ushort MAGIC = 0x5045;
		
		public const int BUFFER_SIZE = 1024;
		public const int MAX_DATA_INITIAL_PACKET = BUFFER_SIZE - 8;
		public const int MAX_DATA_CONTINUATION_PACKET = BUFFER_SIZE - 4;
	}
}
