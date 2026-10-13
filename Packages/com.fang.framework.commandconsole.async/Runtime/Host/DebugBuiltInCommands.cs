#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fang.Framework.CommandConsole.Async
{
    /// <summary>
    /// 内置指令（住在本包程序集里，因此「引用了本包的程序集」这一扫描范围天然覆盖它）。
    /// </summary>
    internal static class DebugBuiltInCommands
    {
        [DebugCommand("ping", "连通性与运行状态自检", DebugCommandDefaults.BuiltInCategory)]
        public static object Ping(DebugCommandContext ctx, DebugCommandArgs args)
        {
            var service = ctx.GetService<DebugCommandService>();
            var registry = ctx.Registry;

            return new
            {
                pong = true,
                listening = service != null && service.IsListening,
                ticking = service != null && service.IsTicking,
                port = service != null ? service.Port : 0,
                address = service != null ? service.ListenAddress : null,
                clients = service != null ? service.ConnectedClients : 0,
                tickCount = service != null ? service.TickCount : 0,
                commands = registry != null ? registry.Count : 0,
                unityVersion = Application.unityVersion
            };
        }

        [DebugCommand("list_commands", "列出已注册指令（可选按关键字过滤名称/分类/说明）", DebugCommandDefaults.BuiltInCategory)]
        public static object ListCommands(DebugCommandContext ctx, DebugCommandArgs args)
        {
            var registry = ctx.Registry;

            if (registry == null)
            {
                throw new DebugCommandException("指令注册表不可用");
            }

            var filter = args.Count > 0 ? args.Get(0, "filter") : null;
            var all = registry.Commands;
            var matched = new List<object>();

            for (var i = 0; i < all.Count; i++)
            {
                var entry = all[i];

                if (!Matches(entry, filter))
                {
                    continue;
                }

                matched.Add(new
                {
                    name = entry.Name,
                    category = entry.Category,
                    description = entry.Description,
                    source = entry.Source
                });
            }

            return new
            {
                total = all.Count,
                shown = matched.Count,
                filter = filter,
                commands = matched
            };
        }

        [DebugCommand("command_log", "查看最近调用日志（默认 10 条，上限 200）", DebugCommandDefaults.BuiltInCategory)]
        public static object CommandLog(DebugCommandContext ctx, DebugCommandArgs args)
        {
            var registry = ctx.Registry;

            if (registry == null)
            {
                throw new DebugCommandException("指令注册表不可用");
            }

            var count = args.Count > 0 ? args.GetInt(0, "count") : 10;

            if (count < 1 || count > DebugCommandRegistry.CallLogCapacity)
            {
                throw new DebugCommandException(
                    $"参数 count 越界：实际值 {count}，允许范围 1~{DebugCommandRegistry.CallLogCapacity}。");
            }

            var log = registry.GetCallLog();
            var taken = Math.Min(count, log.Count);
            var entries = new List<object>(taken);

            for (var i = 0; i < taken; i++)
            {
                var record = log[i];

                entries.Add(new
                {
                    sequence = record.Sequence,
                    time = record.Timestamp.ToString("HH:mm:ss.fff"),
                    command = record.Command,
                    args = record.ArgsText,
                    ok = record.Succeeded,
                    error = record.Error,
                    elapsedMs = Math.Round(record.ElapsedMs, 3),
                    result = record.ResultPreview
                });
            }

            return new
            {
                total = log.Count,
                count = taken,
                entries = entries
            };
        }

        private static bool Matches(DebugCommandEntry entry, string filter)
        {
            if (string.IsNullOrEmpty(filter))
            {
                return true;
            }

            return Contains(entry.Name, filter) ||
                   Contains(entry.Category, filter) ||
                   Contains(entry.Description, filter);
        }

        private static bool Contains(string value, string filter)
        {
            return !string.IsNullOrEmpty(value) && value.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
#endif
