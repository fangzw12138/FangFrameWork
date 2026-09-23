using System.Collections.Generic;
using System.Text;

namespace Fang.Framework.UI.Kit.Editor
{
    /// <summary>一条将要落盘的应用：哪个预制体、哪个节点、哪一条、用哪个 token。</summary>
    public sealed class UIKitApplyRow
    {
        public UIKitApplyRow(
            string prefabPath,
            string nodePath,
            int matchIndex,
            int entryIndex,
            string matchId,
            string targetTypeName,
            TokenSo token)
        {
            PrefabPath = prefabPath;
            NodePath = nodePath;
            MatchIndex = matchIndex;
            EntryIndex = entryIndex;
            MatchId = matchId;
            TargetTypeName = targetTypeName;
            Token = token;
        }

        public string PrefabPath { get; }

        /// <summary>相对预制体根节点的节点路径；空字符串表示根节点自己。</summary>
        public string NodePath { get; }

        /// <summary>该节点上第几个 TokenMatch（同一节点挂多个时用它定位）。</summary>
        public int MatchIndex { get; }

        public int EntryIndex { get; }

        public string MatchId { get; }

        public string TargetTypeName { get; }

        public TokenSo Token { get; }
    }

    /// <summary>没能进计划的一条，附原因。</summary>
    public sealed class UIKitApplySkip
    {
        public UIKitApplySkip(string where, string reason)
        {
            Where = where;
            Reason = reason;
        }

        public string Where { get; }

        public string Reason { get; }
    }

    public sealed class UIKitApplyPlan
    {
        public List<UIKitApplyRow> Rows { get; } = new List<UIKitApplyRow>();

        public List<UIKitApplySkip> Skips { get; } = new List<UIKitApplySkip>();

        /// <summary>本次范围：<c>全体</c> 或某个匹配 id。</summary>
        public string Scope { get; set; }

        public bool HasWork => Rows.Count > 0;

        public string BuildReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("范围：" + (string.IsNullOrEmpty(Scope) ? "全体" : Scope));
            sb.AppendLine();

            if (Rows.Count == 0)
            {
                sb.AppendLine("没有要改的地方。");
            }
            else
            {
                sb.AppendLine("将应用 " + Rows.Count + " 条：");
                sb.AppendLine();

                var currentPrefab = string.Empty;
                for (var i = 0; i < Rows.Count; i++)
                {
                    var row = Rows[i];
                    if (row.PrefabPath != currentPrefab)
                    {
                        currentPrefab = row.PrefabPath;
                        sb.AppendLine(currentPrefab);
                    }

                    sb.AppendLine(
                        "    " + (string.IsNullOrEmpty(row.NodePath) ? "（根节点）" : row.NodePath)
                        + "  ·  " + row.MatchId
                        + "  ·  " + row.TargetTypeName
                        + "  ←  " + (row.Token == null ? "（token 为空）" : row.Token.name));
                }
            }

            if (Skips.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("跳过的 " + Skips.Count + " 条：");
                for (var i = 0; i < Skips.Count; i++)
                {
                    sb.AppendLine("    " + Skips[i].Where + " —— " + Skips[i].Reason);
                }
            }

            return sb.ToString();
        }
    }
}
