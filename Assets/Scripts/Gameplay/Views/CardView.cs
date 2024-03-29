﻿using System;
using System.Collections.Generic;
using System.Threading;
using AurumGames.Animation;
using AurumGames.Animation.Tracks;
using AurumGames.CompositeRoot;
using Cysharp.Threading.Tasks;
using Poker.Gameplay.Configuration;
using Poker.Gameplay.Core;
using Poker.Gameplay.Core.Models;
using TMPro;
using UnityEngine;

namespace Poker.Gameplay.Views
{
	public partial class CardView : LazyMonoBehaviour
	{
		public bool Revealed
		{
			set
			{
				_reveal = value;
				UpdateCardTexture();
			}
			get => _reveal;
		}
		
		[SerializeField] private Material _faceReference;
		[SerializeField] private Material _coverReference;
		[SerializeField] private MeshRenderer _meshRenderer;
		[SerializeField] private Texture2D _card;
		[SerializeField] private bool _reveal;

		public TextMeshPro DebugText;

		[Dependency] private CardsDatabase _cardsDatabase;
		[Dependency] private GameManager _gameManager;

		private Material _face;

		private AnimationPlayer _revealPlayer;
		private StatedAnimationPlayer<Visibility> _visibilityPlayer;
		
		protected override void InitInnerState()
		{
			_face = Instantiate(_faceReference);

			Transform self = transform;

			Vector3 position = self.localPosition;

			_revealPlayer = new AnimationPlayer(this, new ITrack[]
			{
				new LocalEulerAnglesTrack(self, new []
				{
					new KeyFrame<Vector3>(0, new Vector3(0, 0, 180), Easing.QuadOut),
					new KeyFrame<Vector3>(600, new Vector3(0, 0, 360), Easing.QuadOut)
				}),
				new LocalPositionTrack(self, new []
				{
					new KeyFrame<Vector3>(0, new Vector3(position.x, 0, position.z), Easing.QuintOut),
					new KeyFrame<Vector3>(350, new Vector3(position.x, 2, position.z), Easing.QuintIn),
					new KeyFrame<Vector3>(700, new Vector3(position.x, 0, position.z), Easing.QuintIn),
				}),
				new TriggerTrack(new []
				{
					new TriggerKeyFrame(50, () =>
					{
						Revealed = true;
					})
				})
			});

			Vector3 scale = self.localScale;
			
			var show = new TracksEvaluator(new ITrack[]
			{
				new ScaleTrack(self, new []
				{
					new KeyFrame<Vector3>(0, Vector3.zero, Easing.QuintOut),
					new KeyFrame<Vector3>(400, scale, Easing.QuintOut),
				})
			});

			var hide = new TracksEvaluator(new ITrack[]
			{
				new ScaleTrack(self, new []
				{
					new KeyFrame<Vector3>(0, scale, Easing.CubicIn),
					new KeyFrame<Vector3>(400, Vector3.zero, Easing.QuintOut),
				})
			});

			_visibilityPlayer = new StatedAnimationPlayer<Visibility>(this, new Dictionary<Visibility, TracksEvaluator>()
			{
				{ Visibility.Visible, show },
				{ Visibility.Hidden, hide },
			});
			_visibilityPlayer.SetStateInstant(Visibility.Hidden);
			
			UpdateCardTexture();
		}

		protected override void Initialized()
		{
			_visibilityPlayer.TimeSource = _gameManager.TimeSource;
			_revealPlayer.TimeSource = _gameManager.TimeSource;
		}

		public async UniTask RevealAnimation(CancellationToken cancellationToken)
		{
			if (_gameManager.IsPlaying == false)
				return;
			
			_revealPlayer.PlayFromStart();

			await UniTask.WaitWhile(() => _revealPlayer.IsPlaying, cancellationToken: cancellationToken);
		}

		public async UniTask ShowAnimation(CancellationToken cancellationToken)
		{
			if (_gameManager.IsPlaying == false)
				return;
			
			if (_visibilityPlayer.CurrentState == Visibility.Visible)
				return;
			
			_visibilityPlayer.SetState(Visibility.Visible);
			await UniTask.WaitWhile(() => _visibilityPlayer.IsPlaying, cancellationToken: cancellationToken);
		}
		
		public async UniTask HideAnimation(CancellationToken cancellationToken)
		{
			if (_gameManager.IsPlaying == false)
				return;
			
			if (_visibilityPlayer.CurrentState == Visibility.Hidden)
				return;
			
			_visibilityPlayer.SetState(Visibility.Hidden);
			await UniTask.WaitWhile(() => _visibilityPlayer.IsPlaying, cancellationToken: cancellationToken);
		}

		public void Bind(CardModel model)
		{
			DebugText.text = $"{model.Type}\n<size=150%>{model.Value}";
			_card = _cardsDatabase.GetTexture(model);
			
			UpdateCardTexture();
		}

		private void UpdateCardTexture()
		{
			Material material = _reveal ? _face : _coverReference;
			
			if (_reveal)
			{
				_face.mainTexture = _card;
			}
			
			var materials = _meshRenderer.sharedMaterials;
			materials[0] = material;
			_meshRenderer.sharedMaterials = materials;
		}
		
		public void Show()
		{
			if (_visibilityPlayer.CurrentState == Visibility.Hidden)
				_visibilityPlayer.SetState(Visibility.Visible);
		}

		public void Hide()
		{
			if (_visibilityPlayer.CurrentState == Visibility.Visible)
				_visibilityPlayer.SetState(Visibility.Hidden);
		}
		
#if UNITY_EDITOR
		[EasyButtons.Button]
		private void OnValidate()
		{
			if (_face == _faceReference || _face == null)
			{
				_face = Instantiate(_faceReference);
			}
			
			UpdateCardTexture();
		}
#endif
	}
}