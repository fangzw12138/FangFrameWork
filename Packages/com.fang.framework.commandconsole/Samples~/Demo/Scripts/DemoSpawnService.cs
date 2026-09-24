#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;

namespace Fang.Framework.CommandConsole.Demo
{
    /// <summary>
    /// 生成服务（Scene 层）：真的往场景里建 / 删 GameObject。
    /// 用它来验证「指令在主线程执行」——后台线程碰 UnityEngine API 会直接炸。
    /// </summary>
    public sealed class DemoSpawnService : Service
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();

        private int _nextIndex;

        public int Count => _spawned.Count;

        public Vector3 LastPosition =>
            _spawned.Count > 0 && _spawned[_spawned.Count - 1] != null
                ? _spawned[_spawned.Count - 1].transform.position
                : Vector3.zero;

        public void Spawn(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.name = "Box_" + _nextIndex++;
                box.transform.SetParent(transform, false);
                box.transform.position = NextPosition(_spawned.Count);
                box.transform.localScale = Vector3.one * 0.6f;

                _spawned.Add(box);
            }
        }

        public void Clear()
        {
            for (var i = _spawned.Count - 1; i >= 0; i--)
            {
                var box = _spawned[i];
                if (box == null)
                {
                    continue;
                }

                // 编辑器里（非播放）Destroy 会报错，必须走 DestroyImmediate。
                if (Application.isPlaying)
                {
                    Destroy(box);
                }
                else
                {
                    DestroyImmediate(box);
                }
            }

            _spawned.Clear();
        }

        public override void OnDispose()
        {
            Clear();
        }

        private static Vector3 NextPosition(int index)
        {
            var x = (index % 6) * 1.2f - 3f;
            var z = (index / 6) * 1.2f - 3f;
            return new Vector3(x, 0.3f, z);
        }
    }
}
#endif
