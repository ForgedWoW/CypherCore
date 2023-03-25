// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Forged.RealmServer.Networking.Packets;

public class ClientGossipText
{
	public uint QuestID;
	public uint ContentTuningID;
	public int QuestType;
	public bool Repeatable;
	public string QuestTitle;
	public uint QuestFlags;
	public uint QuestFlagsEx;

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(QuestID);
		data.WriteUInt32(ContentTuningID);
		data.WriteInt32(QuestType);
		data.WriteUInt32(QuestFlags);
		data.WriteUInt32(QuestFlagsEx);

		data.WriteBit(Repeatable);
		data.WriteBits(QuestTitle.GetByteCount(), 9);
		data.FlushBits();

		data.WriteString(QuestTitle);
	}
}