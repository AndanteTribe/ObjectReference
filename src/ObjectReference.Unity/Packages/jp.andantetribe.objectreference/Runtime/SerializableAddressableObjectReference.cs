#if ENABLE_ADDRESSABLES && ENABLE_UNITASK
#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ObjectReference
{
    [Serializable]
    internal sealed class SerializableAddressableObjectReference<T> : IObjectReference<T> where T : UnityEngine.Object
    {
        [SerializeField]
        private AssetReferenceT<T> _value = null!;
        private T? _cached;

        /// <inheritdoc />
        public async ValueTask<T> LoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if(_cached == null)
            {
                _cached = await _value.LoadAssetAsync<T>().ToUniTask(cancellationToken: cancellationToken, autoReleaseWhenCanceled: true);
            }
            return _cached;
        }

        /// <inheritdoc />
        public async ValueTask<T> LoadAsync(IProgress<float> progress, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_cached == null)
            {
                _cached ??= await _value.LoadAssetAsync<T>().ToUniTask(progress: progress, cancellationToken: cancellationToken, autoReleaseWhenCanceled: true);
            }
            progress.Report(1.0f);
            return _cached;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_cached != null)
            {
                _value.ReleaseAsset();
                _cached = null;
            }
        }
    }
}

#endif