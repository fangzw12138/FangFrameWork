#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;

namespace Fang.Framework.CommandConsole
{
    /// <summary>
    /// 指令参数包装：按索引读取 + 类型转换，失败抛 <see cref="DebugCommandException"/>。
    /// 错误文案统一带「第 n 个参数 &lt;名&gt;」与「实际值」，方便调用方（尤其 AI）自己纠正调用。
    /// </summary>
    public sealed class DebugCommandArgs
    {
        private readonly string[] _args;

        public DebugCommandArgs(string[] args)
        {
            _args = args ?? Array.Empty<string>();
        }

        public int Count => _args.Length;

        public string Get(int index, string argName)
        {
            if (index < 0 || index >= _args.Length)
            {
                throw new DebugCommandException($"缺少参数 {index + 1} <{argName}>");
            }

            return _args[index];
        }

        public int GetInt(int index, string argName)
        {
            var raw = Get(index, argName);
            if (!int.TryParse(raw, out var value))
            {
                throw new DebugCommandException($"参数 {index + 1} <{argName}> 必须是整数, 实际值: '{raw}'");
            }

            return value;
        }

        public float GetFloat(int index, string argName)
        {
            var raw = Get(index, argName);
            if (!float.TryParse(raw, out var value))
            {
                throw new DebugCommandException($"参数 {index + 1} <{argName}> 必须是数字, 实际值: '{raw}'");
            }

            return value;
        }

        public bool GetBool(int index, string argName)
        {
            var raw = Get(index, argName).Trim();
            if (bool.TryParse(raw, out var value))
            {
                return value;
            }

            if (raw == "1")
            {
                return true;
            }

            if (raw == "0")
            {
                return false;
            }

            throw new DebugCommandException($"参数 {index + 1} <{argName}> 必须是 true/false/1/0, 实际值: '{raw}'");
        }

        /// <summary>网格坐标：<c>"x,y,z"</c> 格式。</summary>
        public Vector3Int GetVector3Int(int index, string argName)
        {
            var raw = Get(index, argName);
            var parts = raw.Split(',');

            if (parts.Length != 3
                || !int.TryParse(parts[0], out var x)
                || !int.TryParse(parts[1], out var y)
                || !int.TryParse(parts[2], out var z))
            {
                throw new DebugCommandException($"参数 {index + 1} <{argName}> 必须是 'x,y,z' 网格坐标, 实际值: '{raw}'");
            }

            return new Vector3Int(x, y, z);
        }
    }
}
#endif
