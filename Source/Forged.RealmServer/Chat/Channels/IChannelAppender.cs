// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Networking.Packets.Channel;

namespace Forged.RealmServer.Chat;

interface IChannelAppender
{
	void Append(ChannelNotify data);
	ChatNotify GetNotificationType();
}