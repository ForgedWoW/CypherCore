﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Networking.Packets;

namespace Forged.RealmServer.Chat;

struct NotModeratedAppend : IChannelAppender
{
	public ChatNotify GetNotificationType() => ChatNotify.NotModeratedNotice;

	public void Append(ChannelNotify data) { }
}