using System;
using System.Net.Sockets;

namespace Poker.Net
{
	public class Receiver
	{
		private readonly byte[] m_buffer = new byte[Protocol.BUFFER_SIZE];
		
		public enum WaitResult
		{
			Received,
			ShouldStop,
			SocketClosed
		}
		
		public bool ShouldStop { get; set; }
		
		public WaitResult WaitForMessage(Socket socket, out Message message)
		{
			MessageId pendingMessageId = default(MessageId);
			byte[] pendingMessageData = null;
			int pendingDataReceived = 0;
			
			message.Data = null;
			message.Id = default(MessageId);
			
			while (true)
			{
				int bytesReceived;
				
				try
				{
					bytesReceived = socket.Receive(m_buffer);
				}
				catch (SocketException ex)
				{
					if (ex.SocketErrorCode == SocketError.TimedOut || ex.SocketErrorCode == SocketError.Interrupted)
					{
						if (ShouldStop)
							return WaitResult.ShouldStop;
						continue;
					}
					
					if (ex.SocketErrorCode == SocketError.OperationAborted ||
						ex.SocketErrorCode == SocketError.ConnectionAborted ||
						ex.SocketErrorCode == SocketError.ConnectionReset)
					{
						return WaitResult.SocketClosed;
					}
					
					throw;
				}
				
				if (bytesReceived == 0)
					return WaitResult.SocketClosed;
				if (bytesReceived < 4)
					continue;
				
				ushort magic = BitConverter.ToUInt16(m_buffer, 0);
				if (magic != Protocol.MAGIC)
					continue;
				
				MessageId id = (MessageId)BitConverter.ToUInt16(m_buffer, 2);
				
				if (pendingMessageData == null)
				{
					//We have not received anything yet, so this should be an initial packet.
					if (id == MessageId.Continuation || bytesReceived < 8)
						continue;
					
					//Reads the remander of the initial packet header
					pendingMessageId = id;
					ushort packetCount = BitConverter.ToUInt16(m_buffer, 4);
					ushort lastPacketData = BitConverter.ToUInt16(m_buffer, 6);
					
					int firstPacketBytes = packetCount == 1 ? lastPacketData : Protocol.MAX_DATA_INITIAL_PACKET;
					
					//If we didn't receive enough data, drop the packet
					if (bytesReceived < firstPacketBytes + 8)
						continue;
					
					int dataBytes = Math.Max(packetCount - 2, 0) * Protocol.MAX_DATA_CONTINUATION_PACKET + lastPacketData;
					if (packetCount > 1)
						dataBytes += Protocol.MAX_DATA_INITIAL_PACKET;
					
					pendingMessageData = new byte[dataBytes];
					Array.Copy(m_buffer, 8, pendingMessageData, 0, firstPacketBytes);
					
					if (packetCount == 1)
						break;
					
					pendingDataReceived = Protocol.MAX_DATA_INITIAL_PACKET;
				}
				else
				{
					if (id != MessageId.Continuation)
						continue;
					
					int expectedDataBytes = Math.Min(pendingMessageData.Length - pendingDataReceived,
					                                 Protocol.MAX_DATA_CONTINUATION_PACKET);
					if (bytesReceived < expectedDataBytes + 4)
					{
						pendingMessageData = null;
						continue;
					}
					
					Array.Copy(m_buffer, 4, pendingMessageData, pendingDataReceived, expectedDataBytes);
					pendingDataReceived += expectedDataBytes;
					
					if (pendingDataReceived >= pendingMessageData.Length)
						break;
				}
			}
			
			Console.WriteLine("Got message " + pendingMessageId);
			
			message.Data = pendingMessageData;
			message.Id = pendingMessageId;
			return WaitResult.Received;
		}
	}
}
