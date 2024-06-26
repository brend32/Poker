﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AurumGames.CompositeRoot;
using Cysharp.Threading.Tasks;
using Poker.Gameplay.Core;
using Poker.Gameplay.Core.Models;
using Poker.Gameplay.Core.States;
using TMPro;
using UnityEngine;

namespace Poker.Gameplay.Views
{
	public partial class PlayersView : LazyMonoBehaviour
	{
		[SerializeField] private PlayerView[] _others;
		[SerializeField] private PlayerView _me;
		
		[Dependency] private GameManager _gameManager;

		private readonly Dictionary<PlayerState, PlayerView> _map = new();
		private int _otherPlayersCount;
		
		protected override void InitInnerState()
		{
			
		}

		protected override void Initialized()
		{
			
		}

		public void Bind()
		{
			_map.Clear();
			
			PlayerState me = _gameManager.State.Me;
			if (me != null)
			{
				_map.Add(me, _me);
				_me.BindTo(me);
			}
			else
			{
				_me.Hide();
			}
			var players = _gameManager.State.Players.Where(player => player != me).ToArray();

			if (players.Length > _others.Length)
				throw new Exception($"Not enough slots; Need: {players.Length}, Has: {_others.Length}");

			_otherPlayersCount = players.Length;
			
			for (int i = 0; i < _others.Length; i++)
			{
				PlayerView view = _others[i];
				if (i < players.Length)
				{
					PlayerState state = players[i];
					_map.Add(state, view);
					view.BindTo(state);
				}
				else
				{
					view.Hide();
				}
			}
		}

		public async UniTask RevealOthersCardsRoundEndAnimation(CancellationToken cancellationToken)
		{
			if (_gameManager.IsPlaying == false)
				return;
			
			for (var i = 0; i < _otherPlayersCount; i++)
			{
				PlayerView playerView = _others[i];
				PlayerState state = playerView.PlayerState;
				if (state.Folded || state.IsOutOfPlay)
					continue;

				await playerView.RevealCardsRoundEndAnimation(cancellationToken);
				await _gameManager.DelayAsync(500, cancellationToken);
			}
		}
		
		public async UniTask DealCardsAnimation(CancellationToken cancellationToken)
		{
			if (_gameManager.IsPlaying == false)
				return;
			
			await Deal(_me);
				
			for (var i = 0; i < _otherPlayersCount; i++)
			{
				if (_gameManager.IsPlaying == false)
					return;
				
				await Deal(_others[i]);
			}
			
			return;

			UniTask Deal(PlayerView playerView)
			{
				PlayerState state = playerView.PlayerState;
				if (state.Folded || state.IsOutOfPlay)
					return UniTask.CompletedTask;
				
				return playerView.DealCardsAnimation(cancellationToken).ContinueWith(() => _gameManager.DelayAsync(50, cancellationToken));
			}
		}
		
		public async UniTask HideCardsRoundEndAnimation(CancellationToken cancellationToken)
		{
			if (_gameManager.IsPlaying == false)
				return;
			
			List<UniTask> tasks = new();
			
			Add(_me);
				
			for (var i = 0; i < _otherPlayersCount; i++)
			{
				Add(_others[i]);
			}

			await UniTask.WhenAll(tasks);
			return;

			void Add(PlayerView playerView)
			{
				if (_gameManager.IsPlaying == false)
					return;
				
				PlayerState state = playerView.PlayerState;
				if (state.Folded || state.IsOutOfPlay)
					return;
				
				tasks.Add(playerView.HideCardsRoundEndAnimation(cancellationToken));
			}
		}

		public async UniTask MakeChoiceAnimation(PlayerState player, VotingResponse response, CancellationToken cancellationToken)
		{
			if (_gameManager.IsPlaying == false)
				return;
			
			await _map[player].MakeChoiceAnimation(response, cancellationToken);
		}
	}
}