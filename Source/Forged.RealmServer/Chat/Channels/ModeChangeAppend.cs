// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking.Packets.Channel;

namespace Forged.RealmServer.Chat;

struct ModeChangeAppend : IChannelAppender
{
	public ModeChangeAppend(ObjectGuid guid, ChannelMemberFlags oldFlags, ChannelMemberFlags newFlags)
	{
		_guid = guid;
		_oldFlags = oldFlags;
		_newFlags = newFlags;
	}

	public ChatNotify GetNotificationType() => ChatNotify.ModeChangeNotice;

	public void Append(ChannelNotify data)
	{
		data.SenderGuid = _guid;
		data.OldFlags = _oldFlags;
		data.NewFlags = _newFlags;
	}

	readonly ObjectGuid _guid;
	readonly ChannelMemberFlags _oldFlags;
	readonly ChannelMemberFlags _newFlags;
}