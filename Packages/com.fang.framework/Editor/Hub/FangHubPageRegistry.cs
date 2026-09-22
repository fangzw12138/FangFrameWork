using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Fang.Framework.Editor.Hub
{
    public static class FangHubPageRegistry
    {
        public static IReadOnlyList<FangHubPageDescriptor> Discover()
        {
            var descriptors = new List<FangHubPageDescriptor>();
            var ids = new HashSet<string>();

            foreach (var type in TypeCache.GetTypesWithAttribute<FangHubPageAttribute>())
            {
                if (type.IsAbstract || type.IsInterface)
                {
                    continue;
                }

                if (!typeof(IFangHubPage).IsAssignableFrom(type))
                {
                    continue;
                }

                if (!typeof(IFangHubImGuiPage).IsAssignableFrom(type)
                    && !typeof(IFangHubVisualElementPage).IsAssignableFrom(type))
                {
                    continue;
                }

                var attribute = ReadAttribute(type);
                if (attribute == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(attribute.Id)
                    || string.IsNullOrWhiteSpace(attribute.Title)
                    || string.IsNullOrWhiteSpace(attribute.Category))
                {
                    Debug.LogWarning("[FangHub] 页面 " + type.FullName + " 的 Id / Title / Category 不能为空，已跳过。");
                    continue;
                }

                if (type.GetConstructor(Type.EmptyTypes) == null)
                {
                    Debug.LogWarning("[FangHub] 页面 " + type.FullName + " 缺少公开无参构造函数，已跳过。");
                    continue;
                }

                if (!ids.Add(attribute.Id))
                {
                    Debug.LogWarning("[FangHub] 页面 Id " + attribute.Id + " 重复，" + type.FullName + " 已跳过。");
                    continue;
                }

                descriptors.Add(new FangHubPageDescriptor(
                    attribute.Id,
                    attribute.Title,
                    attribute.Category,
                    attribute.Description,
                    attribute.Order,
                    type));
            }

            descriptors.Sort(Compare);
            return descriptors;
        }

        public static List<FangHubGroupView> BuildView(
            IReadOnlyList<FangHubPageDescriptor> pages,
            FangHubLayout layout,
            string search)
        {
            if (pages == null)
            {
                throw new ArgumentNullException(nameof(pages));
            }

            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            var views = new List<FangHubGroupView>();
            layout.Normalize();

            var map = new Dictionary<string, FangHubPageDescriptor>();
            for (var i = 0; i < pages.Count; i++)
            {
                var page = pages[i];
                if (page != null)
                {
                    map[page.Id] = page;
                }
            }

            var filter = search == null ? string.Empty : search.Trim();

            foreach (var group in layout.groups)
            {
                var groupPages = new List<FangHubPageDescriptor>();
                foreach (var pageId in group.pageIds)
                {
                    if (!map.TryGetValue(pageId, out var page))
                    {
                        continue;
                    }

                    if (filter.Length > 0 && !Matches(page, filter))
                    {
                        continue;
                    }

                    groupPages.Add(page);
                }

                if (filter.Length > 0 && groupPages.Count == 0)
                {
                    continue;
                }

                views.Add(new FangHubGroupView(group, groupPages));
            }

            return views;
        }

        private static FangHubPageAttribute ReadAttribute(Type type)
        {
            var attributes = type.GetCustomAttributes(typeof(FangHubPageAttribute), false);
            return attributes.Length == 0 ? null : attributes[0] as FangHubPageAttribute;
        }

        private static bool Matches(FangHubPageDescriptor page, string filter)
        {
            return Contains(page.Title, filter)
                || Contains(page.Description, filter)
                || Contains(page.Category, filter);
        }

        private static bool Contains(string value, string filter)
        {
            return !string.IsNullOrEmpty(value) && value.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static int Compare(FangHubPageDescriptor left, FangHubPageDescriptor right)
        {
            var orderCompare = left.Order.CompareTo(right.Order);
            return orderCompare != 0 ? orderCompare : string.CompareOrdinal(left.Title, right.Title);
        }
    }
}
