using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yuuta.CookMeat
{
    public class BBQSoundPlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource _audioSource;

        private int _count = 0;

        public void Play()
        {
            ++_count;
            _audioSource.loop = true;
            _audioSource.Play();
        }

        public void Stop()
        {
            --_count;
            if (_count > 0)
                return;
            
            _audioSource.Stop();
        }
    }
}

