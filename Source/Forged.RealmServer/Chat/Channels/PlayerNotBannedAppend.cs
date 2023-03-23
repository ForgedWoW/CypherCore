// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Networking.Packets;

namespace Forged.RealmServer.Chat;

struct PlayerNotBannedAppend : IChannelAppender
{
	public PlayerNotBannedAppend(string playerName)
	{
		_playerName = playerName;
	}

	public ChatNotify GetNotificationType() => ChatNotify.PlayerNotBannedNotice;

	public void Append(ChannelNotify data)
	{
		data.Sender = _playerName;
	}

	readonly string _playerName;
}