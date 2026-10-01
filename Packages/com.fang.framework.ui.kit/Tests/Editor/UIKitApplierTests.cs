using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Fang.Framework.UI.Kit.Editor.Tests
{
    public class UIKitApplierTests
    {
        private const string TestRoot = "Assets/UiKitApplyTests";

        [SetUp]
        public void SetUp()
        {
            DeleteTestRoot();
        }

        [TearDown]
        public void TearDown()
        {
            KitTestSetup.Cleanup();
            DeleteTestRoot();
        }

        [Test]
        public void Build_plans_every_entry_that_has_a_token()
        {
            var builder = new TestPrefab("Button");
            var prefab = builder.Entry("Text/Title", builder.Text).Save("Button");
            var project = KitTestSetup.NewProject(KitTestSetup.NewToken<FontSizeTokenSo>("Text/Title"));
            KitTestSetup.AddPrefab(project, prefab);

            var plan = UIKitApplier.Build(project, null);

            Assert.AreEqual(1, plan.Rows.Count);
            Assert.IsTrue(plan.HasWork);
            Assert.AreEqual("全体", plan.Scope);
            Assert.AreEqual(0, plan.Skips.Count);

            var row = plan.Rows[0];
            Assert.AreEqual(TestRoot + "/Button.prefab", row.PrefabPath);
            Assert.AreEqual("Label", row.NodePath);
            Assert.AreEqual("Text/Title", row.MatchId);
            Assert.AreEqual("TextMeshProUGUI", row.TargetTypeName);
        }

        [Test]
        public void Build_plans_one_row_per_token_on_the_same_id()
        {
            var builder = new TestPrefab("Button");
            var prefab = builder.Entry("Text/Title", builder.Text).Save("Button");
            var project = KitTestSetup.NewProject(
                KitTestSetup.NewToken<FontSizeTokenSo>("Text/Title"),
                KitTestSetup.NewToken<ColorTokenSo>("Text/Title"));
            KitTestSetup.AddPrefab(project, prefab);

            Assert.AreEqual(2, UIKitApplier.Build(project, null).Rows.Count);
        }

        [Test]
        public void Build_filters_by_the_requested_id()
        {
            var builder = new TestPrefab("Button");
            var prefab = builder
                .Entry("Text/Title", builder.Text)
                .Entry("Color/Primary", builder.Text)
                .Save("Button");
            var project = KitTestSetup.NewProject(
                KitTestSetup.NewToken<FontSizeTokenSo>("Text/Title"),
                KitTestSetup.NewToken<ColorTokenSo>("Color/Primary"));
            KitTestSetup.AddPrefab(project, prefab);

            var plan = UIKitApplier.Build(project, "Color/Primary");

            Assert.AreEqual(1, plan.Rows.Count);
            Assert.AreEqual("Color/Primary", plan.Rows[0].MatchId);
            Assert.AreEqual("Color/Primary", plan.Scope);
        }

        [Test]
        public void Build_reports_why_a_row_was_skipped()
        {
            var builder = new TestPrefab("Button");
            var prefab = builder
                .Entry("Text/Title", null)
                .Entry(string.Empty, builder.Text)
                .Entry("Text/Title", builder.Icon)
                .Entry("Color/Danger", builder.Text)
                .Save("Button");
            var project = KitTestSetup.NewProject(KitTestSetup.NewToken<FontSizeTokenSo>("Text/Title"));
            KitTestSetup.AddPrefab(project, prefab);

            var plan = UIKitApplier.Build(project, null);

            Assert.AreEqual(0, plan.Rows.Count);
            Assert.AreEqual(4, plan.Skips.Count);
            StringAssert.Contains("没有指定 target", plan.Skips[0].Reason);
            StringAssert.Contains("没有填匹配 id", plan.Skips[1].Reason);
            StringAssert.Contains("不接受", plan.Skips[2].Reason);
            StringAssert.Contains("没有 token", plan.Skips[3].Reason);
        }

        [Test]
        public void Build_reports_a_prefab_without_a_match_and_an_empty_reference()
        {
            var bare = new TestPrefab("Bare").Save("Bare");
            var project = KitTestSetup.NewProject();
            KitTestSetup.AddPrefab(project, bare);
            KitTestSetup.AddPrefab(project, null);

            var plan = UIKitApplier.Build(project, null);

            Assert.AreEqual(0, plan.Rows.Count);
            Assert.AreEqual(2, plan.Skips.Count);
            StringAssert.Contains("没有 TokenMatch", plan.Skips[0].Reason);
            StringAssert.Contains("空引用", plan.Skips[1].Reason);
        }

        [Test]
        public void Build_reports_a_missing_project()
        {
            var plan = UIKitApplier.Build(null, null);

            Assert.AreEqual(0, plan.Rows.Count);
            Assert.AreEqual(1, plan.Skips.Count);
        }

        [Test]
        public void Apply_writes_the_value_into_the_prefab_asset()
        {
            var builder = new TestPrefab("Button");
            var prefab = builder.Entry("Text/Title", builder.Text).Save("Button");
            var token = KitTestSetup.NewToken<FontSizeTokenSo>("Text/Title");
            KitTestSetup.SetFloat(token, "_fontSize", 77f);
            var project = KitTestSetup.NewProject(token);
            KitTestSetup.AddPrefab(project, prefab);

            var errors = new List<string>();
            var applied = UIKitApplier.Apply(UIKitApplier.Build(project, null), errors);

            Assert.AreEqual(1, applied);
            Assert.AreEqual(0, errors.Count);
            Assert.AreEqual(77f, LoadLabel("Button").fontSize);
        }

        [Test]
        public void Apply_writes_only_the_requested_id()
        {
            var builder = new TestPrefab("Button");
            var prefab = builder
                .Entry("Text/Title", builder.Text)
                .Entry("Color/Primary", builder.Text)
                .Save("Button");

            var size = KitTestSetup.NewToken<FontSizeTokenSo>("Text/Title");
            KitTestSetup.SetFloat(size, "_fontSize", 77f);
            var color = KitTestSetup.NewToken<ColorTokenSo>("Color/Primary");
            KitTestSetup.SetColor(color, "_color", Color.green);
            var project = KitTestSetup.NewProject(size, color);
            KitTestSetup.AddPrefab(project, prefab);

            UIKitApplier.Apply(UIKitApplier.Build(project, "Text/Title"), new List<string>());

            var written = LoadLabel("Button");
            Assert.AreEqual(77f, written.fontSize);
            Assert.AreEqual(Color.white, written.color);
        }

        [Test]
        public void Apply_keeps_a_prefab_variant_a_variant_and_leaves_the_base_alone()
        {
            var builder = new TestPrefab("Base");
            var basePrefab = builder.Entry("Text/Title", builder.Text).Save("Base");
            var variantPath = TestRoot + "/Variant.prefab";

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            var variant = PrefabUtility.SaveAsPrefabAsset(instance, variantPath);
            Object.DestroyImmediate(instance);

            var token = KitTestSetup.NewToken<FontSizeTokenSo>("Text/Title");
            KitTestSetup.SetFloat(token, "_fontSize", 77f);
            var project = KitTestSetup.NewProject(token);
            KitTestSetup.AddPrefab(project, variant);

            Assert.AreEqual(1, UIKitApplier.Apply(UIKitApplier.Build(project, null), new List<string>()));

            var writtenVariant = AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
            Assert.AreEqual(PrefabAssetType.Variant, PrefabUtility.GetPrefabAssetType(writtenVariant));
            Assert.AreSame(
                AssetDatabase.LoadAssetAtPath<GameObject>(TestRoot + "/Base.prefab"),
                PrefabUtility.GetCorrespondingObjectFromSource(writtenVariant));
            Assert.AreEqual(77f, writtenVariant.transform.Find("Label").GetComponent<TextMeshProUGUI>().fontSize);

            Assert.AreEqual(36f, LoadLabel("Base").fontSize);
        }

        [Test]
        public void Apply_reports_a_missing_node_instead_of_writing()
        {
            var builder = new TestPrefab("Button");
            var prefab = builder.Entry("Text/Title", builder.Text).Save("Button");
            var token = KitTestSetup.NewToken<FontSizeTokenSo>("Text/Title");
            KitTestSetup.SetFloat(token, "_fontSize", 77f);
            var project = KitTestSetup.NewProject(token);
            KitTestSetup.AddPrefab(project, prefab);

            var built = UIKitApplier.Build(project, null);
            Assert.AreEqual(1, built.Rows.Count);

            var broken = new UIKitApplyPlan { Scope = built.Scope };
            broken.Rows.Add(new UIKitApplyRow(
                built.Rows[0].PrefabPath,
                "Missing/Node",
                0,
                0,
                built.Rows[0].MatchId,
                built.Rows[0].TargetTypeName,
                built.Rows[0].Token));

            var errors = new List<string>();
            var applied = UIKitApplier.Apply(broken, errors);

            Assert.AreEqual(0, applied);
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("找不到节点", errors[0]);
        }

        [Test]
        public void Apply_writes_rows_for_matches_on_different_nodes()
        {
            var builder = new TestPrefab("MultiNode");
            var prefab = builder
                .Entry("Text/Test", builder.Text)
                .Match(builder.Root, "Color/Test", builder.RootImage)
                .Save("MultiNode");

            var size = KitTestSetup.NewToken<FontSizeTokenSo>("Text/Test");
            KitTestSetup.SetFloat(size, "_fontSize", 77f);
            var color = KitTestSetup.NewToken<ColorTokenSo>("Color/Test");
            KitTestSetup.SetColor(color, "_color", Color.green);
            var project = KitTestSetup.NewProject(size, color);
            KitTestSetup.AddPrefab(project, prefab);

            var plan = UIKitApplier.Build(project, null);

            Assert.AreEqual(2, plan.Rows.Count);
            Assert.AreEqual(0, plan.Skips.Count);

            var labelRow = plan.Rows.Find(row => row.MatchId == "Text/Test");
            Assert.IsNotNull(labelRow);
            Assert.AreEqual(0, labelRow.MatchIndex);

            var errors = new List<string>();
            var applied = UIKitApplier.Apply(plan, errors);

            Assert.AreEqual(2, applied);
            Assert.AreEqual(0, errors.Count);

            var written = LoadPrefab("MultiNode");
            Assert.AreEqual(77f, written.transform.Find("Label").GetComponent<TextMeshProUGUI>().fontSize);
            Assert.AreEqual(Color.green, written.GetComponent<Image>().color);
        }

        [Test]
        public void Apply_writes_rows_for_two_matches_on_the_same_node()
        {
            var builder = new TestPrefab("TwinNode");
            var prefab = builder
                .Entry("Text/Test", builder.Text)
                .Match(builder.Root, "Color/Root", builder.RootImage)
                .ExtraMatch(builder.LabelObject, "Color/Label", builder.Text)
                .Save("TwinNode");

            var size = KitTestSetup.NewToken<FontSizeTokenSo>("Text/Test");
            KitTestSetup.SetFloat(size, "_fontSize", 77f);
            var labelColor = KitTestSetup.NewToken<ColorTokenSo>("Color/Label");
            KitTestSetup.SetColor(labelColor, "_color", Color.green);
            var rootColor = KitTestSetup.NewToken<ColorTokenSo>("Color/Root");
            KitTestSetup.SetColor(rootColor, "_color", Color.red);
            var project = KitTestSetup.NewProject(size, labelColor, rootColor);
            KitTestSetup.AddPrefab(project, prefab);

            var plan = UIKitApplier.Build(project, null);

            Assert.AreEqual(3, plan.Rows.Count);
            Assert.AreEqual(0, plan.Skips.Count);

            var firstRow = plan.Rows.Find(row => row.NodePath == "Label" && row.MatchId == "Text/Test");
            var secondRow = plan.Rows.Find(row => row.NodePath == "Label" && row.MatchId == "Color/Label");

            Assert.IsNotNull(firstRow);
            Assert.IsNotNull(secondRow);
            Assert.AreEqual(0, firstRow.MatchIndex);
            Assert.AreEqual(1, secondRow.MatchIndex);

            var errors = new List<string>();
            var applied = UIKitApplier.Apply(plan, errors);

            Assert.AreEqual(3, applied);
            Assert.AreEqual(0, errors.Count);

            var written = LoadPrefab("TwinNode");
            var label = written.transform.Find("Label").GetComponent<TextMeshProUGUI>();
            Assert.AreEqual(77f, label.fontSize);
            Assert.AreEqual(Color.green, label.color);
            Assert.AreEqual(Color.red, written.GetComponent<Image>().color);
        }

        [Test]
        public void Apply_targets_the_right_node_when_siblings_share_a_name()
        {
            var builder = new TestPrefab("TwinName");
            builder.LabelObject.name = "Item";
            builder.IconObject.name = "Item";

            var prefab = builder
                .Entry("Text/Test", builder.Text)
                .Match(builder.IconObject, "Color/Test", builder.Icon)
                .Save("TwinName");

            var size = KitTestSetup.NewToken<FontSizeTokenSo>("Text/Test");
            KitTestSetup.SetFloat(size, "_fontSize", 77f);
            var color = KitTestSetup.NewToken<ColorTokenSo>("Color/Test");
            KitTestSetup.SetColor(color, "_color", Color.green);
            var project = KitTestSetup.NewProject(size, color);
            KitTestSetup.AddPrefab(project, prefab);

            var plan = UIKitApplier.Build(project, null);

            Assert.AreEqual(2, plan.Rows.Count);
            Assert.AreEqual(0, plan.Skips.Count);

            var errors = new List<string>();
            Assert.AreEqual(2, UIKitApplier.Apply(plan, errors));
            Assert.AreEqual(0, errors.Count);

            var written = LoadPrefab("TwinName");
            var text = written.transform.GetChild(0);
            var image = written.transform.GetChild(1);

            Assert.AreEqual("Item", text.name);
            Assert.AreEqual("Item", image.name);
            Assert.AreEqual(77f, text.GetComponent<TextMeshProUGUI>().fontSize);
            Assert.AreEqual(Color.white, text.GetComponent<TextMeshProUGUI>().color);
            Assert.AreEqual(Color.green, image.GetComponent<Image>().color);
        }

        private static GameObject LoadPrefab(string prefabName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TestRoot + "/" + prefabName + ".prefab");
            Assert.IsNotNull(prefab, "找不到预制体：" + prefabName);
            return prefab;
        }

        private static TextMeshProUGUI LoadLabel(string prefabName)
        {
            return LoadPrefab(prefabName).transform.Find("Label").GetComponent<TextMeshProUGUI>();
        }

        private static void DeleteTestRoot()
        {
            if (AssetDatabase.IsValidFolder(TestRoot))
            {
                AssetDatabase.DeleteAsset(TestRoot);
            }
        }

        /// <summary>搭一个带 Label(TMP_Text) / Icon(Image) / 根 Image 的测试预制体；条目按宿主节点分组写进该节点上的 TokenMatch。</summary>
        private sealed class TestPrefab
        {
            private readonly List<(GameObject Host, string Id, Component Target)> entries =
                new List<(GameObject, string, Component)>();

            private readonly List<(GameObject Host, string Id, Component Target)> extraEntries =
                new List<(GameObject, string, Component)>();

            public TestPrefab(string name)
            {
                Root = new GameObject(name, typeof(RectTransform));
                RootImage = Root.AddComponent<Image>();

                var label = new GameObject("Label", typeof(RectTransform));
                label.transform.SetParent(Root.transform, false);
                Text = label.AddComponent<TextMeshProUGUI>();
                Text.fontSize = 36f;

                var icon = new GameObject("Icon", typeof(RectTransform));
                icon.transform.SetParent(Root.transform, false);
                Icon = icon.AddComponent<Image>();
            }

            public GameObject Root { get; }

            public Image RootImage { get; }

            public TextMeshProUGUI Text { get; }

            public Image Icon { get; }

            public GameObject LabelObject => Text.gameObject;

            public GameObject IconObject => Icon.gameObject;

            /// <summary>条目挂在 Label 节点的那个 TokenMatch 上。</summary>
            public TestPrefab Entry(string id, Component target)
            {
                return Match(LabelObject, id, target);
            }

            /// <summary>条目挂在指定节点上（该节点共用一个 TokenMatch）。</summary>
            public TestPrefab Match(GameObject host, string id, Component target)
            {
                entries.Add((host, id, target));
                return this;
            }

            /// <summary>给指定节点再加一个 TokenMatch，用来测同节点多个匹配组件。</summary>
            public TestPrefab ExtraMatch(GameObject host, string id, Component target)
            {
                extraEntries.Add((host, id, target));
                return this;
            }

            public GameObject Save(string name)
            {
                var hosts = new List<GameObject>();

                for (var i = 0; i < entries.Count; i++)
                {
                    if (!hosts.Contains(entries[i].Host))
                    {
                        hosts.Add(entries[i].Host);
                    }
                }

                for (var h = 0; h < hosts.Count; h++)
                {
                    var host = hosts[h];
                    var rows = new List<(string Id, Component Target)>();

                    for (var i = 0; i < entries.Count; i++)
                    {
                        if (entries[i].Host == host)
                        {
                            rows.Add((entries[i].Id, entries[i].Target));
                        }
                    }

                    WriteEntries(host.GetComponent<TokenMatch>() ?? host.AddComponent<TokenMatch>(), rows);
                }

                for (var i = 0; i < extraEntries.Count; i++)
                {
                    var extra = extraEntries[i];
                    WriteEntries(
                        extra.Host.AddComponent<TokenMatch>(),
                        new List<(string Id, Component Target)> { (extra.Id, extra.Target) });
                }

                if (!AssetDatabase.IsValidFolder(TestRoot))
                {
                    AssetDatabase.CreateFolder("Assets", "UiKitApplyTests");
                }

                var prefab = PrefabUtility.SaveAsPrefabAsset(Root, TestRoot + "/" + name + ".prefab");
                Object.DestroyImmediate(Root);
                return prefab;
            }

            private static void WriteEntries(TokenMatch match, List<(string Id, Component Target)> rows)
            {
                var serialized = new SerializedObject(match);
                var list = serialized.FindProperty("_entries");
                list.arraySize = rows.Count;

                for (var i = 0; i < rows.Count; i++)
                {
                    var element = list.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("_id").stringValue = rows[i].Id;
                    element.FindPropertyRelative("_target").objectReferenceValue = rows[i].Target;
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
