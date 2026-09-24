using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Fang.Framework.CommandConsole.Editor
{
    /// <summary>找工程里的 <see cref="DebugProjectSo"/>（编辑器侧共用，避免页面与菜单各写一套查找）。</summary>
    internal static class DebugProjectLocator
    {
        /// <summary>工程里全部调试项目配置，按资产路径排序。</summary>
        public static IReadOnlyList<DebugProjectSo> FindAll()
        {
            var paths = new List<string>();
            var guids = AssetDatabase.FindAssets("t:" + nameof(DebugProjectSo));

            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!string.IsNullOrEmpty(path))
                {
                    paths.Add(path);
                }
            }

            paths.Sort(StringComparer.Ordinal);

            var result = new List<DebugProjectSo>();
            for (var i = 0; i < paths.Count; i++)
            {
                var project = AssetDatabase.LoadAssetAtPath<DebugProjectSo>(paths[i]);
                if (project != null)
                {
                    result.Add(project);
                }
            }

            return result;
        }

        /// <summary>菜单 / 导出这类「没有界面上下文」的场景：优先当前选中，否则工程里第一个。</summary>
        public static DebugProjectSo Resolve()
        {
            if (Selection.activeObject is DebugProjectSo selected)
            {
                return selected;
            }

            var all = FindAll();
            if (all.Count == 0)
            {
                return null;
            }

            if (all.Count > 1)
            {
                Debug.Log(
                    "[DebugCommand] 工程里有 " + all.Count + " 个调试项目配置，这次用了 '" + all[0].name
                    + "'；想指定的话先在 Project 里选中目标资产。");
            }

            return all[0];
        }
    }
}
