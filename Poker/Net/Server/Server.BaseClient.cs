using System.Collections.Generic;
using System.Text;

namespace Poker.Net.Server
{
	public partial class Server
	{
		private abstract class BaseClient : IClient
		{
			public ushort ClientId { get; }
			public string Name { get; private set; }
			public abstract bool IsConnected { get; }
			
			public int PocketCardsPosition { get; set; }
			public readonly List<ulong>[] PocketCardDecryptKeys = new List<ulong>[2];
			
			public Card[] RevealedPocketCards { get; set; }
			public byte[] NameUtf8 { get; private set; }
			
			protected BaseClient(ushort clientId, string name = null)
			{
				ClientId = clientId;
				Name = name;
				NameUtf8 = name == null ? null : Encoding.UTF8.GetBytes(name);
				
				for (int i = 0; i < PocketCardDecryptKeys.Length; i++)
					PocketCardDecryptKeys[i] = new List<ulong>();
			}
			
			protected void SetUtf8Name(byte[] utf8Name)
			{
				NameUtf8 = utf8Name;
				Name = Encoding.UTF8.GetString(utf8Name);
			}
		}
	}
}
