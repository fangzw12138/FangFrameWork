using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Fang.Framework.Async.LoadingTransition
{
    public sealed class LoadingTransitionHost : MonoBehaviour
    {
        private AppScope _app;
        private LoadingTransitionVisual _visual;

        private void Awake()
        {
            var canvas = UiFactory.CreateCanvas(transform, "Canvas");
            BuildMenu(canvas.transform);
            _visual = BuildOverlay(canvas.transform);

            var app = new GameObject("App");
            app.transform.SetParent(transform, false);
            _app = app.AddComponent<AppScope>();
            _app.RoundPrepared += OnRoundPrepared;

            BootAsync().Forget();
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
            _visual.OnDispose();
            _app.DisposeAsync().Forget();
        }

        private void BuildMenu(Transform parent)
        {
            var enter = UiFactory.CreateButton(parent, "EnterGameButton", "ENTER GAME", new Vector2(280f, 64f), new Vector2(0f, -120f));
            enter.onClick.AddListener(() => _app.EnterGame().Forget());

            var title = UiFactory.CreateButton(parent, "ReturnToTitleButton", "RETURN TO TITLE", new Vector2(280f, 64f), new Vector2(0f, -200f));
            title.onClick.AddListener(() => _app.ReturnToTitle().Forget());
        }

        private static LoadingTransitionVisual BuildOverlay(Transform parent)
        {
            var host = new GameObject("LoadingTransitionOverlay", typeof(RectTransform));
            host.transform.SetParent(parent, false);

            var visual = host.AddComponent<LoadingTransitionVisual>();
            visual.Initialize();
            return visual;
        }

        private async UniTaskVoid BootAsync()
        {
            Debug.Log("LoadingTransition: boot, initializing app");

            _visual.Watch(_app);
            await _app.InitAsync();
        }

        private void OnRoundPrepared(AsyncScope scope)
        {
            Debug.Log($"LoadingTransition: round prepared, {scope.name}");

            _visual.Watch(scope);
        }
    }
}
