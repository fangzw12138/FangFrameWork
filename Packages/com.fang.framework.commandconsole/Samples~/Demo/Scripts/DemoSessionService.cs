#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace Fang.Framework.CommandConsole.Demo
{
    /// <summary>
    /// 会话服务（App 层）：带可变状态，并且是 <see cref="ITickable"/> —— 它跑起来就说明主线程真的在 tick，
    /// 而「装了但没人 tick」正是本包最典型的静默失败。
    /// </summary>
    public sealed class DemoSessionService : Service, ITickable
    {
        private string _name = "示例会话";
        private float _elapsed;

        public string Name => _name;

        /// <summary>被 tick 的次数（帧数）。</summary>
        public int TickCount { get; private set; }

        /// <summary>累计的 deltaTime。</summary>
        public float ElapsedSeconds => _elapsed;

        public float TimeScale => Time.timeScale;

        public void Rename(string name)
        {
            _name = name;
        }

        public void SetTimeScale(float value)
        {
            Time.timeScale = value;
        }

        public void OnTick(float deltaTime)
        {
            TickCount++;
            _elapsed += deltaTime;
        }
    }
}
#endif
