namespace Forged.MapServer.DataStorage.Structs.I;

public sealed record ItemModifiedAppearanceExtraRecord
{
    public uint Id;
    public int IconFileDataID;
    public int UnequippedIconFileDataID;
    public byte SheatheType;
    public sbyte DisplayWeaponSubclassID;
    public sbyte DisplayInventoryType;
}