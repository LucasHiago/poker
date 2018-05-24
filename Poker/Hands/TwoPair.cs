using System.Collections.Generic;
using System.Linq;

namespace Poker.Hands
{
	public class TwoPair : Hand
	{
		public override int Rank => 2;
		public override string Name => "Two Pair";
		public override IEnumerable<Card> Cards => m_cards;
		public override Color Color => new Color(0.5f, 0.7f, 1.0f);
		
		private readonly Card[] m_cards;
		
		private TwoPair(Card[] cards)
		{
			m_cards = cards;
		}
		
		public static TwoPair Create(Card[] cards)
		{
			int[] pairs = IterateSets(cards, 2).Take(2).ToArray();
			if (pairs.Length != 2)
				return null;
			
			Card kicker = SelectKickers(cards, pairs, 2).First();
			
			return new TwoPair(new[] { cards[pairs[0]], cards[pairs[0] + 1], cards[pairs[1]], cards[pairs[1] + 1], kicker });
		}
		
		private static readonly int[] COMPARE_INDICES = { 0, 2, 4 };
		
		public int CompareSame(TwoPair other)
		{
			foreach (int index in COMPARE_INDICES)
			{
				int result = m_cards[index].Rank.CompareTo(other.m_cards[index].Rank);
				if (result != 0)
					return result;
			}
			return 0;
		}
	}
}
