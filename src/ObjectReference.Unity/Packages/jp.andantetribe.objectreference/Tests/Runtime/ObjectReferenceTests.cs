#nullable enable

using System;
using System.Collections;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if ENABLE_ADDRESSABLES && ENABLE_UNITASK
using Cysharp.Threading.Tasks;
#endif

namespace ObjectReference.Tests
{
    /// <summary>
    /// Tests for SerializableObjectReference&lt;T&gt; via the [SerializeReference] workflow.
    /// </summary>
    public class SerializableObjectReferenceTests
    {
        private DummyObjectReferenceData _dummyData = null!;

        [SetUp]
        public void SetUp()
        {
            _dummyData = DummyObjectReferenceData.Load();
        }

        [Test]
        public void LoadAsync_Prefab_ReturnsCube()
        {
            Assert.IsNotNull(_dummyData);
            var task = _dummyData.PrefabReference.LoadAsync(CancellationToken.None);
            Assert.IsTrue(task.IsCompleted);
            Assert.IsNotNull(task.Result);
            Assert.That(task.Result.name, Is.EqualTo("Cube"));
        }

        [Test]
        public void LoadAsync_Material_ReturnsMaterial()
        {
            Assert.IsNotNull(_dummyData);
            var task = _dummyData.MaterialReference.LoadAsync(CancellationToken.None);
            Assert.IsTrue(task.IsCompleted);
            Assert.IsNotNull(task.Result);
            Assert.That(task.Result.name, Is.EqualTo("New Material"));
        }

        [Test]
        public void LoadAsync_WithProgress_ReportsCompletionAndReturnsPrefab()
        {
            var progress = new CapturingProgress();
            var task = _dummyData.PrefabReference.LoadAsync(progress, CancellationToken.None);
            Assert.IsTrue(task.IsCompleted);
            Assert.IsNotNull(task.Result);
            Assert.That(progress.LastValue, Is.EqualTo(1.0f).Within(0.001f));
        }

        [Test]
        public void LoadAsync_WithCancelledToken_ThrowsOperationCanceledException()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            Assert.Throws<OperationCanceledException>(
                () => _dummyData.PrefabReference.LoadAsync(cts.Token));
        }

        [Test]
        public void LoadAsync_WithProgressAndCancelledToken_ThrowsOperationCanceledException()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var progress = new CapturingProgress();
            Assert.Throws<OperationCanceledException>(
                () => _dummyData.PrefabReference.LoadAsync(progress, cts.Token));
        }

        [Test]
        public void LoadAsync_WithNullValue_ThrowsNullReferenceException()
        {
            // Edge case: tests the null guard when _value is not assigned in the inspector
            var openType = typeof(IObjectReference<>).Assembly
                .GetType("ObjectReference.SerializableObjectReference`1")!;
            var closedType = openType.MakeGenericType(typeof(GameObject));
            using var reference = (IObjectReference<GameObject>)Activator.CreateInstance(closedType, nonPublic: true)!;
            Assert.Throws<NullReferenceException>(() => reference.LoadAsync(CancellationToken.None));
        }

        [Test]
        public void Dispose_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _dummyData.PrefabReference.Dispose());
        }

        private sealed class CapturingProgress : IProgress<float>
        {
            public float LastValue { get; private set; } = -1f;
            public void Report(float value) => LastValue = value;
        }
    }

