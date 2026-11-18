using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Fang.Framework.UI.Async.Tests
{
    public sealed class FocusTargetTests
    {
        [Test]
        public void OnSelect_raises_any_selected()
        {
            var host = new GameObject("FocusTargetTest", typeof(RectTransform));
            var target = host.AddComponent<FocusTarget>();
            FocusTarget selected = null;

            void Handler(FocusTarget value)
            {
                selected = value;
            }

            FocusTarget.AnySelected += Handler;

            try
            {
                target.OnSelect(new BaseEventData(EventSystem.current));
            }
            finally
            {
                FocusTarget.AnySelected -= Handler;
            }

            Assert.AreSame(target, selected);
            Object.DestroyImmediate(host);
        }

        [Test]
        public void SetNavigationEnabled_round_trips_the_original_navigation()
        {
            var host = new GameObject("FocusTargetTest", typeof(RectTransform));
            var target = host.AddComponent<FocusTarget>();
            var belowHost = new GameObject("Below", typeof(RectTransform));
            var below = belowHost.AddComponent<FocusTarget>();
            target.navigation = new Navigation
            {
                mode = Navigation.Mode.Vertical,
                selectOnDown = below
            };

            target.SetNavigationEnabled(false);

            Assert.AreEqual(Navigation.Mode.None, target.navigation.mode);
            Assert.AreSame(below, target.navigation.selectOnDown);

            target.SetNavigationEnabled(true);

            Assert.AreEqual(Navigation.Mode.Vertical, target.navigation.mode);
            Assert.AreSame(below, target.navigation.selectOnDown);

            Object.DestroyImmediate(host);
            Object.DestroyImmediate(belowHost);
        }
    }
}
