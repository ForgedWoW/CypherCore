namespace Forged.MapServer.DataStorage.Structs.I;

public sealed record ItemLimitCategoryRecord
{
    public uint Id;
    public string Name;
    public byte Quantity;
    public byte Flags;
}