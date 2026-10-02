using System;
using System.IO;
using NUnit.Framework;

namespace Fang.Framework.Editor.Tests
{
    public class UnitaskDependencyTests
    {
        private const string ManifestWithoutRegistries =
            "{\n" +
            "  \"dependencies\": {\n" +
            "    \"com.unity.test-framework\": \"1.6.0\"\n" +
            "  }\n" +
            "}\n";

        private const string ManifestWithDependencyButNoRegistry =
            "{\n" +
            "  \"dependencies\": {\n" +
            "    \"com.cysharp.unitask\": \"2.5.11\",\n" +
            "    \"com.unity.test-framework\": \"1.6.0\"\n" +
            "  }\n" +
            "}\n";

        private const string ManifestWithForeignRegistry =
            "{\n" +
            "  \"scopedRegistries\": [\n" +
            "    {\n" +
            "      \"name\": \"my.registry\",\n" +
            "      \"url\": \"https://my.registry\",\n" +
            "      \"scopes\": [\n" +
            "        \"com.my.package\"\n" +
            "      ]\n" +
            "    }\n" +
            "  ],\n" +
            "  \"dependencies\": {\n" +
            "    \"com.unity.test-framework\": \"1.6.0\"\n" +
            "  }\n" +
            "}\n";

        [Test]
        public void TryBuildManifest_inserts_the_block_above_dependencies()
        {
            string updated;
            string error;

            Assert.IsTrue(UnitaskDependency.TryBuildManifest(ManifestWithoutRegistries, out updated, out error));
            Assert.IsNull(error);

            var registries = updated.IndexOf("\"scopedRegistries\"", StringComparison.Ordinal);
            var dependencies = updated.IndexOf("\"dependencies\"", StringComparison.Ordinal);
            Assert.GreaterOrEqual(registries, 0);
            Assert.Greater(dependencies, registries);

            Assert.IsTrue(updated.Contains("\"name\": \"package.openupm.com\""));
            Assert.IsTrue(updated.Contains("\"url\": \"https://package.openupm.com\""));
            Assert.IsTrue(updated.Contains("\"com.cysharp.unitask\""));
            Assert.IsTrue(updated.Contains("\"com.unity.test-framework\": \"1.6.0\""));
            Assert.IsTrue(UnitaskDependency.IsConfigured(updated));
        }

        [Test]
        public void TryBuildManifest_leaves_the_project_manifest_untouched()
        {
            var manifest = File.ReadAllText(UnitaskDependency.ManifestPath);
            string updated;
            string error;

            Assert.IsTrue(UnitaskDependency.TryBuildManifest(manifest, out updated, out error));
            Assert.IsNull(error);
            Assert.AreEqual(manifest, updated);
        }

        [Test]
        public void TryBuildManifest_is_idempotent()
        {
            string injected;
            string error;
            Assert.IsTrue(UnitaskDependency.TryBuildManifest(ManifestWithoutRegistries, out injected, out error));

            string second;
            Assert.IsTrue(UnitaskDependency.TryBuildManifest(injected, out second, out error));
            Assert.AreEqual(injected, second);
        }

        [Test]
        public void TryBuildManifest_still_injects_when_only_the_dependency_entry_exists()
        {
            string updated;
            string error;

            Assert.IsFalse(UnitaskDependency.IsConfigured(ManifestWithDependencyButNoRegistry));
            Assert.IsTrue(UnitaskDependency.TryBuildManifest(ManifestWithDependencyButNoRegistry, out updated, out error));
            Assert.AreNotEqual(ManifestWithDependencyButNoRegistry, updated);
            Assert.IsTrue(UnitaskDependency.IsConfigured(updated));
        }

        [Test]
        public void TryBuildManifest_rejects_scoped_registries_without_openupm()
        {
            string updated;
            string error;

            Assert.IsFalse(UnitaskDependency.TryBuildManifest(ManifestWithForeignRegistry, out updated, out error));
            Assert.IsNotNull(error);
            Assert.AreEqual(ManifestWithForeignRegistry, updated);
        }

        [Test]
        public void TryBuildManifest_rejects_a_manifest_without_dependencies()
        {
            string updated;
            string error;

            Assert.IsFalse(UnitaskDependency.TryBuildManifest("{\n}\n", out updated, out error));
            Assert.IsNotNull(error);
        }
    }
}
