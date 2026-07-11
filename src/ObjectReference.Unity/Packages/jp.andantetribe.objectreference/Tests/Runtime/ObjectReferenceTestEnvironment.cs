#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;

namespace ObjectReference.Tests
{
    internal sealed class ObjectReferenceTestEnvironment : IDisposable
    {
        public const string CubeAddress = "objectreference-tests-cube";
        public const string MaterialAddress = "objectreference-tests-material";
        public const string CubeGuid = "11111111111111111111111111111111";
        public const string MaterialGuid = "22222222222222222222222222222222";

        private const BindingFlags InstanceFieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private readonly FieldInfo _addressablesInstanceField;
        private readonly object _previousAddressablesInstance;
        private readonly object _testAddressablesInstance;
        private readonly InMemoryObjectProvider _provider;
        private readonly ResourceLocationMap _locator;

        public ObjectReferenceTestEnvironment()
        {
            Cube = new GameObject("Cube");
            var shader = Shader.Find("Hidden/InternalErrorShader")
                ?? throw new InvalidOperationException("The built-in error shader is unavailable.");
            Material = new Material(shader) { name = "New Material" };

            _addressablesInstanceField = typeof(Addressables).GetField(
                "m_AddressablesInstance",
                BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new MissingFieldException(typeof(Addressables).FullName, "m_AddressablesInstance");
            _previousAddressablesInstance = _addressablesInstanceField.GetValue(null)
                ?? throw new InvalidOperationException("The Addressables instance is unavailable.");

            var addressablesImplType = _previousAddressablesInstance.GetType();
            _testAddressablesInstance = Activator.CreateInstance(
                addressablesImplType,
                new LRUCacheAllocationStrategy(1000, 1000, 100, 10))
                ?? throw new InvalidOperationException("Could not create a test Addressables instance.");
            _addressablesInstanceField.SetValue(null, _testAddressablesInstance);

            SetAddressablesField(addressablesImplType, "hasStartedInitialization", true);
            SetAddressablesField(
                addressablesImplType,
                "m_InitializationOperation",
                default(AsyncOperationHandle<IResourceLocator>));
            SetAddressablesField(
                addressablesImplType,
                "m_OnHandleCompleteAction",
                new Action<AsyncOperationHandle>(_ => { }));

            _provider = new InMemoryObjectProvider(new Dictionary<string, UnityEngine.Object>
            {
                [CubeAddress] = Cube,
                [MaterialAddress] = Material,
                [CubeGuid] = Cube,
                [MaterialGuid] = Material,
            });
            Addressables.ResourceManager.ResourceProviders.Add(_provider);

            _locator = new ResourceLocationMap("ObjectReferenceTests", 4);
            AddLocation(CubeAddress, typeof(GameObject));
            AddLocation(MaterialAddress, typeof(Material));
            AddLocation(CubeGuid, typeof(GameObject));
            AddLocation(MaterialGuid, typeof(Material));
            Addressables.AddResourceLocator(_locator);
        }

        public GameObject Cube { get; }

        public Material Material { get; }

        public IObjectReference<T> CreateSerializableReference<T>(T value)
            where T : UnityEngine.Object => CreateSerializableReference<T>(
                "SerializableObjectReference`1",
                value);

        public IObjectReference<T> CreateSerializableAddressableReference<T>(string guid)
            where T : UnityEngine.Object => CreateSerializableReference<T>(
                "SerializableAddressableObjectReference`1",
                new AssetReferenceT<T>(guid));

        public void Dispose()
        {
            Addressables.RemoveResourceLocator(_locator);
            Addressables.ResourceManager.ResourceProviders.Remove(_provider);

            var releaseMethod = _testAddressablesInstance.GetType().GetMethod(
                "ReleaseSceneManagerOperation",
                InstanceFieldFlags);
            releaseMethod?.Invoke(_testAddressablesInstance, null);
            _addressablesInstanceField.SetValue(null, _previousAddressablesInstance);

            UnityEngine.Object.DestroyImmediate(Cube);
            UnityEngine.Object.DestroyImmediate(Material);
        }

        private IObjectReference<T> CreateSerializableReference<T>(string genericTypeName, object? value)
            where T : UnityEngine.Object
        {
            var openType = typeof(IObjectReference<>).Assembly.GetType(
                $"ObjectReference.{genericTypeName}",
                throwOnError: true)!;
            var closedType = openType.MakeGenericType(typeof(T));
            var instance = Activator.CreateInstance(closedType, nonPublic: true)
                ?? throw new InvalidOperationException($"Could not create {closedType.FullName}.");
            var valueField = closedType.GetField("_value", InstanceFieldFlags)
                ?? throw new MissingFieldException(closedType.FullName, "_value");
            valueField.SetValue(instance, value);
            return (IObjectReference<T>)instance;
        }

        private void AddLocation(string key, Type resourceType)
        {
            _locator.Add(key, new ResourceLocationBase(key, key, _provider.ProviderId, resourceType));
        }

        private void SetAddressablesField(Type addressablesImplType, string name, object value)
        {
            var field = addressablesImplType.GetField(name, InstanceFieldFlags)
                ?? throw new MissingFieldException(addressablesImplType.FullName, name);
            field.SetValue(_testAddressablesInstance, value);
        }
    }

    internal sealed class InMemoryObjectProvider : ResourceProviderBase
    {
        private readonly IReadOnlyDictionary<string, UnityEngine.Object> _objects;

        public InMemoryObjectProvider(IReadOnlyDictionary<string, UnityEngine.Object> objects)
        {
            _objects = objects;
        }

        public override Type GetDefaultType(IResourceLocation location) => location.ResourceType;

        public override bool CanProvide(Type type, IResourceLocation location) =>
            type.IsAssignableFrom(location.ResourceType);

        public override void Provide(ProvideHandle provideHandle) => CompleteAsync(provideHandle).Forget();

        public override void Release(IResourceLocation location, object asset)
        {
        }

        private async UniTaskVoid CompleteAsync(ProvideHandle provideHandle)
        {
            await UniTask.Yield();
            var key = provideHandle.Location.PrimaryKey;
            if (!_objects.TryGetValue(key, out var value))
            {
                provideHandle.Complete<object>(
                    null!,
                    status: false,
                    new InvalidOperationException($"No in-memory object is registered for '{key}'."));
                return;
            }

            provideHandle.Complete(value, status: true, exception: null);
        }
    }
}