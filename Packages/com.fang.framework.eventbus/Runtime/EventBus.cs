using System;
using System.Collections.Generic;

namespace Fang.Framework.EventBus
{
    public sealed class EventBus : Service
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();

        public void Subscribe<T>(Action<T> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                _handlers.Add(type, list);
            }

            if (!list.Contains(handler))
            {
                list.Add(handler);
            }
        }

        public void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null)
            {
                return;
            }

            if (!_handlers.TryGetValue(typeof(T), out var list))
            {
                return;
            }

            list.Remove(handler);

            if (list.Count == 0)
            {
                _handlers.Remove(typeof(T));
            }
        }

        public void Publish<T>(T payload)
        {
            if (!_handlers.TryGetValue(typeof(T), out var list))
            {
                return;
            }

            var snapshot = list.ToArray();
            for (var i = 0; i < snapshot.Length; i++)
            {
                ((Action<T>)snapshot[i]).Invoke(payload);
            }
        }

        public override void OnDispose()
        {
            _handlers.Clear();
        }
    }
}
