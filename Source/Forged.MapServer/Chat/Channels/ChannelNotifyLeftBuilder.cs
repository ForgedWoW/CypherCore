// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Maps.Workers;
using Forged.MapServer.Networking.Packets.Channel;
using Forged.MapServer.Text;
using Framework.Constants;

namespace Forged.MapServer.Chat.Channels;

class ChannelNotifyLeftBuilder : MessageBuilder
{
	readonly Channel _source;
	readonly bool _suspended;

	public ChannelNotifyLeftBuilder(Channel source, bool suspend)
	{
		_source = source;
		_suspended = suspend;
	}

	public override PacketSenderOwning<ChannelNotifyLeft> Invoke(Locale locale = Locale.enUS)
	{
		var localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

		PacketSenderOwning<ChannelNotifyLeft> notify = new()
		{
			Data =
			{
				Channel = _source.GetName(localeIdx),
				ChatChannelID = _source.GetChannelId(),
				Suspended = _suspended
			}
		};

		return notify;
	}
}