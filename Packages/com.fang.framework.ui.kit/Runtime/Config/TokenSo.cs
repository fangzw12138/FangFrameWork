using System;
using Fang.Framework;
using UnityEngine;

namespace Fang.Framework.UI.Kit
{
    public abstract class TokenSo : ConfigDataSo
    {
        [Tooltip("匹配 id（MatchId，连接键）：手填的字符串，预制体端 TokenMatch 的条目写同一个字符串，按它来这份 token 库里找 token。留空 = 这条还没启用。注意与资产自己的 Id 无关。")]
        [SerializeField] private string _matchId;

        public string MatchId => _matchId;

        public abstract Type TargetType { get; }

        public abstract void Apply(Component target);

        public bool Accepts(Component target)
        {
            return target != null && TargetType.IsInstanceOfType(target);
        }

        protected bool EnsureTarget(Component target)
        {
            if (Accepts(target))
            {
                return true;
            }

            Debug.LogError(
                $"[{GetType().Name}] target 类型不符：期望 {TargetType.Name}，实际 {(target == null ? "null" : target.GetType().Name)}（token id = {Id}）",
                this);
            return false;
        }
    }
}
