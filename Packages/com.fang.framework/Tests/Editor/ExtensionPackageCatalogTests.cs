using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor.PackageManager;

namespace Fang.Framework.Editor.Tests
{
    public class ExtensionPackageCatalogTests
    {
        private string _tempFolder;

        [TearDown]
        public void TearDown()
        {
            if (_tempFolder != null && Directory.Exists(_tempFolder))
            {
                Directory.Delete(_tempFolder, true);
            }

            _tempFolder = null;
        }

        [Test]
        public void Build_returns_empty_without_index()
        {
            Assert.AreEqual(0, ExtensionPackageCatalog.Build(null, null, ExtensionPackageFilter.All, null).Count);
        }

        [Test]
        public void Build_returns_empty_when_index_has_no_packages_array()
        {
            var index = new ExtensionPackageIndexDocument { repository = "https://github.com/x/y.git" };

            Assert.AreEqual(0, ExtensionPackageCatalog.Build(index, null, ExtensionPackageFilter.All, null).Count);
        }

        [Test]
        public void Build_keeps_the_index_order()
        {
            var index = Index(
                Entry("com.fang.framework.zeta", "Zeta", "0.1.0"),
                Entry("com.fang.framework.alpha", "Alpha", "0.1.0"));

            var rows = ExtensionPackageCatalog.Build(index, null, ExtensionPackageFilter.All, null);

            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual("com.fang.framework.zeta", rows[0].Name);
            Assert.AreEqual("com.fang.framework.alpha", rows[1].Name);
        }

        [Test]
        public void Build_resolves_each_row_state()
        {
            var index = Index(
                Entry("com.fang.framework.plain", "Plain", "0.1.0"),
                Entry("com.fang.framework.same", "Same", "0.1.0"),
                Entry("com.fang.framework.behind", "Behind", "0.2.0"),
                Entry("com.fang.framework.ahead", "Ahead", "0.1.0"),
                Entry("com.fang.framework.embedded", "Embedded", "0.1.0"));

            var installed = new List<ExtensionPackageLocalInfo>
            {
                Local("com.fang.framework.same", "0.1.0"),
                Local("com.fang.framework.behind", "0.1.0"),
                Local("com.fang.framework.ahead", "0.3.0"),
                Local("com.fang.framework.embedded", "0.1.0", true)
            };

            var rows = ExtensionPackageCatalog.Build(index, installed, ExtensionPackageFilter.All, null);

            Assert.AreEqual(ExtensionPackageState.NotInstalled, rows[0].State);
            Assert.AreEqual(ExtensionPackageState.Installed, rows[1].State);
            Assert.AreEqual(ExtensionPackageState.UpdateAvailable, rows[2].State);
            Assert.AreEqual(ExtensionPackageState.LocalAhead, rows[3].State);
            Assert.AreEqual(ExtensionPackageState.Embedded, rows[4].State);
        }

        [Test]
        public void Build_carries_index_and_local_versions()
        {
            var index = Index(Entry("com.fang.framework.ui", "UI", "0.2.0", "索引描述"));

            var rows = ExtensionPackageCatalog.Build(
                index,
                new List<ExtensionPackageLocalInfo> { Local("com.fang.framework.ui", "0.1.0", false, "本地描述") },
                ExtensionPackageFilter.All,
                null);

            Assert.AreEqual("UI", rows[0].DisplayName);
            Assert.AreEqual("com.fang.framework.ui", rows[0].Name);
            Assert.AreEqual("0.2.0", rows[0].IndexVersion);
            Assert.AreEqual("0.1.0", rows[0].InstalledVersion);
            Assert.AreEqual("本地描述", rows[0].Description);
            Assert.IsNotNull(rows[0].Entry);
            Assert.IsNotNull(rows[0].Local);
        }

        [Test]
        public void Build_filters_installed_rows()
        {
            var index = Index(
                Entry("com.fang.framework.plain", "Plain", "0.1.0"),
                Entry("com.fang.framework.same", "Same", "0.1.0"),
                Entry("com.fang.framework.behind", "Behind", "0.2.0"),
                Entry("com.fang.framework.embedded", "Embedded", "0.1.0"));

            var installed = new List<ExtensionPackageLocalInfo>
            {
                Local("com.fang.framework.same", "0.1.0"),
                Local("com.fang.framework.behind", "0.1.0"),
                Local("com.fang.framework.embedded", "0.1.0", true)
            };

            var rows = ExtensionPackageCatalog.Build(index, installed, ExtensionPackageFilter.Installed, null);

            Assert.AreEqual(3, rows.Count);
            Assert.AreEqual("com.fang.framework.same", rows[0].Name);
            Assert.AreEqual("com.fang.framework.behind", rows[1].Name);
            Assert.AreEqual("com.fang.framework.embedded", rows[2].Name);
        }

