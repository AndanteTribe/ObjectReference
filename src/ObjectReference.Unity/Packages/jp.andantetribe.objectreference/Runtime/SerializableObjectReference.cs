#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ObjectReference
{
    [Serializable]
    internal sealed class SerializableObjectReference<T> : IObjectReference<T> where T : UnityEngine.Object
    {
        [SerializeField]
        private T _value = null!;

        /// <inheritdoc />
        public ValueTask<T> LoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_value == null)
            {
                throw new NullReferenceException("Object reference is null.");
            }
            return new ValueTask<T>(_value);
        }

        /// <inheritdoc />
        public ValueTask<T> LoadAsync(IProgress<float> progress, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_value == null)
            {
                throw new NullReferenceException("Object reference is null.");
            }
            progress.Report(1.0f);
            return new ValueTask<T>(_value);
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}