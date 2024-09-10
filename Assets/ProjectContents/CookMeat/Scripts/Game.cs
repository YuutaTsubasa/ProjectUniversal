using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using R3;
using R3.Triggers;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Yuuta.Core.Utils;
using Timer = Yuuta.Core.Utils.Timer;

namespace Yuuta.CookMeat {
    public class Game : MonoBehaviour
    {
        [Serializable]
        private struct StatusToPersonImageMapping
        {
            public Meat.Status Status;
            public Sprite PersonSprite;
        }
        
        public record Setting(TimeSpan Duration);
        
        [Header("Count Down Page")]
        [SerializeField] private GameObject _countDownPageGameObject;
        [SerializeField] private Text _countDownPageText;
        
        [Header("Main Page")]
        [SerializeField] private Image _meatPlate;
        [SerializeField] private Meat _meat;
        [SerializeField] private RectTransform _meatMovingAreaRectTransform;
        
        [SerializeField] private RectTransform _cookAreaRectTransform;
        [SerializeField] private RectTransform _trashCanRectTransform;
        [SerializeField] private RectTransform _personRectTransform;

        [SerializeField] private Text _scoreText;
        [SerializeField] private Text _countDownTimerText;

        [SerializeField] private Image _personImage;
        [SerializeField] private StatusToPersonImageMapping[] _statusToPersonImageMappings;
        private IDictionary<Meat.Status, Sprite> _statusToPersonImageDictionary;

        [SerializeField] private SoundManager _soundManager;
        [SerializeField] private BBQSoundPlayer _bbqSoundPlayer;
        
        [Header("Result Page")]
        [SerializeField] private GameObject _resultPageGameObject;
        [SerializeField] private Text _resultPageScoreText;
        [SerializeField] private Button _retryButton;

        private ReactiveProperty<int> _score = new(0);

        private void _UpdateTime(float time)
        {
            _countDownTimerText.text = $"Time: {Mathf.FloorToInt(time / 60):00}:{Mathf.FloorToInt(time % 60):00}";
        }
        
        public async UniTask Run(
            Setting setting,
            CancellationToken cancellationToken)
        {
            _statusToPersonImageDictionary = _statusToPersonImageMappings
                .ToDictionary(mapping => mapping.Status, mapping => mapping.PersonSprite);
            
            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken).AddTo(this);
            using var _ = _RegisterHudBehaviours();
            _score.Value = 0;
            _UpdateTime((float)setting.Duration.TotalSeconds);
            
            await _RunCountDownPhase(cancellationTokenSource.Token);
            await _RunMainGamePhase(setting.Duration, cancellationTokenSource.Token);
            await _RunResultPhase(cancellationTokenSource.Token);
        }

        private IDisposable _RegisterHudBehaviours()
            => new CompositeDisposable
            {
                _score.Subscribe(score => _scoreText.text = $"Score: {score}")
                    .AddTo(this)
            };

        private async UniTask _RunCountDownPhase(
            CancellationToken cancellationToken)
        {
            _countDownPageGameObject.SetActive(true);
            _countDownPageText.transform.localScale = Vector3.zero;
            _countDownPageText.text = "3";
            _soundManager.PlaySound(SoundManager.SoundType.CountDown);
            await _countDownPageText.transform.DOScale(1.0f, 0.5f).AsyncWaitForCompletion();
            await _countDownPageText.transform.DOScale(0.0f, 0.5f).AsyncWaitForCompletion();
            _countDownPageText.text = "2";
            _soundManager.PlaySound(SoundManager.SoundType.CountDown);
            await _countDownPageText.transform.DOScale(1.0f, 0.5f).AsyncWaitForCompletion();
            await _countDownPageText.transform.DOScale(0.0f, 0.5f).AsyncWaitForCompletion();
            _countDownPageText.text = "1";
            _soundManager.PlaySound(SoundManager.SoundType.CountDown);
            await _countDownPageText.transform.DOScale(1.0f, 0.5f).AsyncWaitForCompletion();
            await _countDownPageText.transform.DOScale(0.0f, 0.5f).AsyncWaitForCompletion();
            _countDownPageText.text = "GO!";
            _soundManager.PlaySound(SoundManager.SoundType.Confirm);
            await _countDownPageText.transform.DOScale(1.0f, 0.5f).AsyncWaitForCompletion();
            await _countDownPageText.transform.DOScale(0.0f, 0.5f).AsyncWaitForCompletion();
            _countDownPageGameObject.SetActive(false);
        }

        private async UniTask _RunMainGamePhase(
            TimeSpan duration,
            CancellationToken cancellationToken)
        {
            var gameTimer = new Timer(duration);

            using var _ = _RegisterGameBehaviours(gameTimer);
            while (!gameTimer.IsTimeUp.Value &&
                   !cancellationToken.IsCancellationRequested)
            {
                _UpdateTime((float)gameTimer.RemainingTime);
                await UniTask.Yield(cancellationToken);
            }
        }

        private IDisposable _RegisterGameBehaviours(Timer gameTimer)
            => new CompositeDisposable()
            {
                _meatPlate.OnPointerDownAsObservable()
                    .Subscribe(pointerEventData =>
                    {
                        var pointerPosition = pointerEventData.position;
                        var meat = Instantiate(_meat, _meatMovingAreaRectTransform);
                        meat.transform.position = pointerPosition;

                        meat.Initialize(
                            rectTransform => rectTransform.Overlaps(_cookAreaRectTransform),
                            rectTransform => rectTransform.Overlaps(_trashCanRectTransform),
                            rectTransform => rectTransform.Overlaps(_personRectTransform),
                            status =>
                            {
                                _personImage.sprite = _statusToPersonImageDictionary[status];
                                switch (status)
                                {
                                    case Meat.Status.Raw:
                                        _score.Value -= 5;
                                        _soundManager.PlaySound(SoundManager.SoundType.Bad);
                                        break;
                                    
                                    case Meat.Status.Cooked:
                                        _score.Value += 10;
                                        _soundManager.PlaySound(SoundManager.SoundType.Confirm);
                                        break;
                                    
                                    case Meat.Status.Burnt:
                                        _score.Value -= 15;
                                        _soundManager.PlaySound(SoundManager.SoundType.Bad);
                                        break;
                                }
                            },
                            () => _bbqSoundPlayer.Play(),
                            () => _bbqSoundPlayer.Stop(),
                            gameTimer.IsTimeUp
                                .Where(isTimeUp => isTimeUp)
                                .AsUnitObservable());
                        
                        // Let the instantiated meat will be release.
                        IDisposable disposable = null;
                        disposable = _meatPlate.OnPointerUpAsObservable()
                            .Subscribe(pointerUpEventData =>
                            {
                                meat.OnPointerUp(pointerUpEventData);
                                disposable?.Dispose();
                            })
                            .AddTo(this);
                    }).AddTo(this)
            };

        private async UniTask _RunResultPhase(
            CancellationToken cancellationToken)
        {
            _soundManager.PlaySound(SoundManager.SoundType.GameOver);
            _resultPageGameObject.SetActive(true);
            _resultPageScoreText.text = $"Score: {_score.Value}";
            await _retryButton.OnClickAsObservable().FirstAsync(cancellationToken: cancellationToken);
            _resultPageGameObject.SetActive(false);
        }
    }
}