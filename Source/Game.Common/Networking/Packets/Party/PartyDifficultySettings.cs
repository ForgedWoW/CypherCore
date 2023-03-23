// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Party;

struct PartyDifficultySettings
{
	public void Write(WorldPacket data)
	{
		data.WriteUInt32(DungeonDifficultyID);
		data.WriteUInt32(RaidDifficultyID);
		data.WriteUInt32(LegacyRaidDifficultyID);
	}

	public uint DungeonDifficultyID;
	public uint RaidDifficultyID;
	public uint LegacyRaidDifficultyID;
}
