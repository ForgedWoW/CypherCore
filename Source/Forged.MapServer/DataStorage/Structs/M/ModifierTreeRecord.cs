namespace Forged.MapServer.DataStorage.Structs.M;

public sealed class ModifierTreeRecord
{
    public uint Id;
    public uint Parent;
    public sbyte Operator;
    public sbyte Amount;
    public uint Type;
    public uint Asset;
    public int SecondaryAsset;
    public int TertiaryAsset;
}