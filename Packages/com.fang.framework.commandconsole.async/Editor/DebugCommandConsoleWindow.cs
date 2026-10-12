#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Fang.Framework.CommandConsole.Async.Editor
{
    /// <summary>
    /// 进程内指令控制台。有活实例时直接用运行时注册表执行；没有活实例（未进入 Play）时
    /// 退回到「编辑器侧扫描」，至少能列出与查看指令清单。
    /// </summary>
    public sealed class DebugCommandConsoleWindow : EditorWindow
    {
        private const int OutputCapacity = 400;

        private readonly List<string> _output = new List<string>();

        private DebugCommandRegistry _fallbackRegistry;
        private string _input = string.Empty;
        private Vector2 _outputScroll;
        private Vector2 _logScroll;
        private bool _showCallLog = true;

        [MenuItem("Fang/调试/指令控制台", false, 10)]
        public static void Open()
        {
            var window = GetWindow<DebugCommandConsoleWindow>();
            window.titleContent = new GUIContent("指令控制台");
            window.minSize = new Vector2(560f, 340f);
            window.Show();
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void OnGUI()
        {
            HandleSubmitKey();

            DrawStatus();
            EditorGUILayout.Space();
            DrawInput();
            DrawQuickActions();
            DrawOutput();

            if (_showCallLog)
            {
                DrawCallLog();
            }
        }

        private void OnDestroy()
        {
            _fallbackRegistry = null;
        }

        private void HandleSubmitKey()
        {
            if (Event.current.type != EventType.KeyDown)
            {
                return;
            }

            if (Event.current.keyCode != KeyCode.Return && Event.current.keyCode != KeyCode.KeypadEnter)
            {
                return;
            }

            Submit(_input);
            Event.current.Use();
        }

        private void DrawStatus()
        {
            var service = ActiveService();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (service == null)
            {
                EditorGUILayout.LabelField(
                    "状态：无活实例。进入 Play 后 AppScope 会装上指令服务；当前用编辑器侧扫描的代用注册表执行。",
                    EditorStyles.wordWrappedLabel);
            }
            else
            {
                EditorGUILayout.LabelField(
                    $"监听：{(service.IsListening ? "是" : "否")}　地址：{service.ListenAddress}:{service.Port}　" +
                    $"ticking：{(service.IsTicking ? "是" : "否")}　tick：{service.TickCount}　客户端：{service.ConnectedClients}",
                    EditorStyles.wordWrappedLabel);
            }

            EditorGUILayout.EndVertical();

            using (new EditorGUILayout.HorizontalScope())
            {
                var registry = ResolveRegistry();

                EditorGUILayout.LabelField(
                    $"指令数：{(registry != null ? registry.Count : 0)}（{(service != null ? "运行时注册表" : "编辑器侧扫描")}）",
                    GUILayout.Width(240f));

                if (GUILayout.Button("重新扫描", GUILayout.Width(84f)))
                {
                    Rescan(service);
                }

                if (GUILayout.Button("清空输出", GUILayout.Width(84f)))
                {
                    _output.Clear();
                }

                _showCallLog = GUILayout.Toggle(_showCallLog, "显示调用日志", GUILayout.Width(120f));
            }
        }

        private void DrawInput()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.SetNextControlName("Fang.Framework.CommandConsole.Async.Input");
                _input = EditorGUILayout.TextField(_input);

                if (GUILayout.Button("执行", GUILayout.Width(64f)))
                {
                    Submit(_input);
                    GUI.FocusControl("Fang.Framework.CommandConsole.Async.Input");
                }
            }

            EditorGUILayout.LabelField("提示：回车即执行；参数按空格分隔，不支持引号转义。", EditorStyles.miniLabel);
        }

        private void DrawQuickActions()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("ping"))
                {
                    Submit("ping");
                }

                if (GUILayout.Button("list_commands"))
                {
                    Submit("list_commands");
                }

                if (GUILayout.Button("command_log 20"))
                {
                    Submit("command_log 20");
                }
            }
        }

        private void DrawOutput()
        {
            EditorGUILayout.LabelField($"输出（{_output.Count}）", EditorStyles.boldLabel);

            _outputScroll = EditorGUILayout.BeginScrollView(_outputScroll, EditorStyles.helpBox, GUILayout.MinHeight(140f));

            for (var i = 0; i < _output.Count; i++)
            {
                EditorGUILayout.LabelField(_output[i], EditorStyles.wordWrappedLabel);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawCallLog()
        {
            var registry = ResolveRegistry();
            var log = registry != null ? registry.GetCallLog() : new List<DebugCommandCallRecord>();

            EditorGUILayout.LabelField($"调用日志（{log.Count}，最新在前）", EditorStyles.boldLabel);

            _logScroll = EditorGUILayout.BeginScrollView(_logScroll, EditorStyles.helpBox, GUILayout.MinHeight(80f));

            for (var i = 0; i < log.Count; i++)
            {
                EditorGUILayout.LabelField(log[i].ToString(), EditorStyles.wordWrappedLabel);
            }

            EditorGUILayout.EndScrollView();
        }

        private void Submit(string line)
        {
            var trimmed = line?.Trim();

            if (string.IsNullOrEmpty(trimmed))
            {
                return;
            }

            var service = ActiveService();
            var registry = service != null && service.Registry != null ? service.Registry : ResolveRegistry();

            if (registry == null)
            {
                Append("[错误] 指令注册表不可用。");
                return;
            }

            Append($"> {trimmed}");

            try
            {
                Append(registry.ExecuteText(trimmed));
            }
            catch (System.Exception exception)
            {
                Append($"[错误] {exception.Message}");
            }

            _input = string.Empty;
            _outputScroll = new Vector2(0f, float.MaxValue);
        }

        private void Rescan(DebugCommandService service)
        {
            if (service != null && service.Registry != null)
            {
                service.Rescan();
            }
            else
            {
                RescanFallback();
            }

            Append("[编辑器] 已重新扫描程序集。");
        }

        private DebugCommandRegistry ResolveRegistry()
        {
            var service = ActiveService();

            if (service != null && service.Registry != null)
            {
                return service.Registry;
            }

            if (_fallbackRegistry == null)
            {
                RescanFallback();
            }

            return _fallbackRegistry;
        }

        private void RescanFallback()
        {
            var registry = new DebugCommandRegistry();

            // 没有活 Scope，因此查找起点为 null：GetService<T>() 只会返回 null，不会抛。
            registry.ContextFactory = () => new DebugCommandContext(registry, () => null);
            registry.ScanLoadedAssemblies();

            _fallbackRegistry = registry;
        }

        private void Append(string text)
        {
            _output.Add(text);

            while (_output.Count > OutputCapacity)
            {
                _output.RemoveAt(0);
            }
        }

        private static DebugCommandService ActiveService()
        {
            var services = DebugCommandService.ActiveServices;
            return services.Count > 0 ? services[0] : null;
        }
    }
}
#endif
