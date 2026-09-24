#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;

namespace Fang.Framework.CommandConsole
{
    /// <summary>
    /// 游戏内调试控制台（IMGUI）：快捷键开关 + 输入行 + 快捷按钮 + 输出区。
    ///
    /// 由 <see cref="DebugCommandService"/> 建在自己的子物体上并驱动；直接执行（不经 TCP）。
    /// 换 Input System 时继承本类覆写 <see cref="IsTogglePressed"/> 即可 —— 包内不写死输入方案。
    /// </summary>
    public class DebugCommandConsole : MonoBehaviour
    {
        private const string InputControlName = "DebugCommandConsoleInput";

        private const float BaseWidth = 560f;
        private const float BaseHeight = 420f;
        private const float BaseMargin = 16f;

        private readonly List<string> _output = new List<string>();
        private readonly List<DebugQuickButton> _quickButtons = new List<DebugQuickButton>();

        private DebugCommandService _service;
        private KeyCode _toggleKey = DebugCommandDefaults.ConsoleToggleKey;
        private bool _visible;
        private string _input = string.Empty;
        private Vector2 _scroll;

        /// <summary>是否正在显示（快捷键切换的就是它）。</summary>
        public bool IsVisible => _visible;

        /// <summary>当前生效的开关快捷键。</summary>
        public KeyCode ToggleKey => _toggleKey;

        /// <summary>输出区内容（最新在最后）。公开出来是为了测试与外部脚本能直接读。</summary>
        public IReadOnlyList<string> Output => _output;

        /// <summary>当前快捷按钮（内置三个在前，配置的在后面）。</summary>
        public IReadOnlyList<DebugQuickButton> QuickButtons => _quickButtons;

        public void Initialize(DebugCommandService service)
        {
            _service = service;
        }

        /// <summary>套用配置：快捷键 + 快捷按钮（内置 ping / list_commands / command_log 始终保留）。</summary>
        public void Apply(KeyCode toggleKey, IReadOnlyList<DebugQuickButton> quickButtons)
        {
            _toggleKey = toggleKey;

            _quickButtons.Clear();
            _quickButtons.Add(new DebugQuickButton("ping", "ping"));
            _quickButtons.Add(new DebugQuickButton("list_commands", "list_commands"));
            _quickButtons.Add(new DebugQuickButton("command_log", "command_log"));

            if (quickButtons == null)
            {
                return;
            }

            for (var i = 0; i < quickButtons.Count; i++)
            {
                var button = quickButtons[i];
                if (button != null && !string.IsNullOrWhiteSpace(button.CommandLine))
                {
                    _quickButtons.Add(button);
                }
            }
        }

        /// <summary>运行时再加一个快捷按钮（扩展包 / 业务侧用）。</summary>
        public void RegisterQuickButton(string label, string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine))
            {
                return;
            }

            _quickButtons.Add(new DebugQuickButton(label, commandLine));
        }

        /// <summary>关掉时不显示、也不再收快捷键（<c>enabled = false</c> 让 OnGUI 根本不被调用）。</summary>
        public void SetEnabled(bool value)
        {
            enabled = value;

            if (!value)
            {
                _visible = false;
            }
        }

        public void Toggle()
        {
            _visible = !_visible;

            if (_visible)
            {
                _scroll.y = float.MaxValue;
            }
        }

        /// <summary>执行一行指令并把「输入 + 结果」写进输出区（快捷按钮、测试、外部脚本都走它）。</summary>
        public void Execute(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine))
            {
                return;
            }

            var line = commandLine.Trim();
            AddOutput("> " + line);
            AddOutput(_service != null ? _service.ExecuteText(line) : "指令服务不可用。");
        }

        public void ClearOutput()
        {
            _output.Clear();
        }

        /// <summary>
        /// 这一帧是否按下了开关键。默认读 IMGUI 事件（不占用 <c>Update</c>）；
        /// 换 Input System 就覆写它，例如 <c>Keyboard.current[Key.Backquote].wasPressedThisFrame</c>。
        /// </summary>
        protected virtual bool IsTogglePressed()
        {
            var current = Event.current;
            return current != null
                && current.type == EventType.KeyDown
                && current.keyCode == _toggleKey;
        }

        private void OnGUI()
        {
            // 即使没显示也要跑 OnGUI，否则收不到开关键。
            if (IsTogglePressed())
            {
                Toggle();
            }

            if (!_visible)
            {
                return;
            }

            Draw();
        }

        private void Draw()
        {
            var scale = Mathf.Max(1f, Screen.height / 720f);
            var width = BaseWidth * scale;
            var height = BaseHeight * scale;
            var area = new Rect(
                BaseMargin * scale,
                Screen.height - height - BaseMargin * scale,
                width,
                height);

            GUILayout.BeginArea(area, "调试控制台 (" + _toggleKey + ")", GUI.skin.window);

            DrawInputRow(scale);
            DrawQuickButtonRows(width, scale);
            DrawOutput(scale);

            GUILayout.EndArea();
        }

        private void DrawInputRow(float scale)
        {
            GUILayout.BeginHorizontal();

            GUI.SetNextControlName(InputControlName);
            _input = GUILayout.TextField(_input, GUILayout.ExpandWidth(true));

            var submitted = GUILayout.Button("执行", GUILayout.Width(56f * scale));

            var current = Event.current;
            var enterPressed = current != null
                && current.type == EventType.KeyDown
                && (current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter)
                && GUI.GetNameOfFocusedControl() == InputControlName;

            if (enterPressed)
            {
                current.Use();
            }

            if (submitted || enterPressed)
            {
                Execute(_input);
                _input = string.Empty;
            }

            if (GUILayout.Button("清空", GUILayout.Width(56f * scale)))
            {
                ClearOutput();
            }

            GUILayout.EndHorizontal();
        }

        /// <summary>快捷按钮按可用宽度自动换行（按钮多了不会溢出窗口）。</summary>
        private void DrawQuickButtonRows(float width, float scale)
        {
            if (_quickButtons.Count == 0)
            {
                return;
            }

            var available = width - BaseMargin * scale;
            var used = 0f;
            var rowOpen = false;

            for (var i = 0; i < _quickButtons.Count; i++)
            {
                var button = _quickButtons[i];
                var label = button.Label ?? string.Empty;
                var buttonWidth = Mathf.Clamp(label.Length * 9f * scale + 16f * scale, 48f * scale, 180f * scale);

                if (!rowOpen || (used > 0f && used + buttonWidth > available))
                {
                    if (rowOpen)
                    {
                        GUILayout.EndHorizontal();
                    }

                    GUILayout.BeginHorizontal();
                    rowOpen = true;
                    used = 0f;
                }

                if (GUILayout.Button(label, GUILayout.Width(buttonWidth)))
                {
                    Execute(button.CommandLine);
                    _input = string.Empty;
                }

                used += buttonWidth;
            }

            if (rowOpen)
            {
                GUILayout.EndHorizontal();
            }
        }

        private void DrawOutput(float scale)
        {
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));

            var previousSize = GUI.skin.label.fontSize;
            GUI.skin.label.fontSize = Mathf.RoundToInt(13f * scale);

            for (var i = 0; i < _output.Count; i++)
            {
                GUILayout.Label(_output[i]);
            }

            GUI.skin.label.fontSize = previousSize;
            GUILayout.EndScrollView();
        }

        private void AddOutput(string text)
        {
            _output.Add(text ?? string.Empty);

            while (_output.Count > DebugCommandDefaults.ConsoleMaxOutputLines)
            {
                _output.RemoveAt(0);
            }

            _scroll.y = float.MaxValue;
        }
    }
}
#endif
