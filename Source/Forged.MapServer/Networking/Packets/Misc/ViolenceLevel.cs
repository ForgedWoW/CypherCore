// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Misc;

public class ViolenceLevel : ClientPacket
{
    public sbyte Level; // 0 - no combat effects, 1 - display some combat effects, 2 - blood, 3 - bloody, 4 - bloodier, 5 - bloodiest
    public ViolenceLevel(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Level = WorldPacket.ReadInt8();
    }
}