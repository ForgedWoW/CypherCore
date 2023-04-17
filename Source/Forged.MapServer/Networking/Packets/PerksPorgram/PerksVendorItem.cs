// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.PerksPorgram;

public struct PerksVendorItem
{
    public long AvailableUntil;
    public int BattlePetSpeciesID;
    public bool Disabled;
    public int Field_14;
    public int Field_18;
    public int ItemModifiedAppearanceID;
    public int MountID;
    public int Price;
    public int TransmogSetID;
    public int VendorItemID;

    public void Write(WorldPacket data)
    {
        data.WriteInt32(VendorItemID);
        data.WriteInt32(MountID);
        data.WriteInt32(BattlePetSpeciesID);
        data.WriteInt32(TransmogSetID);
        data.WriteInt32(ItemModifiedAppearanceID);
        data.WriteInt32(Field_14);
        data.WriteInt32(Field_18);
        data.WriteInt32(Price);
        data.WriteInt64(AvailableUntil);
        data.WriteBit(Disabled);
        data.FlushBits();
    }
}