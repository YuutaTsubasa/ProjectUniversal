using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Yuuta.Core.Utils
{
    public static class AnimationUtils
    {
        public static async UniTask PlayInputTextAnimation(
            this Text text,
            string content, 
            TimeSpan duration,
            CancellationToken cancellationToken)
        {
            var currentContent = "";
            foreach (var c in content)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                await UniTask.Delay(duration, cancellationToken: cancellationToken).SuppressCancellationThrow();
                currentContent += c;
                text.text = currentContent;
            }
            text.text = content;
        }
    }
}
