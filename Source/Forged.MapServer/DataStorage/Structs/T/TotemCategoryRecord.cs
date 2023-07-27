namespace Forged.MapServer.DataStorage.Structs.T;

public sealed record TotemCategoryRecord
{
    public uint Id;
    public string Name;
    public byte TotemCategoryType;
    public int TotemCategoryMask;
}