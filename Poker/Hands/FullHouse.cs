using System.Collections.Generic;
using System.Linq;

namespace Poker.Hands
{
	public class FullHouse : Hand
	{
		public override int Rank => 6;
		public override string Name => "Full House";
		public override Color Color => new Color(0.5f, 1.0f, 0.7f);
		
		public override IEnumerable<Card> Cards
		{
			get
			{
				yield return m_set30;
				yield return m_set31;
				yield return m_set32;
				yield return m_set20;
				yield return m_set21;
			}
		}
		
		private readonly Card m_set30;
		private readonly Card m_set31;
		private readonly Card m_set32;
		private readonly Card m_set20;
		private readonly Card m_set21;
		
		private FullHouse(Card[] cards, int set3Begin, int set2Begin)
		{
			m_set30 = cards[set3Begin + 0];
			m_set31 = cards[set3Begin + 1];
			m_set32 = cards[set3Begin + 2];
			m_set20 = cards[set2Begin + 0];
			m_set21 = cards[set2Begin + 1];
		}
		
		public static FullHouse Create(Card[] cards)
		{
			int[] set3 = IterateSets(cards, 3).ToArray();
			if (set3.Length == 0)
				return null;
			
			if (set3.Length == 2)
				return new FullHouse(cards, set3[0], set3[1]);
			
			int set2Begin = IterateSets(cards, 2).DefaultIfEmpty(-1).First();
			if (set2Begin == -1)
				return null;
			
			return new FullHouse(cards, set3[0], set2Begin);
		}
		
		public int CompareSame(FullHouse other)
		{
			int result = m_set30.Rank.CompareTo(other.m_set30.Rank);
			if (result == 0)
				result = m_set20.Rank.CompareTo(other.m_set20.Rank);
			return result;
		}
	}
}
