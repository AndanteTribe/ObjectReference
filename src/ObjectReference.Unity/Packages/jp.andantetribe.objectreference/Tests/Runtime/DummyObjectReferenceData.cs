#nullable enable

using NUnit.Framework;
using UnityEngine;

namespace ObjectReference.Tests
{
    [Ignore("This is a dummy class for testing purposes. It should not be included in the test suite.")]
    public class DummyObjectReferenceData : ScriptableObject
    {
        [SerializeReference]
        private IObjectReference<GameObject> _gameObjectReference = null!;

        [SerializeReference]
        private IObjectReference<Material> _materialReference = null!;

        public IObjectReference<GameObject> GameObjectReference => _gameObjectReference;
        public IObjectReference<Material> MaterialReference => _materialReference;

        public static DummyObjectReferenceData Load(string path)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<DummyObjectReferenceData>(path);
#else
            throw new System.NotSupportedException("DummyObjectReferenceData can only be loaded in the Unity Editor.");
#endif
        }
    }
}
