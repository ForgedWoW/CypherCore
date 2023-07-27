namespace Forged.MapServer.DataStorage.Structs.P;

public sealed record PvpTalentRecord
{
    public string Description;
    public uint Id;
    public int SpecID;
    public uint SpellID;
    public uint OverridesSpellID;
    public int Flags;
    public int ActionBarSpellID;
    public int PvpTalentCategoryID;
    public int LevelRequired;
    public int PlayerConditionID;
}