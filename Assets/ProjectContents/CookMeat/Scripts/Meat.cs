using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Yuuta.Core.Utils;

namespace Yuuta.CookMeat
{
    public class Meat : MonoBehaviour, IPointerMoveHandler, IPointerDownHandler, IPointerUpHandler
    {
        public enum Status
        {
            Raw,
            Cooked,
            Burnt,
        }
        
        public enum Side
        {
            Front,
            Back
        }

        [Serializable]
        private struct StatusToMeatImageMapping
        {
            [SerializeField] public Status Status;
            [SerializeField] public Sprite Sprite;
        }

        [SerializeField] private Image _meatImage;
        [SerializeField] private StatusToMeatImageMapping[] _statusToMeatImageMappings;
        
        private bool _isDragging = true;

        private Option<Vector2> _previousPosition = Option<Vector2>.None();
        private Func<RectTransform, bool> _isOnFire;
        private Func<RectTransform, bool> _isOnTrashCan;
        private Func<RectTransform, bool> _isOnPerson;
        private Action _onFire;
        private Action _onLeaveFire;
        private Action<Status> _onPersonEat;

        private ReactiveProperty<Side> _currentSide = new ReactiveProperty<Side>(Side.Back);
        private IDictionary<Side, float> _sideCookedTime = new Dictionary<Side, float>()
        {
            { Side.Front, 0f },
            { Side.Back, 0f },
        };
        private bool _isOnFired = false;
        
        public void Initialize(
            Func<RectTransform, bool> isOnFire,
            Func<RectTransform, bool> isOnTrashCan,
            Func<RectTransform, bool> isOnPerson,
            Action<Status> onPersonEat,
            Action onFire,
            Action onLeaveFire,
            Observable<Unit> onTimeUp)
        {
            _isOnFire = isOnFire;
            _isOnTrashCan = isOnTrashCan;
            _isOnPerson = isOnPerson;
            _onFire = onFire;
            _onLeaveFire = onLeaveFire;
            _onPersonEat = onPersonEat;
            
            var statusToMeatImageDictionary = _statusToMeatImageMappings
                .ToDictionary(mapping => mapping.Status, mapping => mapping.Sprite);
            _currentSide
                .Select(currentSide => 
                    _ConvertCookedTimeToStatus(
                        _sideCookedTime[_GetOtherSide(currentSide)]))
                .Subscribe(status => _meatImage.sprite = statusToMeatImageDictionary[status])
                .AddTo(this);
            _currentSide
                .Subscribe(side => _meatImage.transform.localScale = new Vector3(
                    side switch
                    {
                        Side.Front => 1f,
                        Side.Back => -1f,
                        _ => throw new ArgumentOutOfRangeException(),
                    },
                    1.0f, 
                    1.0f))
                .AddTo(this);
            
            var everyUpdateTime = Observable.EveryUpdate().Select(_ => Time.time);
            everyUpdateTime
                .Zip(everyUpdateTime.Skip(1), (previousTime, currentTime) => currentTime - previousTime)
                .Where(_ => _isOnFired)
                .Subscribe(deltaTime => _sideCookedTime[_currentSide.Value] += deltaTime)
                .AddTo(this);

            onTimeUp.Subscribe(_ => Destroy(gameObject)).AddTo(this);
        }
        
        public void OnPointerMove(PointerEventData eventData)
        {
            if (!_isDragging)
                return;
            
            this.transform.position = eventData.position;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isDragging = true;
            _isOnFired = false;
            _onLeaveFire?.Invoke();
            transform.SetAsLastSibling();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isDragging = false;
            
            var rectTransform = GetComponent<RectTransform>();
            if (_isOnFire(rectTransform))
            {
                _previousPosition = rectTransform.anchoredPosition;
                _isOnFired = true;
                _onFire?.Invoke();
                
                _currentSide.Value = _GetOtherSide(_currentSide.Value);
                return;
            }
            
            if (_isOnTrashCan(rectTransform))
            {
                Destroy(gameObject);
                return;
            }
            
            if (_isOnPerson(rectTransform))
            {
                Destroy(gameObject);
                _onPersonEat(_CombinedStatus(
                    _ConvertCookedTimeToStatus(_sideCookedTime[Side.Front]),
                    _ConvertCookedTimeToStatus(_sideCookedTime[Side.Back])));
                return;
            }
            
            _previousPosition.Switch(
                previousPosition =>
                {
                    rectTransform.anchoredPosition = previousPosition;
                    if (_isOnFire(rectTransform))
                    {
                        _isOnFired = true;
                        _onFire?.Invoke();
                        _currentSide.Value = _GetOtherSide(_currentSide.Value);
                    }
                },
                () => Destroy(gameObject));
        }

        private Status _ConvertCookedTimeToStatus(float cookedTime)
            => cookedTime switch
            {
                <= 5f => Status.Raw,
                <= 8f => Status.Cooked,
                _ => Status.Burnt,
            };

        private Status _CombinedStatus(Status frontSideStatus, Status backSideStatus)
        {
            if (frontSideStatus == Status.Burnt || backSideStatus == Status.Burnt)
                return Status.Burnt;

            if (frontSideStatus == Status.Raw || backSideStatus == Status.Raw)
                return Status.Raw;
            
            return Status.Cooked;
        }

        private Side _GetOtherSide(Side side)
            => side switch
            {
                Side.Front => Side.Back,
                Side.Back => Side.Front,
                _ => throw new ArgumentOutOfRangeException()
            };
    }
}
