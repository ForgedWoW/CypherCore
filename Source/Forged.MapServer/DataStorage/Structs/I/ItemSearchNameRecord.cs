namespace Forged.MapServer.DataStorage.Structs.I;

public sealed record ItemSearchNameRecord
{
    public uint Id;
    public long AllowableRace;
    public string Display;
    public byte OverallQualityID;
    public int ExpansionID;
    public ushort MinFactionID;
    public int MinReputation;
    public int AllowableClass;
    public sbyte RequiredLevel;
    public ushort RequiredSkill;
    public ushort RequiredSkillRank;
    public uint RequiredAbility;
    public ushort ItemLevel;
    public int[] Flags = new int[4];
}