using System.Collections.Generic;
using System.Linq;

namespace Poker.Hands
{
	public class HighCard : Hand
	{
		public override int Rank => 0;
		public override string Name => "High Card";
		public override IEnumerable<Card> Cards => m_cards;
		
		private readonly Card[] m_cards;
		
		private HighCard(Card[] cards)
		{
			m_cards = cards;
		}
		
		public static HighCard Create(Card[] cards)
		{
			Card[] outCards = new Card[5];
			for (int i = 0; i < 5; i++)
				outCards[i] = cards[cards.Length - i - 1];
			return new HighCard(outCards);
		}
		
		public int CompareSame(HighCard other)
		{
			for (int i = 0; i < m_cards.Length; i++)
			{
				int result = m_cards[i].Rank.CompareTo(other.m_cards[i].Rank);
				if (result != 0)
					return result;
			}
			return 0;
		}
	}
}
