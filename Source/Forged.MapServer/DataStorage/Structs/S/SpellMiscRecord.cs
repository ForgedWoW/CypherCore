namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SpellMiscRecord
{
    public uint Id;
    public int[] Attributes = new int[15];
    public byte DifficultyID;
    public ushort CastingTimeIndex;
    public ushort DurationIndex;
    public ushort RangeIndex;
    public byte SchoolMask;
    public float Speed;
    public float LaunchDelay;
    public float MinDuration;
    public uint SpellIconFileDataID;
    public uint ActiveIconFileDataID;
    public uint ContentTuningID;
    public int ShowFutureSpellPlayerConditionID;
    public int SpellVisualScript;
    public int ActiveSpellVisualScript;
    public uint SpellID;
}