using System;
using System.Collections.Generic;

namespace Fang.Framework.Editor.Hub
{
    public sealed class FangHubGroupView
    {
        public FangHubGroupView(FangHubGroup group, List<FangHubPageDescriptor> pages)
        {
            Group = group ?? throw new ArgumentNullException(nameof(group));
            Pages = pages ?? throw new ArgumentNullException(nameof(pages));
        }

        public FangHubGroup Group { get; }

        public List<FangHubPageDescriptor> Pages { get; }
    }
}
