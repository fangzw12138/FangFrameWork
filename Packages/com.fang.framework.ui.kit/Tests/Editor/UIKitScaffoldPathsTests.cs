using NUnit.Framework;

namespace Fang.Framework.UI.Kit.Editor.Tests
{
    public class UIKitScaffoldPathsTests
    {
        [Test]
        public void NormalizeFolderPath_keeps_assets_paths_and_trims_trailing_slashes()
        {
            Assert.AreEqual("Assets/UIKit", UIKitScaffoldPaths.NormalizeFolderPath("Assets/UIKit"));
            Assert.AreEqual("Assets/UIKit", UIKitScaffoldPaths.NormalizeFolderPath("Assets/UIKit/"));
            Assert.AreEqual("Assets/UIKit", UIKitScaffoldPaths.NormalizeFolderPath("  Assets\\UIKit  "));
            Assert.AreEqual("Assets", UIKitScaffoldPaths.NormalizeFolderPath("Assets"));
        }

        [Test]
        public void NormalizeFolderPath_rejects_paths_outside_assets()
        {
            Assert.IsNull(UIKitScaffoldPaths.NormalizeFolderPath(null));
            Assert.IsNull(UIKitScaffoldPaths.NormalizeFolderPath(string.Empty));
            Assert.IsNull(UIKitScaffoldPaths.NormalizeFolderPath("   "));
            Assert.IsNull(UIKitScaffoldPaths.NormalizeFolderPath("Packages/UIKit"));
            Assert.IsNull(UIKitScaffoldPaths.NormalizeFolderPath("AssetsX/UIKit"));
        }

        [Test]
        public void TryValidateProjectName_accepts_identifiers()
        {
            Assert.IsTrue(UIKitScaffoldPaths.TryValidateProjectName("UIKitProject", out var name, out var error));
            Assert.AreEqual("UIKitProject", name);
            Assert.IsNull(error);

            Assert.IsTrue(UIKitScaffoldPaths.TryValidateProjectName("  _kit2  ", out var trimmed, out _));
            Assert.AreEqual("_kit2", trimmed);
        }

        [Test]
        public void TryValidateProjectName_rejects_invalid_names()
        {
            Assert.IsFalse(UIKitScaffoldPaths.TryValidateProjectName(null, out _, out var emptyError));
            Assert.IsNotNull(emptyError);

            Assert.IsFalse(UIKitScaffoldPaths.TryValidateProjectName("2Kit", out _, out var digitError));
            Assert.IsNotNull(digitError);

            Assert.IsFalse(UIKitScaffoldPaths.TryValidateProjectName("UI Kit", out _, out var spaceError));
            Assert.IsNotNull(spaceError);
        }

        [Test]
        public void ProjectAssetPath_combines_the_folder_and_the_name()
        {
            Assert.AreEqual("Assets/UIKit/MyKit.asset", UIKitScaffoldPaths.ProjectAssetPath("Assets/UIKit/", "MyKit"));
        }

        [Test]
        public void ProjectAssetPath_is_empty_for_an_invalid_folder()
        {
            Assert.AreEqual(string.Empty, UIKitScaffoldPaths.ProjectAssetPath("Packages/UIKit", "MyKit"));
            Assert.AreEqual(string.Empty, UIKitScaffoldPaths.ProjectAssetPath("Assets/UIKit", string.Empty));
        }

        [Test]
        public void TryValidateAssetName_names_the_thing_it_validates()
        {
            Assert.IsFalse(UIKitScaffoldPaths.TryValidateAssetName("UI Kit", "预制体名", out _, out var spaceError));
            StringAssert.Contains("预制体名", spaceError);
            StringAssert.Contains("标识符", spaceError);

            Assert.IsTrue(UIKitScaffoldPaths.TryValidateAssetName("  Widget_1  ", "预制体名", out var name, out _));
            Assert.AreEqual("Widget_1", name);
        }

        [Test]
        public void PrefabAssetPath_appends_the_prefab_extension()
        {
            Assert.AreEqual("Assets/UIKit/Widget.prefab", UIKitScaffoldPaths.PrefabAssetPath("Assets/UIKit/", "Widget"));
            Assert.AreEqual(string.Empty, UIKitScaffoldPaths.PrefabAssetPath("Packages/UIKit", "Widget"));
            Assert.AreEqual(string.Empty, UIKitScaffoldPaths.PrefabAssetPath("Assets/UIKit", null));
        }

        [Test]
        public void TokenAssetPath_appends_the_asset_extension()
        {
            Assert.AreEqual("Assets/UIKit/TitleSize.asset", UIKitScaffoldPaths.TokenAssetPath("Assets/UIKit", "TitleSize"));
            Assert.AreEqual(string.Empty, UIKitScaffoldPaths.TokenAssetPath(null, "TitleSize"));
        }
    }
}
