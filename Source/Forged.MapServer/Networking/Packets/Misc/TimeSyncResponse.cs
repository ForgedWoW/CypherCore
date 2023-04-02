// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Forged.MapServer.Networking.Packets.Misc;

public class TimeSyncResponse : ClientPacket
{
    public uint ClientTime;    // Client ticks in ms
    public uint SequenceIndex; // Same index as in request
    public TimeSyncResponse(WorldPacket packet) : base(packet) { }

    public DateTime GetReceivedTime()
    {
        return WorldPacket.ReceivedTime;
    }

    public override void Read()
    {
        SequenceIndex = WorldPacket.ReadUInt32();
        ClientTime = WorldPacket.ReadUInt32();
    }
}