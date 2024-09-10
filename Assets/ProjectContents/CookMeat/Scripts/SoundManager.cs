using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yuuta.Core.Utils;

namespace Yuuta.CookMeat
{
    public class SoundManager : MonoBehaviour
    {
        public enum SoundType
        {
            CountDown = 0,
            Confirm = 1,
            GameOver = 2,
            Bad = 3,
        }
        
        [Serializable]
        private struct SoundTypeToAudioSourceMapping
        {
            public SoundType SoundType;
            public AudioSource AudioSource;
        }
        
        [SerializeField] private SoundTypeToAudioSourceMapping[] soundTypeToAudioSourceMappings;

        public void PlaySound(SoundType soundType)
        {
            soundTypeToAudioSourceMappings
                .FirstOrNone(mapping => mapping.SoundType == soundType)
                .SwitchSome(mapping => mapping.AudioSource.Play());
        }
    }

}

