using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Fang.Framework.CommandConsole.Editor.Tests
{
    /// <summary>测试用的 <see cref="DebugProjectSo"/> 构造器（私有序列化字段只能走 SerializedObject）。</summary>
    internal static class DebugProjectProbe
    {
        public static DebugProjectSo New()
        {
            return ScriptableObject.CreateInstance<DebugProjectSo>();
        }

        public static void Destroy(DebugProjectSo project)
        {
            if (project != null)
            {
                Object.DestroyImmediate(project);
            }
        }

        public static DebugProjectSo SetBool(DebugProjectSo project, string field, bool value)
        {
            return Edit(project, field, property => property.boolValue = value);
        }

        public static DebugProjectSo SetInt(DebugProjectSo project, string field, int value)
        {
            return Edit(project, field, property => property.intValue = value);
        }

        public static DebugProjectSo SetString(DebugProjectSo project, string field, string value)
        {
            return Edit(project, field, property => property.stringValue = value);
        }

        public static DebugProjectSo SetStringList(DebugProjectSo project, string field, IReadOnlyList<string> values)
        {
            return Edit(project, field, property =>
            {
                property.arraySize = values == null ? 0 : values.Count;
                for (var i = 0; i < property.arraySize; i++)
                {
                    property.GetArrayElementAtIndex(i).stringValue = values[i];
                }
            });
        }

        public static DebugProjectSo SetQuickButtons(
            DebugProjectSo project,
            IReadOnlyList<DebugQuickButton> buttons)
        {
            return Edit(project, "_quickButtons", property =>
            {
                property.arraySize = buttons == null ? 0 : buttons.Count;
                for (var i = 0; i < property.arraySize; i++)
                {
                    var element = property.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("_label").stringValue = buttons[i].Label;
                    element.FindPropertyRelative("_commandLine").stringValue = buttons[i].CommandLine;
                }
            });
        }

        private static DebugProjectSo Edit(DebugProjectSo project, string field, System.Action<SerializedProperty> set)
        {
            var serialized = new SerializedObject(project);
            var property = serialized.FindProperty(field);

            Assert_NotNull(property, field);

            set(property);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return project;
        }

        private static void Assert_NotNull(SerializedProperty property, string field)
        {
            if (property == null)
            {
                throw new System.InvalidOperationException(
                    "DebugProjectSo 上找不到序列化字段 '" + field + "'（字段被改名了？）");
            }
        }
    }
}
