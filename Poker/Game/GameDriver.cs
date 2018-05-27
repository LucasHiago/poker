using System;
using System.Collections.Generic;
using System.Linq;

namespace Poker
{
	public class GameDriver
	{
		public readonly Player[] Players;
		
		public int FirstPlayerIndex { get; private set; }
		public int CurrentPlayerIndex { get; private set; }
		
		public Player CurrentPlayer => Players[(FirstPlayerIndex + CurrentPlayerIndex) % Players.Length];
		
		public int PotSize => Players.Select(player => player.ContributionAmount).Sum();
		
		public readonly int BigBlind;
		
		public int CurrentBet => m_targetPotSizes.Sum();
		public int MinimumRaise { get; private set; }
		
		public HandStage Stage { get; private set; }
		
		public delegate void StageChangedEventHandler(HandStage newStage);
		public event StageChangedEventHandler OnStageChanged;
		
		private int m_lastRaiseIndex;
		private bool m_anyAllIn;
		
		private readonly List<int> m_targetPotSizes = new List<int>();
		
		public GameDriver(IEnumerable<ushort> clientIds, int startChips, int bigBlind)
		{
			BigBlind = bigBlind;
			Players = clientIds.Select(id => new Player(id, startChips)).ToArray();
		}
		
		public Player GetPlayer(ushort id)
		{
			return Players.First(p => p.ClientId == id);
		}
		
		public int GetPotSize(int pot)
		{
			int size = 0;
			for (int i = 0; i < Players.Length; i++)
			{
				if (pot < Players[i].ContributionAmounts.Count)
					size += Players[i].ContributionAmounts[pot];
			}
			return size;
		}
		
		private void IncPlayerIndex()
		{
			CurrentPlayerIndex = (CurrentPlayerIndex + 1) % Players.Length;
		}
		
		private void StepTurn()
		{
			if (Players.All(player => player.AllIn))
			{
				Stage = HandStage.End;
				OnStageChanged?.Invoke(Stage);
				return;
			}
			
			bool incrementStage = false;
			int prevPlayerIndex = CurrentPlayerIndex;
			
			int numNotFolded = Players.Count(p => !p.Bust && !p.HasFolded);
			if (numNotFolded <= 1)
			{
				//All other players have folded
				Stage = HandStage.End;
				OnStageChanged?.Invoke(Stage);
				return;
			}
			
			do
			{
				IncPlayerIndex();
				if (CurrentPlayerIndex == m_lastRaiseIndex)
					incrementStage = true;
			}
			//Skip players that have folded or have gone all in.
			while (CurrentPlayer.HasFolded || CurrentPlayer.AllIn);
			
			if (CurrentPlayerIndex == prevPlayerIndex)
			{
				//All other players must have folded
				Stage = HandStage.End;
				OnStageChanged?.Invoke(Stage);
			}
			else if (incrementStage)
			{
				CurrentPlayerIndex = 0;
				while (CurrentPlayer.HasFolded || CurrentPlayer.AllIn)
					IncPlayerIndex();
				
				m_lastRaiseIndex = CurrentPlayerIndex;
				Stage++;
				
				Log.Write("Game stage changed to " + Stage + ".");
				OnStageChanged?.Invoke(Stage);
			}
		}
		
		public int GetCallAmount(Player player)
		{
			return Math.Min(player.Chips, CurrentBet - player.ContributionAmount);
		}
		
		private void AddChipsToPots(Player player, ref int chips)
		{
			for (int i = 0; i < m_targetPotSizes.Count && chips > 0; i++)
			{
				int space = m_targetPotSizes[i] - player.ContributionAmounts[i];
				int numToAdd = Math.Min(space, chips);
				player.ContributionAmounts[i] += numToAdd;
				chips -= numToAdd;
			}
		}
		
		public void Call()
		{
			Player player = CurrentPlayer;
			
			int callAmount = GetCallAmount(player);
			player.Chips -= callAmount;
			AddChipsToPots(player, ref callAmount);
			
			StepTurn();
		}
		
		private void GenericRaise(int amount)
		{
			MinimumRaise = Math.Max(MinimumRaise, amount);
			m_lastRaiseIndex = CurrentPlayerIndex;
			
			Player player = CurrentPlayer;
			int currentPot = m_targetPotSizes.Count - 1;
			
			int chipsToAdd = GetCallAmount(player) + amount;
			player.Chips -= chipsToAdd;
			
			AddChipsToPots(CurrentPlayer, ref chipsToAdd);
			
			if (chipsToAdd > 0)
			{
				if (m_anyAllIn)
				{
					//Creates a side pot containing the extra chips
					m_targetPotSizes.Add(chipsToAdd);
					for (int i = 0; i < Players.Length; i++)
					{
						Players[i].ContributionAmounts.Add(Players[i] == player ? chipsToAdd : 0);
					}
				}
				else
				{
					m_targetPotSizes[currentPot] += chipsToAdd;
					player.ContributionAmounts[currentPot] += chipsToAdd;
				}
			}
		}
		
		public void AllIn()
		{
			Player player = CurrentPlayer;
			
			int callAmount = CurrentBet - player.ContributionAmount;
			int raise = player.Chips - callAmount;
			int currentPot = m_targetPotSizes.Count - 1;
			
			if (raise < 0)
			{
				//This player doesn't have enough chips to call the all in, so create side pots.
				
				int totalContrib = player.ContributionAmount + player.Chips;
				
				//Counts how many pots the player can fill
				int filledPots = 0;
				int potSizeAcc = 0;
				for (; filledPots < m_targetPotSizes.Count; filledPots++)
				{
					potSizeAcc += m_targetPotSizes[filledPots];
					if (potSizeAcc >= totalContrib)
						break;
					player.ContributionAmounts[filledPots] = m_targetPotSizes[filledPots];
				}
				
				int missing = potSizeAcc - totalContrib; //The amount of chips missing to fill up the current pot
				int lPotSize = m_targetPotSizes[filledPots] - missing;
				player.ContributionAmounts[filledPots] = lPotSize;
				if (missing > 0)
				{
					for (int i = 0; i < Players.Length; i++)
					{
						if (Players[i].Bust || Players[i].HasFolded)
							continue;
						int delta = Math.Max(Players[i].ContributionAmounts[currentPot] - lPotSize, 0);
						Players[i].ContributionAmounts.Insert(filledPots + 1, delta);
						Players[i].ContributionAmounts[filledPots] -= delta;
					}
				}
				
				m_targetPotSizes[filledPots] = lPotSize;
				m_targetPotSizes.Insert(filledPots + 1, missing);
			}
			else
			{
				GenericRaise(raise);
			}
			
			m_anyAllIn = true;
			player.AllIn = true;
			player.Chips = 0;
			StepTurn();
		}
		
		public void Raise(int amount)
		{
			if (amount < MinimumRaise)
				throw new InvalidOperationException("Raise too small.");
			
			GenericRaise(amount);
			StepTurn();
		}
		
		public void Fold()
		{
			CurrentPlayer.HasFolded = true;
			StepTurn();
		}
		
		public void StartHand(ushort firstPlayerClientId)
		{
			for (int i = 0; i < Players.Length; i++)
			{
				Players[i].StartHand();
				if (Players[i].ClientId == firstPlayerClientId)
					FirstPlayerIndex = i;
			}
			
			MinimumRaise = BigBlind;
			Stage = HandStage.PreFlop;
			m_lastRaiseIndex = 0;
			CurrentPlayerIndex = 0;
			m_anyAllIn = false;
			
			m_targetPotSizes.Clear();
			m_targetPotSizes.Add(BigBlind);
		}
	}
}
