// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.LFG;

internal class LFGQueueStatus : ServerPacket
{
    public uint AvgWaitTime;
    public uint[] AvgWaitTimeByRole = new uint[3];
    public uint AvgWaitTimeMe;
    public byte[] LastNeeded = new byte[3];
    public uint QueuedTime;
    public uint Slot;
    public RideTicket Ticket;
    public LFGQueueStatus() : base(ServerOpcodes.LfgQueueStatus) { }

    public override void Write()
    {
        Ticket.Write(WorldPacket);

        WorldPacket.WriteUInt32(Slot);
        WorldPacket.WriteUInt32(AvgWaitTimeMe);
        WorldPacket.WriteUInt32(AvgWaitTime);

        for (var i = 0; i < 3; i++)
        {
            WorldPacket.WriteUInt32(AvgWaitTimeByRole[i]);
            WorldPacket.WriteUInt8(LastNeeded[i]);
        }

        WorldPacket.WriteUInt32(QueuedTime);
    }
}