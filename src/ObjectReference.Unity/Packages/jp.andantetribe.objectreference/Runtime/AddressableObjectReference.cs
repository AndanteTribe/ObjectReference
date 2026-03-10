#if ENABLE_ADDRESSABLES && ENABLE_UNITASK
#nullable enable

using System;
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

        public async ValueTask<T> LoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_handle.IsValid())
            {
                _handle = Addressables.LoadAssetAsync<T>(_address);
            }

            return await _handle.ToUniTask(cancellationToken: cancellationToken, autoReleaseWhenCanceled: true);
        }

        /// <inheritdoc />
        public async ValueTask<T> LoadAsync(IProgress<float> progress, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_handle.IsValid())
            {
                _handle = Addressables.LoadAssetAsync<T>(_address);
            }

            var result = await _handle.ToUniTask(progress: progress, cancellationToken: cancellationToken, autoReleaseWhenCanceled: true);
            progress.Report(1.0f);
            return result;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_handle.IsValid())
            {
                Addressables.Release(_handle);
                _handle = default;
            }
        }
    }
}

#endif