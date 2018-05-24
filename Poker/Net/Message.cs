namespace Poker.Net
{
	public struct Message
	{
		public MessageId Id;
		public byte[] Data;
		
		public Message(MessageId id, byte[] data)
		{
			Id = id;
			Data = data;
		}
	}
}
