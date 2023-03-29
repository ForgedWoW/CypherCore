// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.LFG;

public class LFGBlackListSlot
{
    public uint Slot;
    public uint Reason;
    public int SubReason1;
    public int SubReason2;
    public uint SoftLock;

    public LFGBlackListSlot(uint slot, uint reason, int subReason1, int subReason2, uint softLock)
    {
        Slot = slot;
        Reason = reason;
        SubReason1 = subReason1;
        SubReason2 = subReason2;
        SoftLock = softLock;
    }

    public void Write(WorldPacket data)
    {
        data.WriteUInt32(Slot);
        data.WriteUInt32(Reason);
        data.WriteInt32(SubReason1);
        data.WriteInt32(SubReason2);
        data.WriteUInt32(SoftLock);
    }
}