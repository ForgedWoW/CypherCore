// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.LFG;

public class RideTicket
{
    public uint Id;
    public ObjectGuid RequesterGuid;
    public long Time;
    public RideType Type;
    public bool Unknown925;

    public void Read(WorldPacket data)
    {
        RequesterGuid = data.ReadPackedGuid();
        Id = data.ReadUInt32();
        Type = (RideType)data.ReadUInt32();
        Time = data.ReadInt64();
        Unknown925 = data.HasBit();
        data.ResetBitPos();
    }

    public void Write(WorldPacket data)
    {
        data.WritePackedGuid(RequesterGuid);
        data.WriteUInt32(Id);
        data.WriteUInt32((uint)Type);
        data.WriteInt64(Time);
        data.WriteBit(Unknown925);
        data.FlushBits();
    }
}