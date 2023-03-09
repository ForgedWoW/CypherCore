// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

struct FactionStandingData
{
	public FactionStandingData(int index, int standing)
	{
		Index = index;
		Standing = standing;
	}

	public void Write(WorldPacket data)
	{
		data.WriteInt32(Index);
		data.WriteInt32(Standing);
	}

	readonly int Index;
	readonly int Standing;
}