        [Test]
        public void Build_filters_update_available_rows()
        {
            var index = Index(
                Entry("com.fang.framework.plain", "Plain", "0.1.0"),
                Entry("com.fang.framework.same", "Same", "0.1.0"),
                Entry("com.fang.framework.behind", "Behind", "0.2.0"));

            var installed = new List<ExtensionPackageLocalInfo>
            {
                Local("com.fang.framework.same", "0.1.0"),
                Local("com.fang.framework.behind", "0.1.0")
            };

            var rows = ExtensionPackageCatalog.Build(index, installed, ExtensionPackageFilter.UpdateAvailable, null);

            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual("com.fang.framework.behind", rows[0].Name);
        }

        [Test]
        public void Build_filters_not_installed_rows()
        {
            var index = Index(
                Entry("com.fang.framework.plain", "Plain", "0.1.0"),
                Entry("com.fang.framework.same", "Same", "0.1.0"));

            var installed = new List<ExtensionPackageLocalInfo> { Local("com.fang.framework.same", "0.1.0") };

            var rows = ExtensionPackageCatalog.Build(index, installed, ExtensionPackageFilter.NotInstalled, null);

            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual("com.fang.framework.plain", rows[0].Name);
        }

        [TestCase("event bus")]
        [TestCase("EVENT")]
        [TestCase("com.fang.framework.eventbus")]
        [TestCase("发布订阅")]
        [TestCase("com.fang.framework.eventbus/v0.1.0")]
        public void Build_searches_display_name_id_description_and_tag(string search)
        {
            var index = Index(
                Entry("com.fang.framework.eventbus", "Event Bus", "0.1.0", "类型安全发布订阅。"),
                Entry("com.fang.framework.ui", "UI", "0.1.0", "面板栈。"));

            var rows = ExtensionPackageCatalog.Build(index, null, ExtensionPackageFilter.All, search);

            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual("com.fang.framework.eventbus", rows[0].Name);
        }

        [Test]
        public void Build_ignores_blank_search()
        {
            var index = Index(Entry("com.fang.framework.ui", "UI", "0.1.0"));

            Assert.AreEqual(1, ExtensionPackageCatalog.Build(index, null, ExtensionPackageFilter.All, "   ").Count);
        }

        [Test]
        public void Build_returns_nothing_when_search_matches_no_row()
        {
            var index = Index(Entry("com.fang.framework.ui", "UI", "0.1.0"));

            Assert.AreEqual(0, ExtensionPackageCatalog.Build(index, null, ExtensionPackageFilter.All, "audio").Count);
        }

        [Test]
        public void Build_combines_filter_and_search()
        {
            var index = Index(
                Entry("com.fang.framework.ui", "UI", "0.2.0"),
                Entry("com.fang.framework.audio", "Audio", "0.2.0"));

            var installed = new List<ExtensionPackageLocalInfo> { Local("com.fang.framework.audio", "0.1.0") };

            var rows = ExtensionPackageCatalog.Build(index, installed, ExtensionPackageFilter.UpdateAvailable, "audio");

            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual("com.fang.framework.audio", rows[0].Name);
        }

        [Test]
        public void Build_uses_local_info_when_index_entry_is_missing()
        {
            var row = ExtensionPackageCatalog.Build(null, Local("com.fang.framework", "0.5.1", true, "核心包描述"));

            Assert.AreEqual("com.fang.framework", row.Name);
            Assert.AreEqual("com.fang.framework", row.DisplayName);
            Assert.AreEqual("核心包描述", row.Description);
            Assert.AreEqual(string.Empty, row.IndexVersion);
            Assert.AreEqual("0.5.1", row.InstalledVersion);
            Assert.AreEqual(ExtensionPackageState.Embedded, row.State);
        }

        [Test]
        public void Build_marks_a_missing_core_package_as_not_installed()
        {
            Assert.AreEqual(ExtensionPackageState.NotInstalled, ExtensionPackageCatalog.Build(null, null).State);
        }

