// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Party;

internal struct PartyLFGInfo
{
    public bool Aborted;

    public byte BootCount;

    public bool MyFirstReward;

    public byte MyFlags;

    public float MyGearDiff;

    public byte MyKickVoteCount;

    public byte MyPartialClear;

    public uint MyRandomSlot;

    public byte MyStrangerCount;

    public uint Slot;

    public void Write(WorldPacket data)
    {
        data.WriteUInt8(MyFlags);
        data.WriteUInt32(Slot);
        data.WriteUInt32(MyRandomSlot);
        data.WriteUInt8(MyPartialClear);
        data.WriteFloat(MyGearDiff);
        data.WriteUInt8(MyStrangerCount);
        data.WriteUInt8(MyKickVoteCount);
        data.WriteUInt8(BootCount);
        data.WriteBit(Aborted);
        data.WriteBit(MyFirstReward);
        data.FlushBits();
    }
}