using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Fang.Framework.FmodAudio.Editor.Tests
{
    internal static class FmodTestSetup
    {
        private static readonly List<UnityEngine.Object> Created = new List<UnityEngine.Object>();

        public static FmodAudioConfigSo NewConfig()
        {
            var config = ScriptableObject.CreateInstance<FmodAudioConfigSo>();
            Created.Add(config);
            return config;
        }

        public static FmodAudioService NewService()
        {
            var gameObject = new GameObject("FmodAudioService");
            Created.Add(gameObject);
            return gameObject.AddComponent<FmodAudioService>();
        }

        public static void ExpectLog(LogType type, string pattern, int count = 1)
        {
            for (var i = 0; i < count; i++)
            {
                LogAssert.Expect(type, new Regex(pattern));
            }
        }

        public static void SetMasterBusPath(FmodAudioConfigSo config, string busPath)
        {
            Edit(config, serialized => serialized.FindProperty("_masterBusPath").stringValue = busPath);
        }

        public static void SetDefaultMasterVolume(FmodAudioConfigSo config, float volume)
        {
            Edit(config, serialized => serialized.FindProperty("_defaultMasterVolume").floatValue = volume);
        }

        public static void SetChannels(FmodAudioConfigSo config, params (string ChannelId, string BusPath, float Volume)[] channels)
        {
            Edit(config, serialized =>
            {
                var list = serialized.FindProperty("_busChannels");
                list.ClearArray();

                for (var i = 0; i < channels.Length; i++)
                {
                    var element = Append(list);
                    Write(element, channels[i].ChannelId, channels[i].BusPath, channels[i].Volume);
                }
            });
        }

        public static void SetChannelVolumeOf(FmodAudioConfigSo config, int index, float volume)
        {
            Edit(config, serialized =>
            {
                var element = serialized.FindProperty("_busChannels").GetArrayElementAtIndex(index);
                element.FindPropertyRelative("_defaultVolume").floatValue = volume;
            });
        }

        public static void AddNullChannel(FmodAudioConfigSo config)
        {
            var field = typeof(FmodAudioConfigSo).GetField("_busChannels", BindingFlags.Instance | BindingFlags.NonPublic);
            var list = (List<FmodBusChannel>)field.GetValue(config);
            list.Add(null);
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

        private static SerializedProperty Append(SerializedProperty list)
        {
            var index = list.arraySize;
            list.InsertArrayElementAtIndex(index);
            return list.GetArrayElementAtIndex(index);
        }

        private static void Write(SerializedProperty element, string channelId, string busPath, float volume)
        {
            element.FindPropertyRelative("_channelId").stringValue = channelId;
            element.FindPropertyRelative("_busPath").stringValue = busPath;
            element.FindPropertyRelative("_defaultVolume").floatValue = volume;
        }

        private static void Edit(FmodAudioConfigSo config, Action<SerializedObject> write)
        {
            var serialized = new SerializedObject(config);
            write(serialized);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
