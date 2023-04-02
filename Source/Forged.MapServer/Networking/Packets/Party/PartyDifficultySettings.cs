// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Party;

internal struct PartyDifficultySettings
{
    public uint DungeonDifficultyID;

    public uint LegacyRaidDifficultyID;

    public uint RaidDifficultyID;

    public void Write(WorldPacket data)
    {
        data.WriteUInt32(DungeonDifficultyID);
        data.WriteUInt32(RaidDifficultyID);
        data.WriteUInt32(LegacyRaidDifficultyID);
    }
}