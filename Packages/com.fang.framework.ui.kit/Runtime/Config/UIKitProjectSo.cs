using System;
using System.Collections.Generic;
using Fang.Framework;
using UnityEngine;

namespace Fang.Framework.UI.Kit
{
    [CreateAssetMenu(fileName = "UIKitProject", menuName = "Fang Framework/UI Kit/项目配置")]
    public sealed class UIKitProjectSo : ConfigDataSo
    {
        [Tooltip("token 库：每条 token 资产自己带 MatchId，按它去匹配预制体端 TokenMatch 的条目。")]
        [SerializeField] private List<TokenSo> _tokens = new List<TokenSo>();

        [Tooltip("扫描范围：全体应用只处理这个列表里的预制体。")]
        [SerializeField] private List<GameObject> _prefabs = new List<GameObject>();

        public IReadOnlyList<TokenSo> Tokens => _tokens;

        public IReadOnlyList<GameObject> Prefabs => _prefabs;

        /// <summary>
        /// 按匹配 id（MatchId）取 token。同一条 id 上有多个 token 时，按 token 库里的顺序返回，
        /// 应用时后面的覆盖前面的（不视为错误）。
        /// </summary>
        public IReadOnlyList<TokenSo> GetTokens(string id)
        {
            var tokens = new List<TokenSo>();

            if (string.IsNullOrEmpty(id))
            {
                return tokens;
            }

            for (var i = 0; i < _tokens.Count; i++)
            {
                var token = _tokens[i];
                if (token == null || !string.Equals(token.MatchId, id, StringComparison.Ordinal))
                {
                    continue;
                }

                tokens.Add(token);
            }

            return tokens;
        }
    }
}
