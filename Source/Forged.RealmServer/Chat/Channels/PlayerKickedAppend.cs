// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Networking.Packets;

namespace Forged.RealmServer.Chat;

struct PlayerKickedAppend : IChannelAppender
{
	public PlayerKickedAppend(ObjectGuid kicker, ObjectGuid kickee)
	{
		_kicker = kicker;
		_kickee = kickee;
	}

	public ChatNotify GetNotificationType() => ChatNotify.PlayerKickedNotice;

	public void Append(ChannelNotify data)
	{
		data.SenderGuid = _kicker;
		data.TargetGuid = _kickee;
	}

	readonly ObjectGuid _kicker;
	readonly ObjectGuid _kickee;
}