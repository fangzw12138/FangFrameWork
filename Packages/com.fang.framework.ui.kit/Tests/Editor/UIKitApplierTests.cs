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
            var bare = new TestPrefab("Bare", false).Save("Bare");
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

        private static TextMeshProUGUI LoadLabel(string prefabName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TestRoot + "/" + prefabName + ".prefab");
            Assert.IsNotNull(prefab, "找不到预制体：" + prefabName);
            return prefab.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        }

        private static void DeleteTestRoot()
        {
            if (AssetDatabase.IsValidFolder(TestRoot))
            {
                AssetDatabase.DeleteAsset(TestRoot);
            }
        }

        /// <summary>搭一个带 Label(TMP_Text) / Icon(Image) 的测试预制体，条目指向它自己内部的组件。</summary>
        private sealed class TestPrefab
        {
            private readonly List<(string Id, Component Target)> entries = new List<(string, Component)>();

            public TestPrefab(string name, bool withMatch = true)
            {
                Root = new GameObject(name, typeof(RectTransform));

                var label = new GameObject("Label", typeof(RectTransform));
                label.transform.SetParent(Root.transform, false);
                Text = label.AddComponent<TextMeshProUGUI>();
                Text.fontSize = 36f;

                var icon = new GameObject("Icon", typeof(RectTransform));
                icon.transform.SetParent(Root.transform, false);
                Icon = icon.AddComponent<Image>();

                if (withMatch)
                {
                    label.AddComponent<TokenMatch>();
                }
            }

            public GameObject Root { get; }

            public TextMeshProUGUI Text { get; }

            public Image Icon { get; }

            public TestPrefab Entry(string id, Component target)
            {
                entries.Add((id, target));
                return this;
            }

            public GameObject Save(string name)
            {
                var match = Root.GetComponentInChildren<TokenMatch>(true);
                if (match != null)
                {
                    var serialized = new SerializedObject(match);
                    var list = serialized.FindProperty("_entries");
                    list.arraySize = entries.Count;

                    for (var i = 0; i < entries.Count; i++)
                    {
                        var element = list.GetArrayElementAtIndex(i);
                        element.FindPropertyRelative("_id").stringValue = entries[i].Id;
                        element.FindPropertyRelative("_target").objectReferenceValue = entries[i].Target;
                    }

                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                if (!AssetDatabase.IsValidFolder(TestRoot))
                {
                    AssetDatabase.CreateFolder("Assets", "UiKitApplyTests");
                }

                var prefab = PrefabUtility.SaveAsPrefabAsset(Root, TestRoot + "/" + name + ".prefab");
                Object.DestroyImmediate(Root);
                return prefab;
            }
        }
    }
}
