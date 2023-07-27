using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.I;

public sealed record ItemRecord
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