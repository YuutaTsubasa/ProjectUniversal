using System;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Yuuta.Core.AssetManager
{
    [ExecuteInEditMode, RequireComponent(typeof(Image))]
    public class ImageAssetProvider : MonoBehaviour
    {
        [SerializeField] private string _imagePath;

        private void Start()
        {
            var imageComponent = GetComponent<Image>();
            Observable.EveryUpdate()
                .Select(_ => _imagePath)
                .DistinctUntilChanged()
                .Subscribe(path => UniTask.Void(async () =>
                    imageComponent.sprite = await AssetManager.LoadAsset<Sprite>(path)))
                .AddTo(this);
        }
    }
}

