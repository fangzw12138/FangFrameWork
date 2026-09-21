using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fang.Framework.Editor
{
    [Serializable]
    public sealed class ExtensionPackageEntry
    {
        public string name;
        public string displayName;
        public string description;
        public string path;
        public string version;
        public string tag;
        public string unity;
    }

    [Serializable]
    public sealed class ExtensionPackageIndexDocument
    {
        public string repository;
        public ExtensionPackageEntry core;
        public ExtensionPackageEntry[] packages;
    }

    public enum ExtensionPackageState
    {
        NotInstalled,
        Installed,
        UpdateAvailable,
        LocalAhead,
        Embedded
    }

    public static class ExtensionPackageIndex
    {
        public static ExtensionPackageIndexDocument Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            ExtensionPackageIndexDocument document;
            try
            {
                document = JsonUtility.FromJson<ExtensionPackageIndexDocument>(json);
            }
            catch (ArgumentException)
            {
                return null;
            }

            if (document == null)
            {
                return null;
            }

            document.repository = NormalizeRepository(document.repository);
            if (string.IsNullOrEmpty(document.repository))
            {
                return null;
            }

            if (document.core == null || string.IsNullOrEmpty(document.core.name))
            {
                return null;
            }

            document.packages = FilterValidEntries(document.packages);
            return document;
        }

        public static string NormalizeRepository(string repository)
        {
            if (string.IsNullOrWhiteSpace(repository))
            {
                return string.Empty;
            }

            var value = repository.Trim().TrimEnd('/');
            if (value.Length == 0)
            {
                return string.Empty;
            }

            if (value.StartsWith("git+", StringComparison.Ordinal))
            {
                return value;
            }

            if (value.EndsWith(".git", StringComparison.Ordinal))
            {
                return value;
            }

            if (value.StartsWith("http://", StringComparison.Ordinal)
                || value.StartsWith("https://", StringComparison.Ordinal)
                || value.StartsWith("ssh://", StringComparison.Ordinal))
            {
                return value + ".git";
            }

            return value;
        }

        public static string BuildInstallUrl(string repository, ExtensionPackageEntry entry)
        {
            if (string.IsNullOrEmpty(repository) || entry == null)
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(entry.name) || string.IsNullOrEmpty(entry.tag))
            {
                return string.Empty;
            }

            var path = entry.path == null ? string.Empty : entry.path.Trim();
            if (path.Length == 0)
            {
                return string.Empty;
            }

            if (!path.StartsWith("/", StringComparison.Ordinal))
            {
                path = "/" + path;
            }

            return repository + "?path=" + path + "#" + entry.tag.Trim();
        }

        public static bool IsNewer(string candidate, string installed)
        {
            if (string.IsNullOrEmpty(candidate))
            {
                return false;
            }

            if (string.IsNullOrEmpty(installed))
            {
                return true;
            }

            SplitVersion(candidate, out var candidateNumbers, out var candidatePreRelease);
            SplitVersion(installed, out var installedNumbers, out var installedPreRelease);

            var length = Math.Max(candidateNumbers.Count, installedNumbers.Count);
            for (var i = 0; i < length; i++)
            {
                var left = i < candidateNumbers.Count ? candidateNumbers[i] : 0;
                var right = i < installedNumbers.Count ? installedNumbers[i] : 0;
                if (left != right)
                {
                    return left > right;
                }
            }

            if (candidatePreRelease.Length == 0)
            {
                return installedPreRelease.Length > 0;
            }

            if (installedPreRelease.Length == 0)
            {
                return false;
            }

            return string.CompareOrdinal(candidatePreRelease, installedPreRelease) > 0;
        }

        public static ExtensionPackageState ResolveState(string indexVersion, string installedVersion, bool isEmbedded)
        {
            if (isEmbedded)
            {
                return ExtensionPackageState.Embedded;
            }

            if (string.IsNullOrEmpty(installedVersion))
            {
                return ExtensionPackageState.NotInstalled;
            }

            if (IsNewer(indexVersion, installedVersion))
            {
                return ExtensionPackageState.UpdateAvailable;
            }

            if (IsNewer(installedVersion, indexVersion))
            {
                return ExtensionPackageState.LocalAhead;
            }

            return ExtensionPackageState.Installed;
        }

        private static ExtensionPackageEntry[] FilterValidEntries(ExtensionPackageEntry[] entries)
        {
            if (entries == null)
            {
                return new ExtensionPackageEntry[0];
            }

            var valid = new List<ExtensionPackageEntry>();
            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (entry == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(entry.name) || string.IsNullOrEmpty(entry.path) || string.IsNullOrEmpty(entry.tag))
                {
                    continue;
                }

                valid.Add(entry);
            }

            return valid.ToArray();
        }

        private static void SplitVersion(string version, out List<int> numbers, out string preRelease)
        {
            numbers = new List<int>();
            preRelease = string.Empty;

            var value = version.Trim();
            var separatorIndex = value.IndexOf('-');
            if (separatorIndex >= 0)
            {
                preRelease = value.Substring(separatorIndex + 1);
                value = value.Substring(0, separatorIndex);
            }

            var segments = value.Split('.');
            for (var i = 0; i < segments.Length; i++)
            {
                numbers.Add(int.TryParse(segments[i], out var number) ? number : 0);
            }
        }
    }
}
