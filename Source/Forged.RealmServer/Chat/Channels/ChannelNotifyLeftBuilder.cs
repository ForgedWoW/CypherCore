// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Maps;
using Game.Networking.Packets;

namespace Forged.RealmServer.Chat;

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

		PacketSenderOwning<ChannelNotifyLeft> notify = new();
		notify.Data.Channel = _source.GetName(localeIdx);
		notify.Data.ChatChannelID = _source.GetChannelId();
		notify.Data.Suspended = _suspended;

		return notify;
	}
}