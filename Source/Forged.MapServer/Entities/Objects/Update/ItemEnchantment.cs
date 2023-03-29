// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;

namespace Forged.MapServer.Entities.Objects.Update;

public class ItemEnchantment : BaseUpdateData<Item>
{
    public UpdateField<uint> ID = new(0, 1);
    public UpdateField<uint> Duration = new(0, 2);
    public UpdateField<short> Charges = new(0, 3);
    public UpdateField<ushort> Inactive = new(0, 4);

    public ItemEnchantment() : base(5) { }

    public void WriteCreate(WorldPacket data, Item owner, Player receiver)
    {
        data.WriteUInt32(ID);
        data.WriteUInt32(Duration);
        data.WriteInt16(Charges);
        data.WriteUInt16(Inactive);
    }

    public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Item owner, Player receiver)
    {
        var changesMask = ChangesMask;

        if (ignoreChangesMask)
            changesMask.SetAll();

        data.WriteBits(changesMask.GetBlock(0), 5);

        data.FlushBits();

        if (changesMask[0])
        {
            if (changesMask[1])
                data.WriteUInt32(ID);

            if (changesMask[2])
                data.WriteUInt32(Duration);

            if (changesMask[3])
                data.WriteInt16(Charges);

            if (changesMask[4])
                data.WriteUInt16(Inactive);
        }
    }

    public override void ClearChangesMask()
    {
        ClearChangesMask(ID);
        ClearChangesMask(Duration);
        ClearChangesMask(Charges);
        ClearChangesMask(Inactive);
        ChangesMask.ResetAll();
    }
}