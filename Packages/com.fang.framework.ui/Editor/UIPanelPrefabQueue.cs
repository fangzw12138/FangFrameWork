using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Fang.Framework.UI.Editor
{
    [InitializeOnLoad]
    public static class UIPanelPrefabQueue
    {
        private const string SessionKey = "Fang.Framework.UI.PendingPrefabs";
        private const int MaxAttempts = 20;
        private const char FieldSeparator = '|';
        private const char EntrySeparator = ';';

        private struct Entry
        {
            public string ProjectGuid;
            public string PanelName;
            public int Attempts;
        }

        static UIPanelPrefabQueue()
        {
            EditorApplication.delayCall += Flush;
            AssemblyReloadEvents.afterAssemblyReload += Flush;
        }

        public static event System.Action PrefabCreated;

        public static void Enqueue(UIProjectConfigDataSo project, string panelName)
        {
            if (project == null || string.IsNullOrWhiteSpace(panelName))
            {
                return;
            }

            var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(project));
            if (string.IsNullOrEmpty(guid))
            {
                return;
            }

            var entries = Load();
            for (var i = 0; i < entries.Count; i++)
            {
                if (entries[i].ProjectGuid == guid && entries[i].PanelName == panelName)
                {
                    return;
                }
            }

            entries.Add(new Entry { ProjectGuid = guid, PanelName = panelName });
            Save(entries);
        }

        private static void Flush()
        {
            var entries = Load();
            if (entries.Count == 0)
            {
                return;
            }

            var remaining = new List<Entry>();
            var created = false;

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var project = AssetDatabase.LoadAssetAtPath<UIProjectConfigDataSo>(
                    AssetDatabase.GUIDToAssetPath(entry.ProjectGuid));

                if (project == null)
                {
                    continue;
                }

                var result = UIPanelScaffolder.CreatePrefab(project, entry.PanelName);
                if (result.Success)
                {
                    created = true;
                    Debug.Log("[Fang.UI] " + result.ToStatusMessage());
                    continue;
                }

                entry.Attempts++;
                if (entry.Attempts >= MaxAttempts)
                {
                    Debug.LogWarning("[Fang.UI] 面板预制体自动创建失败（" + entry.PanelName + "）：" + result.ToStatusMessage());
                    continue;
                }

                remaining.Add(entry);
            }

            Save(remaining);

            if (created)
            {
                PrefabCreated?.Invoke();
            }
        }

        private static List<Entry> Load()
        {
            var entries = new List<Entry>();
            var raw = SessionState.GetString(SessionKey, string.Empty);
            if (string.IsNullOrEmpty(raw))
            {
                return entries;
            }

            var chunks = raw.Split(EntrySeparator);
            for (var i = 0; i < chunks.Length; i++)
            {
                var fields = chunks[i].Split(FieldSeparator);
                if (fields.Length != 3)
                {
                    continue;
                }

                entries.Add(new Entry
                {
                    ProjectGuid = fields[0],
                    PanelName = fields[1],
                    Attempts = int.TryParse(fields[2], out var attempts) ? attempts : 0,
                });
            }

            return entries;
        }

        private static void Save(List<Entry> entries)
        {
            if (entries.Count == 0)
            {
                SessionState.EraseString(SessionKey);
                return;
            }

            var builder = new StringBuilder();
            for (var i = 0; i < entries.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(EntrySeparator);
                }

                builder.Append(entries[i].ProjectGuid).Append(FieldSeparator)
                    .Append(entries[i].PanelName).Append(FieldSeparator)
                    .Append(entries[i].Attempts);
            }

            SessionState.SetString(SessionKey, builder.ToString());
        }
    }
}
