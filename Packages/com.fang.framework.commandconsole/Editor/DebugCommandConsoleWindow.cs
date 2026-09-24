using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Fang.Framework.CommandConsole.Editor
{
    /// <summary>
    /// 指令控制台：游戏运行时<b>进程内</b>执行指令并显示 JSON 结果（不经 TCP），
    /// 外加调用日志与状态行。覆盖「手动调试」场景；未运行 / 服务未就绪时给出明确提示。
    /// </summary>
    public sealed class DebugCommandConsoleWindow : EditorWindow
    {
        /// <summary>菜单路径（FangHub 页也会引用它）。</summary>
        public const string MenuPath = "Tools/Fang Framework/指令/打开控制台";

        private const int MaxHistory = 20;
        private const string InputControlName = "DebugCommandConsoleWindowInput";

        private string _input = string.Empty;
        private string _output = string.Empty;
        private readonly List<string> _history = new List<string>();
        private int _historySelection = -1;
        private Vector2 _outputScroll;
        private Vector2 _logScroll;
        private bool _showLog = true;

        [MenuItem(MenuPath)]
        public static void Open()
        {
            GetWindow<DebugCommandConsoleWindow>("指令控制台");
        }

        private void OnGUI()
        {
            var service = ResolveService();

            DrawStatus(service);
            DrawInputRow(service);
            DrawQuickButtons(service);
            DrawOutput();
            DrawCallLog(service);
        }

        private static DebugCommandService ResolveService()
        {
            var live = DebugCommandService.ActiveServices;
            return live.Count > 0 ? live[0] : null;
        }

        private void DrawStatus(DebugCommandService service)
        {
            EditorGUILayout.Space(4);

            if (service == null)
            {
                EditorGUILayout.HelpBox(
                    "游戏未运行或指令服务未就绪（需要进入 PlayMode，且编译为 Editor / Development Build）。",
                    MessageType.Info);
                return;
            }

            var state = service.IsListening
                ? "监听 " + service.ListenAddress + ":" + service.Port
                : "未监听（AutoStart 关掉了？）";

            var ticking = service.IsTicking
                ? "tick 正常"
                : "⚠ 没有 tick（没人驱动 Scope.Tick，TCP 不会响应）";

            EditorGUILayout.LabelField(
                "服务：" + state + "  ·  " + ticking + "  ·  " + service.Registry.Count + " 条指令",
                EditorStyles.miniLabel);
        }

        private void DrawInputRow(DebugCommandService service)
        {
            EditorGUILayout.LabelField("指令行：指令名 + 空格分隔参数（示例：ping / list_commands / command_log 5）");

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.SetNextControlName(InputControlName);
                _input = EditorGUILayout.TextField(_input);

                var execute = GUILayout.Button("执行", GUILayout.Width(60));

                var current = Event.current;
                var enterPressed = current != null
                    && current.type == EventType.KeyDown
                    && (current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter)
                    && GUI.GetNameOfFocusedControl() == InputControlName;

                if (enterPressed)
                {
                    current.Use();
                }

                if (execute || enterPressed)
                {
                    Execute(service, _input);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawHistoryPopup();

                if (GUILayout.Button("清空结果", GUILayout.Width(80)))
                {
                    _output = string.Empty;
                }
            }
        }

        private void DrawHistoryPopup()
        {
            if (_history.Count == 0)
            {
                return;
            }

            var label = _historySelection >= 0 && _historySelection < _history.Count
                ? _history[_historySelection]
                : "历史指令…";

            var selected = EditorGUILayout.Popup(label, _historySelection, _history.ToArray());
            if (selected == _historySelection)
            {
                return;
            }

            _historySelection = selected;

            if (selected >= 0 && selected < _history.Count)
            {
                _input = _history[selected];
            }
        }

        private void DrawQuickButtons(DebugCommandService service)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("ping", GUILayout.Width(60)))
                {
                    Execute(service, "ping");
                }

                if (GUILayout.Button("list_commands", GUILayout.Width(100)))
                {
                    Execute(service, "list_commands");
                }

                if (GUILayout.Button("command_log", GUILayout.Width(100)))
                {
                    Execute(service, "command_log");
                }
            }
        }

        private void DrawOutput()
        {
            EditorGUILayout.Space(4);

            _outputScroll = EditorGUILayout.BeginScrollView(_outputScroll, GUILayout.Height(200));
            EditorGUILayout.TextArea(_output, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        /// <summary>调用日志区：TCP 的调用与手动调用在同一个列表里（最新在上）。</summary>
        private void DrawCallLog(DebugCommandService service)
        {
            EditorGUILayout.Space(6);
            _showLog = EditorGUILayout.Foldout(
                _showLog,
                "调用日志（最新在上，最多 " + DebugCommandDefaults.CallLogMaxSize + " 条）",
                true);

            if (!_showLog)
            {
                return;
            }

            var registry = service?.Registry;
            if (registry == null)
            {
                EditorGUILayout.HelpBox("指令服务未就绪，没有调用记录。", MessageType.Info);
                return;
            }

            var log = registry.GetCallLog();
            _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.ExpandHeight(true));

            if (log.Count == 0)
            {
                EditorGUILayout.LabelField("暂无调用记录");
            }
            else
            {
                for (var i = 0; i < log.Count; i++)
                {
                    var entry = log[i];
                    var mark = entry.Ok ? "ok " : "ERR";
                    var args = entry.Args != null && entry.Args.Length > 0
                        ? " " + string.Join(" ", entry.Args)
                        : string.Empty;

                    EditorGUILayout.LabelField(
                        "[" + entry.Time + "] " + mark + " " + entry.Cmd + args,
                        entry.Ok ? EditorStyles.miniLabel : EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("    " + entry.Summary, EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void Execute(DebugCommandService service, string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine))
            {
                return;
            }

            if (service == null)
            {
                _output = "游戏未运行或指令服务未就绪（需要进入 PlayMode）。";
                return;
            }

            var line = commandLine.Trim();

            if (_history.Count == 0 || _history[_history.Count - 1] != line)
            {
                _history.Add(line);
                if (_history.Count > MaxHistory)
                {
                    _history.RemoveAt(0);
                }
            }

            _historySelection = _history.Count - 1;

            _output = "> " + line + "\n" + service.ExecuteText(line);
            _input = string.Empty;
            _outputScroll = Vector2.zero;

            Repaint();
        }
    }
}
