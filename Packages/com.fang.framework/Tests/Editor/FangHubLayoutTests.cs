using System.Collections.Generic;
using Fang.Framework.Editor.Hub;
using NUnit.Framework;

namespace Fang.Framework.Editor.Tests
{
    public class FangHubLayoutTests
    {
        [Test]
        public void CreateDefault_groups_pages_by_category_and_orders_them()
        {
            var pages = Pages(Page("b", "B", 2), Page("a", "A", 0), Page("c", "A", 1));

            var layout = FangHubLayout.CreateDefault(pages);

            Assert.AreEqual(2, layout.groups.Count);
            Assert.AreEqual("A", layout.groups[0].name);
            CollectionAssert.AreEqual(new[] { "a", "c" }, layout.groups[0].pageIds);
            Assert.AreEqual("B", layout.groups[1].name);
            CollectionAssert.AreEqual(new[] { "b" }, layout.groups[1].pageIds);
        }

        [Test]
        public void CreateDefault_orders_pages_with_equal_order_by_title()
        {
            var pages = Pages(
                new FangHubPageDescriptor("p1", "Beta", "A", string.Empty, 0, typeof(FangHubLayoutTests)),
                new FangHubPageDescriptor("p2", "Alpha", "A", string.Empty, 0, typeof(FangHubLayoutTests)));

            var layout = FangHubLayout.CreateDefault(pages);

            CollectionAssert.AreEqual(new[] { "p2", "p1" }, layout.groups[0].pageIds);
        }

        [Test]
        public void CreateDefault_marks_every_page_seen()
        {
            var layout = FangHubLayout.CreateDefault(Pages(Page("a", "A", 0), Page("b", "B", 0)));

            CollectionAssert.AreEquivalent(new[] { "a", "b" }, layout.seenPageIds);
        }

        [Test]
        public void Reconcile_inserts_a_new_page_at_its_order_position()
        {
            var layout = FangHubLayout.CreateDefault(Pages(Page("old", "A", 5)));

            layout.Reconcile(Pages(Page("old", "A", 5), Page("fresh", "A", 1)));

            CollectionAssert.AreEqual(new[] { "fresh", "old" }, layout.groups[0].pageIds);
        }

        [Test]
        public void Reconcile_creates_a_group_for_a_new_category()
        {
            var layout = FangHubLayout.CreateDefault(Pages(Page("a", "A", 0)));

            layout.Reconcile(Pages(Page("a", "A", 0), Page("z", "Z", 0)));

            Assert.AreEqual(2, layout.groups.Count);
            Assert.AreEqual("Z", layout.groups[1].name);
            CollectionAssert.AreEqual(new[] { "z" }, layout.groups[1].pageIds);
        }

        [Test]
        public void Reconcile_does_not_restore_a_removed_page()
        {
            var pages = Pages(Page("a", "A", 0), Page("b", "A", 1));
            var layout = FangHubLayout.CreateDefault(pages);

            layout.RemovePage("a");
            layout.Reconcile(pages);

            CollectionAssert.AreEqual(new[] { "b" }, layout.groups[0].pageIds);
            CollectionAssert.AreEqual(new[] { "a" }, layout.GetHiddenPageIds(pages));
        }

        [Test]
        public void Reconcile_keeps_unknown_page_ids_in_place()
        {
            var pages = Pages(Page("a", "A", 0));
            var layout = FangHubLayout.CreateDefault(pages);
            layout.groups[0].pageIds.Add("ghost");

            layout.Reconcile(pages);

            CollectionAssert.AreEqual(new[] { "a", "ghost" }, layout.groups[0].pageIds);
            Assert.AreEqual(0, layout.GetHiddenPageIds(pages).Count);
        }

        [Test]
        public void AddPage_moves_a_page_into_the_target_group()
        {
            var pages = Pages(Page("a", "A", 0), Page("b", "B", 0));
            var layout = FangHubLayout.CreateDefault(pages);
            var targetGroupId = layout.groups[1].id;

            layout.RemovePage("a");
            layout.AddPage("a", targetGroupId);

            Assert.AreEqual(0, layout.groups[0].pageIds.Count);
            CollectionAssert.AreEqual(new[] { "b", "a" }, layout.groups[1].pageIds);
            Assert.AreEqual(0, layout.GetHiddenPageIds(pages).Count);
        }

        [Test]
        public void AddPage_is_idempotent()
        {
            var layout = FangHubLayout.CreateDefault(Pages(Page("a", "A", 0)));
            var groupId = layout.groups[0].id;

            layout.AddPage("a", groupId);
            layout.AddPage("a", groupId);

            CollectionAssert.AreEqual(new[] { "a" }, layout.groups[0].pageIds);
        }

        [Test]
        public void AddPage_ignores_an_unknown_group()
        {
            var layout = FangHubLayout.CreateDefault(Pages(Page("a", "A", 0)));

            layout.AddPage("a", "missing");

            CollectionAssert.AreEqual(new[] { "a" }, layout.groups[0].pageIds);
        }

        [Test]
        public void RemovePage_moves_a_page_into_the_hidden_list()
        {
            var pages = Pages(Page("a", "A", 0), Page("b", "A", 1));
            var layout = FangHubLayout.CreateDefault(pages);

            layout.RemovePage("a");

            CollectionAssert.AreEqual(new[] { "a" }, layout.GetHiddenPageIds(pages));
        }

