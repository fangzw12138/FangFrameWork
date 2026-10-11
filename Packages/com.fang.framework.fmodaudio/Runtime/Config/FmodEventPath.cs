using System;
using UnityEngine;

namespace Fang.Framework.FmodAudio
{
    [Serializable]
    public sealed class FmodEventPath
    {
        [SerializeField] private string _path;

        public FmodEventPath()
        {
        }

        public FmodEventPath(string path)
        {
            _path = path;
        }

        public string Path => _path;

        public bool IsValid => !string.IsNullOrEmpty(_path);
    }
}
