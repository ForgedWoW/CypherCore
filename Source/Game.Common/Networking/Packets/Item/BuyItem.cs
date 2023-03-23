// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Item;

namespace Game.Common.Networking.Packets.Item;

public class BuyItem : ClientPacket
{
	public ObjectGuid VendorGUID;
	public ItemInstance Item;
	public uint Muid;
	public uint Slot;
	public ItemVendorType ItemType;
	public int Quantity;
	public ObjectGuid ContainerGUID;

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
