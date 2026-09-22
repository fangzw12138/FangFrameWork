using System;

namespace Fang.Framework.Editor.Hub
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class FangHubPageAttribute : Attribute
    {
        public FangHubPageAttribute(string id, string title, string category)
        {
            Id = id ?? string.Empty;
            Title = title ?? string.Empty;
            Category = category ?? string.Empty;
        }

        public string Id { get; }

        public string Title { get; }

        public string Category { get; }

        public string Description { get; set; }

        public int Order { get; set; }
    }
}
