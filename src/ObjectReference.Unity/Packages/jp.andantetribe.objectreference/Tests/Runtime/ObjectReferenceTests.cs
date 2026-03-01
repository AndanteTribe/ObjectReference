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
using UnityEngine.AddressableAssets;
#endif

namespace ObjectReference.Tests
{
    /// <summary>
    /// Tests for SerializableObjectReference&lt;T&gt;.
    /// Since this is an internal class, instances are created via reflection.
    /// </summary>
    public class SerializableObjectReferenceTests
    {
        private Texture2D? _texture;

        [SetUp]
        public void SetUp()
        {
            _texture = new Texture2D(1, 1);
        }

        [TearDown]
        public void TearDown()
        {
            if (_texture != null)
            {
                UnityEngine.Object.DestroyImmediate(_texture);
                _texture = null;
            }
        }

        private const string SerializableObjectReferenceTypeName = "ObjectReference.SerializableObjectReference`1";

        private static IObjectReference<T> CreateReference<T>(T? value = null) where T : UnityEngine.Object
        {
            var openType = typeof(IObjectReference<>).Assembly
                .GetType(SerializableObjectReferenceTypeName)!;
            var closedType = openType.MakeGenericType(typeof(T));
            var instance = Activator.CreateInstance(closedType, nonPublic: true)!;
            if (value != null)
            {
                var field = closedType.GetField("_value", BindingFlags.NonPublic | BindingFlags.Instance)!;
                field.SetValue(instance, value);
            }
            return (IObjectReference<T>)instance;
        }

        [Test]
        public void LoadAsync_WithValidValue_ReturnsValue()
        {
            using var reference = CreateReference(_texture!);
            var task = reference.LoadAsync(CancellationToken.None);
            Assert.IsTrue(task.IsCompleted);
            Assert.That(task.Result, Is.SameAs(_texture));
        }

        [Test]
        public void LoadAsync_WithNullValue_ThrowsNullReferenceException()
        {
            using var reference = CreateReference<Texture2D>();
            Assert.Throws<NullReferenceException>(() => reference.LoadAsync(CancellationToken.None));
        }

        [Test]
        public void LoadAsync_WithCancelledToken_ThrowsOperationCanceledException()
        {
            using var reference = CreateReference(_texture!);
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            Assert.Throws<OperationCanceledException>(() => reference.LoadAsync(cts.Token));
        }

        [Test]
        public void LoadAsync_WithProgressAndValidValue_ReportsProgressAndReturnsValue()
        {
            using var reference = CreateReference(_texture!);
            var progress = new CapturingProgress();
            var task = reference.LoadAsync(progress, CancellationToken.None);
            Assert.IsTrue(task.IsCompleted);
            Assert.That(task.Result, Is.SameAs(_texture));
            Assert.That(progress.LastValue, Is.EqualTo(1.0f).Within(0.001f));
        }

        [Test]
        public void LoadAsync_WithProgressAndNullValue_ThrowsNullReferenceException()
        {
            using var reference = CreateReference<Texture2D>();
            var progress = new CapturingProgress();
            Assert.Throws<NullReferenceException>(() => reference.LoadAsync(progress, CancellationToken.None));
        }

        [Test]
        public void LoadAsync_WithProgressAndCancelledToken_ThrowsOperationCanceledException()
        {
            using var reference = CreateReference(_texture!);
            var progress = new CapturingProgress();
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            Assert.Throws<OperationCanceledException>(() => reference.LoadAsync(progress, cts.Token));
        }

        [Test]
        public void Dispose_DoesNotThrow()
        {
            var reference = CreateReference(_texture!);
            Assert.DoesNotThrow(() => reference.Dispose());
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
            Assert.Throws<ArgumentNullException>(() => new AddressableObjectReference<Texture2D>(null!));
        }

        [Test]
        public void Constructor_WithValidAddress_CreatesInstance()
        {
            using var reference = new AddressableObjectReference<Texture2D>("valid_address");
            Assert.IsNotNull(reference);
        }

