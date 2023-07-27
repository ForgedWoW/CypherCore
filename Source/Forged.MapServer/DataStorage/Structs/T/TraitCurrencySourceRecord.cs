using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.T;

public sealed record TraitCurrencySourceRecord
{
    public LocalizedString Requirement;
    public uint Id;
    public int TraitCurrencyID;
    public int Amount;
    public uint QuestID;
    public uint AchievementID;
    public uint PlayerLevel;
    public int TraitNodeEntryID;
    public int OrderIndex;
}