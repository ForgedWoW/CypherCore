namespace Forged.MapServer.DataStorage.Structs.S;

public sealed class SpellShapeshiftRecord
{
    public uint Id;
    public uint SpellID;
    public sbyte StanceBarOrder;
    public uint[] ShapeshiftExclude = new uint[2];
    public uint[] ShapeshiftMask = new uint[2];
}