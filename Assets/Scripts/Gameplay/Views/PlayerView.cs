﻿using System.Linq;
using AurumGames.CompositeRoot;
using Cysharp.Threading.Tasks;
using Poker.Gameplay.Core;
using Poker.Gameplay.Core.Models;
using Poker.Gameplay.Core.States;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Poker.Gameplay.Views
{
	public partial class PlayerView : LazyMonoBehaviour
	{
		public PlayerState PlayerState => _player;
		
		[SerializeField] private TextMeshPro _balance;
		[SerializeField] private TextMeshPro _name;
		[SerializeField] private CardView _card1;
		[SerializeField] private CardView _card2;
		[SerializeField] private StatusView Status;
		[SerializeField] private BetView _bet;
		
		[Dependency] private GameManager _gameManager;

		private bool IsMe => _gameManager.State.Me == _player;
		private TableState TableState => _gameManager.State.Table;
		
		private PlayerState _player;
		private bool _revealCards;
		private VotingResponse? _votingResponse;
		
		protected override void InitInnerState()
		{
			
		}

		protected override void Initialized()
		{
			RoundController roundController = _gameManager.Controller.Round;
			
			TableState.NewVoterAssigned += NewVoterAssigned;
			TableState.NewCardRevealed += NewCardRevealed;
			roundController.RoundStarted += RoundStarted;
			roundController.Voting.VotingEnded += VotingEnded;
		}

		private void NewCardRevealed()
		{
			_votingResponse = null;
			DataChanged();
		}

		private void VotingEnded()
		{
			_votingResponse = null;
			DataChanged();
		}

		private void RoundStarted()
		{
			_revealCards = false;
			_votingResponse = null;
			DataChanged();
		}

		private void NewVoterAssigned()
		{
			DataChanged();
		}

		public void BindTo(PlayerState player)
		{
			_player = player;
			_player.DataChanged += DataChanged;
			gameObject.SetActive(true);
			
			DataChanged();
		}

		public void Hide()
		{
			gameObject.SetActive(false);
		}

		public async UniTask MakeChoiceAnimation(VotingResponse response)
		{
			_votingResponse = response;
			DataChanged();
			await UniTask.Delay(500);
		}

		public async UniTask RevealCardsRoundEndAnimation()
		{
			_revealCards = true;
			DataChanged();
			await UniTask.WhenAll(
				UniTask.Delay(100).ContinueWith(_card1.RevealAnimation),
				_card2.RevealAnimation()
			);
		}
		
		public async UniTask HideCardsRoundEndAnimation()
		{
			_votingResponse = null;
			DataChanged();
			await UniTask.WhenAll(
				_card1.HideAnimation(),
				_card2.HideAnimation()
			);
		}

		public async UniTask DealCardsAnimation()
		{
			DataChanged();
			await UniTask.WhenAll(
				_card1.ShowAnimation(),
				_card2.ShowAnimation()
			);
		}

		private void DataChanged()
		{
			var shouldShow = IsMe || _revealCards;
			_card1.Revealed = shouldShow;
			_card2.Revealed = shouldShow;

			if (_votingResponse.HasValue)
			{
				Status.SetText(_votingResponse.Value.Action.ToString());
				Status.Show();
			}
			else if (shouldShow && TableState.CardsRevealed > 0 && TableState.RoundEnded == false)
			{
				Status.SetText(new Combination(_player.Cards, TableState.Cards.Take(TableState.CardsRevealed)).Name);
				Status.Show();
			}
			else
			{
				Status.Hide();
			}
			
			if (_player.IsOutOfPlay)
			{
				_name.text = "Out";
			}
			else
			{
				_name.text = _player.Name;
			}
			_balance.text = $"${_player.Balance}";
			_card1.Bind(_player.Cards[0]);
			_card2.Bind(_player.Cards[1]);

			if (_player.Bet == 0 || TableState.IsVoting == false)
			{
				_bet.Hide();
			}
			else
			{
				_bet.SetBet(_player.Bet);
				_bet.Show();
			}

			if (TableState.Winner == _player)
			{
				_name.color = Color.yellow;
			}
			else if (TableState.IsVoting && TableState.Voter == _player)
			{
				_name.color = Color.green;
			}
			else if (_player.Folded || _player.IsOutOfPlay)
			{
				_name.color = Color.red;
				if (IsMe == false)
				{
					_card1.Hide();
					_card2.Hide();
				}
			}
			else
			{
				_name.color = Color.white;
			}
		}
	}
}