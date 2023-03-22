﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

public struct AzeriteEssenceData
{
	public uint Index;
	public uint AzeriteEssenceID;
	public uint Rank;
	public bool SlotUnlocked;

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(Index);
		data.WriteUInt32(AzeriteEssenceID);
		data.WriteUInt32(Rank);
		data.WriteBit(SlotUnlocked);
		data.FlushBits();
	}
}