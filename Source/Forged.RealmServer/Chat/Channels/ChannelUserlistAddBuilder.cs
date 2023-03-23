// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Forged.RealmServer.Maps;
using Game.Common.Entities.Objects;
using Game.Common.Networking.Packets.Channel;

namespace Forged.RealmServer.Chat;

class ChannelUserlistAddBuilder : MessageBuilder
{
	readonly Channel _source;
	readonly ObjectGuid _guid;

	public ChannelUserlistAddBuilder(Channel source, ObjectGuid guid)
	{
		_source = source;
		_guid = guid;
	}

	public override PacketSenderOwning<UserlistAdd> Invoke(Locale locale = Locale.enUS)
	{
		var localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

		PacketSenderOwning<UserlistAdd> userlistAdd = new();
		userlistAdd.Data.AddedUserGUID = _guid;
		userlistAdd.Data.ChannelFlags = _source.GetFlags();
		userlistAdd.Data.UserFlags = _source.GetPlayerFlags(_guid);
		userlistAdd.Data.ChannelID = _source.GetChannelId();
		userlistAdd.Data.ChannelName = _source.GetName(localeIdx);

		return userlistAdd;
	}
}