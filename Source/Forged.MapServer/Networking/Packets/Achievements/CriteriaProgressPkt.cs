// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Achievements;

public struct CriteriaProgressPkt
{
    public long Date;

    public uint Flags;

    public uint Id;

    public ObjectGuid Player;

    public ulong Quantity;

    public ulong? RafAcceptanceID;

    public long TimeFromCreate;

    public long TimeFromStart;

    public uint Unused_10_1_5;

    public void Write(WorldPacket data)
    {
        data.WriteUInt32(Id);
        data.WriteUInt64(Quantity);
        data.WritePackedGuid(Player);
        data.WriteUInt32(Unused_10_1_5);
        data.WritePackedTime(Date);
        data.WriteInt64(TimeFromStart);
        data.WriteInt64(TimeFromCreate);
        data.WriteBit(RafAcceptanceID.HasValue);
        data.FlushBits();

        if (RafAcceptanceID.HasValue)
            data.WriteUInt64(RafAcceptanceID.Value);
    }
}