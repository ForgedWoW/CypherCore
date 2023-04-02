// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.I;

public sealed class ItemRecord
{
    public ItemClass ClassID;
    public int ContentTuningID;
    public int CraftingQualityID;
    public int IconFileDataID;
    public uint Id;
    public InventoryType inventoryType;
    public byte ItemGroupSoundsID;
    public byte Material;
    public int ModifiedCraftingReagentItemID;
    public byte SheatheType;
    public sbyte SoundOverrideSubclassID;
    public byte SubclassID;
}