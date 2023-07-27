using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SkillLineAbilityRecord
{
    public long RaceMask;
    public string AbilityVerb;
    public string AbilityAllVerb;
    public uint Id;
    public ushort SkillLine;
    public uint Spell;
    public short MinSkillLineRank;
    public int ClassMask;
    public uint SupercedesSpell;
    public AbilityLearnType AcquireMethod;
    public ushort TrivialSkillLineRankHigh;
    public ushort TrivialSkillLineRankLow;
    public SkillLineAbilityFlags Flags;
    public byte NumSkillUps;
    public short UniqueBit;
    public short TradeSkillCategoryID;
    public ushort SkillupSkillLineID;
}