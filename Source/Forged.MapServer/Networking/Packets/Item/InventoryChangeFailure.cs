// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Item;

public class InventoryChangeFailure : ServerPacket
{
    public InventoryResult BagResult;
    public byte ContainerBSlot;
    public ObjectGuid DstContainer;
    public ObjectGuid[] Item = new ObjectGuid[2];
    public int Level;
    public int LimitCategory;
    public ObjectGuid SrcContainer;
    public int SrcSlot;
    public InventoryChangeFailure() : base(ServerOpcodes.InventoryChangeFailure) { }

    public override void Write()
    {
        WorldPacket.WriteInt8((sbyte)BagResult);
        WorldPacket.WritePackedGuid(Item[0]);
        WorldPacket.WritePackedGuid(Item[1]);
        WorldPacket.WriteUInt8(ContainerBSlot); // bag type subclass, used with EQUIP_ERR_EVENT_AUTOEQUIP_BIND_CONFIRM and EQUIP_ERR_WRONG_BAG_TYPE_2

        switch (BagResult)
        {
            case InventoryResult.CantEquipLevelI:
            case InventoryResult.PurchaseLevelTooLow:
                WorldPacket.WriteInt32(Level);

                break;
            case InventoryResult.EventAutoequipBindConfirm:
                WorldPacket.WritePackedGuid(SrcContainer);
                WorldPacket.WriteInt32(SrcSlot);
                WorldPacket.WritePackedGuid(DstContainer);

                break;
            case InventoryResult.ItemMaxLimitCategoryCountExceededIs:
            case InventoryResult.ItemMaxLimitCategorySocketedExceededIs:
            case InventoryResult.ItemMaxLimitCategoryEquippedExceededIs:
                WorldPacket.WriteInt32(LimitCategory);

                break;
        }
    }
}