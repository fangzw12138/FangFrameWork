using System;
using System.Collections.Generic;
using FMODUnity;
using UnityEditor;
using UnityEngine;

namespace Fang.Framework.FmodAudio.Editor
{
    [CustomPropertyDrawer(typeof(FmodEventPath))]
    public sealed class FmodEventPathDrawer : PropertyDrawer
    {
        private const float ButtonWidth = 22f;

        private static readonly List<string> NoEvents = new List<string>();

        private static bool eventCacheUnavailable;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var pathProperty = property.FindPropertyRelative("_path");
            if (pathProperty == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            position = EditorGUI.PrefixLabel(position, label);

            var events = CollectEvents();
            if (events.Count == 0)
            {
                EditorGUI.PropertyField(position, pathProperty, GUIContent.none);
                return;
            }

            var textRect = new Rect(position.x, position.y, position.width - ButtonWidth - 2f, position.height);
            var buttonRect = new Rect(position.xMax - ButtonWidth, position.y, ButtonWidth, position.height);

            EditorGUI.PropertyField(textRect, pathProperty, GUIContent.none);

            if (!GUI.Button(buttonRect, new GUIContent("⊙", "从 FMOD 事件缓存里选一个。")))
            {
                return;
            }

            var menu = new GenericMenu();
            for (var i = 0; i < events.Count; i++)
            {
                var path = events[i];
                menu.AddItem(
                    new GUIContent(path),
                    pathProperty.stringValue == path,
                    () =>
                    {
                        pathProperty.stringValue = path;
                        pathProperty.serializedObject.ApplyModifiedProperties();
                    });
            }

            menu.DropDown(buttonRect);
        }

        private static List<string> CollectEvents()
        {
            if (eventCacheUnavailable)
            {
                return NoEvents;
            }

            List<EditorEventRef> events;
            try
            {
                events = EventManager.Events;
            }
            catch (Exception exception)
            {
                eventCacheUnavailable = true;
                Debug.LogWarning("[FmodAudio] FMOD 事件缓存不可用，事件选择器已停用（可以直接手填路径）：" + exception.Message);
                return NoEvents;
            }

            if (events == null || events.Count == 0)
            {
                return NoEvents;
            }

            var paths = new List<string>();
            for (var i = 0; i < events.Count; i++)
            {
                var eventRef = events[i];
                if (eventRef != null && !string.IsNullOrEmpty(eventRef.Path))
                {
                    paths.Add(eventRef.Path);
                }
            }

            paths.Sort(StringComparer.Ordinal);
            return paths;
        }
    }
}
