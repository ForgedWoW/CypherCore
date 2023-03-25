// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Item;

public class InventoryChangeFailure : ServerPacket
{
	public InventoryResult BagResult;
	public byte ContainerBSlot;
	public ObjectGuid SrcContainer;
	public ObjectGuid DstContainer;
	public int SrcSlot;
	public int LimitCategory;
	public int Level;
	public ObjectGuid[] Item = new ObjectGuid[2];
	public InventoryChangeFailure() : base(ServerOpcodes.InventoryChangeFailure) { }

	public override void Write()
	{
		_worldPacket.WriteInt8((sbyte)BagResult);
		_worldPacket.WritePackedGuid(Item[0]);
		_worldPacket.WritePackedGuid(Item[1]);
		_worldPacket.WriteUInt8(ContainerBSlot); // bag type subclass, used with EQUIP_ERR_EVENT_AUTOEQUIP_BIND_CONFIRM and EQUIP_ERR_WRONG_BAG_TYPE_2

		switch (BagResult)
		{
			case InventoryResult.CantEquipLevelI:
			case InventoryResult.PurchaseLevelTooLow:
				_worldPacket.WriteInt32(Level);

				break;
			case InventoryResult.EventAutoequipBindConfirm:
				_worldPacket.WritePackedGuid(SrcContainer);
				_worldPacket.WriteInt32(SrcSlot);
				_worldPacket.WritePackedGuid(DstContainer);

				break;
			case InventoryResult.ItemMaxLimitCategoryCountExceededIs:
			case InventoryResult.ItemMaxLimitCategorySocketedExceededIs:
			case InventoryResult.ItemMaxLimitCategoryEquippedExceededIs:
				_worldPacket.WriteInt32(LimitCategory);

				break;
		}
	}
}