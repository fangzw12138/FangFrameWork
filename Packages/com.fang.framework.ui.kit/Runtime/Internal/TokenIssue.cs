namespace Fang.Framework.UI.Kit.Internal
{
    internal enum TokenIssueKind
    {
        EmptyEntry,
        EmptyId,
        EmptyTarget,
        MissingToken,
        TargetTypeMismatch,
        TokenNotEnabled
    }

    internal sealed class TokenIssue
    {
        public TokenIssue(TokenIssueKind kind, string id, string message, TokenMatch match = null, int entryIndex = -1)
        {
            Kind = kind;
            Id = id;
            Message = message;
            Match = match;
            EntryIndex = entryIndex;
        }

        public TokenIssueKind Kind { get; }

        public string Id { get; }

        public string Message { get; }

        public TokenMatch Match { get; }

        public int EntryIndex { get; }

        /// <summary>提醒（不算错，不挡使用），例如「这条 token 还没填匹配 id，不参与匹配」。</summary>
        public bool IsHint => Kind == TokenIssueKind.TokenNotEnabled;

        public override string ToString()
        {
            return Message;
        }
    }
}
