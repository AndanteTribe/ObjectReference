#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
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
    /// Tests for all <see cref="IObjectReference{T}"/> implementations via the [SerializeReference] workflow.
    /// Common load/dispose behaviour is parameterized over both SerializableObjectReference and
    /// SerializableAddressableObjectReference assets using <see cref="ValueSourceAttribute"/>.
    /// </summary>
    public class ObjectReferenceTests
    {
        private const string PackagePath = "Packages/jp.andantetribe.objectreference/Tests/Runtime";
        private const string DirectDataPath = PackagePath + "/DummyData.asset";
        private const string EmptyDataPath = PackagePath + "/DummyEmptyData.asset";
#if ENABLE_ADDRESSABLES && ENABLE_UNITASK
        private const string AddressableDataPath = PackagePath + "/DummyAddressableData.asset";
#endif

        private static IEnumerable<string> AllImplementationPaths()
        {
            yield return DirectDataPath;
#if ENABLE_ADDRESSABLES && ENABLE_UNITASK
            yield return AddressableDataPath;
#endif
        }

        private static DummyObjectReferenceData LoadData(string path)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<DummyObjectReferenceData>(path);
#else
            throw new NotSupportedException("Test assets can only be loaded in the Unity Editor.");
#endif
        }

        // ---- Parameterized tests covering both implementations ----

#if ENABLE_ADDRESSABLES && ENABLE_UNITASK
        [UnityTest]
        public IEnumerator LoadAsync_GameObjectReference_ReturnsCube(
            [ValueSource(nameof(AllImplementationPaths))] string dataPath) => UniTask.ToCoroutine(async () =>
        {
            var data = LoadData(dataPath);
            try
            {
                var result = await data.GameObjectReference.LoadAsync(CancellationToken.None);
                Assert.That(result, Is.Not.Null);
                Assert.That(result.name, Is.EqualTo("Cube"));
            }
            finally
            {
                data.GameObjectReference.Dispose();
            }
        });

        [UnityTest]
        public IEnumerator LoadAsync_MaterialReference_ReturnsMaterial(
            [ValueSource(nameof(AllImplementationPaths))] string dataPath) => UniTask.ToCoroutine(async () =>
        {
            var data = LoadData(dataPath);
            try
            {
                var result = await data.MaterialReference.LoadAsync(CancellationToken.None);
                Assert.That(result, Is.Not.Null);
                Assert.That(result.name, Is.EqualTo("New Material"));
            }
            finally
            {
                data.MaterialReference.Dispose();
            }
        });

        [UnityTest]
        public IEnumerator LoadAsync_WithProgress_ReportsAndReturnsAsset(
            [ValueSource(nameof(AllImplementationPaths))] string dataPath) => UniTask.ToCoroutine(async () =>
        {
            var data = LoadData(dataPath);
            float lastProgress = -1f;
            var progress = new Progress<float>(v => lastProgress = v);
            try
            {
                var result = await data.GameObjectReference.LoadAsync(progress, CancellationToken.None);
                Assert.That(result, Is.Not.Null);
                Assert.That(lastProgress, Is.EqualTo(1.0f).Within(0.001f));
            }
            finally
            {
                data.GameObjectReference.Dispose();
            }
        });

        [UnityTest]
        public IEnumerator LoadAsync_WithCancelledToken_ThrowsOperationCanceledException(
            [ValueSource(nameof(AllImplementationPaths))] string dataPath) => UniTask.ToCoroutine(async () =>
        {
            var data = LoadData(dataPath);
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var caught = false;
            try
            {
                await data.GameObjectReference.LoadAsync(cts.Token);
                Assert.Fail("Expected OperationCanceledException");
            }
            catch (OperationCanceledException)
            {
                caught = true;
            }
            Assert.That(caught, Is.True);
        });

        [UnityTest]
        public IEnumerator Dispose_AfterLoad_DoesNotThrow(
            [ValueSource(nameof(AllImplementationPaths))] string dataPath) => UniTask.ToCoroutine(async () =>
        {
            var data = LoadData(dataPath);
            await data.GameObjectReference.LoadAsync(CancellationToken.None);
            Assert.That(() => data.GameObjectReference.Dispose(), Throws.Nothing);
        });

        [UnityTest]
        public IEnumerator Dispose_WithoutLoad_DoesNotThrow(
            [ValueSource(nameof(AllImplementationPaths))] string dataPath) => UniTask.ToCoroutine(async () =>
        {
            var data = LoadData(dataPath);
            Assert.That(() => data.GameObjectReference.Dispose(), Throws.Nothing);
        });

        // ---- SerializableObjectReference-specific ----

        [UnityTest]
        public IEnumerator LoadAsync_EmptyReference_ThrowsNullReferenceException() => UniTask.ToCoroutine(async () =>
        {
            var data = LoadData(EmptyDataPath);
            var caught = false;
            try
            {
                await data.GameObjectReference.LoadAsync(CancellationToken.None);
                Assert.Fail("Expected NullReferenceException");
            }
            catch (NullReferenceException)
            {
                caught = true;
            }
            Assert.That(caught, Is.True);
        });

        // ---- SerializableAddressableObjectReference-specific ----

        [UnityTest]
        public IEnumerator LoadAsync_Addressable_SecondCall_ReturnsCachedValue() => UniTask.ToCoroutine(async () =>
        {
            var data = LoadData(AddressableDataPath);
            try
            {
                var r1 = await data.GameObjectReference.LoadAsync(CancellationToken.None);
                var r2 = await data.GameObjectReference.LoadAsync(CancellationToken.None);
                Assert.That(r1, Is.Not.Null);
                Assert.That(r1, Is.SameAs(r2));
            }
            finally
            {
                data.GameObjectReference.Dispose();
            }
        });

