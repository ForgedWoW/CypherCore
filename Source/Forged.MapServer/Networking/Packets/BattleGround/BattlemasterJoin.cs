// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Networking.Packets.BattleGround;

internal class BattlemasterJoin : ClientPacket
{
    public Array<ulong> QueueIDs = new(1);
    public byte Roles;
    public int[] BlacklistMap = new int[2];
    public BattlemasterJoin(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        var queueCount = _worldPacket.ReadUInt32();
        Roles = _worldPacket.ReadUInt8();
        BlacklistMap[0] = _worldPacket.ReadInt32();
        BlacklistMap[1] = _worldPacket.ReadInt32();

        for (var i = 0; i < queueCount; ++i)
            QueueIDs[i] = _worldPacket.ReadUInt64();
    }
}