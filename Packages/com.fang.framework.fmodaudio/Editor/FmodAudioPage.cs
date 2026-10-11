using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Fang.Framework.Editor.Hub;
using FMODUnity;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fang.Framework.FmodAudio.Editor
{
    [FangHubPage("framework-fmod-audio", "FMOD 音频", "FMOD",
        Description = "FMOD 总线通道配置：新建、校验、直接编辑；运行时查看音量与活动实例。", Order = 0)]
    public sealed class FmodAudioPage : IFangHubVisualElementPage
    {
        private const string StateScope = "framework-fmod-audio";
        private const string SelectedGuidKey = "SelectedConfig";
        private const string SettingsResourceName = "FMODStudioSettings";

        private const float ListPaneWidth = 230f;
        private const float DetailLabelWidth = 96f;

        private static readonly Color DimColor = new Color(0.65f, 0.65f, 0.65f);
        private static readonly Color WarnColor = new Color(1f, 0.6f, 0.4f);
        private static readonly Color LineColor = new Color(0.28f, 0.28f, 0.28f);
        private static readonly Color HoverColor = new Color(0.21f, 0.29f, 0.44f);
        private static readonly Color SelectedColor = new Color(0.24f, 0.37f, 0.59f);

        private static readonly FieldInfo InitializedField = typeof(FmodAudioService).GetField(
            "_initialized",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo ActiveEventsField = typeof(FmodAudioService).GetField(
            "_activeEvents",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo ChannelVolumesField = typeof(FmodAudioService).GetField(
            "_channelVolumes",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly Type ActiveEventType = typeof(FmodAudioService).GetNestedType(
            "ActiveEvent",
            BindingFlags.NonPublic);

        private static readonly FieldInfo ActiveEventInstanceField = ActiveEventType == null
            ? null
            : ActiveEventType.GetField("Instance", BindingFlags.Instance | BindingFlags.Public);

        private static readonly FieldInfo ActiveEventRefField = ActiveEventType == null
            ? null
            : ActiveEventType.GetField("EventRef", BindingFlags.Instance | BindingFlags.Public);

        private readonly List<FmodAudioConfigSo> choices = new List<FmodAudioConfigSo>();

        private Label statusLabel;
        private Label titleLabel;
        private Label listHeaderLabel;
        private VisualElement listContainer;
        private VisualElement detailContent;
        private FmodAudioConfigSo selectedConfig;
        private SerializedObject inspectorSerializedObject;
        private VisualElement inspectorElement;
        private bool inspectorNeedsRebuild;

        public void OnInitialize(FangHubWindow window)
        {
        }

        public void OnSelected()
        {
            RebuildChoices();
            Refresh();
        }

        public VisualElement CreateVisualElement()
        {
            var root = new VisualElement();
            root.style.flexGrow = 1f;

            root.Add(BuildToolbar());
            root.Add(BuildTitleRow());
            root.Add(BuildSplit());

            root.RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);
            root.RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);

            RebuildChoices();
            Refresh();
            return root;
        }

        // 停靠 / 拖出 Fang Hub 窗口时，Unity 会把整棵树从旧 panel 摘下来再挂到新 panel。
        // 必须先摘掉绑着 SerializedObject 的 Inspector 才能释放它；否则重新挂载时
        // Unity 的绑定会去读已释放的对象并抛 NullReferenceException，窗口就此半挂。
        private void OnDetachedFromPanel(DetachFromPanelEvent evt)
        {
            DetachInspector();
            inspectorNeedsRebuild = true;
        }

        private void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            if (!inspectorNeedsRebuild)
            {
                return;
            }

            inspectorNeedsRebuild = false;
            RebuildDetail();
        }

        private VisualElement BuildToolbar()
        {
            var toolbar = new Toolbar();
            toolbar.style.marginBottom = 4f;

            toolbar.Add(new ToolbarButton(RefreshAll) { text = "刷新" });
            toolbar.Add(new ToolbarButton(CreateOrSelectConfig) { text = "新建配置" });
            toolbar.Add(new ToolbarButton(ValidateSelected) { text = "校验" });

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1f;
            toolbar.Add(spacer);

            statusLabel = new Label();
            statusLabel.style.color = DimColor;
            statusLabel.style.whiteSpace = WhiteSpace.NoWrap;
            statusLabel.style.overflow = Overflow.Hidden;
            statusLabel.style.marginRight = 6f;
            toolbar.Add(statusLabel);

            return toolbar;
        }

        private VisualElement BuildTitleRow()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 4f;
            row.style.marginBottom = 4f;

            titleLabel = new Label();
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.fontSize = 14f;
            row.Add(titleLabel);

            return row;
        }

        private VisualElement BuildSplit()
        {
            var split = new VisualElement();
            split.style.flexDirection = FlexDirection.Row;
            split.style.flexGrow = 1f;
            split.style.alignItems = Align.Stretch;

            split.Add(BuildListPane());
            split.Add(BuildDetailPane());
            return split;
        }

        private VisualElement BuildListPane()
        {
            var pane = new VisualElement();
            pane.style.width = ListPaneWidth;
            pane.style.flexShrink = 0f;
            pane.style.borderRightWidth = 1f;
            pane.style.borderRightColor = LineColor;
            pane.style.paddingLeft = 6f;
            pane.style.paddingRight = 6f;

            listHeaderLabel = new Label();
            listHeaderLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            listHeaderLabel.style.marginTop = 6f;
            listHeaderLabel.style.marginBottom = 2f;
            pane.Add(listHeaderLabel);

            var scroll = new ScrollView();
            scroll.style.flexGrow = 1f;
            listContainer = scroll.contentContainer;
            pane.Add(scroll);

            return pane;
        }

        private VisualElement BuildDetailPane()
        {
            var pane = new VisualElement();
            pane.style.flexGrow = 1f;
            pane.style.paddingLeft = 12f;
            pane.style.paddingRight = 8f;

            var scroll = new ScrollView();
            scroll.style.flexGrow = 1f;
            detailContent = scroll.contentContainer;
            pane.Add(scroll);

            return pane;
        }

        private void RefreshAll()
        {
            RebuildChoices();
            Refresh();
        }

        private void RebuildChoices()
        {
            var selectedGuid = FangHubPageState.GetString(StateScope, SelectedGuidKey);
            choices.Clear();

            var paths = new List<string>();
            var guids = AssetDatabase.FindAssets("t:" + nameof(FmodAudioConfigSo));
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!string.IsNullOrEmpty(path))
                {
                    paths.Add(path);
                }
            }

            paths.Sort(StringComparer.Ordinal);

            selectedConfig = null;
            for (var i = 0; i < paths.Count; i++)
            {
                var config = AssetDatabase.LoadAssetAtPath<FmodAudioConfigSo>(paths[i]);
                if (config == null)
                {
                    continue;
                }

                choices.Add(config);

                if (AssetDatabase.AssetPathToGUID(paths[i]) == selectedGuid)
                {
                    selectedConfig = config;
                }
            }

            if (selectedConfig == null && choices.Count > 0)
            {
                selectedConfig = choices[0];
            }
        }

        private void Refresh()
        {
            if (listContainer == null)
            {
                return;
            }

            titleLabel.text = selectedConfig == null
                ? "当前配置：未选择"
                : "当前配置：" + selectedConfig.name;

            RebuildList();
            RebuildDetail();
        }

        private void RebuildList()
        {
            listContainer.Clear();
            listHeaderLabel.text = "配置（" + choices.Count + "）";

            if (choices.Count == 0)
            {
                listContainer.Add(BuildMessage("工程里还没有 FMOD 音频配置，点上面的「新建配置」在 "
                    + FmodAudioConfigScaffolder.AssetPath + " 建一份。"));
                return;
            }

            for (var i = 0; i < choices.Count; i++)
            {
                listContainer.Add(BuildConfigRow(choices[i]));
            }
        }

        private VisualElement BuildConfigRow(FmodAudioConfigSo config)
        {
            var path = AssetDatabase.GetAssetPath(config);
            var row = BuildRowShell(config == selectedConfig);

            var mark = new Label(HasNoIssue(config) ? "✓" : "✗");
            mark.style.width = 14f;
            mark.style.flexShrink = 0f;
            mark.style.color = HasNoIssue(config) ? Color.white : WarnColor;
            row.Add(mark);

            var text = new VisualElement();
            text.style.flexGrow = 1f;
            text.style.overflow = Overflow.Hidden;

            var name = new Label(config.name);
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            name.style.overflow = Overflow.Hidden;
            text.Add(name);

            var detail = new Label(path);
            detail.style.color = DimColor;
            detail.style.fontSize = 10f;
            detail.style.overflow = Overflow.Hidden;
            text.Add(detail);

            row.Add(text);
            row.RegisterCallback<ClickEvent>(_ => SelectConfig(config));

            return row;
        }

        private void SelectConfig(FmodAudioConfigSo config)
        {
            selectedConfig = config;
            FangHubPageState.SetString(StateScope, SelectedGuidKey, GetAssetGuid(config));
            Refresh();
        }

        private void CreateOrSelectConfig()
        {
            var existed = FmodAudioConfigScaffolder.Load() != null;
            var config = FmodAudioConfigScaffolder.Create();

            if (config == null)
            {
                SetStatus("新建配置失败。");
                return;
            }

            RebuildChoices();
            SelectConfig(config);
            SetStatus(existed
                ? "配置已存在，已选中：" + FmodAudioConfigScaffolder.AssetPath
                : "配置已创建：" + FmodAudioConfigScaffolder.AssetPath);
        }

        private void ValidateSelected()
        {
            var config = selectedConfig;
            if (config == null)
            {
                SetStatus("未选择配置。");
                return;
            }

            SetStatus(FmodConfigValidator.TryValidate(config, out var error)
                ? "✓ 配置通过校验。"
                : "✗ " + error);
        }

        private void RebuildDetail()
        {
            if (detailContent == null)
            {
                return;
            }

            DetachInspector();
            detailContent.Clear();

            detailContent.Add(BuildFmodStatus());

            if (selectedConfig == null)
            {
                detailContent.Add(BuildSection("配置"));
                detailContent.Add(BuildMessage("左侧还没有可查看的配置。"));
                return;
            }

            var path = AssetDatabase.GetAssetPath(selectedConfig);

            detailContent.Add(BuildDetailHeader(selectedConfig.name, path));

            detailContent.Add(BuildSection("基本信息"));
            detailContent.Add(BuildFieldRow("资产路径", path));
            detailContent.Add(BuildFieldRow("主总线", selectedConfig.MasterBusPath));
            detailContent.Add(BuildFieldRow("默认主音量", selectedConfig.DefaultMasterVolume.ToString("0.00")));
            detailContent.Add(BuildFieldRow("通道数", selectedConfig.BusChannels.Count.ToString()));

            detailContent.Add(BuildSection("通道"));
            detailContent.Add(BuildChannelList(selectedConfig));

            detailContent.Add(BuildSection("校验"));
            detailContent.Add(BuildValidation(selectedConfig));

            detailContent.Add(BuildSection("运行时快照"));
            detailContent.Add(BuildRuntimeSnapshot());

            detailContent.Add(BuildSection("配置（可直接编辑）"));
            detailContent.Add(BuildInspector(selectedConfig));
        }

        private static VisualElement BuildFmodStatus()
        {
            var basic = BuildCollapsibleSection("FMOD 状态", false);

            basic.Add(BuildFieldRow("FMOD for Unity", FmodVersion()));
            basic.Add(BuildFieldRow("设置资产", SettingsAssetPath() ?? "未找到 FMODStudioSettings.asset"));
            basic.Add(BuildFieldRow("事件缓存", EventCacheText()));
            basic.Add(BuildFieldRow("运行时", Application.isPlaying
                ? (RuntimeManager.IsInitialized ? "已初始化" : "未初始化")
                : "未进入 Play 模式"));

            return basic;
        }

        private static VisualElement BuildChannelList(FmodAudioConfigSo config)
        {
            var channels = config.BusChannels;
            var container = new VisualElement();

            if (channels.Count == 0)
            {
                container.Add(BuildMessage("这个配置没有任何通道。"));
                return container;
            }

            for (var i = 0; i < channels.Count; i++)
            {
                var channel = channels[i];
                if (channel == null)
                {
                    var empty = new Label("✗ 第 " + i + " 条通道是空引用。");
                    empty.style.color = WarnColor;
                    container.Add(empty);
                    continue;
                }

                container.Add(BuildFieldRow(
                    string.IsNullOrEmpty(channel.ChannelId) ? "（没有 id）" : channel.ChannelId,
                    channel.BusPath + "  ·  默认音量 " + channel.DefaultVolume.ToString("0.00")));
            }

            return container;
        }

        private static VisualElement BuildValidation(FmodAudioConfigSo config)
        {
            var container = new VisualElement();

            if (FmodConfigValidator.TryValidate(config, out var error))
            {
                var ok = new Label("✓ 通过：通道 id 非空且不重复，bus 路径都是 bus:/ 前缀。");
                ok.style.whiteSpace = WhiteSpace.Normal;
                container.Add(ok);
            }
            else
            {
                var bad = new Label("✗ " + error);
                bad.style.whiteSpace = WhiteSpace.Normal;
                bad.style.color = WarnColor;
                container.Add(bad);
            }

            var note = new Label(
                "总线和事件是否真实存在只能在 FMOD 运行时判定（RuntimeManager.GetBus / CreateInstance）；"
                + "FMOD 编辑器缓存不暴露总线列表，所以编辑器侧只校验配置本身。"
                + "事件路径用字段上的 ⊙ 从事件缓存里选，选中即存在。");
            note.style.whiteSpace = WhiteSpace.Normal;
            note.style.color = DimColor;
            note.style.marginTop = 4f;
            container.Add(note);

            return container;
        }

        private static VisualElement BuildRuntimeSnapshot()
        {
            var container = new VisualElement();

            if (!Application.isPlaying)
            {
                container.Add(BuildMessage("运行时快照只在 Play 模式有内容。"));
                return container;
            }

            container.Add(BuildFieldRow("静音", RuntimeManager.IsInitialized
                ? (RuntimeManager.IsMuted ? "是" : "否")
                : "运行时未初始化"));

            var services = FindServices();
            if (services.Count == 0)
            {
                container.Add(BuildMessage("场景里没有 FmodAudioService。它由 Scope.AddService<FmodAudioService>() 创建。"));
                return container;
            }

            for (var i = 0; i < services.Count; i++)
            {
                container.Add(BuildServiceSnapshot(services[i]));
            }

            return container;
        }

        private static VisualElement BuildServiceSnapshot(FmodAudioService service)
        {
            var block = new VisualElement();
            block.style.marginTop = 4f;

            var title = new Label(BuildPath(service.transform));
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            block.Add(title);

            block.Add(BuildFieldRow("已初始化", ReadBool(InitializedField, service) ? "是" : "否"));
            block.Add(BuildFieldRow("主音量", service.MasterVolume.ToString("0.00")));

            var volumes = ReadChannelVolumes(service);
            if (volumes.Count == 0)
            {
                block.Add(BuildMessage("没有登记任何通道。"));
            }
            else
            {
                for (var i = 0; i < volumes.Count; i++)
                {
                    block.Add(BuildFieldRow("通道 " + volumes[i].Key, volumes[i].Value.ToString("0.00")));
                }
            }

            var active = ReadActiveEvents(service);
            block.Add(BuildFieldRow("活动实例", active.Count.ToString()));

            for (var i = 0; i < active.Count; i++)
            {
                var line = new Label("· " + active[i]);
                line.style.marginLeft = 12f;
                line.style.fontSize = 11f;
                line.style.color = DimColor;
                block.Add(line);
            }

            return block;
        }

        private static List<FmodAudioService> FindServices()
        {
            var services = new List<FmodAudioService>();
            var candidates = Resources.FindObjectsOfTypeAll<FmodAudioService>();

            for (var i = 0; i < candidates.Length; i++)
            {
                var service = candidates[i];
                if (service != null && service.gameObject.scene.IsValid())
                {
                    services.Add(service);
                }
            }

            return services;
        }

        private static List<string> ReadActiveEvents(FmodAudioService service)
        {
            var result = new List<string>();
            if (ActiveEventsField == null)
            {
                return result;
            }

            if (!(ActiveEventsField.GetValue(service) is IDictionary raw))
            {
                return result;
            }

            foreach (DictionaryEntry entry in raw)
            {
                var active = entry.Value;
                if (active == null)
                {
                    continue;
                }

                var reference = ActiveEventRefField == null ? null : ActiveEventRefField.GetValue(active);
                var instance = ActiveEventInstanceField == null ? null : ActiveEventInstanceField.GetValue(active);

                result.Add(DescribeInstance(instance, reference));
            }

            return result;
        }

        private static string DescribeInstance(object instance, object reference)
        {
            var text = reference == null ? "（没有事件引用）" : reference.ToString();

            if (instance is FMOD.Studio.EventInstance eventInstance && eventInstance.isValid())
            {
                eventInstance.getPlaybackState(out var state);
                text = state + "  ·  " + text;
            }

            return text;
        }

        private static List<KeyValuePair<string, float>> ReadChannelVolumes(FmodAudioService service)
        {
            var result = new List<KeyValuePair<string, float>>();
            if (ChannelVolumesField == null)
            {
                return result;
            }

            if (!(ChannelVolumesField.GetValue(service) is IDictionary raw))
            {
                return result;
            }

            foreach (DictionaryEntry entry in raw)
            {
                if (entry.Key is string id && entry.Value is float volume)
                {
                    result.Add(new KeyValuePair<string, float>(id, volume));
                }
            }

            result.Sort((left, right) => string.CompareOrdinal(left.Key, right.Key));
            return result;
        }

        private static bool ReadBool(FieldInfo field, object target)
        {
            if (field == null || target == null)
            {
                return false;
            }

            var value = field.GetValue(target);
            return value is bool flag && flag;
        }

        private static bool HasNoIssue(FmodAudioConfigSo config)
        {
            return config != null && FmodConfigValidator.TryValidate(config, out _);
        }

        private static string FmodVersion()
        {
            var number = FMOD.VERSION.number;
            var major = (number >> 16) & 0xFF;
            var minor = (number >> 8) & 0xFF;
            var patch = number & 0xFF;

            var suffix = string.IsNullOrEmpty(FMOD.VERSION.suffix) ? string.Empty : " · " + FMOD.VERSION.suffix;
            return major + "." + minor.ToString("00") + "." + patch + "（2.03 起受支持）" + suffix;
        }

        private static string SettingsAssetPath()
        {
            var asset = Resources.Load(SettingsResourceName);
            if (asset == null)
            {
                return null;
            }

            var path = AssetDatabase.GetAssetPath(asset);
            return string.IsNullOrEmpty(path) ? null : path;
        }

        private static string EventCacheText()
        {
            try
            {
                var events = EventManager.Events;
                var count = events == null ? 0 : events.Count;
                return count == 0
                    ? "0 个事件（为空：未指定 FMOD 工程或 bank 未导入，事件路径取不到列表）"
                    : count + " 个事件";
            }
            catch (Exception exception)
            {
                return "不可用：" + exception.Message;
            }
        }

        private VisualElement BuildInspector(FmodAudioConfigSo config)
        {
            inspectorSerializedObject = new SerializedObject(config);
            inspectorElement = new InspectorElement(inspectorSerializedObject);
            inspectorElement.style.marginTop = 2f;
            return inspectorElement;
        }

        /// <summary>先摘元素再释放 SerializedObject，顺序不能反。</summary>
        private void DetachInspector()
        {
            if (inspectorElement != null)
            {
                inspectorElement.RemoveFromHierarchy();
                inspectorElement = null;
            }

            DisposeInspector();
        }

        private void DisposeInspector()
        {
            if (inspectorSerializedObject == null)
            {
                return;
            }

            inspectorSerializedObject.Dispose();
            inspectorSerializedObject = null;
        }

        private static VisualElement BuildDetailHeader(string title, string subtitle)
        {
            var header = new VisualElement();

            var name = new Label(title);
            name.style.fontSize = 14f;
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(name);

            var line = new Label(string.IsNullOrEmpty(subtitle) ? "（没有路径）" : subtitle);
            line.style.color = DimColor;
            line.style.whiteSpace = WhiteSpace.Normal;
            header.Add(line);

            return header;
        }

        private static VisualElement BuildFieldRow(string label, string value)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            var name = new Label(label);
            name.style.width = DetailLabelWidth;
            name.style.flexShrink = 0f;
            name.style.color = DimColor;
            row.Add(name);

            var text = new Label(string.IsNullOrEmpty(value) ? "（空）" : value);
            text.style.flexGrow = 1f;
            text.style.whiteSpace = WhiteSpace.Normal;
            row.Add(text);

            return row;
        }

        private static Label BuildSection(string title)
        {
            var label = new Label(title);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginTop = 8f;
            label.style.marginBottom = 2f;
            return label;
        }

        private static Foldout BuildCollapsibleSection(string title, bool expanded)
        {
            var foldout = new Foldout { text = title, value = expanded };
            foldout.style.marginTop = 8f;

            var header = foldout.Q<Toggle>();
            if (header != null && header.labelElement != null)
            {
                header.labelElement.style.unityFontStyleAndWeight = FontStyle.Bold;
            }

            return foldout;
        }

        private static VisualElement BuildRowShell(bool selected)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = 6f;
            row.style.paddingRight = 6f;
            row.style.paddingTop = 3f;
            row.style.paddingBottom = 3f;
            row.style.backgroundColor = selected ? SelectedColor : Color.clear;

            if (!selected)
            {
                row.RegisterCallback<MouseEnterEvent>(_ => row.style.backgroundColor = HoverColor);
                row.RegisterCallback<MouseLeaveEvent>(_ => row.style.backgroundColor = Color.clear);
            }

            return row;
        }

        private static Label BuildMessage(string text)
        {
            var label = new Label(text);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.color = DimColor;
            return label;
        }

        private void SetStatus(string message)
        {
            if (statusLabel != null)
            {
                statusLabel.text = message;
            }
        }

        private static string BuildPath(Transform transform)
        {
            var path = transform.name;
            var parent = transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        private static string GetAssetGuid(UnityEngine.Object asset)
        {
            return asset == null
                ? string.Empty
                : AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
        }
    }
}
