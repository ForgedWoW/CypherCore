// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Transmogification;

internal struct TransmogrifyItem
{
    public int ItemModifiedAppearanceID;

    public int SecondaryItemModifiedAppearanceID;

    public uint Slot;

    public int SpellItemEnchantmentID;

    public void Read(WorldPacket data)
    {
        ItemModifiedAppearanceID = data.ReadInt32();
        Slot = data.ReadUInt32();
        SpellItemEnchantmentID = data.ReadInt32();
        SecondaryItemModifiedAppearanceID = data.ReadInt32();
    }
}