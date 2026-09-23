using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fang.Framework.UI.Kit.Editor
{
    /// <summary>
    /// TokenMatch 的条目编辑：id 可手填，也可以点「选择…」按「项目 ▸ token 类型 ▸ MatchId」挑。
    /// 写进条目的始终是 id 字符串本身 —— 预制体与项目 SO 之间不建立引用。
    /// </summary>
    [CustomEditor(typeof(TokenMatch))]
    internal sealed class TokenMatchEditor : UnityEditor.Editor
    {
        private const string EntriesField = "_entries";

        private static readonly Color DimColor = new Color(0.65f, 0.65f, 0.65f);
        private static readonly Color LineColor = new Color(0.28f, 0.28f, 0.28f);

        private VisualElement root;

        public override VisualElement CreateInspectorGUI()
        {
            root = new VisualElement();
            Rebuild();
            return root;
        }

        private void Rebuild()
        {
            if (root == null)
            {
                return;
            }

            root.Clear();

            if (serializedObject == null || serializedObject.targetObject == null)
            {
                return;
            }

            serializedObject.Update();

            var hint = new Label("每条 = 匹配 id（MatchId）+ target 组件。id 可手填，也可以点「选择…」从某个控件库项目里挑。");
            hint.style.whiteSpace = WhiteSpace.Normal;
            hint.style.color = DimColor;
            hint.style.marginBottom = 6f;
            root.Add(hint);

            var entries = serializedObject.FindProperty(EntriesField);
            if (entries == null)
            {
                return;
            }

            for (var i = 0; i < entries.arraySize; i++)
            {
                root.Add(BuildEntryRow(i));
            }

            if (entries.arraySize == 0)
            {
                var empty = new Label("还没有条目。");
                empty.style.color = DimColor;
                root.Add(empty);
            }

            var separator = new VisualElement();
            separator.style.borderTopWidth = 1f;
            separator.style.borderTopColor = LineColor;
            separator.style.marginTop = 6f;
            separator.style.marginBottom = 4f;
            root.Add(separator);

            var add = new Button(AddEntry) { text = "＋ 添加条目" };
            add.style.alignSelf = Align.FlexStart;
            root.Add(add);
        }

        private VisualElement BuildEntryRow(int index)
        {
            var entries = serializedObject.FindProperty(EntriesField);
            var entry = entries.GetArrayElementAtIndex(index);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 2f;

            var idField = new TextField { value = entry.FindPropertyRelative("_id").stringValue ?? string.Empty };
            idField.style.flexGrow = 1f;
            idField.style.flexShrink = 1f;
            idField.RegisterValueChangedCallback(evt => SetId(index, evt.newValue));
            row.Add(idField);

            var pick = new Button(() => ShowIdMenu(index, idField)) { text = "选择…" };
            pick.style.flexShrink = 0f;
            pick.style.marginLeft = 4f;
            row.Add(pick);

            var targetField = new ObjectField { objectType = typeof(Component), allowSceneObjects = true };
            targetField.value = entry.FindPropertyRelative("_target").objectReferenceValue;
            targetField.style.width = 170f;
            targetField.style.flexShrink = 0f;
            targetField.style.marginLeft = 4f;
            targetField.RegisterValueChangedCallback(evt => SetTarget(index, evt.newValue));
            row.Add(targetField);

            var remove = new Button(() => RemoveEntry(index)) { text = "－" };
            remove.style.flexShrink = 0f;
            remove.style.marginLeft = 4f;
            row.Add(remove);

            return row;
        }

        private void SetId(int index, string value)
        {
            if (!TryGetEntry(index, out var entry))
            {
                return;
            }

            entry.FindPropertyRelative("_id").stringValue = value ?? string.Empty;
            serializedObject.ApplyModifiedProperties();
        }

        private void SetTarget(int index, UnityEngine.Object value)
        {
            if (!TryGetEntry(index, out var entry))
            {
                return;
            }

            entry.FindPropertyRelative("_target").objectReferenceValue = value;
            serializedObject.ApplyModifiedProperties();
        }

        private void AddEntry()
        {
            serializedObject.Update();

            var entries = serializedObject.FindProperty(EntriesField);
            entries.arraySize++;
            var entry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
            entry.FindPropertyRelative("_id").stringValue = string.Empty;
            entry.FindPropertyRelative("_target").objectReferenceValue = null;
            serializedObject.ApplyModifiedProperties();

            Rebuild();
        }

        private void RemoveEntry(int index)
        {
            serializedObject.Update();

            var entries = serializedObject.FindProperty(EntriesField);
            if (index >= entries.arraySize)
            {
                return;
            }

            var sizeBefore = entries.arraySize;
            entries.DeleteArrayElementAtIndex(index);
            if (entries.arraySize == sizeBefore)
            {
                entries.DeleteArrayElementAtIndex(index);
            }

            serializedObject.ApplyModifiedProperties();

            Rebuild();
        }

        private bool TryGetEntry(int index, out SerializedProperty entry)
        {
            entry = null;

            if (serializedObject == null || serializedObject.targetObject == null)
            {
                return false;
            }

            serializedObject.Update();

            var entries = serializedObject.FindProperty(EntriesField);
            if (entries == null || index < 0 || index >= entries.arraySize)
            {
                return false;
            }

            entry = entries.GetArrayElementAtIndex(index);
            return true;
        }

        private void ShowIdMenu(int index, VisualElement anchor)
        {
            var menu = new GenericMenu();
            var current = CurrentId(index);
            var seen = new HashSet<string>();
            var any = false;

            var guids = AssetDatabase.FindAssets("t:" + nameof(UIKitProjectSo));

            for (var i = 0; i < guids.Length; i++)
            {
                var projectPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                var project = AssetDatabase.LoadAssetAtPath<UIKitProjectSo>(projectPath);
                if (project == null)
                {
                    continue;
                }

                var tokens = project.Tokens;
                for (var t = 0; t < tokens.Count; t++)
                {
                    var token = tokens[t];
                    if (token == null || string.IsNullOrEmpty(token.MatchId))
                    {
                        continue;
                    }

                    var itemPath = project.name + "/" + token.GetType().Name + "/" + token.MatchId;
                    if (!seen.Add(itemPath))
                    {
                        continue;
                    }

                    any = true;
                    var matchId = token.MatchId;
                    menu.AddItem(new GUIContent(itemPath), matchId == current, () => SetId(index, matchId));
                }
            }

            if (!any)
            {
                menu.AddDisabledItem(new GUIContent("（工程里还没有可用的 token）"));
            }

            menu.DropDown(new Rect(anchor.worldBound.position, anchor.worldBound.size));
        }

        private string CurrentId(int index)
        {
            return TryGetEntry(index, out var entry)
                ? entry.FindPropertyRelative("_id").stringValue
                : string.Empty;
        }
    }
}
