using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Fang.Framework.Async.LoadingTransition
{
    internal sealed class AppScope : AsyncScope
    {
        public event Action<AsyncScope> RoundPrepared;

        protected override async UniTask OnInitAsync()
        {
            AddService<AppConfigLoadingStep>();
            AddService<AppAudioLoadingStep>();
            AddService<InitDriver>();

            var title = CreateChildScope<TitleScope>();

            await UniTask.WhenAll(this.AwaitServicesAsync(), title.InitAsync());
        }

        public async UniTask EnterGame()
        {
            Debug.Log("LoadingTransition: enter game");

            await RemoveChild<TitleScope>();
            await RemoveChild<GameScope>();

            var game = CreateChildScope<GameScope>();
            RoundPrepared?.Invoke(game);
            await game.InitAsync();
        }

        public async UniTask ReturnToTitle()
        {
            Debug.Log("LoadingTransition: return to title");

            await RemoveChild<GameScope>();
            await RemoveChild<TitleScope>();

            var title = CreateChildScope<TitleScope>();
            RoundPrepared?.Invoke(title);
            await title.InitAsync();
        }
    }
}
