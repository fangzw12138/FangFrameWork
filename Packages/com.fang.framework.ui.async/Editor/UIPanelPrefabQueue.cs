using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Fang.Framework.UI.Async.Editor
{
    [InitializeOnLoad]
    public static class UIPanelPrefabQueue
    {
        private const string SessionKey = "Fang.Framework.UI.Async.PendingPrefabs";
        private const int MaxAttempts = 20;
        private const char FieldSeparator = '|';
        private const char EntrySeparator = ';';

        private struct Entry
        {
            public string ProjectGuid;
            public string PanelName;
            public int Attempts;
        }

        private static int _idleFrames;

        static UIPanelPrefabQueue()
        {
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                _idleFrames = 0;
                return;
            }

            _idleFrames++;

            if (_idleFrames < 10)
            {
                return;
            }

            _idleFrames = 0;

            if (Load().Count == 0)
            {
                return;
            }

            Flush();
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
                    var refreshed = entries[i];
                    refreshed.Attempts = 0;
                    entries[i] = refreshed;
                    Save(entries);
                    return;
                }
            }

            entries.Add(new Entry { ProjectGuid = guid, PanelName = panelName });
            Save(entries);
        }

        public static void Cancel(UIProjectConfigDataSo project, string panelName)
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
            var removed = false;

            for (var i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i].ProjectGuid == guid && entries[i].PanelName == panelName)
                {
                    entries.RemoveAt(i);
                    removed = true;
                }
            }

            if (removed)
            {
                Save(entries);
            }
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
