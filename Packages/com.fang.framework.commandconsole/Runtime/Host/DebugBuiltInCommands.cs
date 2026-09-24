#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fang.Framework.CommandConsole
{
    /// <summary>
    /// 包内置指令。做成普通的 <c>[DebugCommand]</c> 静态方法 → 随扫描自动出现，
    /// 所以运行时 <c>list_commands</c> 与导出的文档天然一致，不用另维护一份清单。
    /// 分类固定 <c>builtin</c>。
    /// </summary>
    public static class DebugBuiltInCommands
    {
        [DebugCommand("ping", "探活：确认服务在监听、主线程在 tick、注册了多少指令", DebugCommandDefaults.BuiltInCategory)]
        public static object Ping(DebugCommandContext ctx, DebugCommandArgs args)
        {
            var service = ctx.GetService<DebugCommandService>();

            return new
            {
                pong = true,
                listening = service != null && service.IsListening,
                ticking = service != null && service.IsTicking,
                port = service != null ? service.Port : 0,
                commands = ctx.Registry != null ? ctx.Registry.Count : 0,
                unityVersion = Application.unityVersion,
            };
        }

        [DebugCommand("list_commands", "列出全部指令，可用 <filter> 按名称/分类/描述过滤", DebugCommandDefaults.BuiltInCategory)]
        public static object ListCommands(DebugCommandContext ctx, DebugCommandArgs args)
        {
            var registry = RequireRegistry(ctx);
            var filter = args.Count > 0 ? args.Get(0, "filter").Trim() : string.Empty;
            var commands = registry.Commands;

            var items = new List<object>();
            for (var i = 0; i < commands.Count; i++)
            {
                var entry = commands[i];
                if (filter.Length > 0 && !Matches(entry, filter))
                {
                    continue;
                }

                items.Add(new
                {
                    name = entry.Name,
                    category = entry.Category,
                    description = entry.Description,
                    source = entry.Source,
                });
            }

            return new
            {
                total = commands.Count,
                shown = items.Count,
                filter,
                commands = items,
            };
        }

        [DebugCommand("command_log", "最近的指令调用记录 <count>（默认 10，最多 200，最新在前）", DebugCommandDefaults.BuiltInCategory)]
        public static object CommandLog(DebugCommandContext ctx, DebugCommandArgs args)
        {
            var registry = RequireRegistry(ctx);
            var max = DebugCommandDefaults.CallLogMaxSize;
            var count = args.Count > 0 ? args.GetInt(0, "count") : 10;

            if (count < 1 || count > max)
            {
                throw new DebugCommandException($"参数 1 <count> 必须在 1~{max} 之间, 实际值: {count}");
            }

            var log = registry.GetCallLog();
            var items = new List<object>();

            for (var i = 0; i < log.Count && i < count; i++)
            {
                items.Add(new
                {
                    time = log[i].Time,
                    cmd = log[i].Cmd,
                    args = log[i].Args,
                    ok = log[i].Ok,
                    summary = log[i].Summary,
                });
            }

            return new
            {
                total = log.Count,
                shown = items.Count,
                entries = items,
            };
        }

        private static DebugCommandRegistry RequireRegistry(DebugCommandContext ctx)
        {
            var registry = ctx.Registry;

            if (registry == null)
            {
                throw new DebugCommandException("指令注册表不可用（上下文里没有注册表）");
            }

            return registry;
        }

        private static bool Matches(DebugCommandEntry entry, string filter)
        {
            return Contains(entry.Name, filter)
                || Contains(entry.Category, filter)
                || Contains(entry.Description, filter);
        }

        private static bool Contains(string value, string filter)
        {
            return !string.IsNullOrEmpty(value)
                && value.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
#endif
