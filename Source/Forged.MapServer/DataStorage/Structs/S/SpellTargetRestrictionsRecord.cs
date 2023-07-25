namespace Forged.MapServer.DataStorage.Structs.S;

public sealed class SpellTargetRestrictionsRecord
{
    public uint Id;
    public byte DifficultyID;
    public float ConeDegrees;
    public byte MaxTargets;
    public uint MaxTargetLevel;
    public ushort TargetCreatureType;
    public int Targets;
    public float Width;
    public uint SpellID;
}