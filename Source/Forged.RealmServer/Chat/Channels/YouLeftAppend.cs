// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Networking.Packets.Channel;

namespace Forged.RealmServer.Chat;

struct YouLeftAppend : IChannelAppender
{
	public YouLeftAppend(Channel channel)
	{
		_channel = channel;
	}

	public ChatNotify GetNotificationType() => ChatNotify.YouLeftNotice;

	public void Append(ChannelNotify data)
	{
		data.ChatChannelID = (int)_channel.GetChannelId();
	}

	readonly Channel _channel;
}