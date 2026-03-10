#nullable enable

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ObjectReference.Tests
{
    [ExcludeFromCoverage]
    [Ignore("This is a dummy class for testing purposes. It should not be included in the test suite.")]
    public class DummyObjectReferenceData : ScriptableObject
    {
        [SerializeReference]
        private IObjectReference<GameObject> _gameObjectReference = null!;

        [SerializeReference]
        private IObjectReference<Material> _materialReference = null!;

        public IObjectReference<GameObject> GameObjectReference => _gameObjectReference;
        public IObjectReference<Material> MaterialReference => _materialReference;

        public const string DirectDataPath = "Assets/Tests/Runtime/DummyData.asset";
        public const string EmptyDataPath = "Assets/Tests/Runtime/DummyEmptyData.asset";
        public const string AddressableDataPath = "Assets/Tests/Runtime/DummyAddressableData.asset";
        public static IEnumerable<string> AllImplementationPaths()
        {
            yield return DirectDataPath;
            yield return AddressableDataPath;
        }

        public static DummyObjectReferenceData[] LoadData()
        {
#if UNITY_EDITOR
            var data = UnityEditor.AssetDatabase.LoadAssetAtPath<DummyObjectReferenceData>(DirectDataPath);
            var addressableData = UnityEditor.AssetDatabase.LoadAssetAtPath<DummyObjectReferenceData>(AddressableDataPath);
            return new[] { data , addressableData };
#else
            throw new NotSupportedException("Test assets can only be loaded in the Unity Editor.");
#endif
        }

        public static DummyObjectReferenceData LoadEmptyData()
        {
#if UNITY_EDITOR
            var data = UnityEditor.AssetDatabase.LoadAssetAtPath<DummyObjectReferenceData>(EmptyDataPath);
            return data;
#else
            throw new NotSupportedException("Test assets can only be loaded in the Unity Editor.");
#endif
        }

        public enum DataIndex
        {
            DirectDataPath = 0,
            AddressableDataPath = 1
        }

    }
}
