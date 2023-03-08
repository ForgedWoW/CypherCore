// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.Networking;

namespace Game.Entities;

public class ConversationLine
{
	public uint ConversationLineID;
	public uint StartTime;
	public uint UiCameraID;
	public byte ActorIndex;
	public byte Flags;
	public byte ChatType;

	public void WriteCreate(WorldPacket data, Conversation owner, Player receiver)
	{
		data.WriteUInt32(ConversationLineID);
		data.WriteUInt32(GetViewerStartTime(this, owner, receiver));
		data.WriteUInt32(UiCameraID);
		data.WriteUInt8(ActorIndex);
		data.WriteUInt8(Flags);
		data.WriteUInt8(ChatType);
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Conversation owner, Player receiver)
	{
		data.WriteUInt32(ConversationLineID);
		data.WriteUInt32(GetViewerStartTime(this, owner, receiver));
		data.WriteUInt32(UiCameraID);
		data.WriteUInt8(ActorIndex);
		data.WriteUInt8(Flags);
		data.WriteUInt8(ChatType);
	}

	public uint GetViewerStartTime(ConversationLine conversationLine, Conversation conversation, Player receiver)
	{
		var startTime = conversationLine.StartTime;
		var locale = receiver.Session.SessionDbLocaleIndex;

		var localizedStartTime = conversation.GetLineStartTime(locale, (int)conversationLine.ConversationLineID);

		if (localizedStartTime != TimeSpan.Zero)
			startTime = (uint)localizedStartTime.TotalMilliseconds;

		return startTime;
	}
}