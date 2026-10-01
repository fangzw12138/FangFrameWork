using System.Collections.Generic;
using UnityEngine;

namespace Fang.Framework.UI.Kit
{
    public sealed class KitToastHost : MonoBehaviour
    {
        [Tooltip("提示预制体（带 KitToast 的 Toast 控件）。")]
        [SerializeField] private KitToast _template;

        [Tooltip("提示的父节点；留空 = 挂在本物体下。")]
        [SerializeField] private RectTransform _container;

        [Tooltip("最多同时保留的提示条数。")]
        [SerializeField] private int _maxCount = 3;

        [Tooltip("提示之间的纵向间距。")]
        [SerializeField] private float _spacing = 8f;

        [Tooltip("Show 不传时长时的停留时长（秒）。")]
        [SerializeField] private float _duration = 2f;

        private readonly List<KitToast> _pool = new List<KitToast>();

        private readonly List<KitToast> _live = new List<KitToast>();

        public KitToast Show(string message)
        {
            return Show(message, _duration);
        }

        public KitToast Show(string message, float seconds)
        {
            var toast = Rent();

            if (toast == null)
            {
                return null;
            }

            _live.Add(toast);
            toast.Show(message, seconds);
            Stack();

            return toast;
        }

        private void LateUpdate()
        {
            var dirty = false;

            for (var i = _live.Count - 1; i >= 0; i--)
            {
                if (!_live[i].IsVisible)
                {
                    _live.RemoveAt(i);
                    dirty = true;
                }
            }

            if (dirty)
            {
                Stack();
            }
        }

        private KitToast Rent()
        {
            if (_template == null)
            {
                Debug.LogError("[KitToastHost] 没有配置提示预制体。", this);
                return null;
            }

            for (var i = 0; i < _pool.Count; i++)
            {
                if (!_pool[i].gameObject.activeSelf)
                {
                    return _pool[i];
                }
            }

            if (_maxCount <= 0 || _pool.Count < _maxCount)
            {
                var created = Instantiate(_template, _container != null ? _container : transform);
                created.name = _template.name;
                _pool.Add(created);
                return created;
            }

            var oldest = _pool[0];
            _pool.RemoveAt(0);
            _pool.Add(oldest);

            return oldest;
        }

        private void Stack()
        {
            var offset = 0f;

            for (var i = 0; i < _live.Count; i++)
            {
                var toast = _live[i];

                if (!toast.IsVisible)
                {
                    continue;
                }

                var rect = (RectTransform)toast.transform;
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -offset);
                offset += rect.rect.height + _spacing;
            }
        }
    }
}
