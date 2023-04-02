// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Item;

internal class AutoEquipItemSlot : ClientPacket
{
    public InvUpdate Inv;
    public ObjectGuid Item;
    public byte ItemDstSlot;
    public AutoEquipItemSlot(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Inv = new InvUpdate(WorldPacket);
        Item = WorldPacket.ReadPackedGuid();
        ItemDstSlot = WorldPacket.ReadUInt8();
    }
}