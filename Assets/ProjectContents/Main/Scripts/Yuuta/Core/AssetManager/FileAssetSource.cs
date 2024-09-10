using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Yuuta.Core.AssetManager
{
    public class FileAssetSource : IAssetSource
    {
        private string _rootPath;
        
        public FileAssetSource(string rootPath)
        {
            _rootPath = rootPath;
        }

        async UniTask<T> IAssetSource.LoadAsset<T>(string assetPath) where T : class
        {
            string fullPath = Path.Combine(_rootPath, assetPath);

            if (typeof(T) == typeof(Texture))
                return (await _LoadTexture(fullPath)) as T;
            
            if (typeof(T) == typeof(Sprite))
                return (await _LoadSprite(fullPath)) as T;

            return (await _LoadBytes(fullPath)) as T;
        }

        private async UniTask<Texture> _LoadTexture(string fullPath)
        {
            try
            {
                using var webRequest = UnityWebRequestTexture.GetTexture(fullPath);
                await webRequest.SendWebRequest();
                return DownloadHandlerTexture.GetContent(webRequest);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            return Texture2D.whiteTexture;
        }
        
        private async UniTask<Sprite> _LoadSprite(string fullPath)
        {
            var texture = (await _LoadTexture(fullPath)) as Texture2D;
            if (texture != null)
            {
                var pivot = new Vector2(
                    // ReSharper disable once PossibleLossOfFraction
                    texture.width / 2, 
                    // ReSharper disable once PossibleLossOfFraction
                    texture.height / 2);
                return Sprite.Create(
                    texture,
                    new Rect(Vector2.zero, new Vector2(texture.width, texture.height)),
                    pivot);
            }

            return Sprite.Create(null, Rect.zero, Vector2.zero);
        }
        
        private async UniTask<byte[]> _LoadBytes(string fullPath)
        {
            using var webRequest = UnityWebRequest.Get(fullPath);
            await webRequest.SendWebRequest();
            return webRequest.downloadHandler.data;
        }
    }

}
