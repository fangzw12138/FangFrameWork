using System.Collections.Generic;

namespace Fang.Framework.UI.Editor
{
    public sealed class UIPanelScaffoldInfo
    {
        public UIPanelScaffoldInfo(
            UIPanelConfigDataSo config,
            string configPath,
            UIPanelScaffoldPaths paths,
            bool registered,
            bool uxmlExists,
            bool ussExists,
            bool scriptExists,
            bool prefabExists,
            bool panelSettingsAssigned)
        {
            Config = config;
            ConfigPath = configPath;
            Paths = paths;
            Registered = registered;
            UxmlExists = uxmlExists;
            UssExists = ussExists;
            ScriptExists = scriptExists;
            PrefabExists = prefabExists;
            PanelSettingsAssigned = panelSettingsAssigned;
        }

        public UIPanelConfigDataSo Config { get; }

        public string ConfigPath { get; }

        public UIPanelScaffoldPaths Paths { get; }

        public bool Registered { get; }

        public bool UxmlExists { get; }

        public bool UssExists { get; }

        public bool ScriptExists { get; }

        public bool PrefabExists { get; }

        public bool PanelSettingsAssigned { get; }

        public string PanelId => Config == null ? string.Empty : Config.Id;

        public string DisplayName
        {
            get
            {
                if (Config == null)
                {
                    return string.Empty;
                }

                return string.IsNullOrWhiteSpace(Config.DisplayName) ? Config.Id : Config.DisplayName;
            }
        }

        public string LayerId => Config == null ? string.Empty : Config.LayerId;

        public bool IsVisualTree => Config is UIVisualTreePanelConfigDataSo;

        public string TrackName => IsVisualTree ? "UITK" : "UGUI";

        public bool IsComplete
        {
            get
            {
                if (!Registered || !ScriptExists)
                {
                    return false;
                }

                return IsVisualTree ? UxmlExists && PanelSettingsAssigned : PrefabExists;
            }
        }

        public IReadOnlyList<string> GetMissingItems()
        {
            var missing = new List<string>();
            if (!Registered)
            {
                missing.Add("未登记到项目配置");
            }

            if (!ScriptExists)
            {
                missing.Add("控制器脚本缺失");
            }

            if (IsVisualTree)
            {
                if (!UxmlExists)
                {
                    missing.Add("UXML 缺失");
                }

                if (!PanelSettingsAssigned)
                {
                    missing.Add("PanelSettings 未设置");
                }
            }
            else if (!PrefabExists)
            {
                missing.Add("预制体缺失");
            }

            return missing;
        }
    }
}
