using System;
using UnityEngine;

namespace Fang.Framework.FmodAudio
{
    [Serializable]
    public sealed class FmodBusChannel
    {
        [SerializeField] private string _channelId;
        [SerializeField] private string _busPath;
        [SerializeField, Range(0f, 1f)] private float _defaultVolume = 1f;

        public string ChannelId => _channelId;

        public string BusPath => _busPath;

        public float DefaultVolume => Mathf.Clamp01(_defaultVolume);
    }
}
