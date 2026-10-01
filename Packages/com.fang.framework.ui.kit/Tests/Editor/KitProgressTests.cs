using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Fang.Framework.UI.Kit.Editor.Tests
{
    public class KitProgressTests
    {
        [TearDown]
        public void TearDown()
        {
            KitTestSetup.Cleanup();
        }

        [Test]
        public void Sliced_fill_takes_the_proportion_from_the_anchor()
        {
            var fill = NewFill(Image.Type.Sliced);
            var progress = NewProgress(fill);

            progress.SetValue(0.35f);

            Assert.AreEqual(0.35f, fill.rectTransform.anchorMax.x, 1e-4f);
            Assert.AreEqual(1f, fill.rectTransform.anchorMax.y, 1e-4f, "纵向锚点不该被改");
            Assert.AreEqual(1f, fill.fillAmount, 1e-4f, "Sliced 填充不该去动 fillAmount");
            Assert.AreEqual(0.35f, progress.Normalized, 1e-4f);

            progress.SetValue(1.5f);
            Assert.AreEqual(1f, fill.rectTransform.anchorMax.x, 1e-4f, "超过 1 要夹到 1");

            progress.SetValue(-0.5f);
            Assert.AreEqual(0f, fill.rectTransform.anchorMax.x, 1e-4f, "小于 0 要夹到 0");
        }

        [Test]
        public void Filled_fill_still_takes_the_proportion_from_fill_amount()
        {
            var fill = NewFill(Image.Type.Filled);
            fill.fillMethod = Image.FillMethod.Horizontal;
            var progress = NewProgress(fill);

            progress.SetValue(0.4f);

            Assert.AreEqual(0.4f, fill.fillAmount, 1e-4f);
            Assert.AreEqual(1f, fill.rectTransform.anchorMax.x, 1e-4f, "Filled 填充不该去动锚点");
            Assert.AreEqual(0.4f, progress.Normalized, 1e-4f);
        }

        private static Image NewFill(Image.Type type)
        {
            var fill = KitTestSetup.NewComponent<Image>("Fill");
            fill.type = type;
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            return fill;
        }

        private static KitProgress NewProgress(Image fill)
        {
            var progress = KitTestSetup.NewComponent<KitProgress>("Progress");
            KitTestSetup.SetObject(progress, "_fill", fill);
            return progress;
        }
    }
}
