using System;
using System.IO;
using System.Net.Sockets;

namespace Poker.Net
{
	public class Sender
	{
		private readonly byte[] m_buffer = new byte[Protocol.BUFFER_SIZE];
		
		public Sender()
		{
			//Initializes the first two bytes of the buffer to the protocol magic
			byte[] magicBytes = BitConverter.GetBytes(Protocol.MAGIC);
			Array.Copy(magicBytes, m_buffer, magicBytes.Length);
		}
		
		public void Send(Socket socket, Message message)
		{
			if (message.Data.Length == 0)
				throw new InvalidOperationException("Attempted to send empty message.");
			
			//Calculates how many packets will be needed
			int packetCount = 1;
			int lastPacketSize = message.Data.Length;
			if (lastPacketSize > Protocol.MAX_DATA_INITIAL_PACKET)
			{
				packetCount++;
				lastPacketSize -= Protocol.MAX_DATA_INITIAL_PACKET;
				
				packetCount += (ushort)(lastPacketSize / Protocol.MAX_DATA_CONTINUATION_PACKET);
				lastPacketSize %= Protocol.MAX_DATA_CONTINUATION_PACKET;
				
				if (lastPacketSize == 0)
					lastPacketSize = Protocol.MAX_DATA_CONTINUATION_PACKET;
				else
					packetCount++;
			}
			
			//Writes the first packet's header
			using (BinaryWriter headerWriter = new BinaryWriter(new MemoryStream(m_buffer, 2, 6)))
			{
				headerWriter.Write((ushort)message.Id);
				headerWriter.Write((ushort)packetCount);
				headerWriter.Write((ushort)lastPacketSize);
			}
			
			//Copies message data to the first packet
			int firstPacketDataSize = Math.Min(Protocol.MAX_DATA_INITIAL_PACKET, message.Data.Length);
			Array.Copy(message.Data, 0, m_buffer, 8, firstPacketDataSize);
			
			//Sends the first packet
			socket.Send(m_buffer, 0, firstPacketDataSize + 8, SocketFlags.None);
			
			//Sends continuation packets
			m_buffer[2] = m_buffer[3] = 0;
			for (int i = 0; i < packetCount - 1; i++)
			{
				int offset = Protocol.MAX_DATA_INITIAL_PACKET + Protocol.MAX_DATA_CONTINUATION_PACKET * i;
				int dataSize = Math.Min(message.Data.Length - offset, Protocol.MAX_DATA_CONTINUATION_PACKET);
				
				Array.Copy(message.Data, offset, m_buffer, 4, dataSize);
				
				socket.Send(m_buffer, 0, dataSize + 4, SocketFlags.None);
			}
			
			Log.Write("Sent message " + message.Id + " to " + socket.RemoteEndPoint);
		}
	}
}
