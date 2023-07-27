namespace Forged.MapServer.DataStorage.Structs.I;

public sealed record ItemModifiedAppearanceRecord
{
    public uint Id;
    public uint ItemID;
    public int ItemAppearanceModifierID;
    public int ItemAppearanceID;
    public int OrderIndex;
    public byte TransmogSourceTypeEnum;
}