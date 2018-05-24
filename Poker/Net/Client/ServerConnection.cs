using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Poker.Net.Server;

namespace Poker.Net.Client
{
	public class ServerConnection : Connection
	{
		public enum State
		{
			Connecting,
			SocketError,
			Rejected,
			Connected,
			Disconnected
		}
		
		private readonly Thread m_thread;
		
		private readonly Socket m_socket;
		private readonly Receiver m_receiver = new Receiver();
		private readonly Sender m_sender = new Sender();
		
		private readonly object m_sendMutex = new object();
		
		private readonly IPAddress m_serverIP;
		
		private readonly string m_name;
		private readonly byte[] m_nameUTF8;
		
		private volatile bool m_shouldDisconnect;
		
		public event Action OnStateChanged;
		
		private ushort m_selfClientId;
		public override ushort SelfClientId => m_selfClientId; 
		
		private List<Client> m_clients;
		public override IEnumerable<IClient> Clients => m_clients;
		
		private Client[] m_turnOrderClients;
		public override IList<IClient> TurnOrderClients => m_turnOrderClients;
		
		public override IClient CurrentClient => GetClientById(GameDriver.CurrentPlayer.ClientId);
		
		public override bool Closed => CState == State.Disconnected;
		public override bool Connecting => CState == State.Connecting;
		
		private bool m_waitingForNetwork = false;
		public override bool WaitingForNetwork => m_waitingForNetwork;
		
		public ConnectionResponseStatus ConnectionResponseStatus { get; private set; }
		public SocketError SocketError { get; private set; }
		
		public State CState { get; private set; } = State.Connecting;
		
		private int m_communityCardsDeckPosition;
		private bool m_waitingForCommunityCards;
		
		private DeckEncrypter m_deckEncrypter;
		
		private bool m_waitingForP1Encrypt;
		private bool m_waitingForP2Encrypt;
		private ulong[] m_encryptedDeck;
		
		private ulong[] m_serverIndividualInvKey;
		
		//Stores the index at which this client's pocket cards are stored in the encrypted deck
		private int m_pocketCardsPosition;
		
		private readonly Random m_random = new Random();
		
		public ServerConnection(IPAddress ipAddress, string name)
		{
			m_name = name;
			m_nameUTF8 = Encoding.UTF8.GetBytes(name);
			if (m_nameUTF8.Length > 255)
				throw new ArgumentException("Name is too long.");
			
			m_serverIP = ipAddress;
			
			m_socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			
			m_thread = new Thread(ThreadTarget);
			m_thread.IsBackground = true;
			m_thread.Start();
		}
		
		public void Send(Message message)
		{
			lock (m_sendMutex)
			{
				m_sender.Send(m_socket, message);
			}
		}
		
		public override void Disconnect()
		{
			m_shouldDisconnect = true;
			m_receiver.ShouldStop = true;
			m_socket.Close();
			m_thread.Join();
		}
		
		public override void NextHand()
		{
			m_waitingForP1Encrypt = true;
			m_waitingForNetwork = true;
			Send(new Message(MessageId.NextHand, new byte[] { 0 }));
		}
		
		public override void Fold()
		{
			EndTurn(TurnEndAction.Fold, 0);
		}
		
		public override void Call()
		{
			EndTurn(TurnEndAction.Call, 0);
		}
		
		public override void Raise(int amount)
		{
			EndTurn(TurnEndAction.Raise, amount);
		}
		
		public override void AllIn()
		{
			EndTurn(TurnEndAction.AllIn, 0);
		}
		
		private void EndTurn(TurnEndAction action, int raiseAmount)
		{
			MemoryStream messageStream = new MemoryStream(5);
			BinaryWriter writer = new BinaryWriter(messageStream);
			writer.Write((byte)action);
			writer.Write(raiseAmount);
			
			Send(new Message(MessageId.EndTurn, messageStream.GetBuffer()));
		}
		
		private void SendCommunityCardDecryptInfo(int cardOffset, int numCards, MessageId messageId)
		{
			MemoryStream messageStream = new MemoryStream(sizeof(ulong) * numCards);
			BinaryWriter writer = new BinaryWriter(messageStream);
			
			int firstCardIndex = m_communityCardsDeckPosition + cardOffset;
			for (int i = 0; i < numCards; i++)
			{
				writer.Write(m_deckEncrypter.GetIndividualInverseKey(firstCardIndex + i));
			}
			
			m_waitingForCommunityCards = true;
			
			Send(new Message(messageId, messageStream.GetBuffer()));
		}
		
