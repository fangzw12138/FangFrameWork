using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Fang.Framework.UI.Kit.Editor.Tests
{
    internal static class KitTestSetup
    {
        private static readonly List<UnityEngine.Object> Created = new List<UnityEngine.Object>();

        public static GameObject NewGameObject(string name)
        {
            var go = new GameObject(name);
            Created.Add(go);
            return go;
        }

        public static T NewComponent<T>(string name) where T : Component
        {
            return NewGameObject(name).AddComponent<T>();
        }

        public static T Track<T>(T value) where T : UnityEngine.Object
        {
            Created.Add(value);
            return value;
        }

        public static Sprite NewSprite(string name)
        {
            var texture = Track(new Texture2D(4, 4));
            var sprite = Track(Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f)));
            sprite.name = name;
            return sprite;
        }

        public static T NewToken<T>(string matchId = null) where T : TokenSo
        {
            var token = ScriptableObject.CreateInstance<T>();
            Created.Add(token);

            if (!string.IsNullOrEmpty(matchId))
            {
                SetString(token, "_matchId", matchId);
            }

            return token;
        }

        public static UIKitProjectSo NewProject(params TokenSo[] tokens)
        {
            var project = ScriptableObject.CreateInstance<UIKitProjectSo>();
            Created.Add(project);
            SetString(project, "_id", "UIKitProject");

            Edit(project, "_tokens", list =>
            {
                list.arraySize = tokens.Length;
                for (var i = 0; i < tokens.Length; i++)
                {
                    list.GetArrayElementAtIndex(i).objectReferenceValue = tokens[i];
                }
            });

            return project;
        }

        public static TokenMatch NewMatch(params (string Id, Component Target)[] entries)        {
            var go = NewGameObject("Match");
            var match = go.AddComponent<TokenMatch>();

            Edit(match, "_entries", list =>
            {
                list.arraySize = entries.Length;
                for (var i = 0; i < entries.Length; i++)
                {
                    var element = list.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("_id").stringValue = entries[i].Id;
                    element.FindPropertyRelative("_target").objectReferenceValue = entries[i].Target;
                }
            });

            return match;
        }

        public static void AddPrefab(UIKitProjectSo project, GameObject prefab)
        {
            Edit(project, "_prefabs", list =>
            {
                var index = list.arraySize;
                list.arraySize = index + 1;
                list.GetArrayElementAtIndex(index).objectReferenceValue = prefab;
            });
        }

        public static void Cleanup()
        {
            for (var i = 0; i < Created.Count; i++)
            {
                if (Created[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(Created[i]);
                }
            }

            Created.Clear();
        }

        public static void SetString(UnityEngine.Object target, string field, string value)
        {
            Edit(target, field, p => p.stringValue = value);
        }

        public static void SetFloat(UnityEngine.Object target, string field, float value)
        {
            Edit(target, field, p => p.floatValue = value);
        }

        public static void SetInt(UnityEngine.Object target, string field, int value)
        {
            Edit(target, field, p => p.intValue = value);
        }

        public static void SetBool(UnityEngine.Object target, string field, bool value)
        {
            Edit(target, field, p => p.boolValue = value);
        }

        public static void SetColor(UnityEngine.Object target, string field, Color value)
        {
            Edit(target, field, p => p.colorValue = value);
        }

        public static void SetObject(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            Edit(target, field, p => p.objectReferenceValue = value);
        }

        private static void Edit(UnityEngine.Object target, string field, Action<SerializedProperty> set)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            set(property);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
