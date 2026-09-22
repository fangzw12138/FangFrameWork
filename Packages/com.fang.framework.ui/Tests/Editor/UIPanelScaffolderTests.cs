using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Fang.Framework.UI.Editor.Tests
{
    public class UIPanelScaffolderTests
    {
        private const string TestRoot = "Assets/UiScaffoldTests";
        private const string RootNamespace = "Game";

        private const string PanelConfigFolder = TestRoot + "/Panels";
        private const string PanelPrefabFolder = TestRoot + "/Prefabs";
        private const string PanelScriptFolder = TestRoot + "/Scripts";
        private const string PanelVisualTreeFolder = TestRoot + "/VisualTrees";
        private const string LayerConfigFolder = TestRoot + "/Layers";
        private const string ProjectAssetPath = TestRoot + "/UiScaffoldProject.asset";

        private readonly List<Object> _transient = new List<Object>();
        private UIProjectConfigDataSo _project;

        [SetUp]
        public void SetUp()
        {
            DeleteTestRoot();
            AssetDatabase.CreateFolder("Assets", "UiScaffoldTests");
            EnsureFolder(PanelConfigFolder);
            EnsureFolder(PanelPrefabFolder);
            EnsureFolder(PanelScriptFolder);
            EnsureFolder(PanelVisualTreeFolder);
            EnsureFolder(LayerConfigFolder);
            _project = CreateProjectAsset();
        }

        [TearDown]
        public void TearDown()
        {
            DeleteTestRoot();

            for (var i = 0; i < _transient.Count; i++)
            {
                if (_transient[i] != null)
                {
                    Object.DestroyImmediate(_transient[i]);
                }
            }

            _transient.Clear();
            _project = null;
        }

        [Test]
        public void CreateProject_creates_the_asset_and_writes_the_given_folders()
        {
            var assetPath = TestRoot + "/FreshProject.asset";

            var result = UIPanelScaffolder.CreateProject(BuildOptions("FreshProject", "Game.Fresh", TestRoot));

            Assert.IsTrue(result.Success, result.ToStatusMessage());

            var project = AssetDatabase.LoadAssetAtPath<UIProjectConfigDataSo>(assetPath);
            Assert.IsNotNull(project);
            Assert.AreEqual("FreshProject", project.Id);
            Assert.AreEqual("Game.Fresh", project.RootNamespace);
            Assert.AreEqual(TestRoot + "/Panels", project.PanelConfigFolder);
            Assert.AreEqual(TestRoot + "/Prefabs", project.PanelPrefabFolder);
            Assert.AreEqual(TestRoot + "/Scripts", project.PanelScriptFolder);
            Assert.AreEqual(TestRoot + "/VisualTrees", project.PanelVisualTreeFolder);
            Assert.AreEqual(TestRoot + "/Layers", project.LayerConfigFolder);
        }

        [Test]
        public void CreateProject_honours_folders_that_are_typed_by_hand()
        {
            var options = BuildOptions("FreshProject", "Game.Fresh", TestRoot);
            options.PanelConfigFolder = TestRoot + "/Custom/Configs";

            var result = UIPanelScaffolder.CreateProject(options);

            Assert.IsTrue(result.Success, result.ToStatusMessage());
            Assert.IsTrue(AssetDatabase.IsValidFolder(TestRoot + "/Custom/Configs"));

            var project = AssetDatabase.LoadAssetAtPath<UIProjectConfigDataSo>(TestRoot + "/FreshProject.asset");
            Assert.AreEqual(TestRoot + "/Custom/Configs", project.PanelConfigFolder);
        }

        [Test]
        public void CreateProject_rejects_an_existing_asset_and_an_invalid_input()
        {
            var first = UIPanelScaffolder.CreateProject(BuildOptions("FreshProject", "Game.Fresh", TestRoot));
            Assert.IsTrue(first.Success, first.ToStatusMessage());

            var duplicate = UIPanelScaffolder.CreateProject(BuildOptions("FreshProject", "Game.Fresh", TestRoot));
            Assert.IsFalse(duplicate.Success);
            StringAssert.Contains("已存在", duplicate.ToStatusMessage());

            var outside = UIPanelScaffolder.CreateProject(BuildOptions("FreshProject", "Game.Fresh", "Temp"));
            Assert.IsFalse(outside.Success);
            StringAssert.Contains("Assets", outside.ToStatusMessage());

            var badName = UIPanelScaffolder.CreateProject(BuildOptions("Bad Name", "Game.Fresh", TestRoot));
            Assert.IsFalse(badName.Success);
            StringAssert.Contains("项目名称", badName.ToStatusMessage());

            var badFolder = BuildOptions("OtherProject", "Game.Fresh", TestRoot);
            badFolder.LayerConfigFolder = string.Empty;
            var folderResult = UIPanelScaffolder.CreateProject(badFolder);
            Assert.IsFalse(folderResult.Success);
            StringAssert.Contains("层 SO 目录", folderResult.ToStatusMessage());

            var badNamespace = UIPanelScaffolder.CreateProject(BuildOptions("OtherProject", string.Empty, TestRoot));
            Assert.IsFalse(badNamespace.Success);
            StringAssert.Contains("根命名空间", badNamespace.ToStatusMessage());
        }

        [Test]
        public void CreatePanel_visual_tree_writes_assets_and_registers_the_panel()
        {
            var result = UIPanelScaffolder.CreatePanel(_project, "Hud", UIPanelTrack.VisualTree);

            Assert.IsTrue(result.Success, result.ToStatusMessage());

            var paths = new UIPanelScaffoldPaths(_project, "Hud");
            Assert.AreEqual("Assets/UiScaffoldTests/Panels/HudConfigData.asset", paths.ConfigPath);
            Assert.AreEqual("Assets/UiScaffoldTests/VisualTrees/Hud/Hud.uxml", paths.UxmlPath);
            Assert.AreEqual("Assets/UiScaffoldTests/Scripts/Hud/HudController.cs", paths.ControllerScriptPath);

            Assert.IsTrue(File.Exists(Path.GetFullPath(paths.UxmlPath)));
            Assert.IsTrue(File.Exists(Path.GetFullPath(paths.UssPath)));
            Assert.IsTrue(File.Exists(Path.GetFullPath(paths.DataScriptPath)));
            Assert.IsTrue(File.Exists(Path.GetFullPath(paths.ControllerScriptPath)));

            var config = AssetDatabase.LoadAssetAtPath<UIVisualTreePanelConfigDataSo>(paths.ConfigPath);
            Assert.IsNotNull(config);
            Assert.AreEqual("Hud", config.Id);
            Assert.IsNotNull(config.VisualTreeAsset);
            Assert.AreEqual(1, config.StyleSheets.Count);
            Assert.IsNotNull(config.StyleSheets[0]);

            Assert.IsTrue(ContainsPanel(_project, config));

            var controllerSource = File.ReadAllText(Path.GetFullPath(paths.ControllerScriptPath));
            StringAssert.Contains("namespace Game.UI.Panels", controllerSource);
            StringAssert.Contains(
                "public sealed class HudController : UIVisualTreePanelController<UIVisualTreePanelConfigDataSo, HudData>",
                controllerSource);
        }

        [Test]
        public void CreatePanel_prefab_track_writes_scripts_and_config_without_visual_tree_assets()
        {
            var result = UIPanelScaffolder.CreatePanel(_project, "Menu", UIPanelTrack.Prefab);

            Assert.IsTrue(result.Success, result.ToStatusMessage());

            var paths = new UIPanelScaffoldPaths(_project, "Menu");
            Assert.IsFalse(File.Exists(Path.GetFullPath(paths.UxmlPath)));
            Assert.IsFalse(File.Exists(Path.GetFullPath(paths.UssPath)));
            Assert.IsTrue(File.Exists(Path.GetFullPath(paths.ControllerScriptPath)));

            var config = AssetDatabase.LoadAssetAtPath<UIPrefabPanelConfigDataSo>(paths.ConfigPath);
            Assert.IsNotNull(config);
            Assert.AreEqual("Menu", config.Id);
            Assert.IsNull(config.Prefab);

            var controllerSource = File.ReadAllText(Path.GetFullPath(paths.ControllerScriptPath));
            StringAssert.Contains(
                "public sealed class MenuController : UIPanelController<UIPrefabPanelConfigDataSo, MenuData>",
                controllerSource);
        }

        [Test]
        public void CreatePanel_rejects_an_existing_config()
        {
            UIPanelScaffolder.CreatePanel(_project, "Hud", UIPanelTrack.Prefab);

            var result = UIPanelScaffolder.CreatePanel(_project, "Hud", UIPanelTrack.Prefab);

            Assert.IsFalse(result.Success);
            StringAssert.Contains("已存在", result.ToStatusMessage());
        }

        [Test]
        public void CreatePanel_rejects_an_invalid_panel_name()
        {
            var result = UIPanelScaffolder.CreatePanel(_project, "Bad Name", UIPanelTrack.Prefab);

            Assert.IsFalse(result.Success);
            Assert.IsNotEmpty(result.ToStatusMessage());
            Assert.AreEqual(0, AssetDatabase.FindAssets("t:UIPrefabPanelConfigDataSo", new[] { PanelConfigFolder }).Length);
        }

        [Test]
        public void CreatePanel_rejects_a_missing_project()
        {
            var result = UIPanelScaffolder.CreatePanel(null, "Hud", UIPanelTrack.Prefab);

            Assert.IsFalse(result.Success);
            StringAssert.Contains("未选择 UI 项目", result.ToStatusMessage());
        }

        [Test]
        public void CreatePanel_rejects_a_project_without_folders()
        {
            var project = CreateTransientProject(RootNamespace, false);

            var result = UIPanelScaffolder.CreatePanel(project, "Hud", UIPanelTrack.Prefab);

            Assert.IsFalse(result.Success);
            StringAssert.Contains("未填写", result.ToStatusMessage());
        }

        [Test]
        public void CreatePanel_rejects_an_empty_root_namespace()
        {
            var project = CreateTransientProject(string.Empty, true);

            var result = UIPanelScaffolder.CreatePanel(project, "Hud", UIPanelTrack.Prefab);

            Assert.IsFalse(result.Success);
            StringAssert.Contains("根命名空间不能为空", result.ToStatusMessage());
        }

        [Test]
        public void CreatePrefab_reports_a_missing_panel_config()
        {
            var result = UIPanelScaffolder.CreatePrefab(_project, "Missing");

            Assert.IsFalse(result.Success);
            StringAssert.Contains("未找到 UGUI 面板配置", result.ToStatusMessage());
        }

        [Test]
        public void DeletePanel_removes_assets_and_unregisters_the_panel()
        {
            UIPanelScaffolder.CreatePanel(_project, "Hud", UIPanelTrack.VisualTree);

            var paths = new UIPanelScaffoldPaths(_project, "Hud");
            var config = AssetDatabase.LoadAssetAtPath<UIVisualTreePanelConfigDataSo>(paths.ConfigPath);

            var preview = UIPanelScaffolder.GetDeletionPreview(_project, config);
            CollectionAssert.Contains(preview, "配置：" + paths.ConfigPath);
            CollectionAssert.Contains(preview, "目录：" + paths.VisualTreeFolderPath);
            CollectionAssert.Contains(preview, "目录：" + paths.ScriptFolderPath);
            CollectionAssert.Contains(preview, "从 UI 项目中摘除：Hud");

            var result = UIPanelScaffolder.DeletePanel(_project, config);

            Assert.IsTrue(result.Success, result.ToStatusMessage());
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<UIVisualTreePanelConfigDataSo>(paths.ConfigPath));
            Assert.IsFalse(File.Exists(Path.GetFullPath(paths.UxmlPath)));
            Assert.IsFalse(File.Exists(Path.GetFullPath(paths.ControllerScriptPath)));
            Assert.AreEqual(0, _project.Panels.Count);
        }

        [Test]
        public void ScanPanels_reports_the_generated_panel()
        {
            UIPanelScaffolder.CreatePanel(_project, "Hud", UIPanelTrack.VisualTree);

            var infos = UIPanelScaffolder.ScanPanels(_project);

            Assert.AreEqual(1, infos.Count);
            Assert.AreEqual("Hud", infos[0].PanelId);
            Assert.AreEqual("Hud", infos[0].Paths.PanelName);
            Assert.AreEqual("UITK", infos[0].TrackName);
            Assert.IsTrue(infos[0].Registered);
            Assert.IsTrue(infos[0].UxmlExists);
            Assert.IsTrue(infos[0].UssExists);
        }

        [Test]
        public void ScanPanels_and_ValidateProject_handle_a_missing_project()
        {
            Assert.AreEqual(0, UIPanelScaffolder.ScanPanels(null).Count);
            Assert.AreEqual("未选择 UI 项目。", UIPanelScaffolder.ValidateProject(null));
        }

        [Test]
        public void ValidateProject_reports_duplicate_ids()
        {
            CreateRegisteredRawConfig("DupA", "dup");
            CreateRegisteredRawConfig("DupB", "dup");

            var report = UIPanelScaffolder.ValidateProject(_project);

            StringAssert.Contains("Id 重复：dup", report);
        }

        [Test]
        public void ValidateProject_reports_unregistered_configs()
        {
            CreateRawConfig("Orphan", "orphan");

            var report = UIPanelScaffolder.ValidateProject(_project);

            StringAssert.Contains("未登记", report);
        }

        [Test]
        public void ValidateProject_reports_no_project_level_errors_for_a_generated_panel()
        {
            UIPanelScaffolder.CreatePanel(_project, "Hud", UIPanelTrack.VisualTree);

            var report = UIPanelScaffolder.ValidateProject(_project);

            StringAssert.DoesNotContain("未填写", report);
            StringAssert.DoesNotContain("根命名空间", report);
            StringAssert.DoesNotContain("未登记", report);
        }

        private static UIProjectCreateOptions BuildOptions(string projectName, string rootNamespace, string projectFolder)
        {
            return new UIProjectCreateOptions
            {
                ProjectFolder = projectFolder,
                ProjectName = projectName,
                RootNamespace = rootNamespace,
                PanelConfigFolder = projectFolder + "/Panels",
                PanelPrefabFolder = projectFolder + "/Prefabs",
                PanelScriptFolder = projectFolder + "/Scripts",
                PanelVisualTreeFolder = projectFolder + "/VisualTrees",
                LayerConfigFolder = projectFolder + "/Layers",
            };
        }

        private UIProjectConfigDataSo CreateProjectAsset()
        {
            var project = CreateTransientProject(RootNamespace, true);
            AssetDatabase.CreateAsset(project, ProjectAssetPath);
            AssetDatabase.SaveAssets();
            return project;
        }

        private UIProjectConfigDataSo CreateTransientProject(string rootNamespace, bool withFolders)
        {
            var project = ScriptableObject.CreateInstance<UIProjectConfigDataSo>();
            var serialized = new SerializedObject(project);
            serialized.FindProperty("_id").stringValue = "ui-project";
            serialized.FindProperty("_rootNamespace").stringValue = rootNamespace;

            if (withFolders)
            {
                serialized.FindProperty("_panelConfigFolder").stringValue = PanelConfigFolder;
                serialized.FindProperty("_panelPrefabFolder").stringValue = PanelPrefabFolder;
                serialized.FindProperty("_panelScriptFolder").stringValue = PanelScriptFolder;
                serialized.FindProperty("_panelVisualTreeFolder").stringValue = PanelVisualTreeFolder;
                serialized.FindProperty("_layerConfigFolder").stringValue = LayerConfigFolder;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            _transient.Add(project);
            return project;
        }

        private void CreateRegisteredRawConfig(string fileName, string id)
        {
            var config = CreateRawConfig(fileName, id);

            var serialized = new SerializedObject(_project);
            var panels = serialized.FindProperty("_panels");
            panels.InsertArrayElementAtIndex(panels.arraySize);
            panels.GetArrayElementAtIndex(panels.arraySize - 1).objectReferenceValue = config;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private UIPrefabPanelConfigDataSo CreateRawConfig(string fileName, string id)
        {
            var config = ScriptableObject.CreateInstance<UIPrefabPanelConfigDataSo>();
            var serialized = new SerializedObject(config);
            serialized.FindProperty("_id").stringValue = id;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(config, PanelConfigFolder + "/" + fileName + "ConfigData.asset");
            AssetDatabase.SaveAssets();
            return config;
        }

        private static bool ContainsPanel(UIProjectConfigDataSo project, UIPanelConfigDataSo config)
        {
            if (project == null || config == null)
            {
                return false;
            }

            for (var i = 0; i < project.Panels.Count; i++)
            {
                if (project.Panels[i] == config)
                {
                    return true;
                }
            }

            return false;
        }

        private static void EnsureFolder(string assetFolderPath)
        {
            if (AssetDatabase.IsValidFolder(assetFolderPath))
            {
                return;
            }

            var parent = Path.GetDirectoryName(assetFolderPath).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(assetFolderPath));
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
