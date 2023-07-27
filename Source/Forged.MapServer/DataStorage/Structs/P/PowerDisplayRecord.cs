namespace Forged.MapServer.DataStorage.Structs.P;

public sealed record PowerDisplayRecord
{
    public uint Id;
    public string GlobalStringBaseTag;
    public byte ActualType;
    public byte Red;
    public byte Green;
    public byte Blue;
}