#else
        // When Addressables/UniTask are not present, run the direct-reference tests using ToCoroutineEnumerator.

        [UnityTest]
        public IEnumerator LoadAsync_GameObjectReference_ReturnsCube() => new ToCoroutineEnumerator(async () =>
        {
            var data = LoadData(DirectDataPath);
            var result = await data.GameObjectReference.LoadAsync(CancellationToken.None);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.name, Is.EqualTo("Cube"));
        });

        [UnityTest]
        public IEnumerator LoadAsync_MaterialReference_ReturnsMaterial() => new ToCoroutineEnumerator(async () =>
        {
            var data = LoadData(DirectDataPath);
            var result = await data.MaterialReference.LoadAsync(CancellationToken.None);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.name, Is.EqualTo("New Material"));
        });

        [UnityTest]
        public IEnumerator LoadAsync_WithProgress_ReportsAndReturnsAsset() => new ToCoroutineEnumerator(async () =>
        {
            var data = LoadData(DirectDataPath);
            float lastProgress = -1f;
            var progress = new Progress<float>(v => lastProgress = v);
            var result = await data.GameObjectReference.LoadAsync(progress, CancellationToken.None);
            Assert.That(result, Is.Not.Null);
            Assert.That(lastProgress, Is.EqualTo(1.0f).Within(0.001f));
        });

        [UnityTest]
        public IEnumerator LoadAsync_WithCancelledToken_ThrowsOperationCanceledException() => new ToCoroutineEnumerator(async () =>
        {
            var data = LoadData(DirectDataPath);
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var caught = false;
            try
            {
                await data.GameObjectReference.LoadAsync(cts.Token);
                Assert.Fail("Expected OperationCanceledException");
            }
            catch (OperationCanceledException)
            {
                caught = true;
            }
            Assert.That(caught, Is.True);
        });

        [UnityTest]
        public IEnumerator LoadAsync_EmptyReference_ThrowsNullReferenceException() => new ToCoroutineEnumerator(async () =>
        {
            var data = LoadData(EmptyDataPath);
            var caught = false;
            try
            {
                await data.GameObjectReference.LoadAsync(CancellationToken.None);
                Assert.Fail("Expected NullReferenceException");
            }
            catch (NullReferenceException)
            {
                caught = true;
            }
            Assert.That(caught, Is.True);
        });

        [UnityTest]
        public IEnumerator Dispose_AfterLoad_DoesNotThrow() => new ToCoroutineEnumerator(async () =>
        {
            var data = LoadData(DirectDataPath);
            await data.GameObjectReference.LoadAsync(CancellationToken.None);
            Assert.That(() => data.GameObjectReference.Dispose(), Throws.Nothing);
        });

        [UnityTest]
        public IEnumerator Dispose_WithoutLoad_DoesNotThrow() => new ToCoroutineEnumerator(async () =>
        {
            var data = LoadData(DirectDataPath);
            Assert.That(() => data.GameObjectReference.Dispose(), Throws.Nothing);
        });
