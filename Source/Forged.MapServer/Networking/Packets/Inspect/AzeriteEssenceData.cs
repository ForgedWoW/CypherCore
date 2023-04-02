// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Inspect;

public struct AzeriteEssenceData
{
    public uint AzeriteEssenceID;
    public uint Index;
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