﻿using System;
using Poker.Gameplay.Core.States;

namespace Poker.Gameplay.Core.Models
{
	public enum VotingAction
	{
		Fold,
		Raise,
		Call
	}
	
	public struct VotingResponse
	{
		public readonly int RaiseAmount;
		public readonly VotingAction Action;

		private VotingResponse(VotingAction action, int raiseAmount = 0)
		{
			Action = action;
			RaiseAmount = raiseAmount;
		}

		public static VotingResponse Fold()
		{
			return new VotingResponse(VotingAction.Fold);
		}
		
		public static VotingResponse Call()
		{
			return new VotingResponse(VotingAction.Call);
		}
		
		public static VotingResponse Raise(int amount)
		{
			if (amount <= 0)
				return Call();
			
			return new VotingResponse(VotingAction.Raise, amount);
		}
	}
	
	public class VotingContext
	{
		public int MinimumBet { get; set; }
		public TableState Table { get; set; }
		public PlayerState Voter => Table.Voter;
	}
}