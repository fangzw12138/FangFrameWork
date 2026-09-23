using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fang.Framework.UI.Kit
{
    public sealed class TokenMatch : MonoBehaviour
    {
        [SerializeField] private List<TokenMatchEntry> _entries = new List<TokenMatchEntry>();

        public IReadOnlyList<TokenMatchEntry> Entries => _entries;

        public void ApplyFrom(UIKitProjectSo project)
        {
            if (project == null)
            {
                return;
            }

            for (var i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (entry == null || entry.Target == null)
                {
                    continue;
                }

                var tokens = project.GetTokens(entry.Id);
                for (var t = 0; t < tokens.Count; t++)
                {
                    tokens[t].Apply(entry.Target);
                }
            }
        }
    }

    [Serializable]
    public sealed class TokenMatchEntry
    {
        [SerializeField] private string _id;
        [SerializeField] private Component _target;

        public string Id => _id;

        public Component Target => _target;
    }
}
