using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Yuuta.CookMeat;

namespace Yuuta
{
    public class Main : MonoBehaviour
    {
        [SerializeField] private Game _cookMeatGame;

        public async UniTaskVoid OnEnable()
        {
            while (true) 
                await _cookMeatGame.Run(new Game.Setting(
                    TimeSpan.FromSeconds(120)),
                    new CancellationToken());
        }
    }
}

