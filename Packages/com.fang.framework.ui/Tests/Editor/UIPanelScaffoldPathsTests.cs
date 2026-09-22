using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Fang.Framework.UI.Editor.Tests
{
    public class UIPanelScaffoldPathsTests
    {
        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < _created.Count; i++)
            {
                if (_created[i] != null)
                {
                    Object.DestroyImmediate(_created[i]);
                }
            }

            _created.Clear();
        }

        [Test]
        public void NormalizeFolderPath_keeps_assets_paths_and_trims_trailing_slashes()
        {
            Assert.AreEqual("Assets/Game", UIPanelScaffoldPaths.NormalizeFolderPath("Assets/Game"));
            Assert.AreEqual("Assets/Game", UIPanelScaffoldPaths.NormalizeFolderPath("Assets/Game/"));
            Assert.AreEqual("Assets/Game", UIPanelScaffoldPaths.NormalizeFolderPath("Assets\\Game"));
            Assert.AreEqual("Assets", UIPanelScaffoldPaths.NormalizeFolderPath("Assets"));
        }

        [Test]
        public void NormalizeFolderPath_rejects_paths_outside_assets()
        {
            Assert.IsNull(UIPanelScaffoldPaths.NormalizeFolderPath(null));
            Assert.IsNull(UIPanelScaffoldPaths.NormalizeFolderPath("  "));
            Assert.IsNull(UIPanelScaffoldPaths.NormalizeFolderPath("Packages/com.fang.framework.ui"));
            Assert.IsNull(UIPanelScaffoldPaths.NormalizeFolderPath("AssetsExtra/Game"));
        }

        [Test]
        public void TryValidatePanelName_accepts_identifiers()
        {
            Assert.IsTrue(UIPanelScaffoldPaths.TryValidatePanelName("Hud", out var name, out _));
            Assert.AreEqual("Hud", name);

            Assert.IsTrue(UIPanelScaffoldPaths.TryValidatePanelName("Hud_Panel2", out var second, out _));
            Assert.AreEqual("Hud_Panel2", second);

            Assert.IsTrue(UIPanelScaffoldPaths.TryValidatePanelName("  Hud  ", out var trimmed, out _));
            Assert.AreEqual("Hud", trimmed);
        }

        [Test]
        public void TryValidatePanelName_rejects_invalid_names()
        {
            Assert.IsFalse(UIPanelScaffoldPaths.TryValidatePanelName(null, out _, out var emptyError));
            Assert.IsNotEmpty(emptyError);

            Assert.IsFalse(UIPanelScaffoldPaths.TryValidatePanelName("   ", out _, out _));
            Assert.IsFalse(UIPanelScaffoldPaths.TryValidatePanelName("Bad Name", out _, out var spaceError));
            Assert.IsNotEmpty(spaceError);
            Assert.IsFalse(UIPanelScaffoldPaths.TryValidatePanelName("Hud-Panel", out _, out _));
            Assert.IsFalse(UIPanelScaffoldPaths.TryValidatePanelName("2Hud", out _, out _));
            Assert.IsFalse(UIPanelScaffoldPaths.TryValidatePanelName("Hud.Panel", out _, out _));
        }

        [Test]
        public void TryValidateNamespace_accepts_dotted_identifiers()
        {
            Assert.IsTrue(UIPanelScaffoldPaths.TryValidateNamespace("Game", out var single, out _));
            Assert.AreEqual("Game", single);

            Assert.IsTrue(UIPanelScaffoldPaths.TryValidateNamespace("Game.UI.Panels", out var dotted, out _));
            Assert.AreEqual("Game.UI.Panels", dotted);
        }

        [Test]
        public void TryValidateNamespace_rejects_invalid_namespaces()
        {
            Assert.IsFalse(UIPanelScaffoldPaths.TryValidateNamespace(null, out _, out var emptyError));
            Assert.IsNotEmpty(emptyError);

            Assert.IsFalse(UIPanelScaffoldPaths.TryValidateNamespace("Game..UI", out _, out _));
            Assert.IsFalse(UIPanelScaffoldPaths.TryValidateNamespace("1Game", out _, out _));
            Assert.IsFalse(UIPanelScaffoldPaths.TryValidateNamespace("Game.UI Panel", out _, out _));
        }

        [Test]
        public void Paths_are_derived_from_the_project_folders_and_the_panel_name()
        {
            var project = CreateProject();
            var paths = new UIPanelScaffoldPaths(project, "Hud");

            Assert.AreSame(project, paths.Project);
            Assert.AreEqual("Hud", paths.PanelName);
            Assert.AreEqual("HudData", paths.DataTypeName);
            Assert.AreEqual("HudController", paths.ControllerTypeName);

            Assert.AreEqual("Assets/DemoA/UI/Panels/HudConfigData.asset", paths.ConfigPath);
            Assert.AreEqual("Assets/DemoA/UI/Prefabs/Hud.prefab", paths.PrefabPath);
            Assert.AreEqual("Assets/DemoA/Scripts/UI/Hud", paths.ScriptFolderPath);
            Assert.AreEqual("Assets/DemoA/Scripts/UI/Hud/HudData.cs", paths.DataScriptPath);
            Assert.AreEqual("Assets/DemoA/Scripts/UI/Hud/HudController.cs", paths.ControllerScriptPath);
            Assert.AreEqual("Assets/DemoA/UI/VisualTrees/Hud", paths.VisualTreeFolderPath);
            Assert.AreEqual("Assets/DemoA/UI/VisualTrees/Hud/Hud.uxml", paths.UxmlPath);
            Assert.AreEqual("Assets/DemoA/UI/VisualTrees/Hud/Hud.uss", paths.UssPath);
            Assert.AreEqual("Assets/DemoA/UI/Layers", paths.LayerConfigFolderPath);
        }

        [Test]
        public void Paths_fall_back_to_empty_strings_when_the_project_has_no_folders()
        {
            var project = CreateInstance();
            var paths = new UIPanelScaffoldPaths(project, "Hud");

            Assert.AreEqual(string.Empty, paths.ConfigPath);
            Assert.AreEqual(string.Empty, paths.PrefabPath);
            Assert.AreEqual(string.Empty, paths.ScriptFolderPath);
            Assert.AreEqual(string.Empty, paths.UxmlPath);
            Assert.AreEqual(string.Empty, paths.LayerConfigFolderPath);
        }

        [Test]
        public void Paths_fall_back_to_empty_strings_for_a_null_project()
        {
            var paths = new UIPanelScaffoldPaths(null, "Hud");

            Assert.AreEqual(string.Empty, paths.ConfigPath);
            Assert.AreEqual(string.Empty, paths.ScriptFolderPath);
            Assert.AreEqual(string.Empty, paths.LayerConfigFolderPath);
        }

        [Test]
        public void TryValidateProjectFolders_reports_every_missing_folder()
        {
            var project = CreateInstance();

            Assert.IsFalse(UIPanelScaffoldPaths.TryValidateProjectFolders(project, out var errors));
            Assert.AreEqual(5, errors.Count);
        }

        [Test]
        public void TryValidateProjectFolders_rejects_folders_outside_assets()
        {
            var project = CreateProject(panelConfigFolder: "Packages/Whatever");

            Assert.IsFalse(UIPanelScaffoldPaths.TryValidateProjectFolders(project, out var errors));
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("面板 SO 目录", errors[0]);
        }

        [Test]
        public void TryValidateProjectFolders_accepts_a_complete_project()
        {
            var project = CreateProject();

            Assert.IsTrue(UIPanelScaffoldPaths.TryValidateProjectFolders(project, out var errors));
            Assert.AreEqual(0, errors.Count);
        }

        private UIProjectConfigDataSo CreateProject(
            string panelConfigFolder = "Assets/DemoA/UI/Panels",
            string panelPrefabFolder = "Assets/DemoA/UI/Prefabs",
            string panelScriptFolder = "Assets/DemoA/Scripts/UI",
            string panelVisualTreeFolder = "Assets/DemoA/UI/VisualTrees",
            string layerConfigFolder = "Assets/DemoA/UI/Layers",
            string rootNamespace = "DemoA")
        {
            var project = CreateInstance();
            var serialized = new SerializedObject(project);
            serialized.FindProperty("_rootNamespace").stringValue = rootNamespace;
            serialized.FindProperty("_panelConfigFolder").stringValue = panelConfigFolder;
            serialized.FindProperty("_panelPrefabFolder").stringValue = panelPrefabFolder;
            serialized.FindProperty("_panelScriptFolder").stringValue = panelScriptFolder;
            serialized.FindProperty("_panelVisualTreeFolder").stringValue = panelVisualTreeFolder;
            serialized.FindProperty("_layerConfigFolder").stringValue = layerConfigFolder;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return project;
        }

        private UIProjectConfigDataSo CreateInstance()
        {
            var project = ScriptableObject.CreateInstance<UIProjectConfigDataSo>();
            _created.Add(project);
            return project;
        }
    }
}
