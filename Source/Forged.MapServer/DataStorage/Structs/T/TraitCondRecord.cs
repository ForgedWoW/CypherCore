using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.T;

public sealed record TraitCondRecord
{
    public uint Id;
    public int CondType;
    public int TraitTreeID;
    public int GrantedRanks;
    public uint QuestID;
    public uint AchievementID;
    public int SpecSetID;
    public int TraitNodeGroupID;
    public int TraitNodeID;
    public int TraitCurrencyID;
    public int SpentAmountRequired;
    public int Flags;
    public int RequiredLevel;
    public int FreeSharedStringID;
    public int SpendMoreSharedStringID;

    public TraitConditionType GetCondType() { return (TraitConditionType)CondType; }
}