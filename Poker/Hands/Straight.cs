using System.Collections.Generic;
using System.Linq;

namespace Poker.Hands
{
	public class Straight : Hand
	{
		public override int Rank => 4;
		public override string Name => "Straight";
		public override IEnumerable<Card> Cards => m_cards;
		public override Color Color => new Color(255, 150, 150);
		
		private readonly Card[] m_cards;
		
		private Straight(Card[] cards)
		{
			m_cards = cards;
		}
		
		public static Straight Create(Card[] cards)
		{
			for (int o = 2; o >= 0; o--)
			{
				Card[] straight = IterateStraight(cards, o).Take(5).ToArray();
				if (straight.Length == 5)
				{
					return new Straight(straight);
				}
			}
			
			return null;
		}
		
		public int CompareSame(Straight other)
		{
			return m_cards[0].Rank.CompareTo(other.m_cards[0].Rank);
		}
	}
}
