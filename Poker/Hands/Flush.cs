using System.Collections.Generic;
using System.Linq;

namespace Poker.Hands
{
	public class Flush : Hand
	{
		public override int Rank => 5;
		public override string Name => "Flush";
		public override IEnumerable<Card> Cards => m_cards;
		//public override Color Color => new Color(230, 147, 39);
		
		private readonly Card[] m_cards;
		
		private Flush(Card[] cards)
		{
			m_cards = cards;
		}
		
		public static Flush Create(Card[] cards)
		{
			for (int i = 6; i >= 4; i--)
			{
				Suits suit = cards[i].Suit;
				Card[] flush = cards.Where(card => card.Suit == suit).Take(5).ToArray();
				if (flush.Length == 5)
					return new Flush(flush);
			}
			
			return null;
		}
		
		public int CompareSame(Flush other)
		{
			for (int i = m_cards.Length - 1; i >= 0; i--)
			{
				int comp = m_cards[i].Rank.CompareTo(other.m_cards[i].Rank);
				if (comp != 0)
					return comp;
			}
			return 0;
		}
	}
}
