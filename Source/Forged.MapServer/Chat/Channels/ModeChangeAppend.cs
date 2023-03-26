// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Channel;
using Framework.Constants;

namespace Forged.MapServer.Chat.Channels;

internal struct ModeChangeAppend : IChannelAppender
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

    private readonly ObjectGuid _guid;
    private readonly ChannelMemberFlags _oldFlags;
    private readonly ChannelMemberFlags _newFlags;
}