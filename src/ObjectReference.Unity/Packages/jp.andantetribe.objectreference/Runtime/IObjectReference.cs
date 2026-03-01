#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace ObjectReference
{
    /// <summary>
    /// Interface for object reference.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IObjectReference<T> : IDisposable where T : UnityEngine.Object
    {
        /// <summary>
        /// Loads the object asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        ValueTask<T> LoadAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Loads the object asynchronously with progress reporting.
        /// </summary>
        /// <param name="progress">Progress callback.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        ValueTask<T> LoadAsync(IProgress<float> progress, CancellationToken cancellationToken);
    }
}
