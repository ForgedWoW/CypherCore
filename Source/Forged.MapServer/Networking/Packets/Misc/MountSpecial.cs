// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Misc;

internal class MountSpecial : ClientPacket
{
    public int SequenceVariation;
    public int[] SpellVisualKitIDs;
    public MountSpecial(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        SpellVisualKitIDs = new int[WorldPacket.ReadUInt32()];
        SequenceVariation = WorldPacket.ReadInt32();

        for (var i = 0; i < SpellVisualKitIDs.Length; ++i)
            SpellVisualKitIDs[i] = WorldPacket.ReadInt32();
    }
}