        [Test]
        public void ResolveDescription_prefers_the_local_manifest()
        {
            var entry = Entry("com.fang.framework.ui", "UI", "0.1.0", "索引描述");

            Assert.AreEqual("本地描述", ExtensionPackageCatalog.ResolveDescription(entry, Local("com.fang.framework.ui", "0.1.0", false, "本地描述")));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void ResolveDescription_falls_back_to_the_index(string localDescription)
        {
            var entry = Entry("com.fang.framework.ui", "UI", "0.1.0", "索引描述");

            Assert.AreEqual("索引描述", ExtensionPackageCatalog.ResolveDescription(entry, Local("com.fang.framework.ui", "0.1.0", false, localDescription)));
        }

        [Test]
        public void ResolveDescription_returns_empty_when_both_sides_are_missing()
        {
            Assert.AreEqual(string.Empty, ExtensionPackageCatalog.ResolveDescription(Entry("com.fang.framework.ui", "UI", "0.1.0"), null));
            Assert.AreEqual(string.Empty, ExtensionPackageCatalog.ResolveDescription(null, null));
        }

        [TestCase(PackageSource.Embedded, "内嵌")]
        [TestCase(PackageSource.Git, "Git")]
        [TestCase(PackageSource.Registry, "注册表")]
        [TestCase(PackageSource.BuiltIn, "内置")]
        [TestCase(PackageSource.Local, "本地")]
        [TestCase(PackageSource.Unknown, "未知")]
        public void DescribeSource_maps_known_sources(PackageSource source, string expected)
        {
            Assert.AreEqual(expected, ExtensionPackageCatalog.DescribeSource(source));
        }

        [Test]
        public void GetManifestPath_appends_the_manifest_file_name()
        {
            Assert.AreEqual(
                Path.Combine("Packages", "com.fang.framework", "package.json"),
                ExtensionPackageCatalog.GetManifestPath(Path.Combine("Packages", "com.fang.framework")));
        }

        [TestCase(null)]
        [TestCase("")]
        public void GetManifestPath_returns_empty_without_a_folder(string folder)
        {
            Assert.AreEqual(string.Empty, ExtensionPackageCatalog.GetManifestPath(folder));
        }

        [Test]
        public void TryReadManifestDescription_reads_the_local_manifest()
        {
            var folder = WriteManifest(@"{ ""name"": ""com.fang.framework.ui"", ""description"": ""面板栈。"" }");

            Assert.IsTrue(ExtensionPackageCatalog.TryReadManifestDescription(folder, out var description));
            Assert.AreEqual("面板栈。", description);
        }

        [Test]
        public void TryReadManifestDescription_returns_false_without_a_description_field()
        {
            var folder = WriteManifest(@"{ ""name"": ""com.fang.framework.ui"" }");

            Assert.IsFalse(ExtensionPackageCatalog.TryReadManifestDescription(folder, out var description));
            Assert.AreEqual(string.Empty, description);
        }

        [Test]
        public void TryReadManifestDescription_returns_false_when_the_manifest_is_missing()
        {
            _tempFolder = Path.Combine(Path.GetTempPath(), "FangFrameworkTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempFolder);

            Assert.IsFalse(ExtensionPackageCatalog.TryReadManifestDescription(_tempFolder, out var description));
            Assert.AreEqual(string.Empty, description);
        }

        [TestCase(null)]
        [TestCase("")]
        public void TryReadManifestDescription_returns_false_without_a_folder(string folder)
        {
            Assert.IsFalse(ExtensionPackageCatalog.TryReadManifestDescription(folder, out _));
        }

        private string WriteManifest(string json)
        {
            _tempFolder = Path.Combine(Path.GetTempPath(), "FangFrameworkTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempFolder);
            File.WriteAllText(Path.Combine(_tempFolder, "package.json"), json);
            return _tempFolder;
        }

        private static ExtensionPackageIndexDocument Index(params ExtensionPackageEntry[] packages)
        {
            return new ExtensionPackageIndexDocument
            {
                repository = "https://github.com/fangzw12138/FangFrameWork.git",
                core = Entry("com.fang.framework", "Fang Framework", "0.5.0"),
                packages = packages
            };
        }

        private static ExtensionPackageEntry Entry(string name, string displayName, string version, string description = null)
        {
            return new ExtensionPackageEntry
            {
                name = name,
                displayName = displayName,
                description = description,
                path = "/Packages/" + name,
                version = version,
                tag = name + "/v" + version,
                unity = "2022.3"
            };
        }

        private static ExtensionPackageLocalInfo Local(string name, string version, bool embedded = false, string description = null)
        {
            return new ExtensionPackageLocalInfo
            {
                Name = name,
                Version = version,
                Source = embedded ? PackageSource.Embedded : PackageSource.Git,
                IsEmbedded = embedded,
                Description = description,
                PackageJsonPath = "C:/Temp/" + name + "/package.json"
            };
        }
    }
}
