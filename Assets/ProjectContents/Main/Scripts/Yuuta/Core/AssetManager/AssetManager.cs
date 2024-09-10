using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Yuuta.Core.AssetManager
{
    public static class AssetManager
    {
        private static readonly IAssetSource _assetSource = 
            new FileAssetSource(@"C:\Users\User\Repo\ProjectUniversal\ProjectAssets");

        public static async UniTask<T> LoadAsset<T>(string assetPath) where T : class
            => await _assetSource.LoadAsset<T>(assetPath);
    }
}