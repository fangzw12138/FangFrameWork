using System;
using System.Collections.Generic;

namespace Fang.Framework.Editor.Hub
{
    [Serializable]
    public sealed class FangHubGroup
    {
        public string id;
        public string name;
        public bool expanded = true;
        public List<string> pageIds = new List<string>();
    }

    [Serializable]
    public sealed class FangHubLayout
    {
        public List<FangHubGroup> groups = new List<FangHubGroup>();
        public List<string> seenPageIds = new List<string>();

        public static FangHubLayout CreateDefault(IReadOnlyList<FangHubPageDescriptor> pages)
        {
            if (pages == null)
            {
                throw new ArgumentNullException(nameof(pages));
            }

            var layout = new FangHubLayout();
            foreach (var page in Sort(pages))
            {
                var group = layout.EnsureCategoryGroup(page.Category);
                if (!group.pageIds.Contains(page.Id))
                {
                    group.pageIds.Add(page.Id);
                }

                layout.MarkSeen(page.Id);
            }

            return layout;
        }

        public void Reconcile(IReadOnlyList<FangHubPageDescriptor> pages)
        {
            if (pages == null)
            {
                throw new ArgumentNullException(nameof(pages));
            }

            Normalize();

            var map = BuildMap(pages);
            foreach (var page in Sort(pages))
            {
                if (FindGroupIdOf(page.Id) != null)
                {
                    MarkSeen(page.Id);
                    continue;
                }

                if (seenPageIds.Contains(page.Id))
                {
                    continue;
                }

                InsertByOrder(EnsureCategoryGroup(page.Category), page, map);
                MarkSeen(page.Id);
            }
        }

        public List<string> GetHiddenPageIds(IReadOnlyList<FangHubPageDescriptor> pages)
        {
            if (pages == null)
            {
                throw new ArgumentNullException(nameof(pages));
            }

            var hidden = new List<string>();
            foreach (var page in Sort(pages))
            {
                if (FindGroupIdOf(page.Id) == null)
                {
                    hidden.Add(page.Id);
                }
            }

            return hidden;
        }

        public void AddPage(string pageId, string groupId)
        {
            if (string.IsNullOrEmpty(pageId))
            {
                return;
            }

            var group = FindGroup(groupId);
            if (group == null || group.pageIds.Contains(pageId))
            {
                return;
            }

            RemoveFromGroups(pageId);
            group.pageIds.Add(pageId);
            MarkSeen(pageId);
        }

        public void RemovePage(string pageId)
        {
            if (string.IsNullOrEmpty(pageId))
            {
                return;
            }

            RemoveFromGroups(pageId);
            MarkSeen(pageId);
        }

        public void ReorderPage(string groupId, string pageId, int insertIndex)
        {
            var group = FindGroup(groupId);
            if (group == null)
            {
                return;
            }

            var currentIndex = group.pageIds.IndexOf(pageId);
            if (currentIndex < 0)
            {
                return;
            }

            group.pageIds.RemoveAt(currentIndex);
            group.pageIds.Insert(Clamp(Adjust(insertIndex, currentIndex), group.pageIds.Count), pageId);
        }

        public void MovePage(string pageId, string targetGroupId, int insertIndex)
        {
            if (string.IsNullOrEmpty(pageId))
            {
                return;
            }

            var target = FindGroup(targetGroupId);
            if (target == null)
            {
                return;
            }

            RemoveFromGroups(pageId);
            var index = insertIndex < 0 ? target.pageIds.Count : Clamp(insertIndex, target.pageIds.Count);
            target.pageIds.Insert(index, pageId);
            MarkSeen(pageId);
        }

        public string CreateGroup(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Normalize();

            var group = new FangHubGroup
            {
                id = Guid.NewGuid().ToString("N"),
                name = name
            };

            groups.Add(group);
            return group.id;
        }

