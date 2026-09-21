using NUnit.Framework;

namespace Fang.Framework.Editor.Tests
{
    public class ExtensionPackageIndexTests
    {
        private const string ValidIndexJson = @"{
  ""repository"": ""https://github.com/fangzw12138/FangFrameWork"",
  ""core"": {
    ""name"": ""com.fang.framework"",
    ""displayName"": ""Fang Framework"",
    ""path"": ""/Packages/com.fang.framework"",
    ""version"": ""0.4.0"",
    ""tag"": ""com.fang.framework/v0.4.0""
  },
  ""packages"": [
    {
      ""name"": ""com.fang.framework.eventbus"",
      ""displayName"": ""Event Bus"",
      ""description"": ""类型安全发布订阅。"",
      ""path"": ""/Packages/com.fang.framework.eventbus"",
      ""version"": ""0.1.0"",
      ""tag"": ""com.fang.framework.eventbus/v0.1.0"",
      ""unity"": ""2022.3""
    }
  ]
}";

        [Test]
        public void Parse_maps_repository_core_and_packages()
        {
            var document = ExtensionPackageIndex.Parse(ValidIndexJson);

            Assert.IsNotNull(document);
            Assert.AreEqual("https://github.com/fangzw12138/FangFrameWork.git", document.repository);
            Assert.AreEqual("com.fang.framework", document.core.name);
            Assert.AreEqual("0.4.0", document.core.version);
            Assert.AreEqual("com.fang.framework/v0.4.0", document.core.tag);
            Assert.AreEqual(1, document.packages.Length);
            Assert.AreEqual("com.fang.framework.eventbus", document.packages[0].name);
            Assert.AreEqual("Event Bus", document.packages[0].displayName);
            Assert.AreEqual("0.1.0", document.packages[0].version);
        }

