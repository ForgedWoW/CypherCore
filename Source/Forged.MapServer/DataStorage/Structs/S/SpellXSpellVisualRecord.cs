namespace Forged.MapServer.DataStorage.Structs.S;

public sealed class SpellXSpellVisualRecord
{
    public uint Id;
    public byte DifficultyID;
    public uint SpellVisualID;
    public float Probability;
    public int Flags;
    public int Priority;
    public int SpellIconFileID;
    public int ActiveIconFileID;
    public ushort ViewerUnitConditionID;
    public uint ViewerPlayerConditionID;
    public ushort CasterUnitConditionID;
    public uint CasterPlayerConditionID;
    public uint SpellID;
}