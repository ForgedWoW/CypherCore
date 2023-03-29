// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.LFG;

internal class LFGQueueStatus : ServerPacket
{
    public RideTicket Ticket;
    public uint Slot;
    public uint AvgWaitTimeMe;
    public uint AvgWaitTime;
    public uint[] AvgWaitTimeByRole = new uint[3];
    public byte[] LastNeeded = new byte[3];
    public uint QueuedTime;
    public LFGQueueStatus() : base(ServerOpcodes.LfgQueueStatus) { }

    public override void Write()
    {
        Ticket.Write(_worldPacket);

        _worldPacket.WriteUInt32(Slot);
        _worldPacket.WriteUInt32(AvgWaitTimeMe);
        _worldPacket.WriteUInt32(AvgWaitTime);

        for (var i = 0; i < 3; i++)
        {
            _worldPacket.WriteUInt32(AvgWaitTimeByRole[i]);
            _worldPacket.WriteUInt8(LastNeeded[i]);
        }

        _worldPacket.WriteUInt32(QueuedTime);
    }
}