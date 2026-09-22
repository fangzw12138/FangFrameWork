using System;

namespace Fang.Framework.Editor.Hub
{
    public sealed class FangHubPageDescriptor
    {
        public FangHubPageDescriptor(string id, string title, string category, string description, int order, Type pageType)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Category = category ?? throw new ArgumentNullException(nameof(category));
            Description = description ?? string.Empty;
            Order = order;
            PageType = pageType ?? throw new ArgumentNullException(nameof(pageType));
        }

        public string Id { get; }

        public string Title { get; }

        public string Category { get; }

        public string Description { get; }

        public int Order { get; }

        public Type PageType { get; }
    }
}