        [Test]
        public void GetHiddenPageIds_orders_by_order_then_title()
        {
            var pages = Pages(Page("a", "A", 0), Page("b", "B", 1), Page("c", "C", 1));
            var layout = FangHubLayout.CreateDefault(pages);

            layout.RemovePage("c");
            layout.RemovePage("b");
            layout.RemovePage("a");

            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, layout.GetHiddenPageIds(pages));
        }

        [Test]
        public void ReorderPage_moves_a_page_down_within_its_group()
        {
            var layout = FangHubLayout.CreateDefault(Pages(Page("a", "A", 0), Page("b", "A", 1), Page("c", "A", 2)));
            var groupId = layout.groups[0].id;

            layout.ReorderPage(groupId, "a", 2);

            CollectionAssert.AreEqual(new[] { "b", "a", "c" }, layout.groups[0].pageIds);
        }

        [Test]
        public void ReorderPage_moves_a_page_up_within_its_group()
        {
            var layout = FangHubLayout.CreateDefault(Pages(Page("a", "A", 0), Page("b", "A", 1), Page("c", "A", 2)));
            var groupId = layout.groups[0].id;

            layout.ReorderPage(groupId, "c", 0);

            CollectionAssert.AreEqual(new[] { "c", "a", "b" }, layout.groups[0].pageIds);
        }

        [Test]
        public void MovePage_inserts_a_page_at_the_requested_index()
        {
            var layout = FangHubLayout.CreateDefault(Pages(Page("a", "A", 0), Page("b", "A", 1), Page("c", "C", 0)));
            var targetGroupId = layout.groups[1].id;

            layout.MovePage("a", targetGroupId, 0);

            CollectionAssert.AreEqual(new[] { "b" }, layout.groups[0].pageIds);
            CollectionAssert.AreEqual(new[] { "a", "c" }, layout.groups[1].pageIds);
        }

        [Test]
        public void MovePage_appends_a_page_when_the_index_is_negative()
        {
            var layout = FangHubLayout.CreateDefault(Pages(Page("a", "A", 0), Page("c", "C", 0)));
            var targetGroupId = layout.groups[1].id;

            layout.MovePage("a", targetGroupId, -1);

            CollectionAssert.AreEqual(new[] { "c", "a" }, layout.groups[1].pageIds);
        }

        [Test]
        public void DeleteGroup_hides_the_pages_it_held()
        {
            var pages = Pages(Page("a", "A", 0), Page("b", "A", 1), Page("c", "C", 0));
            var layout = FangHubLayout.CreateDefault(pages);

            layout.DeleteGroup(layout.groups[0].id);

            Assert.AreEqual(1, layout.groups.Count);
            CollectionAssert.AreEquivalent(new[] { "a", "b" }, layout.GetHiddenPageIds(pages));
        }

        [Test]
        public void MoveGroup_reorders_groups()
        {
            var layout = FangHubLayout.CreateDefault(Pages(Page("a", "A", 0), Page("b", "B", 0), Page("c", "C", 0)));

            layout.MoveGroup(2, 0);

            Assert.AreEqual("C", layout.groups[0].name);
            Assert.AreEqual("A", layout.groups[1].name);
            Assert.AreEqual("B", layout.groups[2].name);
        }

        [Test]
        public void MoveGroup_appends_a_group_when_the_index_is_the_last_position()
        {
            var layout = FangHubLayout.CreateDefault(Pages(Page("a", "A", 0), Page("b", "B", 0), Page("c", "C", 0)));

            layout.MoveGroup(0, 3);

            Assert.AreEqual("B", layout.groups[0].name);
            Assert.AreEqual("C", layout.groups[1].name);
            Assert.AreEqual("A", layout.groups[2].name);
        }

        [Test]
        public void SetExpanded_updates_the_group_flag()
        {
            var layout = FangHubLayout.CreateDefault(Pages(Page("a", "A", 0)));
            var groupId = layout.groups[0].id;

            layout.SetExpanded(groupId, false);

            Assert.IsFalse(layout.groups[0].expanded);
        }

        [Test]
        public void RenameGroup_trims_the_name_and_ignores_blank_input()
        {
            var layout = FangHubLayout.CreateDefault(Pages(Page("a", "A", 0)));
            var groupId = layout.groups[0].id;

            layout.RenameGroup(groupId, "  重命名  ");
            Assert.AreEqual("重命名", layout.groups[0].name);

            layout.RenameGroup(groupId, "   ");
            Assert.AreEqual("重命名", layout.groups[0].name);
        }

        [Test]
        public void CreateGroup_appends_a_group_with_a_unique_id()
        {
            var layout = FangHubLayout.CreateDefault(Pages(Page("a", "A", 0)));

            var firstId = layout.CreateGroup("新分组");
            var secondId = layout.CreateGroup("新分组");

            Assert.AreEqual(3, layout.groups.Count);
            Assert.IsNotEmpty(firstId);
            Assert.AreNotEqual(firstId, secondId);
            Assert.AreEqual("新分组", layout.groups[2].name);
        }

        private static FangHubPageDescriptor Page(string id, string category, int order)
        {
            return new FangHubPageDescriptor(id, id, category, string.Empty, order, typeof(FangHubLayoutTests));
        }

        private static List<FangHubPageDescriptor> Pages(params FangHubPageDescriptor[] pages)
        {
            return new List<FangHubPageDescriptor>(pages);
        }
    }
}