#if ENABLE_ADDRESSABLES && ENABLE_UNITASK
    /// <summary>
    /// Tests for AddressableObjectReference&lt;T&gt;.
    /// </summary>
    public class AddressableObjectReferenceTests
    {
        [Test]
        public void Constructor_WithNullAddress_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new AddressableObjectReference<GameObject>(null!));
        }

        [Test]
        public void Constructor_WithValidAddress_CreatesInstance()
        {
            using var reference = new AddressableObjectReference<GameObject>("valid_address");
            Assert.IsNotNull(reference);
        }

        [UnityTest]
        public IEnumerator LoadAsync_WithCancelledToken_ThrowsOperationCanceledException() => UniTask.ToCoroutine(async () =>
        {
            using var reference = new AddressableObjectReference<GameObject>("some_address");
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            try
            {
                await reference.LoadAsync(cts.Token);
                Assert.Fail("Expected OperationCanceledException");
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        });

        [UnityTest]
        public IEnumerator LoadAsync_WithInvalidAddress_ThrowsException() => UniTask.ToCoroutine(async () =>
        {
            var reference = new AddressableObjectReference<GameObject>("__invalid_address_for_test__");
            Exception? caughtException = null;

            try
            {
                await reference.LoadAsync(CancellationToken.None);
                Assert.Fail("Expected an exception for invalid address");
            }
            catch (Exception ex) when (ex is not AssertionException)
            {
                caughtException = ex;
            }
            finally
            {
                // Dispose covers the if (_handle.IsValid()) true branch
                reference.Dispose();
            }

            Assert.IsNotNull(caughtException);
        });

        [UnityTest]
        public IEnumerator LoadAsync_WhenHandleAlreadyExists_ReusesExistingHandle() => UniTask.ToCoroutine(async () =>
        {
            var reference = new AddressableObjectReference<GameObject>("__invalid_address_for_test__");

            // First call creates the handle (will fail for invalid address)
            try
            {
                await reference.LoadAsync(CancellationToken.None);
            }
            catch (Exception ex) when (ex is not AssertionException)
            {
                // Expected failure for invalid address
            }

            // Second call reuses the existing handle (covers the if (!_handle.IsValid()) false branch)
            Exception? caughtException = null;
            try
            {
                await reference.LoadAsync(CancellationToken.None);
            }
            catch (Exception ex) when (ex is not AssertionException)
            {
                caughtException = ex;
            }
            finally
            {
                reference.Dispose();
            }

            Assert.IsNotNull(caughtException, "Expected exception on second call with existing invalid handle");
        });

        [UnityTest]
        public IEnumerator LoadAsync_WithProgressAndCancelledToken_ThrowsOperationCanceledException() => UniTask.ToCoroutine(async () =>
        {
            using var reference = new AddressableObjectReference<GameObject>("some_address");
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            try
            {
                var progress = new Progress<float>(_ => { });
                await reference.LoadAsync(progress, cts.Token);
                Assert.Fail("Expected OperationCanceledException");
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        });

        [UnityTest]
        public IEnumerator LoadAsync_WithProgressAndInvalidAddress_ThrowsException() => UniTask.ToCoroutine(async () =>
        {
            var reference = new AddressableObjectReference<GameObject>("__invalid_address_for_test__");
            Exception? caughtException = null;

            try
            {
                var progress = new Progress<float>(_ => { });
                await reference.LoadAsync(progress, CancellationToken.None);
                Assert.Fail("Expected an exception for invalid address");
            }
            catch (Exception ex) when (ex is not AssertionException)
            {
                caughtException = ex;
            }
            finally
            {
                // Dispose covers the if (_handle.IsValid()) true branch for the progress overload
                reference.Dispose();
            }

            Assert.IsNotNull(caughtException);
        });

        [Test]
        public void Dispose_WhenNotLoaded_DoesNotThrow()
        {
            var reference = new AddressableObjectReference<GameObject>("some_address");
            // Dispose without loading covers the if (_handle.IsValid()) false branch
            Assert.DoesNotThrow(() => reference.Dispose());
        }
    }

    /// <summary>
    /// Tests for SerializableAddressableObjectReference&lt;T&gt; via the [SerializeReference] workflow.
    /// </summary>
    public class SerializableAddressableObjectReferenceTests
    {
        private DummyObjectReferenceData _dummyData = null!;

        [SetUp]
        public void SetUp()
        {
            _dummyData = DummyObjectReferenceData.Load();
        }

        [TearDown]
        public void TearDown()
        {
            _dummyData.AddressablePrefabReference.Dispose();
            _dummyData.AddressableMaterialReference.Dispose();
        }

        [UnityTest]
        public IEnumerator LoadAsync_Prefab_ReturnsCube() => UniTask.ToCoroutine(async () =>
        {
            var result = await _dummyData.AddressablePrefabReference.LoadAsync(CancellationToken.None);
            Assert.IsNotNull(result);
            Assert.That(result.name, Is.EqualTo("Cube"));
        });

        [UnityTest]
        public IEnumerator LoadAsync_Material_ReturnsMaterial() => UniTask.ToCoroutine(async () =>
        {
            var result = await _dummyData.AddressableMaterialReference.LoadAsync(CancellationToken.None);
            Assert.IsNotNull(result);
            Assert.That(result.name, Is.EqualTo("New Material"));
        });

        [UnityTest]
        public IEnumerator LoadAsync_SecondCall_ReturnsCachedValue() => UniTask.ToCoroutine(async () =>
        {
            var result1 = await _dummyData.AddressablePrefabReference.LoadAsync(CancellationToken.None);
            var result2 = await _dummyData.AddressablePrefabReference.LoadAsync(CancellationToken.None);
            Assert.IsNotNull(result1);
            Assert.That(result1, Is.SameAs(result2));
        });

        [UnityTest]
        public IEnumerator LoadAsync_WithProgress_ReturnsAsset() => UniTask.ToCoroutine(async () =>
        {
            var progress = new Progress<float>(_ => { });
            var result = await _dummyData.AddressablePrefabReference.LoadAsync(progress, CancellationToken.None);
            Assert.IsNotNull(result);
        });

        [UnityTest]
        public IEnumerator LoadAsync_WithCancelledToken_ThrowsOperationCanceledException() => UniTask.ToCoroutine(async () =>
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            try
            {
                await _dummyData.AddressablePrefabReference.LoadAsync(cts.Token);
                Assert.Fail("Expected OperationCanceledException");
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        });

        [UnityTest]
        public IEnumerator Dispose_WhenCached_ReleasesAsset() => UniTask.ToCoroutine(async () =>
        {
            var result = await _dummyData.AddressablePrefabReference.LoadAsync(CancellationToken.None);
            Assert.IsNotNull(result);
            Assert.DoesNotThrow(() => _dummyData.AddressablePrefabReference.Dispose());
        });

        [Test]
        public void Dispose_WhenNotCached_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _dummyData.AddressableMaterialReference.Dispose());
        }
    }
#endif
}

