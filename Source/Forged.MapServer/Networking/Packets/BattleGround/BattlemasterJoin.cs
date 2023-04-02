// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Networking.Packets.BattleGround;

internal class BattlemasterJoin : ClientPacket
{
    public int[] BlacklistMap = new int[2];
    public Array<ulong> QueueIDs = new(1);
    public byte Roles;
    public BattlemasterJoin(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        var queueCount = WorldPacket.ReadUInt32();
        Roles = WorldPacket.ReadUInt8();
        BlacklistMap[0] = WorldPacket.ReadInt32();
        BlacklistMap[1] = WorldPacket.ReadInt32();

        for (var i = 0; i < queueCount; ++i)
            QueueIDs[i] = WorldPacket.ReadUInt64();
    }
}