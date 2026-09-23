using System.Collections.Generic;

namespace Fang.Framework.UI.Kit.Internal
{
    internal static class TokenMatchValidator
    {
        public static IReadOnlyList<TokenIssue> Validate(TokenMatch match, UIKitProjectSo project)
        {
            var issues = new List<TokenIssue>();
            ValidateInto(match, project, issues);
            return issues;
        }

        public static IReadOnlyList<TokenIssue> Validate(IEnumerable<TokenMatch> matches, UIKitProjectSo project)
        {
            var issues = new List<TokenIssue>();
            if (matches == null)
            {
                return issues;
            }

            foreach (var match in matches)
            {
                ValidateInto(match, project, issues);
            }

            return issues;
        }

        public static IReadOnlyList<TokenIssue> ValidateProject(UIKitProjectSo project)
        {
            var issues = new List<TokenIssue>();
            if (project == null)
            {
                return issues;
            }

            var tokens = project.Tokens;

            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];

                if (token == null)
                {
                    issues.Add(new TokenIssue(TokenIssueKind.EmptyEntry, null, $"{project.name}：token 库第 {i} 行是空行"));
                    continue;
                }

                var id = token.MatchId;

                if (string.IsNullOrEmpty(id))
                {
                    issues.Add(new TokenIssue(
                        TokenIssueKind.TokenNotEnabled,
                        null,
                        $"{project.name}：token 库第 {i} 行（{token.name}）还没填匹配 id，这条 token 不参与匹配"));
                }
            }

            return issues;
        }

        private static void ValidateInto(TokenMatch match, UIKitProjectSo project, List<TokenIssue> issues)
        {
            if (match == null)
            {
                return;
            }

            var entries = match.Entries;

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];

                if (entry == null)
                {
                    issues.Add(new TokenIssue(TokenIssueKind.EmptyEntry, null, $"{match.name}：第 {i} 条是空项", match, i));
                    continue;
                }

                var id = entry.Id;

                if (string.IsNullOrEmpty(id))
                {
                    issues.Add(new TokenIssue(TokenIssueKind.EmptyId, null, $"{match.name}：第 {i} 条没有填匹配 id", match, i));
                    continue;
                }

                if (entry.Target == null)
                {
                    issues.Add(new TokenIssue(TokenIssueKind.EmptyTarget, id, $"{match.name}：「{id}」没有指定 target 组件", match, i));
                    continue;
                }

                var tokens = project == null ? null : project.GetTokens(id);

                if (tokens == null || tokens.Count == 0)
                {
                    issues.Add(new TokenIssue(TokenIssueKind.MissingToken, id, $"{match.name}：项目配置里没有「{id}」的 token", match, i));
                    continue;
                }

                for (var t = 0; t < tokens.Count; t++)
                {
                    var token = tokens[t];

                    if (token.Accepts(entry.Target))
                    {
                        continue;
                    }

                    issues.Add(new TokenIssue(
                        TokenIssueKind.TargetTypeMismatch,
                        id,
                        $"{match.name}：「{id}」的 target 是 {entry.Target.GetType().Name}，{token.GetType().Name} 只接受 {token.TargetType.Name}",
                        match,
                        i));
                }
            }
        }
    }
}
