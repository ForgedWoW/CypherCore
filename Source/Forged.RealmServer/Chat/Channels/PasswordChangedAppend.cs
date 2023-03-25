// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Entities.Objects;
using Forged.RealmServer.Networking.Packets.Channel;

namespace Forged.RealmServer.Chat;

struct PasswordChangedAppend : IChannelAppender
{
	public PasswordChangedAppend(ObjectGuid guid)
	{
		_guid = guid;
	}

	public ChatNotify GetNotificationType() => ChatNotify.PasswordChangedNotice;

	public void Append(ChannelNotify data)
	{
		data.SenderGuid = _guid;
	}

	readonly ObjectGuid _guid;
}