// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;

namespace Forged.MapServer.Entities.Objects.Update;

public class CraftingOrderItem : BaseUpdateData<Player>
{
    public OptionalUpdateField<byte> DataSlotIndex = new(-1, 6);
    public UpdateField<ulong> Field_0 = new(-1, 0);
    public UpdateField<ObjectGuid> ItemGUID = new(-1, 1);
    public UpdateField<int> ItemID = new(-1, 3);
    public UpdateField<ObjectGuid> OwnerGUID = new(-1, 2);
    public UpdateField<uint> Quantity = new(-1, 4);
    public UpdateField<int> ReagentQuality = new(-1, 5);
    public CraftingOrderItem() : base(7) { }

    public override void ClearChangesMask()
    {
        ClearChangesMask(Field_0);
        ClearChangesMask(ItemGUID);
        ClearChangesMask(OwnerGUID);
        ClearChangesMask(ItemID);
        ClearChangesMask(Quantity);
        ClearChangesMask(ReagentQuality);
        ClearChangesMask(DataSlotIndex);
        ChangesMask.ResetAll();
    }

    public void WriteCreate(WorldPacket data, Player owner, Player receiver)
    {
        data.WriteUInt64(Field_0);
        data.WritePackedGuid(ItemGUID);
        data.WritePackedGuid(OwnerGUID);
        data.WriteInt32(ItemID);
        data.WriteUInt32(Quantity);
        data.WriteInt32(ReagentQuality);
        data.WriteBits(DataSlotIndex.HasValue(), 1);

        if (DataSlotIndex.HasValue())
            data.WriteUInt8(DataSlotIndex);
    }

    public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
    {
        var changesMask = ChangesMask;

        if (ignoreChangesMask)
            changesMask.SetAll();

        data.WriteBits(changesMask.GetBlock(0), 7);

        data.FlushBits();

        if (changesMask[0])
            data.WriteUInt64(Field_0);

        if (changesMask[1])
            data.WritePackedGuid(ItemGUID);

        if (changesMask[2])
            data.WritePackedGuid(OwnerGUID);

        if (changesMask[3])
            data.WriteInt32(ItemID);

        if (changesMask[4])
            data.WriteUInt32(Quantity);

        if (changesMask[5])
            data.WriteInt32(ReagentQuality);

        data.WriteBits(DataSlotIndex.HasValue(), 1);

        if (changesMask[6])
            if (DataSlotIndex.HasValue())
                data.WriteUInt8(DataSlotIndex);
    }
}