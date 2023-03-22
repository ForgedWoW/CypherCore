// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Networking.Packets;

namespace Game.Chat;

class ChannelUserlistRemoveBuilder : MessageBuilder
{
	readonly Channel _source;
	readonly ObjectGuid _guid;

	public ChannelUserlistRemoveBuilder(Channel source, ObjectGuid guid)
	{
		_source = source;
		_guid = guid;
	}

	public override PacketSenderOwning<UserlistRemove> Invoke(Locale locale = Locale.enUS)
	{
		var localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

		PacketSenderOwning<UserlistRemove> userlistRemove = new();
		userlistRemove.Data.RemovedUserGUID = _guid;
		userlistRemove.Data.ChannelFlags = _source.GetFlags();
		userlistRemove.Data.ChannelID = _source.GetChannelId();
		userlistRemove.Data.ChannelName = _source.GetName(localeIdx);

		return userlistRemove;
	}
}