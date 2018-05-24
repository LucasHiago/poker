using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;

namespace Poker.Net
{
	public class DeckEncrypter
	{
		private readonly ulong m_globalKey;
		private readonly ulong m_globalInvKey;
		
		private readonly ulong[] m_individualKeys = new ulong[52];
		private readonly ulong[] m_individualInvKeys = new ulong[52];
		
		private const ulong PRIME = 66797;//4294967291;
		
		private static void GenerateKey(RNGCryptoServiceProvider rng, out ulong key, out ulong invKey)
		{
			byte[] buffer = new byte[sizeof(ulong)];
			rng.GetBytes(buffer);
			
			key = BitConverter.ToUInt64(buffer, 0) % (PRIME - 3);
			invKey = 0;
			
			while (invKey == 0)
			{
				key++;
				invKey = CryptoUtils.MInverse(key, PRIME - 1);
			}
		}
		
		public DeckEncrypter()
		{
			RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
			
			GenerateKey(rng, out m_globalKey, out m_globalInvKey);
			
			for (int i = 0; i < 52; i++)
			{
				GenerateKey(rng, out m_individualKeys[i], out m_individualInvKeys[i]);
			}
		}
		
		public static ulong[] ReadCardsFromMessage(byte[] messageBuffer, int offset)
		{
			ulong[] cards = new ulong[52];
			for (int i = 0; i < cards.Length; i++)
			{
				cards[i] = BitConverter.ToUInt64(messageBuffer, offset);
				offset += sizeof(ulong);
			}
			return cards;
		}
		
		public static ulong DecryptCard(ulong card, IEnumerable<ulong> decryptKeys)
		{
			return decryptKeys.Aggregate(card, (current, key) => CryptoUtils.ExpMod(current, key, PRIME));
		}
		
		public ulong GetIndividualInverseKey(int index)
		{
			return m_individualInvKeys[index];
		}
		
		public void ApplyGlobal(ulong[] cards)
		{
			for (int i = 0; i < cards.Length; i++)
				cards[i] = CryptoUtils.ExpMod(cards[i], m_globalKey, PRIME);
		}
		
		public void RemoveGlobal(ulong[] cards)
		{
			for (int i = 0; i < cards.Length; i++)
				cards[i] = CryptoUtils.ExpMod(cards[i], m_globalInvKey, PRIME);
		}
		
		public void ApplyIndividual(ulong[] cards)
		{
			for (int i = 0; i < cards.Length; i++)
				cards[i] = CryptoUtils.ExpMod(cards[i], m_individualKeys[i], PRIME);
		}
	}
}
