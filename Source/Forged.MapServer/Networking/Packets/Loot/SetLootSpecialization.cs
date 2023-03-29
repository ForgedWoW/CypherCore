// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Loot;

internal class SetLootSpecialization : ClientPacket
{
    public uint SpecID;
    public SetLootSpecialization(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        SpecID = _worldPacket.ReadUInt32();
    }
}