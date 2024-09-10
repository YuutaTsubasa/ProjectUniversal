using System;
using System.Collections;
using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Yuuta.Core.Utils
{
    public class Timer
    {
        private double _endTime = 0f;
        public ReactiveProperty<bool> IsTimeUp { get; private set; }
        
        public double RemainingTime => Math.Max(0f, _endTime - Time.time);
        
        public Timer(TimeSpan timeSpan)
        {
            _endTime = Time.time + timeSpan.TotalSeconds;
            IsTimeUp = Observable.Timer(timeSpan)
                .Select(_ => true)
                .ToBindableReactiveProperty();
        }
    }

}
