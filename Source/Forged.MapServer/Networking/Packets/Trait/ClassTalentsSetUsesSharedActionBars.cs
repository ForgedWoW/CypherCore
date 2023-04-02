// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Trait;

internal class ClassTalentsSetUsesSharedActionBars : ClientPacket
{
    public int ConfigID;
    public bool IsLastSelectedSavedConfig;
    public bool UsesShared;
    public ClassTalentsSetUsesSharedActionBars(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        ConfigID = WorldPacket.ReadInt32();
        UsesShared = WorldPacket.HasBit();
        IsLastSelectedSavedConfig = WorldPacket.HasBit();
    }
}