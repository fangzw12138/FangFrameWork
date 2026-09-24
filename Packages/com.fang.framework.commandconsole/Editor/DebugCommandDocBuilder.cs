using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Fang.Framework.CommandConsole.Editor
{
    /// <summary>
    /// 纯逻辑文档生成：指令条目 + 生效配置 → <c>{工程相对路径: 内容}</c>。
    /// 不碰 AssetDatabase、不落盘（落盘在 <see cref="DebugCommandDocGenerator"/>），所以可以直接单测。
    ///
    /// 分类顺序 = 配置里列出的（只保留真有指令的）→ 其余按名称排序追加；没列出的分类不会被丢掉。
    /// </summary>
    public static class DebugCommandDocBuilder
    {
        /// <summary>导出指令清单的菜单路径（生成器与文档头共用，改一处即可）。</summary>
        public const string ExportMenuPath = "Tools/Fang Framework/指令/导出指令清单";

        private const string TableHeader = "| 指令 | 说明 | 实现 |\n|---|---|---|";

        /// <summary>没有指令时返回空字典（生成器据此警告并不落盘）。</summary>
        public static IReadOnlyDictionary<string, string> Build(
            IReadOnlyList<DebugCommandEntry> entries,
            DebugSettings settings)
        {
            var documents = new SortedDictionary<string, string>(StringComparer.Ordinal);

            if (entries == null || entries.Count == 0)
            {
                return documents;
            }

            settings = settings ?? DebugSettings.From(null);

            var folder = NormalizeFolder(settings.DocFolder);
            var fileName = NormalizeFileName(settings.DocFileName);
            var indexPath = IndexPath(settings);

            var categories = OrderCategories(entries, settings.CategoryOrder);

            documents[indexPath] = BuildIndex(entries, categories, settings, fileName);

            if (!settings.DocSplitByCategory)
            {
                return documents;
            }

            var domainFolder = Combine(folder, Path.GetFileNameWithoutExtension(fileName));

            for (var i = 0; i < categories.Count; i++)
            {
                var category = categories[i];
                var domainPath = UniquePath(documents, Combine(domainFolder, SanitizeFileName(category) + ".md"));

                documents[domainPath] = BuildCategoryDocument(
                    category,
                    entries,
                    RelativeLink(domainFolder, indexPath));
            }

            return documents;
        }

        /// <summary>总索引的工程相对路径（生成器用它定位「导出后要揭示的文件」）。</summary>
        public static string IndexPath(DebugSettings settings)
        {
            settings = settings ?? DebugSettings.From(null);
            return Combine(NormalizeFolder(settings.DocFolder), NormalizeFileName(settings.DocFileName));
        }

        /// <summary>分类顺序：配置里列出的（去重、只留真有指令的）→ 其余按名称排序。</summary>
        public static IReadOnlyList<string> OrderCategories(
            IReadOnlyList<DebugCommandEntry> entries,
            IReadOnlyList<string> configuredOrder)
        {
            var present = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            for (var i = 0; i < entries.Count; i++)
            {
                if (seen.Add(entries[i].Category))
                {
                    present.Add(entries[i].Category);
                }
            }

            var result = new List<string>();
            var taken = new HashSet<string>(StringComparer.Ordinal);

            if (configuredOrder != null)
            {
                for (var i = 0; i < configuredOrder.Count; i++)
                {
                    var category = configuredOrder[i];
                    if (seen.Contains(category) && taken.Add(category))
                    {
                        result.Add(category);
                    }
                }
            }

            var rest = new List<string>();
            for (var i = 0; i < present.Count; i++)
            {
                if (!taken.Contains(present[i]))
                {
                    rest.Add(present[i]);
                }
            }

            rest.Sort(StringComparer.Ordinal);
            result.AddRange(rest);

            return result;
        }

        /// <summary>工程相对目录：去空白、统一分隔符、去掉首尾斜杠；空则回落默认。</summary>
        public static string NormalizeFolder(string folder)
        {
            var value = DebugSettings.NormalizeText(folder, DebugCommandDefaults.DocFolder).Replace('\\', '/');

            while (value.StartsWith("/", StringComparison.Ordinal))
            {
                value = value.Substring(1);
            }

            while (value.EndsWith("/", StringComparison.Ordinal))
            {
                value = value.Substring(0, value.Length - 1);
            }

            return string.IsNullOrEmpty(value) ? DebugCommandDefaults.DocFolder : value;
        }

        /// <summary>文档文件名：去路径分隔符（只留文件名部分）；空则回落默认。</summary>
        public static string NormalizeFileName(string fileName)
        {
            var value = DebugSettings.NormalizeText(fileName, DebugCommandDefaults.DocFileName)
                .Replace('\\', '/');

            var slash = value.LastIndexOf('/');
            if (slash >= 0)
            {
                value = value.Substring(slash + 1);
            }

            return string.IsNullOrEmpty(value) ? DebugCommandDefaults.DocFileName : value;
        }

        /// <summary>分类名 → 合法文件名（非法字符换成下划线；全非法时用 other）。</summary>
        public static string SanitizeFileName(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return "other";
            }

            var invalid = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(category.Length);

            for (var i = 0; i < category.Length; i++)
            {
                var ch = category[i];
                var bad = ch == '/' || ch == '\\';

                if (!bad)
                {
                    for (var j = 0; j < invalid.Length; j++)
                    {
                        if (invalid[j] == ch)
                        {
                            bad = true;
                            break;
                        }
                    }
                }

                builder.Append(bad ? '_' : ch);
            }

            var result = builder.ToString().Trim().Trim('.');
            return result.Length == 0 ? "other" : result;
        }

        private static string BuildIndex(
            IReadOnlyList<DebugCommandEntry> entries,
            IReadOnlyList<string> categories,
            DebugSettings settings,
            string fileName)
        {
            var builder = new StringBuilder();

            builder.AppendLine("# 指令清单 (Debug Commands)");
            builder.AppendLine();
            builder.AppendLine("> 由编辑器菜单 `" + ExportMenuPath + "` 自动生成，改完指令请重新导出。");
            builder.AppendLine(
                "> 连接方式：游戏运行后 TCP 连接 `" + settings.ListenAddress + ":" + settings.Port
                + "`（仅 Editor / Development Build），行分隔 JSON 协议：");
            builder.AppendLine(
                "> 请求 `" + DebugCommandProtocol.RequestExample
                + "` → 响应 `{\"id\":1,\"ok\":true,\"data\":{...}}` 或 `{\"id\":1,\"ok\":false,\"error\":\"...\"}`。");
            builder.AppendLine("> 运行时精确清单用 `list_commands` 指令。");
            builder.AppendLine();
            builder.AppendLine("共 " + entries.Count + " 条指令，" + categories.Count + " 个分类。");
            builder.AppendLine();

            var domainFolder = Path.GetFileNameWithoutExtension(fileName);

            for (var i = 0; i < categories.Count; i++)
            {
                var category = categories[i];

                builder.AppendLine("## " + category);
                builder.AppendLine();

                if (settings.DocSplitByCategory)
                {
                    builder.AppendLine(
                        "> 分域文件：[" + domainFolder + "/" + SanitizeFileName(category) + ".md]("
                        + domainFolder + "/" + SanitizeFileName(category) + ".md)");
                    builder.AppendLine();
                }

                builder.AppendLine(TableHeader);
                AppendRows(builder, entries, category);
                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static string BuildCategoryDocument(
            string category,
            IReadOnlyList<DebugCommandEntry> entries,
            string indexLink)
        {
            var builder = new StringBuilder();

            builder.AppendLine("# " + category + " 域指令");
            builder.AppendLine();
            builder.AppendLine(
                "> 由编辑器菜单 `" + ExportMenuPath + "` 自动生成；权威总索引见 [" + indexLink + "](" + indexLink + ")。");
            builder.AppendLine();
            builder.AppendLine(TableHeader);
            AppendRows(builder, entries, category);
            builder.AppendLine();

            return builder.ToString();
        }

        private static void AppendRows(StringBuilder builder, IReadOnlyList<DebugCommandEntry> entries, string category)
        {
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (!string.Equals(entry.Category, category, StringComparison.Ordinal))
                {
                    continue;
                }

                builder.AppendLine(
                    "| `" + entry.Name + "` | " + Escape(entry.Description) + " | `" + Escape(entry.Source) + "` |");
            }
        }

        /// <summary>Markdown 表格单元转义：换行压成空格，竖线转义（否则表格会散架）。</summary>
        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Replace("\r", " ")
                .Replace("|", "\\|");
        }

        private static string Combine(string folder, string name)
        {
            return string.IsNullOrEmpty(folder) ? name : folder + "/" + name;
        }

        /// <summary>两个工程相对路径之间的相对链接：先吃掉公共目录前缀，再按剩余层数补 <c>../</c>。</summary>
        private static string RelativeLink(string fromFolder, string toPath)
        {
            var from = SplitSegments(fromFolder);
            var to = SplitSegments(toPath);

            var common = 0;
            while (common < from.Count
                && common < to.Count - 1
                && string.Equals(from[common], to[common], StringComparison.Ordinal))
            {
                common++;
            }

            var builder = new StringBuilder();

            for (var i = common; i < from.Count; i++)
            {
                builder.Append("../");
            }

            for (var i = common; i < to.Count; i++)
            {
                if (i > common)
                {
                    builder.Append('/');
                }

                builder.Append(to[i]);
            }

            return builder.ToString();
        }

        private static List<string> SplitSegments(string path)
        {
            var segments = new List<string>();

            if (string.IsNullOrEmpty(path))
            {
                return segments;
            }

            var parts = path.Split('/');
            for (var i = 0; i < parts.Length; i++)
            {
                if (!string.IsNullOrEmpty(parts[i]))
                {
                    segments.Add(parts[i]);
                }
            }

            return segments;
        }

        private static string UniquePath(IDictionary<string, string> existing, string path)
        {
            if (!existing.ContainsKey(path))
            {
                return path;
            }

            var extension = Path.GetExtension(path);
            var withoutExtension = path.Substring(0, path.Length - extension.Length);

            for (var i = 2; i < 1000; i++)
            {
                var candidate = withoutExtension + "_" + i + extension;
                if (!existing.ContainsKey(candidate))
                {
                    return candidate;
                }
            }

            return path;
        }
    }
}
