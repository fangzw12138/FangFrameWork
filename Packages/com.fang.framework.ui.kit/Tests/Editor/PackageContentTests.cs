using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Fang.Framework.UI.Kit.Editor.Tests
{
    public class PackageContentTests
    {
        private const string PackageRoot = "Packages/com.fang.framework.ui.kit";

        private static readonly string[] PublishedIds =
        {
            "Text/Title", "Text/Subtitle", "Text/Body", "Text/Caption", "Text/Button",
            "Color/Primary", "Color/Secondary", "Color/Background", "Color/Surface", "Color/Text",
            "Color/Success", "Color/Warning", "Color/Danger", "Color/Disabled",
            "Icon/Primary", "Icon/Secondary",
            "Button/Primary", "Button/Secondary", "Button/Ghost",
        };

        private static readonly string[] NestedDemoTokens = { "ButtonLabelText.asset", "ButtonIconToken.asset" };

        [Test]
        public void Every_package_prefab_entry_uses_a_published_id_and_has_a_target()
        {
            var entries = 0;

            foreach (var prefab in PackagePrefabs())
            {
                foreach (var match in prefab.GetComponentsInChildren<TokenMatch>(true))
                {
                    foreach (var entry in match.Entries)
                    {
                        Assert.IsNotNull(entry, prefab.name);
                        Assert.IsFalse(string.IsNullOrEmpty(entry.Id), prefab.name + " 有一条没填匹配 id");
                        Assert.IsNotNull(entry.Target, prefab.name + " 的「" + entry.Id + "」没有 target");
                        CollectionAssert.Contains(PublishedIds, entry.Id, prefab.name + " 用了规范外的匹配 id：" + entry.Id);
                        entries++;
                    }
                }
            }

            Assert.Greater(entries, 0, "包内预制体上没有 TokenMatch 条目。");
        }

        [Test]
        public void Package_button_prefabs_wire_exactly_the_slots_their_shape_has()
        {
            var iconText = KitButtonOf("ButtonIconText");
            Assert.IsNotNull(iconText.Background, "图标+文本按钮应该有底图");
            Assert.IsNotNull(iconText.Target, "图标+文本按钮应该有按钮本体");
            Assert.IsNotNull(iconText.Icon, "图标+文本按钮应该有图标");
            Assert.IsNotNull(iconText.Label, "图标+文本按钮应该有文字");

            var icon = KitButtonOf("ButtonIcon");
            Assert.IsNotNull(icon.Background, "图标按钮应该有底图");
            Assert.IsNotNull(icon.Icon, "图标按钮应该有图标");
            Assert.IsNull(icon.Label, "图标按钮不该有文字引用");

            var text = KitButtonOf("ButtonText");
            Assert.IsNull(text.Background, "文字按钮不该有底图");
            Assert.IsNull(text.Icon, "文字按钮不该有图标");
            Assert.IsNotNull(text.Label, "文字按钮应该有文字");
            Assert.IsNotNull(text.Target, "文字按钮应该有按钮本体");
        }

        [Test]
        public void Text_prefabs_point_their_match_at_their_own_text()
        {
            foreach (var name in new[] { "TextTitle", "TextBody", "TextCaption" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(name));
                var match = prefab.GetComponent<TokenMatch>();

                Assert.IsNotNull(match, name + " 上没有 TokenMatch");
                Assert.AreEqual(1, match.Entries.Count, name + " 应该只有一条条目");
                Assert.AreSame(prefab.GetComponent<TMPro.TMP_Text>(), match.Entries[0].Target, name + " 的 target 应该指向自己的 TMP_Text");
            }
        }

        [Test]
        public void Demo_tokens_cover_every_id_the_package_prefabs_use()
        {
            var folder = SamplesDemoFolder() + "/Tokens";
            Assert.IsTrue(Directory.Exists(folder), "示例 token 目录不存在：" + folder);

            var declared = new List<string>();
            foreach (var file in Directory.GetFiles(folder, "*.asset"))
            {
                var value = MatchIdIn(file);
                if (!string.IsNullOrEmpty(value))
                {
                    declared.Add(value);
                }
            }

            foreach (var prefab in PackagePrefabs())
            {
                foreach (var match in prefab.GetComponentsInChildren<TokenMatch>(true))
                {
                    foreach (var entry in match.Entries)
                    {
                        CollectionAssert.Contains(declared, entry.Id, "示例 token 里没有「" + entry.Id + "」，示例校验会报缺 token");
                    }
                }
            }
        }

        [Test]
        public void Demo_nested_tokens_keep_their_match_id_empty()
        {
            var folder = SamplesDemoFolder() + "/Tokens";

            foreach (var name in NestedDemoTokens)
            {
                var file = folder + "/" + name;
                Assert.IsTrue(File.Exists(file), "缺示例 token：" + file);
                Assert.IsTrue(string.IsNullOrEmpty(MatchIdIn(file)), name + " 的匹配 id 应该留空（它只被组合 token 嵌套引用）");
            }
        }

        [Test]
        public void Every_guid_referenced_by_the_demo_resolves_inside_the_package_or_the_demo()
        {
            var demoGuids = new HashSet<string>();
            CollectGuids(SamplesDemoFolder(), demoGuids);

            var checkedReferences = 0;
            var dangling = new List<string>();

            foreach (var file in Directory.GetFiles(SamplesDemoFolder(), "*", SearchOption.AllDirectories))
            {
                var extension = Path.GetExtension(file).ToLowerInvariant();
                if (extension != ".unity" && extension != ".prefab" && extension != ".asset")
                {
                    continue;
                }

                foreach (Match match in Regex.Matches(File.ReadAllText(file), @"guid: ([0-9a-f]{32})"))
                {
                    checkedReferences++;
                    var guid = match.Groups[1].Value;

                    if (demoGuids.Contains(guid))
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)))
                    {
                        continue;
                    }

                    dangling.Add(Path.GetFileName(file) + " -> " + guid);
                }
            }

            Assert.Greater(checkedReferences, 0, "示例里没有任何 GUID 引用，检查没生效。");
            CollectionAssert.IsEmpty(dangling, "示例里有断链引用：" + string.Join(", ", dangling));
        }

        private static IEnumerable<GameObject> PackagePrefabs()
        {
            var folder = PackageRoot + "/Prefabs";
            var paths = Directory.GetFiles(folder, "*.prefab");
            Assert.Greater(paths.Length, 0, "包内 Prefabs/ 里没有预制体。");

            foreach (var path in paths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path.Replace('\\', '/'));
                Assert.IsNotNull(asset, path);
                yield return asset;
            }
        }

        private static KitButton KitButtonOf(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(name));
            Assert.IsNotNull(prefab, name);
            var kit = prefab.GetComponent<KitButton>();
            Assert.IsNotNull(kit, name + " 上没有 KitButton");
            return kit;
        }

        private static string PrefabPath(string name)
        {
            return PackageRoot + "/Prefabs/" + name + ".prefab";
        }

        private static string MatchIdIn(string assetFile)
        {
            var match = Regex.Match(File.ReadAllText(assetFile), @"_matchId:[ \t]*(\S*)");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private static void CollectGuids(string folder, HashSet<string> into)
        {
            if (!Directory.Exists(folder))
            {
                return;
            }

            foreach (var meta in Directory.GetFiles(folder, "*.meta", SearchOption.AllDirectories))
            {
                var match = Regex.Match(File.ReadAllText(meta), @"guid: ([0-9a-f]{32})");
                if (match.Success)
                {
                    into.Add(match.Groups[1].Value);
                }
            }
        }

        private static string PackageResolvedRoot()
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(PackageRoot + "/package.json");
            Assert.IsNotNull(info, "找不到包 " + PackageRoot);
            return info.resolvedPath.Replace('\\', '/');
        }

        private static string SamplesDemoFolder()
        {
            return PackageResolvedRoot() + "/Samples~/Demo";
        }
    }
}
