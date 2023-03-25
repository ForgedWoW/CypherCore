// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Maps.Workers;
using Forged.MapServer.Networking.Packets.Channel;
using Forged.MapServer.Text;
using Framework.Constants;

namespace Forged.MapServer.Chat.Channels;

class ChannelNotifyJoinedBuilder : MessageBuilder
{
	readonly Channel _source;

	public ChannelNotifyJoinedBuilder(Channel source)
	{
		_source = source;
	}

	public override PacketSenderOwning<ChannelNotifyJoined> Invoke(Locale locale = Locale.enUS)
	{
		var localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

		PacketSenderOwning<ChannelNotifyJoined> notify = new()
        {
            Data =
            {
                //notify.ChannelWelcomeMsg = "";
                ChatChannelID = (int)_source.GetChannelId(),
                //notify.InstanceID = 0;
                ChannelFlags = _source.GetFlags(),
                Channel = _source.GetName(localeIdx),
                ChannelGUID = _source.GetGUID()
            }
        };

        return notify;
	}
}