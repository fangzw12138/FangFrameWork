#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace Fang.Framework.CommandConsole.Demo
{
    /// <summary>
    /// 示例宿主：<b>只驱动生命周期</b>（OnInit / Tick / FixedTick / OnDispose），装配全在 <see cref="AppScope"/> 里。
    ///
    /// 与核心包 MinimalSample 的差别：这里 <see cref="AppScope"/> 直接放在场景里，项目配置 SO 作为序列化引用
    /// 挂在它上面，所以不需要 Resources.Load。
    /// </summary>
    public sealed class DemoHost : MonoBehaviour
    {
        [SerializeField] private AppScope _app;

        private void Awake()
        {
            if (_app == null)
            {
                Debug.LogError("[CommandConsoleDemo] DemoHost 没有引用 AppScope。");
                return;
            }

            _app.OnInit();
        }

        private void Update()
        {
            if (_app != null)
            {
                _app.Tick(Time.deltaTime);
            }
        }

        private void FixedUpdate()
        {
            if (_app != null)
            {
                _app.FixedTick(Time.fixedDeltaTime);
            }
        }

        private void OnDestroy()
        {
            if (_app != null)
            {
                _app.OnDispose();
            }
        }
    }
}
#endif
