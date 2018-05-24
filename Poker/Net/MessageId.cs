namespace Poker.Net
{
	public enum MessageId : ushort
	{
		Continuation,
		ConnectionRequest,
		ConnectionResponse,
		ClientConnected,
		ClientDisconnected,
		
		StartGame,
		P1EncryptRequest,
		P1EncryptResponse,
		P2EncryptRequest,
		P2EncryptResponse,
		DealDecryptRequest,
		DealDecryptResponse,
		DealDecryptInfo,
		
		FlopDecryptInfo,
		Flop,
		TurnDecryptInfo,
		Turn,
		RiverDecryptInfo,
		River,
		
		AllInDecryptInfo,
		AllInReveal,
		
		EndTurn,
		BeginShowdown,
		ShowdownDecryptInfo,
		ShowdownEnd,
		
		NextHand
	}
}
