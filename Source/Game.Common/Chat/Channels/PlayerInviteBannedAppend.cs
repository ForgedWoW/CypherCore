// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Networking.Packets.Channel;

namespace Game.Common.Chat.Channels;

struct PlayerInviteBannedAppend : IChannelAppender
{
	public PlayerInviteBannedAppend(string playerName)
	{
		_playerName = playerName;
	}

	public ChatNotify GetNotificationType() => ChatNotify.PlayerInviteBannedNotice;

	public void Append(ChannelNotify data)
	{
		data.Sender = _playerName;
	}

	readonly string _playerName;
}
