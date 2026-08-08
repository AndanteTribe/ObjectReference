#if ENABLE_ADDRESSABLES && ENABLE_UNITASK
#nullable enable

using System;
using System.Runtime.CompilerServices;
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
        public ValueTask<T> LoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_cached != null)
            {
                return new ValueTask<T>(_cached);
            }

            return LoadAsyncCore(this, cancellationToken);

            static async ValueTask<T> LoadAsyncCore(
                SerializableAddressableObjectReference<T> reference,
                CancellationToken cancellationToken)
            {
                try
                {
                    reference._cached = await reference._value.LoadAssetAsync<T>()
                        .ToUniTask(cancellationToken: cancellationToken);
                    return reference._cached;
                }
                catch
                {
                    reference.Release();
                    throw;
                }
            }
        }

        /// <inheritdoc />
        public ValueTask<T> LoadAsync(IProgress<float> progress, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_cached != null)
            {
                try
                {
                    progress.Report(1.0f);
                    return new ValueTask<T>(_cached);
                }
                catch
                {
                    Release();
                    throw;
                }
            }

            return LoadAsyncCore(this, progress, cancellationToken);

            static async ValueTask<T> LoadAsyncCore(
                SerializableAddressableObjectReference<T> reference,
                IProgress<float> progress,
                CancellationToken cancellationToken)
            {
                try
                {
                    reference._cached = await reference._value.LoadAssetAsync<T>()
                        .ToUniTask(progress: progress, cancellationToken: cancellationToken);
                    progress.Report(1.0f);
                    return reference._cached;
                }
                catch
                {
                    reference.Release();
                    throw;
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Release();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Release()
        {
            if (_value != null && _value.OperationHandle.IsValid())
            {
                _value.ReleaseAsset();
            }

            _cached = null;
        }
    }
}

#endif
