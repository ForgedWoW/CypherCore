// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Maps;
using Game.Common.Chat.Channels;
using Game.Common.Networking.Packets.Channel;
using Game.Common.Text;

namespace Game.Common.Chat.Channels;

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

		PacketSenderOwning<ChannelNotifyJoined> notify = new();
		//notify.ChannelWelcomeMsg = "";
		notify.Data.ChatChannelID = (int)_source.GetChannelId();
		//notify.InstanceID = 0;
		notify.Data.ChannelFlags = _source.GetFlags();
		notify.Data.Channel = _source.GetName(localeIdx);
		notify.Data.ChannelGUID = _source.GetGUID();

		return notify;
	}
}
