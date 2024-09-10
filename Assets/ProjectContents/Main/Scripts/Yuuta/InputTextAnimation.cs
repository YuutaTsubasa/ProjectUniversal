using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Yuuta.Core.Utils;

namespace Yuuta.Core
{
    public class InputTextAnimation : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private Text _text;
        [SerializeField] private string[] _contents;
        
        private Subject<Unit> _onPointerDown = new Subject<Unit>();
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        
        private async UniTaskVoid OnEnable()
        {
            foreach (var content in _contents)
            {
                _cancellationTokenSource = new CancellationTokenSource();
                await _text.PlayInputTextAnimation(
                    content,
                    TimeSpan.FromSeconds(0.02f),
                    _cancellationTokenSource.Token)
                    .SuppressCancellationThrow();
                await _onPointerDown.FirstAsync();
                await UniTask.Yield();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _onPointerDown.OnNext(Unit.Default);
            _cancellationTokenSource.Cancel();
        }
    }

}
