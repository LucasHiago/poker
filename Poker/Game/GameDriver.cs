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
				CurrentPlayerIndex = (CurrentPlayerIndex + 1) % Players.Length;
				
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
				m_lastRaiseIndex = 0;
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
		
		public void AllIn()
		{
			Player player = CurrentPlayer;
			
			int callAmount = GetCallAmount(player);
			int raise = player.Chips - callAmount;
			int currentPot = m_targetPotSizes.Count - 1;
			
			if (raise < 0)
			{
				//This player doesn't have enough chips to call the all in, so create side pots.
				
				int missing = -raise;
				
				for (int i = 0; i < Players.Length; i++)
				{
					int delta = Math.Min(missing, Players[i].ContributionAmounts[currentPot]);
					Players[i].ContributionAmounts[currentPot] -= delta;
					Players[i].ContributionAmounts.Add(delta);
				}
				
				m_targetPotSizes[currentPot] -= missing;
				m_targetPotSizes.Add(missing);
			}
			else
			{
				MinimumRaise = Math.Max(MinimumRaise, raise);
				m_lastRaiseIndex = CurrentPlayerIndex;
				
				int chipsToAdd = player.Chips;
				AddChipsToPots(player, ref chipsToAdd);
				
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
			
			m_anyAllIn = true;
			player.AllIn = true;
			player.Chips = 0;
			StepTurn();
		}
		
		public void Raise(int amount)
		{
			if (amount < MinimumRaise)
				throw new InvalidOperationException("Raise too small.");
			
			m_targetPotSizes[m_targetPotSizes.Count - 1] += amount;
			MinimumRaise = amount;
			m_lastRaiseIndex = CurrentPlayerIndex;
			
			Call();
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
			m_lastRaiseIndex = FirstPlayerIndex;
			m_anyAllIn = false;
			
			m_targetPotSizes.Clear();
			m_targetPotSizes.Add(BigBlind);
		}
	}
}
