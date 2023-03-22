// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.DataStorage;

public sealed class ItemRecord
{
	public uint Id;
	public ItemClass ClassID;
	public byte SubclassID;
	public byte Material;
	public InventoryType inventoryType;
	public byte SheatheType;
	public sbyte SoundOverrideSubclassID;
	public int IconFileDataID;
	public byte ItemGroupSoundsID;
	public int ContentTuningID;
	public int ModifiedCraftingReagentItemID;
	public int CraftingQualityID;
}