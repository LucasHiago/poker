using System;
using System.Linq;
using System.Collections.Generic;

namespace Poker.Net
{
	public delegate void TurnChangeEventHandler(IClient newCurrentClient);
	public delegate void FlopEventHandler(Card flop1, Card flop2, Card flop3);
	public delegate void TurnEventHandler(Card card);
	public delegate void DealEventHandler(Card pocketCard1, Card pocketCard2);
	public delegate void PlayerActionEventHandler(ushort clientId, TurnEndAction action, int raiseAmount);
	
	public abstract class Connection
	{
		public abstract ushort SelfClientId { get; }
		public abstract IEnumerable<IClient> Clients { get; }
		public abstract IList<IClient> TurnOrderClients { get; }
		public abstract bool Closed { get; }
		public abstract bool Connecting { get; }
		public abstract bool WaitingForNetwork { get; }
		public abstract IClient CurrentClient { get; }
		
		public bool HasDealtPocketCards { get; protected set; }
		public int CommunityCardsRevealed { get; protected set; }
		
		public bool InGame { get; protected set; }
		public int BigBlind { get; protected set; }
		public int FirstPlayerIndex { get; protected set; }
		public bool IsOwnTurn => CurrentClient.ClientId == SelfClientId;
		
		public readonly Card[] PocketCards = new Card[2];
		public readonly Card[] CommunityCards = new Card[5];
		
		public GameDriver GameDriver { get; protected set; }
		public Player[] Players => GameDriver.Players;
		public int CallAmount => GameDriver.GetCallAmount(GameDriver.CurrentPlayer);
		public int MinimumRaise => GameDriver.MinimumRaise;
		
		public event TurnChangeEventHandler OnTurnChanged;
		public event Action OnGameStart;
		public event PlayerActionEventHandler OnPlayerAction;
		public event DealEventHandler OnDeal;
		public event FlopEventHandler OnFlop;
		public event TurnEventHandler OnTurn;
		public event TurnEventHandler OnRiver;
		
		public Player GetPlayer(ushort playerID)
		{
			return GameDriver.GetPlayer(playerID);
		}
		
		public abstract void Disconnect();
		
		public abstract void Fold();
		public abstract void Call();
		public abstract void Raise(int amount);
		public abstract void AllIn();
		
		public abstract void NextHand();
		
		protected void StepFirstPlayerIndex()
		{
			do
			{
				FirstPlayerIndex = (FirstPlayerIndex + 1) % TurnOrderClients.Count;
			}
			while (GameDriver.GetPlayer(TurnOrderClients[FirstPlayerIndex].ClientId).Bust);
		}
		
		protected void RaiseTurnChanged()
		{
			OnTurnChanged?.Invoke(CurrentClient);
		}
		
		protected void RaiseOnGameStart()
		{
			OnGameStart?.Invoke();
		}
		
		protected void RaiseOnPlayerAction(ushort clientId, TurnEndAction action, int raiseAmount)
		{
			OnPlayerAction?.Invoke(clientId, action, raiseAmount);
		}
		
		protected void RaiseFlopEvent(Card flop1, Card flop2, Card flop3)
		{
			OnFlop?.Invoke(flop1, flop2, flop3);
		}
		
		protected void RaiseTurnEvent(Card card)
		{
			OnTurn?.Invoke(card);
		}
		
		protected void RaiseRiverEvent(Card card)
		{
			OnRiver?.Invoke(card);
		}
		
		protected void RaiseDealEvent(Card card1, Card card2)
		{
			OnDeal?.Invoke(card1, card2);
		}
		
		public IClient GetClientById(ushort id)
		{
			return Clients.FirstOrDefault(client => client.ClientId == id);
		}
	}
}
