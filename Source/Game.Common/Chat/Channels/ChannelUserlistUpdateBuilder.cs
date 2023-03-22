// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Networking.Packets;

namespace Game.Chat;

class ChannelUserlistUpdateBuilder : MessageBuilder
{
	readonly Channel _source;
	readonly ObjectGuid _guid;

	public ChannelUserlistUpdateBuilder(Channel source, ObjectGuid guid)
	{
		_source = source;
		_guid = guid;
	}

	public override PacketSenderOwning<UserlistUpdate> Invoke(Locale locale = Locale.enUS)
	{
		var localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

		PacketSenderOwning<UserlistUpdate> userlistUpdate = new();
		userlistUpdate.Data.UpdatedUserGUID = _guid;
		userlistUpdate.Data.ChannelFlags = _source.GetFlags();
		userlistUpdate.Data.UserFlags = _source.GetPlayerFlags(_guid);
		userlistUpdate.Data.ChannelID = _source.GetChannelId();
		userlistUpdate.Data.ChannelName = _source.GetName(localeIdx);

		return userlistUpdate;
	}
}