		private void DecryptCommunityCards(int cardOffset, int numCards, BinaryReader messageReader)
		{
			int numKeys = messageReader.ReadByte();
			ulong[] keys = new ulong[numKeys];
			
			int firstCardIndex = m_communityCardsDeckPosition + cardOffset;
			for (int i = 0; i < numCards; i++)
			{
				int cardIndex = firstCardIndex + i;
				ulong ownKey = m_deckEncrypter.GetIndividualInverseKey(cardIndex);
				bool ownKeyFound = false;
				
				for (int k = 0; k < numKeys; k++)
				{
					keys[k] = messageReader.ReadUInt64();
					if (keys[k] == ownKey)
						ownKeyFound = true;
				}
				
				if (!ownKeyFound)
				{
					Log.Error("Personal key not found for community card " + cardOffset + ".");
					return;
				}
				
				ulong cardId = DeckEncrypter.DecryptCard(m_encryptedDeck[cardIndex], keys);
				CommunityCards[cardOffset + i] = new Card((byte)cardId);
			}
			
			CommunityCardsRevealed += numCards;
			m_waitingForCommunityCards = false;
		}
		
		private void GameStageChanged(HandStage newStage)
		{
			switch (newStage)
			{
			case HandStage.Flop:
			{
				SendCommunityCardDecryptInfo(0, 3, MessageId.FlopDecryptInfo);
				break;
			}
			case HandStage.Turn:
			{
				SendCommunityCardDecryptInfo(3, 1, MessageId.TurnDecryptInfo);
				break;
			}
			case HandStage.River:
			{
				SendCommunityCardDecryptInfo(4, 1, MessageId.RiverDecryptInfo);
				break;
			}
			case HandStage.End:
			{
				if (CommunityCardsRevealed < 5)
				{
					int numCards = 5 - CommunityCardsRevealed;
					MemoryStream messageStream = new MemoryStream(sizeof(byte) + sizeof(ulong) * numCards);
					BinaryWriter writer = new BinaryWriter(messageStream);
					
					writer.Write((byte)numCards);
					for (int i = CommunityCardsRevealed; i < 5; i++)
					{
						writer.Write(m_deckEncrypter.GetIndividualInverseKey(m_communityCardsDeckPosition + i));
					}
					
					m_waitingForCommunityCards = true;
					
					Send(new Message(MessageId.AllInDecryptInfo, messageStream.GetBuffer()));
				}
				break;
			}
			}
		}
		
