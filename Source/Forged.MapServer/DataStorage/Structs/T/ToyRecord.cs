namespace Forged.MapServer.DataStorage.Structs.T;

public sealed record ToyRecord
{
    public string SourceText;
    public uint Id;
    public uint ItemID;
    public byte Flags;
    public sbyte SourceTypeEnum;
}