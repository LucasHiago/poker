using System.Collections.Generic;
using System.Linq;

namespace Poker.Hands
{
	public class Pair : Hand
	{
		public override int Rank => 1;
		public override string Name => "Pair";
		public override IEnumerable<Card> Cards => m_cards;
		public override Color Color => new Color(0.8f, 0.8f, 1.0f);
		
		private readonly Card[] m_cards;
		
		private Pair(Card[] cards)
		{
			m_cards = cards;
		}
		
		public static Pair Create(Card[] cards)
		{
			int index = IterateSets(cards, 2).DefaultIfEmpty(-1).First();
			if (index == -1)
				return null;
			
			Card[] kickers = SelectKickers(cards, new[] {index}, 2).Take(3).ToArray();
			
			return new Pair(new[] { cards[index], cards[index + 1], kickers[0], kickers[1], kickers[2] });
		}
		
		public int CompareSame(Pair other)
		{
			for (int i = 1; i < 5; i++)
			{
				int result = m_cards[i].Rank.CompareTo(other.m_cards[i].Rank);
				if (result != 0)
					return result;
			}
			return 0;
		}
	}
}
