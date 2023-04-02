// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Item;

public class BuyItem : ClientPacket
{
    public ObjectGuid ContainerGUID;
    public ItemInstance Item;
    public ItemVendorType ItemType;
    public uint Muid;
    public int Quantity;
    public uint Slot;
    public ObjectGuid VendorGUID;
    public BuyItem(WorldPacket packet) : base(packet)
    {
        Item = new ItemInstance();
    }

    public override void Read()
    {
        VendorGUID = _worldPacket.ReadPackedGuid();
        ContainerGUID = _worldPacket.ReadPackedGuid();
        Quantity = _worldPacket.ReadInt32();
        Muid = _worldPacket.ReadUInt32();
        Slot = _worldPacket.ReadUInt32();
        Item.Read(_worldPacket);
        ItemType = (ItemVendorType)_worldPacket.ReadBits<int>(3);
    }
}