using System.Collections.Generic;
using System.Linq;

namespace Poker
{
	public class Player
	{
		public readonly ushort ClientId;
		public int Chips { get; set; }
		public bool HasFolded { get; set; }
		public bool AllIn { get; set; }
		
		public bool Bust => Chips == 0 && !AllIn;
		public bool InGame => !Bust && !HasFolded;
		
		public readonly List<int> ContributionAmounts;
		public int ContributionAmount => ContributionAmounts.Sum();
		
		public Player(ushort clientId, int chips)
		{
			ClientId = clientId;
			Chips = chips;
			ContributionAmounts = new List<int>();
		}
		
		public void StartHand()
		{
			HasFolded = false;
			AllIn = false;
			ContributionAmounts.Clear();
			ContributionAmounts.Add(0);
		}
	}
}