        public void RenameGroup(string groupId, string name)
        {
            var group = FindGroup(groupId);
            if (group == null || string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            group.name = name.Trim();
        }

        public void DeleteGroup(string groupId)
        {
            var index = FindGroupIndex(groupId);
            if (index < 0)
            {
                return;
            }

            groups.RemoveAt(index);
        }

        public void MoveGroup(int fromIndex, int toIndex)
        {
            Normalize();

            if (fromIndex < 0 || fromIndex >= groups.Count)
            {
                return;
            }

            if (toIndex < 0 || toIndex > groups.Count)
            {
                return;
            }

            if (fromIndex == toIndex)
            {
                return;
            }

            var group = groups[fromIndex];
            groups.RemoveAt(fromIndex);
            groups.Insert(Clamp(Adjust(toIndex, fromIndex), groups.Count), group);
        }

        public void SetExpanded(string groupId, bool expanded)
        {
            var group = FindGroup(groupId);
            if (group == null)
            {
                return;
            }

            group.expanded = expanded;
        }

        internal void Normalize()
        {
            if (groups == null)
            {
                groups = new List<FangHubGroup>();
            }

            if (seenPageIds == null)
            {
                seenPageIds = new List<string>();
            }

            for (var i = groups.Count - 1; i >= 0; i--)
            {
                var group = groups[i];
                if (group == null)
                {
                    groups.RemoveAt(i);
                    continue;
                }

                if (group.pageIds == null)
                {
                    group.pageIds = new List<string>();
                }

                if (string.IsNullOrEmpty(group.id))
                {
                    group.id = Guid.NewGuid().ToString("N");
                }

                if (group.name == null)
                {
                    group.name = group.id;
                }
            }
        }

        private static List<FangHubPageDescriptor> Sort(IReadOnlyList<FangHubPageDescriptor> pages)
        {
            var sorted = new List<FangHubPageDescriptor>(pages);
            sorted.Sort(Compare);
            return sorted;
        }

        private static Dictionary<string, FangHubPageDescriptor> BuildMap(IReadOnlyList<FangHubPageDescriptor> pages)
        {
            var map = new Dictionary<string, FangHubPageDescriptor>();
            for (var i = 0; i < pages.Count; i++)
            {
                var page = pages[i];
                if (page != null)
                {
                    map[page.Id] = page;
                }
            }

            return map;
        }

        private static int Compare(FangHubPageDescriptor left, FangHubPageDescriptor right)
        {
            var orderCompare = left.Order.CompareTo(right.Order);
            return orderCompare != 0 ? orderCompare : string.CompareOrdinal(left.Title, right.Title);
        }

        private static int Adjust(int insertIndex, int removedIndex)
        {
            return removedIndex < insertIndex ? insertIndex - 1 : insertIndex;
        }

        private static int Clamp(int index, int count)
        {
            if (index < 0)
            {
                return 0;
            }

            return index > count ? count : index;
        }

        private FangHubGroup EnsureCategoryGroup(string category)
        {
            for (var i = 0; i < groups.Count; i++)
            {
                if (groups[i].name == category)
                {
                    return groups[i];
                }
            }

            var id = category;
            if (FindGroup(id) != null)
            {
                id = Guid.NewGuid().ToString("N");
            }

            var group = new FangHubGroup
            {
                id = id,
                name = category
            };

            groups.Add(group);
            return group;
        }

        private void InsertByOrder(FangHubGroup group, FangHubPageDescriptor page, Dictionary<string, FangHubPageDescriptor> map)
        {
            var index = group.pageIds.Count;
            for (var i = 0; i < group.pageIds.Count; i++)
            {
                if (!map.TryGetValue(group.pageIds[i], out var existing))
                {
                    continue;
                }

                if (Compare(existing, page) > 0)
                {
                    index = i;
                    break;
                }
            }

            group.pageIds.Insert(index, page.Id);
        }

        private void RemoveFromGroups(string pageId)
        {
            for (var i = 0; i < groups.Count; i++)
            {
                groups[i].pageIds.Remove(pageId);
            }
        }

        private string FindGroupIdOf(string pageId)
        {
            for (var i = 0; i < groups.Count; i++)
            {
                if (groups[i].pageIds.Contains(pageId))
                {
                    return groups[i].id;
                }
            }

            return null;
        }

        private FangHubGroup FindGroup(string groupId)
        {
            var index = FindGroupIndex(groupId);
            return index < 0 ? null : groups[index];
        }

        private int FindGroupIndex(string groupId)
        {
            if (string.IsNullOrEmpty(groupId))
            {
                return -1;
            }

            for (var i = 0; i < groups.Count; i++)
            {
                if (groups[i].id == groupId)
                {
                    return i;
                }
            }

            return -1;
        }

        private void MarkSeen(string pageId)
        {
            if (!seenPageIds.Contains(pageId))
            {
                seenPageIds.Add(pageId);
            }
        }
    }
}
