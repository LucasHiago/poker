using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Poker.Net.Server
{
	public partial class Server : Connection
	{
		private enum PendingNetOperation
		{
			None,
			DealEncryptP1,
			DealEncryptP2,
			DealDecryptResponse,
			FlopDecryptInfo,
			TurnDecryptInfo,
			RiverDecryptInfo,
			AllInDecryptInfo,
			ShowdownDecryptInfo
		};
		
		private readonly Socket m_socket;
		
		private readonly List<RemoteClient> m_clients = new List<RemoteClient>();
		
		private readonly SelfClient m_selfClient;
		
		public override ushort SelfClientId => 0;
		
		public override bool Closed => m_closed;
		public override bool Connecting => false;
		public override bool WaitingForNetwork => m_pendingNetOperation != PendingNetOperation.None;
		
		private volatile bool m_closed;
		
		public override IEnumerable<IClient> Clients => ServerClients;
		
		private IEnumerable<BaseClient> ServerClients
		{
			get
			{
				yield return m_selfClient;
				foreach (RemoteClient client in RemoteClients)
					yield return client;
			}
		}
		
		private IEnumerable<RemoteClient> RemoteClients
		{
			get
			{
				foreach (RemoteClient client in m_clients)
				{
					if (client.State == RemoteClient.ConnectionState.Connected)
						yield return client;
				}
			}
		}
		
		private BaseClient[] m_turnOrderClients;
		public override IList<IClient> TurnOrderClients => m_turnOrderClients;
		
		private PendingNetOperation m_pendingNetOperation;
		private bool m_startShowdownAfterReveal;
		
		public override IClient CurrentClient =>
			m_pendingNetOperation == PendingNetOperation.None ? GetClientById(GameDriver.CurrentPlayer.ClientId) : null;
		
		private int m_startChips = 500;
		
		private List<ulong>[] m_communityCardDecryptKeys;
		private int m_communityCardsDeckPosition;
		
		private readonly Random m_rand = new Random();
		
		private readonly object m_mutex = new object();
		
		private ushort m_nextClientId = 1;
		
		private DeckEncrypter m_deckEncrypter;
		private int m_encryptionClientIndex = -1;
		
		private int m_numNextHandMessagesReceived = 0;
		
		//Stores a list of clients which havn't yet submitted decrypt responses
		private readonly List<ushort> m_pendingDecryptResponses = new List<ushort>();
		
		private ulong[] m_encryptedDeck;
		
		public Server(string nickname)
		{
			InGame = false;
			BigBlind = 2;
			
			m_selfClient = new SelfClient(nickname);
			m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			m_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			
			m_socket.Bind(new IPEndPoint(IPAddress.Any, Protocol.SERVER_PORT));
			m_socket.Listen(Protocol.MAX_CLIENTS);
			m_socket.BeginAccept(AcceptCallback, null);
		}
		
		private void AcceptCallback(IAsyncResult ar)
		{
			if (m_closed)
				return;
			
			m_clients.Add(new RemoteClient(this, m_socket.EndAccept(ar), m_nextClientId++));
			m_socket.BeginAccept(AcceptCallback, null);
		}
		
		public void SendToAll(Message message)
		{
			foreach (RemoteClient client in RemoteClients)
			{
				client.Send(message);
			}
		}
		
		private void InitializePendingDecryptResponses()
		{
			m_pendingDecryptResponses.Clear();
			foreach (RemoteClient client in m_clients)
				m_pendingDecryptResponses.Add(client.ClientId);
		}
		
		private void HandleNextHandMessage()
		{
			if (GameDriver.Stage != HandStage.End && GameDriver.Stage != HandStage.River)
			{
				Log.Error("Invalid stage for next hand message.");
				return;
			}
			m_numNextHandMessagesReceived++;
			MaybeStartNextHand();
		}
		
		private void MaybeStartNextHand()
		{
			if (m_numNextHandMessagesReceived <= RemoteClients.Count())
				return;
			m_numNextHandMessagesReceived = 0;
			
			StartHand();
		}
		
		public override void NextHand()
		{
			m_numNextHandMessagesReceived++;
			MaybeStartNextHand();
		}
		
		public override void Fold()
		{
			TurnEnded(TurnEndAction.Fold, 0);
		}
		
		public override void Call()
		{
			TurnEnded(TurnEndAction.Call, 0);
		}
		
		public override void Raise(int amount)
		{
			TurnEnded(TurnEndAction.Raise, amount);
		}
		
		public override void AllIn()
		{
			TurnEnded(TurnEndAction.AllIn, 0);
		}
		
		private void TurnEnded(TurnEndAction action, int raiseAmount)
		{
			RaiseOnPlayerAction(GameDriver.CurrentPlayer.ClientId, action, raiseAmount);
			
			switch (action)
			{
			case TurnEndAction.Call:
				GameDriver.Call();
				break;
			case TurnEndAction.Raise:
				GameDriver.Raise(raiseAmount);
				break;
			case TurnEndAction.Fold:
				GameDriver.Fold();
				break;
			case TurnEndAction.AllIn:
				GameDriver.AllIn();
				break;
			}
			
			lock (m_mutex)
			{
				MemoryStream messageStream = new MemoryStream(5);
				BinaryWriter writer = new BinaryWriter(messageStream);
				writer.Write((byte)action);
				writer.Write(raiseAmount);
				
				SendToAll(new Message(MessageId.EndTurn, messageStream.GetBuffer()));
				
				//The previous calls to m_gameDriver may have cause a stage change.
				//In this case the turn changed event should be postponed until the stage change is finalized.
				if (m_pendingNetOperation == PendingNetOperation.None)
					RaiseTurnChanged();
			}
		}
		
		private void BeginShowdown()
		{
			Log.Write("Starting showdown");
			
			MemoryStream messageStream = new MemoryStream(2 * sizeof(ulong));
			BinaryWriter writer = new BinaryWriter(messageStream);
			for (int i = 0; i < 2; i++)
			{
				writer.Write(m_deckEncrypter.GetIndividualInverseKey(m_selfClient.PocketCardsPosition + i));
			}
			
			Message message = new Message(MessageId.BeginShowdown, messageStream.GetBuffer());
			foreach (RemoteClient remote in RemoteClients)
			{
				remote.Send(message);
			}
			
			m_pendingNetOperation = PendingNetOperation.ShowdownDecryptInfo;
			InitializePendingDecryptResponses();
		}
		
		private void HandleShowdownDecryptInfo(RemoteClient client, ulong[] keys)
		{
			lock (m_mutex)
			{
				Log.Write("Got showdown decrypt info from " + client.ClientId + ".");
				
				int index = m_pendingDecryptResponses.IndexOf(client.ClientId);
				if (m_pendingNetOperation != PendingNetOperation.ShowdownDecryptInfo || index == -1)
				{
					Log.Error("Client " + client.ClientId + " sent unwanted showdown decrypt info.");
					return;
				}
				
				for (int i = 0; i < 2; i++)
				{
					client.PocketCardDecryptKeys[i].Add(keys[i]);
				}
				
				m_pendingDecryptResponses.RemoveAt(index);
				if (m_pendingDecryptResponses.Count != 0)
					return;
			}
			
			MemoryStream responseStream = new MemoryStream();
			BinaryWriter responseWriter = new BinaryWriter(responseStream);
			
			//Selects all clients which are still active (not folded or bust)
			BaseClient[] activeClients = ServerClients.Where(c => GameDriver.GetPlayer(c.ClientId).InGame).ToArray();
			
			int numKeys = ServerClients.Count();
			
			responseWriter.Write((uint)activeClients.Length);
			responseWriter.Write((uint)numKeys);
			for (int i = 0; i < activeClients.Length; i++)
			{
				//Decrypts this client's pocket cards
				if (activeClients[i].ClientId == SelfClientId)
				{
					activeClients[i].RevealedPocketCards = PocketCards;
				}
				else
				{
					activeClients[i].RevealedPocketCards = new Card[2];
					for (int k = 0; k < 2; k++)
					{
						ulong encryptedCard = m_encryptedDeck[activeClients[i].PocketCardsPosition + k];
						ulong card = DeckEncrypter.DecryptCard(encryptedCard, activeClients[i].PocketCardDecryptKeys[k]);
						activeClients[i].RevealedPocketCards[k] = new Card((byte)card);
					}
				}
				
				//Adds decryption keys for this client to the message
				responseWriter.Write(activeClients[i].ClientId);
				for (int k = 0; k < 2; k++)
				{
					foreach (ulong key in activeClients[i].PocketCardDecryptKeys[k].Take(numKeys))
					{
						responseWriter.Write(key);
					}
				}
			}
			
			SendToAll(new Message(MessageId.ShowdownEnd, responseStream.GetBuffer()));
			
			m_pendingNetOperation = PendingNetOperation.None;
			
			//TODO: Raise event?
		}
		
		private void GameStageChanged(HandStage newStage)
		{
			switch (newStage)
			{
			case HandStage.Flop:
			{
				m_pendingNetOperation = PendingNetOperation.FlopDecryptInfo;
				BeginDecryptingCommunityCards(0, 3);
				break;
			}
			case HandStage.Turn:
			{
				m_pendingNetOperation = PendingNetOperation.TurnDecryptInfo;
				BeginDecryptingCommunityCards(3, 1);
				break;
			}
			case HandStage.River:
			{
				m_pendingNetOperation = PendingNetOperation.RiverDecryptInfo;
				BeginDecryptingCommunityCards(4, 1);
				m_startShowdownAfterReveal = true;
				break;
			}
			case HandStage.End:
			{
				if (CommunityCardsRevealed < 5)
				{
					m_pendingNetOperation = PendingNetOperation.AllInDecryptInfo;
					BeginDecryptingCommunityCards(CommunityCardsRevealed, 5 - CommunityCardsRevealed);
					m_startShowdownAfterReveal = true;
				}
				else
				{
					//Maybe not always call this?
					BeginShowdown();
				}
				break;
			}
			}
		}
		
		private void BeginDecryptingCommunityCards(int deckPositionOffset, int numCards)
		{
			int deckPosition = m_communityCardsDeckPosition + deckPositionOffset;
			m_communityCardDecryptKeys = new List<ulong>[numCards];
			
			for (int i = 0; i < numCards; i++)
			{
				m_communityCardDecryptKeys[i] = new List<ulong>
				{
					m_deckEncrypter.GetIndividualInverseKey(deckPosition + i)
				};
			}
			
			InitializePendingDecryptResponses();
		}
		
		private bool HandleCommunityCardDecryptInfo(ushort senderId, ulong[] keys)
		{
			if (keys.Length != m_communityCardDecryptKeys.Length)
			{
				Log.Error("Client " + senderId + " sent incorrect number of community card decrypt keys.");
				return false;
			}
			
			int index = m_pendingDecryptResponses.IndexOf(senderId);
			if (index == -1)
			{
				Log.Error("Client " + senderId + " sent unwanted community card decrypt info.");
				return false;
			}
			
			for (int i = 0; i < m_communityCardDecryptKeys.Length; i++)
				m_communityCardDecryptKeys[i].Add(keys[i]);
			
			m_pendingDecryptResponses.RemoveAt(index);
			return m_pendingDecryptResponses.Count == 0;
		}
		
		private void DecryptCommunityCards(int firstCommunityCardIndex, out byte[] messageData)
		{
			MemoryStream messageStream = new MemoryStream();
			BinaryWriter messageWriter = new BinaryWriter(messageStream);
			
			messageWriter.Write((byte)m_communityCardDecryptKeys[0].Count);
			
			int deckPosition = m_communityCardsDeckPosition + firstCommunityCardIndex;
			
			for (int i = 0; i < m_communityCardDecryptKeys.Length; i++)
			{
				foreach (ulong key in m_communityCardDecryptKeys[i])
					messageWriter.Write(key);
				
				ulong encryptedCard = m_encryptedDeck[deckPosition + i];
				ulong cardId = DeckEncrypter.DecryptCard(encryptedCard, m_communityCardDecryptKeys[i]);
				CommunityCards[firstCommunityCardIndex + i] = new Card((byte)cardId);
			}
			
			CommunityCardsRevealed += m_communityCardDecryptKeys.Length;
			m_communityCardDecryptKeys = null;
			
			messageData = messageStream.GetBuffer();
			
			m_pendingNetOperation = PendingNetOperation.None;
		}
		
		private void HandleFlopDecryptInfo(ushort senderId, ulong[] keys)
		{
			lock (m_mutex)
			{
				if (m_pendingNetOperation != PendingNetOperation.FlopDecryptInfo)
					return;
				
				if (!HandleCommunityCardDecryptInfo(senderId, keys))
					return;
				
				DecryptCommunityCards(0, out byte[] messageData);
				SendToAll(new Message(MessageId.Flop, messageData));
				
				RaiseFlopEvent(CommunityCards[0], CommunityCards[1], CommunityCards[2]);
				RaiseTurnChanged();
			}
		}
		
		private void HandleTurnDecryptInfo(ushort senderId, ulong key)
		{
			lock (m_mutex)
			{
				if (m_pendingNetOperation != PendingNetOperation.TurnDecryptInfo)
					return;
				
				if (!HandleCommunityCardDecryptInfo(senderId, new [] { key }))
					return;
				
				DecryptCommunityCards(3, out byte[] messageData);
				SendToAll(new Message(MessageId.Turn, messageData));
				
				RaiseTurnEvent(CommunityCards[3]);
				RaiseTurnChanged();
			}
		}
		
		private void HandleRiverDecryptInfo(ushort senderId, ulong key)
		{
			lock (m_mutex)
			{
				if (m_pendingNetOperation != PendingNetOperation.RiverDecryptInfo)
					return;
				
				if (!HandleCommunityCardDecryptInfo(senderId, new [] { key }))
					return;
				
				DecryptCommunityCards(4, out byte[] messageData);
				SendToAll(new Message(MessageId.River, messageData));
				
				RaiseRiverEvent(CommunityCards[4]);
				RaiseTurnChanged();
				
				if (m_startShowdownAfterReveal && CommunityCardsRevealed == 5)
				{
					m_startShowdownAfterReveal = false;
					BeginShowdown();
				}
			}
		}
		
		private void HandleAllInDecryptInfo(ushort senderId, ulong[] keys)
		{
			lock (m_mutex)
			{
				if (m_pendingNetOperation != PendingNetOperation.AllInDecryptInfo)
					return;
				
				if (!HandleCommunityCardDecryptInfo(senderId, keys))
					return;
				
				bool preFlop = CommunityCardsRevealed < 3;
				bool preTurn = CommunityCardsRevealed < 4;
				bool preRiver = CommunityCardsRevealed < 5;
				
				DecryptCommunityCards(CommunityCardsRevealed, out byte[] messageData);
				SendToAll(new Message(MessageId.AllInReveal, messageData));
				
				if (preFlop)
					RaiseFlopEvent(CommunityCards[0], CommunityCards[1], CommunityCards[2]);
				if (preTurn)
					RaiseTurnEvent(CommunityCards[3]);
				if (preRiver)
					RaiseRiverEvent(CommunityCards[4]);
				
				RaiseTurnChanged();
				
				if (m_startShowdownAfterReveal && CommunityCardsRevealed == 5)
				{
					m_startShowdownAfterReveal = false;
					BeginShowdown();
				}
			}
		}
		
		public void StartGame()
		{
			if (InGame)
				return;
			InGame = true;
			
			lock (m_mutex)
			{
				for (int i = 0; i < m_clients.Count;)
				{
					if (!m_clients[i].IsConnected)
					{
						m_clients[i].Disconnect();
						m_clients.RemoveAt(i);
					}
					else
					{
						i++;
					}
				}
				
				FirstPlayerIndex = 0;
				
				//Shuffles player order
				ushort[] clientIds = Clients.Select(c => c.ClientId).ToArray();
				Utils.Shuffle(clientIds, m_rand);
				m_turnOrderClients = clientIds.Select(GetClientById).Cast<BaseClient>().ToArray();
				
				GameDriver = new GameDriver(m_turnOrderClients.Select(client => client.ClientId), m_startChips, BigBlind);
				GameDriver.OnStageChanged += GameStageChanged;
				
				//Prepares and sends the start game message
				using (MemoryStream startMessageStream = new MemoryStream())
				{
					BinaryWriter writer = new BinaryWriter(startMessageStream);
					
					writer.Write((uint)m_startChips);
					writer.Write((uint)BigBlind);
					
					writer.Write((byte)clientIds.Length);
					for (int i = 0; i < clientIds.Length; i++)
						writer.Write(clientIds[i]);
					
					Message startMessage = new Message(MessageId.StartGame, startMessageStream.GetBuffer());
					foreach (RemoteClient client in RemoteClients)
						client.Send(startMessage);
				}
				
				StartHand();
			}
			
			RaiseOnGameStart();
		}
		
		private void StartHand()
		{
			StepFirstPlayerIndex();
			
			HasDealtPocketCards = false;
			CommunityCardsRevealed = 0;
			
			m_deckEncrypter = new DeckEncrypter();
			
			//Generates an initial encrypted card deck and shuffles it
			ulong[] cards = new ulong[52];
			for (int i = 0; i < cards.Length; i++)
				cards[i] = (ulong)i;
			m_deckEncrypter.ApplyGlobal(cards);
			Utils.Shuffle(cards, m_rand);
			
			m_encryptionClientIndex = 0;
			m_pendingNetOperation = PendingNetOperation.DealEncryptP1;
			SendEncryptRequest(m_clients[m_encryptionClientIndex], cards);
		}
		
		private void SendEncryptRequest(RemoteClient client, ulong[] cards)
		{
			MemoryStream stream = new MemoryStream(sizeof(ulong) * 52);
			BinaryWriter writer = new BinaryWriter(stream);
			
			for (int i = 0; i < 52; i++)
				writer.Write(cards[i]);
			
			MessageId messageId;
			if (m_pendingNetOperation == PendingNetOperation.DealEncryptP1)
				messageId = MessageId.P1EncryptRequest;
			else if (m_pendingNetOperation == PendingNetOperation.DealEncryptP2)
				messageId = MessageId.P2EncryptRequest;
			else
				throw new InvalidOperationException("Invalid pending net operation state.");
			
			client.Send(new Message(messageId, stream.GetBuffer()));
		}
		
		private void HandleP1EncryptResponse(ulong[] cards)
		{
			lock (m_mutex)
			{
				if (m_pendingNetOperation != PendingNetOperation.DealEncryptP1)
					return;
				
				m_encryptionClientIndex++;
				
				if (m_encryptionClientIndex < m_clients.Count)
				{
					SendEncryptRequest(m_clients[m_encryptionClientIndex], cards);
				}
				else
				{
					m_encryptionClientIndex = 0;
					m_pendingNetOperation = PendingNetOperation.DealEncryptP2;
					
					m_deckEncrypter.RemoveGlobal(cards);
					m_deckEncrypter.ApplyIndividual(cards);
					
					SendEncryptRequest(m_clients[m_encryptionClientIndex], cards);
				}
			}
		}
		
		private void HandleP2EncryptResponse(ulong[] cards)
		{
			lock (m_mutex)
			{
				if (m_pendingNetOperation != PendingNetOperation.DealEncryptP2)
					return;
				
				m_encryptionClientIndex++;
				
				if (m_encryptionClientIndex < m_clients.Count)
				{
					SendEncryptRequest(m_clients[m_encryptionClientIndex], cards);
				}
				else
				{
					m_encryptedDeck = cards;
					
					//Prepares and sends the start hand message
					MemoryStream messageStream = new MemoryStream();
					BinaryWriter writer = new BinaryWriter(messageStream);
					
					foreach (ulong card in cards)
						writer.Write(card);
					
					//Writes where each client's pocket cards are positioned in the encrypted array
					writer.Write((byte)(m_clients.Count + 1));
					int cardIndex = 0;
					foreach (BaseClient client in ServerClients)
					{
						client.PocketCardsPosition = cardIndex;
						
						for (int i = 0; i < 2; i++)
						{
							client.PocketCardDecryptKeys[i].Clear();
							client.PocketCardDecryptKeys[i].Add(m_deckEncrypter.GetIndividualInverseKey(cardIndex + i));
						}
						
						writer.Write(client.ClientId);
						writer.Write((byte)cardIndex);
						
						cardIndex += 2;
					}
					
					m_communityCardsDeckPosition = cardIndex;
					writer.Write((byte)m_communityCardsDeckPosition);
					
					m_pendingNetOperation = PendingNetOperation.DealDecryptResponse;
					InitializePendingDecryptResponses();
					
					SendToAll(new Message(MessageId.DealDecryptRequest, messageStream.GetBuffer()));
				}
			}
		}
		
		private void HandleDealDecryptResponse(ushort senderId, ClientDecryptKey[] keys)
		{
			lock (m_mutex)
			{
				if (m_pendingNetOperation != PendingNetOperation.DealDecryptResponse)
					return;
				
				int index = m_pendingDecryptResponses.IndexOf(senderId);
				if (index == -1)
				{
					Log.Error("Client " + senderId + " sent unwanted deal decrypt response.");
					return;
				}
				
				foreach (BaseClient client in ServerClients)
				{
					//The sender won't send it's own key
					if (client.ClientId == senderId)
						continue;
					
					int keyIndex = Array.FindIndex(keys, key => key.ClientId == client.ClientId);
					if (keyIndex == -1)
					{
						Log.Error("Client " + senderId + " did not send decrypt key for client " + client.ClientId + ".");
						return;
					}
					
					client.PocketCardDecryptKeys[0].Add(keys[keyIndex].Key1);
					client.PocketCardDecryptKeys[1].Add(keys[keyIndex].Key2);
				}
				
				m_pendingDecryptResponses.RemoveAt(index);
				
				if (m_pendingDecryptResponses.Count > 0)
					return;
				//The server has received all decrypt responses
				
				//Sends out decrypt keys to the clients
				foreach (RemoteClient client in m_clients)
				{
					MemoryStream stream = new MemoryStream();
					BinaryWriter writer = new BinaryWriter(stream);
					
					writer.Write((byte)client.PocketCardDecryptKeys[0].Count);
					for (int i = 0; i < client.PocketCardDecryptKeys[0].Count; i++)
					{
						for (int j = 0; j < 2; j++)
						{
							writer.Write(client.PocketCardDecryptKeys[j][i]);
						}
					}
					
					client.RevealedPocketCards = null;
					client.Send(new Message(MessageId.DealDecryptInfo, stream.GetBuffer()));
				}
				
				m_selfClient.RevealedPocketCards = null;
				
				//Decrypts the host's pocket cards
				for (int i = 0; i < 2; i++)
				{
					ulong encryptedCard = m_encryptedDeck[m_selfClient.PocketCardsPosition + i];
					ulong cardId = DeckEncrypter.DecryptCard(encryptedCard, m_selfClient.PocketCardDecryptKeys[i]);
					
					Card card = new Card((byte)cardId);
					PocketCards[i] = card;
				}
				
				m_pendingNetOperation = PendingNetOperation.None;
				HasDealtPocketCards = true;
				
				RaiseDealEvent(PocketCards[0], PocketCards[1]);
				
				GameDriver.StartHand(m_turnOrderClients[FirstPlayerIndex].ClientId);
				RaiseTurnChanged();
			}
		}
		
		public override void Disconnect()
		{
			m_closed = true;
			
			RemoteClient[] clients = m_clients.ToArray();
			m_clients.Clear();
			foreach (RemoteClient client in clients)
				client.Disconnect();
			
			m_clients.Sort();
			
			m_socket.Close();
		}
	}
}
