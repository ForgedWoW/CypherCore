namespace Forged.MapServer.DataStorage.Structs.S;

public sealed class SpellAuraRestrictionsRecord
{
    public uint Id;
    public uint DifficultyID;
    public int CasterAuraState;
    public int TargetAuraState;
    public int ExcludeCasterAuraState;
    public int ExcludeTargetAuraState;
    public uint CasterAuraSpell;
    public uint TargetAuraSpell;
    public uint ExcludeCasterAuraSpell;
    public uint ExcludeTargetAuraSpell;
    public int CasterAuraType;
    public int TargetAuraType;
    public int ExcludeCasterAuraType;
    public int ExcludeTargetAuraType;
    public uint SpellID;
}