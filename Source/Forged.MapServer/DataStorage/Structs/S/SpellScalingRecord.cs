namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SpellScalingRecord
{
    public uint Id;
    public uint SpellID;
    public uint MinScalingLevel;
    public uint MaxScalingLevel;
    public ushort ScalesFromItemLevel;
}