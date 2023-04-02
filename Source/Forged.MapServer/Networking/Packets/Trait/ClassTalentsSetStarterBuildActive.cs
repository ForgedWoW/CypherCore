// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Trait;

internal class ClassTalentsSetStarterBuildActive : ClientPacket
{
    public bool Active;
    public int ConfigID;
    public ClassTalentsSetStarterBuildActive(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        ConfigID = _worldPacket.ReadInt32();
        Active = _worldPacket.HasBit();
    }
}