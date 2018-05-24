using System.Collections.Generic;
using System.Linq;

namespace Poker.Hands
{
	public class ThreeOfAKind : Hand
	{
		public override int Rank => 3;
		public override string Name => "Three of a Kind";
		public override Color Color => new Color(148, 244, 255);
		
		public override IEnumerable<Card> Cards
		{
			get
			{
				foreach (Suits suit in m_suits)
					yield return new Card(suit, m_rank);
				foreach (Card kicker in m_kickers)
					yield return kicker;
			}
		}
		
		private readonly int m_rank;
		private readonly Suits[] m_suits;
		
		//Kicker 1 is the highest rank kicker
		private readonly Card[] m_kickers;
		
		private ThreeOfAKind(int rank, Suits[] suits, Card[] kickers)
		{
			m_rank = rank;
			m_suits = suits;
			m_kickers = kickers;
		}
		
		public static ThreeOfAKind Create(Card[] cards)
		{
			int index = IterateSets(cards, 3).DefaultIfEmpty(-1).First();
			if (index == -1)
				return null;
			
			Card[] kickers = SelectKickers(cards, new[] {index}, 3).Take(2).ToArray();
			Suits[] suits = cards.Skip(index).Take(3).Select(card => card.Suit).ToArray();
			
			return new ThreeOfAKind(cards[index].Rank, suits, kickers);
		}
		
		public int CompareSame(ThreeOfAKind other)
		{
			int result = m_rank.CompareTo(other.m_rank);
			for (int i = 0; i < m_kickers.Length && result == 0; i++)
				result = m_kickers[i].Rank.CompareTo(other.m_kickers[i].Rank);
			return result;
		}
	}
}
