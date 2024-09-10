using Cysharp.Threading.Tasks;

namespace Yuuta.Core.AssetManager
{
    public interface IAssetSource
    {
        public UniTask<T> LoadAsset<T>(string assetPath) where T : class;
    }
}