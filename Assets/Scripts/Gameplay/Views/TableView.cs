﻿using AurumGames.CompositeRoot;
using Poker.Gameplay.Core;
using TMPro;
using UnityEngine;

namespace Poker.Gameplay.Views
{
	public partial class TableView : LazyMonoBehaviour
	{
		[SerializeField] private BalanceView _pot;
		
		[Dependency] private GameManager _gameManager;
		
		protected override void InitInnerState()
		{
			
		}

		protected override void Initialized()
		{
			_gameManager.State.Table.PotChanged += PotChanged;
			PotChanged();
		}

		private void PotChanged()
		{
			_pot.SetValue(_gameManager.State.Table.Pot);
		}
	}
}