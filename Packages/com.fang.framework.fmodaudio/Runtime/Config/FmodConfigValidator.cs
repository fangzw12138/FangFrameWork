using System;
using System.Collections.Generic;

namespace Fang.Framework.FmodAudio
{
    internal static class FmodConfigValidator
    {
        private const string BusPathPrefix = "bus:/";

        public static bool TryValidate(FmodAudioConfigSo config, out string error)
        {
            error = null;

            if (config == null)
            {
                error = "配置为空。";
                return false;
            }

            var channels = config.BusChannels;
            if (channels == null)
            {
                error = "通道列表为空引用。";
                return false;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);

            for (var i = 0; i < channels.Count; i++)
            {
                var channel = channels[i];
                if (channel == null)
                {
                    error = "第 " + i + " 条通道是空引用。";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(channel.ChannelId))
                {
                    error = "第 " + i + " 条通道的 channelId 为空。";
                    return false;
                }

                if (!seen.Add(channel.ChannelId))
                {
                    error = "channelId 重复：" + channel.ChannelId + "。";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(channel.BusPath))
                {
                    error = "通道「" + channel.ChannelId + "」的 busPath 为空。";
                    return false;
                }

                if (!channel.BusPath.StartsWith(BusPathPrefix, StringComparison.Ordinal))
                {
                    error = "通道「" + channel.ChannelId + "」的 busPath 必须以 bus:/ 开头：" + channel.BusPath;
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(config.MasterBusPath))
            {
                error = "主总线路径为空。";
                return false;
            }

            if (!config.MasterBusPath.StartsWith(BusPathPrefix, StringComparison.Ordinal))
            {
                error = "主总线路径必须以 bus:/ 开头：" + config.MasterBusPath;
                return false;
            }

            return true;
        }
    }
}
