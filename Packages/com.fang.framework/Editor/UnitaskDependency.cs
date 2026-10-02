using System;
using System.IO;

namespace Fang.Framework.Editor
{
    public static class UnitaskDependency
    {
        public const string PackageName = "com.cysharp.unitask";
        public const string Version = "2.5.11";
        public const string RegistryName = "package.openupm.com";
        public const string RegistryUrl = "https://package.openupm.com";
        public const string ManifestPath = "Packages/manifest.json";

        private const string ScopedRegistriesKey = "\"scopedRegistries\"";
        private const string DependenciesKey = "\"dependencies\"";
        private const string PackageToken = "\"com.cysharp.unitask\"";

        private static string _backupPath;
        private static string _backupText;

        public static string Identifier => PackageName + "@" + Version;

        public static bool IsConfigured(string manifestText)
        {
            return !string.IsNullOrEmpty(manifestText)
                && manifestText.IndexOf(RegistryUrl, StringComparison.Ordinal) >= 0
                && HasScopeEntry(manifestText);
        }

        public static bool TryBuildManifest(string manifestText, out string updatedText, out string error)
        {
            updatedText = manifestText;
            error = null;

            if (string.IsNullOrEmpty(manifestText))
            {
                error = ManifestPath + " 是空的，无法写入注册表。";
                return false;
            }

            if (manifestText.IndexOf(ScopedRegistriesKey, StringComparison.Ordinal) >= 0)
            {
                if (IsConfigured(manifestText))
                {
                    return true;
                }

                error = ManifestPath + " 里已有 scopedRegistries 段，但没有 OpenUPM（" + RegistryUrl
                    + "）的 " + PackageName + " 作用域。不自动改写已有注册表配置，请按「快速开始 → 首次安装」手动补一条。";
                return false;
            }

            var anchor = FindDependenciesLineStart(manifestText);
            if (anchor < 0)
            {
                error = ManifestPath + " 里找不到顶层 " + DependenciesKey + " 段，无法定位插入点。";
                return false;
            }

            updatedText = manifestText.Insert(anchor, BuildRegistryBlock());
            return true;
        }

        public static bool TryEnsureRegistry(out bool changed, out string error)
        {
            changed = false;
            error = null;

            string text;
            try
            {
                text = File.ReadAllText(Path.GetFullPath(ManifestPath));
            }
            catch (Exception exception)
            {
                error = "读取 " + ManifestPath + " 失败：" + exception.Message;
                return false;
            }

            string updated;
            if (!TryBuildManifest(text, out updated, out error))
            {
                return false;
            }

            if (string.Equals(updated, text, StringComparison.Ordinal))
            {
                return true;
            }

            try
            {
                File.WriteAllText(Path.GetFullPath(ManifestPath), updated);
            }
            catch (Exception exception)
            {
                error = "写入 " + ManifestPath + " 失败：" + exception.Message;
                return false;
            }

            _backupPath = ManifestPath;
            _backupText = text;
            changed = true;
            return true;
        }

        public static bool TryRestore(out string error)
        {
            error = null;

            var path = _backupPath;
            var text = _backupText;
            _backupPath = null;
            _backupText = null;

            if (text == null)
            {
                return true;
            }

            try
            {
                File.WriteAllText(Path.GetFullPath(path), text);
                return true;
            }
            catch (Exception exception)
            {
                error = "还原 " + path + " 失败：" + exception.Message;
                return false;
            }
        }

        public static void Commit()
        {
            _backupPath = null;
            _backupText = null;
        }

        private static bool HasScopeEntry(string manifestText)
        {
            var index = manifestText.IndexOf(PackageToken, StringComparison.Ordinal);

            while (index >= 0)
            {
                var next = index + PackageToken.Length;
                while (next < manifestText.Length && char.IsWhiteSpace(manifestText[next]))
                {
                    next++;
                }

                if (next >= manifestText.Length || manifestText[next] != ':')
                {
                    return true;
                }

                index = manifestText.IndexOf(PackageToken, index + PackageToken.Length, StringComparison.Ordinal);
            }

            return false;
        }

        private static int FindDependenciesLineStart(string manifestText)
        {
            var index = manifestText.IndexOf(DependenciesKey, StringComparison.Ordinal);
            if (index < 0)
            {
                return -1;
            }

            var lineStart = manifestText.LastIndexOf('\n', index);
            return lineStart < 0 ? -1 : lineStart + 1;
        }

        private static string BuildRegistryBlock()
        {
            return "  " + ScopedRegistriesKey + ": [\n"
                + "    {\n"
                + "      \"name\": \"" + RegistryName + "\",\n"
                + "      \"url\": \"" + RegistryUrl + "\",\n"
                + "      \"scopes\": [\n"
                + "        \"" + PackageName + "\"\n"
                + "      ]\n"
                + "    }\n"
                + "  ],\n";
        }
    }
}
