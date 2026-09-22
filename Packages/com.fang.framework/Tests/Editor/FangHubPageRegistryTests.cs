using System.Collections.Generic;
using System.Linq;
using Fang.Framework.Editor.Hub;
using Fang.Framework.Editor.Hub.Pages;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Fang.Framework.Editor.Tests
{
    public class FangHubPageRegistryTests
    {
        [Test]
        public void Discover_finds_the_builtin_extension_packages_page()
        {
            var page = FangHubPageRegistry.Discover().FirstOrDefault(entry => entry.Id == "framework-extension-packages");

            Assert.IsNotNull(page);
            Assert.AreEqual("扩展包", page.Title);
            Assert.AreEqual("Framework", page.Category);
            Assert.AreEqual(0, page.Order);
            Assert.AreEqual(typeof(ExtensionPackagesPage), page.PageType);
        }

        [Test]
        public void Discover_skips_types_without_the_page_interface()
        {
            Assert.IsFalse(Contains("tests-no-page-interface"));
        }

        [Test]
        public void Discover_skips_types_without_a_render_interface()
        {
            Assert.IsFalse(Contains("tests-no-render-interface"));
        }

        [Test]
        public void Discover_skips_abstract_types()
        {
            Assert.IsFalse(Contains("tests-abstract"));
        }

        [Test]
        public void BuildView_keeps_the_layout_group_order_and_drops_unknown_ids()
        {
            var pages = Pages(Page("a", "A", "A"), Page("b", "B", "B"));
            var layout = FangHubLayout.CreateDefault(pages);
            layout.groups[0].pageIds.Add("ghost");

            var views = FangHubPageRegistry.BuildView(pages, layout, null);

            Assert.AreEqual(2, views.Count);
            Assert.AreEqual("A", views[0].Group.name);
            Assert.AreEqual(1, views[0].Pages.Count);
            Assert.AreEqual("a", views[0].Pages[0].Id);
            Assert.AreEqual("B", views[1].Group.name);
        }

        [Test]
        public void BuildView_filters_by_title_description_and_category()
        {
            var pages = Pages(
                Page("a", "扩展包", "Framework"),
                Page("b", "音频", "Framework", "音频工具"),
                Page("c", "场景", "Scene"));
            var layout = FangHubLayout.CreateDefault(pages);

            var byTitle = FangHubPageRegistry.BuildView(pages, layout, "扩展");
            Assert.AreEqual(1, byTitle.Count);
            Assert.AreEqual("a", byTitle[0].Pages[0].Id);

            var byDescription = FangHubPageRegistry.BuildView(pages, layout, "音频工具");
            Assert.AreEqual(1, byDescription.Count);
            Assert.AreEqual("b", byDescription[0].Pages[0].Id);

            var byCategory = FangHubPageRegistry.BuildView(pages, layout, "scene");
            Assert.AreEqual(1, byCategory.Count);
            Assert.AreEqual("c", byCategory[0].Pages[0].Id);
        }

        [Test]
        public void BuildView_keeps_empty_groups_only_without_search()
        {
            var pages = Pages(Page("a", "A", "A"), Page("b", "B", "B"));
            var layout = FangHubLayout.CreateDefault(pages);
            layout.CreateGroup("空分组");

            Assert.AreEqual(3, FangHubPageRegistry.BuildView(pages, layout, null).Count);
            Assert.AreEqual(3, FangHubPageRegistry.BuildView(pages, layout, "  ").Count);

            var filtered = FangHubPageRegistry.BuildView(pages, layout, "B");
            Assert.AreEqual(1, filtered.Count);
            Assert.AreEqual("B", filtered[0].Group.name);
        }

        private static bool Contains(string id)
        {
            return FangHubPageRegistry.Discover().Any(entry => entry.Id == id);
        }

        private static FangHubPageDescriptor Page(string id, string title, string category, string description = "")
        {
            return new FangHubPageDescriptor(id, title, category, description, 0, typeof(FangHubPageRegistryTests));
        }

        private static List<FangHubPageDescriptor> Pages(params FangHubPageDescriptor[] pages)
        {
            return new List<FangHubPageDescriptor>(pages);
        }
    }

    [FangHubPage("tests-no-page-interface", "缺少页面接口", "测试")]
    public class FangHubProbeWithoutPageInterface
    {
    }

    [FangHubPage("tests-no-render-interface", "缺少渲染接口", "测试")]
    public class FangHubProbeWithoutRenderInterface : IFangHubPage
    {
        public void OnInitialize(FangHubWindow window)
        {
        }

        public void OnSelected()
        {
        }
    }

    [FangHubPage("tests-abstract", "抽象页面", "测试")]
    public abstract class FangHubAbstractProbe : IFangHubPage, IFangHubVisualElementPage
    {
        public void OnInitialize(FangHubWindow window)
        {
        }

        public void OnSelected()
        {
        }

        public VisualElement CreateVisualElement()
        {
            return null;
        }
    }
}
