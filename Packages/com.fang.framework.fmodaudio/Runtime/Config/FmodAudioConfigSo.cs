using System.Collections.Generic;
using UnityEngine;

namespace Fang.Framework.FmodAudio
{
    public sealed class FmodAudioConfigSo : ConfigDataSo
    {
        [SerializeField, Range(0f, 1f)] private float _defaultMasterVolume = 1f;
        [SerializeField] private string _masterBusPath = "bus:/";
        [SerializeField] private List<FmodBusChannel> _busChannels = new List<FmodBusChannel>();

        public float DefaultMasterVolume => Mathf.Clamp01(_defaultMasterVolume);

        public string MasterBusPath => _masterBusPath;

        public IReadOnlyList<FmodBusChannel> BusChannels => _busChannels;
    }
}
