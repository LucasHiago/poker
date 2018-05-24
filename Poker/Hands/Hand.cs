using System;
using System.Collections.Generic;
using System.Linq;

namespace Poker.Hands
{
	public abstract class Hand : IComparable<Hand>
	{
		public abstract int Rank { get; }
		public abstract string Name { get; }
		public abstract IEnumerable<Card> Cards { get; }
		public virtual Color Color => Color.White;
		
		private static readonly Type[] HAND_TYPES = 
		{
			typeof(StraightFlush),
			typeof(FourOfAKind),
			typeof(FullHouse),
			typeof(Flush),
			typeof(Straight),
			typeof(ThreeOfAKind),
			typeof(TwoPair),
			typeof(Pair),
			typeof(HighCard)
		};
		
		public static Hand CreateBest(Card[] cards)
		{
			Array.Sort(cards);
			
			foreach (Type type in HAND_TYPES)
			{
				Hand hand = (Hand)type.GetMethod("Create").Invoke(null, new object[] {cards});
				if (hand != null)
					return hand;
			}
			
			return null;
		}
		
		public int CompareTo(Hand other)
		{
			int result = Rank.CompareTo(other.Rank);
			if (result == 0)
				result = (int)GetType().GetMethod("CompareSame").Invoke(this, new object[] {other});
			return result;
		}
		
		public override string ToString()
		{
			return string.Format("\"{0}\" {1}", Name, string.Join(", ", Cards));
		}
		
		protected static IEnumerable<Card> SelectKickers(Card[] cards, int[] setIndices, int setSize)
		{
			for (int i = cards.Length - 1; i >= 0; i--)
			{
				if (setIndices.Any(setIndex => i >= setIndex && i < setIndex + setSize))
					continue;
				yield return cards[i];
			}
		}
		
		protected static IEnumerable<Card> IterateStraight(Card[] cards, int startOffset)
		{
			yield return cards[startOffset];
			for (int i = startOffset + 1; i < cards.Length; i++)
			{
				int rankDiff = cards[i].Rank - cards[i - 1].Rank;
				if (rankDiff > 1)
					break;
				if (rankDiff == 1)
					yield return cards[i];
			}
		}
		
		protected static IEnumerable<int> IterateSets(Card[] cards, int size)
		{
			for (int i = cards.Length - 1; i > size - 2; i--)
			{
				if ((i < cards.Length - 1 && cards[i + 1].Rank == cards[i].Rank) || (i >= size && cards[i - size].Rank == cards[i].Rank))
					continue;
				
				bool isSet = true;
				for (int j = 1; j < size; j++)
				{
					if (cards[i - j].Rank != cards[i].Rank)
					{
						isSet = false;
						break;
					}
				}
				
				if (isSet)
				{
					yield return i - size + 1;
				}
			}
		}
	}
}
