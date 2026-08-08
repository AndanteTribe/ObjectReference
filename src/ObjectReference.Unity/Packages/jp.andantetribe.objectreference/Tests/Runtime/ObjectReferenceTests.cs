#nullable enable

using System;
using System.Collections;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ObjectReference.Tests
{
    public abstract class ObjectReferenceTestFixture
    {
        private protected ObjectReferenceTestEnvironment Environment { get; private set; } = null!;

        [SetUp]
        public void SetUp() => Environment = new ObjectReferenceTestEnvironment();

        [TearDown]
        public void TearDown() => Environment.Dispose();
    }

    public class ObjectReferenceTests : ObjectReferenceTestFixture
    {
        private static readonly ReferenceKind[] s_referenceKinds =
        {
            ReferenceKind.Direct,
            ReferenceKind.Addressable,
        };

        [UnityTest]
        public IEnumerator LoadAsync_GameObjectReference_ReturnsCube(
            [ValueSource(nameof(s_referenceKinds))] ReferenceKind kind) => new ToCoroutineEnumerator(async () =>
        {
            using var reference = CreateGameObjectReference(kind);
            var result = await reference.LoadAsync(CancellationToken.None);
            Assert.That(result, Is.SameAs(Environment.Cube));
            Assert.That(result.name, Is.EqualTo("Cube"));
        });

        [UnityTest]
        public IEnumerator LoadAsync_MaterialReference_ReturnsMaterial(
            [ValueSource(nameof(s_referenceKinds))] ReferenceKind kind) => new ToCoroutineEnumerator(async () =>
        {
            using var reference = CreateMaterialReference(kind);
            var result = await reference.LoadAsync(CancellationToken.None);
            Assert.That(result, Is.SameAs(Environment.Material));
            Assert.That(result.name, Is.EqualTo("New Material"));
        });

        [UnityTest]
        public IEnumerator LoadAsync_WithProgress_ReportsAndReturnsAsset(
            [ValueSource(nameof(s_referenceKinds))] ReferenceKind kind) => new ToCoroutineEnumerator(async () =>
        {
            using var reference = CreateGameObjectReference(kind);
            var progress = new ProgressRecorder();
            var result = await reference.LoadAsync(progress, CancellationToken.None);
            Assert.That(result, Is.SameAs(Environment.Cube));
            Assert.That(progress.Value, Is.EqualTo(1.0f).Within(0.001f));
        });

        [UnityTest]
        public IEnumerator LoadAsync_WithCancelledToken_ThrowsOperationCanceledException(
            [ValueSource(nameof(s_referenceKinds))] ReferenceKind kind) => new ToCoroutineEnumerator(async () =>
        {
            using var reference = CreateGameObjectReference(kind);
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
        public IEnumerator Dispose_AfterLoad_DoesNotThrow(
            [ValueSource(nameof(s_referenceKinds))] ReferenceKind kind) => new ToCoroutineEnumerator(async () =>
        {
            var reference = CreateGameObjectReference(kind);
            await reference.LoadAsync(CancellationToken.None);
            Assert.That(() => reference.Dispose(), Throws.Nothing);
        });

        [Test]
        public void Dispose_WithoutLoad_DoesNotThrow(
            [ValueSource(nameof(s_referenceKinds))] ReferenceKind kind)
        {
            var reference = CreateGameObjectReference(kind);
            Assert.That(() => reference.Dispose(), Throws.Nothing);
        }

        [Test]
        public void LoadAsync_SerializableObjectReference_NullValue_ThrowsNullReferenceException()
        {
            using var reference = Environment.CreateSerializableReference<GameObject>(null!);
            Assert.Throws<NullReferenceException>(() => reference.LoadAsync(CancellationToken.None));
        }

        [Test]
        public void LoadAsync_WithProgress_SerializableObjectReference_NullValue_ThrowsNullReferenceException()
        {
            using var reference = Environment.CreateSerializableReference<GameObject>(null!);
            Assert.Throws<NullReferenceException>(() =>
                reference.LoadAsync(new ProgressRecorder(), CancellationToken.None));
        }

        [UnityTest]
        public IEnumerator LoadAsync_Addressable_SecondCall_ReturnsCachedValue(
            [Values(false, true)] bool withProgress) => new ToCoroutineEnumerator(async () =>
        {
            using var reference = Environment.CreateSerializableAddressableReference<GameObject>(
                ObjectReferenceTestEnvironment.CubeGuid);
            var first = await reference.LoadAsync(CancellationToken.None);
            var progress = new ProgressRecorder();
            var second = withProgress
                ? await reference.LoadAsync(progress, CancellationToken.None)
                : await reference.LoadAsync(CancellationToken.None);
            Assert.That(first, Is.SameAs(Environment.Cube));
            Assert.That(second, Is.SameAs(first));
            if (withProgress)
            {
                Assert.That(progress.Value, Is.EqualTo(1.0f).Within(0.001f));
            }
        });

        [UnityTest]
        public IEnumerator LoadAsync_Addressable_CachedProgressThrows_ReleasesHandle() => new ToCoroutineEnumerator(async () =>
        {
            using var reference = Environment.CreateSerializableAddressableReference<GameObject>(
                ObjectReferenceTestEnvironment.CubeGuid);
            await reference.LoadAsync(CancellationToken.None);

            Exception? caught = null;
            try
            {
                await reference.LoadAsync(new ThrowingProgress(), CancellationToken.None);
            }
            catch (Exception exception)
            {
                caught = exception;
            }

            Assert.That(caught, Is.TypeOf<InvalidOperationException>());
            Assert.That(Environment.HasValidOperationHandle(reference), Is.False);
        });

        [UnityTest]
        public IEnumerator LoadAsync_Addressable_WhenLoadingFails_ReleasesHandle(
            [Values(false, true)] bool withProgress) => new ToCoroutineEnumerator(async () =>
        {
            using var reference = Environment.CreateSerializableAddressableReference<GameObject>(
                ObjectReferenceTestEnvironment.MissingGuid);
            Exception? caught = null;
            try
            {
                if (withProgress)
                {
                    await reference.LoadAsync(new ProgressRecorder(), CancellationToken.None);
                }
                else
                {
                    await reference.LoadAsync(CancellationToken.None);
                }
            }
            catch (Exception exception)
            {
                caught = exception;
            }

            Assert.That(caught, Is.TypeOf<InvalidOperationException>());
            Assert.That(Environment.HasValidOperationHandle(reference), Is.False);
        });

        private IObjectReference<GameObject> CreateGameObjectReference(ReferenceKind kind) => kind switch
        {
            ReferenceKind.Direct => Environment.CreateSerializableReference(Environment.Cube),
            ReferenceKind.Addressable => Environment.CreateSerializableAddressableReference<GameObject>(
                ObjectReferenceTestEnvironment.CubeGuid),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };

        private IObjectReference<Material> CreateMaterialReference(ReferenceKind kind) => kind switch
        {
            ReferenceKind.Direct => Environment.CreateSerializableReference(Environment.Material),
            ReferenceKind.Addressable => Environment.CreateSerializableAddressableReference<Material>(
                ObjectReferenceTestEnvironment.MaterialGuid),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };

        public enum ReferenceKind
        {
            Direct,
            Addressable,
        }
    }

    public class AddressableObjectReferenceTests : ObjectReferenceTestFixture
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
                await reference.LoadAsync(new ProgressRecorder(), cts.Token);
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

        [UnityTest]
        public IEnumerator LoadAsync_WithValidAddress_ReturnsGameObject() => new ToCoroutineEnumerator(async () =>
        {
            using var reference = new AddressableObjectReference<GameObject>(
                ObjectReferenceTestEnvironment.CubeAddress);
            var result = await reference.LoadAsync(CancellationToken.None);
            Assert.That(result, Is.SameAs(Environment.Cube));
            Assert.That(result.name, Is.EqualTo("Cube"));
        });

        [UnityTest]
        public IEnumerator LoadAsync_SecondCall_ReusesCachedHandle(
            [Values(false, true)] bool withProgress) => new ToCoroutineEnumerator(async () =>
        {
            using var reference = new AddressableObjectReference<GameObject>(
                ObjectReferenceTestEnvironment.CubeAddress);
            var first = await reference.LoadAsync(CancellationToken.None);
            var progress = new ProgressRecorder();
            var second = withProgress
                ? await reference.LoadAsync(progress, CancellationToken.None)
                : await reference.LoadAsync(CancellationToken.None);
            Assert.That(first, Is.SameAs(Environment.Cube));
            Assert.That(second, Is.SameAs(first));
            if (withProgress)
            {
                Assert.That(progress.Value, Is.EqualTo(1.0f).Within(0.001f));
            }
        });

        [UnityTest]
        public IEnumerator LoadAsync_CachedProgressThrows_ReleasesHandle() => new ToCoroutineEnumerator(async () =>
        {
            using var reference = new AddressableObjectReference<GameObject>(
                ObjectReferenceTestEnvironment.CubeAddress);
            await reference.LoadAsync(CancellationToken.None);

            Exception? caught = null;
            try
            {
                await reference.LoadAsync(new ThrowingProgress(), CancellationToken.None);
            }
            catch (Exception exception)
            {
                caught = exception;
            }

            Assert.That(caught, Is.TypeOf<InvalidOperationException>());
            Assert.That(Environment.HasValidOperationHandle(reference), Is.False);
        });

        [UnityTest]
        public IEnumerator LoadAsync_WithProgress_ValidAddress_ReturnsGameObject() => new ToCoroutineEnumerator(async () =>
        {
            using var reference = new AddressableObjectReference<GameObject>(
                ObjectReferenceTestEnvironment.CubeAddress);
            var progress = new ProgressRecorder();
            var result = await reference.LoadAsync(progress, CancellationToken.None);
            Assert.That(result, Is.SameAs(Environment.Cube));
            Assert.That(progress.Value, Is.EqualTo(1.0f).Within(0.001f));
        });

        [UnityTest]
        public IEnumerator LoadAsync_WhenLoadingFails_ReleasesHandle(
            [Values(false, true)] bool withProgress) => new ToCoroutineEnumerator(async () =>
        {
            using var reference = new AddressableObjectReference<GameObject>(
                ObjectReferenceTestEnvironment.MissingAddress);
            Exception? caught = null;
            try
            {
                if (withProgress)
                {
                    await reference.LoadAsync(new ProgressRecorder(), CancellationToken.None);
                }
                else
                {
                    await reference.LoadAsync(CancellationToken.None);
                }
            }
            catch (Exception exception)
            {
                caught = exception;
            }

            Assert.That(caught, Is.TypeOf<InvalidOperationException>());
            Assert.That(Environment.HasValidOperationHandle(reference), Is.False);
        });

        [UnityTest]
        public IEnumerator Dispose_AfterLoad_ReleasesHandle() => new ToCoroutineEnumerator(async () =>
        {
            var reference = new AddressableObjectReference<GameObject>(
                ObjectReferenceTestEnvironment.CubeAddress);
            await reference.LoadAsync(CancellationToken.None);
            Assert.That(() => reference.Dispose(), Throws.Nothing);
            Assert.That(() => reference.Dispose(), Throws.Nothing);
        });
    }

    internal sealed class ProgressRecorder : IProgress<float>
    {
        public float Value { get; private set; } = -1.0f;

        public void Report(float value) => Value = value;
    }

    internal sealed class ThrowingProgress : IProgress<float>
    {
        public void Report(float value) => throw new InvalidOperationException("Progress reporting failed.");
    }
}
