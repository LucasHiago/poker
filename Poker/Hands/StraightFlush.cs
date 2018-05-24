using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Poker.Hands
{
	public class StraightFlush : Hand
	{
		public override int Rank => 8;
		public override string Name => "Straight Flush";
		public override Color Color => new Color(255, 233, 166);
		
		public override IEnumerable<Card> Cards
		{
			get
			{
				for (int i = 0; i < 5; i++)
				{
					yield return new Card(m_startingCard.Suit, m_startingCard.Rank + i);
				}
			}
		}
		
		private Card m_startingCard;
		
		private StraightFlush(Card startingCard)
		{
			m_startingCard = startingCard;
		}
		
		public static StraightFlush Create(Card[] cards)
		{
			for (int o = 2; o >= 0; o--)
			{
				Card[] straight = IterateStraight(cards, o).Take(5).ToArray();
				if (straight.Length == 5 && straight.Skip(1).All(card => card.Suit == straight[0].Suit))
				{
					return new StraightFlush(straight[0]);
				}
			}
			
			return null;
		}
		
		public int CompareSame(StraightFlush other)
		{
			return m_startingCard.Rank.CompareTo(other.m_startingCard.Rank);
		}
	}
}
