using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Fang.Framework.Async
{
    public abstract class AsyncLifecycleBehaviour : MonoBehaviour, IAsyncLifecycle
    {
        private UniTaskCompletionSource _initializationSource;
        private UniTaskCompletionSource _disposalSource;
        private float _initProgress;
        private bool _started;
        private bool _disposed;

        public bool IsInitialized { get; private set; }

        public bool IsInitializing => _started && !_disposed && !IsInitialized;

        public float InitProgress => _initProgress;

        public UniTask Initialization { get; private set; } = UniTask.CompletedTask;

        public UniTask Disposal { get; private set; } = UniTask.CompletedTask;

        public event Action<AsyncLifecycleBehaviour> InitStarted;

        public event Action<AsyncLifecycleBehaviour, float> InitProgressChanged;

        public event Action<AsyncLifecycleBehaviour> InitCompleted;

        private protected bool HasStarted => _started;

        public virtual UniTask InitAsync()
        {
            if (_started)
            {
                return Initialization;
            }

            _started = true;
            _initializationSource = new UniTaskCompletionSource();
            Initialization = _initializationSource.Task;
            RunAsync().Forget();
            return Initialization;
        }

        public UniTask DisposeAsync()
        {
            if (_disposed || !_started)
            {
                return Disposal;
            }

            _disposed = true;
            _disposalSource = new UniTaskCompletionSource();
            Disposal = _disposalSource.Task;
            RunDisposalAsync().Forget();
            return Disposal;
        }

        public void ReportInitProgress(float value)
        {
            value = Mathf.Clamp01(value);

            if (value == _initProgress)
            {
                return;
            }

            _initProgress = value;
            InitProgressChanged?.Invoke(this, value);
        }

        protected virtual UniTask OnInitAsync()
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask OnDisposeAsync()
        {
            return UniTask.CompletedTask;
        }

        private protected virtual UniTask OnDisposeCoreAsync()
        {
            return UniTask.CompletedTask;
        }

        private async UniTask RunAsync()
        {
            InitStarted?.Invoke(this);

            try
            {
                await OnInitAsync();

                if (_disposed)
                {
                    return;
                }

                _initProgress = 1f;
                IsInitialized = true;
                _initializationSource.TrySetResult();
                InitCompleted?.Invoke(this);
            }
            catch (Exception exception)
            {
                _initializationSource.TrySetException(exception);
            }
        }

        private async UniTask RunDisposalAsync()
        {
            Exception failure = null;

            try
            {
                IsInitialized = false;
                _initializationSource?.TrySetCanceled();

                await OnDisposeAsync();
                await OnDisposeCoreAsync();

                InitStarted = null;
                InitProgressChanged = null;
                InitCompleted = null;
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            if (failure == null)
            {
                _disposalSource.TrySetResult();
            }
            else
            {
                _disposalSource.TrySetException(failure);
            }
        }
    }
}
