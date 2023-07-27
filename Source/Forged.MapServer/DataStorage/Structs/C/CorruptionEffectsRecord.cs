namespace Forged.MapServer.DataStorage.Structs.C;

public sealed record CorruptionEffectsRecord
{
    public uint Id;
    public float MinCorruption;
    public uint Aura;
    public int PlayerConditionID;
    public int Flags;
}