#endif
    }

#if ENABLE_ADDRESSABLES && ENABLE_UNITASK
    /// <summary>
    /// Tests for the public <see cref="AddressableObjectReference{T}"/> (string-address based).
    /// </summary>
    public class AddressableObjectReferenceTests
    {
        [Test]
        public void Constructor_NullAddress_ThrowsArgumentNullException()
        {
            Assert.That(() => new AddressableObjectReference<GameObject>(null!), Throws.ArgumentNullException);
        }

        [Test]
        public void Constructor_ValidAddress_CreatesInstance()
        {
            using var reference = new AddressableObjectReference<GameObject>("valid_address");
            Assert.That(reference, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator LoadAsync_WithCancelledToken_ThrowsOperationCanceledException() => UniTask.ToCoroutine(async () =>
        {
            using var reference = new AddressableObjectReference<GameObject>("some_address");
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var caught = false;
            try
            {
                await reference.LoadAsync(cts.Token);
                Assert.Fail("Expected OperationCanceledException");
            }
            catch (OperationCanceledException)
            {
                caught = true;
            }
            Assert.That(caught, Is.True);
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
                reference.Dispose();
            }
            Assert.That(caughtException, Is.Not.Null);
        });

        [UnityTest]
        public IEnumerator LoadAsync_WhenHandleAlreadyExists_ReusesExistingHandle() => UniTask.ToCoroutine(async () =>
        {
            var reference = new AddressableObjectReference<GameObject>("__invalid_address_for_test__");
            try
            {
                await reference.LoadAsync(CancellationToken.None);
            }
            catch (Exception ex) when (ex is not AssertionException) { }

            Exception? caughtException = null;
            try
            {
                await reference.LoadAsync(CancellationToken.None);
                Assert.Fail("Expected exception on second call with existing handle");
            }
            catch (Exception ex) when (ex is not AssertionException)
            {
                caughtException = ex;
            }
            finally
            {
                reference.Dispose();
            }
            Assert.That(caughtException, Is.Not.Null);
        });

        [UnityTest]
        public IEnumerator LoadAsync_WithProgressAndCancelledToken_ThrowsOperationCanceledException() => UniTask.ToCoroutine(async () =>
        {
            using var reference = new AddressableObjectReference<GameObject>("some_address");
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var caught = false;
            try
            {
                await reference.LoadAsync(new Progress<float>(_ => { }), cts.Token);
                Assert.Fail("Expected OperationCanceledException");
            }
            catch (OperationCanceledException)
            {
                caught = true;
            }
            Assert.That(caught, Is.True);
        });

        [UnityTest]
        public IEnumerator LoadAsync_WithProgressAndInvalidAddress_ThrowsException() => UniTask.ToCoroutine(async () =>
        {
            var reference = new AddressableObjectReference<GameObject>("__invalid_address_for_test__");
            Exception? caughtException = null;
            try
            {
                await reference.LoadAsync(new Progress<float>(_ => { }), CancellationToken.None);
                Assert.Fail("Expected an exception for invalid address");
            }
            catch (Exception ex) when (ex is not AssertionException)
            {
                caughtException = ex;
            }
            finally
            {
                reference.Dispose();
            }
            Assert.That(caughtException, Is.Not.Null);
        });

        [Test]
        public void Dispose_WhenNotLoaded_DoesNotThrow()
        {
            var reference = new AddressableObjectReference<GameObject>("some_address");
            Assert.That(() => reference.Dispose(), Throws.Nothing);
        }
    }
#endif
}


