using System;
using System.Text;

namespace Poker
{
	public struct Card : IComparable<Card>
	{
		public const int RANK_2 = 0;
		public const int RANK_3 = 1;
		public const int RANK_4 = 2;
		public const int RANK_5 = 3;
		public const int RANK_6 = 4;
		public const int RANK_7 = 5;
		public const int RANK_8 = 6;
		public const int RANK_9 = 7;
		public const int RANK_10 = 8;
		public const int RANK_JACK = 9;
		public const int RANK_QUEEN = 10;
		public const int RANK_KING = 11;
		public const int RANK_ACE = 12;
		
		public readonly Suits Suit;
		public readonly int Rank;
		
		public byte PackedValue => (byte)((int)Suit + Rank * 4);
		
		public Card(byte packedValue)
		{
			Suit = (Suits)(packedValue % 4);
			Rank = packedValue / 4;
		}
		
		public Card(Suits suit, int rank)
		{
			Suit = suit;
			Rank = rank;
		}
		
		public override string ToString()
		{
			switch (Rank)
			{
				case RANK_JACK: return "Jack of " + Suit.ToString();
				case RANK_QUEEN: return "Queen of " + Suit.ToString();
				case RANK_KING: return "King of " + Suit.ToString();
				case RANK_ACE: return "Ace of " + Suit.ToString();
				default: return string.Format("{0} of {1}", Rank + 2, Suit);
			}
		}
		
		public int CompareTo(Card other)
		{
			return Rank.CompareTo(other.Rank);
		}
	}
}
