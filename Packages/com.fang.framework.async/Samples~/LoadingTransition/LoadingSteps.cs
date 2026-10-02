using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Fang.Framework.Async.LoadingTransition
{
    internal abstract class LoadingStepService : AsyncService
    {
        protected abstract float Duration { get; }

        protected override async UniTask OnInitAsync()
        {
            var elapsed = 0f;

            while (elapsed < Duration)
            {
                await UniTask.Yield(PlayerLoopTiming.Update);
                elapsed += Time.unscaledDeltaTime;
                ReportInitProgress(elapsed / Duration);
            }
        }
    }

    internal sealed class AppConfigLoadingStep : LoadingStepService
    {
        protected override float Duration => 0.25f;
    }

    internal sealed class AppAudioLoadingStep : LoadingStepService
    {
        protected override float Duration => 0.35f;
    }

    internal sealed class TitleUiLoadingStep : LoadingStepService
    {
        protected override float Duration => 0.30f;
    }

    internal sealed class TitleDataLoadingStep : LoadingStepService
    {
        protected override float Duration => 0.45f;
    }

    internal sealed class GameDataLoadingStep : LoadingStepService
    {
        protected override float Duration => 0.35f;
    }

    internal sealed class GameInputLoadingStep : LoadingStepService
    {
        protected override float Duration => 0.30f;
    }

    internal sealed class TerrainLoadingStep : LoadingStepService
    {
        protected override float Duration => 0.55f;
    }

    internal sealed class ActorsLoadingStep : LoadingStepService
    {
        protected override float Duration => 0.45f;
    }

    internal sealed class NavMeshLoadingStep : LoadingStepService
    {
        protected override float Duration => 0.60f;
    }
}