		private void ThreadTarget()
		{
			try
			{
				m_socket.Connect(new IPEndPoint(m_serverIP, Protocol.SERVER_PORT));
			}
			catch (SocketException ex)
			{
				SocketError = ex.SocketErrorCode;
				CState = State.SocketError;
				return;
			}
			
			// ** Sends the connection request message **
			using (MemoryStream connRequestStream = new MemoryStream())
			{
				BinaryWriter connRequestWriter = new BinaryWriter(connRequestStream);
				connRequestWriter.Write((byte)m_nameUTF8.Length);
				connRequestStream.Write(m_nameUTF8, 0, m_nameUTF8.Length);
				Send(new Message { Id = MessageId.ConnectionRequest, Data = connRequestStream.GetBuffer() });
			}
			
			m_socket.ReceiveTimeout = 500;
			
			while (true)
			{
				var waitResult = m_receiver.WaitForMessage(m_socket, out Message message);
				
				if (waitResult == Receiver.WaitResult.SocketClosed ||
				    waitResult == Receiver.WaitResult.ShouldStop && m_shouldDisconnect)
				{
					Console.WriteLine("Disconnected by server");
					
					CState = State.Disconnected;
					OnStateChanged?.Invoke();
					break;
				}
				
				if (waitResult != Receiver.WaitResult.Received)
					continue;
				
				BinaryReader messageReader = new BinaryReader(new MemoryStream(message.Data));
				
				if (CState == State.Connecting)
				{
					if (message.Id != MessageId.ConnectionResponse)
						continue;
					
					ConnectionResponseStatus = (ConnectionResponseStatus)messageReader.ReadByte();
					if (ConnectionResponseStatus != ConnectionResponseStatus.OK)
					{
						CState = State.Rejected;
						break;
					}
					
					m_selfClientId = messageReader.ReadUInt16();
					
					ushort numOtherClients = messageReader.ReadUInt16();
					m_clients = new List<Client>(numOtherClients + 1);
					
					//Reads information about other clients
					for (ushort i = 0; i < numOtherClients; i++)
					{
						ushort clientId = messageReader.ReadUInt16();
						byte nameLen = messageReader.ReadByte();
						byte[] nameUtf8 = messageReader.ReadBytes(nameLen);
						
						m_clients.Add(new Client(clientId, Encoding.UTF8.GetString(nameUtf8)));
					}
					
					m_clients.Add(new Client(SelfClientId, m_name));
					
					Console.WriteLine("Connected to server");
					
					CState = State.Connected;
					OnStateChanged?.Invoke();
				}
				else if (CState == State.Connected)
				{
					// ReSharper disable once SwitchStatementMissingSomeCases
					switch (message.Id)
					{
					case MessageId.ClientConnected:
					{
						ushort newClientId = messageReader.ReadUInt16();
						byte nameLen = messageReader.ReadByte();
						byte[] nameUtf8 = messageReader.ReadBytes(nameLen);
						
						m_clients.Add(new Client(newClientId, Encoding.UTF8.GetString(nameUtf8)));
						break;
					}
					case MessageId.ClientDisconnected:
					{
						ushort clientId = messageReader.ReadUInt16();
						int index = m_clients.FindIndex(client => client.ClientId == clientId);
						if (index != -1)
						{
							m_clients[index].IsConnected = false;
							m_clients.RemoveAt(index);
						}
						break;
					}
					case MessageId.StartGame:
					{
						int startChips = (int)messageReader.ReadUInt32();
						BigBlind = (int)messageReader.ReadUInt32();
						
						InGame = true;
						
						byte numClients = messageReader.ReadByte();
						Contract.Assert(numClients == m_clients.Count);
						
						m_turnOrderClients = new Client[numClients];
						for (int i = 0; i < numClients; i++)
						{
							ushort id = messageReader.ReadUInt16();
							m_turnOrderClients[i] = m_clients.Find(client => client.ClientId == id);
						}
						
						GameDriver = new GameDriver(m_turnOrderClients.Select(client => client.ClientId), startChips, BigBlind);
						GameDriver.OnStageChanged += GameStageChanged;
						m_waitingForP1Encrypt = true;
						m_waitingForNetwork = true;
						
						RaiseOnGameStart();
						
						break;
					}
					case MessageId.P1EncryptRequest:
					{
						if (!m_waitingForP1Encrypt)
							break;
						m_waitingForP1Encrypt = false;
						m_waitingForP2Encrypt = true;
						
						HasDealtPocketCards = false;
						CommunityCardsRevealed = 0;
						
						m_deckEncrypter = new DeckEncrypter();
						
						ulong[] cards = DeckEncrypter.ReadCardsFromMessage(message.Data, 0);
						Utils.Shuffle(cards, m_random);
						m_deckEncrypter.ApplyGlobal(cards);
						
						MemoryStream responseStream = new MemoryStream(sizeof(ulong) * 52);
						BinaryWriter responseWriter = new BinaryWriter(responseStream);
						foreach (ulong card in cards)
							responseWriter.Write(card);
						Send(new Message(MessageId.P1EncryptResponse, responseStream.GetBuffer()));
						
						break;
					}
					case MessageId.P2EncryptRequest:
					{
						if (!m_waitingForP2Encrypt)
							break;
						m_waitingForP2Encrypt = false;
						
						ulong[] cards = DeckEncrypter.ReadCardsFromMessage(message.Data, 0);
						
						m_deckEncrypter.RemoveGlobal(cards);
						m_deckEncrypter.ApplyIndividual(cards);
						
						MemoryStream responseStream = new MemoryStream(sizeof(ulong) * 52);
						BinaryWriter responseWriter = new BinaryWriter(responseStream);
						foreach (ulong card in cards)
							responseWriter.Write(card);
						Send(new Message(MessageId.P2EncryptResponse, responseStream.GetBuffer()));
						
						break;
					}
					case MessageId.DealDecryptRequest:
					{
						if (m_encryptedDeck == null)
							m_encryptedDeck = new ulong[52];
						for (int i = 0; i < 52; i++)
							m_encryptedDeck[i] = messageReader.ReadUInt64();
						
						int numClients = messageReader.ReadByte();
						
						MemoryStream responseStream = new MemoryStream();
						BinaryWriter responseWriter = new BinaryWriter(responseStream);
						
						responseWriter.Write((byte)(numClients - 1));
						
						bool RangeOverlap(int min1, int len1, int min2, int len2)
						{
							if (min2 > min1)
								return min2 < min1 + len1;
							if (min2 < min1)
								return min2 + len2 > min1;
							return true;
						}
						
						//Reads client card positions. The list maps client ids to their card positions.
						List<Tuple<ushort, int>> clientCardPositions = new List<Tuple<ushort, int>>(numClients);
						for (int i = 0; i < numClients; i++)
						{
							ushort id = messageReader.ReadUInt16();
							int cardPosition = messageReader.ReadByte();
							
							//Checks that the range doesn't overlap any previous range
							if (clientCardPositions.Any(p => RangeOverlap(cardPosition, 2, p.Item2, 2)))
							{
								Log.Error("Client card position overlap!");
								break;
							}
							
							if (id == SelfClientId)
							{
								m_pocketCardsPosition = cardPosition;
							}
							else
							{
								responseWriter.Write(id);
								for (int j = 0; j < 2; j++)
								{
									responseWriter.Write(m_deckEncrypter.GetIndividualInverseKey(cardPosition + j));
								}
							}
							
							((Client)GetClientById(id)).PocketCardsPosition = cardPosition;
							
							clientCardPositions.Add(new Tuple<ushort, int>(id, cardPosition));
						}
						
						m_communityCardsDeckPosition = messageReader.ReadByte();
						if (clientCardPositions.Any(p => RangeOverlap(m_communityCardsDeckPosition, 5, p.Item2, 2)))
						{
							Log.Error("Client card position overlap!");
							break;
						}
						
						Send(new Message(MessageId.DealDecryptResponse, responseStream.GetBuffer()));
						
						break;
					}
					case MessageId.DealDecryptInfo:
					{
						int numKeys = messageReader.ReadByte();
						ulong[][] keys = { new ulong[numKeys + 1], new ulong[numKeys + 1] };
						
						for (int i = 0; i < numKeys; i++)
						{
							for (int j = 0; j < 2; j++)
							{
								keys[j][i] = messageReader.ReadUInt64();
							}
						}
						
						keys[0][numKeys] = m_deckEncrypter.GetIndividualInverseKey(m_pocketCardsPosition + 0);
						keys[1][numKeys] = m_deckEncrypter.GetIndividualInverseKey(m_pocketCardsPosition + 1);
						
						for (int i = 0; i < 2; i++)
						{
							ulong encryptedCard = m_encryptedDeck[m_pocketCardsPosition + i];
							Card card = new Card((byte)DeckEncrypter.DecryptCard(encryptedCard, keys[i]));
							PocketCards[i] = card;
						}
						
						for (int i = 0; i < m_clients.Count; i++)
							m_clients[i].RevealedPocketCards = null;
						
						RaiseDealEvent(PocketCards[0], PocketCards[1]);
						
						m_waitingForNetwork = false;
						HasDealtPocketCards = true;
						
						StepFirstPlayerIndex();
						GameDriver.StartHand(m_turnOrderClients[FirstPlayerIndex].ClientId);
						
						RaiseTurnChanged();
						
						break;
					}
					case MessageId.EndTurn:
					{
						TurnEndAction action = (TurnEndAction)messageReader.ReadByte();
						int raiseAmount = messageReader.ReadInt32();
						
						RaiseOnPlayerAction(GameDriver.CurrentPlayer.ClientId, action, raiseAmount);
						
						switch (action)
						{
							case TurnEndAction.Fold:
								GameDriver.Fold();
								break;
							case TurnEndAction.Call:
								GameDriver.Call();
								break;
							case TurnEndAction.Raise:
								GameDriver.Raise(raiseAmount);
								break;
							case TurnEndAction.AllIn:
								GameDriver.AllIn();
								break;
						}
						
						if (!m_waitingForCommunityCards)
							RaiseTurnChanged();
						
						break;
					}
					case MessageId.Flop:
					{
						if (!m_waitingForCommunityCards)
							break;
						
						DecryptCommunityCards(0, 3, messageReader);
						RaiseFlopEvent(CommunityCards[0], CommunityCards[1], CommunityCards[2]);
						RaiseTurnChanged();
						
						break;
					}
					case MessageId.Turn:
					{
						if (!m_waitingForCommunityCards)
							break;
						
						DecryptCommunityCards(3, 1, messageReader);
						RaiseTurnEvent(CommunityCards[3]);
						RaiseTurnChanged();
						
						break;
					}
					case MessageId.River:
					{
						if (!m_waitingForCommunityCards)
							break;
						
						DecryptCommunityCards(4, 1, messageReader);
						RaiseTurnEvent(CommunityCards[4]);
						RaiseTurnChanged();
						
						break;
					}
					case MessageId.AllInReveal:
					{
						if (!m_waitingForCommunityCards)
							break;
						
						bool preFlop = CommunityCardsRevealed < 3;
						bool preTurn = CommunityCardsRevealed < 4;
						bool preRiver = CommunityCardsRevealed < 5;
						
						DecryptCommunityCards(CommunityCardsRevealed, 5 - CommunityCardsRevealed, messageReader);
						
						if (preFlop)
							RaiseFlopEvent(CommunityCards[0], CommunityCards[1], CommunityCards[2]);
						if (preTurn)
							RaiseTurnEvent(CommunityCards[3]);
						if (preRiver)
							RaiseRiverEvent(CommunityCards[4]);
						
						RaiseTurnChanged();
						
						break;
					}
					case MessageId.BeginShowdown:
					{
						if (!(GameDriver.Stage == HandStage.End || GameDriver.Stage == HandStage.River))
							break;
						
						m_serverIndividualInvKey = new ulong[2];
						m_serverIndividualInvKey[0] = messageReader.ReadUInt64();
						m_serverIndividualInvKey[1] = messageReader.ReadUInt64();
						
						MemoryStream responseStream = new MemoryStream(sizeof(ulong) * 2);
						BinaryWriter responseWriter = new BinaryWriter(responseStream);
						
						for (int i = 0; i < 2; i++)
						{
							responseWriter.Write(m_deckEncrypter.GetIndividualInverseKey(m_pocketCardsPosition + i));
						}
						
						Send(new Message(MessageId.ShowdownDecryptInfo, responseStream.GetBuffer()));
						
						break;
					}
					case MessageId.ShowdownEnd:
					{
						if (GameDriver.Stage != HandStage.End && GameDriver.Stage != HandStage.River)
							break;
						
						uint numActiveClients = messageReader.ReadUInt32();
						int numKeys = (int)messageReader.ReadUInt32();
						
						//Maps client ids to their set of decrypt keys
						var decryptKeys = new Dictionary<ushort, ulong[]>();
						
						for (uint i = 0; i < numActiveClients; i++)
						{
							ushort clientId = messageReader.ReadUInt16();
							
							var keys = new ulong[numKeys * 2];
							for (int j = 0; j < keys.Length; j++)
								keys[j] = messageReader.ReadUInt64();
							
							decryptKeys.Add(clientId, keys);
						}
						
						bool ok = true;
						foreach (Client client in m_clients.Where(c => GameDriver.GetPlayer(c.ClientId).InGame))
						{
							if (!decryptKeys.TryGetValue(client.ClientId, out ulong[] keys))
							{
								Log.Error("Server didn't send decrypt keys for active client '" + client.ClientId + "'.");
								ok = false;
								break;
							}
							
							client.RevealedPocketCards = new Card[2];
							for (int k = 0; k < 2; k++)
							{
								int deckPos = client.PocketCardsPosition + k;
								
								ulong[] cardKeys = keys.Skip(k * numKeys).Take(numKeys).ToArray();
								if (!cardKeys.Contains(m_deckEncrypter.GetIndividualInverseKey(deckPos)))
								{
									Log.Error("Pocket card inverse key mismatch, own key not present.");
									ok = false;
									break;
								}
								
								//ClientId == 0 means that this client is the host
								if (client.ClientId == 0 && !cardKeys.Contains(m_serverIndividualInvKey[k]))
								{
									Log.Error("Pocket card inverse key mismatch, server's key not present.");
									ok = false;
									break;
								}
								
								ulong encryptedCard = m_encryptedDeck[deckPos];
								ulong decryptedCard = DeckEncrypter.DecryptCard(encryptedCard, cardKeys);
								client.RevealedPocketCards[k] = new Card((byte)decryptedCard);
							}
							
							if (!ok)
								break;
						}
						
						if (ok)
						{
							//TODO: Victory logic & maybe raise event
						}
						
						break;
					}
					}
				}
			}
			
			m_socket.Close();
		}
	}
}
