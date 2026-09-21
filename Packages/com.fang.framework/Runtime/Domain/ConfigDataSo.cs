using UnityEngine;

namespace Fang.Framework
{
    public abstract class ConfigDataSo : ScriptableObject
    {
        [SerializeField] protected string _id;
        [SerializeField] protected string _displayName;
        [SerializeField] protected string _description;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
    }
}
