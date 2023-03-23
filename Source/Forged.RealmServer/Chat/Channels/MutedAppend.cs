﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Networking.Packets.Channel;

namespace Forged.RealmServer.Chat;

struct MutedAppend : IChannelAppender
{
	public ChatNotify GetNotificationType() => ChatNotify.MutedNotice;

	public void Append(ChannelNotify data) { }
}