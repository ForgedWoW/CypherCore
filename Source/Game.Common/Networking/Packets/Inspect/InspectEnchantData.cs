// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Inspect;

/// RespondInspectAchievements in AchievementPackets

//Structs
public struct InspectEnchantData
{
	public InspectEnchantData(uint id, byte index)
	{
		Id = id;
		Index = index;
	}

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(Id);
		data.WriteUInt8(Index);
	}

	public uint Id;
	public byte Index;
}
