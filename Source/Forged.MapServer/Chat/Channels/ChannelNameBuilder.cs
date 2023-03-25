// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Maps.Workers;
using Forged.MapServer.Networking.Packets.Channel;
using Forged.MapServer.Text;
using Framework.Constants;

namespace Forged.MapServer.Chat.Channels;

// initial packet data (notify type and channel name)
class ChannelNameBuilder : MessageBuilder
{
	readonly Channel _source;
	readonly IChannelAppender _modifier;

	public ChannelNameBuilder(Channel source, IChannelAppender modifier)
	{
		_source = source;
		_modifier = modifier;
	}

	public override PacketSenderOwning<ChannelNotify> Invoke(Locale locale = Locale.enUS)
	{
		// LocalizedPacketDo sends client DBC locale, we need to get available to server locale
		var localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

		PacketSenderOwning<ChannelNotify> sender = new()
		{
			Data =
			{
				Type = _modifier.GetNotificationType(),
				Channel = _source.GetName(localeIdx)
			}
		};

		_modifier.Append(sender.Data);
		sender.Data.Write();

		return sender;
	}
}

//Appenders