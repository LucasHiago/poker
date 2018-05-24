namespace Poker.Net.Client
{
	public class Client : IClient
	{
		public ushort ClientId { get; private set; }
		public string Name { get; private set; }
		public bool IsConnected { get; set; }
		public Card[] RevealedPocketCards { get; set; }
		
		public int PocketCardsPosition;
		
		public Client(ushort id, string name)
		{
			ClientId = id;
			Name = name;
			IsConnected = true;
		}
	}
}
