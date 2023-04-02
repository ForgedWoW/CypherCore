// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Azerite;

internal class AzeriteEmpoweredItemSelectPower : ClientPacket
{
    public int AzeritePowerID;
    public byte ContainerSlot;
    public byte Slot;
    public int Tier;
    public AzeriteEmpoweredItemSelectPower(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Tier = WorldPacket.ReadInt32();
        AzeritePowerID = WorldPacket.ReadInt32();
        ContainerSlot = WorldPacket.ReadUInt8();
        Slot = WorldPacket.ReadUInt8();
    }
}