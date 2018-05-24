using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace Poker.Net.Server
{
	public partial class Server
	{
		private class RemoteClient : BaseClient
		{
			public enum ConnectionState
			{
				WaitingForConnectionRequest,
				Connected,
				Disconnected
			}
			
			private readonly Socket m_socket;
			private readonly Receiver m_receiver = new Receiver();
			private readonly Sender m_sender = new Sender();
			private readonly object m_sendMutex = new object();
			
			private readonly Thread m_thread;
			
			private readonly Server m_server;
			
			public ConnectionState State { get; private set; } = ConnectionState.WaitingForConnectionRequest;
			
			public override bool IsConnected => State == ConnectionState.Connected;
			
			private volatile bool m_shouldDisconnect;
			
			public RemoteClient(Server server, Socket socket, ushort clientId)
				: base(clientId)
			{
				m_server = server;
				m_socket = socket;
				
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
			
			public void Disconnect()
			{
				m_shouldDisconnect = true;
				m_receiver.ShouldStop = true;
				m_socket.Close();
				m_thread.Join();
			}
			
			private void ThreadTarget()
			{
				m_socket.ReceiveTimeout = 500;
				
				while (!m_shouldDisconnect)
				{
					var waitResult = m_receiver.WaitForMessage(m_socket, out Message receivedMessage);
					
					if (waitResult == Receiver.WaitResult.SocketClosed ||
					    waitResult == Receiver.WaitResult.ShouldStop && m_shouldDisconnect)
					{
						State = ConnectionState.Disconnected;
						
						if (!m_server.Closed)
						{
							//Sends a disconnected message to all remaining clients
							byte[] disconnectMessageData = BitConverter.GetBytes(ClientId);
							var message = new Message(MessageId.ClientDisconnected, disconnectMessageData);
							foreach (RemoteClient client in m_server.RemoteClients)
								client.Send(message);
						}
						
						m_socket.Close();
						
						return;
					}
					
					if (waitResult != Receiver.WaitResult.Received)
						continue;
					
					switch (State)
					{
						case ConnectionState.WaitingForConnectionRequest:
							if (receivedMessage.Id == MessageId.ConnectionRequest)
							{
								//Reads the UTF-8 encoded name from the request message
								byte nameLength = receivedMessage.Data[0];
								byte[] nameUtf8 = new byte[nameLength];
								Array.Copy(receivedMessage.Data, 1, nameUtf8, 0, nameLength);
								SetUtf8Name(nameUtf8);
								
								BaseClient[] otherClients = m_server.ServerClients.ToArray();
								
								ConnectionResponseStatus responseStatus = ConnectionResponseStatus.OK;
								if (otherClients.Length >= Protocol.MAX_CLIENTS)
								{
									responseStatus = ConnectionResponseStatus.ServerFull;
								}
								else if (otherClients.Any(client => client.Name.Equals(Name, StringComparison.OrdinalIgnoreCase)))
								{
									responseStatus = ConnectionResponseStatus.NicknameTaken;
								}
								
								// ** Prepares the response message **
								using (MemoryStream stream = new MemoryStream())
								{
									BinaryWriter writer = new BinaryWriter(stream);
									
									writer.Write((byte)responseStatus);
									
									if (responseStatus == ConnectionResponseStatus.OK)
									{
										writer.Write(ClientId);
										
										//Writes information about other clients
										writer.Write((ushort)otherClients.Length);
										foreach (BaseClient client in otherClients)
										{
											writer.Write(client.ClientId);
											writer.Write((byte)client.NameUtf8.Length);
											writer.Write(client.NameUtf8);
										}
									}
									
									//Sends the response message
									Send(new Message {Id = MessageId.ConnectionResponse, Data = stream.GetBuffer()});
								}
								
								if (responseStatus != ConnectionResponseStatus.OK)
									return;
								
								//Sends a connection message to all already connected clients
								using (MemoryStream stream = new MemoryStream())
								{
									BinaryWriter writer = new BinaryWriter(stream);
									
									writer.Write(ClientId);
									writer.Write((byte)NameUtf8.Length);
									writer.Write(NameUtf8);
									
									var message = new Message(MessageId.ClientConnected, stream.GetBuffer());
									foreach (RemoteClient client in m_server.RemoteClients)
										client.Send(message);
								}
								
								Console.WriteLine("{0} connected with nickname \"{1}\".", m_socket.RemoteEndPoint, Name);
								State = ConnectionState.Connected;
							}
							else
							{
								Console.WriteLine("Invalid message type.");
							}
							break;
						case ConnectionState.Connected:
							MemoryStream receivedMessageStream = new MemoryStream(receivedMessage.Data);
							BinaryReader messageReader = new BinaryReader(receivedMessageStream);
							
							switch (receivedMessage.Id)
							{
							case MessageId.P1EncryptResponse:
							{
								ulong[] cards = DeckEncrypter.ReadCardsFromMessage(receivedMessage.Data, 0);
								m_server.HandleP1EncryptResponse(cards);
								break;
							}
							case MessageId.P2EncryptResponse:
							{
								ulong[] cards = DeckEncrypter.ReadCardsFromMessage(receivedMessage.Data, 0);
								m_server.HandleP2EncryptResponse(cards);
								break;
							}
							case MessageId.DealDecryptResponse:
							{
								int numClients = messageReader.ReadByte();
								ClientDecryptKey[] keys = new ClientDecryptKey[numClients];
								for (int i = 0; i < numClients; i++)
								{
									keys[i].ClientId = messageReader.ReadUInt16();
									keys[i].Key1 = messageReader.ReadUInt64();
									keys[i].Key2 = messageReader.ReadUInt64();
								}
								
								m_server.HandleDealDecryptResponse(ClientId, keys);
								break;
							}
							case MessageId.FlopDecryptInfo:
							{
								ulong[] keys = new ulong[3];
								for (int i = 0; i < keys.Length; i++)
									keys[i] = messageReader.ReadUInt64();
								
								m_server.HandleFlopDecryptInfo(ClientId, keys);
								break;
							}
							case MessageId.TurnDecryptInfo:
							{
								m_server.HandleTurnDecryptInfo(ClientId, messageReader.ReadUInt64());
								break;
							}
							case MessageId.RiverDecryptInfo:
							{
								m_server.HandleRiverDecryptInfo(ClientId, messageReader.ReadUInt64());
								break;
							}
							case MessageId.AllInDecryptInfo:
							{
								byte count = messageReader.ReadByte();
								ulong[] keys = new ulong[count];
								for (byte i = 0; i < count; i++)
									keys[i] = messageReader.ReadUInt64();
								
								m_server.HandleAllInDecryptInfo(ClientId, keys);
								break;
							}
							case MessageId.EndTurn:
							{
								TurnEndAction action = (TurnEndAction)messageReader.ReadByte();
								int raiseAmount = messageReader.ReadInt32();
								
								//TODO: Check validity
								m_server.TurnEnded(action, raiseAmount);
								
								break;
							}
							case MessageId.ShowdownDecryptInfo:
							{
								ulong[] keys = new ulong[2];
								keys[0] = messageReader.ReadUInt64();
								keys[1] = messageReader.ReadUInt64();
								
								m_server.HandleShowdownDecryptInfo(this, keys);
								
								break;
							}
							case MessageId.NextHand:
							{
								m_server.HandleNextHandMessage();
								break;
							}
							}
							break;
					}
				}
			}
		}
	}
}
