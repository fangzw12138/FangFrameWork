using System.Runtime.CompilerServices;

// 校验器 / 校验结果刻意是 internal（不想成为公开 API），但测试要直接验它。
[assembly: InternalsVisibleTo("Fang.Framework.CommandConsole.Editor.Tests")]
