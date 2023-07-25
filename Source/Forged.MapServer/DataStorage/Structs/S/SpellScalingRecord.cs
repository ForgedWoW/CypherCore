namespace Forged.MapServer.DataStorage.Structs.S;

public sealed class SpellScalingRecord
{
    public uint Id;
    public uint SpellID;
    public uint MinScalingLevel;
    public uint MaxScalingLevel;
    public ushort ScalesFromItemLevel;
}