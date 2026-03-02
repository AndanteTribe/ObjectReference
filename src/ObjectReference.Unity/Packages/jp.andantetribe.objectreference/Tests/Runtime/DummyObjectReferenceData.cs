#nullable enable

using NUnit.Framework;
using UnityEngine;

namespace ObjectReference.Tests
{
    [Ignore("This is a dummy class for testing purposes. It should not be included in the test suite.")]
    public class DummyObjectReferenceData : ScriptableObject
    {
        [SerializeReference]
        private IObjectReference<GameObject> _prefabReference = null!;

        [SerializeReference]
        private IObjectReference<Material> _materialReference = null!;

        [SerializeReference]
        private IObjectReference<GameObject> _addressablePrefabReference = null!;

        [SerializeReference]
        private IObjectReference<Material> _addressableMaterialReference = null!;

        public IObjectReference<GameObject> PrefabReference => _prefabReference;
        public IObjectReference<Material> MaterialReference => _materialReference;

#if ENABLE_ADDRESSABLES && ENABLE_UNITASK
        public IObjectReference<GameObject> AddressablePrefabReference => _addressablePrefabReference;
        public IObjectReference<Material> AddressableMaterialReference => _addressableMaterialReference;
#endif

        public static DummyObjectReferenceData Load()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<DummyObjectReferenceData>(
                "Packages/jp.andantetribe.objectreference/Tests/Runtime/DummyData.asset");
#else
            throw new System.NotSupportedException("DummyObjectReferenceData can only be loaded in the Unity Editor.");
#endif
        }
    }
}
