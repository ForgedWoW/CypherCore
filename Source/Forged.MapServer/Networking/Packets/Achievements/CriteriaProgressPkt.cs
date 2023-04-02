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

    public void Write(WorldPacket data)
    {
        data.WriteUInt32(Id);
        data.WriteUInt64(Quantity);
        data.WritePackedGuid(Player);
        data.WritePackedTime(Date);
        data.WriteInt64(TimeFromStart);
        data.WriteInt64(TimeFromCreate);
        data.WriteBits(Flags, 4);
        data.WriteBit(RafAcceptanceID.HasValue);
        data.FlushBits();

        if (RafAcceptanceID.HasValue)
            data.WriteUInt64(RafAcceptanceID.Value);
    }
}