using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Fang.Framework.UI.Kit.Editor.Tests
{
    public class UIKitProjectScaffolderTests
    {
        private const string TestRoot = "Assets/UiKitScaffoldTests";
        private const string ProjectPath = TestRoot + "/UiKitScaffoldProject.asset";

        [SetUp]
        public void SetUp()
        {
            DeleteTestRoot();
        }

        [TearDown]
        public void TearDown()
        {
            DeleteTestRoot();
        }

        [Test]
        public void CreateProject_creates_the_asset_and_writes_the_identity()
        {
            var result = UIKitProjectScaffolder.CreateProject(new UIKitProjectCreateOptions
            {
                ProjectFolder = TestRoot,
                ProjectName = "UiKitScaffoldProject",
                DisplayName = "控件库",
                Description = "测试用",
            });

            Assert.IsTrue(result.Success, result.ToStatusMessage());
            CollectionAssert.Contains(result.CreatedPaths, ProjectPath);

            var project = AssetDatabase.LoadAssetAtPath<UIKitProjectSo>(ProjectPath);
            Assert.IsNotNull(project);
            Assert.AreEqual("UiKitScaffoldProject", project.Id);
            Assert.AreEqual("控件库", project.DisplayName);
            Assert.AreEqual("测试用", project.Description);
            Assert.AreEqual(0, project.Tokens.Count);
        }

        [Test]
        public void CreateProject_falls_back_to_the_name_for_an_empty_display_name()
        {
            var result = UIKitProjectScaffolder.CreateProject(new UIKitProjectCreateOptions
            {
                ProjectFolder = TestRoot,
                ProjectName = "UiKitScaffoldProject",
                DisplayName = "   ",
            });

            Assert.IsTrue(result.Success, result.ToStatusMessage());
            Assert.AreEqual("UiKitScaffoldProject", AssetDatabase.LoadAssetAtPath<UIKitProjectSo>(ProjectPath).DisplayName);
        }

        [Test]
        public void CreateProject_creates_the_folder_when_it_is_missing()
        {
            Assert.IsFalse(AssetDatabase.IsValidFolder(TestRoot));

            var result = UIKitProjectScaffolder.CreateProject(new UIKitProjectCreateOptions
            {
                ProjectFolder = TestRoot,
                ProjectName = "UiKitScaffoldProject",
            });

            Assert.IsTrue(result.Success, result.ToStatusMessage());
            Assert.IsTrue(AssetDatabase.IsValidFolder(TestRoot));
        }

        [Test]
        public void CreateProject_rejects_an_existing_asset()
        {
            UIKitProjectScaffolder.CreateProject(new UIKitProjectCreateOptions
            {
                ProjectFolder = TestRoot,
                ProjectName = "UiKitScaffoldProject",
            });

            var second = UIKitProjectScaffolder.CreateProject(new UIKitProjectCreateOptions
            {
                ProjectFolder = TestRoot,
                ProjectName = "UiKitScaffoldProject",
            });

            Assert.IsFalse(second.Success);
            StringAssert.Contains("已存在", second.ToStatusMessage());
        }

        [Test]
        public void CreateProject_rejects_invalid_input()
        {
            Assert.IsFalse(UIKitProjectScaffolder.CreateProject(null).Success);

            var badFolder = UIKitProjectScaffolder.CreateProject(new UIKitProjectCreateOptions
            {
                ProjectFolder = "Packages/UIKit",
                ProjectName = "UiKitScaffoldProject",
            });
            Assert.IsFalse(badFolder.Success);
            StringAssert.Contains("Assets", badFolder.ToStatusMessage());

            var badName = UIKitProjectScaffolder.CreateProject(new UIKitProjectCreateOptions
            {
                ProjectFolder = TestRoot,
                ProjectName = "UI Kit",
            });
            Assert.IsFalse(badName.Success);
            StringAssert.Contains("标识符", badName.ToStatusMessage());
        }

        [Test]
        public void CreatePrefabShell_makes_a_ui_shell_and_registers_it()
        {
            var project = KitTestSetup.NewProject();

            var result = UIKitProjectScaffolder.CreatePrefabShell(project, TestRoot, "Widget");

            Assert.IsTrue(result.Success, result.ToStatusMessage());

            var path = TestRoot + "/Widget.prefab";
            CollectionAssert.Contains(result.CreatedPaths, path);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab);
            Assert.IsNotNull(prefab.GetComponent<RectTransform>());
            Assert.IsNotNull(prefab.GetComponent<TokenMatch>());

            Assert.AreEqual(1, project.Prefabs.Count);
            Assert.AreSame(prefab, project.Prefabs[0]);
        }

        [Test]
        public void CreatePrefabShell_rejects_invalid_input_and_existing_assets()
        {
            var project = KitTestSetup.NewProject();

            Assert.IsFalse(UIKitProjectScaffolder.CreatePrefabShell(null, TestRoot, "Widget").Success);
            Assert.IsFalse(UIKitProjectScaffolder.CreatePrefabShell(project, "Packages/UIKit", "Widget").Success);

            var badName = UIKitProjectScaffolder.CreatePrefabShell(project, TestRoot, "UI Widget");
            Assert.IsFalse(badName.Success);
            StringAssert.Contains("标识符", badName.ToStatusMessage());

            Assert.IsTrue(UIKitProjectScaffolder.CreatePrefabShell(project, TestRoot, "Widget").Success);

            var second = UIKitProjectScaffolder.CreatePrefabShell(project, TestRoot, "Widget");
            Assert.IsFalse(second.Success);
            StringAssert.Contains("已存在", second.ToStatusMessage());
            Assert.AreEqual(1, project.Prefabs.Count);
        }

        [Test]
        public void CreateToken_writes_the_asset_name_as_id_and_the_hand_written_match_id()
        {
            var project = KitTestSetup.NewProject();
            var draft = KitTestSetup.NewToken<FontSizeTokenSo>();
            KitTestSetup.SetFloat(draft, "_fontSize", 42f);

            var result = UIKitProjectScaffolder.CreateToken(project, draft, TestRoot, "TitleSize", "  My/OwnId  ");

            Assert.IsTrue(result.Success, result.ToStatusMessage());

            var path = TestRoot + "/TitleSize.asset";
            CollectionAssert.Contains(result.CreatedPaths, path);

            var token = AssetDatabase.LoadAssetAtPath<TokenSo>(path);
            Assert.IsNotNull(token);
            Assert.IsInstanceOf<FontSizeTokenSo>(token);
            Assert.AreEqual("TitleSize", token.Id);
            Assert.AreEqual("My/OwnId", token.MatchId);
            Assert.AreEqual(42f, ((FontSizeTokenSo)token).FontSize);

            Assert.AreEqual(1, project.Tokens.Count);
            Assert.AreSame(token, project.Tokens[0]);
            Assert.AreSame(token, project.GetTokens("My/OwnId")[0]);
        }

        [Test]
        public void CreateToken_accepts_an_empty_match_id_which_means_not_enabled()
        {
            var project = KitTestSetup.NewProject();

            var result = UIKitProjectScaffolder.CreateToken(project, KitTestSetup.NewToken<ColorTokenSo>(), TestRoot, "Primary", null);

            Assert.IsTrue(result.Success, result.ToStatusMessage());
            Assert.IsTrue(string.IsNullOrEmpty(AssetDatabase.LoadAssetAtPath<TokenSo>(TestRoot + "/Primary.asset").MatchId));
            Assert.AreEqual(0, project.GetTokens("Color/Primary").Count);
        }

        [Test]
        public void CreateToken_rejects_invalid_input_and_existing_assets()
        {
            var project = KitTestSetup.NewProject();

            Assert.IsFalse(UIKitProjectScaffolder.CreateToken(null, KitTestSetup.NewToken<ColorTokenSo>(), TestRoot, "Primary", null).Success);
            Assert.IsFalse(UIKitProjectScaffolder.CreateToken(project, null, TestRoot, "Primary", null).Success);
            Assert.IsFalse(UIKitProjectScaffolder.CreateToken(project, KitTestSetup.NewToken<ColorTokenSo>(), "Packages/UIKit", "Primary", null).Success);

            Assert.IsTrue(UIKitProjectScaffolder.CreateToken(project, KitTestSetup.NewToken<ColorTokenSo>(), TestRoot, "Primary", null).Success);

            var second = UIKitProjectScaffolder.CreateToken(project, KitTestSetup.NewToken<ColorTokenSo>(), TestRoot, "Primary", null);
            Assert.IsFalse(second.Success);
            StringAssert.Contains("已存在", second.ToStatusMessage());
            Assert.AreEqual(1, project.Tokens.Count);
        }

        private static void DeleteTestRoot()
        {
            if (AssetDatabase.IsValidFolder(TestRoot))
            {
                AssetDatabase.DeleteAsset(TestRoot);
            }
        }
    }
}
