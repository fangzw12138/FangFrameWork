#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;

namespace Fang.Framework.CommandConsole.Demo
{
    /// <summary>
    /// 示例指令：五个分类，覆盖「读状态 / 改状态 / 参数解析 / 参数校验失败 / 主动抛错 / 找不到服务」这些路径。
    ///
    /// 它们住在**示例程序集**里（该程序集引用了本包），所以默认扫描范围（只扫引用了本包的程序集）就能扫到 ——
    /// 不需要额外配置。
    /// </summary>
    public static class DemoCommands
    {
        // ── session：App 层的服务，与指令服务同层（直接命中）──────────────────

        [DebugCommand("session_state", "读会话状态：名称 / tick 次数 / 累计时间 / 当前 timeScale", "session")]
        public static object SessionState(DebugCommandContext ctx, DebugCommandArgs args)
        {
            var session = RequireSession(ctx);

            return new
            {
                name = session.Name,
                tickCount = session.TickCount,
                elapsedSeconds = Math.Round(session.ElapsedSeconds, 2),
                timeScale = session.TimeScale,
            };
        }

        [DebugCommand("session_rename", "改会话名称 <name>（顺便验证中文参数与返回值编码）", "session")]
        public static object SessionRename(DebugCommandContext ctx, DebugCommandArgs args)
        {
            var session = RequireSession(ctx);
            var name = args.Get(0, "name");

            session.Rename(name);

            return new { name = session.Name, length = session.Name.Length };
        }

        [DebugCommand("session_timescale", "设置 Time.timeScale <value>（0~10，越界报错）", "session")]
        public static object SessionTimeScale(DebugCommandContext ctx, DebugCommandArgs args)
        {
            var session = RequireSession(ctx);
            var value = args.GetFloat(0, "value");

            if (value < 0f || value > 10f)
            {
                throw new DebugCommandException($"参数 1 <value> 必须在 0~10 之间, 实际值: {value}");
            }

            session.SetTimeScale(value);

            return new { timeScale = session.TimeScale };
        }

        // ── player：Scene 层的服务，靠向下兜底命中 ──────────────────────────

        [DebugCommand("player_state", "读玩家状态：血量 / 等级 / 坐标", "player")]
        public static object PlayerState(DebugCommandContext ctx, DebugCommandArgs args)
        {
            return Snapshot(RequirePlayer(ctx));
        }

        [DebugCommand("player_sethp", "设置血量 <value>（0~100，越界报错）", "player")]
        public static object PlayerSetHp(DebugCommandContext ctx, DebugCommandArgs args)
        {
            var player = RequirePlayer(ctx);
            var value = args.GetInt(0, "value");

            if (value < 0 || value > DemoPlayerService.MaxHp)
            {
                throw new DebugCommandException(
                    $"参数 1 <value> 必须在 0~{DemoPlayerService.MaxHp} 之间, 实际值: {value}");
            }

            player.SetHp(value);

            return Snapshot(player);
        }

        [DebugCommand("player_damage", "扣血 <amount>", "player")]
        public static object PlayerDamage(DebugCommandContext ctx, DebugCommandArgs args)
        {
            var player = RequirePlayer(ctx);
            var amount = args.GetInt(0, "amount");

            if (amount < 0)
            {
                throw new DebugCommandException($"参数 1 <amount> 不能是负数, 实际值: {amount}");
            }

            player.Damage(amount);

            return Snapshot(player);
        }

        [DebugCommand("player_heal", "回血 <amount>", "player")]
        public static object PlayerHeal(DebugCommandContext ctx, DebugCommandArgs args)
        {
            var player = RequirePlayer(ctx);
            var amount = args.GetInt(0, "amount");

            if (amount < 0)
            {
                throw new DebugCommandException($"参数 1 <amount> 不能是负数, 实际值: {amount}");
            }

            player.Heal(amount);

            return Snapshot(player);
        }

        [DebugCommand("player_level", "设置等级 <value>（>= 1）", "player")]
        public static object PlayerLevel(DebugCommandContext ctx, DebugCommandArgs args)
        {
            var player = RequirePlayer(ctx);
            var value = args.GetInt(0, "value");

            if (value < 1)
            {
                throw new DebugCommandException($"参数 1 <value> 必须 >= 1, 实际值: {value}");
            }

            player.SetLevel(value);

            return Snapshot(player);
        }

        [DebugCommand("player_teleport", "传送 <x,y,z>（网格坐标，例如 1,0,3）", "player")]
        public static object PlayerTeleport(DebugCommandContext ctx, DebugCommandArgs args)
        {
            var player = RequirePlayer(ctx);
            var cell = args.GetVector3Int(0, "x,y,z");

            player.Teleport(new Vector3(cell.x, cell.y, cell.z));

            return Snapshot(player);
        }

        // ── world：Scene 层的服务，真的建 / 删 GameObject（靠向下兜底命中）────

        [DebugCommand("world_spawn", "生成 <count> 个箱子（1~50；在主线程建 GameObject）", "world")]
        public static object WorldSpawn(DebugCommandContext ctx, DebugCommandArgs args)
        {
            var spawner = RequireSpawner(ctx);
            var count = args.GetInt(0, "count");

