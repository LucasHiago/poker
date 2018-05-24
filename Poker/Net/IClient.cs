namespace Poker.Net
{
	public interface IClient
	{
		ushort ClientId { get; }
		string Name { get; }
		bool IsConnected { get; }
		Card[] RevealedPocketCards { get; } //Null if not revealed
	}
}
