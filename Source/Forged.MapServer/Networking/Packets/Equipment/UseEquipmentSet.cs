// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Item;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Equipment;

internal class UseEquipmentSet : ClientPacket
{
    public ulong GUID;
    public InvUpdate Inv;
    public EquipmentSetItem[] Items = new EquipmentSetItem[EquipmentSlot.End];
     //Set Identifier
    public UseEquipmentSet(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Inv = new InvUpdate(WorldPacket);

        for (byte i = 0; i < EquipmentSlot.End; ++i)
        {
            Items[i].Item = WorldPacket.ReadPackedGuid();
            Items[i].ContainerSlot = WorldPacket.ReadUInt8();
            Items[i].Slot = WorldPacket.ReadUInt8();
        }

        GUID = WorldPacket.ReadUInt64();
    }

    public struct EquipmentSetItem
    {
        public byte ContainerSlot;
        public ObjectGuid Item;
        public byte Slot;
    }
}