            if (count < 1 || count > 50)
            {
                throw new DebugCommandException($"参数 1 <count> 必须在 1~50 之间, 实际值: {count}");
            }

            spawner.Spawn(count);

            return new
            {
                spawned = count,
                total = spawner.Count,
                lastPosition = Flat(spawner.LastPosition),
            };
        }

        [DebugCommand("world_clear", "清掉全部箱子", "world")]
        public static object WorldClear(DebugCommandContext ctx, DebugCommandArgs args)
        {
            var spawner = RequireSpawner(ctx);
            var removed = spawner.Count;

            spawner.Clear();

            return new { removed, total = spawner.Count };
        }

        [DebugCommand("world_count", "箱子数量与最后一个箱子的位置", "world")]
        public static object WorldCount(DebugCommandContext ctx, DebugCommandArgs args)
        {
            var spawner = RequireSpawner(ctx);

            return new { total = spawner.Count, lastPosition = Flat(spawner.LastPosition) };
        }

        // ── demo：刻意走失败 / 边界路径 ─────────────────────────────────────

        [DebugCommand("demo_echo", "原样回显 <text>（验证字符串参数与中文）", "demo")]
        public static object DemoEcho(DebugCommandContext ctx, DebugCommandArgs args)
        {
            var text = args.Get(0, "text");

            return new { text, length = text.Length };
        }

        [DebugCommand("demo_throw", "刻意抛 DebugCommandException，验证错误回包格式", "demo")]
        public static object DemoThrow(DebugCommandContext ctx, DebugCommandArgs args)
        {
            throw new DebugCommandException("这是 demo_throw 刻意抛出的示例异常");
        }

        [DebugCommand("demo_bad_int", "把 <value> 当整数读，传非数字会报参数错误", "demo")]
        public static object DemoBadInt(DebugCommandContext ctx, DebugCommandArgs args)
        {
            var value = args.GetInt(0, "value");

            return new { value };
        }

        [DebugCommand("demo_vector3_pitfall", "已知坑复现：返回值里直接放 UnityEngine.Vector3 会失败", "demo")]
        public static object DemoVector3Pitfall(DebugCommandContext ctx, DebugCommandArgs args)
        {
            // 这条指令是**故意**写错的，用来复现一个真实坑：
            // 返回值里直接放 Vector3 → Newtonsoft 默认的 ReferenceLoopHandling.Error 把 Vector3.normalized
            // 判成自引用 → 指令以 "Self referencing loop detected for property 'normalized'" 失败。
            // 正确做法是摊平成 { x, y, z }（本文件其它指令都走 Flat(...)），或在包侧换成
            // ReferenceLoopHandling.Ignore / 注册一个 Vector3 转换器。
            return new { position = new Vector3(1f, 2f, 3f) };
        }

        [DebugCommand("demo_unregistered_service", "演示找不到服务时返回 false 而不是抛异常", "demo")]
        public static object DemoUnregisteredServiceProbe(DebugCommandContext ctx, DebugCommandArgs args)
        {
            var found = ctx.TryGetService<DemoUnregisteredService>(out var service);

            return new
            {
                found,
                serviceType = service != null ? service.GetType().Name : null,
                note = "DemoUnregisteredService 刻意没有注册：向上、向下都找不到服务时返回 false，而不是抛异常。",
            };
        }

        // ── 小工具 ─────────────────────────────────────────────────────────

        private static object Snapshot(DemoPlayerService player)
        {
            return new
            {
                hp = player.Hp,
                maxHp = DemoPlayerService.MaxHp,
                level = player.Level,
                position = Flat(player.Position),
            };
        }

        /// <summary>
        /// 把 <see cref="Vector3"/> 摊平成普通字段再返回。
        ///
        /// 返回值里**不能直接放 Vector3**：Newtonsoft 的默认 <c>ReferenceLoopHandling.Error</c> 会把
        /// <c>Vector3.normalized</c> 这条链判成自引用，整个指令以
        /// <c>JsonSerializationException: Self referencing loop detected for property 'normalized'</c> 失败
        /// （<c>demo_vector3_pitfall</c> 就是这条坑的可执行复现）。
        /// </summary>
        private static object Flat(Vector3 value)
        {
            return new { x = value.x, y = value.y, z = value.z };
        }

        private static DemoSessionService RequireSession(DebugCommandContext ctx)
        {
            var session = ctx.GetService<DemoSessionService>();

            if (session == null)
            {
                throw new DebugCommandException("当前没有游戏会话（DemoSessionService 没有注册）");
            }

            return session;
        }

        private static DemoPlayerService RequirePlayer(DebugCommandContext ctx)
        {
            var player = ctx.GetService<DemoPlayerService>();

            if (player == null)
            {
                throw new DebugCommandException("当前没有玩家（DemoPlayerService 没有注册）");
            }

            return player;
        }

        private static DemoSpawnService RequireSpawner(DebugCommandContext ctx)
        {
            var spawner = ctx.GetService<DemoSpawnService>();

            if (spawner == null)
            {
                throw new DebugCommandException("当前没有场景（DemoSpawnService 没有注册）");
            }

            return spawner;
        }
    }
}
#endif
