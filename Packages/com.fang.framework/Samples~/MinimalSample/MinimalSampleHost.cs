using UnityEngine;

namespace Fang.Framework.MinimalSample
{
    public sealed class MinimalSampleHost : MonoBehaviour
    {
        private AppScope _app;

        private void Awake()
        {
            var app = new GameObject("App");
            app.transform.SetParent(transform, false);

            _app = app.AddComponent<AppScope>();
            _app.OnInit();
        }

        private void Update()
        {
            _app.Tick(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            _app.FixedTick(Time.fixedDeltaTime);
        }

        private void OnDestroy()
        {
            _app.OnDispose();
        }
    }
}
