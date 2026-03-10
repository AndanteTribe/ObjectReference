#nullable enable

using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ObjectReference.Tests
{
    /// <summary>
    /// Tests for all <see cref="IObjectReference{T}"/> implementations via the [SerializeReference] workflow.
    /// Common load/dispose behaviour is parameterized over both SerializableObjectReference and
    /// SerializableAddressableObjectReference assets using <see cref="ValueSourceAttribute"/>.
    /// </summary>
    public class ObjectReferenceTests
    {
        private static readonly DummyObjectReferenceData[] s_testData;

        static ObjectReferenceTests() => s_testData = DummyObjectReferenceData.LoadData();

        // ---- Parameterized tests covering both implementations ----

        [UnityTest]
        public IEnumerator LoadAsync_GameObjectReference_ReturnsCube(
            [ValueSource(nameof(s_testData))] DummyObjectReferenceData data) => new ToCoroutineEnumerator(async () =>
        {
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
            [ValueSource(nameof(s_testData))] DummyObjectReferenceData data) => new ToCoroutineEnumerator(async () =>
        {
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
            [ValueSource(nameof(s_testData))] DummyObjectReferenceData data) => new ToCoroutineEnumerator(async () =>
        {
            var lastProgress = -1f;
            var progress = new Progress<float>(v => lastProgress = v);
            try
            {
                var result = await data.GameObjectReference.LoadAsync(progress, CancellationToken.None);
                Assert.That(result, Is.Not.Null);
                await UniTask.NextFrame();
                Assert.That(lastProgress, Is.EqualTo(1.0f).Within(0.001f));
            }
            finally
            {
                data.GameObjectReference.Dispose();
            }
        });

        [UnityTest]
        public IEnumerator LoadAsync_WithCancelledToken_ThrowsOperationCanceledException(
            [ValueSource(nameof(s_testData))] DummyObjectReferenceData data) => new ToCoroutineEnumerator(async () =>
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var caught = false;
            try
            {
                await data.GameObjectReference.LoadAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                caught = true;
            }
            Assert.That(caught, Is.True);
        });

        [UnityTest]
        public IEnumerator Dispose_AfterLoad_DoesNotThrow(
            [ValueSource(nameof(s_testData))] DummyObjectReferenceData data) => new ToCoroutineEnumerator(async () =>
        {
            await data.GameObjectReference.LoadAsync(CancellationToken.None);
            Assert.That(() => data.GameObjectReference.Dispose(), Throws.Nothing);
        });

        [UnityTest]
        public IEnumerator Dispose_WithoutLoad_DoesNotThrow(
            [ValueSource(nameof(s_testData))] DummyObjectReferenceData data) => new ToCoroutineEnumerator(() =>
        {
            Assert.That(() => data.GameObjectReference.Dispose(), Throws.Nothing);
            return default;
        });

        // ---- SerializableObjectReference-specific ----

        [UnityTest]
        public IEnumerator LoadAsync_EmptyReference_ThrowsNullReferenceException() => new ToCoroutineEnumerator(async () =>
        {
            var data = DummyObjectReferenceData.LoadEmptyData();
            var caught = false;
            try
            {
                await data.GameObjectReference.LoadAsync(CancellationToken.None);
            }
            catch (NullReferenceException)
            {
                caught = true;
            }
            Assert.That(caught, Is.True);
        });

        // ---- SerializableAddressableObjectReference-specific ----

        [UnityTest]
        public IEnumerator LoadAsync_Addressable_SecondCall_ReturnsCachedValue() => new ToCoroutineEnumerator(async () =>
        {
            var data = s_testData[(int)DummyObjectReferenceData.DataIndex.AddressableDataPath];
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
    }

    /// <summary>
    /// Tests for the public <see cref="AddressableObjectReference{T}"/> (string-address based).
    /// Tests focus on non-Addressables-dependent behavior (constructor, cancellation, dispose).
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
        public IEnumerator LoadAsync_WithCancelledToken_ThrowsOperationCanceledException() => new ToCoroutineEnumerator(async () =>
        {
            using var reference = new AddressableObjectReference<GameObject>("some_address");
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var caught = false;
            try
            {
                await reference.LoadAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                caught = true;
            }
            Assert.That(caught, Is.True);
        });

        [UnityTest]
        public IEnumerator LoadAsync_WithProgressAndCancelledToken_ThrowsOperationCanceledException() => new ToCoroutineEnumerator(async () =>
        {
            using var reference = new AddressableObjectReference<GameObject>("some_address");
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var caught = false;
            try
            {
                await reference.LoadAsync(new Progress<float>(_ => { }), cts.Token);
            }
            catch (OperationCanceledException)
            {
                caught = true;
            }
            Assert.That(caught, Is.True);
        });


        [Test]
        public void Dispose_WhenNotLoaded_DoesNotThrow()
        {
            var reference = new AddressableObjectReference<GameObject>("some_address");
            Assert.That(() => reference.Dispose(), Throws.Nothing);
        }

        [Test]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            var reference = new AddressableObjectReference<GameObject>("some_address");
            Assert.That(() =>
            {
                reference.Dispose();
                reference.Dispose();
            }, Throws.Nothing);
        }
    }
}


