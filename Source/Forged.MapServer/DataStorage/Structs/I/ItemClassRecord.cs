namespace Forged.MapServer.DataStorage.Structs.I;

public sealed record ItemClassRecord
{
    public uint Id;
    public string ClassName;
    public sbyte ClassID;
    public float PriceModifier;
    public byte Flags;
}