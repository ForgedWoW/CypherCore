// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.DoWork;
using Game.Common.Networking.Packets.Channel;
using Game.Common.Text;

namespace Game.Common.Chat.Channels;

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

		PacketSenderOwning<ChannelNotify> sender = new();
		sender.Data.Type = _modifier.GetNotificationType();
		sender.Data.Channel = _source.GetName(localeIdx);
		_modifier.Append(sender.Data);
		sender.Data.Write();

		return sender;
	}
}

//Appenders