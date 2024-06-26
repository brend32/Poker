using System.Collections.Generic;
using AurumGames.Animation;
using AurumGames.Animation.Tracks;
using AurumGames.CompositeRoot;
using AurumGames.SceneManagement;
using Poker.Gameplay.Core;
using Poker.Menu.UI;
using UnityEngine;
using UnityEngine.UI;
using Application = UnityEngine.Device.Application;

namespace Poker.Screens
{
    [SceneInitScript("Menu")]
    public class MenuScreen : PageScript
    {
        [SerializeField] private MenuCardButton[] _buttons;
        [SerializeField] private CanvasGroup _title;
        [SerializeField] private GraphicRaycaster _graphicRaycaster;

        private StatedAnimationPlayer<Visibility> _animation;
        
        protected override void BeforeInit()
        {
            SetupAnimation();
            _animation.SetStateInstant(Visibility.Hidden);
            
            Context.Global.Resolve(this);
            CustomInitializer.StartInitialization(gameObject.scene);
        }

        private void SetupAnimation()
        {
            var from = new Vector2(0, -500);
            var showDelay = 50;
            var cardsShow = new ITrack[_buttons.Length];
            for (int i = 0; i < _buttons.Length; i++)
            {
                cardsShow[i] = new AnchoredPositionTrack(_buttons[i].Transform, new []
                {
                    new KeyFrame<Vector2>(50 * i, from, Easing.OutBack),
                    new KeyFrame<Vector2>(300 + 50 * i, Vector2.zero, Easing.OutBack),
                });
            }
            
            var show = new TracksEvaluator(new ITrack[]
            {
                new TracksEvaluator(cardsShow),
                new CanvasGroupOpacityTrack(_title, FloatTrack.KeyFrames01(new TransitionStruct(300, Easing.QuadOut)))
            });
            
            
            var cardsHide = new ITrack[_buttons.Length];
            for (int i = 0; i < _buttons.Length; i++)
            {
                RectTransform target = _buttons[i].Transform;
                cardsHide[i] = new AnchoredPositionTrack(target, new []
                {
                    new KeyFrame<Vector2>(30 * i, () => target.anchoredPosition, Easing.QuadOut),
                    new KeyFrame<Vector2>(240 + 30 * i, from, Easing.OutBack),
                });
            }
            
            var hide = new TracksEvaluator(new ITrack[]
            {
                new TracksEvaluator(cardsHide),
                new CanvasGroupOpacityTrack(_title, FloatTrack.KeyFrames10(new TransitionStruct(240, Easing.QuadOut)))
            });

            _hidePlayer = new AnimationPlayer(this, new VoidTrack((int)hide.FullDuration));
            _animation = new StatedAnimationPlayer<Visibility>(this, new Dictionary<Visibility, TracksEvaluator>()
            {
                { Visibility.Visible, show },
                { Visibility.Hidden, hide },
            });
            _animation.StateChanged += (previous, current) =>
            {
                if (current == Visibility.Hidden)
                {
                    _graphicRaycaster.enabled = false;
                }
                
                foreach (MenuCardButton button in _buttons)
                {
                    button.CancelAnimation();
                }
            };
            _animation.AnimationEnded += (previous, current) =>
            {
                if (current == Visibility.Visible)
                {
                    _graphicRaycaster.enabled = true;
                }
            };
        }

        protected override void Activated()
        {
            _animation.SetState(Visibility.Visible);
        }

        protected override void Deactivated()
        {
            _animation.SetState(Visibility.Hidden);
        }

        public void PlayAnimation()
        {
            _graphicRaycaster.enabled = false;
            _animation.SetState(Visibility.Visible);
        }

        public void Exit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            Application.Quit();
        }

        public void OpenDifficultyChooseScreen()
        {
            PageSystem.Load<ChooseDifficultyScreen>();
        }
    }
}
