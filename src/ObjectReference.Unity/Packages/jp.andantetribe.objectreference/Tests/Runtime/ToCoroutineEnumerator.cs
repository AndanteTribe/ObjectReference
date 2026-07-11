#nullable enable

using System;
using System.Collections;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace ObjectReference.Tests
{
    [ExcludeFromCoverage]
    [Ignore("This is a utility class for testing")]
    internal sealed class ToCoroutineEnumerator : IEnumerator
    {
        private readonly Func<ValueTask> _task;
        private readonly Action<Exception>? _exceptionHandler;

        private bool _completed;
        private bool _isStarted;
        private ExceptionDispatchInfo? _exception;

        public ToCoroutineEnumerator(Func<ValueTask> task, Action<Exception>? exceptionHandler = null)
        {
            _task = task;
            _exceptionHandler = exceptionHandler;
        }

        public object Current => null!;

        public bool MoveNext()
        {
            if (!_isStarted)
            {
                _isStarted = true;
                _ = RunTask();
            }

            _exception?.Throw();
            return !_completed;
        }

        void IEnumerator.Reset() => throw new NotSupportedException("Reset is not supported for this enumerator.");

        private async ValueTask RunTask()
        {
            try
            {
                await _task();
            }
            catch (Exception ex)
            {
                if (_exceptionHandler != null)
                {
                    _exceptionHandler(ex);
                }
                else
                {
                    _exception = ExceptionDispatchInfo.Capture(ex);
                }
            }
            finally
            {
                _completed = true;
            }
        }
    }
}