        [UnityTest]
        public IEnumerator LoadAsync_WithCancelledToken_ThrowsOperationCanceledException() => UniTask.ToCoroutine(async () =>
        {
            using var reference = new AddressableObjectReference<Texture2D>("some_address");
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
            var reference = new AddressableObjectReference<Texture2D>("__invalid_address_for_test__");
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
            var reference = new AddressableObjectReference<Texture2D>("__invalid_address_for_test__");

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
            using var reference = new AddressableObjectReference<Texture2D>("some_address");
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
            var reference = new AddressableObjectReference<Texture2D>("__invalid_address_for_test__");
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
            var reference = new AddressableObjectReference<Texture2D>("some_address");
            // Dispose without loading covers the if (_handle.IsValid()) false branch
            Assert.DoesNotThrow(() => reference.Dispose());
        }
    }

    /// <summary>
    /// Tests for SerializableAddressableObjectReference&lt;T&gt;.
    /// Since this is an internal class, instances are created via reflection.
    /// </summary>
    public class SerializableAddressableObjectReferenceTests
    {
        private const string SerializableAddressableObjectReferenceTypeName = "ObjectReference.SerializableAddressableObjectReference`1";

        private static object CreateInstance()
        {
            var openType = typeof(IObjectReference<>).Assembly
                .GetType(SerializableAddressableObjectReferenceTypeName)!;
            var closedType = openType.MakeGenericType(typeof(Texture2D));
            return Activator.CreateInstance(closedType, nonPublic: true)!;
        }

        [UnityTest]
        public IEnumerator LoadAsync_WithCancelledToken_ThrowsOperationCanceledException() => UniTask.ToCoroutine(async () =>
        {
            var instance = CreateInstance();
            var reference = (IObjectReference<Texture2D>)instance;
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
        public IEnumerator LoadAsync_WithProgressAndCancelledToken_ThrowsOperationCanceledException() => UniTask.ToCoroutine(async () =>
        {
            var instance = CreateInstance();
            var reference = (IObjectReference<Texture2D>)instance;
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
        public IEnumerator LoadAsync_WithCachedValue_ReturnsCachedValueWithoutAccessingAssetReference() => UniTask.ToCoroutine(async () =>
        {
            var texture = new Texture2D(1, 1);
            try
            {
                var instance = CreateInstance();
                var closedType = instance.GetType();
                // Set _cached directly via reflection; _value remains null
                var cachedField = closedType.GetField("_cached", BindingFlags.NonPublic | BindingFlags.Instance)!;
                cachedField.SetValue(instance, texture);

                var reference = (IObjectReference<Texture2D>)instance;
                // When _cached is set, ??= short-circuits and _value is never accessed
                var result = await reference.LoadAsync(CancellationToken.None);
                Assert.That(result, Is.SameAs(texture));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        });

        [UnityTest]
        public IEnumerator LoadAsync_WithProgressAndCachedValue_ReturnsCachedValue() => UniTask.ToCoroutine(async () =>
        {
            var texture = new Texture2D(1, 1);
            try
            {
                var instance = CreateInstance();
                var closedType = instance.GetType();
                var cachedField = closedType.GetField("_cached", BindingFlags.NonPublic | BindingFlags.Instance)!;
                cachedField.SetValue(instance, texture);

                var reference = (IObjectReference<Texture2D>)instance;
                var progress = new Progress<float>(_ => { });
                var result = await reference.LoadAsync(progress, CancellationToken.None);
                Assert.That(result, Is.SameAs(texture));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        });

        [Test]
        public void Dispose_WhenNotCached_DoesNotThrow()
        {
            var instance = CreateInstance();
            var reference = (IObjectReference<Texture2D>)instance;
            // When _cached is null, Dispose is a no-op
            Assert.DoesNotThrow(() => reference.Dispose());
        }

        [Test]
        public void Dispose_WhenCached_ClearsCache()
        {
            var texture = new Texture2D(1, 1);
            try
            {
                var instance = CreateInstance();
                var closedType = instance.GetType();

                // Set _value to an AssetReferenceT with a fake GUID (nothing loaded, ReleaseAsset is a no-op)
                var assetRef = new AssetReferenceT<Texture2D>("00000000000000000000000000000000");
                var valueField = closedType.GetField("_value", BindingFlags.NonPublic | BindingFlags.Instance)!;
                valueField.SetValue(instance, assetRef);

                var cachedField = closedType.GetField("_cached", BindingFlags.NonPublic | BindingFlags.Instance)!;
                cachedField.SetValue(instance, texture);

                var reference = (IObjectReference<Texture2D>)instance;
                Assert.DoesNotThrow(() => reference.Dispose());

                // Verify _cached is cleared after Dispose
                Assert.IsNull(cachedField.GetValue(instance));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
#endif
}
