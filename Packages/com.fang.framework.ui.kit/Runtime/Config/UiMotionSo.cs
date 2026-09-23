using UnityEngine;

namespace Fang.Framework.UI.Kit
{
    [CreateAssetMenu(fileName = "UiMotion", menuName = "Fang Framework/UI Kit/动效")]
    public sealed class UiMotionSo : ConfigDataSo
    {
        [Tooltip("动效的 AnimationClip。按节点路径绑定，一条 clip 可以同时动多个节点。")]
        [SerializeField] private AnimationClip _clip;

        [Tooltip("循环播放。")]
        [SerializeField] private bool _loop;
    }
}
