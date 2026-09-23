using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Fang.Framework.UI.Kit.Editor
{
    /// <summary>
    /// 执行应用：先 <see cref="Build"/> 出一份计划给你看，确认后再 <see cref="Apply"/> 落盘。
    /// 只改工程里的预制体（含 Prefab Variant 的 override），不碰包内只读资产；不做撤销。
    /// </summary>
    public static class UIKitApplier    {
        public static UIKitApplyPlan Build(UIKitProjectSo project, string onlyMatchId)
        {
            var plan = new UIKitApplyPlan
            {
                Scope = string.IsNullOrEmpty(onlyMatchId) ? "全体" : onlyMatchId
            };

            if (project == null)
            {
                plan.Skips.Add(new UIKitApplySkip("（未选择项目）", "没有项目配置"));
                return plan;
            }

            var prefabs = project.Prefabs;

            for (var i = 0; i < prefabs.Count; i++)
            {
                var prefab = prefabs[i];

                if (prefab == null)
                {
                    plan.Skips.Add(new UIKitApplySkip("预制体列表第 " + i + " 行", "空引用"));
                    continue;
                }

                var prefabPath = AssetDatabase.GetAssetPath(prefab);
                var matches = prefab.GetComponentsInChildren<TokenMatch>(true);

                if (matches.Length == 0)
                {
                    plan.Skips.Add(new UIKitApplySkip(prefabPath, "这个预制体上没有 TokenMatch"));
                    continue;
                }

                for (var m = 0; m < matches.Length; m++)
                {
                    var match = matches[m];
                    var nodePath = BuildNodePath(match.transform, prefab.transform);
                    var entries = match.Entries;

                    for (var e = 0; e < entries.Count; e++)
                    {
                        var entry = entries[e];
                        var where = prefabPath + " ▸ " + (string.IsNullOrEmpty(nodePath) ? prefab.name : nodePath) + " ▸ 第 " + e + " 条";

                        if (entry == null)
                        {
                            plan.Skips.Add(new UIKitApplySkip(where, "空条目"));
                            continue;
                        }

                        if (!string.IsNullOrEmpty(onlyMatchId) && !string.Equals(entry.Id, onlyMatchId, StringComparison.Ordinal))
                        {
                            continue;
                        }

                        if (string.IsNullOrEmpty(entry.Id))
                        {
                            plan.Skips.Add(new UIKitApplySkip(where, "条目没有填匹配 id"));
                            continue;
                        }

                        if (entry.Target == null)
                        {
                            plan.Skips.Add(new UIKitApplySkip(where, "「" + entry.Id + "」没有指定 target"));
                            continue;
                        }

                        var tokens = project.GetTokens(entry.Id);

                        if (tokens.Count == 0)
                        {
                            plan.Skips.Add(new UIKitApplySkip(where, "「" + entry.Id + "」在 token 库里没有 token"));
                            continue;
                        }

                        for (var t = 0; t < tokens.Count; t++)
                        {
                            var token = tokens[t];

                            if (!token.Accepts(entry.Target))
                            {
                                plan.Skips.Add(new UIKitApplySkip(
                                    where,
                                    token.GetType().Name + " 不接受 " + entry.Target.GetType().Name));
                                continue;
                            }

                            plan.Rows.Add(new UIKitApplyRow(
                                prefabPath,
                                nodePath,
                                m,
                                e,
                                entry.Id,
                                entry.Target.GetType().Name,
                                token));
                        }
                    }
                }
            }

            return plan;
        }

        /// <summary>按计划落盘，返回真正写成功的条数；失败原因写进 <paramref name="errors"/>。</summary>
        public static int Apply(UIKitApplyPlan plan, List<string> errors)
        {
            if (plan == null)
            {
                return 0;
            }

            var applied = 0;
            var order = new List<string>();
            var groups = new Dictionary<string, List<UIKitApplyRow>>(StringComparer.Ordinal);

            for (var i = 0; i < plan.Rows.Count; i++)
            {
                var row = plan.Rows[i];
                if (!groups.TryGetValue(row.PrefabPath, out var list))
                {
                    list = new List<UIKitApplyRow>();
                    groups[row.PrefabPath] = list;
                    order.Add(row.PrefabPath);
                }

                list.Add(row);
            }

            for (var i = 0; i < order.Count; i++)
            {
                var prefabPath = order[i];

                if (string.IsNullOrEmpty(prefabPath))
                {
                    errors?.Add("有一条没有预制体资产路径，跳过。");
                    continue;
                }

                GameObject contents = null;
                try
                {
                    contents = PrefabUtility.LoadPrefabContents(prefabPath);
                    var changed = false;
                    var rows = groups[prefabPath];

                    for (var r = 0; r < rows.Count; r++)
                    {
                        var row = rows[r];
                        var node = ResolveNode(contents, row.NodePath);

                        if (node == null)
                        {
                            errors?.Add(prefabPath + "：找不到节点「" + row.NodePath + "」，跳过。");
                            continue;
                        }

                        var matches = node.GetComponents<TokenMatch>();

                        if (row.MatchIndex >= matches.Length)
                        {
                            errors?.Add(prefabPath + "：节点「" + row.NodePath + "」上的 TokenMatch 数量对不上，跳过。");
                            continue;
                        }

                        var entries = matches[row.MatchIndex].Entries;

                        if (row.EntryIndex >= entries.Count)
                        {
                            errors?.Add(prefabPath + "：节点「" + row.NodePath + "」上的条目数量对不上，跳过。");
                            continue;
                        }

                        var target = entries[row.EntryIndex].Target;

                        if (target == null)
                        {
                            errors?.Add(prefabPath + "：节点「" + row.NodePath + "」第 " + row.EntryIndex + " 条的 target 是空，跳过。");
                            continue;
                        }

                        row.Token.Apply(target);
                        applied++;
                        changed = true;
                    }

                    if (changed)
                    {
                        PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                    }
                }
                catch (Exception e)
                {
                    errors?.Add(prefabPath + "：写回失败 —— " + e.Message);
                }
                finally
                {
                    if (contents != null)
                    {
                        PrefabUtility.UnloadPrefabContents(contents);
                    }
                }
            }

            return applied;
        }

        /// <summary>节点相对根节点的路径；根节点自己返回空字符串。</summary>
        private static string BuildNodePath(Transform node, Transform root)
        {
            if (node == null || root == null || node == root)
            {
                return string.Empty;
            }

            var names = new List<string>();
            var current = node;

            while (current != null && current != root)
            {
                names.Add(current.name);
                current = current.parent;
            }

            names.Reverse();
            return string.Join("/", names);
        }

        private static Transform ResolveNode(GameObject root, string nodePath)
        {
            if (root == null)
            {
                return null;
            }

            return string.IsNullOrEmpty(nodePath) ? root.transform : root.transform.Find(nodePath);
        }
    }
}
