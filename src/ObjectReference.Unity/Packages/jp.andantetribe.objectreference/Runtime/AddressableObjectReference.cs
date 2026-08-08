#if ENABLE_ADDRESSABLES && ENABLE_UNITASK
#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ObjectReference
{
    /// <summary>
    /// Object references using Addressable addresses.
    /// </summary>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// using System.Threading;
    /// using Cysharp.Threading.Tasks;
    /// using ObjectReference;
    /// using UnityEngine;
    ///
    /// public class SimpleAddressableSample : MonoBehaviour
    /// {
    ///     // Addressable address of the prefab. Make sure to set the address in the Addressable Asset settings.
    ///     private readonly IObjectReference<GameObject> _reference
    ///         = new AddressableObjectReference<GameObject>("assets/prefabs/MyPrefab.prefab");
    ///
    ///     private async UniTask Start()
    ///     {
    ///         // Load the prefab asynchronously.
    ///         var prefab = await _reference.LoadAsync(destroyCancellationToken);
    ///         var instance = Instantiate(prefab, Vector3.zero, Quaternion.identity);
    ///     }
    ///
    ///     private void OnDestroy()
    ///     {
    ///         _reference.Dispose();
    ///     }
    /// }
    /// ]]>
    /// </code>
    /// </example>
    /// <typeparam name="T"></typeparam>
    public sealed class AddressableObjectReference<T> : IObjectReference<T> where T : UnityEngine.Object
    {
        private readonly string _address;
        private AsyncOperationHandle<T> _handle;

        public AddressableObjectReference(string address)
        {
            _address = address ?? throw new ArgumentNullException(nameof(address), "Address cannot be null.");
        }

        public ValueTask<T> LoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_handle.IsValid() && _handle.Status == AsyncOperationStatus.Succeeded)
            {
                return new ValueTask<T>(_handle.Result);
            }

            return LoadAsyncCore(this, cancellationToken);

            static async ValueTask<T> LoadAsyncCore(AddressableObjectReference<T> reference, CancellationToken cancellationToken)
            {
                if (!reference._handle.IsValid())
                {
                    reference._handle = Addressables.LoadAssetAsync<T>(reference._address);
                }

                try
                {
                    return await reference._handle.ToUniTask(cancellationToken: cancellationToken);
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
            if (_handle.IsValid() && _handle.Status == AsyncOperationStatus.Succeeded)
            {
                progress.Report(1.0f);
                return new ValueTask<T>(_handle.Result);
            }

            return LoadAsyncCore(this, progress, cancellationToken);

            static async ValueTask<T> LoadAsyncCore(AddressableObjectReference<T> reference, IProgress<float> progress, CancellationToken cancellationToken)
            {
                if (!reference._handle.IsValid())
                {
                    reference._handle = Addressables.LoadAssetAsync<T>(reference._address);
                }

                T result;
                try
                {
                    result = await reference._handle.ToUniTask(progress: progress, cancellationToken: cancellationToken);
                }
                catch
                {
                    reference.Release();
                    throw;
                }

                progress.Report(1.0f);
                return result;
            }
        }

        /// <inheritdoc />
        public void Dispose() => Release();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Release()
        {
            if (_handle.IsValid())
            {
                _handle.Release();
                _handle = default;
            }
        }
    }
}

#endif