        [TestCase("{")]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("[]")]
        [TestCase("null")]
        [TestCase("not json")]
        public void Parse_rejects_invalid_json(string json)
        {
            Assert.IsNull(ExtensionPackageIndex.Parse(json));
        }

        [Test]
        public void Parse_rejects_index_without_repository()
        {
            const string json = @"{ ""repository"": """", ""core"": { ""name"": ""com.fang.framework"" } }";

            Assert.IsNull(ExtensionPackageIndex.Parse(json));
        }

        [Test]
        public void Parse_rejects_index_without_core_name()
        {
            const string json = @"{ ""repository"": ""https://github.com/fangzw12138/FangFrameWork"", ""core"": { ""path"": ""/Packages/com.fang.framework"" } }";

            Assert.IsNull(ExtensionPackageIndex.Parse(json));
        }

        [Test]
        public void Parse_rejects_index_without_core()
        {
            const string json = @"{ ""repository"": ""https://github.com/fangzw12138/FangFrameWork"" }";

            Assert.IsNull(ExtensionPackageIndex.Parse(json));
        }

        [Test]
        public void Parse_drops_entries_missing_required_fields()
        {
            const string json = @"{
  ""repository"": ""https://github.com/fangzw12138/FangFrameWork"",
  ""core"": { ""name"": ""com.fang.framework"" },
  ""packages"": [
    { ""name"": ""com.fang.framework.no-path"", ""tag"": ""v1"" },
    { ""name"": ""com.fang.framework.no-tag"", ""path"": ""/Packages/com.fang.framework.no-tag"" },
    { ""name"": """", ""path"": ""/Packages/empty"", ""tag"": ""v1"" },
    { ""name"": ""com.fang.framework.eventbus"", ""path"": ""/Packages/com.fang.framework.eventbus"", ""version"": ""0.1.0"", ""tag"": ""com.fang.framework.eventbus/v0.1.0"" }
  ]
}";

            var document = ExtensionPackageIndex.Parse(json);

            Assert.IsNotNull(document);
            Assert.AreEqual(1, document.packages.Length);
            Assert.AreEqual("com.fang.framework.eventbus", document.packages[0].name);
        }

        [Test]
        public void Parse_tolerates_missing_packages_array()
        {
            const string json = @"{ ""repository"": ""https://github.com/fangzw12138/FangFrameWork"", ""core"": { ""name"": ""com.fang.framework"" } }";

            var document = ExtensionPackageIndex.Parse(json);

            Assert.IsNotNull(document);
            Assert.IsNotNull(document.packages);
            Assert.AreEqual(0, document.packages.Length);
        }

        [TestCase("https://github.com/fangzw12138/FangFrameWork/", "https://github.com/fangzw12138/FangFrameWork.git")]
        [TestCase("https://github.com/fangzw12138/FangFrameWork.git", "https://github.com/fangzw12138/FangFrameWork.git")]
        [TestCase("ssh://git@github.com/fangzw12138/FangFrameWork", "ssh://git@github.com/fangzw12138/FangFrameWork.git")]
        [TestCase("git+file:///D:/GameProjects/FangFrameWork", "git+file:///D:/GameProjects/FangFrameWork")]
        [TestCase("file:///D:/GameProjects/FangFrameWork", "file:///D:/GameProjects/FangFrameWork")]
        [TestCase("", "")]
        [TestCase("   ", "")]
        public void NormalizeRepository_normalizes_the_repository_url(string input, string expected)
        {
            Assert.AreEqual(expected, ExtensionPackageIndex.NormalizeRepository(input));
        }

        [Test]
        public void BuildInstallUrl_composes_repository_path_and_tag()
        {
            var entry = new ExtensionPackageEntry
            {
                name = "com.fang.framework.eventbus",
                path = "/Packages/com.fang.framework.eventbus",
                tag = "com.fang.framework.eventbus/v0.1.0"
            };

            var url = ExtensionPackageIndex.BuildInstallUrl(
                "https://github.com/fangzw12138/FangFrameWork.git",
                entry);

            Assert.AreEqual(
                "https://github.com/fangzw12138/FangFrameWork.git?path=/Packages/com.fang.framework.eventbus#com.fang.framework.eventbus/v0.1.0",
                url);
        }

        [Test]
        public void BuildInstallUrl_prefixes_a_missing_leading_slash()
        {
            var entry = new ExtensionPackageEntry
            {
                name = "com.fang.framework.eventbus",
                path = "Packages/com.fang.framework.eventbus",
                tag = "v0.1.0"
            };

            var url = ExtensionPackageIndex.BuildInstallUrl("https://github.com/fangzw12138/FangFrameWork.git", entry);

            Assert.AreEqual(
                "https://github.com/fangzw12138/FangFrameWork.git?path=/Packages/com.fang.framework.eventbus#v0.1.0",
                url);
        }

        [Test]
        public void BuildInstallUrl_returns_empty_without_repository()
        {
            var entry = new ExtensionPackageEntry { name = "x", path = "/Packages/x", tag = "v1" };

            Assert.AreEqual(string.Empty, ExtensionPackageIndex.BuildInstallUrl(string.Empty, entry));
        }

        [Test]
        public void BuildInstallUrl_returns_empty_without_tag()
        {
            var entry = new ExtensionPackageEntry { name = "x", path = "/Packages/x" };

            Assert.AreEqual(string.Empty, ExtensionPackageIndex.BuildInstallUrl("https://github.com/x/y.git", entry));
        }

        [Test]
        public void BuildInstallUrl_returns_empty_without_path()
        {
            var entry = new ExtensionPackageEntry { name = "x", tag = "v1" };

            Assert.AreEqual(string.Empty, ExtensionPackageIndex.BuildInstallUrl("https://github.com/x/y.git", entry));
        }

        [TestCase("0.2.0", "0.1.0", true)]
        [TestCase("0.1.10", "0.1.9", true)]
        [TestCase("0.1.0", "0.1.0", false)]
        [TestCase("0.1.0", "0.2.0", false)]
        [TestCase("1.0.0", "0.9.9", true)]
        [TestCase("0.1.0", "0.1.0-preview.1", true)]
        [TestCase("0.1.0-preview.1", "0.1.0", false)]
        [TestCase("1.0", "1.0.0", false)]
        [TestCase("0.1.0", "", true)]
        [TestCase("", "0.1.0", false)]
        public void IsNewer_compares_semantic_versions(string candidate, string installed, bool expected)
        {
            Assert.AreEqual(expected, ExtensionPackageIndex.IsNewer(candidate, installed));
        }

        [Test]
        public void ResolveState_returns_NotInstalled_when_absent()
        {
            Assert.AreEqual(
                ExtensionPackageState.NotInstalled,
                ExtensionPackageIndex.ResolveState("0.1.0", null, false));
        }

        [Test]
        public void ResolveState_returns_Installed_when_versions_match()
        {
            Assert.AreEqual(
                ExtensionPackageState.Installed,
                ExtensionPackageIndex.ResolveState("0.1.0", "0.1.0", false));
        }

        [Test]
        public void ResolveState_returns_Installed_when_only_formatting_differs()
        {
            Assert.AreEqual(
                ExtensionPackageState.Installed,
                ExtensionPackageIndex.ResolveState("1.0", "1.0.0", false));
        }

        [Test]
        public void ResolveState_returns_UpdateAvailable_when_index_is_newer()
        {
            Assert.AreEqual(
                ExtensionPackageState.UpdateAvailable,
                ExtensionPackageIndex.ResolveState("0.2.0", "0.1.0", false));
        }

        [Test]
        public void ResolveState_returns_LocalAhead_when_installed_is_newer()
        {
            Assert.AreEqual(
                ExtensionPackageState.LocalAhead,
                ExtensionPackageIndex.ResolveState("0.1.0", "0.2.0", false));
        }

        [Test]
        public void ResolveState_returns_Embedded_for_embedded_packages()
        {
            Assert.AreEqual(
                ExtensionPackageState.Embedded,
                ExtensionPackageIndex.ResolveState("0.1.0", "0.1.0", true));
        }

        [Test]
        public void ResolveState_returns_Embedded_even_when_not_installed()
        {
            Assert.AreEqual(
                ExtensionPackageState.Embedded,
                ExtensionPackageIndex.ResolveState("0.1.0", null, true));
        }
    }
}
