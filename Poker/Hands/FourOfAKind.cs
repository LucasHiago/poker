using System.Collections.Generic;
using System.Linq;

namespace Poker.Hands
{
	public class FourOfAKind : Hand
	{
		public override int Rank => 7;
		public override string Name => "Four of a Kind";
		public override Color Color => new Color(255, 148, 251);
		
		public override IEnumerable<Card> Cards
		{
			get
			{
				for (int s = 0; s < 4; s++)
					yield return new Card((Suits)s, m_rank);
				yield return m_kicker;
			}
		}
		
		private readonly int m_rank;
		private readonly Card m_kicker;
		
		private FourOfAKind(int rank, Card kicker)
		{
			m_rank = rank;
			m_kicker = kicker;
		}
		
		public static FourOfAKind Create(Card[] cards)
		{
			int index = IterateSets(cards, 4).DefaultIfEmpty(-1).First();
			if (index == -1)
				return null;
			
			Card kicker = SelectKickers(cards, new[] {index}, 4).First();
			return new FourOfAKind(cards[index].Rank, kicker);
		}
		
		public int CompareSame(FourOfAKind other)
		{
			int result = m_rank.CompareTo(other.m_rank);
			if (result == 0)
				result = m_kicker.Rank.CompareTo(other.m_kicker.Rank);
			return result;
		}
	}
}
