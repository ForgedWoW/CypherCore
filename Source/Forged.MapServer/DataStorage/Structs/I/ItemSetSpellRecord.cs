namespace Forged.MapServer.DataStorage.Structs.I;

public sealed record ItemSetSpellRecord
{
    public uint Id;
    public ushort ChrSpecID;
    public uint SpellID;
    public byte Threshold;
    public uint ItemSetID;
}