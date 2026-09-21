using System;
using UnityEngine;

namespace Fang.Framework
{
    [Serializable]
    public abstract class Data<TConfig> where TConfig : ConfigDataSo
    {
        [NonSerialized] protected string _runtimeId;
        [NonSerialized] protected TConfig _config;

        public string RuntimeId => _runtimeId;
        public TConfig Config => _config;

        protected Data(TConfig config)
        {
            _runtimeId = Guid.NewGuid().ToString();
            _config = config;
        }
    }
}
