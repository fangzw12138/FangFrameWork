using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Fang.Framework.Editor
{
    public enum ExtensionPackageFilter
    {
        All,
        Installed,
        UpdateAvailable,
        NotInstalled
    }

    public sealed class ExtensionPackageLocalInfo
    {
        public string Name;
        public string Version;
        public PackageSource Source;
        public bool IsEmbedded;
        public string Description;
        public string PackageJsonPath;
    }

    public sealed class ExtensionPackageRow
    {
        public ExtensionPackageEntry Entry;
        public ExtensionPackageLocalInfo Local;
        public string Name;
        public string DisplayName;
        public string Description;
        public string IndexVersion;
        public string InstalledVersion;
        public ExtensionPackageState State;
    }

    public static class ExtensionPackageCatalog
    {
        [Serializable]
        private sealed class PackageManifest
        {
            public string description;
        }

        public static List<ExtensionPackageRow> Build(
            ExtensionPackageIndexDocument index,
            IReadOnlyList<ExtensionPackageLocalInfo> installed,
            ExtensionPackageFilter filter,
            string search)
        {
            var rows = new List<ExtensionPackageRow>();

            if (index == null || index.packages == null)
            {
                return rows;
            }

            for (var i = 0; i < index.packages.Length; i++)
            {
                var entry = index.packages[i];
                var row = Build(entry, Find(installed, entry == null ? null : entry.name));

                if (Matches(row, filter, search))
                {
                    rows.Add(row);
                }
            }

            return rows;
        }

        public static ExtensionPackageRow Build(ExtensionPackageEntry entry, ExtensionPackageLocalInfo local)
        {
            var name = entry != null && !string.IsNullOrEmpty(entry.name) ? entry.name : local == null ? string.Empty : local.Name;

            return new ExtensionPackageRow
            {
                Entry = entry,
                Local = local,
                Name = name,
                DisplayName = entry != null && !string.IsNullOrEmpty(entry.displayName) ? entry.displayName : name,
                Description = ResolveDescription(entry, local),
                IndexVersion = entry == null ? string.Empty : entry.version,
                InstalledVersion = local == null ? string.Empty : local.Version,
                State = ResolveState(entry, local)
            };
        }

        public static bool Matches(ExtensionPackageRow row, ExtensionPackageFilter filter, string search)
        {
            if (row == null || !MatchesFilter(row.State, filter))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(search))
            {
                return true;
            }

            var value = search.Trim();

            return Contains(row.DisplayName, value)
                || Contains(row.Name, value)
                || Contains(row.Description, value)
                || Contains(row.Entry == null ? null : row.Entry.tag, value);
        }

        public static string ResolveDescription(ExtensionPackageEntry entry, ExtensionPackageLocalInfo local)
        {
            if (local != null && !string.IsNullOrWhiteSpace(local.Description))
            {
                return local.Description;
            }

            return entry == null || entry.description == null ? string.Empty : entry.description;
        }

        public static string DescribeSource(PackageSource source)
        {
            switch (source)
            {
                case PackageSource.Embedded:
                    return "内嵌";
                case PackageSource.Git:
                    return "Git";
                case PackageSource.Registry:
                    return "注册表";
                case PackageSource.BuiltIn:
                    return "内置";
                case PackageSource.Local:
                    return "本地";
                default:
                    return "未知";
            }
        }

        public static string GetManifestPath(string packageFolder)
        {
            if (string.IsNullOrEmpty(packageFolder))
            {
                return string.Empty;
            }

            return Path.Combine(packageFolder, "package.json");
        }

        public static bool TryReadManifestDescription(string packageFolder, out string description)
        {
            description = string.Empty;

            var path = GetManifestPath(packageFolder);
            if (path.Length == 0 || !File.Exists(path))
            {
                return false;
            }

            try
            {
                var manifest = JsonUtility.FromJson<PackageManifest>(File.ReadAllText(path));
                if (manifest == null || string.IsNullOrWhiteSpace(manifest.description))
                {
                    return false;
                }

                description = manifest.description;
                return true;
            }
            catch (Exception exception) when (exception is IOException
                || exception is UnauthorizedAccessException
                || exception is ArgumentException)
            {
                return false;
            }
        }

        private static ExtensionPackageState ResolveState(ExtensionPackageEntry entry, ExtensionPackageLocalInfo local)
        {
            var isEmbedded = local != null && local.IsEmbedded;
            var indexVersion = entry == null ? string.Empty : entry.version;

            if (string.IsNullOrEmpty(indexVersion))
            {
                if (isEmbedded)
                {
                    return ExtensionPackageState.Embedded;
                }

                return local == null || string.IsNullOrEmpty(local.Version)
                    ? ExtensionPackageState.NotInstalled
                    : ExtensionPackageState.Installed;
            }

            return ExtensionPackageIndex.ResolveState(indexVersion, local == null ? null : local.Version, isEmbedded);
        }

        private static bool MatchesFilter(ExtensionPackageState state, ExtensionPackageFilter filter)
        {
            switch (filter)
            {
                case ExtensionPackageFilter.Installed:
                    return state != ExtensionPackageState.NotInstalled;
                case ExtensionPackageFilter.UpdateAvailable:
                    return state == ExtensionPackageState.UpdateAvailable;
                case ExtensionPackageFilter.NotInstalled:
                    return state == ExtensionPackageState.NotInstalled;
                default:
                    return true;
            }
        }

        private static bool Contains(string text, string value)
        {
            return !string.IsNullOrEmpty(text)
                && text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static ExtensionPackageLocalInfo Find(IReadOnlyList<ExtensionPackageLocalInfo> installed, string name)
        {
            if (installed == null || string.IsNullOrEmpty(name))
            {
                return null;
            }

            for (var i = 0; i < installed.Count; i++)
            {
                var candidate = installed[i];
                if (candidate != null && string.Equals(candidate.Name, name, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
