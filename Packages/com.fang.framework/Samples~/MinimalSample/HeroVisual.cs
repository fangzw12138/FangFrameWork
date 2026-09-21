using UnityEngine;

namespace Fang.Framework.MinimalSample
{
    public sealed class HeroVisual : MonoBehaviour
    {
        private HeroController _controller;
        private GameObject _model;

        public void Initialize(HeroController controller)
        {
            _controller = controller;

            _model = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _model.name = _controller.Data.Config.DisplayName;
            _model.transform.SetParent(transform, false);
        }

        public void OnDispose()
        {
            Destroy(_model);
        }
    }
}
