#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Fang.Framework.CommandConsole.Async
{
    /// <summary>
    /// 指令的位置参数（已按空白切分，不做引号转义）。
    /// 取值不合法一律抛 <see cref="DebugCommandException"/>，消息里带参数序号、参数名与实际值。
    /// </summary>
    public sealed class DebugCommandArgs
    {
        /// <summary>空参数表，供无参指令复用。</summary>
        public static readonly DebugCommandArgs Empty = new DebugCommandArgs(Array.Empty<string>());

        private readonly string[] _values;

        public DebugCommandArgs(string[] values)
        {
            _values = values ?? Array.Empty<string>();
        }

        /// <summary>参数个数。</summary>
        public int Count => _values.Length;

        /// <summary>原始参数副本，用于日志。</summary>
        public IReadOnlyList<string> Values => _values;

        /// <summary>按位置取原始字符串；越界抛 <see cref="DebugCommandException"/>。</summary>
        public string Get(int index, string name)
        {
            if (index < 0 || index >= _values.Length)
            {
                throw new DebugCommandException(
                    $"参数缺失：{name}（位置 {index}），实际只收到 {_values.Length} 个参数{DescribeValues()}。");
            }

            return _values[index];
        }

        /// <summary>按位置取原始字符串；越界时返回 <paramref name="fallback"/>（不抛）。</summary>
        public string GetOrDefault(int index, string name, string fallback)
        {
            return index >= 0 && index < _values.Length ? _values[index] : fallback;
        }

        /// <summary>尝试按位置取原始字符串；越界返回 false（不抛）。</summary>
        public bool TryGet(int index, out string value)
        {
            if (index >= 0 && index < _values.Length)
            {
                value = _values[index];
                return true;
            }

            value = null;
            return false;
        }

        /// <summary>按位置取整数；解析失败抛 <see cref="DebugCommandException"/>。</summary>
        public int GetInt(int index, string name)
        {
            var raw = Get(index, name);

            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            throw new DebugCommandException(
                $"参数格式错误：{name}（位置 {index}）实际值 \"{raw}\" 无法解析为整数。");
        }

        /// <summary>按位置取单精度浮点；解析失败抛 <see cref="DebugCommandException"/>。</summary>
        public float GetFloat(int index, string name)
        {
            var raw = Get(index, name);

            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            throw new DebugCommandException(
                $"参数格式错误：{name}（位置 {index}）实际值 \"{raw}\" 无法解析为浮点数。");
        }

        /// <summary>按位置取布尔（true/false/1/0/on/off/yes/no）；解析失败抛 <see cref="DebugCommandException"/>。</summary>
        public bool GetBool(int index, string name)
        {
            var raw = Get(index, name);

            switch (raw.Trim().ToLowerInvariant())
            {
                case "true":
                case "1":
                case "on":
                case "yes":
                    return true;
                case "false":
                case "0":
                case "off":
                case "no":
                    return false;
                default:
                    throw new DebugCommandException(
                        $"参数格式错误：{name}（位置 {index}）实际值 \"{raw}\" 无法解析为布尔值（可用：true/false/1/0/on/off/yes/no）。");
            }
        }

        /// <summary>以单个空格连接全部参数，用于调用日志。</summary>
        public string ToLine()
        {
            return _values.Length == 0 ? string.Empty : string.Join(" ", _values);
        }

        public override string ToString()
        {
            return ToLine();
        }

        private string DescribeValues()
        {
            return _values.Length == 0 ? "（无）" : $"（{string.Join(", ", _values)}）";
        }
    }
}
#endif
