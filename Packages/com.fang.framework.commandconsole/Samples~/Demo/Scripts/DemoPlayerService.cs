#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace Fang.Framework.CommandConsole.Demo
{
    /// <summary>
    /// 玩家服务（Scene 层）：一坨可变状态。
    ///
    /// 服务自己只做**防御性夹取**，参数合法性由指令负责报错（<see cref="DebugCommandException"/>）——
    /// 这样「调用方传错」和「服务内部出错」在回包上能分得清。
    /// </summary>
    public sealed class DemoPlayerService : Service
    {
        public const int MaxHp = 100;

        public int Hp { get; private set; } = MaxHp;

        public int Level { get; private set; } = 1;

        public Vector3 Position { get; private set; }

        public void SetHp(int value)
        {
            Hp = Mathf.Clamp(value, 0, MaxHp);
        }

        public void Damage(int amount)
        {
            Hp = Mathf.Max(0, Hp - Mathf.Max(0, amount));
        }

        public void Heal(int amount)
        {
            Hp = Mathf.Min(MaxHp, Hp + Mathf.Max(0, amount));
        }

        public void SetLevel(int value)
        {
            Level = Mathf.Max(1, value);
        }

        public void Teleport(Vector3 position)
        {
            Position = position;
        }
    }
}
#endif
