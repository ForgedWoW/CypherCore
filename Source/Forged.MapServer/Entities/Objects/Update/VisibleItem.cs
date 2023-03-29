// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Networking;

namespace Forged.MapServer.Entities.Objects.Update;

public class VisibleItem : BaseUpdateData<Unit>
{
    public UpdateField<uint> ItemID = new(0, 1);
    public UpdateField<uint> SecondaryItemModifiedAppearanceID = new(0, 2);
    public UpdateField<ushort> ItemAppearanceModID = new(0, 3);
    public UpdateField<ushort> ItemVisual = new(0, 4);

    public VisibleItem() : base(5) { }

    public void WriteCreate(WorldPacket data, Unit owner, Player receiver)
    {
        data.WriteUInt32(ItemID);
        data.WriteUInt32(SecondaryItemModifiedAppearanceID);
        data.WriteUInt16(ItemAppearanceModID);
        data.WriteUInt16(ItemVisual);
    }

    public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Unit owner, Player receiver)
    {
        var changesMask = ChangesMask;

        if (ignoreChangesMask)
            changesMask.SetAll();

        data.WriteBits(changesMask.GetBlock(0), 5);

        data.FlushBits();

        if (changesMask[0])
        {
            if (changesMask[1])
                data.WriteUInt32(ItemID);

            if (changesMask[2])
                data.WriteUInt32(SecondaryItemModifiedAppearanceID);

            if (changesMask[3])
                data.WriteUInt16(ItemAppearanceModID);

            if (changesMask[4])
                data.WriteUInt16(ItemVisual);
        }
    }

    public override void ClearChangesMask()
    {
        ClearChangesMask(ItemID);
        ClearChangesMask(SecondaryItemModifiedAppearanceID);
        ClearChangesMask(ItemAppearanceModID);
        ClearChangesMask(ItemVisual);
        ChangesMask.ResetAll();
    }
}