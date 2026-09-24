using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fang.Framework.ConfigInspector.Editor
{
    /// <summary>
    /// 给 <see cref="ConfigDataSo"/> 引用字段加折叠箭头：展开后内联显示被引用配置资产的内容，可直接编辑。
    /// 只作用于声明类型为 <see cref="ConfigDataSo"/> 或其子类的字段，不接管其它 ScriptableObject。
    /// IMGUI 与 UI Toolkit 两条路径都带循环引用与最大深度守卫。
    /// </summary>
    [CustomPropertyDrawer(typeof(ConfigDataSo), true)]
    public sealed class ConfigDataSoFieldDrawer : PropertyDrawer
    {
        private const int MaxExpandDepth = 2;

        private const string ArrowCollapsed = "▶";
        private const string ArrowExpanded = "▼";
        private const string CircularText = "(circular reference)";
        private const string MaxDepthText = "(max depth reached)";

        private static readonly Color HintColor = new Color(0.65f, 0.65f, 0.65f);
        private static readonly Color WarnColor = new Color(1f, 0.8f, 0.3f);

        /// <summary>
        /// 当前正在展开的资产（instanceID）链。进出必须配对 —— 这是绘制期的递归守卫，
        /// 漏掉一次移除会让后续字段被误判成循环引用。
        /// </summary>
        private static readonly HashSet<int> s_expandedChain = new HashSet<int>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var fieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.ObjectField(fieldRect, property, label);

            var target = property.objectReferenceValue;
            if (target == null)
            {
                return;
            }

            property.isExpanded = EditorGUI.Foldout(fieldRect, property.isExpanded, GUIContent.none, true);
            if (!property.isExpanded)
            {
                return;
            }

            var contentRect = new Rect(
                position.x,
                fieldRect.yMax + EditorGUIUtility.standardVerticalSpacing,
                position.width,
                EditorGUIUtility.singleLineHeight);

            var instanceId = target.GetInstanceID();
            if (!TryEnter(instanceId, out var circular))
            {
                EditorGUI.LabelField(contentRect, circular ? CircularText : MaxDepthText);
                return;
            }

            try
            {
                var child = new SerializedObject(target);
                try
                {
                    child.Update();

                    var iterator = child.GetIterator();
                    if (!iterator.NextVisible(true))
                    {
                        return;
                    }

                    var area = contentRect;
                    EditorGUI.indentLevel++;
                    try
                    {
                        while (iterator.NextVisible(false))
                        {
                            var height = EditorGUI.GetPropertyHeight(iterator);
                            area.height = height;
                            EditorGUI.PropertyField(area, iterator);
                            area.y += height + EditorGUIUtility.standardVerticalSpacing;
                        }
                    }
                    finally
                    {
                        EditorGUI.indentLevel--;
                    }

                    child.ApplyModifiedProperties();
                }
                finally
                {
                    child.Dispose();
                }
            }
            finally
            {
                s_expandedChain.Remove(instanceId);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = EditorGUIUtility.singleLineHeight;

            var target = property.objectReferenceValue;
            if (target == null || !property.isExpanded)
            {
                return height;
            }

            height += EditorGUIUtility.standardVerticalSpacing;

            var instanceId = target.GetInstanceID();
            if (!TryEnter(instanceId, out _))
            {
                return height + EditorGUIUtility.singleLineHeight;
            }

            try
            {
                var child = new SerializedObject(target);
                try
                {
                    var iterator = child.GetIterator();
                    if (!iterator.NextVisible(true))
                    {
                        return height;
                    }

                    while (iterator.NextVisible(false))
                    {
                        height += EditorGUI.GetPropertyHeight(iterator) + EditorGUIUtility.standardVerticalSpacing;
                    }
                }
                finally
                {
                    child.Dispose();
                }
            }
            finally
            {
                s_expandedChain.Remove(instanceId);
            }

            return height;
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            var arrow = new Label(ArrowCollapsed);
            arrow.style.width = 12f;
            arrow.style.fontSize = 10f;
            arrow.style.color = HintColor;
            arrow.style.unityTextAlign = TextAnchor.MiddleCenter;
            arrow.style.marginRight = 2f;

            var nameLabel = new Label(property.displayName);
            nameLabel.style.minWidth = 120f;
            nameLabel.style.unityTextAlign = TextAnchor.MiddleLeft;

            var objectField = new ObjectField();
            objectField.objectType = ResolveObjectType();
            objectField.bindingPath = property.propertyPath;
            objectField.Bind(property.serializedObject);
            objectField.style.flexGrow = 1f;

            var content = new VisualElement();
            content.style.display = DisplayStyle.None;
            content.style.paddingLeft = 14f;
            content.style.marginTop = 4f;

            row.Add(arrow);
            row.Add(nameLabel);
            row.Add(objectField);
            root.Add(row);
            root.Add(content);

            SerializedObject child = null;
            var expanded = false;

            void ReleaseChild()
            {
                if (child == null)
                {
                    return;
                }

                child.ApplyModifiedProperties();
                child.Dispose();
                child = null;
            }

            void Collapse()
            {
                expanded = false;
                arrow.text = ArrowCollapsed;
                content.Clear();
                content.style.display = DisplayStyle.None;
                ReleaseChild();
            }

            void BuildContent()
            {
                content.Clear();
                ReleaseChild();

                var value = objectField.value;
                if (value == null)
                {
                    return;
                }

                var instanceId = value.GetInstanceID();
                if (!TryEnter(instanceId, out var circular))
                {
                    content.Add(BuildHint(
                        circular ? CircularText : MaxDepthText,
                        circular ? WarnColor : HintColor));
                    return;
                }

                try
                {
                    child = new SerializedObject(value);

                    var iterator = child.GetIterator();
                    if (!iterator.NextVisible(true))
                    {
                        return;
                    }

                    while (iterator.NextVisible(false))
                    {
                        var field = new PropertyField(iterator);
                        field.Bind(child);
                        content.Add(field);
                    }
                }
                finally
                {
                    s_expandedChain.Remove(instanceId);
                }
            }

            void Toggle()
            {
                if (expanded)
                {
                    Collapse();
                    return;
                }

                expanded = true;
                arrow.text = ArrowExpanded;
                content.style.display = DisplayStyle.Flex;
                BuildContent();
            }

            arrow.AddManipulator(new Clickable(Toggle));
            nameLabel.AddManipulator(new Clickable(Toggle));

            objectField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue == null)
                {
                    Collapse();
                }
                else if (expanded)
                {
                    BuildContent();
                }
            });

            root.RegisterCallback<DetachFromPanelEvent>(_ => Collapse());

            return root;
        }

        /// <summary>
        /// 进入展开链。已被链上某个祖先引用（循环）或已达最大深度时返回 false。
        /// </summary>
        private static bool TryEnter(int instanceId, out bool circular)
        {
            circular = s_expandedChain.Contains(instanceId);

            if (circular || s_expandedChain.Count >= MaxExpandDepth)
            {
                return false;
            }

            s_expandedChain.Add(instanceId);
            return true;
        }

        /// <summary>
        /// 引用字段的声明类型。列表 / 数组元素拿不到元素类型，退化成 <see cref="ConfigDataSo"/>。
        /// </summary>
        private System.Type ResolveObjectType()
        {
            var declared = fieldInfo?.FieldType;
            if (declared != null && typeof(ConfigDataSo).IsAssignableFrom(declared))
            {
                return declared;
            }

            return typeof(ConfigDataSo);
        }

        private static Label BuildHint(string text, Color color)
        {
            var label = new Label(text);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.fontSize = 11f;
            label.style.unityFontStyleAndWeight = FontStyle.Italic;
            label.style.color = color;
            return label;
        }
    }
}
