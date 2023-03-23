// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking.Packets.Channel;

namespace Forged.RealmServer.Chat;

struct VoiceOnAppend : IChannelAppender
{
	public VoiceOnAppend(ObjectGuid guid)
	{
		_guid = guid;
	}

	public ChatNotify GetNotificationType() => ChatNotify.VoiceOnNotice;

	public void Append(ChannelNotify data)
	{
		data.SenderGuid = _guid;
	}

	readonly ObjectGuid _guid;
}