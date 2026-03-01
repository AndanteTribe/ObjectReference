#nullable enable

using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ObjectReference.Editor
{
    [CustomPropertyDrawer(typeof(IObjectReference<>), true)]
    public class ObjectReferenceDrawer : PropertyDrawer
    {
        private static readonly Lazy<Texture2D> s_settingIcon = new(static () =>
            (Texture2D)EditorGUIUtility.Load("SettingsIcon"));

        private static readonly Lazy<Type[]> s_objectReferenceTypes = new(static () =>
        {
            return TypeCache.GetTypesDerivedFrom(typeof(IObjectReference<>))
                .Where(static t => t.IsDefined(typeof(SerializableAttribute), false)).ToArray();
        });

        private static readonly Lazy<Type[]> s_unityObjectTypes = new(static () =>
            TypeCache.GetTypesDerivedFrom(typeof(UnityEngine.Object)).Append(typeof(UnityEngine.Object)).ToArray());

        /// <inheritdoc />
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = UIElementUtils.CreateBox(property.displayName);

            // SerializeReferenceの時だけ表示
            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                SetOptionButton(root, property);
            }

            var valueProperty = property.FindPropertyRelative("_value");
            if (valueProperty != null)
            {
                var field = new PropertyField(valueProperty, "Value");
                root.Add(field);
                field.RegisterCallback<SerializedPropertyChangeEvent>(static evt =>
                {
                    var root = ((VisualElement)evt.currentTarget).parent;
                    var property = evt.changedProperty;
                     AddWarningIfNotPrefab(root, property);
                });
            }

            return root;
        }

        private static void SetOptionButton(VisualElement root, SerializedProperty property)
        {
            var opBtn = new Button()
            {
                style =
                {
                    height = 15,
                    width = 15,
                    left = 5,
                    backgroundImage = s_settingIcon.Value,
                    backgroundColor = Color.white
                }
            };
            opBtn.RegisterCallback<ClickEvent, SerializedProperty>(static (evt, property) =>
            {
                var btn = (Button)evt.currentTarget;
                var genericDropdown = new GenericDropdownMenu();
                foreach (var type in s_objectReferenceTypes.Value)
                {
                    genericDropdown.AddItem(type.Name.AsSpan()[..^5].ToString(), false, static d =>
                    {
                        using var data = (DropDownData)d;
                        var type = data.Type;
                        var property = data.Property;
                        var genericType = GetGenericType(property);

                        property.managedReferenceValue = Activator.CreateInstance(type.MakeGenericType(genericType));
                        property.serializedObject.ApplyModifiedProperties();
                    }, DropDownData.Create(type, property));
                }

#if UNITY_6000_3_OR_NEWER
                genericDropdown.DropDown(btn.worldBound, btn, DropdownMenuSizeMode.Content);
#else
                genericDropdown.DropDown(btn.worldBound, btn, false);
#endif
            }, property);

            var label = root.Q<Label>(property.displayName);
            label.style.flexDirection = FlexDirection.RowReverse;
            label.Add(opBtn);
        }

        protected static Type GetGenericType(SerializedProperty property)
        {
            var genericType = property.managedReferenceValue?.GetType().GetGenericArguments()[0];

            // managedReferenceValueがnullの時は型が取得できないので、無理矢理文字列から型判定.
            if (genericType == null)
            {
                var fieldName = property.managedReferenceFieldTypename.AsSpan();
                var i = fieldName.IndexOf(stackalloc char[]{ '[', '[' });
                var typeName = fieldName.Slice(i + 2);
                typeName = typeName[..typeName.IndexOf(',')];
                genericType = Type.GetType(typeName.ToString());
                if (genericType == null)
                {
                    foreach (var unityType in s_unityObjectTypes.Value)
                    {
                        if (typeName.SequenceEqual(unityType.FullName))
                        {
                            genericType = unityType;
                            break;
                        }
                    }
                }
            }

            return genericType ?? throw new InvalidOperationException("Failed to get generic type.");
        }

        protected static void AddWarningIfNotPrefab(VisualElement root, SerializedProperty valueProperty)
        {
            const string warningClassName = "object-ref-warning";
            foreach (var warning in root.Query<HelpBox>(className: warningClassName).Build())
            {
                warning.RemoveFromHierarchy();
            }

            var obj = valueProperty.objectReferenceValue;
            if (obj == null)
            {
                return;
            }

            var go = obj switch
            {
                GameObject g => g,
                Component c => c.gameObject,
                _ => null
            };

            if (go == null)
            {
                return;
            }

            // Prefabアセットかどうか判定.
            if (!PrefabUtility.IsPartOfPrefabAsset(go))
            {
                var helpBox = new HelpBox(
                    "You are referencing a Scene object. When referencing objects other than Prefabs, consider using SerializeField.",
                    HelpBoxMessageType.Warning);
                helpBox.AddToClassList(warningClassName);
                root.Add(helpBox);
            }
        }

        private sealed class DropDownData : IDisposable
        {
            public Type Type { get; private set; }
            public SerializedProperty Property { get; private set; }

            private static DropDownData? s_head;
            private DropDownData? _next;

            private DropDownData(Type type, SerializedProperty property)
            {
                Type = type;
                Property = property;
            }

            public static DropDownData Create(Type type, SerializedProperty property)
            {
                if (s_head == null)
                {
                    return s_head = new DropDownData(type, property);
                }

                var result = s_head;
                s_head = s_head._next;
                result._next = null;
                result.Type = type;
                result.Property = property;
                return result;
            }

            public void Dispose()
            {
                Type = null!;
                Property = null!;
                _next = s_head;
                s_head = this;
            }
        }
    }
}