// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Trait;

internal class ClassTalentsDeleteConfig : ClientPacket
{
    public int ConfigID;

    public ClassTalentsDeleteConfig(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        ConfigID = WorldPacket.ReadInt32();
    }
}