using System.IO;
using UnityEditor;
using UnityEngine;

namespace Fang.Framework.FmodAudio.Editor
{
    public static class FmodAudioConfigScaffolder
    {
        private const string Folder = "Assets/FangFramework/FmodAudio";
        private const string AssetName = "FmodAudioConfig";

        public static string AssetPath => Folder + "/" + AssetName + ".asset";

        public static FmodAudioConfigSo Load()
        {
            return AssetDatabase.LoadAssetAtPath<FmodAudioConfigSo>(AssetPath);
        }

        public static FmodAudioConfigSo Create()
        {
            var existing = Load();
            if (existing != null)
            {
                return existing;
            }

            EnsureFolder(Folder);

            var config = ScriptableObject.CreateInstance<FmodAudioConfigSo>();
            var serialized = new SerializedObject(config);

            serialized.FindProperty("_id").stringValue = AssetName;
            serialized.FindProperty("_displayName").stringValue = "FMOD 音频";
            serialized.FindProperty("_description").stringValue = "FMOD 总线通道映射与默认音量。";
            serialized.FindProperty("_masterBusPath").stringValue = "bus:/";
            serialized.FindProperty("_defaultMasterVolume").floatValue = 1f;

            var channels = serialized.FindProperty("_busChannels");
            channels.ClearArray();
            AddChannel(channels, "bgm", "bus:/Music", 0.7f);
            AddChannel(channels, "sfx", "bus:/SFX", 1f);
            AddChannel(channels, "ui", "bus:/UI", 1f);
            AddChannel(channels, "voice", "bus:/Voice", 1f);

            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(config, AssetPath);
            AssetDatabase.SaveAssets();

            return config;
        }

        private static void AddChannel(SerializedProperty channels, string channelId, string busPath, float volume)
        {
            var index = channels.arraySize;
            channels.InsertArrayElementAtIndex(index);

            var element = channels.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("_channelId").stringValue = channelId;
            element.FindPropertyRelative("_busPath").stringValue = busPath;
            element.FindPropertyRelative("_defaultVolume").floatValue = volume;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            var parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            var leaf = Path.GetFileName(folderPath);

            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf))
            {
                return;
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
